using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Tickora.Application;
using Tickora.Domain;
using Tickora.Infrastructure.Persistence;

namespace Tickora.Infrastructure.BackgroundJobs;

public sealed class SlaMonitoringService(IServiceScopeFactory scopes, ILogger<SlaMonitoringService> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Sla:ScanSeconds", 60), 5, 3600)));
        do
        {
            try { await ScanAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "SLA monitoring scan failed; retrying next interval"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();
        var now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        await using var db = await factory.CreateDbContextAsync(ct);
        // A transaction-scoped SQL application lock gives a single scanner ownership across replicas.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sp_getapplock @Resource = 'Tickora:SlaMonitor', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; IF @r < 0 THROW 51000, 'SLA monitor busy', 1;", ct);
        var horizon = now.AddMinutes(Math.Clamp(configuration.GetValue("Sla:WarningMinutes", 30), 1, 1440));
        var candidateQuery = db.Tickets.AsNoTracking().Where(t =>
            t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved &&
            ((t.FirstResponseAt == null && t.FirstResponseDue <= horizon) ||
                (!t.Pauses.Any(p => p.EndedAt == null) && t.ResolutionDue <= horizon)));
        var queueStaff = await (from m in db.Memberships
                                join u in db.Users on m.AgentId equals u.Id
                                where u.Active && db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "Agent" || r.Name == "Admin")))
                                select new { m.QueueId, m.AgentId }).ToListAsync(ct);
        var admins = await (from ur in db.UserRoles
                            join r in db.Roles on ur.RoleId equals r.Id
                            join u in db.Users on ur.UserId equals u.Id
                            where r.Name == "Admin" && u.Active
                            select u.Id).ToListAsync(ct);
        var recipients = new HashSet<string>();
        long afterNumber = 0;
        while (true)
        {
            var candidates = await candidateQuery.Where(t => t.Number > afterNumber).OrderBy(t => t.Number).Take(250)
              .Select(t => new
              {
                  t.Id,
                  t.Number,
                  t.QueueId,
                  t.AssignedAgentId,
                  t.FirstResponseAt,
                  t.FirstResponseDue,
                  t.ResolutionDue,
                  t.ResolutionCycle,
                  Paused = t.Pauses.Any(p => p.EndedAt == null)
              }).ToListAsync(ct);
            if (candidates.Count == 0) break;
            afterNumber = candidates[^1].Number;
            foreach (var t in candidates)
            {
                var owners = t.AssignedAgentId is not null && queueStaff.Any(m => m.QueueId == t.QueueId && m.AgentId == t.AssignedAgentId)
                    ? new[] { t.AssignedAgentId }
                    : queueStaff.Where(m => m.QueueId == t.QueueId).Select(m => m.AgentId).Concat(admins).Distinct().ToArray();
                foreach (var (kind, due, cycle) in new[] { ("response", t.FirstResponseDue, 0), ("resolution", t.ResolutionDue, t.ResolutionCycle) })
                {
                    if (kind == "response" && t.FirstResponseAt != null || kind == "resolution" && t.Paused || due > horizon) continue;
                    var breached = due <= now;
                    var key = $"sla:{t.Id}:{kind}:{cycle}:{(breached ? "breach" : "warning")}";
                    var raised = false;
                    foreach (var owner in owners)
                    {
                        if (await db.Notifications.AnyAsync(n => n.EventKey == key && n.RecipientId == owner, ct)) continue;
                        db.Notifications.Add(new Notification
                        {
                            TicketId = t.Id,
                            RecipientId = owner,
                            EventKey = key,
                            Title = $"TK-{t.Number}: {kind} SLA {(breached ? "breached" : "approaching deadline")}",
                            CreatedAt = now
                        });
                        recipients.Add(owner); raised = true;
                    }
                    if (raised && !await db.Set<TicketActivity>().AnyAsync(a => a.TicketId == t.Id && a.EventKey == key, ct))
                        db.Set<TicketActivity>().Add(new TicketActivity
                        {
                            TicketId = t.Id,
                            ActorId = "system",
                            EventKey = key,
                            Description = $"{kind} SLA {(breached ? "breach" : "warning")}",
                            Internal = true,
                            CreatedAt = now
                        });
                }
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        await tx.CommitAsync(ct);
        foreach (var recipient in recipients) await publisher.PublishAsync(recipient, ct);
    }
}

using Microsoft.EntityFrameworkCore;
using Tickora.Application;
using Tickora.Domain;

namespace Tickora.Infrastructure.Persistence;

public sealed class HelpdeskQueries(IDbContextFactory<ApplicationDbContext> factory, IClock clock) : IHelpdeskQueries
{
    public async Task<Page<CustomerRow>> CustomersAsync(Actor actor, string? search, int page, CancellationToken ct)
    {
        if (!actor.Staff) throw TicketService.Forbidden();
        await using var db = await factory.CreateDbContextAsync(ct);
        var tickets = Visible(db, actor);
        var customers = from u in db.Users
                        where (actor.Admin || tickets.Any(t => t.CustomerId == u.Id)) &&
                            db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Customer")) &&
                            (search == null || u.DisplayName.Contains(search) || u.Email!.Contains(search))
                        orderby u.DisplayName
                        select new CustomerRow(u.Id, u.DisplayName, u.Email!, tickets.Count(t => t.CustomerId == u.Id),
                            tickets.Count(t => t.CustomerId == u.Id && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved));
        var count = await customers.CountAsync(ct); page = Math.Clamp(page, 1, Math.Max(1, (count + 11) / 12));
        return new(await customers.Skip((page - 1) * 12).Take(12).ToListAsync(ct), count, page, 12);
    }
    internal static IQueryable<Ticket> Visible(ApplicationDbContext db, Actor actor) => actor.Admin ? db.Tickets :
        actor.Agent ? db.Tickets.Where(x => actor.Queues.Contains(x.QueueId)) : db.Tickets.Where(x => x.CustomerId == actor.Id);

    private static IQueryable<Ticket> ApplyCriteria(IQueryable<Ticket> query, TicketFilter filter)
    {
        if (filter.From.HasValue && filter.To.HasValue && filter.To <= filter.From) throw TicketService.Invalid("The end date must follow the start date.");
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status);
        if (filter.Priority.HasValue) query = query.Where(x => x.Priority == filter.Priority);
        if (filter.Queue.HasValue) query = query.Where(x => x.QueueId == filter.Queue);
        if (filter.Category.HasValue) query = query.Where(x => x.CategoryId == filter.Category);
        if (filter.From.HasValue) query = query.Where(x => x.CreatedAt >= filter.From);
        if (filter.To.HasValue) query = query.Where(x => x.CreatedAt < filter.To);
        return query;
    }

    public async Task<Page<TicketRow>> TicketsAsync(Actor actor, TicketFilter filter, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var q = ApplyCriteria(Visible(db, actor).AsNoTracking(), filter);
        if (!string.IsNullOrWhiteSpace(filter.Search)) q = q.Where(x => x.Subject.Contains(filter.Search) || ("TK-" + x.Number).Contains(filter.Search));
        if (filter.Agent is not null) q = q.Where(x => x.AssignedAgentId == filter.Agent);
        if (filter.Mine) q = q.Where(x => x.AssignedAgentId == actor.Id);
        if (filter.Unassigned) q = q.Where(x => x.AssignedAgentId == null);
        if (filter.Overdue) q = q.Where(x => x.ResolutionDue < clock.UtcNow && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && !x.Pauses.Any(p => p.EndedAt == null));
        var count = await q.CountAsync(ct);
        var size = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling((double)count / size)));
        q = filter.Sort switch { "oldest" => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id), "priority" => q.OrderByDescending(x => x.Priority).ThenByDescending(x => x.UpdatedAt).ThenBy(x => x.Id), _ => q.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id) };
        var rows = await (from t in q.Skip((page - 1) * size).Take(size)
                          join c in db.Users on t.CustomerId equals c.Id
                          join queue in db.Queues on t.QueueId equals queue.Id
                          join agent in db.Users on t.AssignedAgentId equals agent.Id into agents
                          from a in agents.DefaultIfEmpty()
                          select new
                          {
                              t.Id,
                              t.Number,
                              t.Subject,
                              Customer = c.DisplayName,
                              Agent = a == null ? null : a.DisplayName,
                              Queue = queue.Name,
                              t.Status,
                              t.Priority,
                              t.UpdatedAt,
                              t.ResolutionDue,
                              t.RowVersion,
                              Paused = t.Pauses.Any(p => p.EndedAt == null)
                          }).ToListAsync(ct);
        return new(rows.Select(x => new TicketRow(x.Id, x.Number, x.Subject, x.Customer, x.Agent, x.Queue, x.Status, x.Priority, x.UpdatedAt, x.ResolutionDue, x.Paused, Convert.ToBase64String(x.RowVersion))).ToList(), count, page, size);
    }

    public async Task<TicketDetail> DetailAsync(Actor actor, Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var ticket = await Visible(db, actor).AsNoTracking()
            .Include(x => x.Messages.Where(m => actor.Staff || m.Visibility == MessageVisibility.Public))
            .Include(x => x.Activities.Where(a => actor.Staff || !a.Internal))
            .Include(x => x.Attachments.Where(a => actor.Staff || !a.Internal)).Include(x => x.Pauses)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new WorkflowException("not_found", "Ticket not found.", 404);
        var customer = await db.Users.Where(x => x.Id == ticket.CustomerId).Select(x => x.DisplayName).SingleAsync(ct);
        var agent = await db.Users.Where(x => x.Id == ticket.AssignedAgentId).Select(x => x.DisplayName).SingleOrDefaultAsync(ct);
        return new(ticket, customer, agent, await db.Queues.Where(x => x.Id == ticket.QueueId).Select(x => x.Name).SingleAsync(ct),
            await db.Categories.Where(x => x.Id == ticket.CategoryId).Select(x => x.Name).SingleAsync(ct), await db.Feedback.AsNoTracking().SingleOrDefaultAsync(x => x.TicketId == id, ct));
    }

    public async Task<Lookups> LookupsAsync(Actor actor, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        // Format GUIDs after materialization: SQL Server's uppercase conversion does not
        // match the lowercase values emitted by Blazor's Guid-bound select controls.
        var queueRows = await db.Queues.Where(x => x.Active && (!actor.Agent || actor.Admin || actor.Queues.Contains(x.Id))).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var categoryRows = await db.Categories.Where(x => x.Active).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var queues = queueRows.Select(x => new Choice(x.Id.ToString(), x.Name)).ToList();
        var cats = categoryRows.Select(x => new Choice(x.Id.ToString(), x.Name)).ToList();
        var agents = actor.Staff ? await (from u in db.Users where u.Active && db.Memberships.Any(m => m.AgentId == u.Id && (actor.Admin || actor.Queues.Contains(m.QueueId))) orderby u.DisplayName select new Choice(u.Id, u.DisplayName)).ToListAsync(ct) : [];
        return new(queues, cats, agents, actor.Staff ? await db.Responses.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Title).ToListAsync(ct) : []);
    }

    public async Task<Dashboard> DashboardAsync(Actor actor, CancellationToken ct, TicketFilter? filter = null)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var q = ApplyCriteria(Visible(db, actor).AsNoTracking(), filter ?? new());
        var total = await q.CountAsync(ct);
        var active = await q.CountAsync(x => x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed, ct);
        var waiting = await q.CountAsync(x => x.Status == TicketStatus.WaitingForCustomer, ct);
        var resolved = await q.CountAsync(x => x.Status == TicketStatus.Resolved || x.Status == TicketStatus.Closed, ct);
        var overdue = await q.CountAsync(x => x.ResolutionDue < clock.UtcNow && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && !x.Pauses.Any(p => p.EndedAt == null), ct);
        var status = await q.GroupBy(x => x.Status).Select(x => new { Label = x.Key, Count = x.Count() }).ToListAsync(ct);
        var queues = await (from t in q join queue in db.Queues on t.QueueId equals queue.Id group t by queue.Name into g select new CountPoint(g.Key, g.Count())).ToListAsync(ct);
        var start = clock.UtcNow.AddDays(-13).Date;
        var volume = await q.Where(x => x.CreatedAt >= start).GroupBy(x => x.CreatedAt.Date).Select(x => new { Date = x.Key, Count = x.Count() }).ToListAsync(ct);
        var points = Enumerable.Range(0, 14).Select(i => { var d = start.AddDays(i); return new CountPoint(d.ToString("dd MMM"), volume.SingleOrDefault(x => x.Date == d)?.Count ?? 0); }).ToList();
        var feedback = await db.Feedback.Where(f => q.Any(t => t.Id == f.TicketId)).Select(f => (double?)f.Rating).AverageAsync(ct);
        var firstHours = await q.Where(x => x.FirstResponseAt != null).Select(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAt, x.FirstResponseAt!.Value) / 60).AverageAsync(ct);
        var resolutionHours = await q.Where(x => x.ResolvedAt != null).Select(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAt, x.ResolvedAt!.Value) / 60).AverageAsync(ct);
        var completed = await q.CountAsync(x => x.ResolvedAt != null, ct);
        var met = await q.CountAsync(x => x.ResolvedAt != null && x.ResolvedAt <= x.ResolutionDue && x.FirstResponseAt != null && x.FirstResponseAt <= x.FirstResponseDue, ct);
        var categories = await (from t in q join c in db.Categories on t.CategoryId equals c.Id group t by c.Name into g select new CountPoint(g.Key, g.Count())).ToListAsync(ct);
        var workloads = await (from t in q
                               where t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed
                               join a in db.Users on t.AssignedAgentId equals a.Id
                               group t by a.DisplayName into g
                               select new CountPoint(g.Key, g.Count())).ToListAsync(ct);
        var resolutionVolume = await q.Where(t => t.ResolvedAt != null && t.ResolvedAt >= start)
            .GroupBy(t => t.ResolvedAt!.Value.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync(ct);
        var resolutionPoints = Enumerable.Range(0, 14).Select(i => { var d = start.AddDays(i); return new CountPoint(d.ToString("dd MMM"), resolutionVolume.SingleOrDefault(x => x.Date == d)?.Count ?? 0); }).ToList();
        var activeAgents = !actor.Staff ? 0 : await (from u in db.Users
                                                     join ur in db.UserRoles on u.Id equals ur.UserId
                                                     join r in db.Roles on ur.RoleId equals r.Id
                                                     where u.Active && r.Name == "Agent" && (actor.Admin || db.Memberships.Any(m => m.AgentId == u.Id && actor.Queues.Contains(m.QueueId)))
                                                     select u.Id).Distinct().CountAsync(ct);
        return new(total, active, waiting, resolved, overdue, feedback, status.Select(x => new CountPoint(x.Label.ToString(), x.Count)).ToList(),
            queues, points, firstHours, resolutionHours, completed == 0 ? null : 100.0 * met / completed, activeAgents, categories, workloads,
            [new("Both targets met", met), new("At least one target missed", completed - met)], resolutionPoints);
    }

    public async Task<IReadOnlyList<KnowledgeArticle>> ArticlesAsync(string? search, bool drafts, Actor? actor, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Articles.AsNoTracking().Where(x => x.Published || (drafts && actor != null && actor.Staff))
            .Where(x => search == null || x.Title.Contains(search) || x.Summary.Contains(search) || x.Category.Contains(search))
            .OrderByDescending(x => x.Featured).ThenBy(x => x.Title).Take(100).ToListAsync(ct);
    }
    public async Task<KnowledgeArticle?> ArticleAsync(string slug, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Articles.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.Published, ct);
    }
    public async Task<Page<Notification>> NotificationsAsync(string userId, CancellationToken ct, int page = 1, int size = 25, bool unreadOnly = false)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.Notifications.AsNoTracking().Where(x => x.RecipientId == userId && (!unreadOnly || x.ReadAt == null));
        var count = await query.CountAsync(ct);
        size = Math.Clamp(size, 1, 100);
        page = Math.Clamp(page, 1, Math.Max(1, (count + size - 1) / size));
        return new(await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct), count, page, size);
    }
    public async Task<int> UnreadCountAsync(string userId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Notifications.CountAsync(x => x.RecipientId == userId && x.ReadAt == null, ct);
    }
    public async Task MarkReadAsync(string userId, Guid id, DateTimeOffset now, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Notifications.Where(x => x.Id == id && x.RecipientId == userId && x.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, now), ct);
    }
}

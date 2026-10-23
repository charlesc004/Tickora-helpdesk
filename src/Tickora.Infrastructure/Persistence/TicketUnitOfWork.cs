using Microsoft.EntityFrameworkCore;
using Tickora.Application;
using Tickora.Domain;

namespace Tickora.Infrastructure.Persistence;

public sealed class TicketUnitOfWorkFactory(IDbContextFactory<ApplicationDbContext> factory) : ITicketUnitOfWorkFactory
{
    public async Task<ITicketUnitOfWork> CreateAsync(CancellationToken ct) => new TicketUnitOfWork(await factory.CreateDbContextAsync(ct));
}
internal sealed class TicketUnitOfWork(ApplicationDbContext db) : ITicketUnitOfWork
{
    public Task<Ticket?> LoadAsync(Guid id, CancellationToken ct) => db.Tickets.Include(x => x.Messages).Include(x => x.Activities)
        .Include(x => x.Pauses).Include(x => x.Attachments).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Ticket?> FindSubmissionAsync(string customerId, Guid submissionId, CancellationToken ct) =>
        db.Tickets.SingleOrDefaultAsync(x => x.CustomerId == customerId && x.SubmissionId == submissionId, ct);
    public Task<SlaPolicy> PolicyAsync(TicketPriority priority, CancellationToken ct) => db.SlaPolicies.SingleAsync(x => x.Priority == priority, ct);
    public async Task<bool> ValidDestinationAsync(Guid queue, Guid category, CancellationToken ct) =>
        await db.Queues.AnyAsync(x => x.Id == queue && x.Active, ct) && await db.Categories.AnyAsync(x => x.Id == category && x.Active, ct);
    public async Task<bool> EligibleAgentAsync(string agent, Guid queue, CancellationToken ct) =>
        await db.Memberships.AnyAsync(x => x.QueueId == queue && x.AgentId == agent, ct) &&
        await (from u in db.Users
               join ur in db.UserRoles on u.Id equals ur.UserId
               join r in db.Roles on ur.RoleId equals r.Id
               where u.Id == agent && u.Active && (r.Name == "Agent" || r.Name == "Admin")
               select u.Id).AnyAsync(ct);
    public async Task<IReadOnlyList<string>> QueueRecipientsAsync(Guid queue, CancellationToken ct) =>
        await (from m in db.Memberships join u in db.Users on m.AgentId equals u.Id where m.QueueId == queue && u.Active select u.Id).Distinct().ToListAsync(ct);
    public void Add(Ticket ticket, Guid submissionId) { ticket.SubmissionId = submissionId; db.Tickets.Add(ticket); }
    public void Notify(Notification notification) => db.Notifications.Add(notification);
    public void Feedback(CustomerFeedback feedback) => db.Feedback.Add(feedback);
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new WorkflowException("conflict", "This record changed. Reload before saving.", 409); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        { throw new WorkflowException("duplicate", "This submission has already been saved. Refresh to see it.", 409); }
    }
    public ValueTask DisposeAsync() => db.DisposeAsync();
}

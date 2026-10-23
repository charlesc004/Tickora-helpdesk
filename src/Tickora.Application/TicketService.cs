using System.Text.Json;
using Tickora.Domain;

namespace Tickora.Application;

public sealed class TicketService(ITicketUnitOfWorkFactory factory, ICurrentUser currentUser, IClock clock, INotificationPublisher publisher)
{
    public async Task<Guid> CreateAsync(CreateTicket input, CancellationToken ct = default)
    {
        var actor = await currentUser.GetAsync(ct);
        Text(input.Subject, 5, 180, "Subject"); Text(input.Description, 10, 10000, "Description");
        if (!Enum.IsDefined(input.Priority) || input.SubmissionId == Guid.Empty) throw Invalid("Invalid ticket priority or submission identifier.");
        await using var work = await factory.CreateAsync(ct);
        var existing = await work.FindSubmissionAsync(actor.Id, input.SubmissionId, ct);
        if (existing is not null) return existing.Id;
        if (!await work.ValidDestinationAsync(input.QueueId, input.CategoryId, ct)) throw Invalid("Choose an active queue and category.");
        var policy = await work.PolicyAsync(input.Priority, ct);
        var calendar = JsonSerializer.Deserialize<BusinessCalendar>(policy.CalendarJson)!;
        var ticket = new Ticket
        {
            Subject = input.Subject.Trim(),
            Description = input.Description.Trim(),
            CustomerId = actor.Id,
            QueueId = input.QueueId,
            CategoryId = input.CategoryId,
            Priority = input.Priority,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow,
            CalendarSnapshot = policy.CalendarJson,
            FirstResponseMinutes = policy.FirstResponseMinutes,
            ResolutionMinutes = policy.ResolutionMinutes,
            PauseWhileWaiting = policy.PauseWhileWaiting,
            FirstResponseDue = SlaCalculationPolicy.AddBusinessTime(clock.UtcNow, TimeSpan.FromMinutes(policy.FirstResponseMinutes), calendar),
            ResolutionDue = SlaCalculationPolicy.AddBusinessTime(clock.UtcNow, TimeSpan.FromMinutes(policy.ResolutionMinutes), calendar)
        };
        Activity(ticket, actor, "Ticket created");
        work.Add(ticket, input.SubmissionId);
        var recipients = await work.QueueRecipientsAsync(ticket.QueueId, ct);
        foreach (var recipient in recipients.Where(id => id != actor.Id))
            work.Notify(new Notification { TicketId = ticket.Id, RecipientId = recipient, Title = "New ticket in your support queue", EventKey = $"created:{ticket.Id}", CreatedAt = clock.UtcNow });
        await work.SaveAsync(ct);
        foreach (var recipient in recipients.Where(id => id != actor.Id)) await publisher.PublishAsync(recipient, ct);
        return ticket.Id;
    }

    public Task ReplyAsync(Guid id, ReplyTicket input, CancellationToken ct = default) => Mutate(id, input.Version, async (work, ticket, actor) =>
    {
        Text(input.Body, 1, 10000, "Message");
        if (input.SubmissionId == Guid.Empty) throw Invalid("A submission identifier is required.");
        if (ticket.Messages.Any(m => m.SubmissionId == input.SubmissionId && m.AuthorId == actor.Id)) return;
        if (input.Internal && !actor.Staff) throw Forbidden();
        if (ticket.Status is TicketStatus.Closed or TicketStatus.Resolved) throw Invalid("Reopen this ticket before replying.");
        ticket.Messages.Add(new TicketMessage
        {
            TicketId = id,
            AuthorId = actor.Id,
            AuthorName = actor.Name,
            Body = input.Body.Trim(),
            Visibility = input.Internal ? MessageVisibility.Internal : MessageVisibility.Public,
            CreatedAt = clock.UtcNow,
            SubmissionId = input.SubmissionId
        });
        if (actor.Staff && !input.Internal) ticket.FirstResponseAt ??= clock.UtcNow;
        if (!actor.Staff && ticket.Status == TicketStatus.WaitingForCustomer) ChangeStatus(ticket, TicketStatus.InProgress, actor);
        Activity(ticket, actor, input.Internal ? "Internal note added" : actor.Staff ? "Agent replied" : "Customer replied", input.Internal);
        if (!input.Internal) Notify(work, ticket, actor, "New reply on your ticket");
        await Task.CompletedTask;
    }, ct, publishCustomer: !input.Internal);

    public Task TransitionAsync(Guid id, TransitionTicket input, CancellationToken ct = default) => Mutate(id, input.Version, (work, ticket, actor) =>
    {
        ChangeStatus(ticket, input.Status, actor);
        Notify(work, ticket, actor, $"Ticket status: {input.Status}");
        return Task.CompletedTask;
    }, ct);

    public Task AssignAsync(Guid id, AssignTicket input, CancellationToken ct = default) => Mutate(id, input.Version, async (work, ticket, actor) =>
    {
        if (!actor.Staff) throw Forbidden();
        if (input.AgentId is not null && !await work.EligibleAgentAsync(input.AgentId, ticket.QueueId, ct)) throw Invalid("This agent is not an active member of this queue.");
        ticket.AssignedAgentId = input.AgentId;
        Activity(ticket, actor, input.AgentId is null ? "Ticket unassigned" : "Agent assignment updated");
        if (input.AgentId is not null) work.Notify(new Notification
        {
            RecipientId = input.AgentId,
            TicketId = id,
            Title = "A ticket was assigned to you",
            EventKey = Guid.NewGuid().ToString(),
            CreatedAt = clock.UtcNow
        });
    }, ct);

    public Task UpdateAsync(Guid id, UpdateTicket input, CancellationToken ct = default) => Mutate(id, input.Version, async (work, ticket, actor) =>
    {
        if (!actor.Staff) throw Forbidden();
        Text(input.Subject, 5, 180, "Subject");
        if (!Enum.IsDefined(input.Priority) || !await work.ValidDestinationAsync(ticket.QueueId, input.CategoryId, ct)) throw Invalid("Invalid category or priority.");
        // Priority changes affect triage only; historical SLA targets retain their creation snapshot.
        ticket.Subject = input.Subject.Trim(); ticket.CategoryId = input.CategoryId; ticket.Priority = input.Priority;
        Activity(ticket, actor, "Ticket details updated"); Notify(work, ticket, actor, "Ticket details updated");
    }, ct);

    public Task FeedbackAsync(Guid id, int rating, string comment, string version, CancellationToken ct = default) => Mutate(id, version, (work, ticket, actor) =>
    {
        if (actor.Staff || actor.Id != ticket.CustomerId) throw Forbidden();
        if (ticket.Status is not (TicketStatus.Resolved or TicketStatus.Closed) || rating is < 1 or > 5 || comment.Length > 1000) throw Invalid("Feedback needs a resolved ticket and a rating from 1 to 5.");
        work.Feedback(new CustomerFeedback { TicketId = id, Rating = rating, Comment = comment.Trim(), CreatedAt = clock.UtcNow });
        Activity(ticket, actor, "Customer feedback submitted"); return Task.CompletedTask;
    }, ct);

    public async Task AddAttachmentAsync(Guid id, TicketAttachment attachment, string version, CancellationToken ct = default) =>
        await Mutate(id, version, (_, ticket, actor) =>
        {
            if (attachment.Internal && !actor.Staff) throw Forbidden();
            attachment.TicketId = id; attachment.UploadedBy = actor.Id; attachment.CreatedAt = clock.UtcNow;
            ticket.Attachments.Add(attachment); Activity(ticket, actor, "Attachment added", attachment.Internal);
            return Task.CompletedTask;
        }, ct, publishCustomer: !attachment.Internal);

    private async Task Mutate(Guid id, string version, Func<ITicketUnitOfWork, Ticket, Actor, Task> action, CancellationToken ct, bool publishCustomer = true)
    {
        var actor = await currentUser.GetAsync(ct);
        await using var work = await factory.CreateAsync(ct);
        var ticket = await work.LoadAsync(id, ct) ?? throw new WorkflowException("not_found", "Ticket not found.", 404);
        if (!actor.CanAccess(ticket)) throw new WorkflowException("not_found", "Ticket not found.", 404);
        if (string.IsNullOrWhiteSpace(version) || version != Convert.ToBase64String(ticket.RowVersion))
            throw new WorkflowException("conflict", "This ticket changed. Reload it before saving your changes.", 409);
        await action(work, ticket, actor);
        ticket.UpdatedAt = clock.UtcNow;
        await work.SaveAsync(ct);
        // Persist first. Reconnecting clients can always recover notifications from SQL Server.
        if (publishCustomer) await publisher.PublishAsync(ticket.CustomerId, ct);
        if (ticket.AssignedAgentId is not null) await publisher.PublishAsync(ticket.AssignedAgentId, ct);
    }

    private void ChangeStatus(Ticket ticket, TicketStatus status, Actor actor)
    {
        if (!TicketTransitionPolicy.CanTransition(ticket.Status, status, actor.Staff)) throw Invalid("This status transition is not allowed.");
        var now = clock.UtcNow;
        if (ticket.Status == TicketStatus.WaitingForCustomer && ticket.PauseWhileWaiting)
        {
            var pause = ticket.Pauses.SingleOrDefault(p => p.EndedAt == null);
            if (pause is not null)
            {
                pause.EndedAt = now;
                var calendar = JsonSerializer.Deserialize<BusinessCalendar>(ticket.CalendarSnapshot)!;
                var duration = SlaCalculationPolicy.BusinessTimeBetween(pause.StartedAt, now, calendar);
                ticket.ResolutionDue = SlaCalculationPolicy.AddBusinessTime(ticket.ResolutionDue, duration, calendar);
            }
        }
        if (status == TicketStatus.WaitingForCustomer && ticket.PauseWhileWaiting)
            ticket.Pauses.Add(new SlaPause { TicketId = ticket.Id, StartedAt = now });
        if (status == TicketStatus.Resolved) ticket.ResolvedAt = now;
        if (status == TicketStatus.Closed) ticket.ClosedAt = now;
        if (status == TicketStatus.Open && ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            ticket.ResolvedAt = null; ticket.ClosedAt = null; ticket.ResolutionCycle++;
            ticket.ResolutionDue = SlaCalculationPolicy.AddBusinessTime(now, TimeSpan.FromMinutes(ticket.ResolutionMinutes), JsonSerializer.Deserialize<BusinessCalendar>(ticket.CalendarSnapshot)!);
        }
        ticket.Status = status; Activity(ticket, actor, $"Status changed to {status}");
    }

    private void Activity(Ticket ticket, Actor actor, string text, bool internalEvent = false) =>
        ticket.Activities.Add(new TicketActivity { TicketId = ticket.Id, ActorId = actor.Id, Description = text, Internal = internalEvent, CreatedAt = clock.UtcNow });
    private void Notify(ITicketUnitOfWork work, Ticket ticket, Actor actor, string title)
    {
        var recipient = actor.Staff ? ticket.CustomerId : ticket.AssignedAgentId;
        if (recipient is not null && recipient != actor.Id) work.Notify(new Notification
        {
            RecipientId = recipient,
            TicketId = ticket.Id,
            Title = title,
            CreatedAt = clock.UtcNow,
            EventKey = Guid.NewGuid().ToString()
        });
    }
    public static void Text(string? text, int min, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < min || text.Length > max) throw Invalid($"{field} must contain {min}–{max} characters.");
    }
    public static WorkflowException Invalid(string message) => new("validation", message);
    public static WorkflowException Forbidden() => new("forbidden", "You do not have permission to perform this action.", 403);
}

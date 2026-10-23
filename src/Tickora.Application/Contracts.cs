using Tickora.Domain;

namespace Tickora.Application;

public sealed record Actor(string Id, string Name, bool Admin, bool Agent, IReadOnlySet<Guid> Queues)
{
    public bool Staff => Admin || Agent;
    public bool CanAccess(Ticket ticket) => Admin || (Agent && Queues.Contains(ticket.QueueId)) || (!Staff && ticket.CustomerId == Id);
}
public interface ICurrentUser { Task<Actor> GetAsync(CancellationToken ct = default); }
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface INotificationPublisher { Task PublishAsync(string recipient, CancellationToken ct = default); }
public sealed class WorkflowException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
public sealed record TicketFilter(string? Search = null, TicketStatus? Status = null, TicketPriority? Priority = null,
    Guid? Queue = null, Guid? Category = null, string? Agent = null, bool Mine = false, bool Unassigned = false,
    bool Overdue = false, DateTimeOffset? From = null, DateTimeOffset? To = null, int Page = 1, int PageSize = 12, string Sort = "updated");
public sealed record TicketRow(Guid Id, long Number, string Subject, string Customer, string? Agent, string Queue,
    TicketStatus Status, TicketPriority Priority, DateTimeOffset UpdatedAt, DateTimeOffset ResolutionDue, bool Paused, string Version);
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int Number, int Size)
{
    public int Pages => Math.Max(1, (int)Math.Ceiling((double)Total / Size));
}
public sealed record Choice(string Id, string Name);
public sealed record Lookups(IReadOnlyList<Choice> Queues, IReadOnlyList<Choice> Categories, IReadOnlyList<Choice> Agents, IReadOnlyList<CannedResponse> Responses);
public sealed record Dashboard(int Total, int Active, int Waiting, int Resolved, int Overdue,
    double? Satisfaction, IReadOnlyList<CountPoint> Statuses, IReadOnlyList<CountPoint> Queues, IReadOnlyList<CountPoint> Volume,
    double? FirstResponseHours, double? ResolutionHours, double? Compliance, int ActiveAgents,
    IReadOnlyList<CountPoint> Categories, IReadOnlyList<CountPoint> AgentWorkload,
    IReadOnlyList<CountPoint> SlaPerformance, IReadOnlyList<CountPoint> ResolutionVolume);
public sealed record CountPoint(string Label, int Count);
public sealed record CustomerRow(string Id, string Name, string Email, int Tickets, int ActiveTickets);
public sealed record TicketDetail(Ticket Ticket, string Customer, string? Agent, string Queue, string Category, CustomerFeedback? Feedback);
public sealed record CreateTicket(string Subject, string Description, Guid QueueId, Guid CategoryId, TicketPriority Priority, Guid SubmissionId);
public sealed record ReplyTicket(string Body, bool Internal, Guid SubmissionId, string Version);
public sealed record TransitionTicket(TicketStatus Status, string Version);
public sealed record AssignTicket(string? AgentId, string Version);
public sealed record UpdateTicket(string Subject, Guid CategoryId, TicketPriority Priority, string Version);

public interface IHelpdeskQueries
{
    Task<Page<TicketRow>> TicketsAsync(Actor actor, TicketFilter filter, CancellationToken ct);
    Task<TicketDetail> DetailAsync(Actor actor, Guid id, CancellationToken ct);
    Task<Lookups> LookupsAsync(Actor actor, CancellationToken ct);
    Task<Dashboard> DashboardAsync(Actor actor, CancellationToken ct, TicketFilter? filter = null);
    Task<IReadOnlyList<KnowledgeArticle>> ArticlesAsync(string? search, bool drafts, Actor? actor, CancellationToken ct);
    Task<KnowledgeArticle?> ArticleAsync(string slug, CancellationToken ct);
    Task<Page<Notification>> NotificationsAsync(string userId, CancellationToken ct, int page = 1, int size = 25, bool unreadOnly = false);
    Task<int> UnreadCountAsync(string userId, CancellationToken ct);
    Task MarkReadAsync(string userId, Guid id, DateTimeOffset now, CancellationToken ct);
    Task<Page<CustomerRow>> CustomersAsync(Actor actor, string? search, int page, CancellationToken ct);
}
// One short-lived, transactionally saved aggregate per operation; no EF dependencies cross this boundary.
public interface ITicketUnitOfWork : IAsyncDisposable
{
    Task<Ticket?> LoadAsync(Guid id, CancellationToken ct);
    Task<Ticket?> FindSubmissionAsync(string customerId, Guid submissionId, CancellationToken ct);
    Task<SlaPolicy> PolicyAsync(TicketPriority priority, CancellationToken ct);
    Task<bool> ValidDestinationAsync(Guid queue, Guid category, CancellationToken ct);
    Task<bool> EligibleAgentAsync(string agent, Guid queue, CancellationToken ct);
    Task<IReadOnlyList<string>> QueueRecipientsAsync(Guid queue, CancellationToken ct);
    void Add(Ticket ticket, Guid submissionId);
    void Notify(Notification notification);
    void Feedback(CustomerFeedback feedback);
    Task SaveAsync(CancellationToken ct);
}
public interface ITicketUnitOfWorkFactory { Task<ITicketUnitOfWork> CreateAsync(CancellationToken ct); }
public interface IFileStorage
{
    Task<string> WriteAsync(Stream stream, CancellationToken ct);
    Task<Stream> ReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}

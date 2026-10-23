namespace Tickora.Domain;

public enum TicketStatus { Open, InProgress, WaitingForCustomer, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Urgent }
public enum MessageVisibility { Public, Internal }

public sealed class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Number { get; set; }
    public Guid SubmissionId { get; set; }
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string? AssignedAgentId { get; set; }
    public Guid QueueId { get; set; }
    public Guid CategoryId { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset FirstResponseDue { get; set; }
    public DateTimeOffset ResolutionDue { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public string CalendarSnapshot { get; set; } = "";
    public bool PauseWhileWaiting { get; set; }
    public int ResolutionCycle { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<TicketMessage> Messages { get; set; } = [];
    public List<TicketActivity> Activities { get; set; } = [];
    public List<SlaPause> Pauses { get; set; } = [];
    public List<TicketAttachment> Attachments { get; set; } = [];
}

public sealed class TicketMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid SubmissionId { get; set; }
    public string AuthorId { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public MessageVisibility Visibility { get; set; }
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TicketActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public string ActorId { get; set; } = "";
    public string Description { get; set; } = "";
    public string? EventKey { get; set; }
    public bool Internal { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SlaPause
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}

public sealed class SupportQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}
public sealed class TicketCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}
public sealed class QueueMembership
{
    public Guid QueueId { get; set; }
    public string AgentId { get; set; } = "";
}
public sealed class SlaPolicy
{
    public TicketPriority Priority { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool PauseWhileWaiting { get; set; } = true;
    public string CalendarJson { get; set; } = "";
    public byte[] RowVersion { get; set; } = [];
}
public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RecipientId { get; set; } = "";
    public Guid TicketId { get; set; }
    public string Title { get; set; } = "";
    public string EventKey { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
public sealed class KnowledgeArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Body { get; set; } = "";
    public string Category { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public bool Published { get; set; }
    public bool Featured { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class TicketAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public string FileName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public bool Internal { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class CustomerFeedback
{
    public Guid TicketId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class CannedResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool Active { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

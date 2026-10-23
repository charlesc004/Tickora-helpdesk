using System.Text.Json;
using Tickora.Application;
using Tickora.Domain;

namespace Tickora.UnitTests;

public sealed class WorkflowTests
{
    private static readonly Guid Queue = Guid.NewGuid();
    private static Actor Agent => new("agent", "Agent", false, true, new HashSet<Guid> { Queue });
    private static Actor Customer => new("customer", "Customer", false, false, new HashSet<Guid>());
    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.Closed, true, false)]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress, true, true)]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress, false, false)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, false, false)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open, false, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved, true, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Open, false, true)]
    [InlineData(TicketStatus.WaitingForCustomer, TicketStatus.InProgress, false, true)]
    public void Transitions_are_explicit(TicketStatus from, TicketStatus to, bool staff, bool expected) => Assert.Equal(expected, TicketTransitionPolicy.CanTransition(from, to, staff));

    [Fact] public void Agent_cannot_access_other_queues() => Assert.False(Agent.CanAccess(new Ticket { QueueId = Guid.NewGuid(), CustomerId = "other" }));
    [Fact] public void Customer_cannot_access_someone_elses_ticket() => Assert.False(Customer.CanAccess(new Ticket { CustomerId = "other" }));
    [Fact] public void Admin_has_explicit_global_access() => Assert.True((Agent with { Admin = true }).CanAccess(new Ticket()));

    [Fact]
    public async Task Private_note_does_not_count_as_first_response()
    {
        var f = new Fixture(Agent);
        await f.Service.ReplyAsync(f.Ticket.Id, new("Private analysis", true, Guid.NewGuid(), f.Version));
        Assert.Null(f.Ticket.FirstResponseAt); Assert.Empty(f.Work.Notifications);
        Assert.Equal(MessageVisibility.Internal, Assert.Single(f.Ticket.Messages).Visibility);
    }
    [Fact]
    public async Task Public_agent_reply_counts_once_and_notifies_customer()
    {
        var f = new Fixture(Agent);
        await f.Service.ReplyAsync(f.Ticket.Id, new("We can help", false, Guid.NewGuid(), f.Version));
        var first = f.Ticket.FirstResponseAt; f.Clock.UtcNow = f.Clock.UtcNow.AddHours(1);
        await f.Service.ReplyAsync(f.Ticket.Id, new("An update", false, Guid.NewGuid(), f.Version));
        Assert.Equal(first, f.Ticket.FirstResponseAt); Assert.Equal(2, f.Work.Notifications.Count);
        Assert.All(f.Work.Notifications, n => Assert.Equal("customer", n.RecipientId));
    }
    [Fact]
    public async Task Customer_cannot_create_internal_note()
    {
        var f = new Fixture(Customer);
        var e = await Assert.ThrowsAsync<WorkflowException>(() => f.Service.ReplyAsync(f.Ticket.Id, new("Private note", true, Guid.NewGuid(), f.Version)));
        Assert.Equal(403, e.Status); Assert.Empty(f.Ticket.Messages); Assert.Equal(0, f.Work.Saves);
    }
    [Fact]
    public async Task Stale_version_does_not_save()
    {
        var f = new Fixture(Agent);
        var e = await Assert.ThrowsAsync<WorkflowException>(() => f.Service.AssignAsync(f.Ticket.Id, new("agent", "stale")));
        Assert.Equal(409, e.Status); Assert.Equal(0, f.Work.Saves);
    }
    [Fact]
    public async Task Unauthorized_resource_is_indistinguishable_from_missing()
    {
        var f = new Fixture(Customer with { Id = "intruder" });
        var e = await Assert.ThrowsAsync<WorkflowException>(() => f.Service.ReplyAsync(f.Ticket.Id, new("Hello", false, Guid.NewGuid(), f.Version)));
        Assert.Equal(404, e.Status);
    }
    [Fact]
    public async Task Ineligible_assignment_is_rejected()
    {
        var f = new Fixture(Agent); f.Work.Eligible = false;
        await Assert.ThrowsAsync<WorkflowException>(() => f.Service.AssignAsync(f.Ticket.Id, new("other-agent", f.Version)));
        Assert.Null(f.Ticket.AssignedAgentId);
    }
    [Fact]
    public async Task Resolution_pause_preserves_history_and_extends_business_time()
    {
        var f = new Fixture(Agent);
        f.Ticket.ResolutionDue = new(2026, 10, 20, 11, 0, 0, TimeSpan.Zero);
        await f.Service.TransitionAsync(f.Ticket.Id, new(TicketStatus.WaitingForCustomer, f.Version));
        f.Clock.UtcNow = new(2026, 10, 19, 14, 0, 0, TimeSpan.Zero); // 2 business hours since 11:00
        await f.Service.TransitionAsync(f.Ticket.Id, new(TicketStatus.InProgress, f.Version));
        Assert.Equal(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero), f.Ticket.ResolutionDue);
        Assert.NotNull(Assert.Single(f.Ticket.Pauses).EndedAt);
    }
    [Fact]
    public async Task Customer_reply_resumes_waiting_ticket()
    {
        var f = new Fixture(Customer); f.Ticket.Status = TicketStatus.WaitingForCustomer; f.Ticket.PauseWhileWaiting = true;
        f.Ticket.Pauses.Add(new SlaPause { StartedAt = f.Clock.UtcNow.AddHours(-1) });
        await f.Service.ReplyAsync(f.Ticket.Id, new("Here are the details", false, Guid.NewGuid(), f.Version));
        Assert.Equal(TicketStatus.InProgress, f.Ticket.Status); Assert.NotNull(f.Ticket.Pauses[0].EndedAt); Assert.Null(f.Ticket.FirstResponseAt);
    }
    [Fact]
    public async Task Duplicate_reply_is_not_inserted_twice()
    {
        var f = new Fixture(Agent); var request = new ReplyTicket("We can help", false, Guid.NewGuid(), f.Version);
        await f.Service.ReplyAsync(f.Ticket.Id, request); await f.Service.ReplyAsync(f.Ticket.Id, request);
        Assert.Single(f.Ticket.Messages); Assert.Single(f.Work.Notifications);
    }
    [Fact]
    public async Task Priority_edit_preserves_historical_sla_snapshot()
    {
        var f = new Fixture(Agent); var due = f.Ticket.ResolutionDue;
        await f.Service.UpdateAsync(f.Ticket.Id, new("Updated subject", Guid.NewGuid(), TicketPriority.Urgent, f.Version));
        Assert.Equal(due, f.Ticket.ResolutionDue); Assert.Equal(TicketPriority.Urgent, f.Ticket.Priority);
    }
    [Fact]
    public async Task Reopening_starts_new_resolution_cycle()
    {
        var f = new Fixture(Customer); f.Ticket.Status = TicketStatus.Resolved; f.Ticket.ResolvedAt = f.Clock.UtcNow.AddHours(-1);
        await f.Service.TransitionAsync(f.Ticket.Id, new(TicketStatus.Open, f.Version));
        Assert.Equal(1, f.Ticket.ResolutionCycle); Assert.Null(f.Ticket.ResolvedAt); Assert.True(f.Ticket.ResolutionDue > f.Clock.UtcNow);
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public async Task Undefined_priority_is_rejected(int priority)
    {
        var f = new Fixture(Customer);
        await Assert.ThrowsAsync<WorkflowException>(() => f.Service.CreateAsync(new("Valid subject", "A valid description of the problem", Queue, Guid.NewGuid(), (TicketPriority)priority, Guid.NewGuid())));
    }

    private sealed class Fixture
    {
        public FakeWork Work { get; } = new();
        public FakeClock Clock { get; } = new();
        public Ticket Ticket => Work.Ticket;
        public string Version => Convert.ToBase64String(Ticket.RowVersion);
        public TicketService Service { get; }
        public Fixture(Actor actor) => Service = new(Work, new FakeUser(actor), Clock, new FakePublisher());
    }
    private sealed class FakeUser(Actor actor) : ICurrentUser { public Task<Actor> GetAsync(CancellationToken ct = default) => Task.FromResult(actor); }
    private sealed class FakeClock : IClock { public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 19, 11, 0, 0, TimeSpan.Zero); }
    private sealed class FakePublisher : INotificationPublisher { public Task PublishAsync(string recipient, CancellationToken ct = default) => Task.CompletedTask; }
    private sealed class FakeWork : ITicketUnitOfWork, ITicketUnitOfWorkFactory
    {
        public Ticket Ticket { get; } = new()
        {
            QueueId = Queue,
            CustomerId = "customer",
            Subject = "Original subject",
            RowVersion = [1, 2, 3],
            PauseWhileWaiting = true,
            ResolutionMinutes = 120,
            CalendarSnapshot = JsonSerializer.Serialize(new BusinessCalendar())
        };
        public List<Notification> Notifications { get; } = [];
        public int Saves { get; private set; }
        public bool Eligible { get; set; } = true;
        public Task<ITicketUnitOfWork> CreateAsync(CancellationToken ct) => Task.FromResult<ITicketUnitOfWork>(this);
        public Task<Ticket?> LoadAsync(Guid id, CancellationToken ct) => Task.FromResult<Ticket?>(Ticket);
        public Task<Ticket?> FindSubmissionAsync(string customerId, Guid submissionId, CancellationToken ct) => Task.FromResult<Ticket?>(null);
        public Task<SlaPolicy> PolicyAsync(TicketPriority priority, CancellationToken ct) => Task.FromResult(new SlaPolicy { CalendarJson = JsonSerializer.Serialize(new BusinessCalendar()), FirstResponseMinutes = 60, ResolutionMinutes = 120 });
        public Task<bool> ValidDestinationAsync(Guid queue, Guid category, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> EligibleAgentAsync(string agent, Guid queue, CancellationToken ct) => Task.FromResult(Eligible);
        public Task<IReadOnlyList<string>> QueueRecipientsAsync(Guid queue, CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>(["agent"]);
        public void Add(Ticket ticket, Guid submissionId) { }
        public void Notify(Notification notification) => Notifications.Add(notification);
        public void Feedback(CustomerFeedback feedback) { }
        public Task SaveAsync(CancellationToken ct) { Saves++; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

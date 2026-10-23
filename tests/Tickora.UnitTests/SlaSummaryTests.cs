using System.Text.Json;
using Tickora.Application;
using Tickora.Domain;

namespace Tickora.UnitTests;

public sealed class SlaSummaryTests
{
    private static DateTimeOffset At(int day, int hour) => new(2026, 10, day, hour, 0, 0, TimeSpan.Zero);
    private static Ticket Ticket() => new()
    {
        CalendarSnapshot = JsonSerializer.Serialize(new BusinessCalendar()),
        FirstResponseDue = At(19, 16),
        ResolutionDue = At(20, 11)
    };
    [Fact]
    public void Open_pause_keeps_resolution_remaining_business_time_constant()
    {
        var ticket = Ticket(); ticket.Pauses.Add(new SlaPause { StartedAt = At(19, 11) });
        var start = SlaSummary.For(ticket, At(19, 11)); var later = SlaSummary.For(ticket, At(19, 14));
        Assert.Equal(start.ResolutionRemaining, later.ResolutionRemaining);
        Assert.Equal(TimeSpan.FromHours(2), later.PausedBusinessTime);
        Assert.Equal(At(20, 14), later.EffectiveResolutionDue);
    }
    [Fact]
    public void Closed_pause_is_not_added_to_persisted_deadline_twice()
    {
        var ticket = Ticket(); ticket.ResolutionDue = At(20, 14);
        ticket.Pauses.Add(new SlaPause { StartedAt = At(19, 11), EndedAt = At(19, 14) });
        var summary = SlaSummary.For(ticket, At(19, 15));
        Assert.Equal(ticket.ResolutionDue, summary.EffectiveResolutionDue);
        Assert.Equal(TimeSpan.FromHours(2), summary.PausedBusinessTime); Assert.False(summary.Paused);
    }
    [Fact]
    public void Completed_targets_have_no_remaining_clock()
    {
        var ticket = Ticket(); ticket.FirstResponseAt = At(19, 10); ticket.ResolvedAt = At(19, 11);
        var summary = SlaSummary.For(ticket, At(20, 14));
        Assert.Null(summary.ResponseRemaining); Assert.Null(summary.ResolutionRemaining);
    }
}

using System.Text.Json;
using Tickora.Domain;

namespace Tickora.Application;

public sealed record SlaSummary(TimeSpan? ResponseRemaining, TimeSpan? ResolutionRemaining, TimeSpan PausedBusinessTime,
    DateTimeOffset EffectiveResolutionDue, bool Paused)
{
    public static SlaSummary For(Ticket ticket, DateTimeOffset now)
    {
        var calendar = JsonSerializer.Deserialize<BusinessCalendar>(ticket.CalendarSnapshot)!;
        var pausedTime = ticket.Pauses.Aggregate(TimeSpan.Zero, (sum, pause) => sum + SlaCalculationPolicy.BusinessTimeBetween(pause.StartedAt, pause.EndedAt ?? now, calendar));
        var activePause = ticket.Pauses.SingleOrDefault(p => p.EndedAt == null);
        var due = activePause is null ? ticket.ResolutionDue : SlaCalculationPolicy.AddBusinessTime(ticket.ResolutionDue,
            SlaCalculationPolicy.BusinessTimeBetween(activePause.StartedAt, now, calendar), calendar);
        TimeSpan Remaining(DateTimeOffset target) => target >= now
            ? SlaCalculationPolicy.BusinessTimeBetween(now, target, calendar)
            : -SlaCalculationPolicy.BusinessTimeBetween(target, now, calendar);
        return new(ticket.FirstResponseAt.HasValue ? null : Remaining(ticket.FirstResponseDue),
            ticket.ResolvedAt.HasValue ? null : Remaining(due), pausedTime, due, activePause is not null);
    }
}

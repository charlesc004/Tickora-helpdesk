namespace Tickora.Domain;

public sealed record BusinessPeriod(TimeOnly Start, TimeOnly End);
public sealed record BusinessDateSchedule
{
    public DateOnly Date { get; init; }
    public bool Closed { get; init; }
    public TimeOnly Opens { get; init; } = new(9, 0);
    public TimeOnly Closes { get; init; } = new(17, 0);
    public BusinessPeriod[] Breaks { get; init; } = [];
}

public sealed record BusinessCalendar
{
    public string TimeZone { get; init; } = "Etc/UTC";
    public DayOfWeek[] WorkingDays { get; init; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public TimeOnly Opens { get; init; } = new(9, 0);
    public TimeOnly Closes { get; init; } = new(17, 0);
    public TimeOnly BreakStarts { get; init; } = new(12, 0);
    public TimeOnly BreakEnds { get; init; } = new(13, 0);
    public DateOnly[] Holidays { get; init; } = [];
    // Null preserves calendars snapshotted with the original single-break fields.
    public BusinessPeriod[]? Breaks { get; init; }
    public BusinessDateSchedule[] DateOverrides { get; init; } = [];

    public void Validate()
    {
        if (!TimeZoneInfo.FindSystemTimeZoneById(TimeZone).HasIanaId) throw new ArgumentException("Use an IANA time zone identifier.");
        if (WorkingDays is null || Holidays is null || DateOverrides is null || DateOverrides.Any(d => d is null) || WorkingDays.Length == 0 || WorkingDays.Any(d => !Enum.IsDefined(d)) ||
            DateOverrides.Select(d => d.Date).Distinct().Count() != DateOverrides.Length)
            throw new ArgumentException("Calendar must have working days and positive, same-day business intervals.");
        ValidateSchedule(Opens, Closes, NormalBreaks);
        foreach (var date in DateOverrides.Where(d => !d.Closed)) ValidateSchedule(date.Opens, date.Closes, date.Breaks);
    }

    private BusinessPeriod[] NormalBreaks => Breaks ?? [new(BreakStarts, BreakEnds)];
    private static void ValidateSchedule(TimeOnly opens, TimeOnly closes, BusinessPeriod[] breaks)
    {
        if (opens >= closes || breaks is null || breaks.Any(b => b is null)) throw new ArgumentException("Business hours must be positive, same-day intervals.");
        var cursor = opens;
        var excluded = TimeSpan.Zero;
        foreach (var pause in breaks.OrderBy(b => b.Start))
        {
            if (pause.Start < cursor || pause.End < pause.Start || pause.End > closes) throw new ArgumentException("Breaks must be inside business hours and must not overlap.");
            cursor = pause.End; excluded += pause.End - pause.Start;
        }
        if (excluded >= closes - opens) throw new ArgumentException("A schedule must contain working time.");
    }

    internal IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Intervals(DateOnly day)
    {
        var special = DateOverrides.SingleOrDefault(d => d.Date == day);
        if (special?.Closed == true || special is null && (!WorkingDays.Contains(day.DayOfWeek) || Holidays.Contains(day))) yield break;
        var opens = special?.Opens ?? Opens;
        var closes = special?.Closes ?? Closes;
        var breaks = special?.Breaks ?? NormalBreaks;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
        var boundaries = new SortedSet<DateTimeOffset>();
        foreach (var boundary in new[] { opens, closes }.Concat(breaks.SelectMany(b => new[] { b.Start, b.End })))
        {
            boundaries.Add(ToUtc(day.ToDateTime(boundary), zone, false));
            boundaries.Add(ToUtc(day.ToDateTime(boundary), zone, true));
        }
        // Split at actual UTC offset changes as well as all wall-clock boundaries. This avoids
        // double-counting repeated hours and excludes both occurrences of a repeated break.
        var first = boundaries.Min;
        var last = boundaries.Max;
        for (var left = first; left < last;)
        {
            var right = left.AddHours(1) < last ? left.AddHours(1) : last;
            if (zone.GetUtcOffset(left) != zone.GetUtcOffset(right))
            {
                var lo = left.UtcTicks; var hi = right.UtcTicks;
                var offset = zone.GetUtcOffset(left);
                while (hi - lo > 1)
                {
                    var mid = lo + (hi - lo) / 2;
                    if (zone.GetUtcOffset(new DateTimeOffset(mid, TimeSpan.Zero)) == offset) lo = mid; else hi = mid;
                }
                boundaries.Add(new DateTimeOffset(hi, TimeSpan.Zero));
            }
            left = right;
        }
        var points = boundaries.ToArray();
        for (var i = 0; i < points.Length - 1; i++)
        {
            var a = points[i]; var b = points[i + 1];
            var local = TimeZoneInfo.ConvertTime(a + (b - a) / 2, zone);
            var time = TimeOnly.FromDateTime(local.DateTime);
            if (DateOnly.FromDateTime(local.DateTime) == day && time >= opens && time < closes && !breaks.Any(p => time >= p.Start && time < p.End))
                yield return (a, b);
        }
    }

    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo zone, bool end)
    {
        // A skipped wall-clock boundary moves forward to the first valid minute.
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        if (zone.IsAmbiguousTime(local))
        {
            var offsets = zone.GetAmbiguousTimeOffsets(local);
            return new DateTimeOffset(local, end ? offsets.Min() : offsets.Max()).ToUniversalTime();
        }
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }
}

public static class SlaCalculationPolicy
{
    public static DateTimeOffset AddBusinessTime(DateTimeOffset start, TimeSpan duration, BusinessCalendar calendar)
    {
        calendar.Validate();
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (duration == TimeSpan.Zero) return start;
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, TimeZoneInfo.FindSystemTimeZoneById(calendar.TimeZone)).DateTime);
        for (var i = 0; i < 36600; i++, day = day.AddDays(1))
        {
            foreach (var (a, b) in calendar.Intervals(day))
            {
                var cursor = a > start ? a : start;
                if (cursor >= b) continue;
                if (duration <= b - cursor) return cursor + duration;
                duration -= b - cursor;
            }
        }
        throw new ArgumentException("SLA target exceeds the calendar's supported 100-year horizon.");
    }

    public static TimeSpan BusinessTimeBetween(DateTimeOffset start, DateTimeOffset end, BusinessCalendar calendar)
    {
        calendar.Validate();
        if (end <= start) return TimeSpan.Zero;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(calendar.TimeZone);
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, zone).DateTime);
        var last = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(end, zone).DateTime);
        if (last.DayNumber - day.DayNumber > 36600) throw new ArgumentOutOfRangeException(nameof(end));
        var result = TimeSpan.Zero;
        for (; day <= last; day = day.AddDays(1))
            foreach (var (a, b) in calendar.Intervals(day))
            {
                var left = a > start ? a : start;
                var right = b < end ? b : end;
                if (right > left) result += right - left;
            }
        return result;
    }
}

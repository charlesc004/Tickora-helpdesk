using Tickora.Domain;

namespace Tickora.UnitTests;

public sealed class CalendarTests
{
    [Fact]
    public void Multiple_breaks_are_excluded()
    {
        var calendar = new BusinessCalendar { Breaks = [new(new(10, 0), new(10, 15)), new(new(12, 0), new(13, 0))] };
        Assert.Equal(Utc(19, 13, 15), SlaCalculationPolicy.AddBusinessTime(Utc(19, 9), TimeSpan.FromHours(3), calendar));
    }
    [Fact]
    public void Date_override_can_open_a_weekend_and_close_a_workday()
    {
        var calendar = new BusinessCalendar { DateOverrides = [new() { Date = new(2026, 10, 23), Closed = true }, new() { Date = new(2026, 10, 24), Opens = new(10, 0), Closes = new(14, 0) }] };
        Assert.Equal(Utc(24, 11), SlaCalculationPolicy.AddBusinessTime(Utc(23, 9), TimeSpan.FromHours(1), calendar));
    }
    [Fact]
    public void Repeated_dst_break_is_excluded_in_both_occurrences()
    {
        var calendar = new BusinessCalendar { TimeZone = "America/New_York", WorkingDays = [DayOfWeek.Sunday], Opens = new(1, 0), Closes = new(4, 0), Breaks = [new(new(1, 15), new(1, 45))] };
        var start = new DateTimeOffset(2026, 11, 1, 5, 0, 0, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromHours(3), SlaCalculationPolicy.BusinessTimeBetween(start, start.AddHours(4), calendar));
    }
    [Fact]
    public void Repeated_opening_does_not_include_the_closed_repeated_minutes()
    {
        var calendar = new BusinessCalendar { TimeZone = "America/New_York", WorkingDays = [DayOfWeek.Sunday], Opens = new(1, 30), Closes = new(4, 0), Breaks = [] };
        var start = new DateTimeOffset(2026, 11, 1, 5, 0, 0, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromHours(3), SlaCalculationPolicy.BusinessTimeBetween(start, start.AddHours(4), calendar));
    }
    [Fact] public void Overlapping_breaks_are_rejected() => Assert.Throws<ArgumentException>(() => new BusinessCalendar { Breaks = [new(new(10, 0), new(11, 0)), new(new(10, 30), new(12, 0))] }.Validate());
    private static DateTimeOffset Utc(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, TimeSpan.Zero);
    [Fact] public void Lunch_is_excluded() => Assert.Equal(Utc(19, 14), SlaCalculationPolicy.AddBusinessTime(Utc(19, 11), TimeSpan.FromHours(2), new()));
    [Fact] public void Weekend_is_excluded() => Assert.Equal(Utc(26, 10), SlaCalculationPolicy.AddBusinessTime(Utc(23, 16), TimeSpan.FromHours(2), new()));
    [Fact]
    public void Holiday_is_excluded()
    {
        var cal = new BusinessCalendar { Holidays = [new(2026, 10, 26)] };
        Assert.Equal(Utc(27, 10), SlaCalculationPolicy.AddBusinessTime(Utc(23, 16), TimeSpan.FromHours(2), cal));
    }
    [Fact] public void Before_opening_starts_at_opening() => Assert.Equal(Utc(19, 10), SlaCalculationPolicy.AddBusinessTime(Utc(19, 4), TimeSpan.FromHours(1), new()));
    [Fact] public void Exactly_at_closing_finishes_that_day() => Assert.Equal(Utc(19, 17), SlaCalculationPolicy.AddBusinessTime(Utc(19, 16), TimeSpan.FromHours(1), new()));
    [Fact] public void Start_at_closing_moves_to_next_day() => Assert.Equal(Utc(20, 10), SlaCalculationPolicy.AddBusinessTime(Utc(19, 17), TimeSpan.FromHours(1), new()));
    [Fact] public void Break_start_skips_break() => Assert.Equal(Utc(19, 13, 30), SlaCalculationPolicy.AddBusinessTime(Utc(19, 12), TimeSpan.FromMinutes(30), new()));
    [Fact]
    public void Business_duration_is_inverse_of_addition()
    {
        var cal = new BusinessCalendar { Holidays = [new(2026, 10, 20)] };
        var end = SlaCalculationPolicy.AddBusinessTime(Utc(19, 11), TimeSpan.FromHours(19), cal);
        Assert.Equal(TimeSpan.FromHours(19), SlaCalculationPolicy.BusinessTimeBetween(Utc(19, 11), end, cal));
    }
    [Fact]
    public void Half_hour_offset_is_applied()
    {
        var cal = new BusinessCalendar { TimeZone = "Asia/Kolkata" };
        Assert.Equal(Utc(19, 4, 30), SlaCalculationPolicy.AddBusinessTime(Utc(19, 2), TimeSpan.FromHours(1), cal));
    }
    [Fact]
    public void Dst_weekend_uses_new_offset_on_Monday()
    {
        var cal = new BusinessCalendar { TimeZone = "America/New_York" };
        var friday = new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 15, 0, 0, TimeSpan.Zero), SlaCalculationPolicy.AddBusinessTime(friday, TimeSpan.FromHours(2), cal));
    }
    [Fact]
    public void Ambiguous_boundary_covers_both_repeated_hours()
    {
        var cal = new BusinessCalendar { TimeZone = "America/New_York", WorkingDays = [DayOfWeek.Sunday], Opens = new(1, 0), Closes = new(4, 0), BreakStarts = new(4, 0), BreakEnds = new(4, 0) };
        var start = new DateTimeOffset(2026, 11, 1, 5, 0, 0, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromHours(4), SlaCalculationPolicy.BusinessTimeBetween(start, start.AddHours(4), cal));
    }
    [Fact]
    public void Skipped_boundary_advances_to_first_valid_minute()
    {
        var cal = new BusinessCalendar { TimeZone = "America/New_York", WorkingDays = [DayOfWeek.Sunday], Opens = new(2, 30), Closes = new(4, 0), BreakStarts = new(4, 0), BreakEnds = new(4, 0) };
        var start = new DateTimeOffset(2026, 3, 8, 6, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2026, 3, 8, 7, 30, 0, TimeSpan.Zero), SlaCalculationPolicy.AddBusinessTime(start, TimeSpan.FromMinutes(30), cal));
    }
    [Fact] public void Invalid_calendar_is_rejected() => Assert.Throws<ArgumentException>(() => new BusinessCalendar { WorkingDays = [] }.Validate());
    [Fact] public void Negative_target_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => SlaCalculationPolicy.AddBusinessTime(Utc(19, 11), TimeSpan.FromMinutes(-1), new()));
    [Fact] public void No_break_is_supported() => Assert.Equal(Utc(19, 13), SlaCalculationPolicy.AddBusinessTime(Utc(19, 11), TimeSpan.FromHours(2), new() { BreakStarts = new(12, 0), BreakEnds = new(12, 0) }));
}

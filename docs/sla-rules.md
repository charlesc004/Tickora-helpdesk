# SLA behavior

## Targets

Default response/resolution targets are Low 1440/4320, Medium 480/1920, High 120/480, Urgent 30/240 **business minutes**. Administrators can edit them and the calendar in `/admin/sla`. Every new ticket captures its target minutes, pause behavior and calendar JSON. Editing a policy does not rewrite existing ticket history. Priority edits change triage severity while preserving the original SLA snapshot; they do not silently reset a deadline.

The default fictional business calendar uses IANA `Etc/UTC`, Monday–Friday, 09:00–17:00, with a 12:00–13:00 break. One business day is therefore seven business hours. TimeOnly values serialize as `09:00:00`; weekday numbers are Sunday=0 through Saturday=6; holidays serialize as ISO dates. Optional `Breaks` supports multiple non-overlapping breaks. `DateOverrides` supports special opening hours, break lists and closures; explicit date schedules take precedence over regular weekdays and holidays. Existing single-break JSON remains supported.

## Calendar arithmetic

SlaCalculationPolicy is authoritative for both adding business time and measuring elapsed business time. Instants persist as UTC DateTimeOffset. Calendar boundaries are converted through TimeZoneInfo using the configured IANA ID. Opening/break/closing boundaries are same-day intervals; overnight schedules are rejected. Holidays and non-working weekdays produce no interval. Arithmetic traverses working intervals instead of adding wall-clock hours.

Intervals are split at wall-clock schedule boundaries and UTC offset transitions. Membership is evaluated in local time for each resulting interval. Repeated open minutes count twice as actual elapsed business time, while both occurrences of a repeated break or closed minute are excluded. Spring-forward gaps contain no elapsed minutes. Targets and traversal have a 100-year guard. Invalid schedules, duplicate date overrides and overlapping/out-of-hours breaks are rejected.

## First response

Only the first customer-visible reply from a staff member through the conversation workflow sets FirstResponseAt. Internal notes and system events do not count. Later replies preserve the first timestamp. Its deadline never pauses. Moving to Waiting for Customer before providing a qualifying response does not stop the response SLA.

## Resolution and pause

If enabled in the ticket's snapshot, entering Waiting for Customer opens an SlaPause interval. Leaving that state closes the interval and extends ResolutionDue by the business time spent paused, using the same calendar snapshot. Pause history remains stored. Customer replies automatically leave Waiting for Customer. Open pauses suppress resolution warnings/breaches until resumed; first-response monitoring continues.

Resolution records ResolvedAt, closing records ClosedAt. Explicit transitions are enforced centrally. Customers can close a resolved conversation and reopen a resolved/closed conversation. Staff can begin work, wait, resolve, close, and reopen using the same transition policy. Arbitrary status assignment is rejected.

Reopening begins a new resolution cycle using the ticket's original snapshotted resolution target from the reopening instant. First-response history stays unchanged. The most recent ResolvedAt is cleared, but prior resolution/reopening events stay in append-only ticket activity. Aggregate reports describe the current/latest resolution cycle, not a complete multi-cycle performance warehouse.

## Monitoring

The hosted worker runs once on startup and then every sixty seconds, using a new scope/DbContext per scan. It takes transaction-owned SQL `sp_getapplock('Tickora:SlaMonitor')` with zero wait time so concurrent replicas cannot own a scan together. A busy/error scan is logged and retried next interval. Set `Sla__MonitorEnabled=false` to disable it for tests or when choosing a dedicated worker owner.

The worker finds active tickets whose response or unpaused resolution deadline is within thirty elapsed minutes or has passed. Assigned tickets route to their active eligible agent; unassigned tickets route to active queue staff and administrators. Stable notification keys include ticket, target kind, resolution cycle and alert kind, unique per recipient. A separate unique ticket/event key prevents duplicate audit activity across recipients and reassignment. Alerts and activity commit in the same transaction; repeated scans remain idempotent.

The warning horizon is elapsed UTC time. SQL queries use indexed due fields and persisted pause state, traversing candidates in batches of 250 by unique ticket number. `Sla__ScanSeconds` (default 60) and `Sla__WarningMinutes` (default 30) configure the bounded worker intervals.

## Reports and validation

SLA compliance requires both a qualifying first response by its deadline and the latest resolution by its deadline. Average first-response and resolution durations displayed in reports are elapsed hours and explicitly labeled as such; they are not averages of business minutes. Paused tickets are excluded from active resolution-overdue counts.

Executed unit tests cover multiple breaks, date overrides, weekends, holidays, boundaries, half-hour time-zone offsets, DST gaps/overlaps, validation, first-response qualification, pause/resume and reopen cycles. Real SQL Server tests verify persisted alerts, deduplication and unassigned escalation.

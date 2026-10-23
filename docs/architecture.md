# Architecture and implementation decisions

The solution uses exactly four production projects. Domain models ticket state and business-calendar arithmetic without framework references. Application defines authorized use cases and focused persistence/query contracts. Infrastructure owns SQL Server, EF Core, Identity account integration, local private files, seeding and the hosted SLA scan. Web composes dependencies, renders Blazor components, and maps HTTP/SignalR boundaries.

## Request path

Both Interactive Server pages and REST endpoints call TicketService. CurrentUser reads the authenticated server identity, then reloads active status, security stamp, roles and queue memberships from SQL Server. UI visibility is convenience; it is not the permission boundary. Ticket reads start from a SQL-filtered authorized ticket query. Unauthorized ticket IDs return 404. Private message/activity/attachment predicates execute in Infrastructure, before mapping the result.

An ITicketUnitOfWork loads one tracked ticket aggregate and disposes its factory-created DbContext at operation end. The workflow checks access and the supplied base64 rowversion. Domain policies govern transitions and business-time arithmetic. One SaveChanges persists ticket, messages, activities, pauses and notifications transactionally. EF concurrency exceptions map to 409. Generated message IDs are configured ValueGeneratedNever so newly appended children are tracked as inserts. No circuit holds an EF context and no generic repository mirrors DbSet.

Ticket list queries filter, count, sort, project and page on the server. Ticket details load the conversation only when needed. Report aggregations run in SQL; chart components render their results without a chart library. CSV generation streams pages into the export result rather than loading full aggregates, although the final CSV is buffered in memory. Exports are not designed for millions of tickets.

## Schema

Identity tables hold users, roles, claims, tokens and credentials; ApplicationUser adds display name and active status. Ticket references customer, optional assigned agent, queue and category. Children hold messages, attachment metadata, audit events and SLA pause intervals. A unique customer/submission key prevents duplicate ticket creation. A unique ticket/author/submission key prevents duplicate reply insertion. Immutable authorship and visibility are set in the workflow.

QueueMembership has a composite queue/agent key. SlaPolicy is keyed by priority and stores business-minute targets and a serialized calendar. A ticket stores target minutes and the calendar snapshot used at creation. Notification has a unique recipient/event key and a recipient/created index. KnowledgeArticle has a unique slug and safely rendered plain text. CustomerFeedback has one row per ticket and a SQL 1–5 rating constraint. CannedResponse stores staff templates. Business calendars and holidays are contained in versioned ticket/policy JSON snapshots rather than separate editable calendar tables; this is a deliberate compact implementation and a difference from the brief's suggested entity list.

Foreign keys use restrictive deletion for ticket-related data. Queues and categories are deactivated rather than deleting ticket history. Tickets, queues, articles, SLA policies, categories and saved responses use SQL rowversion concurrency tokens. Saved responses can be archived and restored without removing history.

## Components

Shared shell: DashboardLayout for customers, agents and administrators with role-aware links; PublicLayout for account forms. A distinct customer shell was intentionally avoided while behavior remains shared.

Reusable primitives: Icon, AppButton, PageHeader, MetricCard, StatusBadge, PriorityBadge, AppPagination, BreakdownPanel, TrendPanel, EmptyState, ErrorState, LoadingSkeleton. Feature components: TicketTable, TicketProperties, SlaIndicator, TicketTimeline, MessageThread, MessageComposer, AttachmentList and NotificationBell. Framework InputText/InputSelect and HTML controls are reused instead of wrapping every input. There is no duplicate dialog or button framework.

## Identity and sessions

Account forms are statically rendered. The HTTP POST group is `/account/actions`, avoiding overlap with Blazor page endpoints. Identity owns hashing, lockout, cookie issuance, tokens and password changes. Public registration can create Customer only. Cookie auth is HTTP-only, same-site Lax, secure-only outside Development, and limited to eight hours. Login/reset/registration requests have a per-IP fixed-window limiter. Hosting behind a proxy needs explicit trusted forwarding configuration before the limiter is suitable for real client IP accounting.

Workflow calls recheck active status and security stamp. Blazor auth state revalidates once per minute. Customer data is never authorized using a caller-supplied role or queue. Role changes invalidate the security stamp and are serialized using a SQL application lock. The current administrator cannot remove their own administrator role. No team member can self-promote through public endpoints.

Data-protection keys persist outside wwwroot in App_Data/keys. Deployment must encrypt/protect that directory and share keys appropriately; the application does not ship a production key-encryption certificate or managed key integration.

## Notifications

SQL notification rows are the source of truth. The workflow publishes only after commit. An in-process NotificationPulse updates subscribed Interactive Server badge components without opening another hub connection; a thirty-second refresh recovers missed/cross-replica hints. Dedicated independent clients can connect to an authorized NotificationsHub and receive `notificationsChanged`. There are no caller-selected private groups. The hint payload contains no ticket content. Notification pages explicitly refresh their list and can mark owned rows as read.

Hints are best-effort, not a durable transactional outbox: a process crash after commit can lose the immediate hint, while persisted rows remain queryable. The dedicated hub has no custom client-side reconnect implementation in this repository; external clients must reconnect and reload notification rows. Notifications may remain in a staff inbox after a queue membership is removed; opening the linked resource is always checked against current permissions, and note text is never embedded in the notification.

## Files

Authorized uploads are bounded to 5 MB and allow PNG/JPEG/PDF signatures or strict UTF-8 text. Extension/MIME claims alone are insufficient. Files use generated keys under private App_Data storage. A failed metadata operation removes the newly written file. Downloads read authorized filtered metadata first, then the private stream, and force attachment delivery with nosniff. Production malware scanning, quota enforcement, retention and orphan reconciliation remain deployment/feature work.

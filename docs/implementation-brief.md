# TICKORA — Enterprise Helpdesk & Support Management Platform

## MASTER IMPLEMENTATION PROMPT FOR CODEX

### YOUR ROLE

Act as a Principal .NET Software Architect, Senior C# Developer, ASP.NET Core Engineer, Blazor UI Architect, SQL Server Database Engineer, and Senior Enterprise SaaS UI/UX Designer.

Your task is to architect, build, test, optimize, and document a complete enterprise-style customer support and ticket management platform named **TICKORA**.

The application must demonstrate professional capabilities in:

- C# and modern .NET.
- ASP.NET Core.
- Blazor Web Apps.
- Entity Framework Core.
- Microsoft SQL Server.
- ASP.NET Core Identity.
- RESTful API design.
- SignalR real-time communication.
- Background processing with .NET Hosted Services.
- Role-based and resource-based authorization.
- Database concurrency management.
- Complex business workflows.
- SLA calculation and monitoring.
- Reusable Razor components.
- Clean Architecture principles.
- Modular application development.
- Automated testing.
- Docker and CI/CD.

**The highest priorities are complete functionality, clean architecture, reusable code, maintainability, security, excellent visual design, and reliable business logic.**

This must be a real, fully functioning application, not a static dashboard, UI prototype, or collection of disconnected demonstration screens.

Do not generate unnecessary boilerplate, excessive abstractions, duplicate components, or hundreds of meaningless files.

Build a professional application that another experienced .NET engineer can understand, extend, test, and maintain.

---

# 1. PROJECT IDENTITY

**Project Name:** TICKORA

**Brand Name:** TICKORA

**Product Category:** Enterprise Customer Support & Helpdesk

**Application Type:** Full-Stack Enterprise Web Application

**Default Interface Language:** English

**Main Programming Language:** C#

**Framework:** .NET 10 LTS

**Frontend:** Blazor Web App

**Database:** Microsoft SQL Server

**Architecture:** Clean Modular Monolith

**Brand Tagline:** Smarter Support. Better Experiences.

## Product Vision

TICKORA is a professional customer support management platform designed for organizations that need a centralized solution for managing support tickets, customer conversations, team assignments, service-level agreements, and knowledge-base content.

The platform should make support operations more organized, measurable, and efficient.

It includes three user experiences:

1. Customer Support Portal.
2. Support Agent Workspace.
3. Administrator Dashboard.

All three experiences must belong to the same application and reuse the same foundational architecture.

The application must look like a premium, modern enterprise SaaS product rather than a generic administrative template.

Use realistic, fictional demonstration data.

Do not present invented customers, organizations, statistics, or testimonials as genuine business achievements.

---

# 2. CORE ARCHITECTURE PRINCIPLES

The entire application must follow these engineering principles.

## 2.1 Reusability First

Before creating a component, helper, service, validator, or business operation, check whether equivalent functionality already exists.

Reuse existing functionality wherever appropriate.

Examples:

- One reusable button system.
- One reusable dialog system.
- One reusable data table foundation.
- One reusable pagination component.
- One reusable search input.
- One ticket status component.
- One ticket priority component.
- One centralized validation strategy.
- One centralized authorization policy system.
- One centralized ticket transition engine.
- One centralized SLA calculation service.
- One shared notification delivery mechanism.
- One consistent error-handling strategy.

Do not create duplicate implementations for customer, agent, and administrator interfaces when shared components can serve the same purpose.

## 2.2 Component-Based Development

Every substantial interface must be divided into cohesive Razor components.

Example:

```text
TicketDetailPage
├── TicketHeader
├── TicketStatusBadge
├── TicketPriorityBadge
├── TicketInformation
├── TicketConversation
│   ├── MessageThread
│   ├── MessageBubble
│   ├── AttachmentList
│   └── MessageComposer
├── TicketAssignment
├── SlaIndicator
└── TicketActivityTimeline
```

Components must have clear responsibilities.

Do not create giant Razor components containing rendering, database queries, business rules, and complex interaction logic.

Use code-behind files when component complexity genuinely justifies them.

Do not automatically create `.razor.cs` files for trivial presentation components.

## 2.3 DRY Principles

Avoid duplication of:

- Business rules.
- Query logic.
- Ticket transitions.
- SLA formulas.
- Permission checks.
- Validation.
- Error handling.
- Date formatting.
- UI patterns.
- Database mapping.
- Notification logic.

Do not force unrelated behavior into a generic abstraction just to reduce line count.

## 2.4 Separation of Concerns

Use four focused .NET projects:

- Domain.
- Application.
- Infrastructure.
- Web.

Each project must have a clearly defined responsibility.

Follow this dependency direction:

```text
Tickora.Web
   ├── Tickora.Application
   └── Tickora.Infrastructure
              ├── Tickora.Application
              └── Tickora.Domain

Tickora.Application
   └── Tickora.Domain

Tickora.Domain
   └── No application-specific dependencies
```

Domain must not depend on ASP.NET Core, EF Core, Blazor, or Infrastructure.

Application should define use cases and focused interfaces for external dependencies.

Infrastructure implements persistence, authentication integration, storage, and notification adapters.

Web acts as the composition root and contains user interfaces and HTTP endpoints.

Do not create unnecessary additional projects.

## 2.5 Avoid Overengineering

Do not introduce:

- Microservices.
- Kubernetes.
- Event sourcing.
- CQRS frameworks without a demonstrated need.
- Generic repositories that merely duplicate EF Core.
- MediatR for trivial method forwarding.
- Excessive DTO mapping layers.
- Unnecessary service interfaces.
- Complex reflection-based architectures.
- Multiple competing validation systems.
- Unused design patterns.
- Redundant SignalR connections.
- Redis without a specific requirement.
- Large frontend JavaScript frameworks.
- Empty directories and placeholder files.

Use straightforward .NET architecture and built-in framework capabilities where possible.

## 2.6 Code Quality

Require:

- Nullable reference types enabled.
- Consistent naming conventions.
- Appropriate C# records and classes.
- Async/await for I/O operations.
- CancellationToken propagation.
- Dependency injection.
- Strong typing.
- Small cohesive methods.
- Clear exception handling.
- Explicit business invariants.
- No hardcoded secrets.
- No dead code.
- No duplicated business logic.
- No unnecessary static mutable state.
- No fake functionality.

Optimize for engineering quality, not code volume.

---

# 3. REQUIRED TECHNOLOGY STACK

## Core

- C#.
- .NET 10 LTS.
- ASP.NET Core.
- Entity Framework Core.
- ASP.NET Core Identity.
- ASP.NET Core Authorization Policies.
- Built-in dependency injection.
- Microsoft Extensions Logging.
- Options Pattern.
- ProblemDetails for HTTP API errors.

## Frontend

- Blazor Web App.
- Interactive Server render mode where appropriate.
- Static server-side rendering for suitable public content.
- Razor Components.
- Tailwind CSS or a focused CSS design system.
- Minimal JavaScript interop.
- One consistent icon library.

## Database

- Microsoft SQL Server.
- Entity Framework Core SQL Server provider.
- EF Core migrations.
- LINQ.
- SQL transactions.
- Rowversion concurrency tokens.
- Database constraints and indexes.

## Real-Time

- ASP.NET Core SignalR.
- Persistent notification storage.
- Authorized event delivery.
- Reconnection handling.

## Background Processing

- BackgroundService.
- IHostedService.
- IServiceScopeFactory.
- PeriodicTimer.
- CancellationToken.

## Testing

- xUnit.
- ASP.NET Core integration testing.
- SQL Server-backed integration tests.
- Testcontainers where supported.
- Playwright.

## Infrastructure

- Docker Compose.
- Multi-stage Docker builds.
- Environment-based configuration.
- GitHub Actions.
- Health checks.
- Structured logging.

Use mutually compatible stable versions.

Do not install unnecessary NuGet packages.

---

# 4. COMPLETE PROJECT NAMING CONVENTIONS

Use **TICKORA** consistently across the entire solution.

```text
Root Directory:
tickora/

Solution:
Tickora.slnx

Projects:
Tickora.Domain
Tickora.Application
Tickora.Infrastructure
Tickora.Web

Tests:
Tickora.UnitTests
Tickora.IntegrationTests
Tickora.E2ETests

Namespaces:
Tickora.Domain
Tickora.Application
Tickora.Infrastructure
Tickora.Web

Database:
TickoraDb

Docker Application Service:
tickora-web

Docker Database Service:
tickora-sqlserver

Application Display Name:
TICKORA

Default API Prefix:
/api/v1

GitHub Repository:
tickora
```

Do not retain previous project names.

If this is an existing repository, inspect all previous names in:

- Assembly names.
- Namespaces.
- Project references.
- Solution files.
- Docker configuration.
- Connection strings.
- Database configuration.
- Application titles.
- Documentation.
- Tests.
- Environment variables.
- Seed scripts.

Rename safely without deleting user data or unrelated files.

---

# 5. OPTIMIZED SOLUTION FILE ARCHITECTURE

Use the following structure as the architectural blueprint.

```text
tickora/
│
├── src/
│   │
│   ├── Tickora.Domain/
│   │   │
│   │   ├── Entities/
│   │   │   ├── Ticket.cs
│   │   │   ├── TicketMessage.cs
│   │   │   ├── TicketAttachment.cs
│   │   │   ├── TicketActivity.cs
│   │   │   ├── TicketCategory.cs
│   │   │   ├── SupportQueue.cs
│   │   │   ├── QueueMembership.cs
│   │   │   ├── SlaPolicy.cs
│   │   │   ├── BusinessCalendar.cs
│   │   │   ├── KnowledgeArticle.cs
│   │   │   ├── Notification.cs
│   │   │   └── CustomerFeedback.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── TicketStatus.cs
│   │   │   ├── TicketPriority.cs
│   │   │   ├── MessageVisibility.cs
│   │   │   └── NotificationType.cs
│   │   │
│   │   ├── Policies/
│   │   │   ├── TicketTransitionPolicy.cs
│   │   │   └── SlaCalculationPolicy.cs
│   │   │
│   │   └── Common/
│   │
│   ├── Tickora.Application/
│   │   │
│   │   ├── Abstractions/
│   │   │   ├── IClock.cs
│   │   │   ├── ICurrentUser.cs
│   │   │   ├── IFileStorage.cs
│   │   │   ├── INotificationPublisher.cs
│   │   │   └── Persistence/
│   │   │
│   │   ├── Features/
│   │   │   │
│   │   │   ├── Tickets/
│   │   │   │   ├── TicketService.cs
│   │   │   │   ├── TicketQueries.cs
│   │   │   │   ├── TicketDtos.cs
│   │   │   │   └── TicketValidation.cs
│   │   │   │
│   │   │   ├── Conversations/
│   │   │   │   ├── ConversationService.cs
│   │   │   │   └── MessageDtos.cs
│   │   │   │
│   │   │   ├── Assignments/
│   │   │   │   └── AssignmentService.cs
│   │   │   │
│   │   │   ├── Sla/
│   │   │   │   ├── SlaService.cs
│   │   │   │   └── SlaDtos.cs
│   │   │   │
│   │   │   ├── KnowledgeBase/
│   │   │   │   ├── ArticleService.cs
│   │   │   │   └── ArticleDtos.cs
│   │   │   │
│   │   │   ├── Notifications/
│   │   │   │   └── NotificationService.cs
│   │   │   │
│   │   │   ├── Reports/
│   │   │   │   ├── ReportService.cs
│   │   │   │   └── ReportDtos.cs
│   │   │   │
│   │   │   └── Administration/
│   │   │       └── AdministrationService.cs
│   │   │
│   │   └── Common/
│   │       ├── Pagination/
│   │       ├── Errors/
│   │       └── Results/
│   │
│   ├── Tickora.Infrastructure/
│   │   │
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Repositories/
│   │   │   ├── Migrations/
│   │   │   └── Seed/
│   │   │
│   │   ├── Identity/
│   │   │   └── ApplicationUser.cs
│   │   │
│   │   ├── Storage/
│   │   │   └── LocalFileStorage.cs
│   │   │
│   │   ├── Realtime/
│   │   │   └── SignalRNotificationPublisher.cs
│   │   │
│   │   ├── Notifications/
│   │   │   └── NotificationDeliveryService.cs
│   │   │
│   │   ├── BackgroundJobs/
│   │   │   └── SlaMonitoringService.cs
│   │   │
│   │   └── DependencyInjection.cs
│   │
│   └── Tickora.Web/
│       │
│       ├── Components/
│       │   │
│       │   ├── Layout/
│       │   │   ├── PublicLayout.razor
│       │   │   ├── CustomerLayout.razor
│       │   │   └── DashboardLayout.razor
│       │   │
│       │   ├── UI/
│       │   │   ├── AppButton.razor
│       │   │   ├── AppInput.razor
│       │   │   ├── AppSelect.razor
│       │   │   ├── AppDialog.razor
│       │   │   ├── AppDataTable.razor
│       │   │   ├── AppPagination.razor
│       │   │   ├── SearchInput.razor
│       │   │   ├── PageHeader.razor
│       │   │   ├── StatusBadge.razor
│       │   │   ├── PriorityBadge.razor
│       │   │   ├── ConfirmDialog.razor
│       │   │   ├── EmptyState.razor
│       │   │   ├── ErrorState.razor
│       │   │   └── LoadingSkeleton.razor
│       │   │
│       │   ├── Features/
│       │   │   │
│       │   │   ├── Dashboard/
│       │   │   │   ├── Pages/
│       │   │   │   └── Components/
│       │   │   │
│       │   │   ├── Tickets/
│       │   │   │   ├── Pages/
│       │   │   │   └── Components/
│       │   │   │       ├── TicketCard.razor
│       │   │   │       ├── TicketTable.razor
│       │   │   │       ├── TicketFilters.razor
│       │   │   │       ├── TicketForm.razor
│       │   │   │       ├── TicketDetails.razor
│       │   │   │       ├── TicketAssignment.razor
│       │   │   │       └── TicketTimeline.razor
│       │   │   │
│       │   │   ├── Conversations/
│       │   │   │   ├── MessageThread.razor
│       │   │   │   ├── MessageBubble.razor
│       │   │   │   ├── MessageComposer.razor
│       │   │   │   └── AttachmentList.razor
│       │   │   │
│       │   │   ├── KnowledgeBase/
│       │   │   ├── Notifications/
│       │   │   ├── Reports/
│       │   │   ├── Administration/
│       │   │   └── Settings/
│       │   │
│       │   ├── Account/
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   └── _Imports.razor
│       │
│       ├── Endpoints/
│       │   ├── TicketEndpoints.cs
│       │   ├── KnowledgeEndpoints.cs
│       │   └── AttachmentEndpoints.cs
│       │
│       ├── Hubs/
│       │   └── NotificationsHub.cs
│       │
│       ├── wwwroot/
│       │   ├── css/
│       │   ├── js/
│       │   └── images/
│       │
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
│
├── tests/
│   ├── Tickora.UnitTests/
│   ├── Tickora.IntegrationTests/
│   └── Tickora.E2ETests/
│
├── infrastructure/
│   └── docker/
│
├── docs/
│   ├── architecture.md
│   └── sla-rules.md
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── docker-compose.yml
├── .env.example
├── .editorconfig
├── Directory.Build.props
├── Tickora.slnx
└── README.md
```

## Architectural Rules

This structure defines the desired organization.

It does not mean that every possible file must be generated immediately.

Create files only when they provide meaningful functionality.

Keep feature-specific components inside their features.

Move components into the shared UI directory only when they are genuinely reused.

Use focused persistence interfaces for meaningful operations instead of automatically creating a generic repository for every entity.

Do not duplicate business logic between REST endpoints and Blazor components.

Do not create circular project references.

---

# 6. UI/UX DESIGN SYSTEM

Build a premium enterprise SaaS interface.

The visual design should look polished enough for a professional software portfolio.

## Visual Direction

Use:

- Deep navy or charcoal sidebar.
- Clean white content surfaces.
- Soft neutral page backgrounds.
- Restrained indigo or blue accents.
- Subtle teal success states.
- Clear warning and error colors.
- Professional typography.
- Consistent spacing.
- Elegant status badges.
- Compact readable data tables.
- Refined shadows.
- Thoughtful hover interactions.
- Responsive layouts.

Avoid:

- Excessive gradients.
- Neon colors.
- Generic Bootstrap admin templates.
- Oversized decorative cards.
- Unnecessary animations.
- Inconsistent spacing.
- Heavy visual clutter.

## Design Tokens

Centralize:

- Colors.
- Typography.
- Spacing.
- Border radii.
- Shadows.
- Button variants.
- Status colors.
- Responsive breakpoints.

Use CSS custom properties or a similarly simple design-token system.

Do not scatter arbitrary design values throughout Razor components.

## Main Layout

Create reusable:

- PublicLayout.
- CustomerLayout.
- DashboardLayout.

The DashboardLayout must be shared by agents and administrators.

Navigation can vary according to the user's permissions.

Do not duplicate the entire dashboard shell for every role.

## Navigation

Include:

- Overview.
- Tickets.
- My Queue.
- Customers.
- Knowledge Base.
- Reports.
- Notifications.
- Team Management.
- Settings.

Show role-appropriate items.

## Common UI States

Every relevant component must support:

- Loading.
- Empty.
- Error.
- Disabled.
- Success.
- Unauthorized state where appropriate.

No decorative controls that appear functional but do nothing.

---

# 7. TICKET MANAGEMENT

This is the central application feature.

## Ticket Model

Each ticket must support:

- Unique internal ID.
- Human-readable ticket number.
- Subject.
- Description.
- Customer ID.
- Assigned agent ID.
- Queue ID.
- Category ID.
- Priority.
- Status.
- Created timestamp.
- Last activity timestamp.
- First-response timestamp.
- Resolution timestamp.
- Closure timestamp.
- SLA metadata.
- Rowversion concurrency token.

## Statuses

Use:

- Open.
- In Progress.
- Waiting for Customer.
- Resolved.
- Closed.

Implement explicit valid transitions.

Allow reopening when business policy permits.

Do not allow arbitrary transitions.

## Priorities

- Low.
- Medium.
- High.
- Urgent.

## Ticket Operations

Implement:

- Create.
- View.
- Update permitted information.
- Assign.
- Reassign.
- Change priority.
- Change category.
- Change status.
- Reply.
- Add internal note.
- Upload attachment.
- Resolve.
- Close.
- Reopen.
- Search.
- Filter.
- Paginate.
- View activity history.

All operations must use persisted SQL Server data.

## Ticket List

Build a reusable TicketTable.

Support:

- Search by ticket number and subject.
- Status filtering.
- Priority filtering.
- Category filtering.
- Assigned agent filtering.
- Queue filtering.
- Date filtering.
- SLA status filtering.
- Sorting.
- Server-side pagination.

Use URL query parameters for shareable filters where appropriate.

Do not load every record into browser memory.

---

# 8. SUPPORT QUEUES AND ASSIGNMENT

Implement support queues.

Seed:

- General Support.
- Technical Support.
- Billing.
- Account Assistance.

## Queue Management

Administrators can:

- Create queues.
- Edit queues.
- Activate/deactivate queues.
- Assign agents to queues.
- Inspect queue workloads.

## Assignment

Support:

- Manual assignment.
- Reassignment.
- Unassignment.
- Optional simple round-robin assignment.

Validate agent eligibility.

Assignment must be authorized.

When concurrent assignment requests occur, use appropriate database concurrency protection.

Do not silently overwrite another agent's changes.

Do not create a complex workflow automation engine.

---

# 9. CUSTOMER CONVERSATIONS

Every ticket must contain a persistent conversation.

## Message Types

- Customer Reply.
- Agent Reply.
- Internal Note.
- System Activity.

## Message Fields

Each message must include:

- ID.
- Ticket ID.
- Author ID.
- Visibility.
- Body.
- Created timestamp.
- Attachment references.

## Internal Notes

Internal notes are private.

They must never be accessible through customer-facing pages, API responses, exports, attachments, or notifications.

Enforce visibility on the backend query layer.

Do not rely only on hiding internal notes in Razor components.

## Reusable Components

Build:

- MessageThread.
- MessageBubble.
- MessageComposer.
- AttachmentList.
- TicketActivityTimeline.

Use the same conversation components across authorized customer and agent views while respecting message visibility.

## Conversation Rules

- Persist messages before reporting success.
- Validate message length.
- Prevent duplicate submissions.
- Record message authorship.
- Maintain chronological ordering.
- Apply ticket state rules.
- Update ticket activity timestamps.
- Publish appropriate notifications.

Do not silently rewrite historical customer and agent messages.

---

# 10. REAL-TIME UPDATES WITH SIGNALR

Implement real-time support notifications.

## Required Events

- Ticket Created.
- Ticket Assigned.
- Customer Reply Received.
- Agent Reply Received.
- Ticket Status Changed.
- Ticket Priority Changed.
- SLA Warning.
- SLA Breach.

## Persistent Notifications

Store notifications in SQL Server.

Include:

- Recipient.
- Notification type.
- Title.
- Message.
- Related resource.
- Created timestamp.
- Read timestamp.

SignalR must only deliver events.

SQL Server remains the source of truth.

## SignalR Architecture

Create a focused notification publishing abstraction.

Use `IHubContext` in its implementation when a dedicated application hub is warranted.

Use authorized user identifiers or carefully validated groups.

Do not permit arbitrary clients to subscribe to private ticket events.

Authorization must be enforced before adding connections to protected groups.

## Blazor Requirements

Blazor Interactive Server already relies on SignalR.

Do not establish additional connections unnecessarily for ordinary component events.

Use an efficient notification update strategy that fits the selected rendering architecture.

When a dedicated hub is implemented, give it a clear responsibility, such as supporting independent realtime clients.

Ensure reconnecting clients can retrieve missed persistent notifications.

Avoid duplicate displayed notifications.

---

# 11. SLA ENGINE

The SLA engine is a major technical feature and must be implemented carefully.

## SLA Policies

Administrators can configure first-response and resolution targets.

Initial demonstration policies:

| Priority | First Response | Resolution |
|---|---|---|
| Low | 24 business hours | 72 business hours |
| Medium | 8 business hours | 32 business hours |
| High | 2 business hours | 8 business hours |
| Urgent | 30 business minutes | 4 business hours |

These values must be configurable.

Do not scatter these values as hardcoded constants throughout the application.

## Business Calendar

Support:

- Business timezone.
- Working weekdays.
- Opening hours.
- Closing hours.
- Break intervals.
- Holidays.
- Date-specific closures.

Use unambiguous UTC timestamps for persisted instants.

Use a documented IANA timezone policy for business calendars.

Handle timezone conversion and daylight-saving transitions correctly.

Use appropriate .NET time APIs or a focused timezone library when necessary for reliable cross-platform behavior.

## SLA Calculation

Calculate:

- First-response deadline.
- Resolution deadline.
- Remaining business time.
- Paused SLA duration.
- Breach status.
- SLA compliance.

The first response must be defined as the first qualifying customer-visible agent reply.

Internal notes and automated events must not count.

## SLA Pause Rules

Allow configurable pause behavior while a ticket is Waiting for Customer.

Record pause and resume instants.

Do not lose paused-time history when ticket statuses change.

## SLA Policy Snapshots

Persist applicable SLA targets and relevant calendar policy versions when necessary.

Historical SLA performance must not silently change because an administrator updates policy settings later.

## SLA Monitoring

Use a background service to detect:

- Approaching first-response deadlines.
- Missed first-response deadlines.
- Approaching resolution deadlines.
- Missed resolution deadlines.

Create persistent alerts.

Use stable idempotency keys or equivalent database constraints to prevent duplicate alerts across repeated scans.

## Background Processing

Use:

- BackgroundService.
- IServiceScopeFactory.
- CancellationToken.
- PeriodicTimer.
- Structured logging.
- Safe exception handling.
- Graceful shutdown.

Do not use the same DbContext instance across background iterations.

Define and document the monitoring process's ownership.

Avoid multiple uncoordinated scheduler instances in multi-replica deployments.

---

# 12. KNOWLEDGE BASE

Build a complete searchable knowledge base.

## Public Features

- Help Center homepage.
- Article categories.
- Search.
- Featured articles.
- Article detail pages.
- Related articles.
- Contact Support CTA.

## Management

Authorized administrators and agents can:

- Create articles.
- Edit articles.
- Publish/unpublish.
- Assign categories.
- Set featured status.
- Update summaries.

## Article Model

- ID.
- Title.
- Slug.
- Summary.
- Body.
- Category.
- Author.
- Status.
- Created timestamp.
- Updated timestamp.

Use safe content rendering.

Do not allow untrusted HTML or scripts.

## Search

Use SQL Server-backed search with efficient queries and indexes.

Do not add Elasticsearch without a real need.

## Ticket Integration

Suggest relevant help articles during ticket creation based on categories and terms.

Use a straightforward deterministic strategy.

Do not add unnecessary AI features.

---

# 13. CUSTOMER PORTAL

Build a polished customer-facing experience.

## Customer Dashboard

Display:

- Active tickets.
- Waiting tickets.
- Resolved tickets.
- Recent messages.
- Notifications.

## Create Ticket

Fields:

- Subject.
- Category.
- Description.
- Allowed attachments.
- Priority where permitted.

Use form validation.

Submit through the application service.

Avoid duplicate creation.

## Ticket Detail

Display:

- Ticket number.
- Subject.
- Status.
- Priority.
- Created date.
- Conversation.
- Public attachments.
- Relevant actions.

Allow customers to:

- Reply.
- Close eligible tickets.
- Reopen eligible tickets.
- View history.
- Submit feedback after resolution.

Customers must never see other customers' tickets.

---

# 14. AGENT WORKSPACE

Build a productivity-focused support interface.

## Agent Dashboard

Include:

- My assigned tickets.
- Unassigned queue tickets.
- High-priority tickets.
- Tickets approaching SLA deadlines.
- Overdue tickets.
- Recent customer responses.
- Today's activity.

## Ticket Inbox

Create reusable filtered views:

- Assigned to Me.
- Unassigned.
- Open.
- Waiting for Customer.
- High Priority.
- SLA at Risk.
- Recently Updated.

## Ticket Detail Workspace

Build a well-organized interface containing:

- Ticket title and status.
- Conversation thread.
- Reply composer.
- Internal notes.
- Customer information.
- Assignment.
- Queue.
- Priority.
- Category.
- SLA indicator.
- Activity timeline.
- Related knowledge articles.

Keep frequently used actions readily accessible.

Avoid unnecessary navigation for common ticket workflows.

## Canned Responses

Allow authorized agents to insert predefined response templates.

Implement basic management of canned responses.

Reuse the existing message composer.

Do not introduce a completely separate editor.

---

# 15. ADMIN DASHBOARD

Create a premium administration interface.

## Overview Metrics

Include:

- Total tickets.
- Open tickets.
- Resolved tickets.
- Average first-response time.
- Average resolution time.
- SLA compliance rate.
- Active agents.
- Queue workload.
- Customer satisfaction.

Metrics must come from real SQL Server data.

## Charts

Include:

- Ticket volume over time.
- Tickets by status.
- Tickets by category.
- Agent workload.
- SLA performance.
- Resolution trends.

Use a small, justified chart integration.

Do not install multiple chart libraries.

Keep chart data calculation on the backend.

## Team Management

Administrators can:

- View users.
- Create/invite agent accounts.
- Change authorized roles.
- Activate/deactivate accounts.
- Configure queue memberships.

Do not allow public users to register as agents or administrators.

## Categories and Queues

Implement CRUD with correct permissions.

## SLA Settings

Implement policy and business-calendar management.

## Reports

Allow:

- Date-range filtering.
- Team filtering.
- Status filtering.
- Performance summaries.
- CSV export.

Protect reports against unauthorized access.

Ensure exported CSV values are safely escaped against spreadsheet formula injection.

---

# 16. ENTITY FRAMEWORK CORE AND SQL SERVER

Design a normalized relational database.

## Main Entities

Include:

- ApplicationUser.
- Ticket.
- TicketMessage.
- TicketAttachment.
- TicketCategory.
- SupportQueue.
- QueueMembership.
- TicketActivity.
- SlaPolicy.
- BusinessCalendar.
- BusinessHoliday.
- SlaEvent.
- Notification.
- KnowledgeArticle.
- KnowledgeCategory.
- CannedResponse.
- CustomerFeedback.

Add additional entities only when required by working features.

## Database Rules

Use:

- Proper foreign keys.
- Appropriate unique constraints.
- Efficient indexes.
- UTC timestamps.
- Safe string lengths.
- Appropriate numeric types.
- Explicit deletion policies.
- Rowversion concurrency tokens.
- Transactions for related writes.

## Concurrency

Implement optimistic concurrency where appropriate.

Use SQL Server rowversion for mutable records that can be modified concurrently.

Catch `DbUpdateConcurrencyException`.

Return meaningful conflict responses.

Do not silently overwrite another user's changes.

## Database Access

Use EF Core efficiently.

Prefer:

- AsNoTracking for read-only queries.
- Projection into DTOs for lists.
- Server-side filtering.
- Server-side pagination.
- Efficient relationships.
- Correct DbContext lifetimes.

Avoid N+1 queries.

Do not load complete ticket conversations when only summary counts are required.

## Blazor Database Lifetimes

Be especially careful with long-lived Interactive Server circuits.

Do not keep one shared DbContext alive for the lifetime of an interactive circuit.

Use short-lived units of work and appropriate DbContext factories within the Infrastructure implementation.

Keep EF Core details out of presentation components.

Use focused Application persistence interfaces instead of introducing redundant generic repositories.

---

# 17. AUTHENTICATION AND AUTHORIZATION

Use ASP.NET Core Identity.

## Authentication

Implement:

- Customer registration.
- Login.
- Logout.
- Password change.
- Password reset.
- Session management.
- Secure authentication cookies.
- Account lockout.
- Login rate limiting.

Use supported ASP.NET Core authentication patterns compatible with the Blazor render modes.

Handle login and logout through suitable HTTP request/response flows when authentication cookies must be issued or cleared.

Do not attempt to issue authentication cookies from an established interactive circuit where no suitable HTTP response is available.

## Roles

Implement:

- Customer.
- Agent.
- Admin.

## Authorization

Use framework authorization policies and resource-level checks.

Customers can only access their own tickets.

Agents can only access authorized queues and tickets.

Administrators have explicitly defined permissions.

Authorization must be enforced in Application workflows and HTTP boundaries as appropriate.

Do not rely only on hidden navigation items.

## Security

Implement:

- Antiforgery protection.
- Secure cookie settings.
- HTTPS production configuration.
- Input validation.
- Rate limiting.
- Access checks.
- Output encoding.
- Appropriate upload security.
- Safe error handling.
- Safe logging.
- Protection against unauthorized notification subscriptions.

Do not expose internal notes, private attachments, session secrets, or sensitive user fields.

---

# 18. REST API ARCHITECTURE

Expose a focused versioned API.

Base path:

`/api/v1`

## Tickets

```http
GET    /api/v1/tickets
POST   /api/v1/tickets
GET    /api/v1/tickets/{id}
PATCH  /api/v1/tickets/{id}
POST   /api/v1/tickets/{id}/reply
POST   /api/v1/tickets/{id}/assign
POST   /api/v1/tickets/{id}/transition
GET    /api/v1/tickets/{id}/activities
```

## Knowledge Base

```http
GET    /api/v1/knowledge/articles
GET    /api/v1/knowledge/articles/{slug}
```

## Notifications

```http
GET    /api/v1/notifications
POST   /api/v1/notifications/{id}/read
```

## Attachments

Implement authenticated and authorized upload/download endpoints as needed.

## API Standards

Use:

- Typed DTOs.
- Input validation.
- ProblemDetails.
- OpenAPI documentation.
- Authorization.
- Pagination.
- Consistent error codes.
- Appropriate HTTP statuses.
- Request cancellation.

### Critical Architecture Rule

Blazor Interactive Server components should invoke shared application services directly where appropriate.

Do not force server-rendered components to call their own local HTTP endpoints unnecessarily.

REST endpoints and Blazor pages must use the same business workflows.

---

# 19. FILE ATTACHMENTS

Build a secure attachment system.

Support:

- Allowed file types.
- File-size limits.
- Authorized upload.
- Authorized download.
- Safe filename handling.
- Attachment metadata.
- Attachment listing.

Store uploaded files outside publicly accessible static directories.

Use generated storage identifiers.

Never trust client-supplied filenames or MIME types alone.

Validate file signatures where practical.

Do not execute uploaded content.

Check permissions before reading files.

Use a focused IFileStorage abstraction.

Provide local filesystem storage for development.

Document additional malware-scanning requirements for a real production deployment.

---

# 20. AUDIT LOGGING AND ACTIVITY HISTORY

Record meaningful ticket events.

Examples:

- Ticket created.
- Ticket assigned.
- Agent reassigned.
- Priority changed.
- Status changed.
- Customer replied.
- Agent replied.
- Internal note created.
- Ticket resolved.
- Ticket reopened.
- SLA warning.
- SLA breach.

Store:

- Event identifier.
- Ticket ID.
- Actor ID.
- Event type.
- Timestamp.
- Relevant metadata.

Avoid storing unnecessary sensitive information.

Keep activity logs append-only through ordinary workflows.

Centralize activity recording where possible.

---

# 21. PERFORMANCE OPTIMIZATION

Optimize the application without unnecessary complexity.

## C# Backend

Use:

- Async database operations.
- CancellationToken.
- Efficient queries.
- Bounded pagination.
- Appropriate indexes.
- Targeted projections.
- Short-lived DbContexts.
- Database transactions where necessary.

Avoid:

- Excessive memory allocation.
- Unnecessary synchronous I/O.
- Unbounded queries.
- Redundant database access.
- Repeated service operations.

## Blazor

Use:

- Appropriate render modes.
- Focused component state.
- Reusable Razor components.
- Efficient rendering.
- Controlled event subscriptions.
- Correct component disposal.
- Minimal JavaScript interop.

Do not use excessive StateHasChanged calls.

Do not introduce a JavaScript state-management framework.

## Background Processing

Use:

- Efficient SLA queries.
- Configurable scan frequency.
- Cancellation-aware operations.
- Deduplicated alerts.
- Safe scoped dependencies.

Do not add Redis simply to make the architecture appear more advanced.

---

# 22. SEED DATA AND DEMO EXPERIENCE

Create realistic development data.

Include:

- One fictional organization.
- At least 5 agents.
- At least 30 customers.
- At least 100 support tickets.
- Different priorities.
- Multiple categories.
- Multiple support queues.
- Open and resolved tickets.
- Waiting-for-customer tickets.
- Closed tickets.
- Conversation messages.
- Internal notes.
- Ticket activities.
- SLA policies.
- Historical SLA breaches.
- Notifications.
- Knowledge articles.
- Canned responses.
- Customer feedback.

Data must be internally consistent.

Do not create SLA values that contradict recorded ticket history.

Use fictional personal details.

## Demo Accounts

Provide development-only credentials for:

- Administrator.
- Support Agent.
- Customer.

Document credentials in README.

Never use demo credentials in production.

## Seed Requirements

The seed process should be repeatable.

Avoid uncontrolled duplication on repeated runs.

Use clearly separated development seeding and normal application startup.

Do not automatically seed a public production system with known default credentials.

---

# 23. AUTOMATED TESTING

Testing is mandatory.

## Unit Tests

Cover:

- Ticket transition rules.
- Priority rules.
- Assignment eligibility.
- SLA calculation.
- Business hours.
- Break intervals.
- Weekend exclusions.
- Holiday exclusions.
- Timezone calculations.
- First-response detection.
- SLA pause/resume.
- SLA warning deduplication.
- Resource authorization rules.

## Integration Tests

Use SQL Server for database-specific integration testing.

Cover:

- Identity authentication.
- Role authorization.
- Ticket creation.
- Assignment.
- Concurrent updates.
- Message persistence.
- Internal note privacy.
- SLA persistence.
- Knowledge article management.
- Attachment authorization.
- Unauthorized customer access.

Do not use the EF Core InMemory provider as proof of SQL Server transactional or concurrency behavior.

## End-to-End Tests

Use Playwright.

Test:

1. Customer login.
2. Customer ticket creation.
3. Ticket appearing in an authorized support queue.
4. Agent assignment.
5. Agent reply.
6. Customer viewing the reply.
7. Internal note remaining hidden from customers.
8. Ticket resolution.
9. Knowledge-base search.
10. Administrator dashboard.
11. Role restrictions.
12. Notification behavior.
13. Responsive navigation.

Tests must make meaningful assertions.

Do not fabricate test results.

---

# 24. DOCKER AND DEVELOPMENT ENVIRONMENT

Provide a working Docker-based development environment.

## Docker Services

- tickora-web.
- tickora-sqlserver.

Use a multi-stage .NET Dockerfile.

## Configuration

Provide:

- `docker-compose.yml`.
- `.env.example`.
- Development settings.
- Production configuration guidance.
- Health checks.
- Environment validation.

Document required SQL Server credentials and strong development secret configuration.

Account for SQL Server container architecture support.

Do not assume that every ARM-based machine can run the selected SQL Server image natively.

Document supported platforms and suitable development alternatives.

## Development Commands

Provide exact commands for:

- Restoring packages.
- Building the solution.
- Running migrations.
- Seeding.
- Starting the application.
- Running tests.
- Starting Docker services.

## CI/CD

Add GitHub Actions for:

- Restore.
- Build.
- Formatting verification.
- Unit tests.
- Integration tests where infrastructure is available.
- Publish validation.

Do not claim deployment was performed unless it actually was.

---

# 25. IMPLEMENTATION ROADMAP

Follow this implementation sequence.

## Phase 1 — Project Foundation

- Inspect repository.
- Verify installed .NET SDK.
- Create solution.
- Create projects.
- Configure project references.
- Configure dependency injection.
- Configure EF Core.
- Configure SQL Server.
- Configure logging.
- Configure development tools.

## Phase 2 — Domain and Persistence

- Implement ticket entities.
- Implement statuses.
- Implement priorities.
- Implement business policies.
- Configure database mapping.
- Create migrations.
- Add concurrency tokens.
- Create seed infrastructure.

## Phase 3 — Identity and Authorization

- Configure ASP.NET Core Identity.
- Create roles.
- Implement login.
- Implement registration.
- Implement logout.
- Implement authorization policies.
- Add demo accounts.

## Phase 4 — Ticket Management Backend

- Implement ticket workflows.
- Implement queries.
- Implement assignment.
- Implement conversations.
- Implement internal notes.
- Implement activity history.
- Test core operations.

## Phase 5 — Reusable UI Foundation

- Build visual design tokens.
- Create foundational Razor components.
- Implement layouts.
- Implement navigation.
- Add loading/error states.
- Implement role-aware navigation.

## Phase 6 — Customer Portal

- Build help center.
- Build customer dashboard.
- Implement ticket creation.
- Implement ticket details.
- Implement replies.
- Implement notifications.

## Phase 7 — Agent Workspace

- Build agent dashboard.
- Implement ticket inbox.
- Implement filters.
- Implement ticket details.
- Implement replies.
- Implement internal notes.
- Implement assignments.
- Implement canned responses.

## Phase 8 — SLA Engine

- Implement SLA policies.
- Configure business calendars.
- Calculate deadlines.
- Implement pause/resume.
- Implement background monitoring.
- Implement warnings and breaches.
- Test edge cases.

## Phase 9 — Real-Time Communication

- Implement notification persistence.
- Integrate SignalR where appropriate.
- Update relevant UI views.
- Test authorization.
- Test reconnection behavior.

## Phase 10 — Administration

- Implement team management.
- Implement queue management.
- Implement categories.
- Implement SLA settings.
- Implement knowledge management.
- Implement analytics and reports.

## Phase 11 — Quality Assurance

- Review code structure.
- Refactor duplicated code.
- Review component reuse.
- Test authorization.
- Optimize queries.
- Verify responsive layouts.
- Test background processing.
- Run automated tests.
- Verify Docker startup.
- Complete documentation.

Deliver working vertical slices throughout development.

Do not create every feature as a nonfunctional placeholder.

---

# 26. MANDATORY REUSABILITY AND ARCHITECTURE REVIEW

After each major phase, inspect the implementation.

Ask:

1. Are Razor components duplicated?
2. Is business logic repeated?
3. Are Blazor pages directly executing database queries?
4. Are Application services cohesive?
5. Are there unnecessary interfaces?
6. Are there redundant repository layers?
7. Are EF Core contexts being managed safely?
8. Are authentication and authorization centralized?
9. Are SLA calculations implemented in only one authoritative place?
10. Are ticket state transitions enforced consistently?
11. Are real-time notifications authorized?
12. Are there unnecessary packages?
13. Are there unused files?
14. Are any components becoming excessively large?
15. Can repeated interface patterns be reused?
16. Are there circular dependencies?
17. Is the code readable?
18. Can complexity be reduced without sacrificing correctness?
19. Do all important controls have working actions?
20. Does the solution still follow the intended architectural boundaries?

Refactor meaningful problems.

Do not introduce patterns merely to make the architecture look sophisticated.

**Professional architecture is measured by clarity, correctness, cohesion, and maintainability—not the number of layers or files.**

---

# 27. DEFINITION OF DONE

The application is complete only when:

## Core

- The .NET solution builds.
- SQL Server connects.
- EF Core migrations run successfully.
- Development seed data works.
- Authentication works.
- Roles are enforced.
- Authorization is correct.

## Tickets

- Customers can create tickets.
- Agents can manage authorized tickets.
- Assignment works.
- Status transitions work.
- Priorities work.
- Search and filtering work.
- Conversation messages persist.
- Internal notes remain private.
- Attachments are protected.
- Audit history works.

## Real-Time and SLA

- Notifications persist.
- Real-time updates work where implemented.
- Authorized users receive appropriate events.
- SLA deadlines are calculated correctly.
- Background monitoring works.
- SLA warnings and breaches are not duplicated.
- Timezone edge cases are tested.

## Administration

- Administrator dashboard works.
- Team management works.
- Support queues work.
- Knowledge-base management works.
- Reports use real database data.

## Quality

- UI is responsive.
- Razor components are reusable.
- Major business logic is not duplicated.
- Project structure is organized.
- Important tests pass.
- No essential interactions are fake.
- Docker setup is documented.
- README is complete.
- Known limitations are disclosed.

Do not mark unfinished functionality as complete.

---

# 28. FINAL CODEX EXECUTION INSTRUCTIONS

## Before Writing Code

1. Inspect the current repository.
2. Verify installed SDK and development tools.
3. Review project naming conventions.
4. Analyze the requested architecture.
5. Identify the smallest cohesive set of modules.
6. Create a concise implementation plan.

Then proceed with implementation.

Do not stop after generating the solution structure.

## During Implementation

- Use idiomatic modern C#.
- Keep application services focused.
- Reuse Razor components.
- Avoid duplicated business rules.
- Respect project boundaries.
- Use dependency injection appropriately.
- Manage DbContext lifetimes correctly.
- Use asynchronous APIs for I/O.
- Validate important inputs.
- Enforce authorization.
- Implement genuine database persistence.
- Test important workflows.
- Run builds regularly.
- Fix errors instead of suppressing them.
- Avoid unnecessary packages and abstractions.

Do not request approval between ordinary development phases unless a genuine blocker requires clarification.

If execution limits prevent complete implementation in one session, finish a coherent and tested vertical slice, accurately identify remaining work, and prioritize continuation.

## Before Completion

Perform:

1. Architecture review.
2. Reusability review.
3. Database review.
4. Authorization review.
5. Concurrency review.
6. SLA correctness review.
7. SignalR security review.
8. Responsive UI review.
9. Dependency review.
10. Automated testing.
11. Docker verification.
12. Documentation review.

Do not fabricate successful command execution, tests, or completed features.

## Final Delivery Report

Provide:

1. Completed functionality.
2. Final solution architecture.
3. File and folder structure.
4. Reusable Razor components.
5. Database schema overview.
6. Ticket workflow explanation.
7. SLA engine design.
8. Real-time notification architecture.
9. Security implementation summary.
10. Local setup commands.
11. EF Core migration commands.
12. Seed commands.
13. Demo credentials.
14. Tests executed and actual results.
15. Known limitations.
16. Remaining tasks, if any.

---

# FINAL PROJECT OBJECTIVE

Deliver **TICKORA** as a professional, polished, secure, reliable, and fully functional enterprise helpdesk platform.

The project must demonstrate strong expertise in:

**C# + .NET 10 + ASP.NET Core + Blazor + Entity Framework Core + SQL Server + ASP.NET Core Identity + SignalR + Background Services + Clean Architecture.**

The final application must be:

- Modern.
- Visually professional.
- Fully functional.
- Component-based.
- Reusable.
- Modular.
- Secure.
- Optimized.
- Maintainable.
- Testable.
- Well-documented.
- Easy to extend.

Avoid monolithic Razor components, duplicated business services, unnecessary abstraction layers, tangled dependencies, and fake dashboard data.

**Build a professional .NET application that demonstrates advanced full-stack engineering through real business workflows, reliable backend logic, clean code, and excellent software architecture.**

**Optimize for code quality, maintainability, correctness, reusability, performance, and professional presentation—not code volume.**
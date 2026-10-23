# TICKORA code and responsive review — 23 October 2026

## Scope and component boundaries

Reviewed the four-layer application and the browser UI running against local SQL Server 2022. Business mutations remain in the application services and authorization remains enforced by the server. Pages coordinate data loading and actions; reusable components render individual features.

| Area | Components |
| --- | --- |
| Workspace shell | `DashboardLayout`, `WorkspaceSidebar`, `WorkspaceTopbar`, `NotificationBell` |
| Ticket workspace | `TicketTable`, `TicketProperties`, `SlaIndicator`, `TicketTimeline`, `TicketFeedback` |
| Conversation | `MessageThread`, `MessageComposer`, `AttachmentList` |
| Administration | `TeamMemberCard`, `SlaPolicyEditor` |
| Shared UI | Page header, buttons, status/priority badges, metrics, charts, pagination, loading, empty and error states |

The sidebar, top bar, team member card, SLA editor and feedback form were extracted during this review. The parent pages retain service calls, concurrency versions, busy/error handling and authorization context. Components communicate through typed parameters and callbacks.

## Issues corrected

- **Category select appeared blank:** SQL Server converted lookup GUIDs to uppercase while Blazor formatted the bound `Guid` in lowercase. Format lookup IDs after materialization. A SQL regression test and an authenticated browser test cover the mismatch.
- **Empty customer status control:** hide the status selector/action when there are no allowed transitions instead of showing an empty select and disabled button. Server transition policies remain authoritative.
- **Mobile navigation:** hide the closed sidebar from focus/accessibility navigation, provide a backdrop for closing it, allow the sidebar itself to scroll, and keep the mobile header available while scrolling. Added a keyboard skip link and an accessible profile name.
- **Small screens:** use 44px primary controls/touch targets and 16px form input text, wrap composer controls and message metadata, and wrap long text/file names. Conversation text is larger on mobile.
- **Text layout:** message bodies, the composer, ticket descriptions and headings detect text direction automatically. Private notes retain their distinct amber treatment and explicit label.
- **Feedback:** the extracted component uses a real submit form and disables editing while saving. Browser journeys now submit feedback and verify persistence after reload.

## Verification

The responsive browser matrix checks these 11 routes at **320, 390, 768 and 1440px**: overview, ticket inbox, ticket creation, customers, knowledge base, knowledge management, reports, notifications, team/queues, SLA policies and settings. It checks successful content loading, error states, dark color scheme, JavaScript page errors and document overflow. Ticket detail gets additional checks at all four widths. Scrollable tables may scroll inside their own containers.

The customer-to-agent browser journey runs at both **390 and 1440px**: create ticket, upload/download a public file, assign an agent, add a private note/file, send a public reply, confirm customer privacy, resolve the ticket, submit feedback and verify notification history. Navigation checks cover keyboard opening, backdrop closing and closing after a route change. Existing SQL tests exercise cross-user/queue isolation, private attachments, antiforgery, stale writes, identity changes, reporting and realtime recipient isolation.

Visual review also covered the ticket conversation with public/private messages, the 320px composer, the 390px creation form, mobile team cards and desktop reporting. Screenshots are in `docs/screenshots/review-*`.

| Final check | Result |
| --- | --- |
| Solution Debug build | Passed; zero warnings/errors |
| Unit tests | 48 passed; zero failures/skips |
| Real SQL integration suite | 29 passed; zero failures/skips |
| Main browser suite | 9 passed; zero failures/skips |
| Additional mobile ticket search/pagination case | 1 passed; zero failures/skips |
| Formatting/analyzers | `dotnet format --verify-no-changes --severity warn` passed |
| Running application | `/health/ready` returned `ready`; no `fail:` entries or stderr output |

The ten browser cases passed on the final application build. The main nine-case run and the subsequently added focused search/pagination case are retained in ignored `.tools/quality-results/browser-final.trx` and `.tools/quality-results/search-review.trx`; console output for the main run is in `.tools/quality-browser.log`. Earlier diagnostic failures were corrected or rerun with the installed Edge channel; these final result files identify the successful checks.

The mobile search case verifies moving to page two, searching by an existing ticket number, returning to a single matching result/page one, and resetting the filters. The original TK-1044 public/private sample messages were preserved.

## English-only content follow-up

Project-owned source, documentation, example data and saved screenshots now use English. The two TK-1044 demonstration messages were translated in place, preserving their visibility, authors and timestamps. Conversation screenshots were regenerated from the updated customer/staff views. The half-hour calendar test now uses `Asia/Kolkata`; all 48 unit tests passed after the update. A text audit covered 180 project text files/private text attachments with no remaining matches, and a separate database audit covered all 63 text columns with zero remaining matches. Project language guidance is recorded in `AGENTS.md`.

## Reproduce

From the repository root run `./scripts/Test-Tickora.ps1`. It starts SQL Server and the native app, builds the solution, and enables all SQL/browser suites using installed Edge. Browser tests add fictional development records; SQL integration tests use isolated temporary databases. Credentials remain in the ignored `.env` file.

The browser checks use Chromium/Edge with viewport emulation plus the in-app browser. They are not physical iOS/Android, Safari/Firefox, load testing or a production deployment assessment. Deployment-specific requirements remain documented in `delivery-report.md`.

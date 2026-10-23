# TICKORA delivery and verification

For the subsequent component review, category-selection fix, mobile navigation/accessibility updates and expanded desktop/mobile checks, see [the 23 October quality review](quality-review.md).

Verified locally on 22 October 2026. The application runs at http://localhost:5180 with SQL Server 2022 Developer in Docker on Ubuntu-24.04 WSL. The existing Windows machine already had a WSL Docker Engine; the previous report incorrectly inferred Docker was unavailable from the missing Windows CLI.

## Start and accounts

Run `./scripts/Start-Tickora.ps1` from the repository root. It discovers the current private WSL address, keeps WSL alive, starts SQL, applies the three migrations, seeds missing development data and starts the native Windows web process. Logs are under ignored `.tools/`. `./scripts/Stop-Tickora.ps1` stops only TICKORA and preserves SQL/private-file volumes. `./scripts/Test-Tickora.ps1` builds and runs all suites using installed Edge. Use `-SkipBuild` only with current binaries.

Development accounts: `admin@tickora.example`, `agent@tickora.example`, `customer@tickora.example`. Their initial password is `DEMO_PASSWORD` in the ignored root `.env`; SQL has a separate random password. Existing passwords are never reset by seeding. Demo data initially contains 108 tickets, five agents, thirty customers, four queues and six articles. Browser tests add explicitly named fictional tickets/guides/responses to the development database. SQL integration tests instead create and drop isolated `TickoraTest_<guid>` databases and private temporary files.

## Completed continuation

- Provisioned persistent SQL Server 2022, applied migrations and seeded the real development database. Added Windows/WSL startup, shutdown and complete-test scripts.
- Executed the previously skipped SQL and authenticated browser checks. Extended real SQL coverage to account registration/change/reset, queue permissions and live deactivation, file upload/download privacy, stale HTTP writes, notification ownership/pagination, filtered dashboards/reports, content/template administration, SLA escalation and actual SignalR user isolation.
- Fixed nullable API query binding, safe HTTP error handling, permanently disabled response/membership controls, reply input updates, early prerender search submission and mobile sidebar navigation. Cached component cancellation tokens and guarded notification subscriptions prevent navigation/disposal races; acceptance also checks the web log for unexpected errors.
- Suppressed customer realtime hints for internal notes/private files; public replies still signal the owning customer and never another customer.
- Added paginated/unread notification history, related guides, saved-response editing/archiving/restoration and category/template rowversion checks.
- Extended business calendars with multiple breaks and date-specific schedules. Corrected repeated-hour arithmetic around DST breaks/closures.
- Batched SLA candidates and routed unassigned alerts to active queue staff/admins. Unique activity event keys prevent duplicate audit rows while per-recipient notification keys preserve delivery deduplication.
- Updated the generated idempotent SQL script and developer documentation. Desktop dashboard and mobile knowledge screenshots are in `docs/screenshots/` and were visually inspected.

## Executed validation

| Check | Result |
|---|---|
| Debug solution build | Passed, zero warnings/errors |
| Unit tests | 48 passed, zero failures/skips |
| Integration tests | 28 passed, zero failures/skips, including 16 real SQL workflow cases |
| Playwright / installed Edge | 5 passed on native Windows and repeated against the Linux container, zero failures/skips |
| Browser paths | Login/responsive layouts; customer creation, agent assignment/private note/public reply/resolution; public/private file upload and authorized download; notification list; mobile search/navigation; administrator restrictions; article publishing; saved-response editing/archiving |
| SQL-backed security | Cross-customer and cross-queue isolation; deactivation; antiforgery; reset token lifecycle; private files; stale rowversion 409; report permissions; hub recipient isolation |
| EF model | No pending changes; idempotent script regenerated for all three migrations |
| NuGet audit | No known vulnerable direct/transitive packages reported by configured NuGet source |
| Local readiness | `/health/ready` returns 200 with no pending migrations |
| Formatting and Release publish | Passed; publish output in ignored `.tools/publish` |
| SQL persistence after stop/start | Passed; 112 existing tickets, 36 accounts and all three migrations retained |
| Docker source build and startup | Passed; explicit migration/seed and healthy web/SQL containers |
| Private-file volume persistence | Passed; marker retained after replacing the web container |
| Shared Windows/WSL file store | Passed; Linux upload downloaded through the native Windows host after separate sign-in; persisted attachment files checked against SQL metadata |

Test output is retained in ignored `.tools/full-test.log`, `.tools/sql-test.log`, `.tools/browser-test.log`, `.tools/docker-browser-test.log` and `.tools/docker-runtime.log`. The native web application remains running at localhost:5180. The extra Docker web instance was stopped after validation; both use the persistent SQL container. The WSL override binds the same ignored `src/Tickora.Web/App_Data` directory for both web hosts, so attachment metadata, private files and keys remain consistent. Existing container files were preserved without overwriting native files. Standard standalone Docker uses the named private-file volume. This report describes executed local checks, not a remote GitHub Actions run or a production deployment.

## Implementation boundaries

The four-layer modular monolith uses .NET 10, Blazor Interactive Server, ASP.NET Core Identity, EF Core SQL Server and SignalR. TicketService is shared by components and protected HTTP endpoints. Related writes, activity and notifications commit atomically. SQL rowversion protects concurrent administration and ticket changes. Encoded articles and authorized private storage avoid serving untrusted HTML or unprotected attachments.

Notification hints are best-effort after SQL commit; reconnecting clients recover authoritative paginated rows. There is no durable transactional outbox. Reports describe the latest resolution cycle and exports are buffered; long conversations do not have separate message pagination. Archived responses retain history rather than supporting destructive removal. Calendar JSON is edited in the SLA administration screen rather than a separate holiday-calendar designer.

Production deployment still requires deployment-specific TLS/proxy configuration, least-privilege SQL credentials, protected/shared keys and file storage, SMTP delivery credentials, backups, retention/quota/malware-scanning policy and workload/security assessment. Development password reset delivery was verified through its private file outbox; no external SMTP account or production deployment was configured.

## Dark workspace appearance

The default UI now uses layered navy/charcoal surfaces, a restrained violet accent, dark native controls and distinct semantic status colors. Small labels were enlarged, keyboard focus remains visible on controls, and the mobile table's hidden accessible label no longer expands the page horizontally. Dashboard, ticket inbox and creation form were visually checked while signed in as administrator; 390px mobile layout was checked without page overflow. Measured contrast ratios were 15.55:1 for primary text, 6.94:1 for card captions and 4.81:1 for primary button labels. The solution build and formatting checks passed. Screenshots: `docs/screenshots/dark-dashboard.png` and `docs/screenshots/dark-mobile.png`.

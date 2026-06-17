# Implementation Status

## Current phase

Wave 4 dashboard is complete. Wave 5 WhatsApp capture is the next incomplete wave.

Actual repository state:
- .NET 10 modular-monolith backend is present with API, Application, Contracts, Domain and Infrastructure projects.
- React, TypeScript, Vite and Tailwind frontend is present under `frontend/randwise-web`.
- Unit, architecture and integration test projects are present.
- GitHub remote is configured for `Henkomeyer/RandWise`.

## Wave status

| Wave | Status | Integration gate |
|---|---|---|
| 0 Repository bootstrap | Complete | Passed |
| 1 Domain/platform foundation | Complete | Passed |
| 2 First vertical slice | Complete | Passed |
| 3 Budgeting | Complete | Passed |
| 4 Dashboard | Complete | Passed |
| 5 WhatsApp capture | Not started | Pending |
| 6 Intelligence/jobs | Not started | Pending |
| 7 Reports/privacy | Not started | Pending |
| 8 Beta hardening | Not started | Pending |

## Decisions

- .NET 10 modular monolith
- React, TypeScript, Vite and Tailwind
- EF Core with SQLite for MVP
- integer cents for all money
- WhatsApp uses the same transaction command as the web
- deterministic parsing before AI
- dashboard centred on safe-to-spend and spending pace
- Wave 0 added a minimal `GET /api/v1/health` endpoint only.
- Wave 0 integration installed and used .NET SDK 10.0.301 locally because the workspace initially had only .NET SDK 9.0.304.
- Wave 0 frontend uses contract-shaped static dashboard fixture data until live API work begins.

## Completed integration gates

### RW-004 Wave 0 integration baseline

- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet build RandWise.sln` passed with 0 warnings and 0 errors.
- `dotnet test RandWise.sln --no-build` passed with 4 tests.
- `npm ci` passed with 0 vulnerabilities after stopping the local Vite dev server that had locked a native dependency.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 2 tests.
- `npm run build` passed.
- `npm run test:e2e` is not available yet in Wave 0.

### Wave 1 parallel batch RW-101 through RW-104

- RW-101 core domain foundation complete.
- RW-102 authentication/token foundation complete.
- RW-103 frontend design system and app shell complete.
- RW-104 CI pipeline and quality gates complete.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet build RandWise.sln` passed with 0 warnings and 0 errors.
- `dotnet test RandWise.sln --no-build` passed with 27 tests.
- `npm ci` passed with 0 vulnerabilities.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 4 tests.
- `npm run build` passed.
- `npm run test:e2e` is not available yet.

### RW-106 Wave 1 full foundation integration

- RW-105 EF Core model and initial SQLite migration complete.
- Integration wired `RandWise.Api` to `RandWise.Infrastructure` at the composition root and registered `AddRandWisePersistence`.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet build RandWise.sln` passed with 0 warnings and 0 errors.
- `dotnet test RandWise.sln --no-build` passed with 30 tests.
- `npm ci` passed with 0 vulnerabilities.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 4 tests.
- `npm run build` passed.
- `npm run test:e2e` is not available yet.

### Wave 2 prerequisite blocker resolution RW-102B

- Authentication persistence completed because Wave 2 acceptance requires register and sign-in.
- Core auth endpoints `/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/auth/refresh`, `/api/v1/auth/logout`, and `/api/v1/auth/me` now execute against Identity, JWT and hashed rotating refresh tokens.
- Password reset endpoints remain scoped placeholders.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet build RandWise.sln` passed with 0 warnings and 0 errors.
- `dotnet test RandWise.sln --no-build` passed with 26 tests.
- Integration test discovery confirms persistence migration/idempotency tests and real auth integration tests are present.
- `npm ci` passed with 0 vulnerabilities.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 4 tests.
- `npm run build` passed.

### Wave 2 backend slice RW-201 and RW-202

- RW-201 transaction application use cases and API complete for the first vertical slice.
- RW-202 financial profile and onboarding API complete for profile get/upsert.
- Integration manually resolved shared endpoint wiring in `Program.cs`.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet test tests/RandWise.IntegrationTests/RandWise.IntegrationTests.csproj` passed with 9 tests.
- `dotnet test RandWise.sln` passed with 33 tests.

### Wave 2 frontend slice RW-203 and RW-204

- RW-203 frontend authentication and onboarding complete.
- RW-204 transaction frontend list and quick-add shell complete.
- Frontend is connected to live auth, financial-profile and transaction endpoints through `VITE_API_BASE_URL`.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 4 tests.
- `npm run build` passed.

### Wave 2 integration checkpoint

- Current completed Wave 2 task coverage: RW-201, RW-202, RW-203, RW-204.
- Local registration failure fixed by aligning the frontend default API URL with the API launch profile, adding a local development CORS allowlist, and applying EF migrations automatically in Development.
- Browser-backed registration now reaches `/onboarding` successfully against the running API.
- RW-205 live frontend transaction flow is partially implemented through live API client wiring and registration has been browser-verified, but full add/list/edit/delete/restore browser acceptance remains pending.
- RW-206 cross-user authorization integration tests are partially covered by transaction list isolation, but the dedicated task remains pending.
- RW-207 vertical-slice acceptance remains pending.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet test RandWise.sln` passed with 33 tests.
- `npm ci` passed with 0 vulnerabilities.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 4 tests.
- `npm run build` passed.
- In-app browser smoke check passed for `http://127.0.0.1:5173/login`.
- In-app browser registration check passed for `http://127.0.0.1:5173/register` to `/onboarding`.
- `npm run test:e2e` is not available yet.

### RW-207 Wave 2 vertical-slice acceptance

- RW-205 live frontend transaction flow complete for list, quick add, edit, soft-delete and restore.
- RW-206 cross-user authorization integration tests complete for get, update, delete, restore and list isolation.
- Frontend API client and GitHub Pages demo adapter now support the full transaction lifecycle.
- `dotnet test tests/RandWise.IntegrationTests/RandWise.IntegrationTests.csproj` passed with 15 tests.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet test RandWise.sln` passed with 34 tests.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 7 tests.
- `npm run build` passed.
- `VITE_DEMO_MODE=true npm run build` passed.
- Live browser smoke check passed against local API and Vite for register -> transactions -> add -> edit -> soft-delete -> restore.
- `npm run test:e2e` is not available yet.

### RW-307 Wave 3 budgeting acceptance

- RW-301 budget periods and category budgets complete.
- RW-302 recurring commitments complete.
- RW-303 budget frontend complete.
- RW-304 category management frontend complete.
- RW-305 safe-to-spend service complete.
- RW-306 budgeting integration tests complete for lifecycle, safe-to-spend calculation and cross-user isolation.
- Intentional API contract addition: `GET /api/v1/dashboard/safe-to-spend`.
- Frontend `/budget` now supports category creation, period creation, grouped category budgets, recurring commitment creation/pause, and local weekly/monthly saving targets.
- `dotnet test RandWise.sln` passed with 36 tests.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 8 tests.
- `npm run build` passed.
- `VITE_DEMO_MODE=true npm run build` passed.
- `npm run test:e2e` is not available yet.

### Wave 4 dashboard checkpoint

- RW-401 dashboard composite API complete for `/api/v1/dashboard`.
- RW-402 Money Pulse and spending pace complete as MVP backend calculations.
- RW-403 cash-flow forecast complete as a 14-day projected balance read model.
- RW-404 recommended action engine complete as deterministic MVP priority logic.
- RW-405 dashboard frontend structure expanded beyond fixture-only layout.
- RW-406 dashboard live API connection complete with demo fallback.
- App shell width cap removed so desktop uses the full available browser width.
- Browser verification passed for `1920x1080` desktop and `390x844` dark-mode mobile with no horizontal overflow.
- Remaining Wave 4 work: RW-407 calculation breakdown/privacy-specific dashboard treatment, RW-408 accessibility/responsive test expansion and RW-409 acceptance gate.
- `dotnet test RandWise.sln` passed with 36 tests.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 8 tests.
- `npm run build` passed.
- `VITE_DEMO_MODE=true npm run build` passed.

### RW-409 Wave 4 dashboard acceptance

- RW-407 privacy mode and calculation breakdown complete.
- RW-408 dashboard responsive/accessibility test expansion complete.
- Intentional API contract addition: dashboard `safeToSpend` now includes `savingsCommitmentInCents`, `upcomingCommitmentsInCents` and `remainingCategoryBudgetInCents` so the frontend can explain the calculation without inventing client-side logic.
- Dashboard recent-transaction merchant details are hidden when privacy mode is enabled.
- Browser verification passed for desktop `1280x720` light and dark theme transitions with no horizontal overflow.
- Browser verification passed for mobile `390x844` with bottom navigation, open calculation breakdown, privacy redaction and no horizontal overflow.
- In-app browser console verification reported no warnings or errors.
- `dotnet format RandWise.sln --verify-no-changes` passed.
- `dotnet test RandWise.sln` passed with 36 tests.
- `npm run lint` passed.
- `npm run typecheck` passed.
- `npm run test` passed with 9 tests.
- `npm run build` passed.
- `VITE_DEMO_MODE=true npm run build` passed.
- `npm run test:e2e` is not available yet.

## Open decisions

- final product name and visual identity;
- hosting platform;
- AI provider for fallback classification;
- WhatsApp Business account configuration;
- exact retention periods;
- whether refresh tokens are stored hashed in the database;
- first beta cohort size.

# AGENTS.md — RandWise Engineering Instructions

## Mission

Build **RandWise**, a secure, free-first, WhatsApp-first South African budgeting web application.

A user must be able to:

1. Register and complete financial onboarding.
2. Create budgets and recurring commitments.
3. Add income or expenses through the web application.
4. Send a WhatsApp message such as `R250 petrol` and have it recorded.
5. See a highly useful dashboard showing safe-to-spend, payday progress, spending pace, upcoming commitments and recommendations.
6. Correct categories and have the system learn user-specific rules.
7. Export or delete personal data.

## Required stack

### Backend
- .NET 10 LTS
- ASP.NET Core Web API
- C#
- Entity Framework Core
- SQLite for MVP
- ASP.NET Core Identity
- JWT access tokens and rotating refresh tokens
- OpenAPI
- FluentValidation
- Serilog
- xUnit

### Frontend
- React 19+
- TypeScript
- Vite
- Tailwind CSS 4
- React Router
- TanStack Query
- React Hook Form
- Zod
- Zustand only for small client state
- Recharts
- Radix UI primitives or shadcn-style local components
- Vitest, React Testing Library and Playwright

## Architecture constraints

Use a modular monolith with these projects:

```text
src/
  RandWise.Api/
  RandWise.Application/
  RandWise.Domain/
  RandWise.Infrastructure/
  RandWise.Contracts/
frontend/randwise-web/
tests/
  RandWise.UnitTests/
  RandWise.IntegrationTests/
  RandWise.ArchitectureTests/
```

Dependency direction:

```text
Api -> Application -> Domain
Infrastructure -> Application and Domain
Contracts are transport-only DTOs
Domain must not reference ASP.NET Core, EF Core, WhatsApp or an AI provider
```

Do not introduce microservices, Kubernetes, RabbitMQ, Redis, GraphQL, event sourcing or direct banking integrations during the MVP.

## Financial correctness rules

1. Store all monetary values as signed 64-bit integer cents.
2. Never use `float` or `double` for money.
3. Perform authoritative calculations on the backend.
4. Store timestamps in UTC.
5. Use `DateOnly` for transaction and budget dates where time is irrelevant.
6. Safe-to-spend must reserve upcoming commitments, savings commitments and the configured safety buffer.
7. Every user-owned query must filter by authenticated `UserId`.

## SQLite rules

- Use EF Core migrations.
- Avoid provider-specific SQL in Domain or Application.
- Configure WAL mode where appropriate.
- Keep transactions short.
- Ensure the design can migrate to PostgreSQL later.
- Use unique constraints for idempotency and domain invariants.

## Security rules

- HTTPS only in production.
- Strict CORS allowlist.
- Rate limit authentication, AI and webhook endpoints.
- Validate WhatsApp webhook signatures.
- Enforce webhook idempotency using the platform message ID.
- Never log passwords, tokens, full phone numbers, raw sensitive messages or complete webhook payloads.
- Encrypt sensitive message text and phone numbers at application level.
- Keep secrets outside source control.
- Use refresh-token rotation and revocation.
- Soft-delete transactions; account deletion must remove or irreversibly anonymise personal data.
- Add audit events for authentication, phone linking, exports, deletion and administrative access.

## UI and UX rules

The dashboard is a financial control centre, not an accounting screen.

Above the fold, answer:

1. How much can I safely spend?
2. How many days remain until payday?
3. Am I on track?
4. What requires attention?

Mobile-first requirements:
- Minimum 44px touch targets.
- Skeletons instead of blocking spinners.
- Text equivalents for charts.
- Visible keyboard focus.
- `prefers-reduced-motion` support.
- Privacy mode that hides monetary values.
- One primary recommendation at a time.
- Do not use colour as the only status indicator.

## Coding standards

- Nullable reference types enabled.
- Treat warnings as errors in CI.
- Prefer small cohesive classes.
- Use cancellation tokens on asynchronous I/O.
- Use Problem Details for API errors.
- Validate at API boundaries and enforce invariants in Domain.
- Add tests for every business rule.
- Do not silently catch exceptions.
- Do not add packages without documenting why.
- Keep public contracts backward-compatible within `/api/v1`.

## Git and multi-agent rules

Codex agents may work in parallel using separate worktrees or branches.

Branch naming:
- `agent/foundation-*`
- `agent/backend-*`
- `agent/frontend-*`
- `agent/integration-*`
- `agent/qa-*`
- `agent/security-*`

Each agent must:
1. Read this file and the relevant files under `docs/`.
2. Work only inside its assigned ownership boundaries.
3. Add or update tests.
4. Run formatting, build and relevant tests.
5. Update `docs/IMPLEMENTATION_STATUS.md`.
6. Create a concise handoff containing changed files, decisions, risks and next dependencies.
7. Avoid editing another active agent's owned files unless explicitly assigned.

Shared high-conflict files are owned by the **Integration Agent**:
- solution files
- root package manifests
- central dependency files
- CI workflows
- shared OpenAPI snapshot
- root configuration templates
- `docs/IMPLEMENTATION_STATUS.md`

## Definition of done

A task is done only when:
- implementation is complete;
- tests pass;
- validation and error states are covered;
- security implications are addressed;
- API contracts are documented;
- mobile and accessibility behaviour is checked where applicable;
- no secrets or generated build artefacts are committed;
- the handoff is written.

## Starting instruction for the orchestrator

Read:

1. `docs/MASTER_SPEC.md`
2. `docs/AGENT_PLAN.md`
3. `docs/TASK_GRAPH.md`
4. `docs/API_CONTRACT.md`
5. `docs/DATABASE_AND_UML.md`
6. `docs/DASHBOARD_UX.md`

Then execute work in dependency waves. Parallelise only tasks within the same wave whose ownership paths do not overlap. The Integration Agent merges and verifies after every wave.

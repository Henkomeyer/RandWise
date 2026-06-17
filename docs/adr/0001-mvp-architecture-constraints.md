# ADR 0001: MVP Architecture Constraints

## Status

Accepted

## Context

RandWise is an MVP for a secure, free-first, WhatsApp-first South African budgeting web application. The current canonical planning documents define the product scope, stack, public API outline, database model, dashboard UX requirements, and multi-agent implementation plan.

This record captures those existing constraints as implementation guidance for future work. It does not add or change canonical API endpoints, DTOs, database tables, or domain rules.

## Decision

The MVP will be built as a .NET and React modular monolith using the project layout and dependency direction defined in `AGENTS.md`:

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

Dependency direction remains:

```text
Api -> Application -> Domain
Infrastructure -> Application and Domain
Contracts are transport-only DTOs
Domain must not reference ASP.NET Core, EF Core, WhatsApp or an AI provider
```

The MVP keeps these architecture boundaries:

- Use ASP.NET Core Web API, C#, Entity Framework Core, SQLite, ASP.NET Core Identity, JWT access tokens, rotating refresh tokens, OpenAPI, FluentValidation, Serilog, and xUnit on the backend.
- Use React, TypeScript, Vite, Tailwind CSS, React Router, TanStack Query, React Hook Form, Zod, Recharts, accessible UI primitives, Vitest, React Testing Library, and Playwright on the frontend.
- Keep public transport contracts under `/api/v1` and preserve backward compatibility within that version.
- Store money as signed 64-bit integer cents; never use `float` or `double` for money.
- Run authoritative financial calculations on the backend.
- Store timestamps in UTC and use `DateOnly` for transaction and budget dates where time is irrelevant.
- Filter every user-owned query by authenticated `UserId`.
- Use EF Core migrations for SQLite, with a design that can migrate to PostgreSQL later.
- Verify and deduplicate WhatsApp webhooks before processing.
- Use the same application transaction command for web quick-add and WhatsApp capture.
- Prefer deterministic parsing and user/system rules before optional AI classification.
- Keep AI provider integration behind an interface.
- Keep the dashboard centered on safe-to-spend, payday progress, spending pace, commitments, and one primary recommendation.
- Preserve mobile-first accessibility requirements including 44px touch targets, visible focus, text equivalents for charts, reduced-motion support, and privacy mode.

The MVP explicitly excludes microservices, Kubernetes, RabbitMQ, Redis, GraphQL, event sourcing, and direct banking integrations.

## Consequences

This decision keeps the MVP implementation compact and easier to integrate across parallel agents. It also makes architecture tests and code review easier because dependency direction and ownership boundaries are explicit.

The tradeoff is that future scale-out mechanisms, direct banking integrations, and advanced asynchronous infrastructure remain out of scope until a later ADR changes the architecture. Agents should not introduce those technologies indirectly through feature work.

Any future decision that changes API shape, schema, authentication flow, persistence technology, AI provider behavior, or deployment architecture should get its own ADR and must also update the relevant canonical contract documents through the appropriate owner.

## References

- [AGENTS.md](../../AGENTS.md)
- [MASTER_SPEC.md](../MASTER_SPEC.md)
- [API_CONTRACT.md](../API_CONTRACT.md)
- [DATABASE_AND_UML.md](../DATABASE_AND_UML.md)
- [DASHBOARD_UX.md](../DASHBOARD_UX.md)
- [AGENT_PLAN.md](../AGENT_PLAN.md)
- [TASK_GRAPH.md](../TASK_GRAPH.md)

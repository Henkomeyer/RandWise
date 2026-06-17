# Multi-Agent Execution Plan

## Orchestrator responsibilities

The Orchestrator Agent owns planning, sequencing and final acceptance. It must not implement large features unless resolving integration conflicts.

Responsibilities:
- create waves based on `TASK_GRAPH.md`;
- assign one bounded task per subagent;
- make ownership paths explicit;
- verify prerequisites before dispatch;
- require handoff notes;
- dispatch Integration Agent after every wave;
- stop and resolve contract conflicts before further parallel work;
- maintain the canonical implementation status.

## Agent roster

### A. Foundation Agent
Owns:
- repository skeleton;
- development scripts;
- baseline .NET and React setup;
- local environment template;
- formatting and lint rules.

Does not own CI after initial handoff.

### B. Domain Agent
Owns:
- `RandWise.Domain`;
- money value object or integer-cent policies;
- entities, enums and domain rules;
- safe-to-spend and budget-period logic;
- domain unit tests.

### C. Persistence Agent
Owns:
- EF Core DbContext;
- entity configurations;
- SQLite migrations;
- seed data;
- repositories where genuinely required;
- database integration tests.

### D. Identity and Security Agent
Owns:
- Identity integration;
- JWT and refresh tokens;
- authorization;
- rate-limit policy definitions;
- encryption abstractions;
- audit-event foundations;
- security tests.

### E. Transaction API Agent
Owns:
- application use cases for transactions;
- transaction API endpoints;
- validation;
- filtering and pagination;
- transaction integration tests.

### F. Budgeting Agent
Owns:
- financial profile;
- budget periods;
- category budgets;
- recurring commitments;
- safe-to-spend application service;
- related endpoints and tests.

### G. Dashboard Backend Agent
Owns:
- dashboard query composition;
- Money Pulse;
- spending pace;
- cash-flow forecast;
- recommended action selection;
- dashboard contract and tests.

### H. Frontend Platform Agent
Owns:
- app shell;
- routing;
- API client;
- auth state integration;
- design tokens;
- reusable accessible primitives;
- responsive navigation.

### I. Dashboard Frontend Agent
Owns:
- dashboard pages and widgets;
- safe-to-spend hero;
- payday timeline;
- category progress;
- upcoming commitments;
- recent transactions;
- privacy mode;
- responsive and accessibility tests.

### J. Transactions Frontend Agent
Owns:
- transaction listing;
- filters;
- quick add;
- edit/category correction;
- optimistic updates;
- form tests.

### K. WhatsApp Agent
Owns:
- webhook verification;
- deduplication;
- contact linking;
- outbound client abstraction;
- message persistence;
- webhook integration tests.

### L. Parsing Agent
Owns:
- deterministic parser;
- intent classification;
- merchant and personal rules;
- confidence thresholds;
- AI-provider interface and optional adapter;
- parser unit tests.

### M. Background Jobs Agent
Owns:
- database-backed job processing;
- recurring transaction generation;
- notification retries;
- weekly-summary scheduling;
- worker tests.

### N. Reporting and Privacy Agent
Owns:
- weekly/monthly summaries;
- CSV export;
- privacy/data export;
- account deletion workflow;
- audit events for these flows.

### O. QA and Accessibility Agent
Owns:
- end-to-end test suite;
- accessibility audit;
- mobile checks;
- regression checklist;
- performance smoke tests.

### P. Integration Agent
Sole owner of high-conflict shared files. Merges, resolves conflicts, runs full validation, updates status and publishes the next-wave readiness report.

## Dispatch template

Every subagent task must contain:

```text
ROLE:
TASK ID:
GOAL:
PREREQUISITES:
OWNED PATHS:
READ-ONLY PATHS:
DO NOT TOUCH:
REQUIRED OUTPUTS:
TESTS:
ACCEPTANCE CRITERIA:
HANDOFF FORMAT:
```

## Required handoff format

```markdown
## Completed
## Files changed
## API or schema decisions
## Tests run and results
## Known limitations
## Risks
## Required follow-up
```

## Parallel-safety rules

- Never assign two agents to the same migration at once.
- Database schema changes go through Persistence Agent.
- DTO or OpenAPI changes must be announced to affected frontend agents.
- Frontend agents may use fixtures until Integration Agent confirms live API compatibility.
- Shared design primitives belong to Frontend Platform Agent.
- Dashboard Frontend Agent must not redefine shared primitives locally.
- AI integration must remain behind an interface.
- WhatsApp Agent must not implement budget calculations.
- The same application command used by the web must be used by WhatsApp capture.

## Merge gates

After each wave, Integration Agent runs:

```bash
dotnet format --verify-no-changes
dotnet build
dotnet test
npm ci
npm run lint
npm run typecheck
npm run test
npm run build
```

When Playwright is available:

```bash
npm run test:e2e
```

# Dependency-Aware Task Graph

## Wave 0 — Repository bootstrap

Parallel:
- RW-001 Foundation backend solution
- RW-002 Foundation frontend application
- RW-003 Documentation and ADR skeleton

Merge gate:
- RW-004 Integration baseline

## Wave 1 — Domain and platform foundation

Parallel:
- RW-101 Core domain entities and enums
- RW-102 Authentication and token foundation
- RW-103 Frontend design system and app shell
- RW-104 CI pipeline and quality gates

Then:
- RW-105 EF Core model and initial SQLite migration

Merge gate:
- RW-106 Full foundation integration

## Wave 2 — First vertical slice

Parallel after RW-105:
- RW-201 Transaction application use cases and API
- RW-202 Financial profile and onboarding API
- RW-203 Frontend authentication and onboarding
- RW-204 Transaction frontend list and quick-add shell

Then:
- RW-205 Connect live frontend transaction flow
- RW-206 Cross-user authorization integration tests

Merge gate:
- RW-207 Vertical-slice acceptance

Definition:
Register -> sign in -> add expense -> list -> edit -> soft-delete -> restore.

## Wave 3 — Budgeting

Parallel:
- RW-301 Budget periods and category budgets
- RW-302 Recurring commitments
- RW-303 Budget frontend
- RW-304 Category management frontend

Then:
- RW-305 Safe-to-spend service
- RW-306 Safe-to-spend integration tests

Merge gate:
- RW-307 Budgeting acceptance

## Wave 4 — Dashboard

Parallel after safe-to-spend contract:
- RW-401 Dashboard composite API
- RW-402 Money Pulse and spending pace
- RW-403 Cash-flow forecast
- RW-404 Recommended action engine
- RW-405 Dashboard frontend structure and fixtures

Then:
- RW-406 Connect dashboard to live API
- RW-407 Privacy mode and calculation breakdown
- RW-408 Dashboard responsive/accessibility tests

Merge gate:
- RW-409 Dashboard acceptance

## Wave 5 — WhatsApp capture

Parallel:
- RW-501 WhatsApp contact linking
- RW-502 Webhook verification and idempotent ingestion
- RW-503 Deterministic message parser
- RW-504 Outbound notification abstraction

Then:
- RW-505 Process incoming message through existing transaction command
- RW-506 Confirmation modes
- RW-507 Webhook/parser integration tests

Merge gate:
- RW-508 WhatsApp acceptance

## Wave 6 — Intelligence and jobs

Parallel:
- RW-601 Personal category rules
- RW-602 System merchant rules
- RW-603 Confidence and needs-review flow
- RW-604 AI classifier interface and optional provider adapter
- RW-605 Database-backed background worker
- RW-606 Recurring transaction generation

Then:
- RW-607 Category correction learning
- RW-608 Retry and dead-letter behaviour

Merge gate:
- RW-609 Intelligence acceptance

## Wave 7 — Reports and privacy

Parallel:
- RW-701 Weekly financial story
- RW-702 Monthly Money Wrap
- RW-703 CSV export
- RW-704 Data export and account deletion
- RW-705 Audit coverage

Then:
- RW-706 Frontend reports and privacy controls

Merge gate:
- RW-707 Privacy and reporting acceptance

## Wave 8 — Beta hardening

Parallel:
- RW-801 End-to-end critical journeys
- RW-802 Accessibility audit
- RW-803 Security review
- RW-804 Load and SQLite concurrency smoke tests
- RW-805 Logging and observability review
- RW-806 Deployment packaging

Then:
- RW-807 Fix findings
- RW-808 Release-candidate acceptance

## Critical path

```text
RW-001
 -> RW-105
 -> RW-201
 -> RW-301
 -> RW-305
 -> RW-401
 -> RW-406
 -> RW-502
 -> RW-505
 -> RW-507
 -> RW-808
```

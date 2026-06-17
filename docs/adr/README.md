# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for RandWise implementation decisions.

ADRs are lightweight notes for decisions that affect architecture, cross-agent contracts, dependencies, security posture, data ownership, or long-term maintainability. They support handoffs and future implementation work; canonical API and schema contracts remain in:

- [API_CONTRACT.md](../API_CONTRACT.md)
- [DATABASE_AND_UML.md](../DATABASE_AND_UML.md)
- [MASTER_SPEC.md](../MASTER_SPEC.md)
- [AGENTS.md](../../AGENTS.md)

## Naming

Use a zero-padded sequence and short kebab-case title:

```text
0001-mvp-architecture-constraints.md
0002-example-decision-title.md
```

## Status

Use one of:

- `Proposed`
- `Accepted`
- `Superseded`

If an ADR is superseded, keep the old record and link to the replacement.

## Template

```markdown
# ADR NNNN: Decision Title

## Status

Proposed | Accepted | Superseded by ADR NNNN

## Context

What constraints, requirements, or tradeoffs forced this decision?

## Decision

What is the decision?

## Consequences

What becomes easier, harder, or explicitly out of scope?

## References

- `Relevant source path`
```

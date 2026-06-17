# RandWise Master Product and Technical Specification

## 1. Product summary

RandWise is a free-first, WhatsApp-first South African budgeting application. Users record spending with natural messages, while a React web dashboard presents an understandable financial picture.

Primary promise:

> Know what you can safely spend before payday—just WhatsApp your spending.

## 2. MVP outcomes

The MVP must allow a user to:

- register, sign in, refresh a session and sign out;
- set monthly income, payday, safety buffer and budget cycle;
- add, edit, categorise, soft-delete and restore transactions;
- define category budgets and recurring commitments;
- view an actionable dashboard;
- link a WhatsApp number;
- send a simple amount-and-description message;
- have deterministic parsing create a transaction;
- receive a short confirmation according to notification mode;
- correct a category and optionally create a personal rule;
- export transactions;
- request account deletion.

Not in MVP:

- direct bank account integration;
- payments or loans;
- investment recommendations;
- regulated personalised financial advice;
- complex household collaboration;
- receipt OCR;
- voice transcription;
- stokvel administration;
- tax filing.

## 3. Personas

### Salary earner
Needs to know what remains after debit orders and how to survive until payday.

### Student
Tracks allowance, food, rent, transport and side income.

### Side hustler
Separates irregular income and related expenses from ordinary spending.

## 4. Core domain language

- **Budget period:** the user's active financial cycle, often payday-to-payday.
- **Available cash:** opening balance plus received income minus confirmed expenses.
- **Protected money:** upcoming commitments plus savings commitments plus safety buffer.
- **Safe-to-spend:** available cash minus protected money.
- **Spending pace:** percentage of available budget used compared with percentage of the period elapsed.
- **Money Pulse:** transparent internal budgeting-health indicator, not a credit score.
- **Incoming message:** WhatsApp event captured before interpretation.
- **Interpretation:** structured meaning extracted from a message.
- **Category rule:** merchant or keyword mapping learned from user corrections.

## 5. Safe-to-spend

```text
AvailableCash =
  OpeningBalance
  + IncomeReceived
  - ConfirmedExpenses

ProtectedMoney =
  RemainingUpcomingCommitments
  + SavingsCommitments
  + SafetyBuffer

SafeToSpend = AvailableCash - ProtectedMoney

SafeDailySpend =
  max(0, SafeToSpend) / max(1, DaysRemaining)
```

The API must also return the calculation breakdown.

## 6. Message processing

Layered pipeline:

1. Verify webhook and deduplicate.
2. Resolve WhatsApp contact to user.
3. Parse deterministic formats.
4. Apply personal category rules.
5. Apply system merchant/keyword rules.
6. Use an AI classifier only when needed.
7. Auto-confirm high-confidence results.
8. Mark medium-confidence results for review.
9. Ask the user when confidence is low.
10. Save interpretation, transaction and notification.

Initial supported examples:

```text
R250 petrol
250 petrol
spent R250 on petrol
+850 dog sitting
income 1200 tutoring
undo last
change last to groceries
how much left
petrol left
show last 5
help
```

## 7. Suggested confidence policy

- >= 9000 basis points: confirmed
- 7000–8999: create as needs-review
- < 7000: request clarification

## 8. Notification modes

- Silent
- Confirm
- Coach

## 9. Dashboard outcome

Within seconds the user must understand:

- safe-to-spend total and daily amount;
- days until payday;
- financial status;
- money used versus time elapsed;
- upcoming commitments;
- categories at risk;
- one recommended action;
- recent transactions;
- basic cash-flow forecast.

## 10. Rollout

### Phase 1
Core web vertical slice.

### Phase 2
Budgeting, recurring commitments and dashboard.

### Phase 3
WhatsApp deterministic capture.

### Phase 4
Rules, confidence and AI fallback.

### Phase 5
Reports, privacy controls, export and deletion.

### Phase 6
Beta hardening and measured public launch.

## 11. Success criteria

Product:
- user logs at least three transactions weekly;
- safe-to-spend is understandable without support;
- transaction classification reaches >= 80% correct during beta;
- the web app remains usable on a 320px viewport;
- no cross-user data exposure.

Technical:
- all builds and tests pass in CI;
- critical business rules have unit tests;
- key API flows have SQLite integration tests;
- duplicate webhooks never create duplicate transactions;
- dashboard initial API response is generated efficiently;
- no sensitive data appears in logs.

# Dashboard UX Specification

## Dashboard principle

The dashboard is the user's financial control centre. It must be useful before it is decorative.

Above the fold answer:

1. How much can I safely spend?
2. How many days until payday?
3. Am I on track?
4. What requires my attention?

## Desktop layout

```text
┌──────────┬───────────────────────────────────────────────────────────────┐
│ Sidebar  │ Greeting                    Privacy        Notifications      │
│          ├───────────────────────────────────────────────────────────────┤
│          │ Safe to spend      Money Pulse       Payday                  │
│          │ R2,840             74 / 100          10 days                 │
│          ├───────────────────────────────────────────────────────────────┤
│          │ Recommended action                                           │
│          ├──────────────────────────────┬────────────────────────────────┤
│          │ Cash-flow forecast           │ Upcoming commitments           │
│          ├──────────────────────────────┼────────────────────────────────┤
│          │ Category progress            │ Where money went               │
│          ├──────────────────────────────┼────────────────────────────────┤
│          │ Recent transactions          │ Insights                       │
└──────────┴──────────────────────────────┴────────────────────────────────┘
```

## Mobile order

1. Header and privacy toggle
2. Safe-to-spend hero
3. Payday timeline
4. Primary recommendation
5. Quick actions
6. Category warnings
7. Upcoming commitments
8. Recent transactions
9. Weekly story
10. Spending chart
11. Insights

Bottom navigation:
- Dashboard
- Transactions
- Add
- Budget
- More

## Hero card

Display:
- safe-to-spend total;
- daily amount;
- payday date;
- status;
- button to open calculation breakdown.

Example:

```text
Safe to spend
R2,840
About R284 per day for 10 days
[How this is calculated]
```

## Payday timeline

Compare elapsed time with money used:

```text
67% of this budget period has passed
58% of spendable money has been used
You are spending slightly slower than planned
```

## Money Pulse

Transparent budgeting indicator, never called a credit score.

Suggested weights:
- safe-to-spend position: 25%
- spending pace: 20%
- commitment coverage: 20%
- category overruns: 15%
- savings contribution: 10%
- debt payment status: 10%

Ranges:
- 85–100 Comfortable
- 70–84 On track
- 50–69 Watch spending
- 30–49 Budget pressure
- 0–29 Immediate attention

Always show contributing reasons.

## Recommended action

Display only one primary action.

Priority:
1. projected negative balance;
2. uncovered fixed expense;
3. negative safe-to-spend;
4. severe category overrun;
5. spending pace too high;
6. large upcoming commitment;
7. missing buffer;
8. savings opportunity;
9. positive reinforcement.

## Category cards

Show:
- name and icon;
- allocated, spent and remaining;
- percentage;
- spending pace;
- status label;
- latest transaction.

Categories must support sorting by risk, spend, remaining amount and custom order.

## Upcoming commitments

Show commitments before payday:
- due date;
- description;
- amount;
- protected status;
- paid status;
- mark-paid and edit actions.

## Cash-flow forecast

Show projected daily balance, upcoming commitments, payday and safety-buffer line.

Text equivalent:
- projected lowest balance;
- date of lowest balance;
- whether it remains above the safety buffer;
- projected shortfall warning.

## Recent transactions

Show merchant/description, category, date, amount, source and review status.

Actions:
- edit;
- change category;
- add note;
- delete;
- mark recurring;
- create category rule.

## Insights

Maximum three at once. Every insight must provide:
- what happened;
- why it matters;
- suggested action;
- link to evidence.

## Privacy mode

Hide amounts and selected merchant details while retaining general status and percentages.

## Quick add

The quick-add sheet accepts either structured fields or natural input:

```text
R450 Checkers yesterday
```

It must use the same parser/application command as WhatsApp.

## Empty state

Guide users through:
1. income;
2. payday;
3. recurring commitments;
4. first transaction.

Never show meaningless empty charts.

## Accessibility

- WCAG 2.2 AA target.
- 44px touch targets.
- keyboard navigation.
- visible focus.
- reduced-motion support.
- screen-reader announcements for updated totals.
- charts have textual tables/summaries.
- status is not conveyed by colour alone.

## Performance

- shell visible quickly;
- composite dashboard endpoint for first viewport;
- secondary chart details lazy-loaded;
- optimistic transaction mutations;
- invalidate dashboard cache after any financial mutation.

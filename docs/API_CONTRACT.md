# API Contract

Base path: `/api/v1`

All errors use RFC 9457-style Problem Details.

## Authentication
- POST `/auth/register`
- POST `/auth/login`
- POST `/auth/refresh`
- POST `/auth/logout`
- GET `/auth/me`
- POST `/auth/request-password-reset`
- POST `/auth/reset-password`

## Profile
- GET `/profile`
- PUT `/profile`
- DELETE `/profile`
- GET `/profile/export`

## Financial profile
- GET `/financial-profile`
- PUT `/financial-profile`

## Budget periods
- GET `/budget-periods`
- GET `/budget-periods/current`
- GET `/budget-periods/{id}`
- POST `/budget-periods`
- PUT `/budget-periods/{id}`
- POST `/budget-periods/{id}/close`

## Categories
- GET `/categories`
- POST `/categories`
- PUT `/categories/{id}`
- DELETE `/categories/{id}`

## Category budgets
- GET `/budget-periods/{periodId}/category-budgets`
- POST `/budget-periods/{periodId}/category-budgets`
- PUT `/category-budgets/{id}`
- DELETE `/category-budgets/{id}`

## Transactions
- GET `/transactions`
- GET `/transactions/{id}`
- POST `/transactions`
- PUT `/transactions/{id}`
- DELETE `/transactions/{id}`
- POST `/transactions/{id}/restore`
- POST `/transactions/{id}/categorise`

Filters:
- `from`
- `to`
- `categoryId`
- `type`
- `source`
- `search`
- `page`
- `pageSize`

## Recurring transactions
- GET `/recurring-transactions`
- POST `/recurring-transactions`
- PUT `/recurring-transactions/{id}`
- DELETE `/recurring-transactions/{id}`
- POST `/recurring-transactions/{id}/pause`

## Dashboard
- GET `/dashboard`
- GET `/dashboard/safe-to-spend`
- GET `/dashboard/cash-flow`
- GET `/dashboard/spending-trend`
- GET `/dashboard/category-progress`
- GET `/dashboard/insights`
- GET `/dashboard/money-wrap`

## Reports
- GET `/reports/weekly`
- GET `/reports/monthly`
- GET `/reports/category-breakdown`
- GET `/reports/export/csv`

## WhatsApp
- GET `/webhooks/whatsapp`
- POST `/webhooks/whatsapp`
- GET `/whatsapp/status`
- POST `/whatsapp/link`
- POST `/whatsapp/unlink`

## Create transaction request

```json
{
  "amountInCents": 25000,
  "transactionType": "expense",
  "categoryId": "category-id",
  "description": "Petrol",
  "merchant": "Shell",
  "transactionDate": "2026-06-14",
  "source": "web"
}
```

## Dashboard initial response

```json
{
  "generatedUtc": "2026-06-14T11:30:00Z",
  "budgetPeriod": {
    "id": "period-id",
    "startDate": "2026-05-25",
    "endDate": "2026-06-24",
    "daysRemaining": 10,
    "periodProgressPercent": 67
  },
  "financialStatus": {
    "status": "onTrack",
    "message": "You are currently on track.",
    "moneyPulse": 74
  },
  "safeToSpend": {
    "amountInCents": 284000,
    "dailyAmountInCents": 28400,
    "availableCashInCents": 610000,
    "protectedAmountInCents": 326000,
    "safetyBufferInCents": 50000,
    "savingsCommitmentInCents": 220000,
    "upcomingCommitmentsInCents": 56000,
    "remainingCategoryBudgetInCents": 284000
  },
  "spending": {
    "spentThisPeriodInCents": 842000,
    "spendingPercent": 58,
    "expectedSpendingPercent": 67
  },
  "recommendedAction": {
    "type": "categoryWarning",
    "title": "Slow down on takeaways",
    "message": "Keep takeaway spending below R120 this week to remain on track."
  },
  "categories": [],
  "upcomingCommitments": [],
  "recentTransactions": [],
  "cashFlowForecast": [],
  "insights": []
}
```

## Contract rules

- Money values are integer cents.
- Dates are ISO `YYYY-MM-DD`.
- Timestamps are UTC ISO-8601.
- Use stable string enum values in public JSON.
- Pagination returns items, page, pageSize, totalCount and totalPages.
- User IDs are never accepted from normal client requests; derive them from the authenticated principal.

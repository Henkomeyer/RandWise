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

## Category rules
- GET `/category-rules`
- POST `/category-rules`
- DELETE `/category-rules/{id}`

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

### Link WhatsApp request

```json
{
  "phoneNumber": "+27825550101",
  "platformContactId": "whatsapp-contact-id"
}
```

### WhatsApp status response

```json
{
  "isLinked": true,
  "isVerified": true,
  "platformContactId": "whatsapp-contact-id",
  "verifiedUtc": "2026-06-17T10:00:00Z"
}
```

### WhatsApp webhook MVP request

`POST /webhooks/whatsapp` requires `X-Hub-Signature-256` when `WhatsApp:AppSecret` is configured.

```json
{
  "messageId": "wamid.123",
  "platformContactId": "whatsapp-contact-id",
  "fromPhoneNumber": "+27825550101",
  "messageType": "text",
  "text": "R250 petrol",
  "receivedUtc": "2026-06-17T10:00:00Z"
}
```

### WhatsApp webhook response

```json
{
  "messageId": "wamid.123",
  "accepted": true,
  "duplicate": false,
  "processingStatus": "Received"
}
```

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

## Categorise transaction request

`POST /transactions/{id}/categorise` updates the category and confirms a reviewed transaction. When `createRule` is true, the backend learns a personal rule from the correction.

```json
{
  "categoryId": "category-id",
  "createRule": true,
  "matchType": "keyword",
  "matchValue": "petrol"
}
```

## Category rule request

```json
{
  "matchType": "keyword",
  "matchValue": "petrol",
  "categoryId": "category-id",
  "priority": 100
}
```

## Category rule response

```json
{
  "id": "rule-id",
  "matchType": "keyword",
  "matchValue": "petrol",
  "categoryId": "category-id",
  "priority": 100,
  "isActive": true,
  "createdUtc": "2026-06-17T10:00:00Z",
  "updatedUtc": "2026-06-17T10:00:00Z"
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
- WhatsApp webhooks must verify signatures when a provider secret is configured and must be idempotent on provider message ID.
- WhatsApp capture must process parsed transactions through the transaction application service, not direct transaction table writes.
- Personal category rules and deterministic system rules must run before AI classification.
- Category corrections that create rules must be handled by the transaction application service.
- Incoming WhatsApp processing must retry transient processor failures and mark exhausted messages failed instead of looping forever.

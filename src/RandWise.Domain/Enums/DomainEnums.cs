namespace RandWise.Domain.Enums;

public enum AppUserStatus
{
    Active = 1,
    Suspended = 2,
    Deleted = 3
}

public enum BudgetCycleType
{
    CalendarMonth = 1,
    PaydayToPayday = 2
}

public enum NotificationMode
{
    Silent = 1,
    Confirm = 2,
    Coach = 3
}

public enum BudgetPeriodStatus
{
    Open = 1,
    Closed = 2
}

public enum BudgetCategoryType
{
    Expense = 1,
    Income = 2,
    Savings = 3
}

public enum TransactionType
{
    Expense = 1,
    Income = 2
}

public enum TransactionSource
{
    Web = 1,
    WhatsApp = 2,
    Recurring = 3,
    Import = 4
}

public enum TransactionStatus
{
    Confirmed = 1,
    NeedsReview = 2,
    Deleted = 3
}

public enum RecurrenceFrequency
{
    Weekly = 1,
    Monthly = 2
}

public enum MessageProcessingStatus
{
    Received = 1,
    Processed = 2,
    Failed = 3,
    IgnoredDuplicate = 4
}

public enum CategoryRuleMatchType
{
    Merchant = 1,
    Keyword = 2
}

public enum NotificationChannel
{
    WhatsApp = 1,
    Email = 2,
    InApp = 3
}

public enum NotificationType
{
    TransactionConfirmation = 1,
    ClarificationRequest = 2,
    BudgetWarning = 3,
    WeeklySummary = 4,
    AccountEvent = 5
}

public enum NotificationStatus
{
    Scheduled = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}

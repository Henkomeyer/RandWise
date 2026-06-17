using RandWise.Domain.Enums;

namespace RandWise.Application.Common;

public static class DomainEnumNames
{
    public static BudgetCycleType ParseBudgetCycleType(string value) =>
        Normalize(value) switch
        {
            "calendarmonth" => BudgetCycleType.CalendarMonth,
            "paydaytopayday" => BudgetCycleType.PaydayToPayday,
            _ => throw new ApplicationException(ApplicationError.Validation, "Budget cycle type is invalid.")
        };

    public static NotificationMode ParseNotificationMode(string value) =>
        Normalize(value) switch
        {
            "silent" => NotificationMode.Silent,
            "confirm" => NotificationMode.Confirm,
            "coach" => NotificationMode.Coach,
            _ => throw new ApplicationException(ApplicationError.Validation, "Notification mode is invalid.")
        };

    public static DayOfWeek ParseDayOfWeek(string value) =>
        Normalize(value) switch
        {
            "sunday" => DayOfWeek.Sunday,
            "monday" => DayOfWeek.Monday,
            "tuesday" => DayOfWeek.Tuesday,
            "wednesday" => DayOfWeek.Wednesday,
            "thursday" => DayOfWeek.Thursday,
            "friday" => DayOfWeek.Friday,
            "saturday" => DayOfWeek.Saturday,
            _ => throw new ApplicationException(ApplicationError.Validation, "First day of week is invalid.")
        };

    public static TransactionType ParseTransactionType(string value) =>
        Normalize(value) switch
        {
            "expense" => TransactionType.Expense,
            "income" => TransactionType.Income,
            _ => throw new ApplicationException(ApplicationError.Validation, "Transaction type is invalid.")
        };

    public static TransactionSource ParseTransactionSource(string value) =>
        Normalize(value) switch
        {
            "web" => TransactionSource.Web,
            "whatsapp" => TransactionSource.WhatsApp,
            "recurring" => TransactionSource.Recurring,
            "import" => TransactionSource.Import,
            _ => throw new ApplicationException(ApplicationError.Validation, "Transaction source is invalid.")
        };

    public static string ToContract(this BudgetCycleType value) =>
        value switch
        {
            BudgetCycleType.CalendarMonth => "calendarMonth",
            BudgetCycleType.PaydayToPayday => "paydayToPayday",
            _ => throw new ApplicationException(ApplicationError.Validation, "Budget cycle type is invalid.")
        };

    public static string ToContract(this NotificationMode value) =>
        value switch
        {
            NotificationMode.Silent => "silent",
            NotificationMode.Confirm => "confirm",
            NotificationMode.Coach => "coach",
            _ => throw new ApplicationException(ApplicationError.Validation, "Notification mode is invalid.")
        };

    public static string ToContract(this TransactionType value) =>
        value switch
        {
            TransactionType.Expense => "expense",
            TransactionType.Income => "income",
            _ => throw new ApplicationException(ApplicationError.Validation, "Transaction type is invalid.")
        };

    public static string ToContract(this TransactionSource value) =>
        value switch
        {
            TransactionSource.Web => "web",
            TransactionSource.WhatsApp => "whatsapp",
            TransactionSource.Recurring => "recurring",
            TransactionSource.Import => "import",
            _ => throw new ApplicationException(ApplicationError.Validation, "Transaction source is invalid.")
        };

    public static string ToContract(this TransactionStatus value) =>
        value switch
        {
            TransactionStatus.Confirmed => "confirmed",
            TransactionStatus.NeedsReview => "needsReview",
            TransactionStatus.Deleted => "deleted",
            _ => throw new ApplicationException(ApplicationError.Validation, "Transaction status is invalid.")
        };

    public static string ToContract(this DayOfWeek value) =>
        value switch
        {
            DayOfWeek.Sunday => "sunday",
            DayOfWeek.Monday => "monday",
            DayOfWeek.Tuesday => "tuesday",
            DayOfWeek.Wednesday => "wednesday",
            DayOfWeek.Thursday => "thursday",
            DayOfWeek.Friday => "friday",
            DayOfWeek.Saturday => "saturday",
            _ => throw new ApplicationException(ApplicationError.Validation, "First day of week is invalid.")
        };

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationException(ApplicationError.Validation, "Enum value is required.");
        }

        return value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}

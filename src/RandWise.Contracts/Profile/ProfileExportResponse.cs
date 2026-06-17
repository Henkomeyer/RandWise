namespace RandWise.Contracts.Profile;

public sealed record ProfileExportResponse(
    DateTime GeneratedUtc,
    ProfileExportUserResponse User,
    object? FinancialProfile,
    IReadOnlyList<object> Categories,
    IReadOnlyList<object> BudgetPeriods,
    IReadOnlyList<object> CategoryBudgets,
    IReadOnlyList<object> RecurringTransactions,
    IReadOnlyList<object> Transactions,
    IReadOnlyList<object> CategoryRules,
    IReadOnlyList<object> WhatsAppContacts);

public sealed record ProfileExportUserResponse(
    string Id,
    string DisplayName,
    string PreferredCurrency,
    string TimeZone,
    string PreferredLanguage,
    DateTime CreatedUtc);

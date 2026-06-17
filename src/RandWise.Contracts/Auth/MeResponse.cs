namespace RandWise.Contracts.Auth;

public sealed record MeResponse(
    string Email,
    string DisplayName,
    string PreferredCurrency,
    string TimeZone,
    string PreferredLanguage);

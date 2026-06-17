namespace RandWise.Application.Security;

public sealed record AccessTokenDescriptor(
    string UserId,
    string Email,
    string DisplayName,
    DateTimeOffset IssuedUtc,
    DateTimeOffset ExpiresUtc);

namespace RandWise.Application.Auth;

public sealed record AuthenticatedUserContext(
    string UserId,
    string Email,
    string DisplayName);

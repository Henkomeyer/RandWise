namespace RandWise.Application.Auth;

public enum AuthFailure
{
    InvalidCredentials,
    DuplicateEmail,
    InvalidRefreshToken,
    InvalidPrincipal,
    ValidationFailed
}

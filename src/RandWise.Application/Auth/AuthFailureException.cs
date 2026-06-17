namespace RandWise.Application.Auth;

public sealed class AuthFailureException : Exception
{
    public AuthFailureException(AuthFailure failure, string message)
        : base(message)
    {
        Failure = failure;
    }

    public AuthFailure Failure { get; }
}

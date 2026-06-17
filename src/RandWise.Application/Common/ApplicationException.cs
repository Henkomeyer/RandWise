namespace RandWise.Application.Common;

public sealed class ApplicationException : Exception
{
    public ApplicationException(ApplicationError error, string message)
        : base(message)
    {
        Error = error;
    }

    public ApplicationError Error { get; }
}

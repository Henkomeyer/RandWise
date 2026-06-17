namespace RandWise.Application.Common;

public interface IClock
{
    DateTime UtcNow { get; }
}

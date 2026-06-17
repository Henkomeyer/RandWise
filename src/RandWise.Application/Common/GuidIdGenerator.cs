namespace RandWise.Application.Common;

public sealed class GuidIdGenerator : IIdGenerator
{
    public string NewId() => Guid.NewGuid().ToString("N");
}

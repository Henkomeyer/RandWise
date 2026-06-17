namespace RandWise.Infrastructure.Security;

public sealed class SensitiveDataOptions
{
    public const string SectionName = "SensitiveData";

    public string Key { get; set; } = string.Empty;
}

namespace RandWise.Infrastructure.Persistence;

public sealed class RandWisePersistenceOptions
{
    public const string SectionName = "Persistence";

    public string ConnectionString { get; set; } = "Data Source=randwise.db";
}

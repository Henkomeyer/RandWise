using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RandWise.Infrastructure.Persistence;

public sealed class RandWiseDbContextFactory : IDesignTimeDbContextFactory<RandWiseDbContext>
{
    public RandWiseDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<RandWiseDbContext>();
        builder.UseSqlite("Data Source=randwise-design-time.db");

        return new RandWiseDbContext(builder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace XauAi.Infrastructure.Persistence;

public sealed class XauAiDbContextFactory : IDesignTimeDbContextFactory<XauAiDbContext>
{
    public XauAiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=XauAiDesignTime;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(XauAiDbContext).Assembly.FullName))
            .Options;

        return new XauAiDbContext(options);
    }
}

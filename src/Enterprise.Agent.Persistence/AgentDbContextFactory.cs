using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enterprise.Agent.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (`dotnet ef migrations add`, `database
/// update`). Reads the connection string from the EAA_DB_CONNECTION environment variable and
/// falls back to a local development default. A live database is not required to add migrations.
/// </summary>
public sealed class AgentDbContextFactory : IDesignTimeDbContextFactory<AgentDbContext>
{
    public AgentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("EAA_DB_CONNECTION")
            ?? "Server=localhost;Database=EnterpriseAgent;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AgentDbContext).Assembly.FullName))
            .Options;

        return new AgentDbContext(options);
    }
}

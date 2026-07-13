using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Infrastructure.Options;
using Enterprise.Agent.Infrastructure.Sql;
using Enterprise.Agent.Models.Sql;
using Enterprise.Agent.Prompts;
using Enterprise.Agent.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Enterprise.Agent.UnitTests.Sql;

public sealed class SqlAgentServiceTests
{
    private sealed class StubSchemaProvider : ISqlSchemaProvider
    {
        public Task<string> GetSchemaAsync(string? dataSource, CancellationToken cancellationToken = default) =>
            Task.FromResult("TABLE dbo.Customers (Id int, Name nvarchar)");
    }

    private sealed class StubExecutor : ISqlExecutor
    {
        public bool Executed { get; private set; }

        public Task<SqlQueryResult> ExecuteAsync(string sql, string? dataSource, int maxRows, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return Task.FromResult(new SqlQueryResult
            {
                Columns = ["Name"],
                Rows = [new object?[] { "Acme" }, new object?[] { "Globex" }]
            });
        }
    }

    [Fact]
    public async Task Run_GeneratesValidatesExecutesAndSummarises()
    {
        var responses = new Queue<string>(["SELECT Name FROM Customers", "There are two customers: Acme and Globex."]);
        var chat = new FakeChatProvider(_ => responses.Dequeue());
        var executor = new StubExecutor();

        var service = new SqlAgentService(
            new StubSchemaProvider(),
            new SqlSafetyValidator(),
            executor,
            chat,
            DependencyInjection.BuildRegistry(),
            Options.Create(new SqlAgentOptions()),
            NullLogger<SqlAgentService>.Instance);

        var result = await service.RunAsync(new SqlAgentRequest { Question = "How many customers?" });

        Assert.True(result.Executed);
        Assert.True(executor.Executed);
        Assert.Equal("SELECT Name FROM Customers", result.GeneratedSql);
        Assert.Contains("Acme", result.Summary);
    }

    [Fact]
    public async Task Run_BlocksDestructiveSql()
    {
        var chat = new FakeChatProvider("DROP TABLE Customers");
        var executor = new StubExecutor();

        var service = new SqlAgentService(
            new StubSchemaProvider(),
            new SqlSafetyValidator(),
            executor,
            chat,
            DependencyInjection.BuildRegistry(),
            Options.Create(new SqlAgentOptions()),
            NullLogger<SqlAgentService>.Instance);

        var result = await service.RunAsync(new SqlAgentRequest { Question = "delete everything" });

        Assert.False(result.Executed);
        Assert.False(executor.Executed);
        Assert.NotEmpty(result.ValidationViolations);
    }

    [Fact]
    public void CleanSql_StripsCodeFences()
    {
        var cleaned = SqlAgentService.CleanSql("```sql\nSELECT 1\n```");
        Assert.Equal("SELECT 1", cleaned);
    }
}

using System.Text;
using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Sql;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>
/// Natural-language SQL agent. Delegates to the (Semantic Kernel powered)
/// <see cref="ISqlAgentService"/> which performs generate → validate → execute → summarise,
/// blocking any destructive statement.
/// </summary>
public sealed class SqlAgent : IAgent
{
    private readonly ISqlAgentService _sqlAgent;
    private readonly ILogger<SqlAgent> _logger;

    public SqlAgent(ISqlAgentService sqlAgent, ILogger<SqlAgent> logger)
    {
        _sqlAgent = sqlAgent;
        _logger = logger;
    }

    public AgentDescriptor Descriptor { get; } = new()
    {
        Name = "SqlAgent",
        Role = AgentRole.Sql,
        Description = "Answers questions over a relational database by generating and running safe, read-only SQL.",
        Capabilities = AgentCapability.DatabaseQuery | AgentCapability.TextGeneration,
        Keywords = ["sql", "query", "database", "table", "rows", "select", "how many", "count of", "records"]
    };

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var dataSource = request.Parameters.TryGetValue("dataSource", out var ds) ? ds : null;
            var maxRows = request.Parameters.TryGetValue("maxRows", out var mr) && int.TryParse(mr, out var n) ? n : 100;

            var result = await _sqlAgent.RunAsync(
                new SqlAgentRequest { Question = request.Input, DataSource = dataSource, MaxRows = maxRows },
                cancellationToken).ConfigureAwait(false);

            if (result.ValidationViolations.Count > 0)
            {
                return AgentResponse.Fail(Descriptor.Name,
                    $"Generated SQL was rejected by the safety policy: {string.Join("; ", result.ValidationViolations)}");
            }

            var output = new StringBuilder();
            output.AppendLine(result.Summary);
            output.AppendLine();
            output.AppendLine("```sql");
            output.AppendLine(result.GeneratedSql);
            output.AppendLine("```");

            var metadata = new Dictionary<string, string>
            {
                ["executed"] = result.Executed.ToString(),
                ["rows"] = (result.Data?.RowCount ?? 0).ToString()
            };
            return AgentResponse.Ok(Descriptor.Name, output.ToString().TrimEnd(), metadata);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SqlAgent failed.");
            return AgentResponse.Fail(Descriptor.Name, ex.Message);
        }
    }
}

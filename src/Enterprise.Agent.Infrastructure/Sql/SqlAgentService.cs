using System.Text;
using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Infrastructure.Options;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Sql;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Infrastructure.Sql;

/// <summary>
/// Implements the NL→SQL pipeline: fetch schema → generate SQL (LLM) → validate (safety) →
/// execute (read-only) → summarise (LLM). Destructive statements never reach the database.
/// </summary>
public sealed class SqlAgentService : ISqlAgentService
{
    private readonly ISqlSchemaProvider _schemaProvider;
    private readonly ISqlSafetyValidator _validator;
    private readonly ISqlExecutor _executor;
    private readonly IChatCompletionProvider _chat;
    private readonly IPromptProvider _prompts;
    private readonly SqlAgentOptions _options;
    private readonly ILogger<SqlAgentService> _logger;

    public SqlAgentService(
        ISqlSchemaProvider schemaProvider,
        ISqlSafetyValidator validator,
        ISqlExecutor executor,
        IChatCompletionProvider chat,
        IPromptProvider prompts,
        IOptions<SqlAgentOptions> options,
        ILogger<SqlAgentService> logger)
    {
        _schemaProvider = schemaProvider;
        _validator = validator;
        _executor = executor;
        _chat = chat;
        _prompts = prompts;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SqlAgentResult> RunAsync(SqlAgentRequest request, CancellationToken cancellationToken = default)
    {
        var schema = await _schemaProvider.GetSchemaAsync(request.DataSource, cancellationToken).ConfigureAwait(false);

        var generatePrompt = _prompts.Render("sql-generate", new Dictionary<string, string>
        {
            ["schema"] = schema,
            ["question"] = request.Question
        });

        var generated = await _chat.CompleteAsync(
            [new ChatMessage { Role = MessageRole.User, Content = generatePrompt }],
            new ChatCompletionSettings { Temperature = 0.0 },
            cancellationToken).ConfigureAwait(false);

        var sql = CleanSql(generated);

        var validation = _validator.Validate(sql);
        if (!validation.IsAllowed)
        {
            _logger.LogWarning("Generated SQL rejected by safety policy: {Violations}",
                string.Join("; ", validation.Violations));
            return new SqlAgentResult
            {
                Question = request.Question,
                GeneratedSql = sql,
                Executed = false,
                ValidationViolations = validation.Violations,
                Summary = "The generated SQL was blocked by the read-only safety policy and was not executed."
            };
        }

        SqlQueryResult? data = null;
        var executed = false;
        try
        {
            data = await _executor.ExecuteAsync(sql, request.DataSource, request.MaxRows, cancellationToken)
                .ConfigureAwait(false);
            executed = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL execution failed.");
            return new SqlAgentResult
            {
                Question = request.Question,
                GeneratedSql = sql,
                Executed = false,
                Summary = $"The query is valid but could not be executed: {ex.Message}"
            };
        }

        var csv = ToCsv(data);
        var summarizePrompt = _prompts.Render("sql-summarize", new Dictionary<string, string>
        {
            ["question"] = request.Question,
            ["sql"] = sql,
            ["result"] = csv
        });

        var summary = await _chat.CompleteAsync(
            [new ChatMessage { Role = MessageRole.User, Content = summarizePrompt }],
            new ChatCompletionSettings { Temperature = 0.2 },
            cancellationToken).ConfigureAwait(false);

        return new SqlAgentResult
        {
            Question = request.Question,
            GeneratedSql = sql,
            Executed = executed,
            Data = data,
            Summary = summary
        };
    }

    public static string CleanSql(string raw)
    {
        var text = raw.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = text.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                text = text[(firstNewLine + 1)..];
            }

            var fence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
            {
                text = text[..fence];
            }
        }

        return text.Trim();
    }

    private static string ToCsv(SqlQueryResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", result.Columns));
        foreach (var row in result.Rows)
        {
            builder.AppendLine(string.Join(",", row.Select(v => Escape(v?.ToString() ?? string.Empty))));
        }

        return builder.ToString().TrimEnd();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}

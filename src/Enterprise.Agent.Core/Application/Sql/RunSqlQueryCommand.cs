using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Models.Sql;
using FluentValidation;
using MediatR;

namespace Enterprise.Agent.Core.Application.Sql;

/// <summary>Runs the natural-language-to-SQL pipeline for a question.</summary>
public sealed record RunSqlQueryCommand(SqlAgentRequest Request) : IRequest<SqlAgentResult>;

public sealed class RunSqlQueryCommandValidator : AbstractValidator<RunSqlQueryCommand>
{
    public RunSqlQueryCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Question).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.Request.MaxRows).InclusiveBetween(1, 10_000);
    }
}

public sealed class RunSqlQueryCommandHandler : IRequestHandler<RunSqlQueryCommand, SqlAgentResult>
{
    private readonly ISqlAgentService _sqlAgent;

    public RunSqlQueryCommandHandler(ISqlAgentService sqlAgent) => _sqlAgent = sqlAgent;

    public Task<SqlAgentResult> Handle(RunSqlQueryCommand command, CancellationToken cancellationToken) =>
        _sqlAgent.RunAsync(command.Request, cancellationToken);
}

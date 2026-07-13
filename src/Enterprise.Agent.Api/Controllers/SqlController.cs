using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Core.Application.Sql;
using Enterprise.Agent.Models.Sql;
using Enterprise.Agent.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Natural-language SQL: generate → validate → execute (read-only) → summarise.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.RequireAgentOperator)]
public sealed class SqlController : ControllerBase
{
    private readonly ISender _sender;

    public SqlController(ISender sender) => _sender = sender;

    /// <summary>Answers a natural-language question over the configured database.</summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(SqlAgentResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SqlAgentResult>> Query(
        [FromBody] SqlQuestionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RunSqlQueryCommand(new SqlAgentRequest
        {
            Question = request.Question,
            DataSource = request.DataSource,
            MaxRows = request.MaxRows
        }), cancellationToken);

        return Ok(result);
    }
}

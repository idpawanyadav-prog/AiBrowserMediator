using AiBrowserMediator.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AiBrowserMediator.API.Controllers;

[ApiController]
[Route("browser")]
public sealed class BrowserController(ICommandDispatcher dispatcher) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<CommandResult>> ExecuteAsync([FromBody] WorkflowStepDto step, CancellationToken cancellationToken)
        => Ok(await dispatcher.ExecuteAsync(step, cancellationToken));
}

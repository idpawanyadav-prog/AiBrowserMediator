using AiBrowserMediator.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AiBrowserMediator.API.Controllers;

[ApiController]
[Route("workflow")]
public sealed class WorkflowController(IWorkflowEngine workflowEngine) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<IReadOnlyList<CommandResult>>> ExecuteAsync([FromBody] WorkflowDocumentDto workflow, CancellationToken cancellationToken)
        => Ok(await workflowEngine.ExecuteAsync(workflow, cancellationToken));
}

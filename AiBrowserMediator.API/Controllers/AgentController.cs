using AiBrowserMediator.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AiBrowserMediator.API.Controllers;

[ApiController]
[Route("agent")]
public sealed class AgentController(
    ICommandDispatcher dispatcher,
    IBrowserSession browserSession,
    IPageDescriptionService pageDescriptionService) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<object>> ExecuteAsync([FromBody] WorkflowStepDto step, CancellationToken cancellationToken)
    {
        if (string.Equals(step.Action, "capturePageSource", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                success = true,
                action = step.Action,
                pageSource = await browserSession.GetPageSourceAsync(cancellationToken)
            });
        }

        if (string.Equals(step.Action, "describePage", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                success = true,
                action = step.Action,
                description = await pageDescriptionService.DescribeAsync(cancellationToken)
            });
        }

        var result = await dispatcher.ExecuteAsync(step, cancellationToken);
        return Ok(new
        {
            success = result.Success,
            action = step.Action,
            result.Message,
            result.ActualValue,
            result.RetryCount,
            result.Strategy,
            result.FunctionId,
            result.Duration
        });
    }
}

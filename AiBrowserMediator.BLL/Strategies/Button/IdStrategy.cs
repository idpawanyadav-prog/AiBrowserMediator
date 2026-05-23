using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.Button;

public sealed class ButtonIdClickByFindElement(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "Button.Id.ClickByFindElement";
    public override string StrategyName => "Button.IdStrategy";
    public override string ControlType => ControlTypes.Button;
    public override string Action => ActionNames.Click;
    public override string LocatorType => "id";
    public override int Priority => 10;
    public override string Description => "Find button by stable ID and click it.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Locator is null) return CommandResult.Fail("Locator is required.");
        await BrowserSession.ClickAsync(step.Locator, cancellationToken);
        return Ok("Button clicked by ID.");
    }
}

using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.Button;

public sealed class ButtonCssClickByFindElement(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "Button.Css.ClickByFindElement";
    public override string StrategyName => "Button.CssStrategy";
    public override string ControlType => ControlTypes.Button;
    public override string Action => ActionNames.Click;
    public override string LocatorType => "css";
    public override int Priority => 60;
    public override string Description => "Find button by CSS selector and click it.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Locator is null) return CommandResult.Fail("Locator is required.");
        await BrowserSession.ClickAsync(step.Locator, cancellationToken);
        return Ok("Button clicked by CSS.");
    }
}

using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.Button;

public sealed class ButtonXPathClickByFindElement(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "Button.XPath.ClickByFindElement";
    public override string StrategyName => "Button.XPathStrategy";
    public override string ControlType => ControlTypes.Button;
    public override string Action => ActionNames.Click;
    public override string LocatorType => "xpath";
    public override int Priority => 80;
    public override string Description => "Find button by relative XPath and click it.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Locator is null) return CommandResult.Fail("Locator is required.");
        await BrowserSession.ClickAsync(step.Locator, cancellationToken);
        return Ok("Button clicked by XPath.");
    }
}

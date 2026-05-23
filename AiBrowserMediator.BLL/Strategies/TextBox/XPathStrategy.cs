using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.TextBox;

public sealed class TextBoxXPathSetValueByClearAndSendKeys(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "TextBox.XPath.SetValueByClearAndSendKeys";
    public override string StrategyName => "TextBox.XPathStrategy";
    public override string ControlType => ControlTypes.TextBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "xpath";
    public override int Priority => 80;
    public override string Description => "Find textbox by relative XPath, clear it, send keys, then validate value.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Textbox updated by XPath using clear + send keys.", step.Value)
            : CommandResult.Fail("Textbox value did not validate after XPath strategy.");
    }
}

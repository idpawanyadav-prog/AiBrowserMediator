using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.TextBox;

public sealed class TextBoxCssSetValueByClearAndSendKeys(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "TextBox.Css.SetValueByClearAndSendKeys";
    public override string StrategyName => "TextBox.CssStrategy";
    public override string ControlType => ControlTypes.TextBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "css";
    public override int Priority => 60;
    public override string Description => "Find textbox by CSS selector, clear it, send keys, then validate value.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Textbox updated by CSS using clear + send keys.", step.Value)
            : CommandResult.Fail("Textbox value did not validate after CSS strategy.");
    }
}

using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.TextBox;

public sealed class TextBoxNameSetValueByClearAndSendKeys(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "TextBox.Name.SetValueByClearAndSendKeys";
    public override string StrategyName => "TextBox.NameStrategy";
    public override string ControlType => ControlTypes.TextBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "name";
    public override int Priority => 20;
    public override string Description => "Find textbox by stable name, clear it, send keys, then validate value.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Textbox updated by name using clear + send keys.", step.Value)
            : CommandResult.Fail("Textbox value did not validate after name strategy.");
    }
}

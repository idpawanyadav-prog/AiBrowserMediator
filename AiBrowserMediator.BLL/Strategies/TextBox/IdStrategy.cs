using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.TextBox;

public sealed class TextBoxIdSetValueByClearAndSendKeys(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "TextBox.Id.SetValueByClearAndSendKeys";
    public override string StrategyName => "TextBox.IdStrategy";
    public override string ControlType => ControlTypes.TextBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "id";
    public override int Priority => 10;
    public override string Description => "Find textbox by stable ID, clear it, send keys, then validate value.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Textbox updated by ID using clear + send keys.", step.Value)
            : CommandResult.Fail("Textbox value did not validate after clear + send keys.");
    }
}

public sealed class TextBoxIdGetValueByAttribute(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "TextBox.Id.GetValueByAttribute";
    public override string StrategyName => "TextBox.IdStrategy";
    public override string ControlType => ControlTypes.TextBox;
    public override string Action => ActionNames.GetText;
    public override string LocatorType => "id";
    public override int Priority => 20;
    public override string Description => "Find textbox by stable ID and retrieve its value attribute.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Locator is null) return CommandResult.Fail("Locator is required.");
        var value = await BrowserSession.GetValueAsync(step.Locator, cancellationToken);
        return Ok("Textbox value retrieved by ID.", value);
    }
}

using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.ComboBox;

public sealed class ComboBoxCssSelectByTextOrValue(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "ComboBox.Css.SelectByTextOrValue";
    public override string StrategyName => "ComboBox.CssStrategy";
    public override string ControlType => ControlTypes.ComboBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "css";
    public override int Priority => 60;
    public override string Description => "Find combobox by CSS selector and select by visible text, then option value fallback.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Combobox selected by CSS using text/value.", step.Value)
            : CommandResult.Fail("Combobox selection did not validate.");
    }
}

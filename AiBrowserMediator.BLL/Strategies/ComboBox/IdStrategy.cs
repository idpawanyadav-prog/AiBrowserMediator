using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.ComboBox;

public sealed class ComboBoxIdSelectByTextOrValue(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "ComboBox.Id.SelectByTextOrValue";
    public override string StrategyName => "ComboBox.IdStrategy";
    public override string ControlType => ControlTypes.ComboBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "id";
    public override int Priority => 10;
    public override string Description => "Find combobox by stable ID and select by visible text, then option value fallback.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Combobox selected by ID using text/value.", step.Value)
            : CommandResult.Fail("Combobox selection did not validate.");
    }
}

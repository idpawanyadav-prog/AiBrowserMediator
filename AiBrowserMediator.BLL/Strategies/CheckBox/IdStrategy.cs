using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.CheckBox;

public sealed class CheckBoxIdSetCheckedByClickWhenNeeded(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "CheckBox.Id.SetCheckedByClickWhenNeeded";
    public override string StrategyName => "CheckBox.IdStrategy";
    public override string ControlType => ControlTypes.CheckBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "id";
    public override int Priority => 10;
    public override string Description => "Find checkbox by stable ID and click only if current checked state differs from requested value.";

    public override async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Value is null || step.Locator is null) return MissingValue();
        await BrowserSession.UpdateAsync(step.Locator, step.ControlType, step.Value, cancellationToken);
        return await BrowserSession.MatchesValueAsync(step.Locator, step.ControlType, step.Value, cancellationToken)
            ? Ok("Checkbox updated by ID using click-when-needed.", step.Value)
            : CommandResult.Fail("Checkbox state did not validate.");
    }
}

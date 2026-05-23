using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL.Strategies.TextBox;

public sealed class TextBoxJavaScriptStrategyPlaceholder(IBrowserSession browserSession) : BrowserStrategyFunction(browserSession)
{
    public override string FunctionId => "TextBox.JavaScript.SetValueByScript";
    public override string StrategyName => "TextBox.JavaScriptStrategy";
    public override string ControlType => ControlTypes.TextBox;
    public override string Action => ActionNames.Update;
    public override string LocatorType => "javascript";
    public override int Priority => 100;
    public override string Description => "JavaScript fallback placeholder; currently disabled until JS safety policy is implemented.";

    public override Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
        => Task.FromResult(CommandResult.Fail("JavaScript strategy is registered but disabled until safety validation is implemented."));
}

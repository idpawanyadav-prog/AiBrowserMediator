using AiBrowserMediator.Contracts;

namespace AiBrowserMediator.BLL.Strategies.Shared;

public sealed class StrategyFunctionExecutor(IEnumerable<IStrategyFunction> functions) : IStrategyFunctionExecutor
{
    private readonly IReadOnlyList<IStrategyFunction> _functions = functions
        .OrderBy(function => function.Priority)
        .ThenBy(function => function.FunctionId, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public IReadOnlyList<StrategyFunctionInfoDto> GetFunctions(string? controlType = null, string? locatorType = null, string? action = null)
        => _functions
            .Where(function => Matches(function.ControlType, controlType))
            .Where(function => Matches(function.LocatorType, locatorType))
            .Where(function => Matches(function.Action, action))
            .Select(function => new StrategyFunctionInfoDto(
                function.FunctionId,
                function.StrategyName,
                function.ControlType,
                function.Action,
                function.LocatorType,
                function.Priority,
                function.Description))
            .ToArray();

    public async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (step.Locator is null)
        {
            return CommandResult.Fail($"{step.Action} requires a locator.");
        }

        if (!string.IsNullOrWhiteSpace(step.Locator.FunctionId))
        {
            var exact = _functions.FirstOrDefault(function =>
                string.Equals(function.FunctionId, step.Locator.FunctionId, StringComparison.OrdinalIgnoreCase));

            if (exact is null)
            {
                return CommandResult.Fail($"Strategy function '{step.Locator.FunctionId}' was not found.");
            }

            if (!exact.CanHandle(step))
            {
                return CommandResult.Fail($"Strategy function '{exact.FunctionId}' cannot handle action='{step.Action}', controlType='{step.ControlType}', locatorType='{step.Locator.Type}'.");
            }

            return await exact.ExecuteAsync(step, cancellationToken);
        }

        var candidates = _functions.Where(function => function.CanHandle(step)).ToArray();
        if (candidates.Length == 0)
        {
            return CommandResult.Fail($"No strategy function can handle action='{step.Action}', controlType='{step.ControlType}', locatorType='{step.Locator.Type}'.");
        }

        var failures = new List<string>();
        foreach (var candidate in candidates)
        {
            var result = await candidate.ExecuteAsync(step, cancellationToken);
            if (result.Success)
            {
                return result;
            }

            failures.Add($"{candidate.FunctionId}: {result.Message}");
        }

        return CommandResult.Fail($"All strategy functions failed. {string.Join(" | ", failures)}");
    }

    private static bool Matches(string value, string? filter)
        => string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);
}

public abstract class BrowserStrategyFunction(IBrowserSession browserSession) : IStrategyFunction
{
    protected IBrowserSession BrowserSession { get; } = browserSession;

    public abstract string FunctionId { get; }
    public abstract string StrategyName { get; }
    public abstract string ControlType { get; }
    public abstract string Action { get; }
    public abstract string LocatorType { get; }
    public abstract int Priority { get; }
    public abstract string Description { get; }

    public virtual bool CanHandle(WorkflowStepDto step)
        => step.Locator is not null
           && string.Equals(step.Action, Action, StringComparison.OrdinalIgnoreCase)
           && string.Equals(step.ControlType, ControlType, StringComparison.OrdinalIgnoreCase)
           && string.Equals(step.Locator.Type, LocatorType, StringComparison.OrdinalIgnoreCase);

    public abstract Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default);

    protected CommandResult Ok(string message, string? actualValue = null)
        => CommandResult.Ok(message, actualValue, StrategyName, FunctionId);

    protected static CommandResult MissingValue()
        => CommandResult.Fail("Value is required.");
}

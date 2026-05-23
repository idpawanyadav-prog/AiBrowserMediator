using System.Diagnostics;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL;

public sealed class NavigateCommand(
    IBrowserSession browserSession,
    IRetryExecutor retryExecutor) : IBrowserCommand
{
    public string Action => ActionNames.Navigate;

    public async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (!browserSession.IsOpen)
        {
            return CommandResult.Fail("Browser is not open. Click Open Browser first.");
        }

        if (string.IsNullOrWhiteSpace(step.Url))
        {
            return CommandResult.Fail("Navigate requires a URL.");
        }

        var sw = Stopwatch.StartNew();
        await browserSession.NavigateAsync(step.Url, cancellationToken);
        if (step.WaitFor is not null)
        {
            var wait = step.WaitFor;
            var locator = new LocatorDto(wait.SelectorType, wait.Selector);
            await retryExecutor.ExecuteAsync(
                async ct =>
                {
                    if (!await browserSession.ExistsAsync(locator, ct))
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(wait.ExpectedValue))
                    {
                        return true;
                    }

                    var text = await browserSession.GetTextAsync(locator, ct);
                    return string.Equals(text, wait.ExpectedValue, StringComparison.OrdinalIgnoreCase);
                },
                static success => success,
                maxAttempts: Math.Max(1, step.TimeoutSeconds),
                delay: TimeSpan.FromMilliseconds(wait.RetryIntervalMs),
                cancellationToken);
        }
        sw.Stop();
        return CommandResult.Ok(
            step.WaitFor is null ? $"Navigated to {step.Url}" : $"Navigated to {step.Url} and wait condition passed.",
            duration: sw.Elapsed);
    }
}

public sealed class ClickCommand(
    IBrowserSession browserSession,
    IStrategyFunctionExecutor strategyFunctionExecutor) : IBrowserCommand
{
    public string Action => ActionNames.Click;

    public async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (!browserSession.IsOpen)
        {
            return CommandResult.Fail("Browser is not open. Click Open Browser first.");
        }

        if (step.Locator is null)
        {
            return CommandResult.Fail("Click requires a locator.");
        }

        var sw = Stopwatch.StartNew();
        var result = await strategyFunctionExecutor.ExecuteAsync(NormalizeClickStep(step), cancellationToken);
        sw.Stop();
        return result with { Duration = sw.Elapsed };
    }

    private static WorkflowStepDto NormalizeClickStep(WorkflowStepDto step)
        => string.Equals(step.ControlType, ControlTypes.Generic, StringComparison.OrdinalIgnoreCase)
            ? step with { ControlType = ControlTypes.Button }
            : step;
}

public sealed class UpdateCommand(
    IBrowserSession browserSession,
    IStrategyFunctionExecutor strategyFunctionExecutor) : IBrowserCommand
{
    public string Action => ActionNames.Update;

    public async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (!browserSession.IsOpen)
        {
            return CommandResult.Fail("Browser is not open. Click Open Browser first.");
        }

        if (step.Locator is null || step.Value is null)
        {
            return CommandResult.Fail("Update requires a locator and value.");
        }

        var sw = Stopwatch.StartNew();
        var result = await strategyFunctionExecutor.ExecuteAsync(step, cancellationToken);
        sw.Stop();
        return result with { Duration = sw.Elapsed };
    }
}

public sealed class WaitForCommand(
    IBrowserSession browserSession,
    IRetryExecutor retryExecutor) : IBrowserCommand
{
    public string Action => ActionNames.WaitFor;

    public async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (!browserSession.IsOpen)
        {
            return CommandResult.Fail("Browser is not open. Click Open Browser first.");
        }

        var waitFor = step.WaitFor;
        if (waitFor is null)
        {
            return CommandResult.Fail("WaitFor requires wait criteria.");
        }

        var locator = new LocatorDto(waitFor.SelectorType, waitFor.Selector);
        var sw = Stopwatch.StartNew();
        var (_, retries) = await retryExecutor.ExecuteAsync(
            async ct =>
            {
                if (!await browserSession.ExistsAsync(locator, ct))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(waitFor.ExpectedValue))
                {
                    return true;
                }

                var text = await browserSession.GetTextAsync(locator, ct);
                return string.Equals(text, waitFor.ExpectedValue, StringComparison.OrdinalIgnoreCase);
            },
            static exists => exists,
            maxAttempts: Math.Max(1, step.TimeoutSeconds),
            delay: TimeSpan.FromMilliseconds(waitFor.RetryIntervalMs),
            cancellationToken);
        sw.Stop();

        return CommandResult.Ok("Element became available.", strategy: locator.Type, retryCount: retries, duration: sw.Elapsed);
    }
}

public sealed class GetTextCommand(
    IBrowserSession browserSession,
    IStrategyFunctionExecutor strategyFunctionExecutor) : IBrowserCommand
{
    public string Action => ActionNames.GetText;

    public async Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
    {
        if (!browserSession.IsOpen)
        {
            return CommandResult.Fail("Browser is not open. Click Open Browser first.");
        }

        if (step.Locator is null)
        {
            return CommandResult.Fail("GetText requires a locator.");
        }

        var sw = Stopwatch.StartNew();
        var normalized = string.Equals(step.ControlType, ControlTypes.Generic, StringComparison.OrdinalIgnoreCase)
            ? step with { ControlType = ControlTypes.TextBox }
            : step;
        var result = await strategyFunctionExecutor.ExecuteAsync(normalized, cancellationToken);
        if (!result.Success)
        {
            var text = await browserSession.GetTextAsync(step.Locator, cancellationToken);
            result = CommandResult.Ok("Text captured.", actualValue: text, strategy: step.Locator.Type);
        }

        sw.Stop();
        return result with { Duration = sw.Elapsed };
    }
}

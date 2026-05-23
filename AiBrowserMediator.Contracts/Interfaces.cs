namespace AiBrowserMediator.Contracts;

public interface IBrowserSession : IAsyncDisposable
{
    bool IsOpen { get; }
    Task OpenAsync(CancellationToken cancellationToken = default);
    Task NavigateAsync(string url, CancellationToken cancellationToken = default);
    Task ClickAsync(LocatorDto locator, CancellationToken cancellationToken = default);
    Task UpdateAsync(LocatorDto locator, string controlType, string value, CancellationToken cancellationToken = default);
    Task<string?> GetTextAsync(LocatorDto locator, CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(LocatorDto locator, CancellationToken cancellationToken = default);
    Task<bool> MatchesValueAsync(LocatorDto locator, string controlType, string expectedValue, CancellationToken cancellationToken = default);
    Task<string> GetPageSourceAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(LocatorDto locator, CancellationToken cancellationToken = default);
}

public interface IBrowserCommand
{
    string Action { get; }
    Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default);
}

public interface ICommandDispatcher
{
    Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default);
}

public interface IWorkflowEngine
{
    Task<IReadOnlyList<CommandResult>> ExecuteAsync(WorkflowDocumentDto workflow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionEntryDto>> ExecuteDetailedAsync(WorkflowDocumentDto workflow, CancellationToken cancellationToken = default);
}

public interface IWorkflowSerializer
{
    string Serialize(WorkflowDocumentDto workflow);
    WorkflowDocumentDto Deserialize(string xml);
}

public interface IWorkflowRepository
{
    Task SaveAsync(WorkflowDocumentDto workflow, string path, CancellationToken cancellationToken = default);
    Task<WorkflowDocumentDto> LoadAsync(string path, CancellationToken cancellationToken = default);
}

public interface IRetryExecutor
{
    Task<(T Result, int Retries)> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> isSuccess,
        int maxAttempts,
        TimeSpan delay,
        CancellationToken cancellationToken = default);
}

public interface IValidationService
{
    Task<bool> HasExpectedValueAsync(LocatorDto locator, string controlType, string expectedValue, CancellationToken cancellationToken = default);
}

public interface ICapabilityRegistry
{
    IReadOnlyList<CapabilitySummaryDto> GetSummaries();
    CapabilityDescriptorDto? GetDescriptor(string name);
}

public interface IPageDescriptionService
{
    Task<PageDescriptionDto> DescribeAsync(CancellationToken cancellationToken = default);
}

public interface IStrategyFunction
{
    string FunctionId { get; }
    string StrategyName { get; }
    string ControlType { get; }
    string Action { get; }
    string LocatorType { get; }
    int Priority { get; }
    string Description { get; }
    bool CanHandle(WorkflowStepDto step);
    Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default);
}

public interface IStrategyFunctionExecutor
{
    IReadOnlyList<StrategyFunctionInfoDto> GetFunctions(string? controlType = null, string? locatorType = null, string? action = null);
    Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default);
}

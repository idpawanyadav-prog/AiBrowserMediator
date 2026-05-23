namespace AiBrowserMediator.Contracts;

public sealed record LocatorDto(
    string Type,
    string Value,
    string? Strategy = null,
    string? FunctionId = null);

public sealed record WaitForDto(
    string SelectorType,
    string Selector,
    string? ExpectedValue = null,
    int RetryIntervalMs = 500);

public sealed record WorkflowStepDto(
    int Id,
    string Action,
    string? Url = null,
    int TimeoutSeconds = 20,
    string ControlType = "generic",
    LocatorDto? Locator = null,
    string? Value = null,
    WaitForDto? WaitFor = null,
    bool SkipExecution = false,
    string? SkipReason = null);

public sealed record WorkflowDocumentDto(
    string SessionId,
    IReadOnlyList<WorkflowStepDto> Steps);

public sealed record CommandResult(
    bool Success,
    string Message,
    string? ActualValue = null,
    int RetryCount = 0,
    string? Strategy = null,
    string? FunctionId = null,
    TimeSpan? Duration = null)
{
    public static CommandResult Ok(string message, string? actualValue = null, string? strategy = null, string? functionId = null, int retryCount = 0, TimeSpan? duration = null)
        => new(true, message, actualValue, retryCount, strategy, functionId, duration);

    public static CommandResult Fail(string message, int retryCount = 0, TimeSpan? duration = null)
        => new(false, message, null, retryCount, null, null, duration);
}

public sealed record CapabilityParameterDto(
    string Name,
    string Type,
    bool Required,
    string Description,
    object? DefaultValue = null);

public sealed record CapabilitySummaryDto(
    string Name,
    string Summary);

public sealed record CapabilityDescriptorDto(
    string Name,
    string Summary,
    string Description,
    bool RequiresBrowser,
    IReadOnlyList<string> SupportedControlTypes,
    IReadOnlyList<CapabilityParameterDto> Parameters,
    object ExampleRequest,
    IReadOnlyList<string> SafetyNotes);

public sealed record PageControlDto(
    string Tag,
    string ControlType,
    string? Id,
    string? Name,
    string? InputType,
    bool Disabled,
    bool ReadOnly);

public sealed record PageDescriptionDto(
    IReadOnlyList<PageControlDto> Controls);

public sealed record WorkflowExecutionEntryDto(
    int StepId,
    string Action,
    string ControlType,
    string? LocatorType,
    string? LocatorValue,
    bool Success,
    string Message,
    string? ActualValue,
    int RetryCount,
    string? Strategy,
    string? FunctionId,
    TimeSpan? Duration);

public sealed record StrategyFunctionInfoDto(
    string FunctionId,
    string StrategyName,
    string ControlType,
    string Action,
    string LocatorType,
    int Priority,
    string Description);

using System.Collections.ObjectModel;
using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiBrowserMediator.View;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IWorkflowEngine workflowEngine;
    private readonly IWorkflowSerializer serializer;
    private readonly IBrowserSession browserSession;
    private readonly IStrategyFunctionExecutor strategyFunctionExecutor;
    private readonly List<WorkflowStepDto> _workflowSteps = [];
    private WorkflowStepDto? _lastSuccessfulStep;

    public MainWindowViewModel(
        ICommandDispatcher dispatcher,
        IWorkflowEngine workflowEngine,
        IWorkflowSerializer serializer,
        IBrowserSession browserSession,
        IStrategyFunctionExecutor strategyFunctionExecutor)
    {
        this.dispatcher = dispatcher;
        this.workflowEngine = workflowEngine;
        this.serializer = serializer;
        this.browserSession = browserSession;
        this.strategyFunctionExecutor = strategyFunctionExecutor;
        RefreshFunctionIds();
    }

    public IReadOnlyList<string> Actions { get; } =
    [
        ActionNames.Navigate,
        ActionNames.Click,
        ActionNames.Update,
        ActionNames.WaitFor,
        ActionNames.GetText
    ];

    public IReadOnlyList<string> SelectorTypes { get; } = ["id", "name", "css", "xpath"];
    public IReadOnlyList<string> ControlTypes { get; } =
    [
        AiBrowserMediator.Shared.ControlTypes.TextBox,
        AiBrowserMediator.Shared.ControlTypes.ComboBox,
        AiBrowserMediator.Shared.ControlTypes.CheckBox,
        AiBrowserMediator.Shared.ControlTypes.Button,
        AiBrowserMediator.Shared.ControlTypes.Generic
    ];
    public ObservableCollection<string> Steps { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];
    public ObservableCollection<WorkflowExecutionEntryDto> ExecutionHistory { get; } = [];
    public ObservableCollection<string> FunctionIds { get; } = [];

    [ObservableProperty] private string selectedAction = ActionNames.Navigate;
    [ObservableProperty] private string selectorType = "id";
    [ObservableProperty] private string selectedControlType = AiBrowserMediator.Shared.ControlTypes.TextBox;
    [ObservableProperty] private string? selectedFunctionId;
    [ObservableProperty] private string url = "https://example.com";
    [ObservableProperty] private string selector = string.Empty;
    [ObservableProperty] private string value = string.Empty;
    [ObservableProperty] private bool waitUntilControlMatches;
    [ObservableProperty] private int timeoutSeconds = 20;
    [ObservableProperty] private string pageSourceText = string.Empty;
    [ObservableProperty] private string xmlText = """
        <?xml version="1.0" encoding="utf-8"?>
        <Workflow sessionId="S1" />
        """;

    [RelayCommand]
    private async Task OpenBrowserAsync()
    {
        await browserSession.OpenAsync();
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | Browser | OK | Browser opened.");
    }

    [RelayCommand]
    private async Task CapturePageSourceAsync()
    {
        if (!browserSession.IsOpen)
        {
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | Source | FAIL | Browser is not open.");
            return;
        }

        PageSourceText = await browserSession.GetPageSourceAsync();
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | Source | OK | Captured current page source.");
    }

    [RelayCommand]
    private async Task ExecuteStepAsync()
    {
        var step = BuildStep(_workflowSteps.Count + 1);
        var result = await dispatcher.ExecuteAsync(step);
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | {step.Action} | {(result.Success ? "OK" : "FAIL")} | {result.Message}");
        if (result.Success)
        {
            step = ApplyDiscoveredFunction(step, result);
            _lastSuccessfulStep = step;
            SelectDiscoveredFunction(result);
        }
        else
        {
            _lastSuccessfulStep = null;
        }
    }

    [RelayCommand]
    private void AddToFlow()
    {
        if (_lastSuccessfulStep is null)
        {
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | AddToFlow | FAIL | Execute a successful step first.");
            return;
        }

        var step = _lastSuccessfulStep with { Id = _workflowSteps.Count + 1 };
        _workflowSteps.Add(step);
        Steps.Add(RenderStep(step));
        XmlText = serializer.Serialize(new WorkflowDocumentDto("S1", _workflowSteps));
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | AddToFlow | OK | Step {step.Id} recorded.");
    }

    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        var workflow = serializer.Deserialize(XmlText);
        var results = await workflowEngine.ExecuteDetailedAsync(workflow);
        ExecutionHistory.Clear();
        foreach (var result in results)
        {
            ExecutionHistory.Add(result);
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | Workflow | Step {result.StepId} | {result.Action} | {(result.Success ? "OK" : "FAIL")} | {result.Message}");
        }
    }

    [RelayCommand]
    private void ClearWorkflow()
    {
        _workflowSteps.Clear();
        Steps.Clear();
        ExecutionHistory.Clear();
        XmlText = serializer.Serialize(new WorkflowDocumentDto("S1", []));
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | Workflow | OK | Cleared.");
    }

    public async Task<IReadOnlyList<WorkflowExecutionEntryDto>> ExecuteWorkflowThroughUiAsync(WorkflowDocumentDto workflow, bool clearExisting = true)
    {
        if (clearExisting)
        {
            ClearWorkflow();
        }

        var history = new List<WorkflowExecutionEntryDto>();
        foreach (var incomingStep in workflow.Steps.OrderBy(step => step.Id))
        {
            ApplyStepToRecorder(incomingStep);

            var uiStep = BuildStep(_workflowSteps.Count + 1);
            var startedAt = DateTime.Now;
            var result = await dispatcher.ExecuteAsync(uiStep);

            if (result.Success)
            {
                uiStep = ApplyDiscoveredFunction(uiStep, result);
                _lastSuccessfulStep = uiStep;
                SelectDiscoveredFunction(result);
                AddToFlow();
            }
            else
            {
                _lastSuccessfulStep = null;
            }

            var entry = new WorkflowExecutionEntryDto(
                StepId: incomingStep.Id,
                Action: incomingStep.Action,
                ControlType: incomingStep.ControlType,
                LocatorType: incomingStep.Locator?.Type,
                LocatorValue: incomingStep.Locator?.Value,
                Success: result.Success,
                Message: result.Message,
                ActualValue: result.ActualValue,
                RetryCount: result.RetryCount,
                Strategy: result.Strategy,
                FunctionId: result.FunctionId,
                Duration: result.Duration);

            history.Add(entry);
            ExecutionHistory.Add(entry);
            Logs.Insert(0, $"{startedAt:HH:mm:ss} | UI Workflow | Step {incomingStep.Id} | {incomingStep.Action} | {(result.Success ? "OK" : "FAIL")} | {result.Message}");

            if (!result.Success)
            {
                break;
            }
        }

        return history;
    }

    private void ApplyStepToRecorder(WorkflowStepDto step)
    {
        SelectedAction = step.Action;
        TimeoutSeconds = step.TimeoutSeconds;
        SelectedControlType = string.IsNullOrWhiteSpace(step.ControlType)
            ? AiBrowserMediator.Shared.ControlTypes.Generic
            : step.ControlType;

        if (!string.IsNullOrWhiteSpace(step.Url))
        {
            Url = step.Url;
        }

        if (step.Locator is not null)
        {
            SelectorType = step.Locator.Type;
            Selector = step.Locator.Value;
            RefreshFunctionIds();
            SelectedFunctionId = step.Locator.FunctionId;
        }

        if (step.Value is not null)
        {
            Value = step.Value;
        }

        if (step.WaitFor is not null)
        {
            WaitUntilControlMatches = true;
            SelectorType = step.WaitFor.SelectorType;
            Selector = step.WaitFor.Selector;
            Value = step.WaitFor.ExpectedValue ?? string.Empty;
        }
        else
        {
            WaitUntilControlMatches = false;
        }
    }

    private WorkflowStepDto BuildStep(int id)
        => SelectedAction switch
        {
            ActionNames.Navigate => new(
                id,
                SelectedAction,
                Url,
                TimeoutSeconds,
                WaitFor: WaitUntilControlMatches
                    ? new WaitForDto(SelectorType, Selector, Value)
                    : null),
            ActionNames.WaitFor => new(id, SelectedAction, TimeoutSeconds: TimeoutSeconds, ControlType: SelectedControlType, WaitFor: new WaitForDto(SelectorType, Selector, Value)),
            ActionNames.Click => new(id, SelectedAction, TimeoutSeconds: TimeoutSeconds, ControlType: SelectedControlType, Locator: BuildLocator()),
            ActionNames.Update => new(id, SelectedAction, TimeoutSeconds: TimeoutSeconds, ControlType: SelectedControlType, Locator: BuildLocator(), Value: Value),
            ActionNames.GetText => new(id, SelectedAction, TimeoutSeconds: TimeoutSeconds, ControlType: SelectedControlType, Locator: BuildLocator()),
            _ => new(id, SelectedAction)
        };

    private LocatorDto BuildLocator()
    {
        var function = GetSelectedFunction();
        return new LocatorDto(
            SelectorType,
            Selector,
            function?.StrategyName,
            string.IsNullOrWhiteSpace(SelectedFunctionId) ? null : SelectedFunctionId);
    }

    private StrategyFunctionInfoDto? GetSelectedFunction()
        => strategyFunctionExecutor
            .GetFunctions(SelectedControlType, SelectorType, SelectedAction)
            .FirstOrDefault(function => string.Equals(function.FunctionId, SelectedFunctionId, StringComparison.OrdinalIgnoreCase));

    private WorkflowStepDto ApplyDiscoveredFunction(WorkflowStepDto step, CommandResult result)
    {
        if (step.Locator is null || string.IsNullOrWhiteSpace(result.FunctionId))
        {
            return step;
        }

        return step with
        {
            Locator = step.Locator with
            {
                Strategy = result.Strategy ?? step.Locator.Strategy,
                FunctionId = result.FunctionId
            }
        };
    }

    private void SelectDiscoveredFunction(CommandResult result)
    {
        if (string.IsNullOrWhiteSpace(result.FunctionId))
        {
            return;
        }

        if (!FunctionIds.Contains(result.FunctionId))
        {
            FunctionIds.Add(result.FunctionId);
        }

        SelectedFunctionId = result.FunctionId;
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} | FunctionId | OK | Selected {result.FunctionId}");
    }

    public bool IsNavigateAction => string.Equals(SelectedAction, ActionNames.Navigate, StringComparison.OrdinalIgnoreCase);
    public bool IsNonNavigateAction => !IsNavigateAction;
    public bool ShowNavigateWaitControls => IsNavigateAction && WaitUntilControlMatches;

    partial void OnSelectedActionChanged(string value)
    {
        OnPropertyChanged(nameof(IsNavigateAction));
        OnPropertyChanged(nameof(IsNonNavigateAction));
        OnPropertyChanged(nameof(ShowNavigateWaitControls));
        RefreshFunctionIds();
    }

    partial void OnWaitUntilControlMatchesChanged(bool value)
        => OnPropertyChanged(nameof(ShowNavigateWaitControls));

    partial void OnSelectedControlTypeChanged(string value)
        => RefreshFunctionIds();

    partial void OnSelectorTypeChanged(string value)
        => RefreshFunctionIds();

    private void RefreshFunctionIds()
    {
        var current = SelectedFunctionId;
        FunctionIds.Clear();
        FunctionIds.Add(string.Empty);

        foreach (var function in strategyFunctionExecutor.GetFunctions(SelectedControlType, SelectorType, SelectedAction))
        {
            FunctionIds.Add(function.FunctionId);
        }

        SelectedFunctionId = current is not null && FunctionIds.Contains(current) ? current : FunctionIds.FirstOrDefault();
    }

    private static string RenderStep(WorkflowStepDto step)
        => step.Action switch
        {
            ActionNames.Navigate => $"{step.Id}. navigate ? {step.Url}",
            ActionNames.Update => $"{step.Id}. update [{step.ControlType}] → {step.Locator?.Type}:{step.Locator?.Value}",
            ActionNames.Click => $"{step.Id}. click [{step.ControlType}] → {step.Locator?.Type}:{step.Locator?.Value}",
            ActionNames.WaitFor => $"{step.Id}. waitfor ? {step.WaitFor?.SelectorType}:{step.WaitFor?.Selector}",
            ActionNames.GetText => $"{step.Id}. gettext ? {step.Locator?.Type}:{step.Locator?.Value}",
            _ => $"{step.Id}. {step.Action}"
        };
}

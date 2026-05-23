using System.Xml.Linq;
using AiBrowserMediator.Contracts;

namespace AiBrowserMediator.BLL;

public sealed class CommandDispatcher(IEnumerable<IBrowserCommand> commands) : ICommandDispatcher
{
    private readonly IReadOnlyDictionary<string, IBrowserCommand> _commands =
        commands.ToDictionary(command => command.Action, StringComparer.OrdinalIgnoreCase);

    public Task<CommandResult> ExecuteAsync(WorkflowStepDto step, CancellationToken cancellationToken = default)
        => _commands.TryGetValue(step.Action, out var command)
            ? command.ExecuteAsync(step, cancellationToken)
            : Task.FromResult(CommandResult.Fail($"Unsupported action '{step.Action}'."));
}

public sealed class WorkflowEngine(ICommandDispatcher dispatcher) : IWorkflowEngine
{
    public async Task<IReadOnlyList<CommandResult>> ExecuteAsync(WorkflowDocumentDto workflow, CancellationToken cancellationToken = default)
    {
        var results = new List<CommandResult>();
        foreach (var step in workflow.Steps.OrderBy(step => step.Id))
        {
            if (step.SkipExecution)
            {
                results.Add(CommandResult.Ok($"Skipped: {step.SkipReason ?? "No reason provided."}"));
                continue;
            }

            var result = await dispatcher.ExecuteAsync(step, cancellationToken);
            results.Add(result);
            if (!result.Success)
            {
                break;
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<WorkflowExecutionEntryDto>> ExecuteDetailedAsync(WorkflowDocumentDto workflow, CancellationToken cancellationToken = default)
    {
        var results = new List<WorkflowExecutionEntryDto>();
        foreach (var step in workflow.Steps.OrderBy(step => step.Id))
        {
            if (step.SkipExecution)
            {
                results.Add(new WorkflowExecutionEntryDto(
                    StepId: step.Id,
                    Action: step.Action,
                    ControlType: step.ControlType,
                    LocatorType: step.Locator?.Type,
                    LocatorValue: step.Locator?.Value,
                    Success: true,
                    Message: $"Skipped: {step.SkipReason ?? "No reason provided."}",
                    ActualValue: null,
                    RetryCount: 0,
                    Strategy: null,
                    FunctionId: step.Locator?.FunctionId,
                    Duration: TimeSpan.Zero));
                continue;
            }

            var result = await dispatcher.ExecuteAsync(step, cancellationToken);
            results.Add(new WorkflowExecutionEntryDto(
                StepId: step.Id,
                Action: step.Action,
                ControlType: step.ControlType,
                LocatorType: step.Locator?.Type,
                LocatorValue: step.Locator?.Value,
                Success: result.Success,
                Message: result.Message,
                ActualValue: result.ActualValue,
                RetryCount: result.RetryCount,
                Strategy: result.Strategy,
                FunctionId: result.FunctionId,
                Duration: result.Duration));

            if (!result.Success)
            {
                break;
            }
        }

        return results;
    }
}

public sealed class XmlWorkflowSerializer : IWorkflowSerializer
{
    public string Serialize(WorkflowDocumentDto workflow)
    {
        var root = new XElement("Workflow", new XAttribute("sessionId", workflow.SessionId));
        foreach (var step in workflow.Steps)
        {
            var element = new XElement("Step",
                new XAttribute("id", step.Id),
                new XAttribute("action", step.Action),
                new XAttribute("timeout", step.TimeoutSeconds),
                new XAttribute("controlType", step.ControlType));

            if (step.SkipExecution)
            {
                element.Add(new XAttribute("skipExecution", true));
            }

            if (!string.IsNullOrWhiteSpace(step.SkipReason))
            {
                element.Add(new XAttribute("skipReason", step.SkipReason));
            }

            if (!string.IsNullOrWhiteSpace(step.Url))
            {
                element.Add(new XAttribute("url", step.Url));
            }

            if (step.Locator is not null)
            {
                element.Add(new XElement("Locator",
                    new XAttribute("type", step.Locator.Type),
                    new XAttribute("value", step.Locator.Value),
                    OptionalAttribute("strategy", step.Locator.Strategy),
                    OptionalAttribute("functionId", step.Locator.FunctionId)));
            }

            if (step.Value is not null)
            {
                element.Add(new XElement("Value", step.Value));
            }

            if (step.WaitFor is not null)
            {
                element.Add(new XElement("WaitFor",
                    new XAttribute("selectorType", step.WaitFor.SelectorType),
                    new XAttribute("selector", step.WaitFor.Selector),
                    new XAttribute("expectedValue", step.WaitFor.ExpectedValue ?? string.Empty),
                    new XAttribute("retryInterval", step.WaitFor.RetryIntervalMs)));
            }

            root.Add(element);
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }

    public WorkflowDocumentDto Deserialize(string xml)
    {
        var document = XDocument.Parse(xml);
        var root = document.Root ?? throw new InvalidOperationException("Workflow XML has no root.");
        var sessionId = root.Attribute("sessionId")?.Value ?? "S1";

        var steps = root.Elements("Step").Select(step =>
        {
            var locator = step.Element("Locator");
            var wait = step.Element("WaitFor");
            return new WorkflowStepDto(
                Id: (int?)step.Attribute("id") ?? 0,
                Action: (string?)step.Attribute("action") ?? string.Empty,
                Url: (string?)step.Attribute("url"),
                TimeoutSeconds: (int?)step.Attribute("timeout") ?? 20,
                ControlType: (string?)step.Attribute("controlType") ?? "generic",
                Locator: locator is null ? null : new LocatorDto(
                    (string?)locator.Attribute("type") ?? "css",
                    (string?)locator.Attribute("value") ?? string.Empty,
                    (string?)locator.Attribute("strategy"),
                    (string?)locator.Attribute("functionId")),
                Value: (string?)step.Element("Value"),
                WaitFor: wait is null ? null : new WaitForDto(
                    (string?)wait.Attribute("selectorType") ?? "css",
                    (string?)wait.Attribute("selector") ?? string.Empty,
                    (string?)wait.Attribute("expectedValue"),
                    (int?)wait.Attribute("retryInterval") ?? 500),
                SkipExecution: (bool?)step.Attribute("skipExecution") ?? false,
                SkipReason: (string?)step.Attribute("skipReason"));
        }).ToArray();

        return new WorkflowDocumentDto(sessionId, steps);
    }

    private static object[] OptionalAttribute(string name, string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : [new XAttribute(name, value)];
}

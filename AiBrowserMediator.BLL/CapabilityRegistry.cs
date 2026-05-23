using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;

namespace AiBrowserMediator.BLL;

public sealed class CapabilityRegistry : ICapabilityRegistry
{
    private readonly IReadOnlyDictionary<string, CapabilityDescriptorDto> _capabilities =
        new Dictionary<string, CapabilityDescriptorDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["openBrowser"] = new(
                "openBrowser",
                "Open the managed browser session.",
                "Create the Selenium-controlled browser session if it is not already open.",
                false,
                [ControlTypes.Generic],
                [],
                new { action = "openBrowser" },
                ["Call this before browser-dependent capabilities when no session exists."]
            ),
            [ActionNames.Navigate] = new(
                ActionNames.Navigate,
                "Open a URL in the current browser session.",
                "Navigate to a URL and optionally wait for a control to exist or display expected text.",
                true,
                [ControlTypes.Generic],
                [
                    new("url", "string", true, "Absolute URL to open."),
                    new("timeoutSeconds", "integer", false, "Maximum wait duration.", 20),
                    new("waitFor", "object", false, "Optional post-navigation wait condition.")
                ],
                new
                {
                    action = ActionNames.Navigate,
                    url = "https://example.com",
                    timeoutSeconds = 20,
                    waitFor = new
                    {
                        selectorType = "id",
                        selector = "header1",
                        expectedValue = "Header Text"
                    }
                },
                ["Prefer absolute URLs.", "Use waitFor when page readiness matters."]
            ),
            [ActionNames.Update] = new(
                ActionNames.Update,
                "Set a value on an editable control.",
                "Update a textbox-like field, select a dropdown value, or toggle a checkbox.",
                true,
                [ControlTypes.TextBox, ControlTypes.ComboBox, ControlTypes.CheckBox],
                [
                    new("locator", "object", true, "Locator for the target control."),
                    new("controlType", "string", true, "textbox, combobox, or checkbox."),
                    new("value", "string", true, "Value to apply.")
                ],
                new
                {
                    action = ActionNames.Update,
                    controlType = ControlTypes.TextBox,
                    locator = new { type = "id", value = "username" },
                    value = "admin"
                },
                ["Prefer id, then name, then css, and xpath only as fallback.", "Checkbox values should be true or false."]
            ),
            [ActionNames.Click] = new(
                ActionNames.Click,
                "Click a target control.",
                "Click a button, radio, checkbox, link-like element, or other clickable control.",
                true,
                [ControlTypes.Button, ControlTypes.CheckBox, ControlTypes.Generic],
                [
                    new("locator", "object", true, "Locator for the target control."),
                    new("controlType", "string", false, "Optional semantic hint.", ControlTypes.Generic)
                ],
                new
                {
                    action = ActionNames.Click,
                    controlType = ControlTypes.Button,
                    locator = new { type = "id", value = "submitButton" }
                },
                ["Avoid destructive clicks unless explicitly requested."]
            ),
            [ActionNames.WaitFor] = new(
                ActionNames.WaitFor,
                "Wait until a control exists or displays expected text.",
                "Poll for a target control and optionally validate its visible text.",
                true,
                [ControlTypes.Generic],
                [
                    new("waitFor", "object", true, "Wait criteria."),
                    new("timeoutSeconds", "integer", false, "Maximum wait duration.", 20)
                ],
                new
                {
                    action = ActionNames.WaitFor,
                    timeoutSeconds = 10,
                    waitFor = new
                    {
                        selectorType = "css",
                        selector = "h1",
                        expectedValue = "Ready"
                    }
                },
                ["Use after navigation or async UI transitions."]
            ),
            [ActionNames.GetText] = new(
                ActionNames.GetText,
                "Read visible text from a control.",
                "Return the visible text from a located element.",
                true,
                [ControlTypes.Generic],
                [
                    new("locator", "object", true, "Locator for the target control.")
                ],
                new
                {
                    action = ActionNames.GetText,
                    locator = new { type = "id", value = "status" }
                },
                ["Use visible text, not raw HTML, when validating labels."]
            ),
            ["capturePageSource"] = new(
                "capturePageSource",
                "Return the current DOM source.",
                "Capture the current page source from the active browser session.",
                true,
                [ControlTypes.Generic],
                [],
                new { action = "capturePageSource" },
                ["Capture source before choosing fallback locators."]
            ),
            ["describePage"] = new(
                "describePage",
                "Return a compact inventory of page controls.",
                "Parse the current DOM and summarize controls an agent can interact with.",
                true,
                [ControlTypes.Generic],
                [],
                new { action = "describePage" },
                ["Prefer this over full source when a compact control inventory is enough."]
            ),
            ["strategyFunctions"] = new(
                "strategyFunctions",
                "List executable strategy functions and their stable function IDs.",
                "Return the function catalog so an AI can discover exact function IDs for XML and API requests.",
                false,
                [ControlTypes.Generic],
                [
                    new("controlType", "string", false, "Filter by control type, e.g. textbox."),
                    new("locatorType", "string", false, "Filter by locator type, e.g. id."),
                    new("action", "string", false, "Filter by action, e.g. update.")
                ],
                new
                {
                    method = "GET",
                    url = "/capabilities/functions?controlType=textbox&locatorType=id&action=update"
                },
                ["Use returned functionId in XML Locator.functionId to skip future discovery."]
            )
        };

    public IReadOnlyList<CapabilitySummaryDto> GetSummaries()
        => _capabilities.Values
            .Select(capability => new CapabilitySummaryDto(capability.Name, capability.Summary))
            .OrderBy(capability => capability.Name)
            .ToArray();

    public CapabilityDescriptorDto? GetDescriptor(string name)
        => _capabilities.TryGetValue(name, out var descriptor) ? descriptor : null;
}

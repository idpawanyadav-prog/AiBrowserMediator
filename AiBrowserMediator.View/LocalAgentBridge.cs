using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AiBrowserMediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AiBrowserMediator.View;

public sealed class LocalAgentBridge(IServiceProvider services) : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public void Start(string prefix = "http://localhost:5050/")
    {
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        _loop = Task.Run(() => ListenAsync(_cts.Token));
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            if (context.Request.HttpMethod == "GET" && string.IsNullOrWhiteSpace(path))
            {
                await WriteJsonAsync(context.Response, new
                {
                    service = "AiBrowserMediator in-app bridge",
                    endpoints = new[]
                    {
                        "GET /capabilities",
                        "GET /capabilities/{name}",
                        "GET /capabilities/functions?controlType=textbox&locatorType=id&action=update",
                        "GET /app/controls",
                        "GET /app/ui-contract",
                        "POST /app/controls/execute",
                        "POST /ui/workflow/execute",
                        "POST /agent/execute"
                    }
                }, cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "GET" && path.Equals("/app/controls", StringComparison.OrdinalIgnoreCase))
            {
                var controls = await Application.Current.Dispatcher.InvokeAsync(GetAppControls);
                await WriteJsonAsync(context.Response, controls, cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "GET" && path.Equals("/app/ui-contract", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, GetUiContract(), cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "POST" && path.Equals("/app/controls/execute", StringComparison.OrdinalIgnoreCase))
            {
                var request = await JsonSerializer.DeserializeAsync<AppControlRequest>(
                    context.Request.InputStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                if (request is null)
                {
                    context.Response.StatusCode = 400;
                    await WriteJsonAsync(context.Response, new { error = "Invalid request body." }, cancellationToken);
                    return;
                }

                var result = await Application.Current.Dispatcher.InvokeAsync(() => ExecuteAppControl(request));
                await WriteJsonAsync(context.Response, result, cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "POST" && path.Equals("/ui/workflow/execute", StringComparison.OrdinalIgnoreCase))
            {
                var requestBody = await ReadBodyAsync(context.Request, cancellationToken);
                var xml = ExtractWorkflowXml(requestBody);
                var serializer = provider.GetRequiredService<IWorkflowSerializer>();
                var workflow = serializer.Deserialize(xml);

                var operation = Application.Current.Dispatcher.InvokeAsync<Task<object>>(() => ExecuteUiWorkflowAsync(workflow));
                var result = await await operation.Task;

                await WriteJsonAsync(context.Response, result, cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "GET" && path.Equals("/capabilities", StringComparison.OrdinalIgnoreCase))
            {
                var registry = provider.GetRequiredService<ICapabilityRegistry>();
                await WriteJsonAsync(context.Response, registry.GetSummaries(), cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "GET" && path.Equals("/capabilities/functions", StringComparison.OrdinalIgnoreCase))
            {
                var strategyFunctionExecutor = provider.GetRequiredService<IStrategyFunctionExecutor>();
                await WriteJsonAsync(
                    context.Response,
                    strategyFunctionExecutor.GetFunctions(
                        context.Request.QueryString["controlType"],
                        context.Request.QueryString["locatorType"],
                        context.Request.QueryString["action"]),
                    cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "GET" && path.StartsWith("/capabilities/", StringComparison.OrdinalIgnoreCase))
            {
                var registry = provider.GetRequiredService<ICapabilityRegistry>();
                var name = path["/capabilities/".Length..];
                var descriptor = registry.GetDescriptor(name);
                if (descriptor is null)
                {
                    context.Response.StatusCode = 404;
                    await WriteJsonAsync(context.Response, new { error = "Capability not found." }, cancellationToken);
                    return;
                }

                await WriteJsonAsync(context.Response, descriptor, cancellationToken);
                return;
            }

            if (context.Request.HttpMethod == "POST" && path.Equals("/agent/execute", StringComparison.OrdinalIgnoreCase))
            {
                var step = await JsonSerializer.DeserializeAsync<WorkflowStepDto>(
                    context.Request.InputStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                if (step is null)
                {
                    context.Response.StatusCode = 400;
                    await WriteJsonAsync(context.Response, new { error = "Invalid request body." }, cancellationToken);
                    return;
                }

                var browserSession = provider.GetRequiredService<IBrowserSession>();

                if (string.Equals(step.Action, "openBrowser", StringComparison.OrdinalIgnoreCase))
                {
                    await browserSession.OpenAsync(cancellationToken);
                    await WriteJsonAsync(context.Response, new
                    {
                        success = true,
                        action = step.Action,
                        message = "Browser opened."
                    }, cancellationToken);
                    return;
                }

                if (string.Equals(step.Action, "capturePageSource", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(context.Response, new
                    {
                        success = true,
                        action = step.Action,
                        pageSource = await browserSession.GetPageSourceAsync(cancellationToken)
                    }, cancellationToken);
                    return;
                }

                if (string.Equals(step.Action, "describePage", StringComparison.OrdinalIgnoreCase))
                {
                    var pageDescriptionService = provider.GetRequiredService<IPageDescriptionService>();
                    await WriteJsonAsync(context.Response, new
                    {
                        success = true,
                        action = step.Action,
                        description = await pageDescriptionService.DescribeAsync(cancellationToken)
                    }, cancellationToken);
                    return;
                }

                var dispatcher = provider.GetRequiredService<ICommandDispatcher>();
                var result = await dispatcher.ExecuteAsync(step, cancellationToken);
                await WriteJsonAsync(context.Response, new
                {
                    success = result.Success,
                    action = step.Action,
                    result.Message,
                    result.ActualValue,
                    result.RetryCount,
                    result.Strategy,
                    result.FunctionId,
                    result.Duration
                }, cancellationToken);
                return;
            }

            context.Response.StatusCode = 404;
            await WriteJsonAsync(context.Response, new { error = "Not found." }, cancellationToken);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await WriteJsonAsync(context.Response, new { error = ex.Message }, cancellationToken);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object value, CancellationToken cancellationToken)
    {
        response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
        var bytes = Encoding.UTF8.GetBytes(payload);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ExtractWorkflowXml(string requestBody)
    {
        var trimmed = requestBody.Trim();
        if (trimmed.StartsWith("<", StringComparison.Ordinal))
        {
            return trimmed;
        }

        using var document = JsonDocument.Parse(trimmed);
        if (document.RootElement.TryGetProperty("xml", out var xmlProperty))
        {
            return xmlProperty.GetString() ?? throw new InvalidOperationException("JSON property 'xml' is empty.");
        }

        if (document.RootElement.TryGetProperty("workflowXml", out var workflowXmlProperty))
        {
            return workflowXmlProperty.GetString() ?? throw new InvalidOperationException("JSON property 'workflowXml' is empty.");
        }

        throw new InvalidOperationException("Expected raw XML or JSON with 'xml' / 'workflowXml'.");
    }

    private static object GetUiContract()
        => new
        {
            modes = new[]
            {
                new
                {
                    name = "backend",
                    endpoint = "POST /agent/execute or POST /workflow/execute",
                    description = "Executes directly against backend services and Selenium. It does not intentionally update recorder UI fields."
                },
                new
                {
                    name = "ui",
                    endpoint = "POST /ui/workflow/execute",
                    description = "Executes an XML workflow by applying each step to the WPF recorder fields, running Execute Step, learning functionId, and adding successful steps to XML."
                }
            },
            recorderControls = new object[]
            {
                new { name = "Action", purpose = "Selects command type such as navigate, update, click, waitfor, gettext.", mapsTo = "Workflow Step.action" },
                new { name = "Timeout Seconds", purpose = "Maximum timeout used by the command.", mapsTo = "Workflow Step.timeout" },
                new { name = "URL", purpose = "Navigation target. Visible when action is navigate.", mapsTo = "Workflow Step.url" },
                new { name = "Selector Type", purpose = "Locator strategy input: id, name, css, xpath.", mapsTo = "Locator.type" },
                new { name = "Control Type", purpose = "Semantic control behavior: textbox, combobox, checkbox, button, generic.", mapsTo = "Workflow Step.controlType" },
                new { name = "Selector", purpose = "Locator value used to find the control.", mapsTo = "Locator.value" },
                new { name = "Value / Expected Text", purpose = "Value to set or expected text for wait conditions.", mapsTo = "Value or WaitFor.expectedValue" },
                new { name = "Function ID", purpose = "Optional exact strategy function. If blank, app discovers and then selects the successful functionId.", mapsTo = "Locator.functionId" },
                new { name = "Execute Step", purpose = "Runs the currently configured recorder step." },
                new { name = "Add To XML", purpose = "Adds the last successful recorder step to the XML editor, including learned functionId." }
            },
            requestExamples = new
            {
                rawXml = "POST /ui/workflow/execute with Content-Type application/xml",
                json = new
                {
                    xml = "<Workflow sessionId=\"S1\"><Step id=\"1\" action=\"navigate\" url=\"https://example.com\" timeout=\"20\" controlType=\"generic\" /></Workflow>"
                }
            }
        };

    private static async Task<object> ExecuteUiWorkflowAsync(WorkflowDocumentDto workflow)
    {
        var viewModel = Application.Current.MainWindow?.DataContext as MainWindowViewModel;
        if (viewModel is null)
        {
            return new
            {
                success = false,
                mode = "ui",
                message = "MainWindowViewModel is not available.",
                history = Array.Empty<WorkflowExecutionEntryDto>()
            };
        }

        var history = await viewModel.ExecuteWorkflowThroughUiAsync(workflow);
        return new
        {
            success = history.All(entry => entry.Success),
            mode = "ui",
            message = "Workflow executed through WPF recorder UI state.",
            history
        };
    }

    private static IReadOnlyList<AppControlInfo> GetAppControls()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow is null)
        {
            return [];
        }

        var controls = EnumerateVisuals(mainWindow)
            .Where(element => element is ButtonBase or TextBox or ComboBox or CheckBox or TabItem)
            .Select((element, index) => new AppControlInfo(
                Index: index,
                ControlType: element.GetType().Name,
                Name: GetControlName(element),
                Value: GetControlValue(element),
                IsEnabled: element.IsEnabled,
                IsVisible: element.IsVisible))
            .ToArray();

        return controls;
    }

    private static object ExecuteAppControl(AppControlRequest request)
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow is null)
        {
            return new { success = false, message = "Main window is not available." };
        }

        var controls = EnumerateVisuals(mainWindow)
            .Where(element => element is ButtonBase or TextBox or ComboBox or CheckBox or TabItem)
            .ToArray();

        var target = request.Index is not null && request.Index >= 0 && request.Index < controls.Length
            ? controls[request.Index.Value]
            : controls.FirstOrDefault(element =>
                string.Equals(element.GetType().Name, request.ControlType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetControlName(element), request.Name, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            return new { success = false, message = "Control not found." };
        }

        try
        {
            switch (request.Operation?.ToLowerInvariant())
            {
                case "invoke" when target is ButtonBase button:
                    button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
                case "select" when target is TabItem tabItem:
                    tabItem.IsSelected = true;
                    break;
                case "setvalue" when target is TextBox textBox:
                    textBox.Text = request.Value ?? string.Empty;
                    break;
                case "setvalue" when target is CheckBox checkBox:
                    checkBox.IsChecked = bool.TryParse(request.Value, out var checkedValue) && checkedValue;
                    break;
                case "setvalue" when target is ComboBox comboBox:
                    comboBox.SelectedItem = request.Value;
                    break;
                default:
                    return new { success = false, message = $"Operation '{request.Operation}' is not supported for {target.GetType().Name}." };
            }

            return new
            {
                success = true,
                message = "App control operation executed.",
                control = new AppControlInfo(-1, target.GetType().Name, GetControlName(target), GetControlValue(target), target.IsEnabled, target.IsVisible)
            };
        }
        catch (Exception ex)
        {
            return new { success = false, message = ex.Message };
        }
    }

    private static IEnumerable<FrameworkElement> EnumerateVisuals(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement element)
            {
                yield return element;
            }

            foreach (var descendant in EnumerateVisuals(child))
            {
                yield return descendant;
            }
        }
    }

    private static string GetControlName(FrameworkElement element)
        => element switch
        {
            CheckBox checkBox => checkBox.Content?.ToString() ?? element.Name,
            ButtonBase button => button.Content?.ToString() ?? element.Name,
            TextBlock textBlock => textBlock.Text,
            TabItem tabItem => tabItem.Header?.ToString() ?? element.Name,
            _ => element.Name
        };

    private static string? GetControlValue(FrameworkElement element)
        => element switch
        {
            TextBox textBox => textBox.Text,
            ComboBox comboBox => comboBox.SelectedItem?.ToString(),
            CheckBox checkBox => checkBox.IsChecked?.ToString(),
            TabItem tabItem => tabItem.IsSelected.ToString(),
            _ => null
        };

    private sealed record AppControlInfo(int Index, string ControlType, string Name, string? Value, bool IsEnabled, bool IsVisible);

    private sealed record AppControlRequest(string? Operation, int? Index, string? ControlType, string? Name, string? Value);

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();

        if (_loop is not null)
        {
            await _loop;
        }

        _cts.Dispose();
    }
}

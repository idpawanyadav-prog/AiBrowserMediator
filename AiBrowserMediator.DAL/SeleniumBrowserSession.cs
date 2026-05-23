using AiBrowserMediator.Contracts;
using AiBrowserMediator.Shared;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AiBrowserMediator.DAL;

public sealed class SeleniumBrowserSession : IBrowserSession
{
    private IWebDriver? _driver;
    public bool IsOpen => _driver is not null;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_driver is null)
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            _driver = new ChromeDriver(options);
        }

        return Task.CompletedTask;
    }

    public Task NavigateAsync(string url, CancellationToken cancellationToken = default)
    {
        EnsureOpen().Navigate().GoToUrl(url);
        return Task.CompletedTask;
    }

    public Task ClickAsync(LocatorDto locator, CancellationToken cancellationToken = default)
    {
        EnsureOpen().FindElement(ToBy(locator)).Click();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LocatorDto locator, string controlType, string value, CancellationToken cancellationToken = default)
    {
        var element = EnsureOpen().FindElement(ToBy(locator));
        switch (controlType.ToLowerInvariant())
        {
            case ControlTypes.ComboBox:
                var select = new SelectElement(element);
                try
                {
                    select.SelectByText(value);
                }
                catch (NoSuchElementException)
                {
                    select.SelectByValue(value);
                }
                break;
            case ControlTypes.CheckBox:
                var shouldBeChecked = bool.TryParse(value, out var parsed) && parsed;
                if (element.Selected != shouldBeChecked)
                {
                    element.Click();
                }
                break;
            default:
                element.Clear();
                element.SendKeys(value);
                break;
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetTextAsync(LocatorDto locator, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(EnsureOpen().FindElement(ToBy(locator)).Text);

    public Task<string?> GetValueAsync(LocatorDto locator, CancellationToken cancellationToken = default)
    {
        var element = EnsureOpen().FindElement(ToBy(locator));
        if (string.Equals(element.TagName, "select", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(new SelectElement(element).SelectedOption.Text);
        }

        if (string.Equals(element.GetAttribute("type"), "checkbox", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(element.Selected.ToString());
        }

        return Task.FromResult(element.GetAttribute("value"));
    }

    public Task<bool> MatchesValueAsync(LocatorDto locator, string controlType, string expectedValue, CancellationToken cancellationToken = default)
    {
        var element = EnsureOpen().FindElement(ToBy(locator));
        if (string.Equals(controlType, ControlTypes.ComboBox, StringComparison.OrdinalIgnoreCase))
        {
            var selected = new SelectElement(element).SelectedOption;
            var matchesText = string.Equals(selected.Text, expectedValue, StringComparison.OrdinalIgnoreCase);
            var matchesValue = string.Equals(selected.GetAttribute("value"), expectedValue, StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(matchesText || matchesValue);
        }

        if (string.Equals(controlType, ControlTypes.CheckBox, StringComparison.OrdinalIgnoreCase))
        {
            var expected = bool.TryParse(expectedValue, out var parsed) && parsed;
            return Task.FromResult(element.Selected == expected);
        }

        return Task.FromResult(string.Equals(element.GetAttribute("value"), expectedValue, StringComparison.OrdinalIgnoreCase));
    }

    public Task<bool> ExistsAsync(LocatorDto locator, CancellationToken cancellationToken = default)
        => Task.FromResult(EnsureOpen().FindElements(ToBy(locator)).Count > 0);

    public Task<string> GetPageSourceAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(EnsureOpen().PageSource);

    public ValueTask DisposeAsync()
    {
        _driver?.Quit();
        _driver?.Dispose();
        _driver = null;
        return ValueTask.CompletedTask;
    }

    private IWebDriver EnsureOpen()
        => _driver ?? throw new InvalidOperationException("Browser is not open. Use Open Browser first.");

    private static By ToBy(LocatorDto locator)
        => locator.Type.ToLowerInvariant() switch
        {
            "id" => By.Id(locator.Value),
            "name" => By.Name(locator.Value),
            "xpath" => By.XPath(locator.Value),
            "css" => By.CssSelector(locator.Value),
            "classname" => By.ClassName(locator.Value),
            _ => By.CssSelector(locator.Value)
        };
}

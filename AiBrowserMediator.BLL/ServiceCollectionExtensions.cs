using AiBrowserMediator.Contracts;
using AiBrowserMediator.BLL.Strategies.Button;
using AiBrowserMediator.BLL.Strategies.CheckBox;
using AiBrowserMediator.BLL.Strategies.ComboBox;
using AiBrowserMediator.BLL.Strategies.Shared;
using AiBrowserMediator.BLL.Strategies.TextBox;
using Microsoft.Extensions.DependencyInjection;

namespace AiBrowserMediator.BLL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowSerializer, XmlWorkflowSerializer>();
        services.AddScoped<IRetryExecutor, RetryExecutor>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();
        services.AddScoped<IPageDescriptionService, PageDescriptionService>();
        services.AddScoped<IStrategyFunctionExecutor, StrategyFunctionExecutor>();
        services.AddScoped<IStrategyFunction, TextBoxIdSetValueByClearAndSendKeys>();
        services.AddScoped<IStrategyFunction, TextBoxIdGetValueByAttribute>();
        services.AddScoped<IStrategyFunction, TextBoxNameSetValueByClearAndSendKeys>();
        services.AddScoped<IStrategyFunction, TextBoxCssSetValueByClearAndSendKeys>();
        services.AddScoped<IStrategyFunction, TextBoxXPathSetValueByClearAndSendKeys>();
        services.AddScoped<IStrategyFunction, TextBoxJavaScriptStrategyPlaceholder>();
        services.AddScoped<IStrategyFunction, ButtonIdClickByFindElement>();
        services.AddScoped<IStrategyFunction, ButtonNameClickByFindElement>();
        services.AddScoped<IStrategyFunction, ButtonCssClickByFindElement>();
        services.AddScoped<IStrategyFunction, ButtonXPathClickByFindElement>();
        services.AddScoped<IStrategyFunction, ComboBoxIdSelectByTextOrValue>();
        services.AddScoped<IStrategyFunction, ComboBoxNameSelectByTextOrValue>();
        services.AddScoped<IStrategyFunction, ComboBoxCssSelectByTextOrValue>();
        services.AddScoped<IStrategyFunction, ComboBoxXPathSelectByTextOrValue>();
        services.AddScoped<IStrategyFunction, CheckBoxIdSetCheckedByClickWhenNeeded>();
        services.AddScoped<IStrategyFunction, CheckBoxNameSetCheckedByClickWhenNeeded>();
        services.AddScoped<IStrategyFunction, CheckBoxCssSetCheckedByClickWhenNeeded>();
        services.AddScoped<IStrategyFunction, CheckBoxXPathSetCheckedByClickWhenNeeded>();
        services.AddScoped<IBrowserCommand, NavigateCommand>();
        services.AddScoped<IBrowserCommand, ClickCommand>();
        services.AddScoped<IBrowserCommand, UpdateCommand>();
        services.AddScoped<IBrowserCommand, WaitForCommand>();
        services.AddScoped<IBrowserCommand, GetTextCommand>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        return services;
    }
}

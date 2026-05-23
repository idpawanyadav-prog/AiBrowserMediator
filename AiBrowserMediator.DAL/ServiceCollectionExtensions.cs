using AiBrowserMediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AiBrowserMediator.DAL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services)
    {
        services.AddSingleton<IBrowserSession, SeleniumBrowserSession>();
        services.AddScoped<IWorkflowRepository, FileWorkflowRepository>();
        return services;
    }
}

using System.Windows;
using AiBrowserMediator.BLL;
using AiBrowserMediator.DAL;
using Microsoft.Extensions.DependencyInjection;

namespace AiBrowserMediator.View;

public partial class App : Application
{
    private ServiceProvider? _services;
    private LocalAgentBridge? _bridge;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = new ServiceCollection()
            .AddBusinessLayer()
            .AddDataAccessLayer()
            .AddSingleton<MainWindowViewModel>()
            .AddSingleton<MainWindow>()
            .BuildServiceProvider();

        _bridge = new LocalAgentBridge(_services);
        _bridge.Start();
        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_services is not null)
        {
            if (_bridge is not null)
            {
                await _bridge.DisposeAsync();
            }

            await _services.DisposeAsync();
        }

        base.OnExit(e);
    }
}

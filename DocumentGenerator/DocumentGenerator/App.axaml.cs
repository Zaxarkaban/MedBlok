using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocumentGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DocumentGenerator
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var services = new ServiceCollection();

                // Регистрация DatabaseInitializer как синглтона
                services.AddSingleton<DatabaseInitializer>(provider =>
                {
                    var initializer = new DatabaseInitializer();
                    initializer.Initialize(); // Инициализация базы данных при создании
                    return initializer;
                });

                // Регистрация MainWindowViewModel с внедрением DatabaseInitializer
                services.AddTransient<MainWindowViewModel>(provider =>
                {
                    var dbInitializer = provider.GetRequiredService<DatabaseInitializer>();
                    return new MainWindowViewModel();
                });

                // Регистрация главного окна
                services.AddTransient<MainWindow>();

                var serviceProvider = services.BuildServiceProvider();

                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
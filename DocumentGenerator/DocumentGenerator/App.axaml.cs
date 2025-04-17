using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocumentGenerator.Data;
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

                // Регистрация AppDbContext
                services.AddDbContext<AppDbContext>();
                services.AddSingleton<DatabaseInitializer>(provider =>
                {
                    var initializer = new DatabaseInitializer();
                    initializer.Initialize(); // Инициализируем базу данных
                    return initializer;
                });

                // Регистрация MainWindowViewModel
                services.AddTransient<MainWindowViewModel>();

                // Регистрация PdfGenerator
                services.AddTransient<PdfGenerator>();

                // Регистрация главного окна
                services.AddTransient<MainWindow>();

                var serviceProvider = services.BuildServiceProvider();

                // Создаём главное окно
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
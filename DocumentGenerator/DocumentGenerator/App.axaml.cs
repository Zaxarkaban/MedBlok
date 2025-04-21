using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocumentGenerator.ViewModels;
using DocumentGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using ReactiveUI;
using System.Reactive.Concurrency;
//using DocumentGenerator.Views;

namespace DocumentGenerator
{
    public partial class App : Application
    {
        public override void Initialize() { AvaloniaXamlLoader.Load(this); }

        public override void OnFrameworkInitializationCompleted()
        {

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var services = new ServiceCollection();

                // Регистрация ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<NewFormViewModel>();
                services.AddTransient<ExcelDataViewModel>();

                // Регистрация сервисов
                services.AddTransient<DocumentService>();
                services.AddTransient<NewFormPdfGenerator>();

                // Регистрация окон
                services.AddTransient<MainWindow>(provider => new MainWindow(provider));
                services.AddTransient<NewForm>(provider => new NewForm(provider));
                services.AddTransient<MenuWindow>(provider => new MenuWindow(provider));
                services.AddTransient<AnalysisView>(provider => new AnalysisView(provider));

                // Регистрация IServiceProvider
                services.AddSingleton<IServiceProvider>(sp => sp);

                var serviceProvider = services.BuildServiceProvider();

                // Открываем MenuWindow вместо MainWindow
                desktop.MainWindow = serviceProvider.GetRequiredService<MenuWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}
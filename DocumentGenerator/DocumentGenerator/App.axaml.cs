using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocumentGenerator.Services;
using DocumentGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DocumentGenerator
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DocumentService>();
            services.AddTransient<DataEntryViewModel>();
            services.AddTransient<ExcelDataViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<IServiceProvider>(sp => sp);

            _serviceProvider = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindowViewModel = _serviceProvider.GetService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
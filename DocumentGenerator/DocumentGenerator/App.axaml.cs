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

                // ����������� AppDbContext
                services.AddDbContext<AppDbContext>();
                services.AddSingleton<DatabaseInitializer>(provider =>
                {
                    var initializer = new DatabaseInitializer();
                    initializer.Initialize(); // �������������� ���� ������
                    return initializer;
                });

                // ����������� MainWindowViewModel
                services.AddTransient<MainWindowViewModel>();

                // ����������� PdfGenerator
                services.AddTransient<PdfGenerator>();

                // ����������� �������� ����
                services.AddTransient<MainWindow>();

                var serviceProvider = services.BuildServiceProvider();

                // ������ ������� ����
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
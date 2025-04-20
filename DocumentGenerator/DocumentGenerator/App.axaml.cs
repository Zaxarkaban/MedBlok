using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocumentGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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

                // ����������� MainWindowViewModel
                services.AddTransient<MainWindowViewModel>();

                // ����������� �������� ����
                services.AddTransient<MainWindow>(provider => new MainWindow(provider));

                // ����������� ���� ����
                services.AddTransient<MenuWindow>(provider => new MenuWindow(provider));

                var serviceProvider = services.BuildServiceProvider();

                // ��������� MenuWindow ������ MainWindow
                desktop.MainWindow = serviceProvider.GetRequiredService<MenuWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

}
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

                // Регистрация MainWindowViewModel
                services.AddTransient<MainWindowViewModel>();

                // Регистрация главного окна
                services.AddTransient<MainWindow>(provider => new MainWindow(provider));

                // Регистрация окна меню
                services.AddTransient<MenuWindow>(provider => new MenuWindow(provider));

                var serviceProvider = services.BuildServiceProvider();

                // Открываем MenuWindow вместо MainWindow
                desktop.MainWindow = serviceProvider.GetRequiredService<MenuWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

}
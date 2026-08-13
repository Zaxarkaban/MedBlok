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
        public static IServiceProvider? Services { get; private set; }

        public override void Initialize() { AvaloniaXamlLoader.Load(this); }

        public override void OnFrameworkInitializationCompleted()
        {

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var services = new ServiceCollection();

                // ����������� ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<NewFormViewModel>();
                services.AddTransient<ExcelDataViewModel>();

                // ����������� ��������
                services.AddTransient<DocumentService>();
                services.AddTransient<NewFormPdfGenerator>();
                services.AddSingleton<IUserProgramStorage, UserProgramStorage>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IExportPathMemory, ExportPathMemory>();
                services.AddTransient<IPdfFormFiller, PdfFormFiller>();

                // ����������� ����
                services.AddTransient<MainWindow>(provider => new MainWindow(provider));
                services.AddTransient<NewForm>(provider => new NewForm(provider));
                services.AddTransient<MenuWindow>(provider => new MenuWindow(provider));
                services.AddTransient<AnalysisView>(provider => new AnalysisView(provider));
                services.AddTransient<EditorWindow>(provider => new EditorWindow(provider));
                services.AddTransient<UserProgramsWindow>(provider => new UserProgramsWindow(provider));
                services.AddTransient<ThemeSettingsWindow>(provider => new ThemeSettingsWindow(provider.GetRequiredService<IThemeService>()));

                // ����������� IServiceProvider
                services.AddSingleton<IServiceProvider>(sp => sp);

                var serviceProvider = services.BuildServiceProvider();
                Services = serviceProvider;
                var themeService = serviceProvider.GetRequiredService<IThemeService>();
                themeService.Load();

                ThemeFluentResources.Apply(Current!, themeService.CurrentTheme);
                themeService.ThemeChanged += m => ThemeFluentResources.Apply(Current!, m);

                // ��������� MenuWindow ������ MainWindow
                PdfFontHelper.Warmup();

                desktop.MainWindow = serviceProvider.GetRequiredService<MenuWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}
using System;
using Avalonia;
using OfficeOpenXml; // Добавлено

namespace DocumentGenerator
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Добавлено
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
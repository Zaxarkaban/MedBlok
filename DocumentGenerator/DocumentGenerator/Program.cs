using System;
using Avalonia;
using Avalonia.Win32;
using OfficeOpenXml;

namespace DocumentGenerator
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

            // Win7: Angle/GPU часто даёт чёрное окно и крутит CPU ~50%.
            // Soft-рендер — известный рабочий обход (Avalonia Win7 больше не поддерживает).
            if (IsWindows7OrOlder())
            {
                builder = builder.With(new Win32PlatformOptions
                {
                    RenderingMode = new[] { Win32RenderingMode.Software }
                });
            }
            else
            {
                builder = builder.With(new Win32PlatformOptions
                {
                    RenderingMode = new[]
                    {
                        Win32RenderingMode.AngleEgl,
                        Win32RenderingMode.Software
                    }
                });
            }

            return builder;
        }

        /// <summary>Windows 7 = 6.1; Vista = 6.0.</summary>
        private static bool IsWindows7OrOlder()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            var v = Environment.OSVersion.Version;
            return v.Major < 6 || (v.Major == 6 && v.Minor <= 1);
        }
    }
}
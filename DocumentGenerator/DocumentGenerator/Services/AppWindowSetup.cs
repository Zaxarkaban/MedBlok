using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DocumentGenerator.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Единая настройка окон: центрирование, тема, классы оформления.
    /// </summary>
    public static class AppWindowSetup
    {
        public static void Configure(Window window, IServiceProvider serviceProvider, bool expandToWorkAreaHeight = false)
        {
            AppBranding.ApplyWindowBranding(window);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var themeService = serviceProvider.GetRequiredService<IThemeService>();
            SpaceWindowChrome.Attach(window, themeService);
            window.Opened += (_, _) =>
            {
                ApplyThemeClasses(window, themeService.CurrentTheme);
                if (expandToWorkAreaHeight)
                    ExpandToWorkAreaHeight(window);
                else
                    EnsureVisibleOnScreen(window);
            };
        }

        /// <summary>Растянуть окно на всю высоту рабочей области экрана, прижать к верху.</summary>
        public static void ExpandToWorkAreaHeight(Window window)
        {
            var screen = window.Screens?.ScreenFromWindow(window) ?? window.Screens?.Primary;
            if (screen == null)
                return;

            var wa = screen.WorkingArea;
            var scale = screen.Scaling;
            var width = window.Width > 0 ? window.Width : window.MinWidth;

            window.Height = wa.Height / scale;
            window.Position = new PixelPoint(
                wa.X + Math.Max(0, (wa.Width - (int)(width * scale)) / 2),
                wa.Y);
        }

        public static void ConfigureDialog(Window dialog, Window? owner)
        {
            AppBranding.ApplyWindowBranding(dialog);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.CanResize = false;
            dialog.SizeToContent = SizeToContent.WidthAndHeight;
            dialog.MinWidth = 420;
            dialog.MaxWidth = 520;

            var themeService = ResolveThemeService();
            if (themeService != null)
            {
                ApplyThemeClasses(dialog, themeService.CurrentTheme);
                // Модальные диалоги — без космического/светлого wrapper, иначе ломается размер и кнопки.
                if (!dialog.Classes.Contains("app-dialog"))
                    SpaceWindowChrome.Attach(dialog, themeService);
            }

            dialog.Opened += (_, _) =>
            {
                if (owner == null || !owner.IsVisible)
                    CenterOnScreen(dialog);
                else
                    EnsureVisibleOnScreen(dialog);
            };
        }

        public static void ApplyThemeClasses(Window window, AppThemeMode mode)
        {
            window.Classes.Remove("light-ui");
            window.Classes.Remove("space-holo-ui");

            switch (mode)
            {
                case AppThemeMode.Colorful:
                    window.Classes.Add("light-ui");
                    break;
                case AppThemeMode.Space:
                    window.Classes.Add("space-holo-ui");
                    break;
            }
        }

        public static IThemeService? ResolveThemeService()
        {
            return App.Services?.GetService<IThemeService>();
        }

        public static Window? ResolveOwner(Window? owner)
        {
            if (owner is { IsVisible: true })
                return owner;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return owner;
        }

        public static void CenterOnScreen(Window window)
        {
            var screens = window.Screens;
            var screen = screens?.ScreenFromWindow(window) ?? screens?.Primary;
            if (screen == null)
                return;

            var scale = screen.Scaling;
            var wa = screen.WorkingArea;
            var w = (int)(window.Bounds.Width * scale);
            var h = (int)(window.Bounds.Height * scale);
            if (w <= 0) w = (int)(window.Width * scale);
            if (h <= 0) h = (int)(window.Height * scale);

            window.Position = new PixelPoint(
                wa.X + Math.Max(0, (wa.Width - w) / 2),
                wa.Y + Math.Max(0, (wa.Height - h) / 2));
        }

        private static void EnsureVisibleOnScreen(Window window)
        {
            var screens = window.Screens;
            var screen = screens?.ScreenFromWindow(window) ?? screens?.Primary;
            if (screen == null)
                return;

            var scale = screen.Scaling;
            var wa = screen.WorkingArea;
            var pos = window.Position;
            var w = Math.Max(1, (int)(window.Bounds.Width * scale));
            var h = Math.Max(1, (int)(window.Bounds.Height * scale));

            var offScreen = pos.X < wa.X - 40
                || pos.Y < wa.Y - 40
                || pos.X + w > wa.X + wa.Width + 40
                || pos.Y + h > wa.Y + wa.Height + 40;

            if (offScreen)
                CenterOnScreen(window);
        }
    }
}

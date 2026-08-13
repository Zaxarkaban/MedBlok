using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using DocumentGenerator.Models;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Подменяет ресурсы Fluent для режимов Space и светлой темы (Colorful).
    /// </summary>
    public static class ThemeFluentResources
    {
        private static ResourceInclude? _include;

        public static void Apply(Application app, AppThemeMode mode)
        {
            if (_include != null)
            {
                app.Resources.MergedDictionaries.Remove(_include);
                _include = null;
            }

            app.RequestedThemeVariant = mode switch
            {
                AppThemeMode.Colorful => ThemeVariant.Light,
                AppThemeMode.Space => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            var source = mode switch
            {
                AppThemeMode.Space => "avares://DocumentGenerator/Styles/SpaceHoloFluentOverrides.axaml",
                AppThemeMode.Colorful => "avares://DocumentGenerator/Styles/LightFluentOverrides.axaml",
                _ => null
            };

            if (source == null)
                return;

            _include = new ResourceInclude(new Uri("avares://DocumentGenerator/"))
            {
                Source = new Uri(source)
            };
            app.Resources.MergedDictionaries.Add(_include);
        }
    }
}

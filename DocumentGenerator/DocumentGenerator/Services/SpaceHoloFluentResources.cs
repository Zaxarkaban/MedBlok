using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using DocumentGenerator.Models;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Подменяет ресурсы Fluent (TextBox, ComboBox, акцент) на голубо-циановые в режиме Space,
    /// в т.ч. для выпадающих списков, которые не наследуют класс окна.
    /// </summary>
    public static class SpaceHoloFluentResources
    {
        private static ResourceInclude? _include;

        public static void Apply(Application app, AppThemeMode mode)
        {
            if (_include != null)
            {
                app.Resources.MergedDictionaries.Remove(_include);
                _include = null;
            }

            if (mode != AppThemeMode.Space)
                return;

            _include = new ResourceInclude(new Uri("avares://DocumentGenerator/"))
            {
                Source = new Uri("avares://DocumentGenerator/Styles/SpaceHoloFluentOverrides.axaml")
            };
            app.Resources.MergedDictionaries.Add(_include);
        }
    }
}

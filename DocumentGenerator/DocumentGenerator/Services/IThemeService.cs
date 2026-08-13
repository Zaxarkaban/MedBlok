using DocumentGenerator.Models;
using System;

namespace DocumentGenerator.Services
{
    public interface IThemeService
    {
        AppThemeMode CurrentTheme { get; }

        event Action<AppThemeMode>? ThemeChanged;

        void SetTheme(AppThemeMode mode);

        void Load();

        void Save();
    }
}

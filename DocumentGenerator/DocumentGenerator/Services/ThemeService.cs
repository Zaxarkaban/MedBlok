using DocumentGenerator.Models;
using System;
using System.IO;
using System.Text.Json;

namespace DocumentGenerator.Services
{
    public sealed class ThemeService : IThemeService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _settingsPath;
        private AppThemeMode _current = AppThemeMode.Default;

        public ThemeService()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DocumentGenerator");
            _settingsPath = Path.Combine(root, "appearance.json");
        }

        public AppThemeMode CurrentTheme => _current;

        public event Action<AppThemeMode>? ThemeChanged;

        public void Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _current = AppThemeMode.Default;
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                var dto = JsonSerializer.Deserialize<AppearanceDto>(json, JsonOptions);
                if (dto?.Theme is { } t && Enum.IsDefined(typeof(AppThemeMode), t))
                    _current = (AppThemeMode)t;
            }
            catch
            {
                _current = AppThemeMode.Default;
            }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var dto = new AppearanceDto { Theme = (int)_current };
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(dto, JsonOptions));
            }
            catch
            {
                // ignore persistence failure
            }
        }

        public void SetTheme(AppThemeMode mode)
        {
            if (_current == mode) return;

            _current = mode;
            Save();
            ThemeChanged?.Invoke(_current);
        }

        private sealed class AppearanceDto
        {
            public int Theme { get; set; }
        }
    }
}

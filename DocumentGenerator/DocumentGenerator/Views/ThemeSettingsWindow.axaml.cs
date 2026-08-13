using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DocumentGenerator.Models;
using DocumentGenerator.Services;
using System;

namespace DocumentGenerator
{
    public partial class ThemeSettingsWindow : Window
    {
        private readonly IThemeService _themeService;
        private readonly AppThemeMode _originalTheme;
        private bool _suppressRadioEvents;

        public ThemeSettingsWindow()
        {
            InitializeComponent();
        }

        public ThemeSettingsWindow(IThemeService themeService) : this()
        {
            _themeService = themeService;
            _originalTheme = themeService.CurrentTheme;

            Opened += (_, _) => ApplyTheme(_themeService.CurrentTheme);

            RadioDefault.IsCheckedChanged += (_, _) => OnRadioChanged(RadioDefault, AppThemeMode.Default);
            RadioColorful.IsCheckedChanged += (_, _) => OnRadioChanged(RadioColorful, AppThemeMode.Colorful);
            RadioSpace.IsCheckedChanged += (_, _) => OnRadioChanged(RadioSpace, AppThemeMode.Space);

            OkButton.Click += OkButton_Click;
            CancelButton.Click += CancelButton_Click;

            _themeService.ThemeChanged += OnThemeChanged;
            Closed += (_, _) => _themeService.ThemeChanged -= OnThemeChanged;

            SyncRadios(themeService.CurrentTheme);
            ApplyTheme(themeService.CurrentTheme);
        }

        private void OnThemeChanged(AppThemeMode mode) => ApplyTheme(mode);

        private void ApplyTheme(AppThemeMode mode)
        {
            AppWindowSetup.ApplyThemeClasses(this, mode);

            switch (mode)
            {
                case AppThemeMode.Colorful:
                    Background = Brush.Parse("#F0F9FF");
                    Foreground = Brush.Parse("#1E293B");
                    SettingsPanel.Background = Brush.Parse("#FFFFFF");
                    SettingsPanel.BorderBrush = Brush.Parse("#BAE6FD");
                    HeadingText.Foreground = Brush.Parse("#0F4C5C");
                    SubtitleText.Foreground = Brush.Parse("#334155");
                    RadioDefault.Foreground = Brush.Parse("#1E293B");
                    RadioColorful.Foreground = Brush.Parse("#1E293B");
                    RadioSpace.Foreground = Brush.Parse("#1E293B");
                    break;

                case AppThemeMode.Space:
                    Background = Brush.Parse("#0B1224");
                    Foreground = Brush.Parse("#E8F0FF");
                    SettingsPanel.Background = Brush.Parse("#283050");
                    SettingsPanel.BorderBrush = Brush.Parse("#6699CCE0");
                    HeadingText.Foreground = Brush.Parse("#F0F8FF");
                    SubtitleText.Foreground = Brush.Parse("#DCE8FF");
                    RadioDefault.Foreground = Brush.Parse("#F0F8FF");
                    RadioColorful.Foreground = Brush.Parse("#F0F8FF");
                    RadioSpace.Foreground = Brush.Parse("#F0F8FF");
                    break;

                default:
                    Background = Brush.Parse("#0F2A24");
                    Foreground = Brush.Parse("#E8F0F4");
                    SettingsPanel.Background = Brush.Parse("#163830");
                    SettingsPanel.BorderBrush = Brush.Parse("#2A6B5C");
                    HeadingText.Foreground = Brush.Parse("#F0F8F4");
                    SubtitleText.Foreground = Brush.Parse("#E8F0F4");
                    RadioDefault.Foreground = Brush.Parse("#E8F0F4");
                    RadioColorful.Foreground = Brush.Parse("#E8F0F4");
                    RadioSpace.Foreground = Brush.Parse("#E8F0F4");
                    break;
            }
        }

        private void OnRadioChanged(RadioButton radio, AppThemeMode mode)
        {
            if (_suppressRadioEvents || radio.IsChecked != true) return;
            ApplyTheme(mode);
            _themeService.SetTheme(mode);
        }

        private void SyncRadios(AppThemeMode mode)
        {
            _suppressRadioEvents = true;
            try
            {
                switch (mode)
                {
                    case AppThemeMode.Colorful:
                        RadioColorful.IsChecked = true;
                        break;
                    case AppThemeMode.Space:
                        RadioSpace.IsChecked = true;
                        break;
                    default:
                        RadioDefault.IsChecked = true;
                        break;
                }
            }
            finally
            {
                _suppressRadioEvents = false;
            }
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e) => Close();

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            _themeService.SetTheme(_originalTheme);
            Close();
        }
    }
}

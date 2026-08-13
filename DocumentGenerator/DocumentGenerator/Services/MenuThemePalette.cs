using Avalonia;
using Avalonia.Media;
using DocumentGenerator.Models;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Набор кистей и флагов для главного меню под выбранную тему.
    /// </summary>
    public sealed class MenuThemePalette
    {
        public required AppThemeMode Mode { get; init; }

        /// <summary>Анимировать ли градиент фона (Default, Colorful).</summary>
        public bool AnimateBackgroundGradient { get; init; }

        /// <summary>Плавающие частицы (Default, Colorful).</summary>
        public bool ShowParticles { get; init; }

        /// <summary>Звёзды и параллакс (Space).</summary>
        public bool ShowStarsAndParallax { get; init; }

        public required IBrush ButtonNormal { get; init; }
        public required IBrush ButtonHover { get; init; }
        public required IBrush ButtonPressed { get; init; }

        public required Color HeaderForeground { get; init; }
        public required Color ParticleColor { get; init; }
        public required Color DropShadowColor { get; init; }

        /// <summary>Фон окна: для космоса непрозрачный; иначе прозрачный.</summary>
        public Color WindowBackgroundOpaque { get; init; }

        /// <summary>Начальные цвета градиента фона (3 стопа).</summary>
        public Color[] BackgroundGradientStops { get; init; } = [];

        /// <summary>Конечные цвета для анимации градиента.</summary>
        public Color[] BackgroundGradientStopsAlt { get; init; } = [];

        public static MenuThemePalette ForMode(AppThemeMode mode) => mode switch
        {
            AppThemeMode.Colorful => BuildColorful(),
            AppThemeMode.Space => BuildSpace(),
            _ => BuildDefault()
        };

        private static LinearGradientBrush BtnGrad(Color top, Color bottom) => new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(top, 0),
                new GradientStop(bottom, 1)
            }
        };

        private static MenuThemePalette BuildDefault() => new()
        {
            Mode = AppThemeMode.Default,
            AnimateBackgroundGradient = true,
            ShowParticles = true,
            ShowStarsAndParallax = false,
            ButtonNormal = BtnGrad(Color.Parse("#2ed1b5"), Color.Parse("#0a5c4e")),
            ButtonHover = BtnGrad(Color.Parse("#3ef7d2"), Color.Parse("#1a9b82")),
            ButtonPressed = BtnGrad(Color.Parse("#0f7a6b"), Color.Parse("#063d2c")),
            HeaderForeground = Color.Parse("#f0f4f8"),
            ParticleColor = Color.Parse("#18ab9a"),
            DropShadowColor = Colors.Black,
            WindowBackgroundOpaque = Color.FromArgb(0, 0, 0, 0),
            // Как в первой версии меню: базовый тёмный градиент; анимация — лишь лёгкое «дыхание» без ухода в яркие оттенки
            BackgroundGradientStops =
            [
                Color.Parse("#063d2c"),
                Color.Parse("#125e47"),
                Color.Parse("#063d2c")
            ],
            BackgroundGradientStopsAlt =
            [
                Color.Parse("#052a22"),
                Color.Parse("#0f4e3f"),
                Color.Parse("#052a22")
            ]
        };

        private static MenuThemePalette BuildColorful() => new()
        {
            Mode = AppThemeMode.Colorful,
            AnimateBackgroundGradient = true,
            ShowParticles = true,
            ShowStarsAndParallax = false,
            ButtonNormal = BtnGrad(Color.Parse("#2dd4bf"), Color.Parse("#0d9488")),
            ButtonHover = BtnGrad(Color.Parse("#5eead4"), Color.Parse("#14b8a6")),
            ButtonPressed = BtnGrad(Color.Parse("#0f766e"), Color.Parse("#115e59")),
            HeaderForeground = Color.Parse("#0f4c5c"),
            ParticleColor = Color.Parse("#7dd3fc"),
            DropShadowColor = Color.Parse("#38bdf8"),
            WindowBackgroundOpaque = Color.FromArgb(0, 0, 0, 0),
            BackgroundGradientStops =
            [
                Color.Parse("#dbeafe"),
                Color.Parse("#ccfbf1"),
                Color.Parse("#fef9c3")
            ],
            BackgroundGradientStopsAlt =
            [
                Color.Parse("#e0f2fe"),
                Color.Parse("#d1fae5"),
                Color.Parse("#fde68a")
            ]
        };

        private static LinearGradientBrush SpaceGlassNormal() => new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(78, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(48, 130, 175, 255), 0.38),
                new GradientStop(Color.FromArgb(58, 55, 75, 140), 0.7),
                new GradientStop(Color.FromArgb(72, 35, 45, 95), 1)
            }
        };

        private static LinearGradientBrush SpaceGlassHover() => new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(95, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(62, 160, 200, 255), 0.4),
                new GradientStop(Color.FromArgb(68, 70, 95, 170), 0.72),
                new GradientStop(Color.FromArgb(80, 45, 55, 115), 1)
            }
        };

        private static LinearGradientBrush SpaceGlassPressed() => new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(55, 200, 220, 255), 0),
                new GradientStop(Color.FromArgb(50, 90, 120, 200), 0.5),
                new GradientStop(Color.FromArgb(75, 25, 30, 70), 1)
            }
        };

        private static MenuThemePalette BuildSpace() => new()
        {
            Mode = AppThemeMode.Space,
            AnimateBackgroundGradient = false,
            ShowParticles = false,
            ShowStarsAndParallax = true,
            WindowBackgroundOpaque = Color.Parse("#030712"),
            ButtonNormal = SpaceGlassNormal(),
            ButtonHover = SpaceGlassHover(),
            ButtonPressed = SpaceGlassPressed(),
            HeaderForeground = Color.Parse("#e8e9ff"),
            ParticleColor = Colors.White,
            DropShadowColor = Color.Parse("#0f172a"),
            BackgroundGradientStops =
            [
                Color.Parse("#020617"),
                Color.Parse("#0b1224"),
                Color.Parse("#020617")
            ],
            BackgroundGradientStopsAlt = []
        };
    }
}

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DocumentGenerator.Models;

namespace DocumentGenerator.Services
{
    /// <summary>
    /// Упрощённый космический фон для окон (градиент как в меню + статичные звёзды, без дыр и анимаций).
    /// </summary>
    public static class SpaceWindowChrome
    {
        private const int StarCount = 44;

        private sealed class State
        {
            public IBrush? OriginalBackground;
            public IBrush? OriginalForeground;
            public object? OriginalContent;
            public Grid? Wrapper;
            public Canvas? StarCanvas;
            public EventHandler<SizeChangedEventArgs>? SizeHandler;
            public Action<AppThemeMode>? ThemeHandler;
            public double LastStarW = -9999;
            public double LastStarH = -9999;
        }

        /// <summary>Подключает реакцию на тему. Главное меню (MenuWindow) не подключает — у него свой слой.</summary>
        public static void Attach(Window window, IThemeService themeService)
        {
            if (window.Tag is State)
                return;

            var state = new State
            {
                OriginalBackground = window.Background,
                OriginalForeground = window.Foreground,
                OriginalContent = window.Content
            };
            window.Tag = state;

            void OnTheme(AppThemeMode _)
            {
                Dispatcher.UIThread.Post(() => Sync(window, themeService, state));
            }

            state.ThemeHandler = OnTheme;
            themeService.ThemeChanged += state.ThemeHandler;
            window.Closed += OnWindowClosed;

            void OnWindowClosed(object? _, EventArgs __)
            {
                if (state.ThemeHandler != null)
                    themeService.ThemeChanged -= state.ThemeHandler;
                DetachVisual(window, state);
                window.Closed -= OnWindowClosed;
                window.Tag = null;
            }

            void RunInitialSync(object? _, RoutedEventArgs __)
            {
                window.Loaded -= RunInitialSync;
                Sync(window, themeService, state);
            }

            if (window.IsLoaded)
                Sync(window, themeService, state);
            else
                window.Loaded += RunInitialSync;
        }

        private const string SpaceHoloUiClass = "space-holo-ui";
        private const string LightUiClass = "light-ui";

        private static void Sync(Window window, IThemeService themeService, State state)
        {
            var mode = themeService.CurrentTheme;
            var space = mode == AppThemeMode.Space;
            var light = mode == AppThemeMode.Colorful;

            window.Classes.Remove(LightUiClass);
            if (!space)
                window.Classes.Remove(SpaceHoloUiClass);

            if (!space && !light)
            {
                DetachVisual(window, state);
                return;
            }

            var wrapperIsSpace = state.StarCanvas != null;
            if (state.Wrapper != null && wrapperIsSpace != space)
                DetachVisual(window, state);

            if (space)
            {
                if (!window.Classes.Contains(SpaceHoloUiClass))
                    window.Classes.Add(SpaceHoloUiClass);

                if (state.Wrapper != null)
                {
                    MaybeRefillStars(window, state);
                    return;
                }

                if (state.OriginalContent is not Control content)
                    return;

                var palette = MenuThemePalette.ForMode(AppThemeMode.Space);
                window.Foreground = new SolidColorBrush(palette.HeaderForeground);

                window.Content = null;

                var host = new Grid();
                var gradient = new Border
                {
                    Background = CreateGradientBrush(palette),
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                var stars = new Canvas
                {
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                host.Children.Add(gradient);
                host.Children.Add(stars);
                host.Children.Add(content);

                window.Background = Brushes.Transparent;
                window.Content = host;
                state.Wrapper = host;
                state.StarCanvas = stars;

                state.SizeHandler = (_, _) => MaybeRefillStars(window, state);
                window.SizeChanged += state.SizeHandler;

                window.Opened += OnOpenedRefill;
                void OnOpenedRefill(object? o, EventArgs e)
                {
                    window.Opened -= OnOpenedRefill;
                    state.LastStarW = -9999;
                    RefillStars(state.StarCanvas, window.Bounds.Width, window.Bounds.Height);
                    state.LastStarW = window.Bounds.Width;
                    state.LastStarH = window.Bounds.Height;
                }

                RefillStars(stars, window.Bounds.Width, window.Bounds.Height);
                state.LastStarW = window.Bounds.Width;
                state.LastStarH = window.Bounds.Height;
            }
            else
            {
                if (!window.Classes.Contains(LightUiClass))
                    window.Classes.Add(LightUiClass);

                if (state.Wrapper != null)
                {
                    if (state.Wrapper.Children[0] is Border existingGradient)
                        existingGradient.Background = CreateGradientBrush(MenuThemePalette.ForMode(AppThemeMode.Colorful));
                    return;
                }

                if (state.OriginalContent is not Control content)
                    return;

                var palette = MenuThemePalette.ForMode(AppThemeMode.Colorful);
                window.Foreground = new SolidColorBrush(palette.HeaderForeground);

                window.Content = null;

                var host = new Grid();
                var gradient = new Border
                {
                    Background = CreateGradientBrush(palette),
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                var ambience = CreateLightAmbienceCanvas();

                host.Children.Add(gradient);
                host.Children.Add(ambience);
                host.Children.Add(content);

                window.Background = Brushes.Transparent;
                window.Content = host;
                state.Wrapper = host;
                state.StarCanvas = null;

                state.SizeHandler = (_, _) => LayoutLightAmbience(ambience, window.Bounds.Width, window.Bounds.Height);
                window.SizeChanged += state.SizeHandler;
                LayoutLightAmbience(ambience, window.Bounds.Width, window.Bounds.Height);
            }
        }

        private static Canvas CreateLightAmbienceCanvas()
        {
            var canvas = new Canvas
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            canvas.Children.Add(new Ellipse
            {
                Width = 320,
                Height = 320,
                Fill = new SolidColorBrush(Color.FromArgb(38, 56, 189, 248)),
                Opacity = 0.9
            });
            canvas.Children.Add(new Ellipse
            {
                Width = 260,
                Height = 260,
                Fill = new SolidColorBrush(Color.FromArgb(32, 45, 212, 191)),
                Opacity = 0.85
            });
            canvas.Children.Add(new Ellipse
            {
                Width = 220,
                Height = 220,
                Fill = new SolidColorBrush(Color.FromArgb(28, 251, 191, 36)),
                Opacity = 0.8
            });

            return canvas;
        }

        private static void LayoutLightAmbience(Canvas canvas, double w, double h)
        {
            if (w < 4 || h < 4 || canvas.Children.Count < 3)
                return;

            canvas.Width = w;
            canvas.Height = h;

            Canvas.SetLeft(canvas.Children[0], w * 0.62);
            Canvas.SetTop(canvas.Children[0], h * 0.04);
            Canvas.SetLeft(canvas.Children[1], w * 0.04);
            Canvas.SetTop(canvas.Children[1], h * 0.52);
            Canvas.SetLeft(canvas.Children[2], w * 0.48);
            Canvas.SetTop(canvas.Children[2], h * 0.68);
        }

        private static void DetachVisual(Window window, State state)
        {
            window.Classes.Remove(SpaceHoloUiClass);
            window.Classes.Remove(LightUiClass);

            if (state.SizeHandler != null)
                window.SizeChanged -= state.SizeHandler;
            state.SizeHandler = null;

            if (state.Wrapper != null)
            {
                if (state.OriginalContent is Control c)
                {
                    state.Wrapper.Children.Remove(c);
                    window.Content = null;
                    window.Content = c;
                }
                else
                    window.Content = state.OriginalContent;

                state.Wrapper = null;
                state.StarCanvas = null;
            }

            window.Background = state.OriginalBackground;
            window.Foreground = state.OriginalForeground;
            state.LastStarW = -9999;
            state.LastStarH = -9999;
        }

        private static void MaybeRefillStars(Window window, State state)
        {
            var w = window.Bounds.Width;
            var h = window.Bounds.Height;
            if (w < 4 || h < 4)
                return;
            if (Math.Abs(w - state.LastStarW) < 20 && Math.Abs(h - state.LastStarH) < 20)
                return;
            RefillStars(state.StarCanvas, w, h);
            state.LastStarW = w;
            state.LastStarH = h;
        }

        private static IBrush CreateGradientBrush(MenuThemePalette palette)
        {
            var stops = palette.BackgroundGradientStops;
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
            };
            if (stops.Length >= 3)
            {
                brush.GradientStops.Add(new GradientStop(stops[0], 0));
                brush.GradientStops.Add(new GradientStop(stops[1], 0.5));
                brush.GradientStops.Add(new GradientStop(stops[2], 1));
            }
            else if (stops.Length > 0)
                brush.GradientStops.Add(new GradientStop(stops[0], 0));

            return brush;
        }

        private static void RefillStars(Canvas? canvas, double w, double h)
        {
            if (canvas == null || w < 4 || h < 4)
                return;

            canvas.Width = w;
            canvas.Height = h;
            canvas.Children.Clear();

            var rnd = Random.Shared;
            for (var i = 0; i < StarCount; i++)
            {
                var sz = 1.0 + rnd.NextDouble() * 1.35;
                var e = new Ellipse
                {
                    Width = sz,
                    Height = sz,
                    Fill = Brushes.White,
                    Opacity = 0.22 + rnd.NextDouble() * 0.5
                };
                Canvas.SetLeft(e, rnd.NextDouble() * Math.Max(1, w - sz));
                Canvas.SetTop(e, rnd.NextDouble() * Math.Max(1, h - sz));
                canvas.Children.Add(e);
            }
        }
    }
}

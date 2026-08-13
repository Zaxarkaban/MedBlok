using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using DocumentGenerator.Models;

namespace DocumentGenerator.Services
{
    public enum AppDialogKind
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Единые стилизованные диалоги для всех тем приложения.
    /// </summary>
    public static class AppDialog
    {
        public static Task ShowAsync(Window? owner, string message, string title, AppDialogKind kind = AppDialogKind.Info)
            => ShowAsync(owner, message, title, kind, primaryText: "OK");

        public static async Task<bool> ConfirmAsync(Window? owner, string message, string title, string confirmText = "Да", string cancelText = "Отмена")
        {
            var dialog = CreateShell(owner, title);
            bool? result = null;

            var content = BuildContent(message, title, AppDialogKind.Warning, confirmText, () =>
            {
                result = true;
                dialog.Close();
            }, cancelText, () =>
            {
                result = false;
                dialog.Close();
            });

            dialog.Content = content;
            await ShowDialogInternal(dialog, owner);
            return result == true;
        }

        public static async Task ShowAsync(Window? owner, string message, string title, AppDialogKind kind, string primaryText)
        {
            var dialog = CreateShell(owner, title);
            var content = BuildContent(message, title, kind, primaryText, () => dialog.Close());
            dialog.Content = content;
            await ShowDialogInternal(dialog, owner);
        }

        private static async Task ShowDialogInternal(Window dialog, Window? owner)
        {
            var resolvedOwner = AppWindowSetup.ResolveOwner(owner);
            if (resolvedOwner != null)
            {
                await dialog.ShowDialog(resolvedOwner);
                return;
            }

            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var tcs = new TaskCompletionSource();
            dialog.Closed += (_, _) => tcs.TrySetResult();
            dialog.Show();
            AppWindowSetup.CenterOnScreen(dialog);
            await tcs.Task;
        }

        private static Window CreateShell(Window? owner, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Classes = { "app-dialog" },
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.BorderOnly,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            AppWindowSetup.ConfigureDialog(dialog, owner);
            return dialog;
        }

        private static Control BuildContent(
            string message,
            string title,
            AppDialogKind kind,
            string primaryText,
            Action onPrimary,
            string? secondaryText = null,
            Action? onSecondary = null)
        {
            var (accent, glyph) = KindVisuals(kind);

            var accentBar = new Border
            {
                Width = 5,
                MinWidth = 5,
                Background = new SolidColorBrush(accent),
                CornerRadius = new CornerRadius(4, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(accentBar, 0);

            var icon = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.FromArgb(42, accent.R, accent.G, accent.B)),
                Child = new TextBlock
                {
                    Text = glyph,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(accent),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                Classes = { "app-dialog-title" },
                FontSize = 17,
                FontWeight = FontWeight.SemiBold
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                Classes = { "app-dialog-message" },
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 21,
                MaxWidth = 440
            };

            var messageScroll = new ScrollViewer
            {
                MaxHeight = 260,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = messageBlock,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var primary = new Button
            {
                Content = primaryText,
                Classes = { "app-dialog-primary" },
                MinWidth = 120,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true
            };
            primary.Click += (_, _) => onPrimary();

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0),
                Children = { primary }
            };

            if (secondaryText != null && onSecondary != null)
            {
                var secondary = new Button
                {
                    Content = secondaryText,
                    Classes = { "app-dialog-secondary" },
                    MinWidth = 110
                };
                secondary.Click += (_, _) => onSecondary();
                buttons.Children.Insert(0, secondary);
            }

            var textStack = new StackPanel
            {
                Margin = new Thickness(12, 2, 4, 0),
                Spacing = 4,
                Children = { titleBlock, messageScroll }
            };
            Grid.SetColumn(textStack, 1);

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Thickness(0, 0, 0, 10),
                Children = { icon, textStack }
            };

            var body = new Border
            {
                Classes = { "app-dialog-body" },
                Padding = new Thickness(18, 16, 18, 16),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = new StackPanel
                {
                    Spacing = 0,
                    Children = { header, buttons }
                }
            };
            Grid.SetColumn(body, 1);

            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Children =
                {
                    accentBar,
                    body
                }
            };
        }

        private static (Color accent, string glyph) KindVisuals(AppDialogKind kind) => kind switch
        {
            AppDialogKind.Success => (Color.Parse("#10B981"), "✓"),
            AppDialogKind.Warning => (Color.Parse("#F59E0B"), "!"),
            AppDialogKind.Error => (Color.Parse("#EF4444"), "✕"),
            _ => (Color.Parse("#0EA5E9"), "i")
        };
    }
}

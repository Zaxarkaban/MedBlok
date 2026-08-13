using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace DocumentGenerator.Services
{
    public sealed class ExportProgressInfo
    {
        public string Message { get; init; } = "";
        public int Current { get; init; }
        public int Total { get; init; }
    }

    /// <summary>
    /// Окно прогресса выгрузки в духе забавных подсказок Terraria.
    /// </summary>
    public sealed class AppExportProgress : Window
    {
        private static readonly string[] FunLines =
        [
            "Заполняем поле «ФИО»...",
            "Подгоняем размер шрифта...",
            "Сверяемся с приказом Минздрава...",
            "Уговариваем PDF не ломаться...",
            "Считаем врачей и исследования...",
            "Проверяем, что СНИЛС на месте...",
            "Разминаем клавишу Enter...",
            "Настраиваем межстрочный интервал...",
            "Переписываем дату рождения в нужный формат...",
            "Упаковываем документы в папку...",
            "Почти готово, не закрывайте окно...",
            "Документы почти сформированы..."
        ];

        private readonly TextBlock _titleText;
        private readonly TextBlock _statusText;
        private readonly ProgressBar _progressBar;
        private readonly TextBlock _counterText;
        private readonly DispatcherTimer _funTimer;
        private int _funIndex;
        private int _total = 1;
        private bool _hasRealStatus;

        private AppExportProgress()
        {
            Title = "Выгрузка документов";
            Classes.Add("app-dialog");
            Classes.Add("export-progress");
            Width = 460;
            MinHeight = 210;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.Parse("#0F2A24"));
            Foreground = new SolidColorBrush(Color.Parse("#E8F0F4"));

            _titleText = new TextBlock
            {
                Text = "⛏ Идёт выгрузка в PDF",
                FontSize = 18,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#F0F8F4"))
            };

            _statusText = new TextBlock
            {
                Text = FunLines[0],
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 20,
                MinHeight = 48,
                Foreground = new SolidColorBrush(Color.Parse("#C5E8DC"))
            };

            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Height = 22,
                Margin = new Thickness(0, 12, 0, 8),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.Parse("#0A322A")),
                Foreground = new SolidColorBrush(Color.Parse("#2ED1B5"))
            };

            _counterText = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#9BB5AD"))
            };

            Content = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#163830")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2A6B5C")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(22, 18),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children = { _titleText, _statusText, _progressBar, _counterText }
                }
            };

            _funTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
            _funTimer.Tick += (_, _) => RotateFunLine();
        }

        public static async Task RunAsync(Window owner, int totalItems, Func<IProgress<ExportProgressInfo>, Task> work)
        {
            var dlg = new AppExportProgress();
            dlg._total = Math.Max(1, totalItems);
            dlg._counterText.Text = $"0 из {dlg._total}";
            AppWindowSetup.ConfigureDialog(dlg, owner);
            ApplyThemeColors(dlg, owner);

            Exception? error = null;
            var progress = new Progress<ExportProgressInfo>(dlg.Report);

            dlg.Opened += async (_, _) =>
            {
                dlg._funTimer.Start();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Task.Yield();

                try
                {
                    PdfFontHelper.Warmup();
                    await Task.Run(async () => await work(progress).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    dlg._funTimer.Stop();
                    dlg.Close();
                }
            };

            await dlg.ShowDialog(owner);

            if (error != null)
                throw error;
        }

        private static void ApplyThemeColors(AppExportProgress dlg, Window owner)
        {
            if (owner.Classes.Contains("light-ui"))
            {
                dlg.Background = new SolidColorBrush(Color.Parse("#F0F9FF"));
                if (dlg.Content is Border border)
                {
                    border.Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
                    border.BorderBrush = new SolidColorBrush(Color.Parse("#BAE6FD"));
                }

                dlg._titleText.Foreground = new SolidColorBrush(Color.Parse("#0F4C5C"));
                dlg._statusText.Foreground = new SolidColorBrush(Color.Parse("#475569"));
                dlg._counterText.Foreground = new SolidColorBrush(Color.Parse("#64748B"));
                dlg._progressBar.Background = new SolidColorBrush(Color.Parse("#E0F2FE"));
                dlg._progressBar.Foreground = new SolidColorBrush(Color.Parse("#0D9488"));
            }
            else if (owner.Classes.Contains("space-holo-ui"))
            {
                dlg.Background = new SolidColorBrush(Color.Parse("#0B1224"));
                if (dlg.Content is Border border)
                {
                    border.Background = new SolidColorBrush(Color.Parse("#283050"));
                    border.BorderBrush = new SolidColorBrush(Color.Parse("#6699CCE0"));
                }

                dlg._titleText.Foreground = new SolidColorBrush(Color.Parse("#F0F8FF"));
                dlg._statusText.Foreground = new SolidColorBrush(Color.Parse("#C8D8F0"));
                dlg._counterText.Foreground = new SolidColorBrush(Color.Parse("#A8C0E8"));
                dlg._progressBar.Background = new SolidColorBrush(Color.Parse("#1A2848"));
                dlg._progressBar.Foreground = new SolidColorBrush(Color.Parse("#66C8F8"));
            }
        }

        private void Report(ExportProgressInfo info)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrWhiteSpace(info.Message))
                {
                    _statusText.Text = info.Message;
                    _hasRealStatus = true;
                }

                if (info.Total > 0)
                    _total = info.Total;

                var current = Math.Clamp(info.Current, 0, _total);
                _progressBar.Value = _total > 0 ? current * 100.0 / _total : 0;
                _counterText.Text = $"{current} из {_total}";
            }, DispatcherPriority.Normal);
        }

        private void RotateFunLine()
        {
            if (_hasRealStatus)
                return;

            _funIndex = (_funIndex + 1) % FunLines.Length;
            _statusText.Text = FunLines[_funIndex];
        }
    }
}

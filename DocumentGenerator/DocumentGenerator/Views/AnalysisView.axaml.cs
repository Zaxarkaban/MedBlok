using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia;
using DocumentGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Commons.Bouncycastle.Asn1.X509;
using System.Collections;
using System.Collections.ObjectModel;


namespace DocumentGenerator
{
    public partial class AnalysisView : Window
    {
        private readonly List<ColumnData> _columns = new List<ColumnData>();
        
      

        public AnalysisView(IServiceProvider provider)
        {
            InitializeComponent();
           
                AddColumnButton.Click += AddColumnButton_Click;
            
            AddNewColumn(); // Первый столбец по умолчанию
        }

        private void AddColumnButton_Click(object? sender, RoutedEventArgs e)
        {
            AddNewColumn();
            CreateRippleEffect(sender as Button);
        }

        private void RemoveColumnButton_Click(object? sender, RoutedEventArgs e, ColumnData columnData)
        {
            var grid = this.FindControl<Grid>("AnalisGrid");
            if (grid == null) return;

            // Проверяем, существует ли columnData в _columns
            if (!_columns.Contains(columnData)) return;

            // Отключаем кнопку, чтобы избежать повторных нажатий
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }

            // Удаляем StackPanel из Grid
            grid.Children.Remove(columnData.StackPanel);
            // Удаляем columnData из _columns
            _columns.Remove(columnData);

            // Перестраиваем ColumnDefinitions и индексы столбцов
            grid.ColumnDefinitions.Clear();
            grid.Children.Clear(); // Очищаем Grid.Children для синхронизации

            // Заново добавляем оставшиеся столбцы
            for (int i = 0; i < _columns.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(_columns[i].StackPanel, i);
                grid.Children.Add(_columns[i].StackPanel);
            }
        }

        private void AddNewColumn()
        {
            var grid = this.FindControl<Grid>("AnalisGrid");
            if (grid == null) return;
            int columnIndex = _columns.Count;

            // Новый столбец
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Элементы управления
            var items = new ObservableCollection<string>(Dictionaries.OrderClauseDataMap.Keys);
            var selectedItems = new ObservableCollection<string>(); 

            var listBox = new ListBox
            {
                Name = $"ListBox_{columnIndex}", // Уникальное имя для каждого ListBox
                ItemsSource = items,
                SelectionMode = SelectionMode.Multiple,
                // SelectedItems привязан к локальной коллекции
            };

            

            var removeButton = new Button { Content = "–" };
            var menUnder40TextBox = new TextBox { Watermark = "Мужчины <40" };
            var menOver40TextBox = new TextBox { Watermark = "Мужчины >40" };
            var womenUnder40TextBox = new TextBox { Watermark = "Женщины <40" };
            var womenOver40TextBox = new TextBox { Watermark = "Женщины >40" };
            var outputTextBlock = new TextBlock { Classes = { "output" } };

            // Создаём columnData до использования в лямбда-выражениях
            var columnData = new ColumnData
            {
                StackPanel = new StackPanel(),
                ListBox = listBox,
                Items = items,
                SelectedItems = selectedItems,
                MenUnder40TextBox = menUnder40TextBox,
                MenOver40TextBox = menOver40TextBox,
                WomenUnder40TextBox = womenUnder40TextBox,
                WomenOver40TextBox = womenOver40TextBox,
                OutputTextBlock = outputTextBlock,
                RemoveButton = removeButton
            };

            // Синхронизируем SelectedItems с нашей коллекцией
            listBox.SelectionChanged += (s, e) =>
            {
                selectedItems.Clear();
                foreach (var item in listBox.SelectedItems?.Cast<string>() ?? new List<string>())
                {
                    selectedItems.Add(item);
                }
                columnData.UpdateOutput();
            };

            // Валидация ввода
            void ValidateTextBox(TextBox textBox, TextChangedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text) && (!int.TryParse(textBox.Text, out int value) || value < 0))
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await MessageBox.Show(this, "Пожалуйста, введите положительное целое число.", "Ошибка ввода", MessageBox.MessageBoxButtons.Ok);
                        textBox.Text = "0";
                    });
                }
                columnData.UpdateOutput();
            }

            menUnder40TextBox.TextChanged += (s, e) => ValidateTextBox(menUnder40TextBox, e);
            menOver40TextBox.TextChanged += (s, e) => ValidateTextBox(menOver40TextBox, e);
            womenUnder40TextBox.TextChanged += (s, e) => ValidateTextBox(womenUnder40TextBox, e);
            womenOver40TextBox.TextChanged += (s, e) => ValidateTextBox(womenOver40TextBox, e);


            // Размещение элементов

            columnData.StackPanel = new StackPanel
            {
                Classes = { "column" },
                Children = { removeButton, listBox, menUnder40TextBox, menOver40TextBox, womenUnder40TextBox, womenOver40TextBox, outputTextBlock }
            };
            Grid.SetColumn(columnData.StackPanel, columnIndex);
            grid.Children.Add(columnData.StackPanel);

            // Анимация появления
            columnData.StackPanel.Opacity = 0;
            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(10); // Небольшая задержка для рендеринга
                columnData.StackPanel.Opacity = 1;
            });

            

            removeButton.Click += (s, e) => RemoveColumnButton_Click(s, e, columnData);

            _columns.Add(columnData);
        }

        private void CreateRippleEffect(Button? button)
        {
            if (button == null) return;

            var ripple = new Ellipse
            {
                Classes = { "ripple" },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var panel = button.Content as Panel;
            if (panel != null)
            {
                panel.Children.Add(ripple);

                ripple.Width = 100;
                ripple.Height = 100;
                ripple.Opacity = 0;

                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(500);
                    panel.Children.Remove(ripple);
                });
            }
        }

        private class ColumnData
        {
            public StackPanel StackPanel { get; set; }
            public Button RemoveButton { get; set; }
            public ListBox ListBox { get; set; }
            public TextBox MenUnder40TextBox { get; set; }
            public TextBox MenOver40TextBox { get; set; }
            public TextBox WomenUnder40TextBox { get; set; }
            public TextBox WomenOver40TextBox { get; set; }
            public TextBlock OutputTextBlock { get; set; }
            public ObservableCollection<string> Items { get; set; } // Добавлено для хранения элементов
            public ObservableCollection<string> SelectedItems { get; set; } // Добавлено для хранения выбранных элементов

            public void UpdateOutput()
            {
                var selectedClauses = SelectedItems.ToList();
                int menUnder40 = int.TryParse(MenUnder40TextBox.Text, out int m1) ? m1 : 0;
                int menOver40 = int.TryParse(MenOver40TextBox.Text, out int m2) ? m2 : 0;
                int womenUnder40 = int.TryParse(WomenUnder40TextBox.Text, out int w1) ? w1 : 0;
                int womenOver40 = int.TryParse(WomenOver40TextBox.Text, out int w2) ? w2 : 0;
                int totalPeople = menUnder40 + menOver40 + womenUnder40 + womenOver40;

                // Уникальные врачи и анализы
                var uniqueDoctors = new HashSet<string>();
                var uniqueTests = new HashSet<string>();

                foreach (var clause in selectedClauses)
                {
                    var data = Dictionaries.OrderClauseDataMap[clause];
                    uniqueDoctors.UnionWith(data.Doctors);
                    uniqueTests.UnionWith(data.Tests);
                }

                // Дополнительные анализы
                if (womenUnder40 > 0)
                {
                    uniqueTests.Add("Бактериологическое (на флору) и цитологическое (на атипичные клетки) исследования");
                    uniqueTests.Add("Ультразвуковое исследование органов малого таза");
                }
                if (womenOver40 > 0)
                {
                    uniqueTests.Add("Бактериологическое (на флору) и цитологическое (на атипичные клетки) исследования");
                    uniqueTests.Add("Ультразвуковое исследование органов малого таза");
                    uniqueTests.Add("Маммография обеих молочных желез в двух проекциях");
                }
                if (menOver40 > 0)
                {
                    uniqueTests.Add("Измерение внутриглазного давления");
                }

                // Формирование вывода
                var output = $"Уникальных анализов: {uniqueTests.Count}\n" +
                             $"Анализы: {string.Join(", ", uniqueTests)}\n" +
                             $"Уникальных врачей: {uniqueDoctors.Count}\n" +
                             $"Врачи:\n{string.Join("\n", uniqueDoctors.Select(d => $"- {d}: {totalPeople} посещений"))}";
                OutputTextBlock.Text = output;
            }
        }

        // Вспомогательный класс для показа MessageBox
        private static class MessageBox
        {
            public static Task Show(Window owner, string message, string title, MessageBoxButtons buttons)
            {
                var dialog = new Window
                {
                    Title = title,
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(10),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };

                okButton.Click += (s, e) => dialog.Close();

                var stackPanel = new StackPanel
                {
                    Children = { textBlock, okButton }
                };

                dialog.Content = stackPanel;

                return dialog.ShowDialog(owner);
            }

            public enum MessageBoxButtons
            {
                Ok
            }
        }
    }
}

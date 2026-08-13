using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DocumentGenerator.Models;
using DocumentGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;

namespace DocumentGenerator
{
    public partial class AnalysisView : Window
    {
        private readonly List<ColumnData> _columns = new List<ColumnData>();
        private readonly IServiceProvider _serviceProvider;

        public AnalysisView(IServiceProvider provider)
        {
            _serviceProvider = provider;
            InitializeComponent();
            AppWindowSetup.Configure(this, provider);
            if (this.FindControl<Button>("AddColumnButton") is Button addColumnButton)
                addColumnButton.Click += AddColumnButton_Click;
            if (this.FindControl<Button>("ExportToExcelButton") is Button exportButton)
                exportButton.Click += ExportToExcelButton_Click;
            if (this.FindControl<Button>("BackToMenuButton") is Button backButton)
                backButton.Click += BackToMenu_Click;
            AddNewColumn();
        }

        private void BackToMenu_Click(object? sender, RoutedEventArgs e)
        {
            var menu = _serviceProvider.GetRequiredService<MenuWindow>();
            menu.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            menu.Show();
            Close();
        }

        private async void ExportToExcelButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Устанавливаем лицензию EPPlus (для некоммерческого использования)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Суммируем данные из всех колонок
                var doctorVisits = new Dictionary<string, int>();
                var testCounts = new Dictionary<string, int>();

                foreach (var column in _columns)
                {
                    // Получаем данные из OutputTextBlock
                    var outputLines = column.OutputTextBlock.Text?.Split('\n') ?? Array.Empty<string>();
                    bool isTestsSection = false;
                    bool isDoctorsSection = false;

                    foreach (var line in outputLines)
                    {
                        if (line.StartsWith("Исследования:"))
                        {
                            isTestsSection = true;
                            isDoctorsSection = false;
                            continue;
                        }
                        else if (line.StartsWith("Врачи:"))
                        {
                            isTestsSection = false;
                            isDoctorsSection = true;
                            continue;
                        }

                        if (isTestsSection && line.StartsWith("- "))
                        {
                            var parts = line.Substring(2).Split(": ");
                            if (parts.Length == 2 && int.TryParse(parts[1].Replace(" раз", ""), out int count))
                            {
                                var testName = parts[0];
                                if (!testCounts.ContainsKey(testName))
                                {
                                    testCounts[testName] = 0;
                                }
                                testCounts[testName] += count;
                            }
                        }
                        else if (isDoctorsSection && line.StartsWith("- "))
                        {
                            var parts = line.Substring(2).Split(": ");
                            if (parts.Length == 2 && int.TryParse(parts[1].Replace(" посещений", ""), out int count))
                            {
                                var doctorName = parts[0];
                                if (!doctorVisits.ContainsKey(doctorName))
                                {
                                    doctorVisits[doctorName] = 0;
                                }
                                doctorVisits[doctorName] += count;
                            }
                        }
                    }
                }

                // Создаём Excel-файл
                using var package = new ExcelPackage();

                // Лист для врачей
                var doctorsSheet = package.Workbook.Worksheets.Add("Врачи");
                doctorsSheet.Cells[1, 1].Value = "Врач";
                doctorsSheet.Cells[1, 2].Value = "Посещения";
                int row = 2;
                foreach (var kvp in doctorVisits.OrderBy(x => x.Key))
                {
                    doctorsSheet.Cells[row, 1].Value = kvp.Key;
                    doctorsSheet.Cells[row, 2].Value = kvp.Value;
                    row++;
                }
                doctorsSheet.Cells.AutoFitColumns();

                // Лист для исследований
                var testsSheet = package.Workbook.Worksheets.Add("Исследования");
                testsSheet.Cells[1, 1].Value = "Врач";
                testsSheet.Cells[1, 2].Value = "Исследование";
                testsSheet.Cells[1, 3].Value = "Количество";
                row = 2;

                // Получаем словарь DoctorTestsMap
                var doctorTestsMap = Dictionaries.DoctorTestsMap;

                // Группируем исследования по врачам
                var testsByDoctor = new Dictionary<string, Dictionary<string, int>>();
                var otherTests = new Dictionary<string, int>();

                foreach (var test in testCounts)
                {
                    bool assigned = false;
                    foreach (var doctor in doctorTestsMap)
                    {
                        if (doctor.Value.Contains(test.Key))
                        {
                            if (!testsByDoctor.ContainsKey(doctor.Key))
                            {
                                testsByDoctor[doctor.Key] = new Dictionary<string, int>();
                            }
                            testsByDoctor[doctor.Key][test.Key] = test.Value;
                            assigned = true;
                            break;
                        }
                    }
                    if (!assigned)
                    {
                        otherTests[test.Key] = test.Value;
                    }
                }

                // Записываем исследования по врачам
                foreach (var doctor in testsByDoctor.OrderBy(d => d.Key))
                {
                    testsSheet.Cells[row, 1].Value = doctor.Key;
                    row++;
                    foreach (var test in doctor.Value.OrderBy(t => t.Key))
                    {
                        testsSheet.Cells[row, 2].Value = test.Key;
                        testsSheet.Cells[row, 3].Value = test.Value;
                        row++;
                    }
                }

                // Записываем исследования, не относящиеся к врачам
                if (otherTests.Any())
                {
                    testsSheet.Cells[row, 1].Value = "Другие исследования";
                    row++;
                    foreach (var test in otherTests.OrderBy(t => t.Key))
                    {
                        testsSheet.Cells[row, 2].Value = test.Key;
                        testsSheet.Cells[row, 3].Value = test.Value;
                        row++;
                    }
                }

                testsSheet.Cells.AutoFitColumns();

                // Сохраняем файл через диалог
                var saveFileDialog = new SaveFileDialog
                {
                    DefaultExtension = "xlsx",
                    InitialFileName = "MedicalAnalysisReport.xlsx",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter
                        {
                            Name = "Excel Files",
                            Extensions = new List<string> { "xlsx" }
                        }
                    }
                };

                var result = await saveFileDialog.ShowAsync(this);
                if (!string.IsNullOrEmpty(result))
                {
                    using var stream = new FileStream(result, FileMode.Create, FileAccess.Write);
                    package.SaveAs(stream);
                    await AppDialog.ShowAsync(this, "Файл успешно сохранён!", "Успех", AppDialogKind.Success);
                }
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", AppDialogKind.Error);
            }
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

            if (!_columns.Contains(columnData)) return;

            if (sender is Button button)
            {
                button.IsEnabled = false;
            }

            grid.Children.Remove(columnData.ColumnBorder);
            _columns.Remove(columnData);

            grid.ColumnDefinitions.Clear();
            grid.Children.Clear();

            for (int i = 0; i < _columns.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(_columns[i].ColumnBorder, i);
                grid.Children.Add(_columns[i].ColumnBorder);
            }
        }

        private void AddNewColumn()
        {
            var grid = this.FindControl<Grid>("AnalisGrid");
            if (grid == null) return;

            int columnIndex = _columns.Count;

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var allClauses = Dictionaries.OrderClauseDataMap.Keys.ToList();
            var items = new ObservableCollection<string>(allClauses);
            var selectedItems = new ObservableCollection<string>();

            var searchBox = new TextBox
            {
                Name = $"ClauseSearch_{columnIndex}",
                Watermark = "Поиск пунктов (например 1.1 или п.4.1)..."
            };

            var listBox = new ListBox
            {
                Name = $"ListBox_{columnIndex}",
                ItemsSource = items,
                SelectionMode = SelectionMode.Multiple,
                MinHeight = 220,
                MaxHeight = 340,
                Width = 260
            };

            var selectedHint = new TextBlock
            {
                Text = "Выбрано: 0",
                Classes = { "column-label" },
                Margin = new Thickness(0, 0, 0, 4),
                Opacity = 0.85
            };

            var removeButton = new Button
            {
                Content = "–",
                Classes = { "remove-btn" }
            };
            var clausesLabel = new TextBlock
            {
                Text = "Пункты приказа",
                Classes = { "column-label" }
            };
            var peopleLabel = new TextBlock
            {
                Text = "Численность",
                Classes = { "column-label" },
                Margin = new Thickness(0, 4, 0, 6)
            };
            var menUnder40TextBox = new TextBox { Watermark = "Мужчины <40" };
            var menOver40TextBox = new TextBox { Watermark = "Мужчины >40" };
            var womenUnder40TextBox = new TextBox { Watermark = "Женщины <40" };
            var womenOver40TextBox = new TextBox { Watermark = "Женщины >40" };
            var outputTextBlock = new TextBlock { Classes = { "output" } };

            var columnData = new ColumnData
            {
                ListBox = listBox,
                Items = items,
                AllClauses = allClauses,
                SelectedItems = selectedItems,
                SearchBox = searchBox,
                SelectedHint = selectedHint,
                MenUnder40TextBox = menUnder40TextBox,
                MenOver40TextBox = menOver40TextBox,
                WomenUnder40TextBox = womenUnder40TextBox,
                WomenOver40TextBox = womenOver40TextBox,
                OutputTextBlock = outputTextBlock,
                RemoveButton = removeButton
            };

            void UpdateSelectedHint()
            {
                selectedHint.Text = $"Выбрано: {selectedItems.Count}";
            }

            void ValidateTextBox(TextBox textBox)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    columnData.UpdateOutput();
                    return;
                }

                if (!int.TryParse(textBox.Text, out int value) || value < 0)
                {
                    textBox.Text = "0";
                }
                else if (value > 10000)
                {
                    textBox.Text = "10000";
                }

                columnData.UpdateOutput();
            }

            void OnSelectionChanged(object? s, SelectionChangedEventArgs e)
            {
                foreach (var removed in e.RemovedItems?.OfType<string>() ?? Enumerable.Empty<string>())
                    selectedItems.Remove(removed);

                foreach (var added in e.AddedItems?.OfType<string>() ?? Enumerable.Empty<string>())
                {
                    if (!selectedItems.Contains(added))
                        selectedItems.Add(added);
                }

                UpdateSelectedHint();
                columnData.UpdateOutput();
            }

            void ApplyClauseFilter()
            {
                var q = OrderClauseSearch.Normalize(searchBox.Text);
                var selectedSnapshot = selectedItems.ToList();

                listBox.SelectionChanged -= OnSelectionChanged;

                items.Clear();
                foreach (var clause in selectedSnapshot)
                    items.Add(clause);

                foreach (var clause in allClauses)
                {
                    if (selectedSnapshot.Contains(clause))
                        continue;
                    if (OrderClauseSearch.Matches(clause, q))
                        items.Add(clause);
                }

                listBox.SelectedItems?.Clear();
                foreach (var sel in selectedSnapshot)
                {
                    if (items.Contains(sel))
                        listBox.SelectedItems?.Add(sel);
                }

                listBox.SelectionChanged += OnSelectionChanged;
                UpdateSelectedHint();
            }

            listBox.SelectionChanged += OnSelectionChanged;
            searchBox.TextChanged += (_, _) => ApplyClauseFilter();

            menUnder40TextBox.TextChanged += (_, _) => ValidateTextBox(menUnder40TextBox);
            menOver40TextBox.TextChanged += (_, _) => ValidateTextBox(menOver40TextBox);
            womenUnder40TextBox.TextChanged += (_, _) => ValidateTextBox(womenUnder40TextBox);
            womenOver40TextBox.TextChanged += (_, _) => ValidateTextBox(womenOver40TextBox);

            columnData.StackPanel = new StackPanel
            {
                Classes = { "column" },
                Children =
                {
                    removeButton,
                    clausesLabel,
                    searchBox,
                    selectedHint,
                    listBox,
                    peopleLabel,
                    menUnder40TextBox,
                    menOver40TextBox,
                    womenUnder40TextBox,
                    womenOver40TextBox,
                    outputTextBlock
                }
            };

            columnData.ColumnBorder = new Border
            {
                Classes = { "analysis-column" },
                Child = columnData.StackPanel
            };

            Grid.SetColumn(columnData.ColumnBorder, columnIndex);
            grid.Children.Add(columnData.ColumnBorder);
            columnData.StackPanel.Opacity = 1;

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
            public Border ColumnBorder { get; set; } = null!;
            public StackPanel StackPanel { get; set; } = null!;
            public Button RemoveButton { get; set; } = null!;
            public ListBox ListBox { get; set; }
            public ObservableCollection<string> Items { get; set; }
            public List<string> AllClauses { get; set; } = new();
            public ObservableCollection<string> SelectedItems { get; set; }
            public TextBox SearchBox { get; set; } = null!;
            public TextBlock SelectedHint { get; set; } = null!;
            public TextBox MenUnder40TextBox { get; set; }
            public TextBox MenOver40TextBox { get; set; }
            public TextBox WomenUnder40TextBox { get; set; }
            public TextBox WomenOver40TextBox { get; set; }
            public TextBlock OutputTextBlock { get; set; }

            private static readonly List<string> MandatoryTests = new List<string>
            {
                "Расчет на основании антропометрии (измерение роста, массы тела, окружности талии) индекса массы тела",
                "Электрокардиография в покое",
                "Измерение артериального давления на периферических артериях",
                "Флюорография или рентгенография легких в двух проекциях (прямая и правая боковая)",
                "Определение абсолютного сердечно-сосудистого риска / Определение относительного сердечно-сосудистого риска",
                "Общий анализ крови (гемоглобин, цветной показатель, эритроциты, тромбоциты, лейкоциты, лейкоцитарная формула, СОЭ)",
                "Клинический анализ мочи (удельный вес, белок, сахар, микроскопия осадка)",
                "Определение уровня общего холестерина в крови (допускается использование экспресс-метода)",
                "Исследование уровня глюкозы в крови натощак (допускается использование экспресс-метода)"
            };

            private static readonly List<string> MandatoryDoctors = new List<string> { "Терапевт", "Невролог", "Психиатр", "Нарколог", "Профпатолог" };

            public void UpdateOutput()
            {
                var selectedClauses = SelectedItems.ToList();
                int menUnder40 = int.TryParse(MenUnder40TextBox.Text, out int m1) ? m1 : 0;
                int menOver40 = int.TryParse(MenOver40TextBox.Text, out int m2) ? m2 : 0;
                int womenUnder40 = int.TryParse(WomenUnder40TextBox.Text, out int w1) ? w1 : 0;
                int womenOver40 = int.TryParse(WomenOver40TextBox.Text, out int w2) ? w2 : 0;

                var doctorVisits = new Dictionary<string, int>(StringComparer.Ordinal);
                var testCounts = new Dictionary<string, int>(StringComparer.Ordinal);

                void AddDoctors(int count, bool isWoman, bool isOver40)
                {
                    if (count <= 0) return;
                    foreach (var doctor in GetDoctorsForPerson(selectedClauses, isWoman, isOver40))
                    {
                        doctorVisits.TryGetValue(doctor, out int n);
                        doctorVisits[doctor] = n + count;
                    }
                }

                void AddTests(int count, bool isWoman, bool isOver40)
                {
                    if (count <= 0) return;
                    foreach (var test in GetTestsForPerson(selectedClauses, isWoman, isOver40))
                    {
                        testCounts.TryGetValue(test, out int n);
                        testCounts[test] = n + count;
                    }
                }

                AddDoctors(menUnder40, false, false);
                AddDoctors(menOver40, false, true);
                AddDoctors(womenUnder40, true, false);
                AddDoctors(womenOver40, true, true);

                AddTests(menUnder40, false, false);
                AddTests(menOver40, false, true);
                AddTests(womenUnder40, true, false);
                AddTests(womenOver40, true, true);

                var output = $"Уникальных исследований: {testCounts.Count}\n";
                output += "Исследования:\n";
                output += string.Join("\n", testCounts.Select(kv => $"- {kv.Key}: {kv.Value} раз"));

                output += $"\nУникальных врачей: {doctorVisits.Count}\n";
                output += "Врачи:\n";
                output += string.Join("\n", doctorVisits.Select(kv => $"- {kv.Key}: {kv.Value} посещений"));

                OutputTextBlock.Text = output;
            }

            private IEnumerable<string> GetDoctorsForPerson(List<string> selectedClauses, bool isWoman, bool isOver40)
            {
                var doctors = new HashSet<string>();

                // Врачи из пунктов вредности
                foreach (var clause in selectedClauses)
                {
                    if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var data))
                        doctors.AddRange(data.Doctors);
                }

                // Добавляем обязательные исследования для каждого человека
                doctors.AddRange(MandatoryDoctors);

                // Дополнительные врачи
                if (isWoman)
                {
                    doctors.Add("Акушер-гинеколог");
                }

                return doctors;
            }

            private IEnumerable<string> GetTestsForPerson(List<string> selectedClauses, bool isWoman, bool isOver40)
            {
                var tests = new HashSet<string>();

                // Добавляем обязательные исследования для каждого человека
                tests.AddRange(MandatoryTests);

                // Исследования из пунктов вредности
                foreach (var clause in selectedClauses)
                {
                    if (Dictionaries.OrderClauseDataMap.TryGetValue(clause, out var data))
                        tests.AddRange(data.Tests);
                }

                // Дополнительные исследования
                if (isWoman && !isOver40) // Женщины младше 40
                {
                    tests.Add("Бактериологическое (на флору) и цитологическое (на атипичные клетки) исследования");
                    tests.Add("Ультразвуковое исследование органов малого таза");
                }
                if (isWoman && isOver40) // Женщины старше 40
                {
                    tests.Add("Бактериологическое (на флору) и цитологическое (на атипичные клетки) исследования");
                    tests.Add("Ультразвуковое исследование органов малого таза");
                    tests.Add("Маммография обеих молочных желез в двух проекциях");
                }
                if (!isWoman && isOver40) // Мужчины старше 40
                {
                    tests.Add("Измерение внутриглазного давления");
                }

                return tests;
            }
        }

    }

    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                hashSet.Add(item);
            }
        }
    }
}
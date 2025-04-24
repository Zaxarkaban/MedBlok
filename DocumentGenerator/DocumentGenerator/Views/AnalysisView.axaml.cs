using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DocumentGenerator.Models;
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

        public AnalysisView(IServiceProvider provider)
        {
            InitializeComponent();
            if (this.FindControl<Button>("AddColumnButton") is Button addColumnButton)
            {
                addColumnButton.Click += AddColumnButton_Click;
            }
            if (this.FindControl<Button>("ExportToExcelButton") is Button exportButton)
            {
                exportButton.Click += ExportToExcelButton_Click;
            }
            AddNewColumn(); // Первый столбец по умолчанию
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
                    await MessageBox.Show(this, "Файл успешно сохранён!", "Успех", MessageBox.MessageBoxButtons.Ok);
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", MessageBox.MessageBoxButtons.Ok);
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

            grid.Children.Remove(columnData.StackPanel);
            _columns.Remove(columnData);

            grid.ColumnDefinitions.Clear();
            grid.Children.Clear();

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

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var items = new ObservableCollection<string>(Dictionaries.OrderClauseDataMap.Keys);
            var selectedItems = new ObservableCollection<string>();

            var listBox = new ListBox
            {
                Name = $"ListBox_{columnIndex}",
                ItemsSource = items,
                SelectionMode = SelectionMode.Multiple,
            };

            var removeButton = new Button { Content = "–" };
            var menUnder40TextBox = new TextBox { Watermark = "Мужчины <40" };
            var menOver40TextBox = new TextBox { Watermark = "Мужчины >40" };
            var womenUnder40TextBox = new TextBox { Watermark = "Женщины <40" };
            var womenOver40TextBox = new TextBox { Watermark = "Женщины >40" };
            var outputTextBlock = new TextBlock { Classes = { "output" } };

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

            listBox.SelectionChanged += (s, e) =>
            {
                selectedItems.Clear();
                foreach (var item in listBox.SelectedItems?.Cast<string>() ?? new List<string>())
                {
                    selectedItems.Add(item);
                }
                columnData.UpdateOutput();
            };

            menUnder40TextBox.TextChanged += (s, e) => ValidateTextBox(menUnder40TextBox, e);
            menOver40TextBox.TextChanged += (s, e) => ValidateTextBox(menOver40TextBox, e);
            womenUnder40TextBox.TextChanged += (s, e) => ValidateTextBox(womenUnder40TextBox, e);
            womenOver40TextBox.TextChanged += (s, e) => ValidateTextBox(womenOver40TextBox, e);

            columnData.StackPanel = new StackPanel
            {
                Classes = { "column" },
                Children = { removeButton, listBox, menUnder40TextBox, menOver40TextBox, womenUnder40TextBox, womenOver40TextBox, outputTextBlock }
            };
            Grid.SetColumn(columnData.StackPanel, columnIndex);
            grid.Children.Add(columnData.StackPanel);

            columnData.StackPanel.Opacity = 0;
            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(10);
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
            public ObservableCollection<string> Items { get; set; }
            public ObservableCollection<string> SelectedItems { get; set; }
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
                int totalPeople = menUnder40 + menOver40 + womenUnder40 + womenOver40;

                // Подсчёт врачей
                var doctorVisits = new Dictionary<string, int>();

                // Посещения врачей для каждого человека
                var peopleDoctors = new List<HashSet<string>>();
                for (int i = 0; i < totalPeople; i++)
                {
                    peopleDoctors.Add(new HashSet<string>());
                }

                // Распределяем людей по категориям
                int personIndex = 0;
                for (int i = 0; i < menUnder40; i++)
                {
                    peopleDoctors[personIndex++].AddRange(GetDoctorsForPerson(selectedClauses, false, false));
                }
                for (int i = 0; i < menOver40; i++)
                {
                    peopleDoctors[personIndex++].AddRange(GetDoctorsForPerson(selectedClauses, false, true));
                }
                for (int i = 0; i < womenUnder40; i++)
                {
                    peopleDoctors[personIndex++].AddRange(GetDoctorsForPerson(selectedClauses, true, false));
                }
                for (int i = 0; i < womenOver40; i++)
                {
                    peopleDoctors[personIndex++].AddRange(GetDoctorsForPerson(selectedClauses, true, true));
                }

                // Подсчитываем посещения врачей
                foreach (var doctors in peopleDoctors)
                {
                    foreach (var doctor in doctors)
                    {
                        if (!doctorVisits.ContainsKey(doctor))
                        {
                            doctorVisits[doctor] = 0;
                        }
                        doctorVisits[doctor]++;
                    }
                }

                // Подсчёт исследований
                var testCounts = new Dictionary<string, int>();

                // Исследования для каждого человека
                var peopleTests = new List<HashSet<string>>();
                for (int i = 0; i < totalPeople; i++)
                {
                    peopleTests.Add(new HashSet<string>());
                }

                personIndex = 0;
                for (int i = 0; i < menUnder40; i++)
                {
                    peopleTests[personIndex++].AddRange(GetTestsForPerson(selectedClauses, false, false));
                }
                for (int i = 0; i < menOver40; i++)
                {
                    peopleTests[personIndex++].AddRange(GetTestsForPerson(selectedClauses, false, true));
                }
                for (int i = 0; i < womenUnder40; i++)
                {
                    peopleTests[personIndex++].AddRange(GetTestsForPerson(selectedClauses, true, false));
                }
                for (int i = 0; i < womenOver40; i++)
                {
                    peopleTests[personIndex++].AddRange(GetTestsForPerson(selectedClauses, true, true));
                }

                // Подсчитываем количество исследований
                foreach (var tests in peopleTests)
                {
                    foreach (var test in tests)
                    {
                        if (!testCounts.ContainsKey(test))
                        {
                            testCounts[test] = 0;
                        }
                        testCounts[test]++;
                    }
                }

                // Формирование вывода
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
                    var data = Dictionaries.OrderClauseDataMap[clause];
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
                    var data = Dictionaries.OrderClauseDataMap[clause];
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
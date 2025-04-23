using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Font;
using iText.IO.Font;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using OfficeOpenXml;
using DocumentGenerator.Services;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using System.Linq;

namespace DocumentGenerator.ViewModels
{
    public class ExcelDataViewModel
    {
        public class Record
        {
            public string? FullName { get; set; } = "";
            public string? Position { get; set; }
            public string? DateOfBirth { get; set; } = "";
            public int Age { get; set; }
            public string? Gender { get; set; } = "";
            public string? OrderClause { get; set; }
            public string? Snils { get; set; } = "";
            public string? MedicalPolicy { get; set; } = "";
            public string? PassportSeries { get; set; } = "";
            public string? PassportNumber { get; set; } = "";
            public string? PassportIssueDate { get; set; } = "";
            public string? PassportIssuedBy { get; set; } = "";
        }

        public List<Record> Records { get; set; } = new List<Record>();
        private readonly DocumentService _documentService;
        private readonly object _pdfLock = new object();
        private Dictionary<string, int> _doctorCounts = new Dictionary<string, int>();
        private Dictionary<string, int> _testCounts = new Dictionary<string, int>();
        private bool _isProcessing = false;

        public ExcelDataViewModel()
        {
            _documentService = new DocumentService();
        }

        public async Task LoadFromExcel(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Excel file not found.", filePath);
                }

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    Records.Clear();
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var record = new Record
                        {
                            FullName = worksheet.Cells[row, 2].Text?.Trim(),
                            Position = worksheet.Cells[row, 3].Text?.Trim(),
                            DateOfBirth = worksheet.Cells[row, 5].Text?.Trim(),
                            Gender = worksheet.Cells[row, 7].Text?.Trim(),
                            OrderClause = worksheet.Cells[row, 8].Text?.Trim(),
                            Snils = worksheet.Cells[row, 9].Text?.Trim(),
                            MedicalPolicy = worksheet.Cells[row, 10].Text?.Trim(),
                            PassportSeries = worksheet.Cells[row, 11].Text?.Trim(),
                            PassportNumber = worksheet.Cells[row, 12].Text?.Trim(),
                            PassportIssueDate = worksheet.Cells[row, 13].Text?.Trim(),
                            PassportIssuedBy = worksheet.Cells[row, 14].Text?.Trim()
                        };

                        if (string.IsNullOrWhiteSpace(record.FullName))
                        {
                            continue;
                        }

                        if (double.TryParse(record.DateOfBirth, out double dateOfBirthDays))
                        {
                            DateTime dateOfBirth = DateTime.FromOADate(dateOfBirthDays);
                            record.DateOfBirth = dateOfBirth.ToString("dd.MM.yyyy");
                        }

                        if (DateTime.TryParseExact(record.DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var birthDate))
                        {
                            var today = DateTime.Today;
                            int age = today.Year - birthDate.Year;
                            if (birthDate.Date > today.AddYears(-age)) age--;
                            record.Age = age;
                        }

                        if (double.TryParse(record.PassportIssueDate, out double passportIssueDays))
                        {
                            DateTime passportIssueDate = DateTime.FromOADate(passportIssueDays);
                            record.PassportIssueDate = passportIssueDate.ToString("dd.MM.yyyy");
                        }

                        Records.Add(record);
                        LogToFile($"Добавлена запись: {record.FullName}, возраст: {record.Age}, пол: {record.Gender}");
                    }

                    LogToFile($"Всего добавлено записей: {Records.Count}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Ошибка при чтении Excel-файла: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка при чтении Excel-файла: {ex.Message}", ex);
            }
        }

        public async Task SaveToPdf(Window parentWindow)
        {
            if (_isProcessing)
            {
                LogToFile("Метод SaveToPdf уже выполняется, повторный вызов проигнорирован.");
                await ShowMessageBox(parentWindow, "Обработка уже выполняется. Пожалуйста, подождите.", "Предупреждение");
                return;
            }

            _isProcessing = true;
            LogToFile("Начало выполнения метода SaveToPdf.");

            try
            {
                var storageProvider = parentWindow.StorageProvider;

                var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Выберите папку для сохранения PDF-файлов",
                    SuggestedStartLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                });

                if (folder == null || folder.Count == 0)
                {
                    await ShowMessageBox(parentWindow, "Выбор папки отменён.", "Информация");
                    return;
                }

                var folderPath = folder[0].Path.LocalPath;
                string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.pdf");

                if (!File.Exists(templatePath))
                {
                    await ShowMessageBox(parentWindow, "Шаблон PDF не найден.", "Ошибка");
                    return;
                }

                string fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
                if (!File.Exists(fontPath))
                {
                    throw new FileNotFoundException("Times New Roman font file not found in the project.", fontPath);
                }

                LogToFile("Очистка словарей перед началом обработки.");
                _doctorCounts.Clear();
                _testCounts.Clear();

                int recordNumber = 1;
                foreach (var record in Records)
                {
                    string safeFileName = SanitizeFileName(record.FullName ?? $"Record_{recordNumber}");
                    string outputPath = System.IO.Path.Combine(folderPath, $"{safeFileName}.pdf");

                    string tempTemplatePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Template_{Guid.NewGuid()}.pdf");
                    try
                    {
                        File.Copy(templatePath, tempTemplatePath, true);
                        LogToFile($"Создана временная копия шаблона: {tempTemplatePath}");

                        lock (_pdfLock)
                        {
                            using (var writer = new PdfWriter(outputPath))
                            using (var reader = new PdfReader(tempTemplatePath))
                            using (var pdf = new PdfDocument(reader, writer))
                            {
                                LogToFile($"Начало обработки PDF для {record.FullName} (№{recordNumber})");

                                PdfFont font;
                                try
                                {
                                    font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
                                    if (font == null)
                                    {
                                        throw new InvalidOperationException("Failed to create font from times.ttf.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogToFile($"Ошибка при загрузке шрифта: {ex.Message}\nStackTrace: {ex.StackTrace}");
                                    throw new InvalidOperationException($"Ошибка при загрузке шрифта: {ex.Message}", ex);
                                }

                                var form = PdfAcroForm.GetAcroForm(pdf, true);
                                var fields = form.GetAllFormFields();

                                LogToFile($"Заполнение полей формы для {record.FullName}");
                                SetFieldValue(fields, "FullName", record.FullName, font);
                                SetFieldValue(fields, "Gender", record.Gender, font);
                                SetFieldValue(fields, "DateOfBirth", record.DateOfBirth, font);
                                SetFieldValue(fields, "Address", "", font);
                                SetFieldValue(fields, "Phone", "", font);
                                SetFieldValue(fields, "PassportSeries", record.PassportSeries, font);
                                SetFieldValue(fields, "PassportNumber", record.PassportNumber, font);
                                SetFieldValue(fields, "PassportIssueDate", record.PassportIssueDate, font);
                                SetFieldValue(fields, "PassportIssuedBy", record.PassportIssuedBy, font);
                                SetFieldValue(fields, "Snils", record.Snils, font);
                                SetFieldValue(fields, "MedicalPolicy", record.MedicalPolicy, font);
                                SetFieldValue(fields, "OrderClause", record.OrderClause, font);
                                SetFieldValue(fields, "Workplace", "", font);
                                SetFieldValue(fields, "MedicalFacility", "", font);
                                SetFieldValue(fields, "Position", record.Position, font);
                                SetFieldValue(fields, "WorkExperience", "", font);
                                SetFieldValue(fields, "MedicalOrganization", "", font);
                                SetFieldValue(fields, "Okved", "", font);
                                int currentYear = DateTime.Now.Year;
                                if (fields.TryGetValue("CurrentYear", out var field))
                                {
                                    var pdfField = (PdfFormField)field;
                                    pdfField.SetValue(currentYear.ToString());
                                    pdfField.SetFontAndSize(font, 24); // Увеличиваем шрифт до 16
                                }
                                // Новая дата (полная)
                                string currentDate = DateTime.Now.ToString("dd.MM.yyyy"); // Формат: 22.04.2025
                                SetFieldValue(fields, "CurrentDate", currentDate, font);

                                // Разбиваем ФИО на части (предполагаем, что ФИО в формате "Фамилия Имя Отчество")
                                string[] fioParts = record.FullName.Split(' ');
                                string lastName = fioParts.Length > 0 ? fioParts[0] : "";
                                string firstName = fioParts.Length > 1 ? fioParts[1] : "";
                                string middleName = fioParts.Length > 2 ? fioParts[2] : "";
                                SetFieldValue(fields, "LastName", lastName, font);
                                SetFieldValue(fields, "FirstName", firstName, font);
                                SetFieldValue(fields, "MiddleName", middleName, font);

                                // Поле с галочкой (чекбокс)
                                SetFieldValue(fields, "CheckBoxField", "Yes", font); // "Yes" — стандартное значение для отмеченного чекбокса

                                // Поле "Документ"
                                SetFieldValue(fields, "Document", "паспорт", font);

                                var selectedClauses = record.OrderClause?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(clause => clause.Trim())
                                    .ToList() ?? new List<string>();
                                var doctors = _documentService.GenerateDoctorsList(selectedClauses, record.Age > 40, record.Gender == "Женский" || record.Gender == "ж");

                                LogToFile($"Список врачей для {record.FullName}: {string.Join(", ", doctors)}");
                                LogToFile($"Количество врачей для {record.FullName}: {doctors.Count}");

                                var uniqueDoctors = doctors.Distinct().ToList();
                                if (uniqueDoctors.Count != doctors.Count)
                                {
                                    LogToFile($"Обнаружены дубликаты врачей для {record.FullName}. После удаления дубликатов: {string.Join(", ", uniqueDoctors)}");
                                }

                                foreach (var doctor in uniqueDoctors)
                                {
                                    if (_doctorCounts.ContainsKey(doctor))
                                    {
                                        _doctorCounts[doctor]++;
                                        LogToFile($"Увеличено количество для врача {doctor}: {_doctorCounts[doctor]}");
                                    }
                                    else
                                    {
                                        _doctorCounts[doctor] = 1;
                                        LogToFile($"Добавлен новый врач {doctor}: 1");
                                    }
                                }

                                LogToFile($"Заполнение списка врачей для {record.FullName}, количество врачей: {uniqueDoctors.Count}");
                                for (int i = 0; i < uniqueDoctors.Count && i < 12; i++)
                                {
                                    string fieldName = $"Doctor_{i + 1}";
                                    SetFieldValue(fields, fieldName, uniqueDoctors[i], font);
                                }

                                if (uniqueDoctors.Count > 12)
                                {
                                    LogToFile($"Внимание: В списке {uniqueDoctors.Count} врачей для записи {record.FullName}, но шаблон поддерживает только 12. Лишние врачи проигнорированы.");
                                }

                                form.FlattenFields();

                                bool isFemale = record.Gender == "Женский" || record.Gender == "ж";
                                bool isOver40 = record.Age > 40;
                                var tests = _documentService.GenerateTestsList(isOver40, isFemale, selectedClauses);

                                LogToFile($"Список исследований для {record.FullName}: {string.Join(", ", tests)}");
                                LogToFile($"Количество исследований для {record.FullName}: {tests.Count}");

                                var uniqueTests = tests.Distinct().ToList();
                                if (uniqueTests.Count != tests.Count)
                                {
                                    LogToFile($"Обнаружены дубликаты исследований для {record.FullName}. После удаления дубликатов: {string.Join(", ", uniqueTests)}");
                                }

                                foreach (var test in uniqueTests)
                                {
                                    if (_testCounts.ContainsKey(test))
                                    {
                                        _testCounts[test]++;
                                        LogToFile($"Увеличено количество для исследования {test}: {_testCounts[test]}");
                                    }
                                    else
                                    {
                                        _testCounts[test] = 1;
                                        LogToFile($"Добавлен новый исследования {test}: 1");
                                    }
                                }

                                AddTestsPage(pdf, uniqueTests, font);

                                LogToFile($"Закрытие документа для {record.FullName}");
                                pdf.Close();
                                LogToFile($"PDF успешно создан: {outputPath}");
                            }
                        }
                    }
                    catch (iText.Kernel.Exceptions.PdfException pdfEx)
                    {
                        LogToFile($"PdfException при создании PDF для {record.FullName} (№{recordNumber}): {pdfEx.Message}\nStackTrace: {pdfEx.StackTrace}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Общая ошибка при создании PDF для {record.FullName} (№{recordNumber}): {ex.Message}\nStackTrace: {ex.StackTrace}");
                        throw;
                    }
                    finally
                    {
                        if (File.Exists(tempTemplatePath))
                        {
                            try
                            {
                                File.Delete(tempTemplatePath);
                                LogToFile($"Временный файл удалён: {tempTemplatePath}");
                            }
                            catch (Exception ex)
                            {
                                LogToFile($"Ошибка при удалении временного файла {tempTemplatePath}: {ex.Message}");
                            }
                        }
                    }

                    recordNumber++;
                }

                LogToFile("Итоговый подсчёт врачей:");
                foreach (var kvp in _doctorCounts.OrderBy(k => k.Key))
                {
                    LogToFile($"{kvp.Key}: {kvp.Value}");
                }

                LogToFile("Итоговый подсчёт исследований:");
                foreach (var kvp in _testCounts.OrderBy(k => k.Key))
                {
                    LogToFile($"{kvp.Key}: {kvp.Value}");
                }

                await SaveStatisticsToExcel(folderPath);

                await ShowMessageBox(parentWindow, $"Все PDF-файлы успешно сохранены в папке:\n{folderPath}\nСтатистика врачей и исследований сохранена в Statistics.xlsx", "Успех");
            }
            catch (Exception ex)
            {
                LogToFile($"Ошибка при сохранении PDF: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await ShowMessageBox(parentWindow, $"Ошибка при сохранении PDF:\n{ex.Message}", "Ошибка");
            }
            finally
            {
                _isProcessing = false;
                LogToFile("Метод SaveToPdf завершён.");
            }
        }

        private async Task SaveStatisticsToExcel(string folderPath)
        {
            try
            {
                string outputPath = System.IO.Path.Combine(folderPath, "Statistics.xlsx");
                using (var package = new ExcelPackage())
                {
                    var doctorsSheet = package.Workbook.Worksheets.Add("Doctors");
                    doctorsSheet.Cells[1, 1].Value = "Врач";
                    doctorsSheet.Cells[1, 2].Value = "Количество";

                    int row = 2;
                    foreach (var doctor in _doctorCounts.OrderBy(d => d.Key))
                    {
                        doctorsSheet.Cells[row, 1].Value = doctor.Key;
                        doctorsSheet.Cells[row, 2].Value = doctor.Value;
                        row++;
                    }

                    // Автоматическая подстройка ширины столбцов на листе Doctors
                    doctorsSheet.Cells[1, 1, row - 1, 2].AutoFitColumns();

                    var testsSheet = package.Workbook.Worksheets.Add("Tests");
                    testsSheet.Cells[1, 1].Value = "Исследование";
                    testsSheet.Cells[1, 2].Value = "Количество";

                    row = 2;
                    foreach (var test in _testCounts.OrderBy(t => t.Key))
                    {
                        testsSheet.Cells[row, 1].Value = test.Key;
                        testsSheet.Cells[row, 2].Value = test.Value;
                        row++;
                    }

                    // Автоматическая подстройка ширины столбцов на листе Tests
                    testsSheet.Cells[1, 1, row - 1, 2].AutoFitColumns();

                    File.WriteAllBytes(outputPath, package.GetAsByteArray());
                    LogToFile($"Статистика успешно сохранена в {outputPath}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Ошибка при сохранении статистики в Excel: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка при сохранении статистики в Excel: {ex.Message}", ex);
            }
        }

        private void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string value, PdfFont font)
        {
            if (fields.TryGetValue(fieldName, out var pdfField))
            {
                // Устанавливаем значение поля
                pdfField.SetValue(value);

                // Пропускаем динамическое изменение шрифта для CurrentYear (шрифт фиксированно 24)
                if (fieldName == "CurrentYear")
                {
                    pdfField.SetFontAndSize(font, 24f);
                    return;
                }

                // Начальный размер шрифта
                float fontSize = 10.5f;
                pdfField.SetFontAndSize(font, fontSize);

                // Получаем размеры поля
                var widget = pdfField.GetWidgets().FirstOrDefault();
                if (widget == null) return;
                var rect = widget.GetRectangle();
                float fieldWidth = rect.GetAsNumber(2).FloatValue() - rect.GetAsNumber(0).FloatValue(); // Ширина поля

                // Проверяем, влезает ли текст, уменьшаем шрифт при необходимости
                float textWidth;
                float minFontSize = 6f; // Минимальный размер шрифта
                do
                {
                    // Измеряем ширину текста с текущим размером шрифта
                    textWidth = font.GetWidth(value, fontSize);

                    if (textWidth > fieldWidth && fontSize > minFontSize)
                    {
                        fontSize -= 0.5f; // Уменьшаем шрифт на 0.5
                    }
                    else
                    {
                        break; // Текст влезает или достигнут минимальный шрифт
                    }
                } while (true);

                // Устанавливаем финальный размер шрифта
                pdfField.SetFontAndSize(font, fontSize);
            }
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            // Убедимся, что в документе есть как минимум 2 страницы
            int currentPageCount = pdfDocument.GetNumberOfPages();
            while (currentPageCount < 2)
            {
                pdfDocument.AddNewPage();
                currentPageCount++;
            }

            // Получаем вторую страницу
            var page = pdfDocument.GetPage(2);
            var pageSize = page.GetPageSize();

            // Определяем область для левой половины страницы (A4: ширина 595, половина = 297.5)
            var leftHalf = new Rectangle(36, 36, 261.5f, pageSize.GetHeight() - 72); // 36 пунктов отступ слева и снизу, ширина 261.5, высота с учётом отступов

            // Создаём ColumnText для управления позицией текста
            var column = new PdfCanvas(page);
            var columnText = new iText.Layout.Canvas(column, leftHalf);

            // Создаём параграф с текстом
            var paragraph = new Paragraph()
                .SetFont(font)
                .SetFontSize(7);

            // Меняем заголовок на "Список исследований"
            paragraph.Add(new Text("Список исследований:\n\n"));
            int testNumber = 1;
            foreach (var test in tests)
            {
                paragraph.Add(new Text($"{testNumber}. {test}\n"));
                testNumber++;
            }

            // Добавляем параграф на вторую страницу
            columnText.Add(paragraph);
            columnText.Close();
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = new string(System.IO.Path.GetInvalidFileNameChars());
            var regex = new System.Text.RegularExpressions.Regex($"[{System.Text.RegularExpressions.Regex.Escape(invalidChars)}]");
            return regex.Replace(fileName, "_").Trim();
        }

        private async Task ShowMessageBox(Window parent, string message, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(10),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            };

            var stackPanel = dialog.Content as StackPanel;
            if (stackPanel != null)
            {
                var okButton = stackPanel.Children[1] as Button;
                if (okButton != null)
                {
                    okButton.Click += (sender, e) => dialog.Close();
                }
            }
            await dialog.ShowDialog(parent);
        }

        private void LogToFile(string message)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ExcelDataViewModel - {message}\n";
                File.AppendAllText(logPath, logEntry);
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в лог: {ex.Message}");
            }
        }
    }
}
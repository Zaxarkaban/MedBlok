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
            public string? Position { get; set; } = "";
            public string? DateOfBirth { get; set; } = "";
            public int Age { get; set; }
            public string? Gender { get; set; } = "";
            public string? OrderClause { get; set; } = "";
            public string? Snils { get; set; } = "";
            public string? MedicalPolicy { get; set; } = "";
            public string? PassportSeries { get; set; } = "";
            public string? PassportNumber { get; set; } = "";
            public string? PassportIssueDate { get; set; } = "";
            public string? PassportIssuedBy { get; set; } = "";
        }

        public List<Record> Records { get; set; } = new List<Record>();
        private readonly DocumentService _documentService;
        private readonly object _pdfLock = new object(); // Для синхронизации доступа к PDF-операциям

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
                            FullName = worksheet.Cells[row, 2].Text,
                            Position = worksheet.Cells[row, 3].Text,
                            DateOfBirth = worksheet.Cells[row, 5].Text,
                            Gender = worksheet.Cells[row, 7].Text,
                            OrderClause = worksheet.Cells[row, 8].Text,
                            Snils = worksheet.Cells[row, 9].Text,
                            MedicalPolicy = worksheet.Cells[row, 10].Text,
                            PassportSeries = worksheet.Cells[row, 11].Text,
                            PassportNumber = worksheet.Cells[row, 12].Text,
                            PassportIssueDate = worksheet.Cells[row, 13].Text,
                            PassportIssuedBy = worksheet.Cells[row, 14].Text
                        };

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
                    }
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

                int recordNumber = 1;
                foreach (var record in Records)
                {
                    string safeFileName = SanitizeFileName(record.FullName ?? $"Record_{recordNumber}");
                    string outputPath = System.IO.Path.Combine(folderPath, $"{safeFileName}.pdf");

                    // Копируем шаблон во временный файл, чтобы избежать блокировки
                    string tempTemplatePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Template_{Guid.NewGuid()}.pdf");
                    try
                    {
                        File.Copy(templatePath, tempTemplatePath, true);
                        LogToFile($"Создана временная копия шаблона: {tempTemplatePath}");

                        lock (_pdfLock) // Синхронизируем доступ к операциям с PDF
                        {
                            // Создаём PdfReader и PdfWriter для каждого документа
                            using (var writer = new PdfWriter(outputPath))
                            using (var reader = new PdfReader(tempTemplatePath))
                            using (var pdf = new PdfDocument(reader, writer))
                            {
                                LogToFile($"Начало обработки PDF для {record.FullName} (№{recordNumber})");

                                // Создаём шрифт для каждого документа
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

                                // Заполняем поля формы
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

                                var selectedClauses = record.OrderClause?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(clause => clause.Trim())
                                    .ToList() ?? new List<string>();
                                var doctors = _documentService.GenerateDoctorsList(selectedClauses, record.Age > 40, record.Gender == "Женский" || record.Gender == "ж");

                                LogToFile($"Заполнение списка врачей для {record.FullName}, количество врачей: {doctors.Count}");
                                for (int i = 0; i < doctors.Count && i < 12; i++)
                                {
                                    string fieldName = $"Doctor_{i + 1}";
                                    SetFieldValue(fields, fieldName, doctors[i], font);
                                }

                                if (doctors.Count > 12)
                                {
                                    LogToFile($"Внимание: В списке {doctors.Count} врачей для записи {record.FullName}, но шаблон поддерживает только 12. Лишние врачи проигнорированы.");
                                }

                                // Финализируем форму
                                LogToFile($"Финализация формы для {record.FullName}");
                                form.FlattenFields();

                                // Добавляем страницу с анализами
                                bool isFemale = record.Gender == "Женский" || record.Gender == "ж";
                                bool isOver40 = record.Age > 40;
                                // Извлекаем selectedClauses из record.OrderClause
                                var tests = _documentService.GenerateTestsList(isOver40, isFemale, selectedClauses);
                                AddTestsPage(pdf, tests, font);

                                // Закрываем документ
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
                        // Удаляем временный файл
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

                await ShowMessageBox(parentWindow, $"Все PDF-файлы успешно сохранены в папке:\n{folderPath}", "Успех");
            }
            catch (Exception ex)
            {
                LogToFile($"Ошибка при сохранении PDF: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await ShowMessageBox(parentWindow, $"Ошибка при сохранении PDF:\n{ex.Message}", "Ошибка");
            }
        }

        private void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string value, PdfFont font)
        {
            if (fields.ContainsKey(fieldName))
            {
                var field = fields[fieldName];
                field.SetValue(value ?? "");
                if (font != null)
                {
                    field.SetFontAndSize(font, 10);
                }
            }
            else
            {
                LogToFile($"Поле {fieldName} не найдено в форме.");
            }
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            // Проверяем количество страниц в документе
            int currentPageCount = pdfDocument.GetNumberOfPages();
            LogToFile($"Количество страниц в документе: {currentPageCount}");

            // Предполагаем, что в шаблоне уже есть 3 страницы
            if (currentPageCount < 3)
            {
                LogToFile($"Ошибка: В шаблоне меньше 3 страниц ({currentPageCount}). Требуется шаблон с минимум 3 страницами.");
                throw new InvalidOperationException("Шаблон PDF должен содержать минимум 3 страницы.");
            }

            // Получаем третью страницу
            var page = pdfDocument.GetPage(3);
            LogToFile($"Работа с третьей страницей (страница №3 из {currentPageCount})");

            // Используем PdfCanvas для низкоуровневого рисования
            PdfCanvas pdfCanvas = new PdfCanvas(page);
            pdfCanvas.BeginText();
            pdfCanvas.SetFontAndSize(font, 12);

            float yPosition = page.GetPageSize().GetHeight() - 50; // Начальная позиция Y (сверху страницы)
            float xPosition = 36; // Отступ слева
            float lineHeight = 15; // Высота строки

            // Добавляем заголовок
            pdfCanvas.SetTextMatrix(xPosition, yPosition);
            pdfCanvas.ShowText("Список необходимых анализов:");
            yPosition -= lineHeight * 2; // Двойной отступ после заголовка

            // Добавляем список анализов
            int testNumber = 1;
            foreach (var test in tests)
            {
                pdfCanvas.SetTextMatrix(xPosition, yPosition);
                pdfCanvas.ShowText($"{testNumber}. {test}");
                yPosition -= lineHeight;
                testNumber++;
            }

            pdfCanvas.EndText();
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
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                File.AppendAllText(logPath, logEntry);
                Console.WriteLine(logEntry); // Также выводим в консоль для Visual Studio
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в лог: {ex.Message}");
            }
        }
    }
}
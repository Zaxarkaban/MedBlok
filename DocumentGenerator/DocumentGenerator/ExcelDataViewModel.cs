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
using System.Data;
using System.Text.RegularExpressions;

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
            public string? Phone { get; set; } = "";
            public string? Address { get; set; } = "";
            public string? Department { get; set; } = "";
            public string? Workplace { get; set; } = "";
            public string? MedicalFacility { get; set; } = "";
            public string? WorkExperience { get; set; } = "";
            public string? MedicalOrganization { get; set; } = "";
            public string? Okved { get; set; } = "";
            public string? ServicePoint { get; set; } = "";
            public string? WorkAddress { get; set; } = "";
            public string? OwnershipForm { get; set; } = "";
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    Records.Clear();
                    for (int row = 2; row <= rowCount; row++)
                    {
                        // Пропускаем пустые строки
                        if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 2].Text))
                        {
                            Console.WriteLine($"Пропущена строка {row}: пустое значение в столбце 2.");
                            continue;
                        }

                        // Получаем значение в столбце 2 (ФИО) и столбце 5 (Дата рождения)
                        string fullName = worksheet.Cells[row, 2].Text?.Trim();
                        string dateOfBirth = worksheet.Cells[row, 5].Text?.Trim();

                        // Пропускаем строку с заголовками (где в столбце 2 указано "ФИО" или похожее)
                        if (row == 2 && (fullName?.ToLower() == "фио" || fullName?.ToLower() == "сотрудник"))
                        {
                            Console.WriteLine($"Пропущена строка {row}: заголовок (ФИО = {fullName}).");
                            continue;
                        }

                        // Пропускаем строку, если данные выглядят как заголовки
                        if (fullName?.ToLower().Contains("сотрудник") == true ||
                            dateOfBirth?.ToLower().Contains("дата рождения") == true)
                        {
                            Console.WriteLine($"Пропущена строка {row}: данные похожи на заголовки (ФИО = {fullName}, Дата рождения = {dateOfBirth}).");
                            continue;
                        }

                        // Проверяем, что дата рождения — это корректная дата
                        bool isValidDate = false;
                        if (!string.IsNullOrEmpty(dateOfBirth))
                        {
                            // Пробуем числовой формат Excel (число дней с 1900 года)
                            if (double.TryParse(dateOfBirth, out double dateOfBirthDays))
                            {
                                try
                                {
                                    DateTime.FromOADate(dateOfBirthDays);
                                    isValidDate = true;
                                }
                                catch
                                {
                                    isValidDate = false;
                                }
                            }
                            // Пробуем текстовый формат "dd.MM.yyyy"
                            else if (DateTime.TryParseExact(dateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                            {
                                isValidDate = true;
                            }
                        }

                        if (!isValidDate)
                        {
                            Console.WriteLine($"Пропущена строка {row}: некорректная дата рождения ({dateOfBirth}).");
                            continue;
                        }

                        var years = worksheet.Cells[row, 17].Text?.Trim() ?? "0";
                        var months = worksheet.Cells[row, 18].Text?.Trim() ?? "0";
                        var workExperience = $"Лет {years}, Месяцев {months}";

                        var record = new Record
                        {
                            FullName = fullName,
                            Position = worksheet.Cells[row, 3].Text?.Trim(),
                            Department = worksheet.Cells[row, 4].Text?.Trim(),
                            DateOfBirth = dateOfBirth,
                            Age = 0, // Изначально устанавливаем 0, будем вычислять ниже
                            Gender = worksheet.Cells[row, 7].Text?.Trim(),
                            OrderClause = worksheet.Cells[row, 8].Text?.Trim(),
                            Snils = worksheet.Cells[row, 9].Text?.Trim(),
                            MedicalPolicy = worksheet.Cells[row, 10].Text?.Trim(),
                            PassportSeries = worksheet.Cells[row, 11].Text?.Trim(),
                            PassportNumber = worksheet.Cells[row, 12].Text?.Trim(),
                            PassportIssueDate = worksheet.Cells[row, 13].Text?.Trim(),
                            PassportIssuedBy = worksheet.Cells[row, 14].Text?.Trim(),
                            Phone = worksheet.Cells[row, 15].Text?.Trim(),
                            Address = worksheet.Cells[row, 16].Text?.Trim(),
                            MedicalFacility = worksheet.Cells[row, 19].Text?.Trim() ?? "",
                            MedicalOrganization = "", // Не заполняем
                            Workplace = worksheet.Cells[1, 3].Text?.Trim() ?? "", // ООО "Рога и копыта"
                            WorkAddress = worksheet.Cells[1, 7].Text?.Trim() ?? "", // СПб, Пушкин
                            Okved = worksheet.Cells[1, 16].Text?.Trim() ?? "", // 22.15.00
                            OwnershipForm = worksheet.Cells[1, 14].Text?.Trim() ?? "", // Государственная
                            WorkExperience = workExperience, // Лет {лет}, Месяцев {месяцев}
                            ServicePoint = worksheet.Cells[2, 20].Text?.Trim() ?? "89" // Пункт обслуживания
                        };

                        // Пробуем преобразовать дату рождения
                        DateTime birthDate = DateTime.MinValue; // Инициализация по умолчанию
                        if (!string.IsNullOrEmpty(record.DateOfBirth))
                        {
                            // Пробуем числовой формат Excel (число дней с 1900 года)
                            if (double.TryParse(record.DateOfBirth, out double dateOfBirthDays))
                            {
                                try
                                {
                                    birthDate = DateTime.FromOADate(dateOfBirthDays);
                                    record.DateOfBirth = birthDate.ToString("dd.MM.yyyy");
                                }
                                catch
                                {
                                    birthDate = DateTime.MinValue; // Если не удалось преобразовать
                                }
                            }
                            // Пробуем текстовый формат "dd.MM.yyyy"
                            else if (DateTime.TryParseExact(record.DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out birthDate))
                            {
                                // Дата уже в правильном формате
                            }
                            else
                            {
                                birthDate = DateTime.MinValue; // Если формат не распознан
                            }

                            // Вычисляем возраст, если дата рождения успешно распознана
                            if (birthDate != DateTime.MinValue)
                            {
                                var today = DateTime.Today;
                                int calculatedAge = today.Year - birthDate.Year;
                                if (birthDate.Date > today.AddYears(-calculatedAge)) calculatedAge--;
                                record.Age = calculatedAge;
                            }
                        }

                        // Преобразуем дату выдачи паспорта
                        if (double.TryParse(record.PassportIssueDate, out double passportIssueDays))
                        {
                            DateTime passportIssueDate = DateTime.FromOADate(passportIssueDays);
                            record.PassportIssueDate = passportIssueDate.ToString("dd.MM.yyyy");
                        }

                        // Нормализация пола
                        if (!string.IsNullOrEmpty(record.Gender))
                        {
                            record.Gender = record.Gender.ToLower() == "ж" ? "Женский" : "Мужской";
                        }

                        Records.Add(record);
                        Console.WriteLine($"Добавлена запись для строки {row}: ФИО = {record.FullName}, Дата рождения = {record.DateOfBirth}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при чтении Excel-файла: {ex.Message}", ex);
            }
        }

        public async Task SaveToPdf(Window parentWindow)
        {
            if (_isProcessing)
            {
                await ShowMessageBox(parentWindow, "Обработка уже выполняется. Пожалуйста, подождите.", "Предупреждение");
                return;
            }

            _isProcessing = true;

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

                        lock (_pdfLock)
                        {
                            using (var writer = new PdfWriter(outputPath))
                            using (var reader = new PdfReader(tempTemplatePath))
                            using (var pdf = new PdfDocument(reader, writer))
                            {
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
                                    throw new InvalidOperationException($"Ошибка при загрузке шрифта: {ex.Message}", ex);
                                }

                                var form = PdfAcroForm.GetAcroForm(pdf, true);
                                var fields = form.GetAllFormFields();

                                SetFieldValue(fields, "FullName", record.FullName, font);
                                SetFieldValue(fields, "Gender", record.Gender, font);
                                SetFieldValue(fields, "DateOfBirth", record.DateOfBirth, font);
                                SetFieldValue(fields, "DateOfBirth1", record.DateOfBirth, font);
                                SetFieldValue(fields, "Address", record.Address, font);
                                SetFieldValue(fields, "Phone", record.Phone, font);
                                SetFieldValue(fields, "PassportSeries", record.PassportSeries, font);
                                SetFieldValue(fields, "PassportNumber", record.PassportNumber, font);
                                SetFieldValue(fields, "PassportIssueDate", record.PassportIssueDate, font);
                                SetFieldValue(fields, "PassportIssuedBy", record.PassportIssuedBy, font);
                                SetFieldValue(fields, "Snils", record.Snils, font);
                                SetFieldValue(fields, "MedicalPolicy", record.MedicalPolicy, font);
                                SetFieldValue(fields, "OrderClause", record.OrderClause, font);
                                SetFieldValue(fields, "Workplace", record.Workplace, font);
                                SetFieldValue(fields, "MedicalFacility", record.MedicalFacility, font);
                                SetFieldValue(fields, "Position", record.Position, font);
                                SetFieldValue(fields, "WorkExperience", record.WorkExperience, font);
                                SetFieldValue(fields, "MedicalOrganization", record.MedicalOrganization, font);
                                SetFieldValue(fields, "Okved", record.Okved, font);
                                SetFieldValue(fields, "ServicePoint", record.ServicePoint, font);
                                SetFieldValue(fields, "WorkAddress", record.WorkAddress, font);
                                SetFieldValue(fields, "Department", record.Department, font);
                                SetFieldValue(fields, "OwnershipForm", record.OwnershipForm, font);
                                SetFieldValue(fields, "normasDate", record.Age.ToString(), font); // Возраст в поле "полных лет"
                                fields["обязательные_анализы"].SetValue("V");

                                int currentYear = DateTime.Now.Year;
                                if (fields.TryGetValue("CurrentYear", out var field))
                                {
                                    var pdfField = (PdfFormField)field;
                                    pdfField.SetValue(currentYear.ToString());
                                    pdfField.SetFontAndSize(font, 24);
                                }
                                SetFieldValue(fields, "CurrentYear1", currentYear.ToString(), font);

                                string currentDate = DateTime.Now.ToString("dd.MM.yyyy");
                                SetFieldValue(fields, "CurrentDate", currentDate, font);

                                string[] fioParts = record.FullName?.Split(' ') ?? new string[] { "", "", "" };
                                string lastName = fioParts.Length > 0 ? fioParts[0] : "";
                                string firstName = fioParts.Length > 1 ? fioParts[1] : "";
                                string middleName = fioParts.Length > 2 ? fioParts[2] : "";
                                SetFieldValue(fields, "LastName", lastName, font);
                                SetFieldValue(fields, "FirstName", firstName, font);
                                SetFieldValue(fields, "MiddleName", middleName, font);

                                SetFieldValue(fields, "CheckBoxField", "Yes", font);
                                SetFieldValue(fields, "Document", "паспорт", font);

                                switch (record.ServicePoint)
                                {
                                    case "ПО 67":
                                        if (fields.ContainsKey("ServicePoint1"))
                                        {
                                            fields["ServicePoint1"].SetValue("V");
                                        }
                                        break;
                                    case "89":
                                        if (fields.ContainsKey("ServicePoint2"))
                                        {
                                            fields["ServicePoint2"].SetValue("V");
                                        }
                                        break;
                                    case "ПО 66":
                                        if (fields.ContainsKey("ServicePoint3"))
                                        {
                                            fields["ServicePoint3"].SetValue("V");
                                        }
                                        break;
                                    case "ПО «Шушары»":
                                        if (fields.ContainsKey("ServicePoint4"))
                                        {
                                            fields["ServicePoint4"].SetValue("V");
                                        }
                                        break;
                                    case "ЖК «Шушары»":
                                        if (fields.ContainsKey("ServicePoint5"))
                                        {
                                            fields["ServicePoint5"].SetValue("V");
                                        }
                                        break;
                                    case "ПО «Славянка»":
                                        if (fields.ContainsKey("ServicePoint6"))
                                        {
                                            fields["ServicePoint6"].SetValue("V");
                                        }
                                        break;
                                }

                                var selectedClauses = record.OrderClause?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(clause => clause.Trim())
                                    .ToList() ?? new List<string>();
                                var doctors = _documentService.GenerateDoctorsList(selectedClauses, record.Age > 40, record.Gender == "Женский" || record.Gender == "ж");

                                var uniqueDoctors = doctors.Distinct().ToList();

                                foreach (var doctor in uniqueDoctors)
                                {
                                    if (_doctorCounts.ContainsKey(doctor))
                                    {
                                        _doctorCounts[doctor]++;
                                    }
                                    else
                                    {
                                        _doctorCounts[doctor] = 1;
                                    }
                                }

                                for (int i = 0; i < uniqueDoctors.Count && i < 12; i++)
                                {
                                    string fieldName = $"Doctor_{i + 1}";
                                    SetFieldValue(fields, fieldName, uniqueDoctors[i], font);
                                }

                                var tests = _documentService.GenerateTestsList(record.Age > 40, record.Gender == "Женский" || record.Gender == "ж", selectedClauses);

                                var testsWithDirectMatch = GetTestsWithDirectMatch();
                                foreach (var test in tests)
                                {
                                    if (testsWithDirectMatch.Contains(test))
                                    {
                                        string fieldName = $"test_{SanitizeFieldName(test)}";
                                        if (fields.ContainsKey(fieldName))
                                        {
                                            fields[fieldName].SetValue("V");
                                        }
                                    }
                                }

                                var uniqueTests = tests.Distinct().ToList();

                                foreach (var test in uniqueTests)
                                {
                                    if (_testCounts.ContainsKey(test))
                                    {
                                        _testCounts[test]++;
                                    }
                                    else
                                    {
                                        _testCounts[test] = 1;
                                    }
                                }

                                form.FlattenFields();
                                AddTestsPage(pdf, uniqueTests, font);

                                pdf.Close();
                            }
                        }
                    }
                    catch (iText.Kernel.Exceptions.PdfException pdfEx)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    finally
                    {
                        if (File.Exists(tempTemplatePath))
                        {
                            try
                            {
                                File.Delete(tempTemplatePath);
                            }
                            catch (Exception)
                            {
                                // Игнорируем ошибки при удалении временного файла
                            }
                        }
                    }

                    recordNumber++;
                }

                await SaveStatisticsToExcel(folderPath);

                await ShowMessageBox(parentWindow, $"Все PDF-файлы успешно сохранены в папке:\n{folderPath}\nСтатистика врачей и исследований сохранена в Statistics.xlsx", "Успех");
            }
            catch (Exception ex)
            {
                await ShowMessageBox(parentWindow, $"Ошибка при сохранении PDF:\n{ex.Message}", "Ошибка");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private string SanitizeFieldName(string name)
        {
            return name.Replace(" ", "_")
                       .Replace("(", "")
                       .Replace(")", "")
                       .Replace(",", "")
                       .Replace(".", "")
                       .Replace(":", "")
                       .Replace(";", "")
                       .Replace("/", "_");
        }

        private List<string> GetTestsWithDirectMatch()
        {
            return new List<string>
            {
                "Исследование крови на сифилис",
                "Исследование уровня аспартат-трансаминазы и аланин-трансаминазы",
                "Исследование уровня креатинина",
                "Исследование уровня мочевины",
                "Исследование уровня калия",
                "Исследование уровня натрия",
                "Исследование уровня железа",
                "Исследование уровня щелочной фосфатазы",
                "Исследование уровня билирубина",
                "Исследование уровня общего белка",
                "Исследование уровня триглицеридов",
                "Исследование уровня холестерина",
                "Исследование уровня фибриногена",
                "Исследование уровня ретикулоцитов в крови",
                "Исследование уровня метгемоглобина в крови",
                "Исследование уровня карбоксигемоглобина в крови",
                "Исследование уровня ретикулоцитов, метгемоглобина в крови",
                "Исследование уровня ретикулоцитов, тромбоцитов в крови",
                "Определение группы крови и резус-фактора"
            };
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

                    testsSheet.Cells[1, 1, row - 1, 2].AutoFitColumns();

                    File.WriteAllBytes(outputPath, package.GetAsByteArray());
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении статистики в Excel: {ex.Message}", ex);
            }
        }

        private void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string value, PdfFont font)
        {
            if (fields.TryGetValue(fieldName, out var pdfField))
            {
                pdfField.SetValue(value ?? "");

                if (fieldName == "CurrentYear")
                {
                    pdfField.SetFontAndSize(font, 24f);
                    return;
                }

                float fontSize = 10.5f;
                pdfField.SetFontAndSize(font, fontSize);

                var widget = pdfField.GetWidgets().FirstOrDefault();
                if (widget == null) return;
                var rect = widget.GetRectangle();
                float fieldWidth = rect.GetAsNumber(2).FloatValue() - rect.GetAsNumber(0).FloatValue();

                float textWidth;
                float minFontSize = 6f;
                do
                {
                    textWidth = font.GetWidth(value ?? "", fontSize);
                    if (textWidth > fieldWidth && fontSize > minFontSize)
                    {
                        fontSize -= 0.5f;
                    }
                    else
                    {
                        break;
                    }
                } while (true);

                pdfField.SetFontAndSize(font, fontSize);
            }
        }

        private void AddTestsPage(PdfDocument pdfDocument, List<string> tests, PdfFont font)
        {
            // Убедимся, что в документе есть как минимум 11 страниц
            int currentPageCount = pdfDocument.GetNumberOfPages();
            while (currentPageCount < 11)
            {
                pdfDocument.AddNewPage();
                currentPageCount++;
            }

            // Получаем 11-ю страницу
            var page = pdfDocument.GetPage(11);
            var pageSize = page.GetPageSize();

            // Определяем область для всей страницы с отступами (A4: ширина 595, высота 842)
            var fullPage = new Rectangle(36, 36, pageSize.GetWidth() - 72, pageSize.GetHeight() - 72); // Отступы 36 пунктов со всех сторон

            // Создаём PdfCanvas и iText.Layout.Canvas для управления позицией текста
            var column = new PdfCanvas(page);
            var columnText = new iText.Layout.Canvas(column, fullPage);

            // Создаём параграф с текстом
            var paragraph = new Paragraph()
                .SetFont(font)
                .SetFontSize(7);

            paragraph.Add(new Text("Список исследований:\n\n"));
            int testNumber = 1;
            foreach (var test in tests)
            {
                paragraph.Add(new Text($"{testNumber}. {test}\n"));
                testNumber++;
            }

            // Добавляем параграф на 11-ю страницу
            columnText.Add(paragraph);
            columnText.Close();
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = new string(System.IO.Path.GetInvalidFileNameChars());
            var regex = new Regex($"[{Regex.Escape(invalidChars)}]");
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
    }
}
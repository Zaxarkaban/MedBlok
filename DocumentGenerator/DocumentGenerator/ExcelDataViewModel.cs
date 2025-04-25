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

            public string? Workplace { get; set; } = "";
            public string? MedicalFacility { get; set; } = "";
            public string? WorkExperience { get; set; } = "";
            public string? MedicalOrganization { get; set; } = "";
            public string? Okved { get; set; } = "";
            public string? ServicePoint { get; set; } = "";
            public string? WorkAddress { get; set; } = "";
            public string? Department { get; set; } = "";
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
                            PassportIssuedBy = worksheet.Cells[row, 14].Text?.Trim(),
                            // Новые поля
                            Workplace = worksheet.Cells[row, 15].Text?.Trim() ?? "",
                            MedicalFacility = worksheet.Cells[row, 16].Text?.Trim() ?? "",
                            WorkExperience = worksheet.Cells[row, 17].Text?.Trim() ?? "",
                            MedicalOrganization = worksheet.Cells[row, 18].Text?.Trim() ?? "",
                            Okved = worksheet.Cells[row, 19].Text?.Trim() ?? "",
                            ServicePoint = worksheet.Cells[row, 20].Text?.Trim() ?? "",
                            WorkAddress = worksheet.Cells[row, 21].Text?.Trim() ?? "",
                            Department = worksheet.Cells[row, 22].Text?.Trim() ?? "",
                            OwnershipForm = worksheet.Cells[row, 23].Text?.Trim() ?? ""
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

                                // Вычисляем возраст и пол
                                int age = 0;
                                bool isFemale = record.Gender == "Женский";
                                if (!string.IsNullOrEmpty(record.DateOfBirth) && DateTime.TryParseExact(record.DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
                                {
                                    var today = DateTime.Today;
                                    age = today.Year - dob.Year;
                                    if (dob.Date > today.AddYears(-age)) age--;
                                }
                                bool isOver40 = age > 40;

                                var form = PdfAcroForm.GetAcroForm(pdf, true);
                                var fields = form.GetAllFormFields();

                                SetFieldValue(fields, "FullName", record.FullName, font);
                                SetFieldValue(fields, "Gender", record.Gender, font);
                                SetFieldValue(fields, "DateOfBirth", record.DateOfBirth, font);
                                SetFieldValue(fields, "DateOfBirth1", record.DateOfBirth, font);
                                SetFieldValue(fields, "Address", "", font);
                                SetFieldValue(fields, "Phone", "", font);
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
                                fields["обязательные_анализы"].SetValue("V"); // Устанавливаем галочку для обязательных анализов

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
                                //Вот тут бахнуть вычисление возраста
                                SetFieldValue(fields, "normasDate", age.ToString(), font); // Заполняем поле возраста в годах

                                string[] fioParts = record.FullName.Split(' ');
                                string lastName = fioParts.Length > 0 ? fioParts[0] : "";
                                string firstName = fioParts.Length > 1 ? fioParts[1] : "";
                                string middleName = fioParts.Length > 2 ? fioParts[2] : "";
                                SetFieldValue(fields, "LastName", lastName, font);
                                SetFieldValue(fields, "FirstName", firstName, font);
                                SetFieldValue(fields, "MiddleName", middleName, font);

                                SetFieldValue(fields, "CheckBoxField", "Yes", font);
                                SetFieldValue(fields, "Document", "паспорт", font);

                                // Устанавливаем галочку для ServicePoint
                                switch (record.ServicePoint)
                                {
                                    case "ПО 67":
                                        if (fields.ContainsKey("ServicePoint1"))
                                        {
                                            fields["ServicePoint1"].SetValue("V");
                                        }
                                        break;
                                    case "ПО 89":
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

                                // Генерация списка исследований
                                isFemale = record.Gender == "Женский" || record.Gender == "ж";
                                isOver40 = record.Age > 40;
                                var tests = _documentService.GenerateTestsList(isOver40, isFemale, selectedClauses);

                                // Проставление галочек для исследований
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

        // Новый метод для обработки имен полей
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

        // Метод для получения списка исследований с прямым соответствием (перенесен из PdfGenerator.cs)
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
    }
}
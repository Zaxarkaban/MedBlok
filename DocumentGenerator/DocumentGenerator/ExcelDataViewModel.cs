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
                    for (int row = 2; row <= rowCount; row++) // Пропускаем заголовок
                    {
                        var record = new Record
                        {
                            FullName = worksheet.Cells[row, 2].Text, // Столбец 2: Сотрудник
                            Position = worksheet.Cells[row, 3].Text, // Столбец 3: Должность
                            DateOfBirth = worksheet.Cells[row, 5].Text, // Столбец 5: Дата рождения
                            Gender = worksheet.Cells[row, 7].Text, // Столбец 7: Пол
                            OrderClause = worksheet.Cells[row, 8].Text, // Столбец 8: Пункты по приказу 29н
                            Snils = worksheet.Cells[row, 9].Text, // Столбец 9: СНИЛС
                            MedicalPolicy = worksheet.Cells[row, 10].Text, // Столбец 10: Полис ОМС
                            PassportSeries = worksheet.Cells[row, 11].Text, // Столбец 11: Серия паспорта
                            PassportNumber = worksheet.Cells[row, 12].Text, // Столбец 12: Номер паспорта
                            PassportIssueDate = worksheet.Cells[row, 13].Text, // Столбец 13: Дата выдачи паспорта
                            PassportIssuedBy = worksheet.Cells[row, 14].Text // Столбец 14: Кем выдан паспорт
                        };

                        // Преобразование числовой даты рождения в формат dd.MM.yyyy
                        if (double.TryParse(record.DateOfBirth, out double dateOfBirthDays))
                        {
                            DateTime dateOfBirth = DateTime.FromOADate(dateOfBirthDays);
                            record.DateOfBirth = dateOfBirth.ToString("dd.MM.yyyy");
                        }

                        // Вычисление возраста на основе даты рождения
                        if (DateTime.TryParseExact(record.DateOfBirth, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var birthDate))
                        {
                            var today = DateTime.Today;
                            int age = today.Year - birthDate.Year;
                            if (birthDate.Date > today.AddYears(-age)) age--;
                            record.Age = age;
                        }

                        // Преобразование числовой даты выдачи паспорта в формат dd.MM.yyyy
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
            try
            {
                var storageProvider = parentWindow.StorageProvider;

                // Запрашиваем папку для сохранения файлов
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
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.pdf");

                if (!File.Exists(templatePath))
                {
                    await ShowMessageBox(parentWindow, "Шаблон PDF не найден.", "Ошибка");
                    return;
                }

                // Создаём PDF для каждой записи
                int recordNumber = 1;
                foreach (var record in Records)
                {
                    // Формируем имя файла на основе FullName
                    string safeFileName = SanitizeFileName(record.FullName ?? $"Record_{recordNumber}");
                    string outputPath = Path.Combine(folderPath, $"{safeFileName}.pdf");

                    using (var writer = new PdfWriter(outputPath))
                    using (var pdf = new PdfDocument(new PdfReader(templatePath), writer))
                    {
                        var form = PdfAcroForm.GetAcroForm(pdf, true);
                        var fields = form.GetAllFormFields();

                        // Загружаем шрифт Times New Roman из проекта
                        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "times.ttf");
                        if (!File.Exists(fontPath))
                        {
                            throw new FileNotFoundException("Times New Roman font file not found in the project.", fontPath);
                        }

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

                        // Заполняем поля в шаблоне
                        SetFieldValue(fields, "FullName", record.FullName, font); // Пункт 1: Ф.И.О.
                        SetFieldValue(fields, "Gender", record.Gender, font); // Пункт 2: Пол
                        SetFieldValue(fields, "DateOfBirth", record.DateOfBirth, font); // Пункт 3: Дата рождения
                        SetFieldValue(fields, "Address", "", font); // Пункт 4: Адрес (нет в Record)
                        // Пункт 5: Телефон; серия, №, дата выдачи, кем выдан паспорта; СНИЛС, № полиса мед. страхования
                        SetFieldValue(fields, "Phone", "", font); // Телефон (нет в Record)
                        SetFieldValue(fields, "PassportSeries", record.PassportSeries, font); // Серия паспорта
                        SetFieldValue(fields, "PassportNumber", record.PassportNumber, font); // Номер паспорта
                        SetFieldValue(fields, "PassportIssueDate", record.PassportIssueDate, font); // Дата выдачи паспорта
                        SetFieldValue(fields, "PassportIssuedBy", record.PassportIssuedBy, font); // Кем выдан паспорт
                        SetFieldValue(fields, "Snils", record.Snils, font); // СНИЛС
                        SetFieldValue(fields, "MedicalPolicy", record.MedicalPolicy, font); // Полис ОМС
                        SetFieldValue(fields, "OrderClause", record.OrderClause, font); // Пункт 6: Структурное подразделение организации
                        SetFieldValue(fields, "Workplace", "", font); // Пункт 7: Место работы (нет в Record)
                        SetFieldValue(fields, "MedicalFacility", "", font); // Пункт 8: Структурное подразделение (нет в Record)
                        SetFieldValue(fields, "Position", record.Position, font); // Пункт 9: Должность
                        SetFieldValue(fields, "WorkExperience", "", font); // Пункт 10: Стаж работы (нет в Record)
                        SetFieldValue(fields, "MedicalOrganization", "", font); // Пункт 11: Нагрузочность ЛПУ (нет в Record)

                        // Дополнительные поля, которые могут быть в шаблоне
                        SetFieldValue(fields, "Okved", "", font); // ОКВЭД (нет в Record)
                        SetFieldValue(fields, "Doctors", "", font); // Врачи (нет в Record)

                        form.FlattenFields();
                        pdf.Close();
                    }

                    recordNumber++;
                }

                await ShowMessageBox(parentWindow, $"Все PDF-файлы успешно сохранены в папке:\n{folderPath}", "Успех");
            }
            catch (Exception ex)
            {
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
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = new string(Path.GetInvalidFileNameChars());
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
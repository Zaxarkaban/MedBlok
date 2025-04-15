using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using iText.Kernel.Font;
using iText.IO.Font;
using System.Collections.Generic;

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

        public async Task SaveToPdf(Window parentWindow)
        {
            try
            {
                var storageProvider = parentWindow.StorageProvider;

                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Сохранить PDF",
                    SuggestedFileName = "excel_data.pdf",
                    DefaultExtension = "pdf",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (file == null)
                {
                    await ShowMessageBox(parentWindow, "Сохранение отменено.", "Информация");
                    return;
                }

                var filePath = file.Path.LocalPath;

                using (var writer = new PdfWriter(filePath))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    // Загружаем шрифт Arial с поддержкой кириллицы
                    PdfFont font = PdfFontFactory.CreateFont("C:/Windows/Fonts/arial.ttf", PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

                    // Заголовок документа
                    document.Add(new Paragraph("Данные из Excel")
                        .SetFont(font)
                        .SetFontSize(16)
                        .SetMarginBottom(20));

                    // Добавляем записи
                    int recordNumber = 1;
                    foreach (var record in Records)
                    {
                        document.Add(new Paragraph($"Запись #{recordNumber}")
                            .SetFont(font)
                            .SetFontSize(14)
                            .SetMarginTop(10)
                            .SetMarginBottom(5));

                        document.Add(new Paragraph($"ФИО: {record.FullName ?? "Не указано"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Должность: {record.Position ?? "Не указана"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Дата рождения: {record.DateOfBirth ?? "Не указана"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Возраст: {record.Age}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Пол: {record.Gender ?? "Не указан"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Пункты по приказу: {record.OrderClause ?? "Не указан"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"СНИЛС: {record.Snils ?? "Не указан"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Полис ОМС: {record.MedicalPolicy ?? "Не указан"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Паспорт: {(record.PassportSeries ?? "Не указана")} {(record.PassportNumber ?? "Не указан")}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Дата выдачи паспорта: {record.PassportIssueDate ?? "Не указана"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(5));
                        document.Add(new Paragraph($"Кем выдан: {record.PassportIssuedBy ?? "Не указано"}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetMarginBottom(10));

                        recordNumber++;
                    }
                }

                await ShowMessageBox(parentWindow, $"PDF успешно сохранён по пути:\n{filePath}", "Успех");
            }
            catch (Exception ex)
            {
                await ShowMessageBox(parentWindow, $"Ошибка при сохранении PDF:\n{ex.Message}", "Ошибка");
            }
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

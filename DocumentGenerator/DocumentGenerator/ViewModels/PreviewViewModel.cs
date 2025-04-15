using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using iText.Kernel.Font;
using iText.IO.Font;

namespace DocumentGenerator.ViewModels
{
    public class PreviewViewModel
    {
        public string? FullName { get; set; } = "";
        public string? Position { get; set; } = "";
        public int Age { get; set; }
        public string? Gender { get; set; } = "";
        public string? OrderClause { get; set; } = "";
        public string? Snils { get; set; } = "";
        public string? PassportSeries { get; set; } = "";
        public string? PassportNumber { get; set; } = "";
        public string? PassportIssueDate { get; set; } = "";
        public string? PassportIssuedBy { get; set; } = "";
        public string? MedicalPolicy { get; set; } = "";

        public async Task SaveToPdf(Window parentWindow)
        {
            try
            {
                var storageProvider = parentWindow.StorageProvider;

                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Сохранить PDF",
                    SuggestedFileName = "document.pdf",
                    DefaultExtension = "pdf",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (file == null)
                {
                    await MessageBox.Show(parentWindow, "Сохранение отменено.", "Информация", MessageBox.MessageBoxButtons.Ok);
                    return;
                }

                var filePath = file.Path.LocalPath;

                using (var writer = new PdfWriter(filePath))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    // Загружаем шрифт Arial
                    PdfFont font = PdfFontFactory.CreateFont("C:/Windows/Fonts/arial.ttf", PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

                    // Заголовок
                    document.Add(new Paragraph("Готовый документ")
                        .SetFont(font)
                        .SetFontSize(16)
                        .SetMarginBottom(20));

                    // Данные
                    document.Add(new Paragraph($"ФИО: {FullName ?? "Не указано"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Должность: {Position ?? "Не указана"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Возраст: {Age}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Пол: {Gender ?? "Не указан"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Пункт приказа: {OrderClause ?? "Не указан"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"СНИЛС: {Snils ?? "Не указан"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Паспорт: {(PassportSeries ?? "Не указана")} {(PassportNumber ?? "Не указан")}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Дата выдачи паспорта: {PassportIssueDate ?? "Не указана"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Кем выдан: {PassportIssuedBy ?? "Не указано"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                    document.Add(new Paragraph($"Полис ОМС: {MedicalPolicy ?? "Не указан"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetMarginBottom(5));
                }

                await MessageBox.Show(parentWindow, $"PDF успешно сохранён по пути:\n{filePath}", "Успех", MessageBox.MessageBoxButtons.Ok);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(parentWindow, $"Ошибка при сохранении PDF:\n{ex.Message}", "Ошибка", MessageBox.MessageBoxButtons.Ok);
            }
        }

        private class MessageBox
        {
            public enum MessageBoxButtons
            {
                Ok
            }

            public static async Task Show(Window parent, string message, string title, MessageBoxButtons buttons)
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
}
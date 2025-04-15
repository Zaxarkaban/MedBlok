using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;

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
                    document.Add(new Paragraph("Готовый документ"));
                    document.Add(new Paragraph($"ФИО: {FullName ?? "Не указано"}"));
                    document.Add(new Paragraph($"Должность: {Position ?? "Не указана"}"));
                    document.Add(new Paragraph($"Возраст: {Age}"));
                    document.Add(new Paragraph($"Пол: {Gender ?? "Не указан"}"));
                    document.Add(new Paragraph($"Пункт приказа: {OrderClause ?? "Не указан"}"));
                    document.Add(new Paragraph($"СНИЛС: {Snils ?? "Не указан"}"));
                    document.Add(new Paragraph($"Паспорт: {(PassportSeries ?? "Не указана")} {(PassportNumber ?? "Не указан")}"));
                    document.Add(new Paragraph($"Дата выдачи паспорта: {PassportIssueDate ?? "Не указана"}"));
                    document.Add(new Paragraph($"Кем выдан: {PassportIssuedBy ?? "Не указано"}"));
                    document.Add(new Paragraph($"Полис ОМС: {MedicalPolicy ?? "Не указан"}"));
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
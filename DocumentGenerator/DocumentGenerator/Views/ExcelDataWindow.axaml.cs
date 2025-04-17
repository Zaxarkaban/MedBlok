using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DocumentGenerator.Services;
// Удаляем using DocumentGenerator.ViewModels, так как ExcelDataViewModel находится в DocumentGenerator
using DocumentGenerator.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public partial class ExcelDataWindow : Window
    {
        private readonly ExcelDataViewModel _viewModel;
        private readonly PdfGenerator _pdfGenerator;

        public ExcelDataWindow(ExcelDataViewModel viewModel, PdfGenerator pdfGenerator)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
            DataContext = _viewModel;
            InitializeComponent();
        }

        private async void SaveToPdf_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить PDF-документ",
                DefaultExtension = "pdf",
                SuggestedFileName = $"{SanitizeFileName(_viewModel.MainViewModel.FullName ?? "Document")}.pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF Files")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    }
                }
            });

            if (file != null)
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.pdf");
                string outputPath = file.Path.LocalPath;
                await _pdfGenerator.GeneratePdfAsync(outputPath, templatePath);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName.Trim();
        }
    }
}
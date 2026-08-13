using Avalonia.Controls;
using Avalonia.Interactivity;
using DocumentGenerator.Models.UserPrograms;
using DocumentGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public partial class DynamicProgramWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserProgramStorage _storage;
        private readonly IPdfFormFiller _pdfFiller;
        private readonly UserProgramInfo _info;
        private readonly UserProgramDefinition _definition;

        private readonly Dictionary<string, Control> _inputByKey = new();
        private readonly Dictionary<string, TextBlock> _errorByKey = new();

        public DynamicProgramWindow()
        {
            InitializeComponent();
            _serviceProvider = null!;
            _storage = null!;
            _info = null!;
            _definition = null!;
        }

        public DynamicProgramWindow(IServiceProvider serviceProvider, UserProgramInfo info) : this()
        {
            _serviceProvider = serviceProvider;
            AppWindowSetup.Configure(this, serviceProvider);
            if (this.FindControl<Button>("BackButton") is Button backButton)
                backButton.Click += Back_Click;
            _storage = _serviceProvider.GetRequiredService<IUserProgramStorage>();
            _pdfFiller = _serviceProvider.GetRequiredService<IPdfFormFiller>();
            _info = info;
            _definition = _storage.LoadDefinition(info.ProgramJsonPath);

            Title = _definition.Name;

            BuildUi();
        }

        private void BuildUi()
        {
            var stack = this.FindControl<StackPanel>("FormStackPanel");
            if (stack == null) return;

            if (this.FindControl<TextBlock>("ProgramTitleTextBlock") is TextBlock title)
                title.Text = _definition.Name;

            foreach (var field in _definition.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key))
                    field.Key = Guid.NewGuid().ToString("N");

                stack.Children.Add(new TextBlock { Text = field.Label });

                Control input = CreateInput(field);
                stack.Children.Add(input);

                var err = new TextBlock { Foreground = Avalonia.Media.Brushes.Red, Text = "" };
                stack.Children.Add(err);

                _inputByKey[field.Key] = input;
                _errorByKey[field.Key] = err;
            }

            // Buttons
            var preview = new Button { Name = "PreviewButton", Content = "Предпросмотр", Margin = new Avalonia.Thickness(10) };
            preview.Click += Preview_Click;

            var save = new Button { Name = "SaveButton", Content = "Сохранить", Margin = new Avalonia.Thickness(10) };
            save.Click += Save_Click;

            stack.Children.Add(preview);
            stack.Children.Add(save);
        }

        private Control CreateInput(UserProgramFieldDefinition field)
        {
            switch (field.Type)
            {
                case UserProgramFieldType.Combo:
                    var combo = new ComboBox
                    {
                        ItemsSource = field.Options ?? new List<string>(),
                        SelectedIndex = -1
                    };
                    combo.SelectionChanged += (_, _) => ValidateField(field);
                    return combo;

                case UserProgramFieldType.Checkbox:
                    var cb = new CheckBox
                    {
                        Content = ""
                    };
                    cb.IsCheckedChanged += (_, _) => ValidateField(field);
                    return cb;

                case UserProgramFieldType.MultiSelect:
                    var lb = new ListBox
                    {
                        ItemsSource = field.Options ?? new List<string>(),
                        SelectionMode = SelectionMode.Multiple,
                        Height = 160
                    };
                    lb.SelectionChanged += (_, _) => ValidateField(field);
                    return lb;

                case UserProgramFieldType.Number:
                case UserProgramFieldType.Date:
                case UserProgramFieldType.Text:
                default:
                    var tb = new TextBox { Text = "" };
                    tb.TextChanged += (_, _) => ValidateField(field);
                    return tb;
            }
        }

        private Dictionary<string, object?> CollectValues()
        {
            var values = new Dictionary<string, object?>();
            foreach (var field in _definition.Fields)
            {
                if (!_inputByKey.TryGetValue(field.Key, out var control))
                    continue;
                values[field.PdfFieldName] = ReadControlValue(control, field.Type);
            }
            return values;
        }

        private static object? ReadControlValue(Control control, UserProgramFieldType type)
        {
            return type switch
            {
                UserProgramFieldType.Combo => (control as ComboBox)?.SelectedItem as string,
                UserProgramFieldType.Checkbox => (control as CheckBox)?.IsChecked == true,
                UserProgramFieldType.MultiSelect => (control as ListBox)?.SelectedItems?.Cast<string>().ToList(),
                _ => (control as TextBox)?.Text
            };
        }

        private bool ValidateAll()
        {
            bool ok = true;
            foreach (var field in _definition.Fields)
            {
                if (!ValidateField(field))
                    ok = false;
            }
            return ok;
        }

        private string? GetValidationSummary()
        {
            ValidateAll();
            var fields = new List<(string Label, string? Error)>();
            foreach (var field in _definition.Fields)
            {
                if (_errorByKey.TryGetValue(field.Key, out var error) && !string.IsNullOrWhiteSpace(error.Text))
                    fields.Add((field.Label ?? field.Key, error.Text));
            }

            return ValidationSummaryHelper.BuildMessage(fields);
        }

        private bool ValidateField(UserProgramFieldDefinition field)
        {
            if (!_inputByKey.TryGetValue(field.Key, out var control) || !_errorByKey.TryGetValue(field.Key, out var error))
                return true;

            string message = "";
            var raw = ReadControlValue(control, field.Type);

            bool isEmpty =
                raw == null ||
                (raw is string s && string.IsNullOrWhiteSpace(s)) ||
                (raw is List<string> list && list.Count == 0);

            // Required
            if (field.Required && isEmpty)
                message = "Поле обязательно.";

            // Rules
            if (string.IsNullOrEmpty(message))
            {
                foreach (var rule in field.Validation)
                {
                    switch (rule.Type)
                    {
                        case UserProgramValidationType.Required:
                            if (isEmpty) message = rule.Message ?? "Поле обязательно.";
                            break;

                        case UserProgramValidationType.MaxLength:
                            if (raw is string str && rule.MaxLength is int ml && ml > 0 && str.Length > ml)
                                message = rule.Message ?? $"Максимальная длина: {ml}.";
                            break;

                        case UserProgramValidationType.Regex:
                            if (raw is string str2 && !string.IsNullOrWhiteSpace(rule.RegexPattern))
                            {
                                try
                                {
                                    if (!Regex.IsMatch(str2, rule.RegexPattern))
                                        message = rule.Message ?? "Не соответствует формату.";
                                }
                                catch
                                {
                                    message = "Ошибка Regex в настройках поля.";
                                }
                            }
                            break;

                        case UserProgramValidationType.Range:
                            if (raw is string str3 && (!string.IsNullOrWhiteSpace(str3)))
                            {
                                if (double.TryParse(str3, out var num))
                                {
                                    if (rule.Min is double min && num < min)
                                        message = rule.Message ?? $"Минимум: {min}.";
                                    if (string.IsNullOrEmpty(message) && rule.Max is double max && num > max)
                                        message = rule.Message ?? $"Максимум: {max}.";
                                }
                                else
                                {
                                    message = "Введите число.";
                                }
                            }
                            break;
                    }

                    if (!string.IsNullOrEmpty(message))
                        break;
                }
            }

            error.Text = message;
            return string.IsNullOrEmpty(message);
        }

        private async void Preview_Click(object? sender, RoutedEventArgs e)
        {
            var validationMessage = GetValidationSummary();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                await AppDialog.ShowAsync(this, validationMessage, "Заполните обязательные поля", AppDialogKind.Warning);
                return;
            }

            try
            {
                var values = CollectValues();
                var tempPath = Path.Combine(Path.GetTempPath(), $"Preview_{Guid.NewGuid():N}.pdf");
                _pdfFiller.FillToFile(_info.TemplatePdfPath, tempPath, values);

                if (File.Exists(tempPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    await AppDialog.ShowAsync(this, "Не удалось создать файл предпросмотра.", "Ошибка", AppDialogKind.Error);
                }
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка предпросмотра: {ex.Message}", "Ошибка", AppDialogKind.Error);
            }
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            var validationMessage = GetValidationSummary();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                await AppDialog.ShowAsync(this, validationMessage, "Заполните обязательные поля", AppDialogKind.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Сохранить PDF-документ",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "PDF Files", Extensions = { "pdf" } }
                },
                DefaultExtension = "pdf",
                InitialFileName = $"{SanitizeFileName(_definition.Name)}.pdf"
            };

            var result = await saveFileDialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result))
                return;

            try
            {
                var values = CollectValues();
                _pdfFiller.FillToFile(_info.TemplatePdfPath, result, values);
                await AppDialog.ShowAsync(this, "Файл успешно сохранён!", "Успех", AppDialogKind.Success);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка сохранения: {ex.Message}", "Ошибка", AppDialogKind.Error);
            }
        }

        private void Back_Click(object? sender, RoutedEventArgs e)
        {
            var win = _serviceProvider.GetRequiredService<UserProgramsWindow>();
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName.Trim();
        }
    }
}


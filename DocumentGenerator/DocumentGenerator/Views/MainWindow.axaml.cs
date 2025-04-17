using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DocumentGenerator.Services;
using DocumentGenerator.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Views
{
    public partial class MainWindow : Window
    {

        private readonly PdfGenerator _pdfGenerator = null!;
        private readonly MainWindowViewModel _viewModel;
        private readonly ExcelDataViewModel _excelDataViewModel;
        private const int CurrentYear = 2025;
        private const int MinYear = CurrentYear - 120; // 1905

        public MainWindow(PdfGenerator pdfGenerator, MainWindowViewModel viewModel, ExcelDataViewModel excelDataViewModel)
        {
            _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _excelDataViewModel = excelDataViewModel ?? throw new ArgumentNullException(nameof(excelDataViewModel));
            DataContext = _viewModel;
            InitializeComponent();
        }

        public MainWindow()
        {
            if (Design.IsDesignMode)
            {
                DataContext = new MainWindowViewModel();
                InitializeComponent();
                return;
            }

            throw new InvalidOperationException("This constructor is only for design-time use. Use the parameterized constructor with DI.");
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            string? text = textBox.Text; // textBox.Text может быть null
            if (string.IsNullOrEmpty(text)) return;

            string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
            var (filteredText, caretIndex) = name switch
            {
                "FullNameTextBox" or "PositionTextBox" or "PassportIssuedByTextBox" or "MedicalFacilityTextBox" =>
                    (text.Length > 1000 ? text.Substring(0, 1000) : text, Math.Min(text.Length, 1000)),
                "PassportSeriesTextBox" or "PassportNumberTextBox" or "MedicalPolicyTextBox" =>
                    FilterNumericInput(text, GetMaxLength(name), textBox),
                "DateOfBirthTextBox" or "PassportIssueDateTextBox" =>
                    FormatAndValidateDate(text, textBox),
                "SnilsTextBox" =>
                    FormatAndValidateSnils(text, textBox),
                "AddressTextBox" or "MedicalOrganizationTextBox" or "WorkplaceTextBox" =>
                    (text.Length > 1000 ? text.Substring(0, 1000) : text, Math.Min(text.Length, 1000)),
                "PhoneTextBox" =>
                    FormatAndValidatePhone(text, textBox),
                "OkvedTextBox" =>
                    FormatAndValidateOkved(text, textBox),
                "WorkExperienceTextBox" =>
                    FormatAndValidateWorkExperience(text, textBox),
                _ => (text, text.Length)
            };

            if (text != filteredText)
            {
                textBox.Text = filteredText;
                textBox.CaretIndex = caretIndex;
            }

            _viewModel.GetType().GetMethod($"Validate{name.Replace("TextBox", "")}")?.Invoke(_viewModel, null);

            int maxLength = GetMaxLengthForField(name);
            if (filteredText.Length == maxLength)
            {
                MoveFocus(textBox, true);
            }
        }

        private void RestrictInput(object sender, TextInputEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (string.IsNullOrEmpty(e.Text)) return;

            string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
            string currentText = textBox.Text ?? "";
            string newText = currentText + e.Text;

            bool isValid = name switch
            {
                "FullNameTextBox" or "PositionTextBox" or "PassportIssuedByTextBox" or "MedicalFacilityTextBox" =>
                    newText.Length <= 1000,
                "PassportSeriesTextBox" or "PassportNumberTextBox" or "MedicalPolicyTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Length <= GetMaxLength(name),
                "DateOfBirthTextBox" or "PassportIssueDateTextBox" =>
                    ValidateDateInput(currentText, e.Text),
                "SnilsTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Replace("-", "").Replace(" ", "").Length <= 11,
                "AddressTextBox" or "MedicalOrganizationTextBox" or "WorkplaceTextBox" =>
                    newText.Length <= 1000,
                "PhoneTextBox" =>
                    ValidatePhoneInput(currentText, e.Text),
                "OkvedTextBox" =>
                    ValidateOkvedInput(currentText, e.Text),
                "WorkExperienceTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Replace(" лет", "").Length <= 2,
                _ => true
            };

            if (!isValid) e.Handled = true;
        }

        private void RestrictKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (e.Key is Key.Enter or Key.Up or Key.Down)
            {
                InputField_KeyDown(sender, e);
                return;
            }

            if ((e.Key == Key.V && (e.KeyModifiers & KeyModifiers.Control) != 0) ||
                (e.Key == Key.Insert && (e.KeyModifiers & KeyModifiers.Shift) != 0))
            {
                e.Handled = true;

                Task.Run(async () =>
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard == null) return;

                    string? clipboardText = await clipboard.GetTextAsync();
                    if (string.IsNullOrEmpty(clipboardText)) return;

                    string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
                    string filteredText = name switch
                    {
                        "FullNameTextBox" or "PositionTextBox" or "PassportIssuedByTextBox" =>
                            clipboardText.Length > 1000 ? clipboardText.Substring(0, 1000) : clipboardText,
                        "PassportSeriesTextBox" or "PassportNumberTextBox" or "MedicalPolicyTextBox" =>
                            new string(clipboardText.Where(char.IsDigit).Take(GetMaxLength(name)).ToArray()),
                        "DateOfBirthTextBox" or "PassportIssueDateTextBox" =>
                            FormatAndValidateDate(new string(clipboardText.Where(c => char.IsDigit(c) || c == '.').ToArray()), textBox).filteredText,
                        "SnilsTextBox" =>
                            FormatAndValidateSnils(new string(clipboardText.Where(c => char.IsDigit(c) || c == '-' || c == ' ').ToArray()), textBox).filteredText,
                        "AddressTextBox" or "MedicalOrganizationTextBox" or "WorkplaceTextBox" =>
                            clipboardText.Length > 1000 ? clipboardText.Substring(0, 1000) : clipboardText,
                        "PhoneTextBox" =>
                            FormatAndValidatePhone(new string(clipboardText.Where(c => char.IsDigit(c) || c == '+' || c == ' ' || c == '(' || c == ')' || c == '-').ToArray()), textBox).filteredText,
                        "OkvedTextBox" =>
                            FormatAndValidateOkved(new string(clipboardText.Where(c => char.IsDigit(c) || c == '.').ToArray()), textBox).filteredText,
                        "WorkExperienceTextBox" =>
                            new string(clipboardText.Where(char.IsDigit).Take(2).ToArray()) + " лет",
                        _ => clipboardText
                    };

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        textBox.Text = filteredText;
                        textBox.CaretIndex = filteredText.Length;

                        int maxLength = GetMaxLengthForField(name);
                        if (filteredText.Length == maxLength)
                        {
                            MoveFocus(textBox, true);
                        }
                    });
                });
            }
        }

        private (string filteredText, int caretIndex) FilterNumericInput(string text, int maxLength, TextBox textBox)
        {
            string filtered = new string(text.Where(char.IsDigit).ToArray());
            if (filtered.Length > maxLength)
            {
                filtered = filtered.Substring(0, maxLength);
            }
            return (filtered, filtered.Length);
        }

        private (string filteredText, int caretIndex) FormatAndValidateDate(string text, TextBox textBox)
        {
            string digits = new string(text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return (text, text.Length);

            string validatedDigits = "";
            for (int i = 0; i < digits.Length; i++)
            {
                string currentDigits = validatedDigits + digits[i];
                int pos = currentDigits.Length;

                if (pos <= 2)
                {
                    if (pos == 1)
                    {
                        int dayFirstDigit = int.Parse(currentDigits);
                        if (dayFirstDigit > 3) return (validatedDigits, validatedDigits.Length);
                    }
                    else if (pos == 2)
                    {
                        int day = int.Parse(currentDigits);
                        if (day > 31 || day == 0) return (validatedDigits[0].ToString(), 1);
                    }
                }
                else if (pos <= 4)
                {
                    if (pos == 3)
                    {
                        int monthFirstDigit = int.Parse(currentDigits[2].ToString());
                        if (monthFirstDigit > 1)
                        {
                            string formatted = FormatDateInput(validatedDigits);
                            return (formatted, formatted.Length);
                        }
                    }
                    else if (pos == 4)
                    {
                        int month = int.Parse(currentDigits.Substring(2, 2));
                        if (month > 12 || month == 0)
                        {
                            string formatted = FormatDateInput(validatedDigits);
                            return (formatted, formatted.Length);
                        }
                    }
                }
                else if (pos <= 8)
                {
                    if (pos == 5)
                    {
                        int yearFirstDigit = int.Parse(currentDigits[4].ToString());
                        if (yearFirstDigit != 1 && yearFirstDigit != 2)
                        {
                            string formatted = FormatDateInput(validatedDigits);
                            return (formatted, formatted.Length);
                        }
                    }
                    else if (pos == 6)
                    {
                        int yearFirstTwo = int.Parse(currentDigits.Substring(4, 2));
                        if (yearFirstTwo != 19 && yearFirstTwo != 20)
                        {
                            string formatted = FormatDateInput(validatedDigits);
                            return (formatted, formatted.Length);
                        }
                    }
                    else if (pos == 7)
                    {
                        int yearFirstTwo = int.Parse(currentDigits.Substring(4, 2));
                        int yearThirdDigit = int.Parse(currentDigits[6].ToString());
                        if (yearFirstTwo == 20 && yearThirdDigit > 2)
                        {
                            string formatted = FormatDateInput(validatedDigits);
                            return (formatted, formatted.Length);
                        }
                    }
                    else if (pos == 8)
                    {
                        int year = int.Parse(currentDigits.Substring(4, 4));
                        if (year < MinYear || year > CurrentYear)
                        {
                            string formatted = FormatDateInput(validatedDigits);
                            return (formatted, formatted.Length);
                        }
                    }
                }

                validatedDigits = currentDigits;
            }

            string finalFormatted = FormatDateInput(validatedDigits);
            return (finalFormatted, finalFormatted.Length);
        }

        private bool ValidateDateInput(string currentText, string input)
        {
            if (!input.All(char.IsDigit)) return false;

            string digits = currentText.Replace(".", "") + input;
            int pos = digits.Length;

            if (pos <= 2)
            {
                if (pos == 1)
                {
                    int dayFirstDigit = int.Parse(input);
                    if (dayFirstDigit > 3) return false;
                }
                else if (pos == 2)
                {
                    int day = int.Parse(digits[..2]);
                    if (day > 31 || day == 0) return false;
                }
            }
            else if (pos <= 4)
            {
                if (pos == 3)
                {
                    int monthFirstDigit = int.Parse(input);
                    if (monthFirstDigit > 1) return false;
                }
                else if (pos == 4)
                {
                    int month = int.Parse(digits.Substring(2, 2));
                    if (month > 12 || month == 0) return false;
                }
            }
            else if (pos <= 8)
            {
                if (pos == 5)
                {
                    int yearFirstDigit = int.Parse(input);
                    if (yearFirstDigit != 1 && yearFirstDigit != 2) return false;
                }
                else if (pos == 6)
                {
                    int yearFirstTwo = int.Parse(digits.Substring(4, 2));
                    if (yearFirstTwo != 19 && yearFirstTwo != 20) return false;
                }
                else if (pos == 7)
                {
                    int yearFirstTwo = int.Parse(digits.Substring(4, 2));
                    int yearThirdDigit = int.Parse(input);
                    if (yearFirstTwo == 20 && yearThirdDigit > 2) return false;
                }
                else if (pos == 8)
                {
                    int year = int.Parse(digits.Substring(4, 4));
                    if (year < MinYear || year > CurrentYear) return false;
                }
            }

            return pos <= 8;
        }

        private (string filteredText, int caretIndex) FormatAndValidateSnils(string text, TextBox textBox)
        {
            string digits = new string(text.Where(c => char.IsDigit(c) || c == '-' || c == ' ').ToArray());
            if (string.IsNullOrEmpty(digits)) return (digits, 0);

            digits = digits.Replace("-", "").Replace(" ", "");
            if (digits.Length > 11) digits = digits.Substring(0, 11);

            string formatted = digits;
            if (digits.Length >= 3)
                formatted = digits.Substring(0, 3) + (digits.Length > 3 ? "-" + digits.Substring(3) : "");
            if (digits.Length >= 6)
                formatted = formatted.Substring(0, 7) + (digits.Length > 6 ? "-" + digits.Substring(6) : "");
            if (digits.Length >= 9)
                formatted = formatted.Substring(0, 11) + (digits.Length > 9 ? " " + digits.Substring(9) : "");

            return (formatted, formatted.Length);
        }

        private (string filteredText, int caretIndex) FormatAndValidatePhone(string text, TextBox textBox)
        {
            string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());
            if (string.IsNullOrEmpty(digits)) return ("+", 1);

            digits = digits.Replace("+", "");
            if (string.IsNullOrEmpty(digits)) return ("+", 1);

            if (digits.Length > 11) digits = digits.Substring(0, 11);

            string validatedDigits = "";
            for (int i = 0; i < digits.Length; i++)
            {
                string currentDigits = validatedDigits + digits[i];
                int pos = currentDigits.Length;

                if (pos == 1)
                {
                    if (digits[i] != '7' && digits[i] != '8')
                    {
                        return (validatedDigits.Length > 0 ? "+" + validatedDigits : "+", validatedDigits.Length + 1);
                    }
                }

                validatedDigits = currentDigits;
            }

            string formatted = "+";
            if (validatedDigits.Length > 0)
                formatted += validatedDigits[0];
            if (validatedDigits.Length > 1)
                formatted += " (" + validatedDigits.Substring(1, Math.Min(3, validatedDigits.Length - 1));
            if (validatedDigits.Length >= 4)
                formatted += ")";
            if (validatedDigits.Length > 4)
                formatted += " " + validatedDigits.Substring(4, Math.Min(3, validatedDigits.Length - 4));
            if (validatedDigits.Length > 7)
                formatted += "-" + validatedDigits.Substring(7, Math.Min(2, validatedDigits.Length - 7));
            if (validatedDigits.Length > 9)
                formatted += "-" + validatedDigits.Substring(9);

            return (formatted, formatted.Length);
        }

        private bool ValidatePhoneInput(string currentText, string input)
        {
            if (!input.All(char.IsDigit)) return false;

            string digits = currentText.Replace("+", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");
            digits += input;
            int pos = digits.Length;

            if (pos == 1)
            {
                if (input != "7" && input != "8") return false;
            }

            return pos <= 11;
        }

        private (string filteredText, int caretIndex) FormatAndValidateOkved(string text, TextBox textBox)
        {
            string digits = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (string.IsNullOrEmpty(digits)) return (digits, 0);

            digits = digits.Replace(".", "");
            if (digits.Length > 6) digits = digits.Substring(0, 6);

            string formatted = digits;
            if (digits.Length >= 2)
                formatted = digits.Substring(0, 2) + (digits.Length > 2 ? "." + digits.Substring(2) : "");
            if (digits.Length >= 4)
                formatted = formatted.Substring(0, 5) + (digits.Length > 4 ? "." + digits.Substring(4) : "");

            return (formatted, formatted.Length);
        }

        private bool ValidateOkvedInput(string currentText, string input)
        {
            if (!input.All(char.IsDigit)) return false;

            string digits = currentText.Replace(".", "") + input;
            int pos = digits.Length;

            if (pos > 6) return false;

            return true;
        }

        private static string FormatOkvedInput(string digits)
        {
            if (string.IsNullOrEmpty(digits)) return "";
            string result = new string(digits.Where(char.IsDigit).ToArray());
            if (result.Length >= 2)
                result = result.Substring(0, 2) + (result.Length > 2 ? "." + result.Substring(2) : "");
            if (result.Length >= 5)
                result = result.Substring(0, 5) + (result.Length > 4 ? "." + result.Substring(4) : "");
            return result.Length > 8 ? result.Substring(0, 8) : result;
        }

        private (string filteredText, int caretIndex) FormatAndValidateWorkExperience(string text, TextBox textBox)
        {
            string digits = new string(text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return (text, text.Length);

            if (digits.Length > 2) digits = digits.Substring(0, 2);

            int years;
            if (int.TryParse(digits, out years))
            {
                if (years > 80) digits = "80";
                else if (years < 0) digits = "0";
            }

            string formatted = digits + " лет";
            return (formatted, formatted.Length);
        }

        private static string FormatDateInput(string digits)
        {
            if (string.IsNullOrEmpty(digits)) return "";
            string result = digits;
            if (digits.Length >= 2)
                result = digits.Substring(0, 2) + (digits.Length > 2 ? "." + digits.Substring(2) : "");
            if (digits.Length >= 4)
                result = result.Substring(0, 5) + (digits.Length > 4 ? "." + digits.Substring(4) : "");
            return result.Length > 10 ? result.Substring(0, 10) : result;
        }

        private static int GetMaxLength(string textBoxName) => textBoxName switch
        {
            "PassportSeriesTextBox" => 4,
            "PassportNumberTextBox" => 6,
            "MedicalPolicyTextBox" => 16,
            _ => throw new ArgumentException($"Unknown TextBox name: {textBoxName}")
        };

        private int GetMaxLengthForField(string textBoxName) => textBoxName switch
        {
            "FullNameTextBox" => 1000,
            "PositionTextBox" => 1000,
            "PassportIssuedByTextBox" => 1000,
            "MedicalFacilityTextBox" => 1000,
            "PassportSeriesTextBox" => 4,
            "PassportNumberTextBox" => 6,
            "MedicalPolicyTextBox" => 16,
            "DateOfBirthTextBox" => 10,
            "PassportIssueDateTextBox" => 10,
            "SnilsTextBox" => 14,
            "AddressTextBox" => 1000,
            "PhoneTextBox" => 18,
            "MedicalOrganizationTextBox" => 1000,
            "WorkplaceTextBox" => 1000,
            "OkvedTextBox" => 8,
            "WorkExperienceTextBox" => 7,
            _ => throw new ArgumentException($"Unknown TextBox name: {textBoxName}")
        };

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OnSave();

            if (string.IsNullOrEmpty(_viewModel.FullNameError) &&
                string.IsNullOrEmpty(_viewModel.PositionError) &&
                string.IsNullOrEmpty(_viewModel.DateOfBirthError) &&
                string.IsNullOrEmpty(_viewModel.GenderError) &&
                string.IsNullOrEmpty(_viewModel.SnilsError) &&
                string.IsNullOrEmpty(_viewModel.PassportSeriesError) &&
                string.IsNullOrEmpty(_viewModel.PassportNumberError) &&
                string.IsNullOrEmpty(_viewModel.PassportIssueDateError) &&
                string.IsNullOrEmpty(_viewModel.PassportIssuedByError) &&
                string.IsNullOrEmpty(_viewModel.AddressError) &&
                string.IsNullOrEmpty(_viewModel.PhoneError) &&
                string.IsNullOrEmpty(_viewModel.MedicalOrganizationError) &&
                string.IsNullOrEmpty(_viewModel.MedicalPolicyError) &&
                string.IsNullOrEmpty(_viewModel.MedicalFacilityError) &&
                string.IsNullOrEmpty(_viewModel.WorkplaceError) &&
                string.IsNullOrEmpty(_viewModel.OwnershipFormError) &&
                string.IsNullOrEmpty(_viewModel.OkvedError) &&
                string.IsNullOrEmpty(_viewModel.WorkExperienceError))
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Сохранить PDF-документ",
                    DefaultExtension = "pdf",
                    SuggestedFileName = $"{SanitizeFileName(_viewModel.FullName ?? "Document")}.pdf",
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
                    await _pdfGenerator.GeneratePdfAsync(outputPath, templatePath); // Используем PdfGenerator
                }
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

        private async void LoadFromExcel_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите файл Excel",
                FileTypeFilter = new[]
                {
            new FilePickerFileType("Excel Files")
            {
                Patterns = new[] { "*.xlsx", "*.xls" },
                MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
            }
        }
            });

            if (file != null && file.Count > 0)
            {
                await _excelDataViewModel.LoadFromExcelAsync(file[0]);
                var excelDataWindow = Program.ServiceProvider.GetRequiredService<ExcelDataWindow>(); // Используем Program.ServiceProvider
                await excelDataWindow.ShowDialog(this);
            }
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OnSave();

            if (string.IsNullOrEmpty(_viewModel.FullNameError) &&
                string.IsNullOrEmpty(_viewModel.PositionError) &&
                string.IsNullOrEmpty(_viewModel.DateOfBirthError) &&
                string.IsNullOrEmpty(_viewModel.GenderError) &&
                string.IsNullOrEmpty(_viewModel.SnilsError) &&
                string.IsNullOrEmpty(_viewModel.PassportSeriesError) &&
                string.IsNullOrEmpty(_viewModel.PassportNumberError) &&
                string.IsNullOrEmpty(_viewModel.PassportIssueDateError) &&
                string.IsNullOrEmpty(_viewModel.PassportIssuedByError) &&
                string.IsNullOrEmpty(_viewModel.AddressError) &&
                string.IsNullOrEmpty(_viewModel.PhoneError) &&
                string.IsNullOrEmpty(_viewModel.MedicalOrganizationError) &&
                string.IsNullOrEmpty(_viewModel.MedicalPolicyError) &&
                string.IsNullOrEmpty(_viewModel.MedicalFacilityError) &&
                string.IsNullOrEmpty(_viewModel.WorkplaceError) &&
                string.IsNullOrEmpty(_viewModel.OwnershipFormError) &&
                string.IsNullOrEmpty(_viewModel.OkvedError) &&
                string.IsNullOrEmpty(_viewModel.WorkExperienceError))
            {
                var previewViewModel = new PreviewViewModel(_viewModel);
                var previewWindow = new PreviewWindow(previewViewModel);
                await previewWindow.ShowDialog(this);
            }
        }

        private void SaveButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.OnSave();
                e.Handled = true;
            }
        }

        private void ComboBox_GotFocus(object sender, GotFocusEventArgs e)
        {
            if (sender is ComboBox comboBox)
                comboBox.IsDropDownOpen = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Control control)
                MoveFocus(control, true);
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not Control control) return;

            if (e.Key == Key.Enter)
            {
                if (control.Name == "WorkExperienceTextBox")
                {
                    var saveButton = this.FindControl<Button>("SaveButton");
                    saveButton?.Focus();
                }
                else
                {
                    MoveFocus(control, true);
                }
                e.Handled = true;
            }
            else if (e.Key is Key.Up or Key.Down)
            {
                if (control is ComboBox comboBox && comboBox.IsDropDownOpen) return;
                MoveFocus(control, e.Key == Key.Down);
                e.Handled = true;
            }
        }

        private void MoveFocus(Control currentControl, bool moveNext)
        {
            var stackPanel = this.FindControl<StackPanel>("MainStackPanel");
            if (stackPanel == null) return;

            var children = stackPanel.Children;
            int currentIndex = children.IndexOf(currentControl);
            int step = moveNext ? 1 : -1;
            int start = moveNext ? currentIndex + 1 : currentIndex - 1;
            int end = moveNext ? children.Count : -1;

            for (int i = start; moveNext ? i < end : i >= end; i += step)
            {
                if (children[i] is TextBox or ComboBox)
                {
                    var nextControl = (Control)children[i];
                    nextControl.Focus();
                    if (nextControl is TextBox textBox)
                    {
                        textBox.SelectionStart = 0;
                        textBox.SelectionEnd = 0;
                    }
                    break;
                }
            }
        }
    }
}
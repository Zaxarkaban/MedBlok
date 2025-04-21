using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DocumentGenerator.ViewModels;
using DocumentGenerator.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DynamicData;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public partial class NewForm : Window
    {
        public NewFormViewModel ViewModel => DataContext as NewFormViewModel; private const int CurrentYear = 2025; private const int MinYear = CurrentYear - 120; // 1905 private readonly IServiceProvider _serviceProvider;
        private readonly IServiceProvider _serviceProvider;
        public NewForm(IServiceProvider serviceProvider)
        {

            InitializeComponent();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            DataContext = new NewFormViewModel(
                new NewFormPdfGenerator(),
                _serviceProvider
            );

            // Привязываем событие Click для кнопки SaveButton
            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton != null)
            {
                saveButton.Click += Save_Click;
            }

            // Привязываем событие Click для кнопки BackButton
            var backButton = this.FindControl<Button>("BackButton");
            if (backButton != null)
            {
                backButton.Click += Back_Click;
            }

            // Привязываем события для TextBox
            BindTextBoxEvents();
            // Привязываем события для ComboBox
            BindComboBoxEvents();
        }

        private void BindTextBoxEvents()
        {
            var textBoxes = new[] { "MedicalSeriesTextBox", "MedicalNumberTextBox", "FullNameTextBox", "DateOfBirthTextBox",
            "PassportSeriesTextBox", "PassportNumberTextBox", "PassportIssuedByTextBox", "PhoneTextBox", "AddressTextBox",
            "DrivingExperienceTextBox", "SnilsTextBox", "FluorographyTextBox", "GynecologistTextBox" };

            foreach (var name in textBoxes)
            {
                var textBox = this.FindControl<TextBox>(name);
                if (textBox != null)
                {
                    textBox.TextChanged += HandleTextChanged;
                    textBox.TextInput += RestrictInput;
                    textBox.KeyDown += RestrictKeyDown;
                }
            }
        }

        private void BindComboBoxEvents()
        {
            var comboBoxes = new[] { "GenderComboBox", "BloodGroupComboBox", "RhFactorComboBox" };
            foreach (var name in comboBoxes)
            {
                var comboBox = this.FindControl<ComboBox>(name);
                if (comboBox != null)
                {
                    comboBox.GotFocus += ComboBox_GotFocus;
                    comboBox.SelectionChanged += ComboBox_SelectionChanged;
                    comboBox.KeyDown += InputField_KeyDown;
                }
            }
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            string text = textBox.Text ?? "";
            if (string.IsNullOrEmpty(text)) return;

            string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
            var (filteredText, caretIndex) = name switch
            {
                "FullNameTextBox" or "PassportIssuedByTextBox" or "FluorographyTextBox" or "GynecologistTextBox" =>
                    (text.Length > 1000 ? text.Substring(0, 1000) : text, Math.Min(text.Length, 1000)),
                "PassportSeriesTextBox" or "PassportNumberTextBox" =>
                    FilterNumericInput(text, GetMaxLength(name), textBox),
                "DateOfBirthTextBox" =>
                    FormatAndValidateDate(text, textBox),
                "SnilsTextBox" =>
                    FormatAndValidateSnils(text, textBox),
                "AddressTextBox" =>
                    (text.Length > 1000 ? text.Substring(0, 1000) : text, Math.Min(text.Length, 1000)),
                "PhoneTextBox" =>
                    FormatAndValidatePhone(text, textBox),
                "DrivingExperienceTextBox" =>
                    FilterNumericInput(text, 2, textBox),
                "MedicalSeriesTextBox" or "MedicalNumberTextBox" =>
                    (text.Length > 50 ? text.Substring(0, 50) : text, Math.Min(text.Length, 50)),
                _ => (text, text.Length)
            };

            if (ViewModel != null)
            {
                if (text != filteredText)
                {
                    textBox.Text = filteredText;
                    textBox.CaretIndex = caretIndex;
                }

                ViewModel.GetType().GetMethod($"Validate{name.Replace("TextBox", "")}")?.Invoke(ViewModel, null);

                int maxLength = GetMaxLengthForField(name);
                if (filteredText.Length == maxLength)
                {
                    MoveFocusByTabIndex(textBox, true);
                }
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
                "FullNameTextBox" or "PassportIssuedByTextBox" or "FluorographyTextBox" or "GynecologistTextBox" =>
                    newText.Length <= 1000,
                "PassportSeriesTextBox" or "PassportNumberTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Length <= GetMaxLength(name),
                "DateOfBirthTextBox" =>
                    ValidateDateInput(currentText, e.Text),
                "SnilsTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Replace("-", "").Replace(" ", "").Length <= 11,
                "AddressTextBox" =>
                    newText.Length <= 1000,
                "PhoneTextBox" =>
                    ValidatePhoneInput(currentText, e.Text),
                "DrivingExperienceTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Length <= 2,
                "MedicalSeriesTextBox" or "MedicalNumberTextBox" =>
                    newText.Length <= 50,
                _ => true
            };

            e.Handled = !isValid;
        }

        private void RestrictKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (e.Key == Key.Enter || e.Key == Key.Up || e.Key == Key.Down)
            {
                if (e.Key == Key.Enter)
                {
                    string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
                    if (name == "GynecologistTextBox")
                    {
                        var saveButton = this.FindControl<Button>("SaveButton");
                        if (saveButton != null)
                        {
                            saveButton.Focus();
                            if (ViewModel != null)
                            {
                                ViewModel.OnSave();
                            }
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        MoveFocusByTabIndex(textBox, true);
                        e.Handled = true;
                    }
                }
                else
                {
                    InputField_KeyDown(sender, e);
                }
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
                        "FullNameTextBox" or "PassportIssuedByTextBox" or "FluorographyTextBox" or "GynecologistTextBox" =>
                            clipboardText.Length > 1000 ? clipboardText.Substring(0, 1000) : clipboardText,
                        "PassportSeriesTextBox" or "PassportNumberTextBox" =>
                            new string(clipboardText.Where(char.IsDigit).Take(GetMaxLength(name)).ToArray()),
                        "DateOfBirthTextBox" =>
                            FormatAndValidateDate(new string(clipboardText.Where(c => char.IsDigit(c) || c == '.').ToArray()), textBox).filteredText,
                        "SnilsTextBox" =>
                            FormatAndValidateSnils(new string(clipboardText.Where(c => char.IsDigit(c) || c == '-' || c == ' ').ToArray()), textBox).filteredText,
                        "AddressTextBox" =>
                            clipboardText.Length > 1000 ? clipboardText.Substring(0, 1000) : clipboardText,
                        "PhoneTextBox" =>
                            FormatAndValidatePhone(new string(clipboardText.Where(c => char.IsDigit(c) || c == '+' || c == ' ' || c == '(' || c == ')' || c == '-').ToArray()), textBox).filteredText,
                        "DrivingExperienceTextBox" =>
                            new string(clipboardText.Where(char.IsDigit).Take(2).ToArray()),
                        "MedicalSeriesTextBox" or "MedicalNumberTextBox" =>
                            clipboardText.Length > 50 ? clipboardText.Substring(0, 50) : clipboardText,
                        _ => clipboardText
                    };

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        textBox.Text = filteredText;
                        textBox.CaretIndex = filteredText.Length;
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
            _ => throw new ArgumentException($"Unknown TextBox name: {textBoxName}")
        };

        private int GetMaxLengthForField(string textBoxName) => textBoxName switch
        {
            "FullNameTextBox" => 1000,
            "PassportIssuedByTextBox" => 1000,
            "FluorographyTextBox" => 1000,
            "GynecologistTextBox" => 1000,
            "PassportSeriesTextBox" => 4,
            "PassportNumberTextBox" => 6,
            "DateOfBirthTextBox" => 10,
            "SnilsTextBox" => 14,
            "AddressTextBox" => 1000,
            "PhoneTextBox" => 18,
            "DrivingExperienceTextBox" => 2,
            "MedicalSeriesTextBox" => 50,
            "MedicalNumberTextBox" => 50,
            _ => throw new ArgumentException($"Unknown TextBox name: {textBoxName}")
        };

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OnSave();

            if (string.IsNullOrEmpty(ViewModel.MedicalSeriesError) &&
                string.IsNullOrEmpty(ViewModel.MedicalNumberError) &&
                string.IsNullOrEmpty(ViewModel.FullNameError) &&
                string.IsNullOrEmpty(ViewModel.DateOfBirthError) &&
                string.IsNullOrEmpty(ViewModel.GenderError) &&
                string.IsNullOrEmpty(ViewModel.PassportSeriesError) &&
                string.IsNullOrEmpty(ViewModel.PassportNumberError) &&
                string.IsNullOrEmpty(ViewModel.PassportIssuedByError) &&
                string.IsNullOrEmpty(ViewModel.BloodGroupError) &&
                string.IsNullOrEmpty(ViewModel.RhFactorError) &&
                string.IsNullOrEmpty(ViewModel.PhoneError) &&
                string.IsNullOrEmpty(ViewModel.AddressError) &&
                string.IsNullOrEmpty(ViewModel.DrivingExperienceError) &&
                string.IsNullOrEmpty(ViewModel.SnilsError) &&
                string.IsNullOrEmpty(ViewModel.FluorographyError) &&
                string.IsNullOrEmpty(ViewModel.GynecologistError))
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Сохранить PDF-документ",
                    Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "PDF Files", Extensions = { "pdf" } }
                },
                    DefaultExtension = "pdf",
                    InitialFileName = $"{SanitizeFileName(ViewModel.FullName ?? "NewForm")}.pdf"
                };

                var result = await saveFileDialog.ShowAsync(this);
                if (!string.IsNullOrEmpty(result))
                {
                    var pdfGenerator = new NewFormPdfGenerator();
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "карта водительская комиссия.pdf");
                    pdfGenerator.GeneratePdf(new Dictionary<string, string>
                {
                    { "MedicalSeries", ViewModel.MedicalSeries ?? "" },
                    { "MedicalNumber", ViewModel.MedicalNumber ?? "" },
                    { "FullName", ViewModel.FullName ?? "" },
                    { "DateOfBirth", ViewModel.DateOfBirth ?? "" },
                    { "Gender", ViewModel.Gender ?? "" },
                    { "PassportSeries", ViewModel.PassportSeries ?? "" },
                    { "PassportNumber", ViewModel.PassportNumber ?? "" },
                    { "PassportIssuedBy", ViewModel.PassportIssuedBy ?? "" },
                    { "BloodGroup", ViewModel.BloodGroup ?? "" },
                    { "RhFactor", ViewModel.RhFactor ?? "" },
                    { "Phone", ViewModel.Phone ?? "" },
                    { "Address", ViewModel.Address ?? "" },
                    { "DrivingExperience", ViewModel.DrivingExperience ?? "" },
                    { "Snils", ViewModel.Snils ?? "" },
                    { "Fluorography", ViewModel.Fluorography ?? "" },
                    { "Gynecologist", ViewModel.Gynecologist ?? "" }
                }, result, templatePath);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result,
                        UseShellExecute = true
                    });

                    // Находим MenuWindow и показываем его
                    if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var menuWindow = desktop.MainWindow as MenuWindow;
                        if (menuWindow != null)
                        {
                            menuWindow.Show();
                        }
                    }

                    Close();
                }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Находим MenuWindow и показываем его
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var menuWindow = desktop.MainWindow as MenuWindow;
                if (menuWindow != null)
                {
                    menuWindow.Show();
                }
            }

            Close();
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName.Trim();
        }

        private void ComboBox_GotFocus(object sender, GotFocusEventArgs e)
        {
            if (sender is ComboBox comboBox)
                comboBox.IsDropDownOpen = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Control control)
                MoveFocusByTabIndex(control, true);
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not Control control) return;

            if (control is ComboBox comboBox && comboBox.IsDropDownOpen)
            {
                if (e.Key == Key.Enter || e.Key == Key.Up || e.Key == Key.Down)
                {
                    comboBox.IsDropDownOpen = false;
                }
                else
                {
                    return;
                }
            }

            if (e.Key == Key.Enter)
            {
                if (control.Name == "GynecologistTextBox")
                {
                    var saveButton = this.FindControl<Button>("SaveButton");
                    if (saveButton != null)
                    {
                        saveButton.Focus();
                        if (ViewModel != null)
                        {
                            ViewModel.OnSave();
                        }
                        e.Handled = true;
                    }
                }
                else
                {
                    MoveFocusByTabIndex(control, true);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                MoveFocusByTabIndex(control, e.Key == Key.Down);
                e.Handled = true;
            }
        }

        private void MoveFocusByTabIndex(Control currentControl, bool moveNext)
        {
            var stackPanel = this.FindControl<StackPanel>("MainStackPanel");
            if (stackPanel == null) return;

            var focusableControls = new List<(Control Control, int TabIndex)>();
            CollectFocusableControls(stackPanel, focusableControls);

            focusableControls.Sort((a, b) => a.TabIndex.CompareTo(b.TabIndex));

            var current = focusableControls.FirstOrDefault(x => x.Control == currentControl);
            if (current.Control == null) return;

            int currentTabIndex = current.TabIndex;
            Control nextControl = null;

            if (moveNext)
            {
                var next = focusableControls.FirstOrDefault(x => x.TabIndex > currentTabIndex);
                nextControl = next.Control;
            }
            else
            {
                var previous = focusableControls.LastOrDefault(x => x.TabIndex < currentTabIndex);
                nextControl = previous.Control;
            }

            if (nextControl != null)
            {
                nextControl.Focus();
                if (nextControl is TextBox textBox)
                {
                    textBox.SelectionStart = 0;
                    textBox.SelectionEnd = 0;
                }
            }
        }

        private void CollectFocusableControls(Control parent, List<(Control, int)> focusableControls)
        {
            foreach (var child in parent.GetVisualChildren().OfType<Control>())
            {
                if (child is TextBox || child is ComboBox || child is Button)
                {
                    int tabIndex = child.GetValue(Control.TabIndexProperty);
                    if (tabIndex >= 0)
                    {
                        focusableControls.Add((child, tabIndex));
                    }
                }
                else if (child is Panel panel)
                {
                    CollectFocusableControls(panel, focusableControls);
                }
            }
        }
    }

}
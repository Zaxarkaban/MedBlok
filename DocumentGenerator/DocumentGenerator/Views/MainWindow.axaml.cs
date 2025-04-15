using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DocumentGenerator.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using System;

namespace DocumentGenerator
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
        private const int CurrentYear = 2025;
        private const int MinYear = CurrentYear - 120; // 1905

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            string text = textBox.Text ?? "";
            if (string.IsNullOrEmpty(text)) return;

            string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
            var (filteredText, caretIndex) = name switch
            {
                "FullNameTextBox" or "PositionTextBox" or "PassportIssuedByTextBox" =>
                    (text.Length > 1000 ? text.Substring(0, 1000) : text, Math.Min(text.Length, 1000)),
                "PassportSeriesTextBox" or "PassportNumberTextBox" or "MedicalPolicyTextBox" =>
                    FilterNumericInput(text, GetMaxLength(name), textBox),
                "DateOfBirthTextBox" or "PassportIssueDateTextBox" =>
                    FormatAndValidateDate(text, textBox),
                "SnilsTextBox" =>
                    FormatAndValidateSnils(text, textBox),
                _ => (text, text.Length)
            };

            if (text != filteredText)
            {
                textBox.Text = filteredText;
                textBox.CaretIndex = caretIndex;
            }

            // Вызываем валидацию
            ViewModel.GetType().GetMethod($"Validate{name.Replace("TextBox", "")}")?.Invoke(ViewModel, null);

            // Проверяем, достиг ли текст максимальной длины, и переходим к следующему полю
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
                "FullNameTextBox" or "PositionTextBox" or "PassportIssuedByTextBox" =>
                    newText.Length <= 1000,
                "PassportSeriesTextBox" or "PassportNumberTextBox" or "MedicalPolicyTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Length <= GetMaxLength(name),
                "DateOfBirthTextBox" or "PassportIssueDateTextBox" =>
                    ValidateDateInput(currentText, e.Text),
                "SnilsTextBox" =>
                    e.Text.All(char.IsDigit) && newText.Replace("-", "").Replace(" ", "").Length <= 11,
                _ => true
            };

            if (!isValid) e.Handled = true;
        }

        private void RestrictKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // Обрабатываем навигацию
            if (e.Key is Key.Enter or Key.Up or Key.Down)
            {
                InputField_KeyDown(sender, e);
                return;
            }

            // Перехватываем вставку текста
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
                        _ => clipboardText
                    };

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        textBox.Text = filteredText;
                        textBox.CaretIndex = filteredText.Length;

                        // Проверяем, достиг ли текст максимальной длины после вставки
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

            // Пошаговая валидация каждой цифры
            string validatedDigits = "";
            for (int i = 0; i < digits.Length; i++)
            {
                string currentDigits = validatedDigits + digits[i];
                int pos = currentDigits.Length;

                // Проверяем день
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
                // Проверяем месяц
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
                // Проверяем год
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

            // Форматируем только после успешной валидации
            string finalFormatted = FormatDateInput(validatedDigits);
            return (finalFormatted, finalFormatted.Length);
        }

        private bool ValidateDateInput(string currentText, string input)
        {
            if (!input.All(char.IsDigit)) return false;

            string digits = currentText.Replace(".", "") + input;
            int pos = digits.Length;

            if (pos <= 2) // Проверка дня
            {
                if (pos == 1) // Первая цифра дня
                {
                    int dayFirstDigit = int.Parse(input);
                    if (dayFirstDigit > 3) return false;
                }
                else if (pos == 2) // Вторая цифра дня
                {
                    int day = int.Parse(digits[..2]);
                    if (day > 31 || day == 0) return false;
                }
            }
            else if (pos <= 4) // Проверка месяца
            {
                if (pos == 3) // Первая цифра месяца
                {
                    int monthFirstDigit = int.Parse(input);
                    if (monthFirstDigit > 1) return false;
                }
                else if (pos == 4) // Вторая цифра месяца
                {
                    int month = int.Parse(digits.Substring(2, 2));
                    if (month > 12 || month == 0) return false;
                }
            }
            else if (pos <= 8) // Проверка года
            {
                if (pos == 5) // Первая цифра года
                {
                    int yearFirstDigit = int.Parse(input);
                    if (yearFirstDigit != 1 && yearFirstDigit != 2) return false;
                }
                else if (pos == 6) // Вторая цифра года
                {
                    int yearFirstTwo = int.Parse(digits.Substring(4, 2));
                    if (yearFirstTwo != 19 && yearFirstTwo != 20) return false;
                }
                else if (pos == 7) // Третья цифра года
                {
                    int yearFirstTwo = int.Parse(digits.Substring(4, 2));
                    int yearThirdDigit = int.Parse(input);
                    if (yearFirstTwo == 20 && yearThirdDigit > 2) return false;
                }
                else if (pos == 8) // Четвёртая цифра года
                {
                    int year = int.Parse(digits.Substring(4, 4));
                    if (year < MinYear || year > CurrentYear) return false;
                }
            }

            return pos <= 8; // Максимум 8 цифр (ДД.ММ.ГГГГ)
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
            "PassportSeriesTextBox" => 4,
            "PassportNumberTextBox" => 6,
            "MedicalPolicyTextBox" => 16,
            "DateOfBirthTextBox" => 10, // ДД.ММ.ГГГГ
            "PassportIssueDateTextBox" => 10, // ДД.ММ.ГГГГ
            "SnilsTextBox" => 14, // XXX-XXX-XXX XX
            _ => throw new ArgumentException($"Unknown TextBox name: {textBoxName}")
        };

        private void Save_Click(object sender, RoutedEventArgs e) => ViewModel.OnSave();

        private void SaveButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.OnSave();
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
                if (control.Name == "MedicalPolicyTextBox")
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
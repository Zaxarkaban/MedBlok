using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DocumentGenerator.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.VisualTree;
using DynamicData;
using Avalonia.Layout;
using Avalonia.Media;
using DocumentGenerator.Services;
using Avalonia;
using Avalonia.Platform.Storage;

namespace DocumentGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel;
        private const int CurrentYear = 2025;
        private const int MinYear = CurrentYear - 120; // 1905
        private readonly IServiceProvider _serviceProvider;
        private bool _isPreviewing; // Флаг для предотвращения двойного открытия

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Для EPPlus
            _isPreviewing = false; // Инициализация флага
            if (ViewModel != null)
                ViewModel.ScrollToItemRequested += ScrollToItem;
        }

        public MainWindow(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
            AppWindowSetup.Configure(this, serviceProvider, expandToWorkAreaHeight: true);
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuWindow = _serviceProvider.GetRequiredService<MenuWindow>();
            menuWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            menuWindow.Show();
            Close();
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            string text = textBox.Text ?? "";
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
                "AddressTextBox" or "MedicalOrganizationTextBox" or "WorkplaceTextBox" or "WorkAddressTextBox" or "DepartmentTextBox" =>
                    (text.Length > 1000 ? text.Substring(0, 1000) : text, Math.Min(text.Length, 1000)),
                "PhoneTextBox" =>
                    FormatAndValidatePhone(text, textBox),
                "OkvedTextBox" =>
                    FormatAndValidateOkved(text, textBox),
                "WorkExperienceYearsTextBox" =>
                    FilterNumericInput(text, 2, textBox),
                _ => (text, text.Length)
            };

            if (ViewModel != null)
            {
                if (name == "WorkExperienceYearsTextBox")
                {
                    ViewModel.ValidateWorkExperience();
                }

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

            if (name == "WorkExperienceYearsTextBox" && filteredText.Length == 2)
            {
                var previewButton = this.FindControl<Button>("PreviewButton");
                previewButton?.Focus();
            }
        }

        private void ScrollToItem(object? sender, int index)
        {
            var listBox = this.FindControl<ListBox>("OrderClausesListBox");
            if (listBox != null && index >= 0 && index < listBox.ItemCount)
            {
                listBox.ScrollIntoView(index);
            }
        }

        private bool ValidateWorkExperienceInput(string currentText, string input)
        {
            if (!input.All(char.IsDigit)) return false;

            string digits = currentText + input;
            if (digits.Length > 2) return false;

            return !(int.TryParse(digits, out int years) && years > 80);
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
                "AddressTextBox" or "WorkplaceTextBox" or "WorkAddressTextBox" or "DepartmentTextBox" =>
                    newText.Length <= 1000,
                "PhoneTextBox" =>
                    ValidatePhoneInput(currentText, e.Text),
                "OkvedTextBox" =>
                    ValidateOkvedInput(currentText, e.Text),
                "WorkExperienceYearsTextBox" =>
                    ValidateWorkExperienceInput(currentText, e.Text),
                "OrderClausesSearchBox" => true,
                _ => true
            };

            e.Handled = !isValid;
        }

        private void RestrictKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (e.Key is Key.Enter or Key.Up or Key.Down)
            {
                if (e.Key == Key.Enter)
                {
                    string name = textBox.Name ?? throw new InvalidOperationException("TextBox must have a Name");
                    if (name == "WorkExperienceYearsTextBox")
                    {
                        var previewButton = this.FindControl<Button>("PreviewButton");
                        if (previewButton != null)
                        {
                            previewButton.Focus();
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
                        "FullNameTextBox" or "PositionTextBox" or "PassportIssuedByTextBox" =>
                            clipboardText.Length > 1000 ? clipboardText.Substring(0, 1000) : clipboardText,
                        "PassportSeriesTextBox" or "PassportNumberTextBox" or "MedicalPolicyTextBox" =>
                            new string(clipboardText.Where(char.IsDigit).Take(GetMaxLength(name)).ToArray()),
                        "DateOfBirthTextBox" or "PassportIssueDateTextBox" =>
                            FormatAndValidateDate(new string(clipboardText.Where(c => char.IsDigit(c) || c == '.').ToArray()), textBox).filteredText,
                        "SnilsTextBox" =>
                            FormatAndValidateSnils(new string(clipboardText.Where(c => char.IsDigit(c) || c == '-' || c == ' ').ToArray()), textBox).filteredText,
                        "AddressTextBox" or "WorkplaceTextBox" or "WorkAddressTextBox" or "DepartmentTextBox" =>
                            clipboardText.Length > 1000 ? clipboardText.Substring(0, 1000) : clipboardText,
                        "PhoneTextBox" =>
                            FormatAndValidatePhone(new string(clipboardText.Where(c => char.IsDigit(c) || c == '+' || c == ' ' || c == '(' || c == ')' || c == '-').ToArray()), textBox).filteredText,
                        "OkvedTextBox" =>
                            FormatAndValidateOkved(new string(clipboardText.Where(c => char.IsDigit(c) || c == '.').ToArray()), textBox).filteredText,
                        "WorkExperienceYearsTextBox" =>
                            new string(clipboardText.Where(char.IsDigit).Take(2).ToArray()),
                        "OrderClausesSearchBox" => clipboardText,
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

            int years = 0;
            if (int.TryParse(digits, out years))
            {
                if (years > 80)
                {
                    years = 80;
                    digits = "80";
                }
                else if (years < 0)
                {
                    years = 0;
                    digits = "0";
                }
            }

            string formatted = digits + " " + PdfFormFieldHelper.GetYearsWord(years);
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
            "WorkplaceTextBox" => 1000,
            "OkvedTextBox" => 8,
            "WorkExperienceYearsTextBox" => 2,
            "WorkAddressTextBox" => 1000,
            "DepartmentTextBox" => 1000,
            _ => throw new ArgumentException($"Unknown TextBox name: {textBoxName}")
        };

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            // Отладочный вывод для проверки количества вызовов
            Console.WriteLine($"Preview_Click called at {DateTime.Now:HH:mm:ss.fff}");

            // Предотвращаем множественные вызовы
            if (_isPreviewing)
            {
                Console.WriteLine("Preview_Click already in progress, skipping...");
                return;
            }
            _isPreviewing = true;

            try
            {
                string? validationMessage = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    validationMessage = ViewModel.GetValidationSummary();
                });

                if (string.IsNullOrEmpty(validationMessage))
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), $"Preview_{Guid.NewGuid()}.pdf");
                    try
                    {
                        var pdfGenerator = new PdfGenerator(ViewModel);
                        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.pdf");

                        pdfGenerator.GeneratePdf(tempPath, templatePath);

                        if (File.Exists(tempPath))
                        {
                            Console.WriteLine($"Opening PDF: {tempPath}");
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = tempPath,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            await AppDialog.ShowAsync(this, "Не удалось создать файл для предпросмотра.", "Ошибка", AppDialogKind.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        await AppDialog.ShowAsync(this, $"Ошибка при открытии предпросмотра: {ex.Message}", "Ошибка", AppDialogKind.Error);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                            {
                                await Task.Delay(3000); // Увеличиваем задержку до 3 секунд
                                File.Delete(tempPath);
                                Console.WriteLine($"Temporary file deleted: {tempPath}");
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"Failed to delete temporary file: {tempPath}");
                        }
                    }
                }
                else
                {
                    await AppDialog.ShowAsync(this, validationMessage!, "Заполните обязательные поля", AppDialogKind.Warning);
                }
            }
            finally
            {
                _isPreviewing = false;
                Console.WriteLine("Preview_Click finished.");
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string? validationMessage = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                validationMessage = ViewModel.GetValidationSummary();
            });

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
                    InitialFileName = $"{SanitizeFileName(ViewModel.FullName ?? "Document")}.pdf"
                };

                var result = await saveFileDialog.ShowAsync(this);
                if (!string.IsNullOrEmpty(result))
                {
                    try
                    {
                        var pdfGenerator = new PdfGenerator(ViewModel);
                        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.pdf");
                        pdfGenerator.GeneratePdf(result, templatePath);
                        await AppDialog.ShowAsync(this, "Файл успешно сохранён!", "Успех", AppDialogKind.Success);
                    }
                    catch (Exception ex)
                    {
                        await AppDialog.ShowAsync(this, $"Ошибка при сохранении файла: {ex.Message}", "Ошибка", AppDialogKind.Error);
                    }
                }
        }

        private async void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var pathMemory = _serviceProvider.GetRequiredService<IExportPathMemory>();

            var openFileDialog = new OpenFileDialog
            {
                Title = "Выбрать Excel-файл",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx", "xls" } },
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                }
            };

            var lastExcel = pathMemory.LastExcelFilePath;
            if (!string.IsNullOrWhiteSpace(lastExcel))
            {
                var lastDir = Path.GetDirectoryName(lastExcel);
                if (!string.IsNullOrWhiteSpace(lastDir) && Directory.Exists(lastDir))
                    openFileDialog.Directory = lastDir;
                if (File.Exists(lastExcel))
                    openFileDialog.InitialFileName = Path.GetFileName(lastExcel);
            }

            var result = await openFileDialog.ShowAsync(this);
            if (result == null || result.Length == 0)
                return;

            var filePath = result[0];
            pathMemory.RememberExcelFile(filePath);
            var viewModel = new ExcelDataViewModel();

            try
            {
                await viewModel.LoadFromExcel(filePath);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка при чтении Excel:\n{ex.Message}", "Ошибка", AppDialogKind.Error);
                return;
            }

            if (viewModel.Records.Count == 0)
            {
                await AppDialog.ShowAsync(this, "В файле не найдено записей для обработки.", "Предупреждение", AppDialogKind.Warning);
                return;
            }

            var storageProvider = StorageProvider;
            IStorageFolder? suggestedFolder = null;
            var lastFolder = pathMemory.LastExportFolderPath;
            if (!string.IsNullOrWhiteSpace(lastFolder) && Directory.Exists(lastFolder))
                suggestedFolder = await storageProvider.TryGetFolderFromPathAsync(lastFolder);

            suggestedFolder ??= await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);

            var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выберите папку для сохранения PDF-файлов",
                SuggestedStartLocation = suggestedFolder
            });

            if (folder == null || folder.Count == 0)
                return;

            var folderPath = folder[0].Path.LocalPath;
            pathMemory.RememberExportFolder(folderPath);

            try
            {
                PdfFontHelper.Warmup();

                await AppExportProgress.RunAsync(this, viewModel.Records.Count, async progress =>
                {
                    await viewModel.ExportToFolderAsync(folderPath, progress);
                });

                await AppDialog.ShowAsync(
                    this,
                    $"Все PDF-файлы сохранены в папке:\n{folderPath}\nСтатистика врачей и исследований — в Statistics.xlsx",
                    "Успех",
                    AppDialogKind.Success);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка при сохранении PDF:\n{ex.Message}", "Ошибка", AppDialogKind.Error);
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
                if (control.Name == "WorkExperienceYearsTextBox")
                {
                    var previewButton = this.FindControl<Button>("PreviewButton");
                    if (previewButton != null)
                    {
                        previewButton.Focus();
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
                if (child is TextBox || child is ComboBox || child is ListBox || child is Button)
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
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia;
using DocumentGenerator.Models.UserPrograms;
using DocumentGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public partial class EditorWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserProgramStorage _storage;

        private readonly ObservableCollection<UserProgramFieldDefinition> _fields = new();
        private UserProgramDefinition _definition = new();
        private string? _templateSourcePath;
        private string? _editingFieldKey;
        private bool _isLoadingEditor;
        private Avalonia.Point? _dragStartPoint;
        private DragSource? _dragSource;
        private string? _dragFieldKey;
        private UserProgramFieldType? _dragFieldType;

        public EditorWindow()
        {
            InitializeComponent();
        }

        public EditorWindow(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
            AppWindowSetup.Configure(this, serviceProvider);
            _storage = _serviceProvider.GetRequiredService<IUserProgramStorage>();

            WireUi();
            InitializeNewProgram();
        }

        public EditorWindow(IServiceProvider serviceProvider, UserProgramInfo existingProgram) : this()
        {
            _serviceProvider = serviceProvider;
            _storage = _serviceProvider.GetRequiredService<IUserProgramStorage>();
            AppWindowSetup.Configure(this, serviceProvider);

            WireUi();
            LoadExistingProgram(existingProgram);
        }

        private void WireUi()
        {
            this.FindControl<Button>("BackToMenuButton")?.AddHandler(Button.ClickEvent, BackToMenu_Click);
            this.FindControl<Button>("PublishButton")?.AddHandler(Button.ClickEvent, Publish_Click);

            this.FindControl<Button>("PickTemplateButton")?.AddHandler(Button.ClickEvent, PickTemplate_Click);

            var palette = this.FindControl<ListBox>("PaletteListBox");
            if (palette != null)
            {
                palette.ItemsSource = new[]
                {
                    UserProgramFieldType.Text,
                    UserProgramFieldType.Number,
                    UserProgramFieldType.Date,
                    UserProgramFieldType.Combo,
                    UserProgramFieldType.Checkbox,
                    UserProgramFieldType.MultiSelect
                };

                palette.ItemTemplate = new FuncDataTemplate<UserProgramFieldType>((item, _) =>
                {
                    var border = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        Background = Avalonia.Media.Brush.Parse("#0F000000"),
                        BorderBrush = Avalonia.Media.Brush.Parse("#8899CCE0"),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(10, 8),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    border.Child = new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock { Text = item.ToString(), FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold },
                            new TextBlock { Text = "Перетащи в форму", Opacity = 0.8, FontSize = 12 }
                        }
                    };

                    return border;
                });

                palette.SelectedIndex = 0;
            }

            var fieldsList = this.FindControl<ListBox>("FieldsListBox");
            if (fieldsList != null)
            {
                fieldsList.ItemsSource = _fields;
                fieldsList.ItemTemplate = new FuncDataTemplate<UserProgramFieldDefinition>((item, _) =>
                {
                    var border = new Border
                    {
                        CornerRadius = new CornerRadius(12),
                        Background = Avalonia.Media.Brush.Parse("#0F000000"),
                        BorderBrush = Avalonia.Media.Brush.Parse("#88D4F0"),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(10, 8),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var title = new TextBlock { FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold };
                    var subtitle = new TextBlock { Opacity = 0.85, FontSize = 12 };

                    title.Bind(TextBlock.TextProperty, new Binding(nameof(UserProgramFieldDefinition.BlockName))
                    {
                        FallbackValue = "Без названия"
                    });

                    var mb = new MultiBinding { Converter = FieldSubtitleMultiConverter.Instance };
                    mb.Bindings.Add(new Binding(nameof(UserProgramFieldDefinition.Type)));
                    mb.Bindings.Add(new Binding(nameof(UserProgramFieldDefinition.PdfFieldName)));
                    subtitle.Bind(TextBlock.TextProperty, mb);

                    border.Child = new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            title,
                            subtitle
                        }
                    };

                    border.ContextMenu = BuildFieldContextMenu(item);
                    return border;
                });

                fieldsList.SelectionChanged += (_, _) =>
                {
                    SaveCurrentEditorToField();
                    LoadSelectedFieldToEditor();
                };

                fieldsList.AddHandler(DragDrop.DragOverEvent, FieldsList_DragOver);
                fieldsList.AddHandler(DragDrop.DropEvent, FieldsList_Drop);
                fieldsList.AddHandler(InputElement.PointerPressedEvent, FieldsList_PointerPressed, RoutingStrategies.Tunnel);
                fieldsList.AddHandler(InputElement.PointerMovedEvent, Any_PointerMoved, RoutingStrategies.Tunnel);
                fieldsList.AddHandler(InputElement.PointerReleasedEvent, Any_PointerReleased, RoutingStrategies.Tunnel);
            }

            var typeCombo = this.FindControl<ComboBox>("FieldTypeComboBox");
            if (typeCombo != null)
            {
                typeCombo.ItemsSource = Enum.GetValues<UserProgramFieldType>();
                typeCombo.SelectionChanged += (_, _) =>
                {
                    if (typeCombo.SelectedItem is UserProgramFieldType ft)
                        UpdatePropertyPanels(ft);
                    ApplyEditorToSelectedField();
                };
            }

            HookTextChange("ProgramNameTextBox", _ => ApplyProgramName());
            HookTextChange("FieldBlockNameTextBox", _ => ApplyEditorToSelectedField());
            HookTextChange("FieldLabelTextBox", _ => ApplyEditorToSelectedField());
            HookTextChange("PdfFieldNameTextBox", _ => ApplyEditorToSelectedField());
            HookTextChange("OptionsTextBox", _ => ApplyEditorToSelectedField());

            HookCheckedChange("RequiredCheckBox", _ => ApplyEditorToSelectedField());
            HookCheckedChange("MaxLengthCheckBox", _ => ApplyEditorToSelectedField());
            HookTextChange("MaxLengthTextBox", _ => ApplyEditorToSelectedField());
            HookCheckedChange("RegexCheckBox", _ => ApplyEditorToSelectedField());
            HookTextChange("RegexTextBox", _ => ApplyEditorToSelectedField());
            HookCheckedChange("RangeCheckBox", _ => ApplyEditorToSelectedField());
            HookTextChange("MinTextBox", _ => ApplyEditorToSelectedField());
            HookTextChange("MaxTextBox", _ => ApplyEditorToSelectedField());

            // Drag from palette into fields list
            var paletteList = this.FindControl<ListBox>("PaletteListBox");
            if (paletteList != null)
            {
                paletteList.AddHandler(InputElement.PointerPressedEvent, Palette_PointerPressed, RoutingStrategies.Tunnel);
                paletteList.AddHandler(InputElement.PointerMovedEvent, Any_PointerMoved, RoutingStrategies.Tunnel);
                paletteList.AddHandler(InputElement.PointerReleasedEvent, Any_PointerReleased, RoutingStrategies.Tunnel);
            }

            UpdatePropertyPanels(UserProgramFieldType.Text);
        }

        private void HookTextChange(string name, Action<TextBox> onChange)
        {
            var tb = this.FindControl<TextBox>(name);
            if (tb == null) return;
            tb.TextChanged += (_, _) => onChange(tb);
        }

        private void HookCheckedChange(string name, Action<CheckBox> onChange)
        {
            var cb = this.FindControl<CheckBox>(name);
            if (cb == null) return;
            cb.IsCheckedChanged += (_, _) => onChange(cb);
        }

        private void InitializeNewProgram()
        {
            _definition = new UserProgramDefinition();
            _fields.Clear();
            _templateSourcePath = null;

            var nameBox = this.FindControl<TextBox>("ProgramNameTextBox");
            if (nameBox != null) nameBox.Text = _definition.Name;

            var templateBox = this.FindControl<TextBox>("TemplatePathTextBox");
            if (templateBox != null) templateBox.Text = "";

            SetStatus($"Папка программ: {_storage.RootFolderPath}");
        }

        private void LoadExistingProgram(UserProgramInfo program)
        {
            _definition = _storage.LoadDefinition(program.ProgramJsonPath);
            _fields.Clear();
            foreach (var f in _definition.Fields)
                _fields.Add(f);

            _templateSourcePath = program.TemplatePdfPath;

            var nameBox = this.FindControl<TextBox>("ProgramNameTextBox");
            if (nameBox != null) nameBox.Text = _definition.Name;

            var templateBox = this.FindControl<TextBox>("TemplatePathTextBox");
            if (templateBox != null) templateBox.Text = program.TemplatePdfPath;

            SetStatus($"Режим редактирования. Папка: {program.FolderPath}");
        }

        private void ApplyProgramName()
        {
            var nameBox = this.FindControl<TextBox>("ProgramNameTextBox");
            if (nameBox == null) return;
            _definition.Name = (nameBox.Text ?? "").Trim();
        }

        private void BackToMenu_Click(object? sender, RoutedEventArgs e)
        {
            var menuWindow = _serviceProvider.GetRequiredService<MenuWindow>();
            menuWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            menuWindow.Show();
            Close();
        }

        private async void PickTemplate_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выбрать PDF-шаблон",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "PDF", Extensions = { "pdf" } }
                },
                AllowMultiple = false
            };

            var result = await dlg.ShowAsync(this);
            if (result == null || result.Length == 0) return;

            _templateSourcePath = result[0];
            var templateBox = this.FindControl<TextBox>("TemplatePathTextBox");
            if (templateBox != null) templateBox.Text = _templateSourcePath;

            SetStatus("Шаблон выбран. Добавляй поля и нажимай «Сохраить».");
        }

        private void AddField(UserProgramFieldType type, int? insertIndex)
        {
            SaveCurrentEditorToField();

            var field = new UserProgramFieldDefinition
            {
                Key = Guid.NewGuid().ToString("N"),
                Type = type,
                BlockName = $"Блок {_fields.Count + 1}",
                Label = "",
                PdfFieldName = ""
            };

            if (insertIndex is int idx && idx >= 0 && idx <= _fields.Count)
                _fields.Insert(idx, field);
            else
                _fields.Add(field);

            var fieldsList = this.FindControl<ListBox>("FieldsListBox");
            if (fieldsList != null)
            {
                fieldsList.SelectedItem = field;
                fieldsList.ScrollIntoView(field);
            }
        }

        private void LoadSelectedFieldToEditor()
        {
            var fieldsList = this.FindControl<ListBox>("FieldsListBox");
            if (fieldsList?.SelectedItem is not UserProgramFieldDefinition field)
                return;

            _editingFieldKey = field.Key;
            _isLoadingEditor = true;

            var typeCombo = this.FindControl<ComboBox>("FieldTypeComboBox");
            if (typeCombo != null) typeCombo.SelectedItem = field.Type;

            SetText("FieldBlockNameTextBox", field.BlockName);
            SetText("FieldLabelTextBox", field.Label);
            SetText("PdfFieldNameTextBox", field.PdfFieldName);

            var req = this.FindControl<CheckBox>("RequiredCheckBox");
            if (req != null) req.IsChecked = field.Required;

            SetText("OptionsTextBox", field.Options == null ? "" : string.Join(Environment.NewLine, field.Options));

            var maxLenRule = field.Validation.FirstOrDefault(v => v.Type == UserProgramValidationType.MaxLength);
            SetCheck("MaxLengthCheckBox", maxLenRule != null);
            SetText("MaxLengthTextBox", maxLenRule?.MaxLength?.ToString() ?? "");

            var regexRule = field.Validation.FirstOrDefault(v => v.Type == UserProgramValidationType.Regex);
            SetCheck("RegexCheckBox", regexRule != null);
            SetText("RegexTextBox", regexRule?.RegexPattern ?? "");

            var rangeRule = field.Validation.FirstOrDefault(v => v.Type == UserProgramValidationType.Range);
            SetCheck("RangeCheckBox", rangeRule != null);
            SetText("MinTextBox", rangeRule?.Min?.ToString() ?? "");
            SetText("MaxTextBox", rangeRule?.Max?.ToString() ?? "");

            UpdatePropertyPanels(field.Type);
            _isLoadingEditor = false;
        }

        private void ApplyEditorToSelectedField()
        {
            if (_isLoadingEditor) return;
            var fieldsList = this.FindControl<ListBox>("FieldsListBox");
            if (fieldsList?.SelectedItem is not UserProgramFieldDefinition field)
                return;

            ApplyEditorToField(field);

            // refresh listbox text (template is now null-safe; redraw is enough)
            fieldsList.InvalidateMeasure();
            fieldsList.InvalidateVisual();
        }

        private void SaveCurrentEditorToField()
        {
            if (string.IsNullOrWhiteSpace(_editingFieldKey))
                return;

            var field = _fields.FirstOrDefault(f => f.Key == _editingFieldKey);
            if (field == null)
                return;

            ApplyEditorToField(field);
        }

        private void ApplyEditorToField(UserProgramFieldDefinition field)
        {
            if (this.FindControl<ComboBox>("FieldTypeComboBox")?.SelectedItem is UserProgramFieldType ft)
                field.Type = ft;

            field.BlockName = (GetText("FieldBlockNameTextBox") ?? "").Trim();
            field.Label = (GetText("FieldLabelTextBox") ?? "").Trim();
            field.PdfFieldName = (GetText("PdfFieldNameTextBox") ?? "").Trim();
            field.Required = this.FindControl<CheckBox>("RequiredCheckBox")?.IsChecked == true;

            var optionsText = GetText("OptionsTextBox") ?? "";
            var options = optionsText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            field.Options = options.Count == 0 ? null : options;

            var validation = new List<UserProgramValidationRule>();
            if (field.Required)
                validation.Add(new UserProgramValidationRule { Type = UserProgramValidationType.Required });

            if (this.FindControl<CheckBox>("MaxLengthCheckBox")?.IsChecked == true)
            {
                if (int.TryParse(GetText("MaxLengthTextBox"), out var maxLen) && maxLen > 0)
                {
                    validation.Add(new UserProgramValidationRule
                    {
                        Type = UserProgramValidationType.MaxLength,
                        MaxLength = maxLen
                    });
                }
            }

            if (this.FindControl<CheckBox>("RegexCheckBox")?.IsChecked == true)
            {
                var pattern = (GetText("RegexTextBox") ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    validation.Add(new UserProgramValidationRule
                    {
                        Type = UserProgramValidationType.Regex,
                        RegexPattern = pattern
                    });
                }
            }

            if (this.FindControl<CheckBox>("RangeCheckBox")?.IsChecked == true)
            {
                double? min = double.TryParse(GetText("MinTextBox"), out var minV) ? minV : null;
                double? max = double.TryParse(GetText("MaxTextBox"), out var maxV) ? maxV : null;
                if (min != null || max != null)
                {
                    validation.Add(new UserProgramValidationRule
                    {
                        Type = UserProgramValidationType.Range,
                        Min = min,
                        Max = max
                    });
                }
            }

            field.Validation = validation;
        }

        private void UpdatePropertyPanels(UserProgramFieldType type)
        {
            var optionsPanel = this.FindControl<StackPanel>("OptionsPanel");
            var validationPanel = this.FindControl<StackPanel>("ValidationPanel");
            var maxLenPanel = this.FindControl<StackPanel>("MaxLengthPanel");
            var regexPanel = this.FindControl<StackPanel>("RegexPanel");
            var rangePanel = this.FindControl<StackPanel>("RangePanel");

            bool showOptions = type is UserProgramFieldType.Combo or UserProgramFieldType.MultiSelect;
            bool showMaxLen = type is UserProgramFieldType.Text;
            bool showRegex = type is UserProgramFieldType.Text or UserProgramFieldType.Date;
            bool showRange = type is UserProgramFieldType.Number;

            if (optionsPanel != null) optionsPanel.IsVisible = showOptions;
            if (maxLenPanel != null) maxLenPanel.IsVisible = showMaxLen;
            if (regexPanel != null) regexPanel.IsVisible = showRegex;
            if (rangePanel != null) rangePanel.IsVisible = showRange;

            if (validationPanel != null) validationPanel.IsVisible = showMaxLen || showRegex || showRange;
        }

        private async void Publish_Click(object? sender, RoutedEventArgs e)
        {
            ApplyProgramName();
            SaveCurrentEditorToField();

            if (string.IsNullOrWhiteSpace(_definition.Name))
            {
                SetStatus("Название программы не задано.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_templateSourcePath) || !File.Exists(_templateSourcePath))
            {
                SetStatus("Выбери PDF-шаблон.");
                return;
            }

            var errors = ValidateDefinitionForPublish();
            if (errors.Count > 0)
            {
                await AppDialog.ShowAsync(
                    this,
                    "Есть недоделанные блоки. Заполни их и попробуй снова.\n\n" + string.Join(Environment.NewLine, errors),
                    "Не получилось сохранить",
                    AppDialogKind.Warning);
                return;
            }

            _definition.Fields = _fields.ToList();

            var folder = _storage.EnsureProgramFolder(_definition.Id);
            var templateDestPath = Path.Combine(folder, _definition.TemplateFileName);
            try
            {
                CopyTemplateFileSafe(_templateSourcePath!, templateDestPath);
            }
            catch (IOException ex)
            {
                await AppDialog.ShowAsync(
                    this,
                    "Не удалось записать template.pdf: файл занят другой программой (часто это просмотрщик PDF). Закрой шаблон и нажми «Сохранить» снова.\n\n" + ex.Message,
                    "Файл занят",
                    AppDialogKind.Error);
                return;
            }

            _storage.SaveDefinition(folder, _definition);

            SetStatus($"Готово. Сохранено: {folder}");

            var listWindow = _serviceProvider.GetRequiredService<UserProgramsWindow>();
            listWindow.Show();
            Close();
        }

        private static void CopyTemplateFileSafe(string sourcePath, string destPath)
        {
            var fullSrc = Path.GetFullPath(sourcePath);
            var fullDst = Path.GetFullPath(destPath);
            if (string.Equals(fullSrc, fullDst, StringComparison.OrdinalIgnoreCase))
                return;

            var directory = Path.GetDirectoryName(fullDst);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Некорректный путь назначения.");

            Directory.CreateDirectory(directory);

            var tempPath = Path.Combine(directory, Path.GetFileName(fullDst) + ".swap." + Guid.NewGuid().ToString("N"));
            try
            {
                File.Copy(fullSrc, tempPath, overwrite: true);
                if (File.Exists(fullDst))
                    File.Delete(fullDst);
                File.Move(tempPath, fullDst, overwrite: true);
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // ignore cleanup failure
                }

                throw;
            }
        }

        private List<string> ValidateDefinitionForPublish()
        {
            var errors = new List<string>();

            if (_fields.Count == 0)
                errors.Add("Добавь хотя бы одно поле.");

            foreach (var f in _fields)
            {
                if (string.IsNullOrWhiteSpace(f.Label))
                    errors.Add("Есть поле без подписи.");

                if (string.IsNullOrWhiteSpace(f.PdfFieldName))
                    errors.Add($"Поле «{f.Label}» без имени поля в PDF.");

                if ((f.Type == UserProgramFieldType.Combo || f.Type == UserProgramFieldType.MultiSelect) &&
                    (f.Options == null || f.Options.Count == 0))
                    errors.Add($"Поле «{f.Label}» ({f.Type}) без опций.");

                foreach (var v in f.Validation)
                {
                    if (v.Type == UserProgramValidationType.Regex && !string.IsNullOrWhiteSpace(v.RegexPattern))
                    {
                        try { _ = new Regex(v.RegexPattern); }
                        catch { errors.Add($"Поле «{f.Label}»: некорректный Regex."); }
                    }
                }
            }

            return errors;
        }

        private void SetStatus(string text)
        {
            var tb = this.FindControl<TextBlock>("ConstructorStatusTextBlock");
            if (tb != null) tb.Text = text;
        }

        private string? GetText(string name) => this.FindControl<TextBox>(name)?.Text;
        private void SetText(string name, string text)
        {
            var tb = this.FindControl<TextBox>(name);
            if (tb != null) tb.Text = text;
        }

        private void SetCheck(string name, bool value)
        {
            var cb = this.FindControl<CheckBox>(name);
            if (cb != null) cb.IsChecked = value;
        }

        private void Palette_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ListBox palette) return;
            if (!e.GetCurrentPoint(palette).Properties.IsLeftButtonPressed) return;

            if (ResolvePaletteFieldType(palette, e) is not UserProgramFieldType fieldType)
                return;

            palette.SelectedItem = fieldType;
            _dragStartPoint = e.GetPosition(palette);
            _dragSource = DragSource.Palette;
            _dragFieldType = fieldType;
            _dragFieldKey = null;
        }

        private static UserProgramFieldType? ResolvePaletteFieldType(ListBox palette, PointerEventArgs e)
        {
            if (e.Source is Visual visual)
            {
                var item = visual.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
                if (item?.DataContext is UserProgramFieldType type)
                    return type;
            }

            return palette.SelectedItem as UserProgramFieldType?;
        }

        private void FieldsList_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed) return;

            // do not block normal selection; just arm possible drag
            if (listBox.SelectedItem is not UserProgramFieldDefinition field) return;
            _dragStartPoint = e.GetPosition(listBox);
            _dragSource = DragSource.Fields;
            _dragFieldKey = field.Key;
            _dragFieldType = null;
        }

        private ContextMenu BuildFieldContextMenu(UserProgramFieldDefinition? field)
        {
            var menu = new ContextMenu();
            if (field == null)
            {
                menu.ItemsSource = Array.Empty<MenuItem>();
                return menu;
            }

            var duplicate = new MenuItem { Header = "Дублировать" };
            duplicate.Click += (_, _) => DuplicateField(field);

            var remove = new MenuItem { Header = "Удалить" };
            remove.Click += (_, _) => RemoveField(field);

            menu.ItemsSource = new object[] { duplicate, remove };
            return menu;
        }

        private void DuplicateField(UserProgramFieldDefinition field)
        {
            SaveCurrentEditorToField();

            var copy = new UserProgramFieldDefinition
            {
                Key = Guid.NewGuid().ToString("N"),
                Type = field.Type,
                BlockName = field.BlockName,
                Label = field.Label,
                PdfFieldName = field.PdfFieldName,
                Required = field.Required,
                Options = field.Options == null ? null : new List<string>(field.Options),
                Validation = field.Validation.Select(v => new UserProgramValidationRule
                {
                    Type = v.Type,
                    Message = v.Message,
                    MaxLength = v.MaxLength,
                    RegexPattern = v.RegexPattern,
                    Min = v.Min,
                    Max = v.Max
                }).ToList()
            };

            var idx = _fields.IndexOf(field);
            _fields.Insert(Math.Max(0, idx + 1), copy);

            var list = this.FindControl<ListBox>("FieldsListBox");
            if (list != null) list.SelectedItem = copy;
        }

        private void RemoveField(UserProgramFieldDefinition field)
        {
            SaveCurrentEditorToField();
            var idx = _fields.IndexOf(field);
            _fields.Remove(field);

            var list = this.FindControl<ListBox>("FieldsListBox");
            if (list != null && _fields.Count > 0)
            {
                list.SelectedIndex = Math.Min(idx, _fields.Count - 1);
            }
        }

        private async void Any_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragStartPoint == null || _dragSource == null) return;
            if (sender is not Control control) return;

            var p = e.GetPosition(control);
            var delta = p - _dragStartPoint.Value;
            if (Math.Abs(delta.X) < 12 && Math.Abs(delta.Y) < 12) return; // threshold (x2)

            // start drag once
            var src = _dragSource;
            var key = _dragFieldKey;
            var type = _dragFieldType;
            _dragStartPoint = null;
            _dragSource = null;
            _dragFieldKey = null;
            _dragFieldType = null;

            var data = new DataObject();
            if (src == DragSource.Palette && type != null)
            {
                data.Set("DG/FieldType", type.Value.ToString());
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
            }
            else if (src == DragSource.Fields && !string.IsNullOrWhiteSpace(key))
            {
                data.Set("DG/MoveFieldKey", key);
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
        }

        private void Any_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _dragStartPoint = null;
            _dragSource = null;
            _dragFieldKey = null;
            _dragFieldType = null;
        }

        private void FieldsList_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("DG/FieldType") || e.Data.Contains("DG/MoveFieldKey"))
            {
                e.DragEffects = e.Data.Contains("DG/FieldType") ? DragDropEffects.Copy : DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void FieldsList_Drop(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            var point = e.GetPosition(listBox);
            int insertIndex = listBox.ItemCount;

            var hit = listBox.InputHitTest(point);
            if (hit is Control controlHit)
            {
                var container = controlHit.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
                if (container != null)
                {
                    var containerIndex = listBox.IndexFromContainer(container);
                    if (containerIndex >= 0)
                    {
                        insertIndex = containerIndex;
                        var inContainer = e.GetPosition(container);
                        if (inContainer.Y > container.Bounds.Height / 2)
                            insertIndex = containerIndex + 1;
                    }
                }
            }

            if (e.Data.Contains("DG/FieldType"))
            {
                var typeStr = e.Data.Get("DG/FieldType") as string;
                if (Enum.TryParse<UserProgramFieldType>(typeStr, out var type))
                {
                    AddField(type, insertIndex);
                }
                e.Handled = true;
                return;
            }

            if (e.Data.Contains("DG/MoveFieldKey"))
            {
                var key = e.Data.Get("DG/MoveFieldKey") as string;
                if (string.IsNullOrWhiteSpace(key)) return;

                var field = _fields.FirstOrDefault(f => f.Key == key);
                if (field == null) return;

                var oldIndex = _fields.IndexOf(field);
                if (oldIndex < 0) return;

                if (insertIndex > oldIndex) insertIndex--;
                insertIndex = Math.Clamp(insertIndex, 0, _fields.Count - 1);

                if (insertIndex != oldIndex)
                {
                    _fields.Move(oldIndex, insertIndex);
                }

                listBox.SelectedItem = field;
                e.Handled = true;
            }
        }
    }

    internal enum DragSource
    {
        Palette,
        Fields
    }

    internal sealed class FieldSubtitleMultiConverter : IMultiValueConverter
    {
        public static readonly FieldSubtitleMultiConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            var typeObj = values.Count > 0 ? values[0] : null;
            var pdfFieldName = values.Count > 1 ? values[1] as string : null;

            var typeText = typeObj?.ToString() ?? "";
            var pdfText = string.IsNullOrWhiteSpace(pdfFieldName) ? "PDF: ?" : $"PDF: {pdfFieldName!.Trim()}";
            return $"{typeText}  •  {pdfText}";
        }
    }
}

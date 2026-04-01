using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using DocumentGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public partial class UserProgramsWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserProgramStorage _storage;

        public UserProgramsWindow()
        {
            InitializeComponent();
        }

        public UserProgramsWindow(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
            _storage = _serviceProvider.GetRequiredService<IUserProgramStorage>();

            WireUi();
        }

        private void WireUi()
        {
            this.FindControl<Button>("BackToMenuButton")?.AddHandler(Button.ClickEvent, BackToMenu_Click);

            this.FindControl<Button>("CreateButton")?.AddHandler(Button.ClickEvent, Create_Click);
            this.FindControl<Button>("OpenButton")?.AddHandler(Button.ClickEvent, Open_Click);
            this.FindControl<Button>("EditButton")?.AddHandler(Button.ClickEvent, Edit_Click);
            this.FindControl<Button>("ExportButton")?.AddHandler(Button.ClickEvent, Export_Click);
            this.FindControl<Button>("ImportButton")?.AddHandler(Button.ClickEvent, Import_Click);
            this.FindControl<Button>("DeleteButton")?.AddHandler(Button.ClickEvent, Delete_Click);

            this.Opened += async (_, _) => await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            var listBox = this.FindControl<ListBox>("ProgramsListBox");
            var status = this.FindControl<TextBlock>("StatusTextBlock");
            if (listBox == null || status == null) return;

            await Task.Yield();
            var programs = _storage.ListPrograms();
            listBox.ItemsSource = programs;
            listBox.ItemTemplate = new FuncDataTemplate<UserProgramInfo>((item, _) =>
            {
                return new TextBlock
                {
                    Text = item.Name
                };
            });

            status.Text = programs.Count == 0
                ? $"Нет пользовательских программ. Папка: {_storage.RootFolderPath}"
                : $"Найдено программ: {programs.Count}. Папка: {_storage.RootFolderPath}";
        }

        private void BackToMenu_Click(object? sender, RoutedEventArgs e)
        {
            var menuWindow = _serviceProvider.GetRequiredService<MenuWindow>();
            menuWindow.Show();
            Close();
        }

        private void Create_Click(object? sender, RoutedEventArgs e)
        {
            var constructor = _serviceProvider.GetRequiredService<EditorWindow>();
            constructor.Show();
            Close();
        }

        private async void Open_Click(object? sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("ProgramsListBox");
            if (listBox?.SelectedItem is not UserProgramInfo info)
            {
                await SimpleMessageBox.Show(this, "Выбери программу из списка.", "Предупреждение");
                return;
            }

            var dynamicWindow = new DynamicProgramWindow(_serviceProvider, info);
            dynamicWindow.Show();
            Close();
        }

        private async void Edit_Click(object? sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("ProgramsListBox");
            if (listBox?.SelectedItem is not UserProgramInfo info)
            {
                await SimpleMessageBox.Show(this, "Выбери программу из списка.", "Предупреждение");
                return;
            }

            var constructor = new EditorWindow(_serviceProvider, info);
            constructor.Show();
            Close();
        }

        private async void Export_Click(object? sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("ProgramsListBox");
            if (listBox?.SelectedItem is not UserProgramInfo info)
            {
                await SimpleMessageBox.Show(this, "Выбери программу из списка.", "Предупреждение");
                return;
            }

            var save = new SaveFileDialog
            {
                Title = "Экспортировать программу",
                DefaultExtension = "zip",
                InitialFileName = $"{FileNameUtil.SanitizeFileName(info.Name)}.zip",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Zip", Extensions = { "zip" } }
                }
            };

            var path = await save.ShowAsync(this);
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                _storage.ExportToZip(info, path);
                await SimpleMessageBox.Show(this, "Экспорт завершён.", "Успех");
            }
            catch (Exception ex)
            {
                await SimpleMessageBox.Show(this, $"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }

        private async void Import_Click(object? sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog
            {
                Title = "Импортировать программу (.zip)",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Zip", Extensions = { "zip" } }
                }
            };

            var result = await open.ShowAsync(this);
            if (result == null || result.Length == 0) return;

            try
            {
                _storage.ImportFromZip(result[0]);
                await ReloadAsync();
                await SimpleMessageBox.Show(this, "Импорт завершён.", "Успех");
            }
            catch (Exception ex)
            {
                await SimpleMessageBox.Show(this, $"Ошибка импорта: {ex.Message}", "Ошибка");
            }
        }

        private async void Delete_Click(object? sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("ProgramsListBox");
            if (listBox?.SelectedItem is not UserProgramInfo info)
            {
                await SimpleMessageBox.Show(this, "Выбери программу из списка.", "Предупреждение");
                return;
            }

            try
            {
                _storage.DeleteProgram(info);
                await ReloadAsync();
                await SimpleMessageBox.Show(this, "Удалено.", "Успех");
            }
            catch (Exception ex)
            {
                await SimpleMessageBox.Show(this, $"Ошибка удаления: {ex.Message}", "Ошибка");
            }
        }

        private async void NotImplemented_Click(object? sender, RoutedEventArgs e)
        {
            await SimpleMessageBox.Show(this, "Функция будет добавлена следующим шагом внедрения конструктора.", "В разработке");
        }
    }

    internal static class FileNameUtil
    {
        public static string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName.Trim();
        }
    }

    internal static class SimpleMessageBox
    {
        public static Task Show(Window owner, string message, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Margin = new Avalonia.Thickness(14),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Width = 120,
                Margin = new Avalonia.Thickness(0, 0, 0, 12)
            };

            okButton.Click += (_, _) => dialog.Close();

            dialog.Content = new StackPanel
            {
                Children =
                {
                    textBlock,
                    okButton
                }
            };

            return dialog.ShowDialog(owner);
        }
    }
}

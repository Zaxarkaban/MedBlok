using Avalonia.Controls;
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
            AppWindowSetup.Configure(this, serviceProvider);

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
            listBox.SelectedItem = null;
            var programs = _storage.ListPrograms();
            listBox.ItemsSource = programs;

            status.Text = programs.Count == 0
                ? $"Нет пользовательских программ. Папка: {_storage.RootFolderPath}"
                : $"Найдено программ: {programs.Count}. Папка: {_storage.RootFolderPath}";
        }

        private void BackToMenu_Click(object? sender, RoutedEventArgs e)
        {
            var menuWindow = _serviceProvider.GetRequiredService<MenuWindow>();
            menuWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
                await AppDialog.ShowAsync(this, "Выбери программу из списка.", "Предупреждение", AppDialogKind.Warning);
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
                await AppDialog.ShowAsync(this, "Выбери программу из списка.", "Предупреждение", AppDialogKind.Warning);
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
                await AppDialog.ShowAsync(this, "Выбери программу из списка.", "Предупреждение", AppDialogKind.Warning);
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
                await AppDialog.ShowAsync(this, "Экспорт завершён.", "Успех", AppDialogKind.Success);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка экспорта: {ex.Message}", "Ошибка", AppDialogKind.Error);
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
                await AppDialog.ShowAsync(this, "Импорт завершён.", "Успех", AppDialogKind.Success);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка импорта: {ex.Message}", "Ошибка", AppDialogKind.Error);
            }
        }

        private async void Delete_Click(object? sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("ProgramsListBox");
            if (listBox?.SelectedItem is not UserProgramInfo info)
            {
                await AppDialog.ShowAsync(this, "Выбери программу из списка.", "Предупреждение", AppDialogKind.Warning);
                return;
            }

            try
            {
                listBox.SelectedItem = null;
                _storage.DeleteProgram(info);
                await ReloadAsync();
                await AppDialog.ShowAsync(this, "Удалено.", "Успех", AppDialogKind.Success);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowAsync(this, $"Ошибка удаления: {ex.Message}", "Ошибка", AppDialogKind.Error);
            }
        }

        private async void NotImplemented_Click(object? sender, RoutedEventArgs e)
        {
            await AppDialog.ShowAsync(this, "Функция будет добавлена следующим шагом внедрения конструктора.", "В разработке", AppDialogKind.Info);
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

}

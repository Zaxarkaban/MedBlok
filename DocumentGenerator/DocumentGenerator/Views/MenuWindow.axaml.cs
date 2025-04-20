using System;
using Avalonia.Controls;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator
{
    public partial class MenuWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MenuWindow()
        {
            InitializeComponent();
        }

        public MenuWindow(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
        }

        private void OpenMainWindowButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Close();
        }
    }
}
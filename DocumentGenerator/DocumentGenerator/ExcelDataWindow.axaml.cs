using Avalonia.Controls;
using Avalonia.Interactivity;
using DocumentGenerator.Services;
using DocumentGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DocumentGenerator
{
    public partial class ExcelDataWindow : Window
    {
        private ExcelDataViewModel ViewModel => (ExcelDataViewModel)DataContext!;

        public ExcelDataWindow()
        {
            InitializeComponent();
        }

        public ExcelDataWindow(IServiceProvider? serviceProvider) : this()
        {
            if (serviceProvider != null)
            {
                var theme = serviceProvider.GetRequiredService<IThemeService>();
                SpaceWindowChrome.Attach(this, theme);
            }
            else if (App.Services != null)
            {
                var theme = App.Services.GetRequiredService<IThemeService>();
                SpaceWindowChrome.Attach(this, theme);
            }

            if (this.FindControl<Button>("CloseButton") is Button closeButton)
                closeButton.Click += (_, _) => Close();
        }

        private async void SaveToPdf_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveToPdf(this);
        }
    }
}

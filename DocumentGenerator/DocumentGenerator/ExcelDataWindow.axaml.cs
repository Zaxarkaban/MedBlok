using Avalonia.Controls;
using Avalonia.Interactivity;
using DocumentGenerator.ViewModels;

namespace DocumentGenerator
{
    public partial class ExcelDataWindow : Window
    {
        private ExcelDataViewModel ViewModel => (ExcelDataViewModel)DataContext;

        public ExcelDataWindow()
        {
            InitializeComponent();
        }

        private async void SaveToPdf_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveToPdf(this);
        }
    }
}
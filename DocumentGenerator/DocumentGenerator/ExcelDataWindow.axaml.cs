using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DocumentGenerator
{
    public partial class ExcelDataWindow : Window
    {
        public ExcelDataWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
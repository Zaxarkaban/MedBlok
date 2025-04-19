using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DocumentGenerator.Views
{
    public partial class MainMenuView : UserControl
    {
        public MainMenuView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
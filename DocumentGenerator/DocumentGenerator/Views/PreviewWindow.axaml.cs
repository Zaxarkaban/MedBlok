using Avalonia.Controls;
using DocumentGenerator.ViewModels;

namespace DocumentGenerator.Views
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(PreviewViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        public PreviewWindow()
        {
            if (Design.IsDesignMode)
            {
                DataContext = new PreviewViewModel(new MainWindowViewModel());
                InitializeComponent();
                return;
            }

            throw new System.InvalidOperationException("This constructor is only for design-time use. Use the parameterized constructor with DI.");
        }
    }
}
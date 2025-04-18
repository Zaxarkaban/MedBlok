using Avalonia.Controls;
using Avalonia.Interactivity;
using DocumentGenerator.ViewModels;
using System;

namespace DocumentGenerator
{
    public partial class PreviewWindow : Window
    {
        private PreviewViewModel ViewModel => (PreviewViewModel)DataContext;

        public PreviewWindow()
        {
            InitializeComponent();
        }

        private async void SaveToPdf_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Кнопка 'Сохранить в PDF' нажата");
            if (ViewModel == null)
            {
                Console.WriteLine("Ошибка: ViewModel не установлен!");
                return;
            }
            await ViewModel.SaveToPdf(this); // Асинхронный вызов
        }
    }
}
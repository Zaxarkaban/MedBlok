using Avalonia.Threading;
using DocumentGenerator.Views;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Windows.Input;

namespace DocumentGenerator.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private object _currentView;
        private readonly IServiceProvider _serviceProvider;

        public object CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public ICommand GoToDataEntryCommand { get; }

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            CurrentView = new MainMenuView();

            GoToDataEntryCommand = new RelayCommand(() =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var dataEntryViewModel = _serviceProvider.GetService<DataEntryViewModel>();
                    CurrentView = new DataEntryView { DataContext = dataEntryViewModel };
                });
            });
        }
    }

    // Простая реализация ICommand
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
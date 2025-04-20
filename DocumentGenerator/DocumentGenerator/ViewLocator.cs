using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DocumentGenerator.ViewModels;

namespace DocumentGenerator
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            // Получаем имя ViewModel (например, "MainWindowViewModel")
            var viewModelName = param.GetType().Name;
            // Убираем "ViewModel" из имени (получаем "MainWindow")
            var viewName = viewModelName.Replace("ViewModel", "");
            // Формируем имя типа представления в пространстве имен DocumentGenerator
            var name = $"DocumentGenerator.{viewName}";

            var type = Type.GetType(name);
            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
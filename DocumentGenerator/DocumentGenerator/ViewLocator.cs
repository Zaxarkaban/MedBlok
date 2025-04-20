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

            // �������� ��� ViewModel (��������, "MainWindowViewModel")
            var viewModelName = param.GetType().Name;
            // ������� "ViewModel" �� ����� (�������� "MainWindow")
            var viewName = viewModelName.Replace("ViewModel", "");
            // ��������� ��� ���� ������������� � ������������ ���� DocumentGenerator
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
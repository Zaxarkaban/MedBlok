using System.Collections.Generic;

namespace DocumentGenerator.Models
{
    public static class Dictionaries
    {
        public static readonly Dictionary<string, string[]> OrderClauseDoctors = new Dictionary<string, string[]>
        {
            { "Общие осмотры", new[] { "Терапевт Иванов", "Офтальмолог Петров" } },
            { "Неврологические исследования", new[] { "Невролог Сидоров" } },
            { "Кардиологические исследования", new[] { "Кардиолог Кузнецов" } },
            { "Дерматологические исследования", new[] { "Дерматолог Смирнова" } }
        };
    }
}
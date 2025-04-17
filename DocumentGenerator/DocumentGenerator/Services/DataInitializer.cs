using DocumentGenerator.Data;
using DocumentGenerator.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentGenerator.Services
{
    public class DataInitializer
    {
        private readonly AppDbContext _context;

        public DataInitializer(AppDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            // Проверяем, применены ли миграции
            if (!(await _context.Database.GetPendingMigrationsAsync()).Any())
            {
                // Инициализация тестовых данных
                if (!await _context.OrderClauses.AnyAsync(c => c.ClauseText == "Пункт 1 - Общие осмотры"))
                {
                    _context.OrderClauses.AddRange(
                        new OrderClause { ClauseText = "Пункт 1 - Общие осмотры" },
                        new OrderClause { ClauseText = "Пункт 2 - Неврологические исследования" },
                        new OrderClause { ClauseText = "Пункт 3 - Кардиологические исследования" },
                        new OrderClause { ClauseText = "Пункт 4 - Дерматологические исследования" }
                    );
                    await _context.SaveChangesAsync();
                }

                if (!await _context.Doctors.AnyAsync(d => d.DoctorName == "Терапевт Иванов" && d.ClauseId == 1))
                {
                    _context.Doctors.AddRange(
                        new Doctor { DoctorName = "Терапевт Иванов", ClauseId = 1 },
                        new Doctor { DoctorName = "Невролог Петров", ClauseId = 2 },
                        new Doctor { DoctorName = "Кардиолог Сидоров", ClauseId = 3 },
                        new Doctor { DoctorName = "Дерматолог Кузнецов", ClauseId = 4 },
                        new Doctor { DoctorName = "Терапевт Иванов", ClauseId = 3 }
                    );
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
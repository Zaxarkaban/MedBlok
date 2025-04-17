using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Database.EnsureCreated();

                if (!context.OrderClauses.Any())
                {
                    context.OrderClauses.AddRange(
                        new OrderClause { ClauseText = "Пункт 1 - Общие осмотры" },
                        new OrderClause { ClauseText = "Пункт 2 - Неврологические исследования" },
                        new OrderClause { ClauseText = "Пункт 3 - Кардиологические исследования" },
                        new OrderClause { ClauseText = "Пункт 4 - Дерматологические исследования" }
                    );
                    context.SaveChanges();
                }

                if (!context.Doctors.Any())
                {
                    context.Doctors.AddRange(
                        new Doctor { DoctorName = "Терапевт Иванов", ClauseId = 1 },
                        new Doctor { DoctorName = "Невролог Петров", ClauseId = 2 },
                        new Doctor { DoctorName = "Кардиолог Сидоров", ClauseId = 3 },
                        new Doctor { DoctorName = "Дерматолог Кузнецов", ClauseId = 4 }
                    );
                    context.SaveChanges();
                }
            }
        }
    }
}
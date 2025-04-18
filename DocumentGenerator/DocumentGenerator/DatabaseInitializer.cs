using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace DocumentGenerator
{
    public class DatabaseInitializer
    {
        private readonly string _dbPath;

        public DatabaseInitializer()
        {
            // Синхронизируем путь с AppDbContext и PdfGenerator
            string dbDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocumentGenerator");
            Directory.CreateDirectory(dbDir); // Создаём папку, если её нет
            _dbPath = Path.Combine(dbDir, "OrderClauses.db");
        }

        public void Initialize()
        {
            // Создаем базу данных, если она не существует
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                // Создаем таблицу OrderClauses
                var createClausesTable = connection.CreateCommand();
                createClausesTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS OrderClauses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ClauseText TEXT NOT NULL
                    )";
                createClausesTable.ExecuteNonQuery();

                // Создаем таблицу Doctors
                var createDoctorsTable = connection.CreateCommand();
                createDoctorsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Doctors (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DoctorName TEXT NOT NULL,
                        ClauseId INTEGER NOT NULL,
                        FOREIGN KEY (ClauseId) REFERENCES OrderClauses(Id)
                    )";
                createDoctorsTable.ExecuteNonQuery();

                // Проверяем, есть ли данные в таблице OrderClauses
                var checkClauses = connection.CreateCommand();
                checkClauses.CommandText = "SELECT COUNT(*) FROM OrderClauses";
                var clauseCount = (long)checkClauses.ExecuteScalar();

                if (clauseCount == 0)
                {
                    // Заполняем тестовые данные для пунктов приказа
                    var insertClauses = connection.CreateCommand();
                    insertClauses.CommandText = @"
                        INSERT INTO OrderClauses (ClauseText) VALUES
                        ('Пункт 1 - Общие осмотры'),
                        ('Пункт 2 - Неврологические исследования'),
                        ('Пункт 3 - Кардиологические исследования'),
                        ('Пункт 4 - Дерматологические исследования')";
                    insertClauses.ExecuteNonQuery();

                    // Заполняем тестовые данные для врачей
                    var insertDoctors = connection.CreateCommand();
                    insertDoctors.CommandText = @"
                        INSERT INTO Doctors (DoctorName, ClauseId) VALUES
                        ('Терапевт Иванов', 1),
                        ('Невролог Петров', 2),
                        ('Кардиолог Сидоров', 3),
                        ('Дерматолог Кузнецов', 4),
                        ('Терапевт Иванов', 3)"; // Пересекающийся врач
                    insertDoctors.ExecuteNonQuery();
                }
            }
        }

        public string GetDbPath()
        {
            return _dbPath;
        }
    }
}
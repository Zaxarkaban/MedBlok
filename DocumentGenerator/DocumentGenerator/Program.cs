using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DocumentGenerator.Data;
using DocumentGenerator.Services;
using DocumentGenerator.ViewModels;
using DocumentGenerator.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;

namespace DocumentGenerator
{
    public class Program
    {
        private static readonly string LogFilePath = "startup_log.txt";

        public static IServiceProvider ServiceProvider { get; set; } = null!;

        public static int Main(string[] args)
        {
            try
            {
                Log("Starting application...");
                Log($"Args: {(args.Length > 0 ? string.Join(", ", args) : "None")}");

                // Инициализация лицензии EPPlus
                Log("Initializing EPPlus license...");
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                Log("EPPlus license initialized successfully.");

                // Создаём AppBuilder и сразу инициализируем платформу
                Log("Building Avalonia app...");
                var builder = BuildAvaloniaApp();
                if (builder == null)
                {
                    Log("ERROR: AppBuilder is null after BuildAvaloniaApp.");
                    throw new InvalidOperationException("AppBuilder is null after BuildAvaloniaApp.");
                }
                Log("Avalonia app built successfully.");

                // Запускаем приложение
                Log("Starting application with ClassicDesktopLifetime...");
                builder.StartWithClassicDesktopLifetime(args, desktop =>
                {
                    try
                    {
                        Log("Inside StartWithClassicDesktopLifetime callback.");

                        // После инициализации платформы создаём область для зависимостей
                        Log("Creating dependency injection scope...");
                        using (var scope = ServiceProvider.CreateScope())
                        {
                            Log("Dependency injection scope created successfully.");

                            // Применяем миграции
                            Log("Resolving AppDbContext...");
                            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            if (dbContext == null)
                            {
                                Log("ERROR: AppDbContext is null.");
                                throw new InvalidOperationException("AppDbContext is null.");
                            }
                            Log("AppDbContext resolved successfully.");

                            Log("Applying database migrations...");
                            dbContext.Database.Migrate();
                            Log("Database migrations applied successfully.");

                            // Создаём главное окно
                            Log("Resolving MainWindow...");
                            var mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
                            if (mainWindow == null)
                            {
                                Log("ERROR: MainWindow is null.");
                                throw new InvalidOperationException("MainWindow is null.");
                            }
                            Log("MainWindow resolved successfully.");

                            desktop.MainWindow = mainWindow;
                            Log("MainWindow assigned to desktop.MainWindow.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"ERROR in StartWithClassicDesktopLifetime: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        throw;
                    }
                });

                Log("Application started successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Log($"FATAL ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return 1;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            try
            {
                // Настраиваем сервисы перед инициализацией Avalonia
                Log("Configuring services...");
                ServiceProvider = ConfigureServices();
                if (ServiceProvider == null)
                {
                    Log("ERROR: ServiceProvider is null after ConfigureServices.");
                    throw new InvalidOperationException("ServiceProvider is null after ConfigureServices.");
                }
                Log("Services configured successfully.");

                // Инициализируем Avalonia
                Log("Initializing Avalonia...");
                var builder = AppBuilder.Configure<App>();
                Log("AppBuilder configured.");

                Log("Calling UsePlatformDetect...");
                builder = builder.UsePlatformDetect();
                Log("UsePlatformDetect called successfully.");

                Log("Setting up fonts and logging...");
                builder = builder.WithInterFont().LogToTrace();
                Log("Fonts and logging set up successfully.");

                // Проверяем, что AppBuilder корректно инициализирован
                if (builder.Instance == null)
                {
                    Log("ERROR: AppBuilder.Instance is null.");
                    throw new InvalidOperationException("AppBuilder.Instance is null after initialization.");
                }

                Log("Avalonia initialized successfully.");
                return builder;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            try
            {
                Log("Creating ServiceCollection...");
                var services = new ServiceCollection();
                Log("ServiceCollection created.");

                // Регистрация AppDbContext для SQLite
                Log("Registering AppDbContext...");
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=app.db"));
                Log("AppDbContext registered.");

                // Регистрируем сервисы
                Log("Registering services...");
                services.AddScoped<PdfGenerator>();
                services.AddScoped<MainWindowViewModel>();
                services.AddScoped<ExcelDataViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<ExcelDataWindow>();
                Log("Services registered successfully.");

                Log("Building ServiceProvider...");
                var provider = services.BuildServiceProvider();
                Log("ServiceProvider built successfully.");

                return provider;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private static void Log(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            // Вывод в консоль
            Console.WriteLine(timestampedMessage);
            // Вывод в окно отладки Visual Studio
            Debug.WriteLine(timestampedMessage);
            // Запись в файл
            try
            {
                File.AppendAllText(LogFilePath, timestampedMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Log] Failed to write to log file: {ex.Message}");
            }
        }
    }

}
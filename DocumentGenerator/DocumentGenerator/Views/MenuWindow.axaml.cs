using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Animation;
using Microsoft.Extensions.DependencyInjection;
using System;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
using System.Linq;

namespace DocumentGenerator
{
    public partial class MenuWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<Particle> _particles = new List<Particle>();
        private const int ParticleCount = 30;
        private bool _isRunning = true;
        private Point _mousePosition;
        private Ellipse? _newDocumentRippleEffect;
        private Ellipse? _exitRippleEffect;
        private bool _isGradientAnimating = true;

        public MenuWindow()
        {
            InitializeComponent();
        }

        public MenuWindow(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;

            // Регистрируем обработчики событий для кнопок
            var newDocumentButton = this.FindControl<Button>("NewDocumentButton");
            newDocumentButton?.AddHandler(Button.ClickEvent, NewDocumentButton_Click);

            var newFormButton = this.FindControl<Button>("NewFormButton");
            newFormButton?.AddHandler(Button.ClickEvent, NewFormButton_Click);

            var exitButton = this.FindControl<Button>("ExitButton");
            exitButton?.AddHandler(Button.ClickEvent, ExitButton_Click);

            // Добавляем обработчик для эффекта волны
            newDocumentButton?.AddHandler(Button.PointerPressedEvent, OnButtonPressed);
            exitButton?.AddHandler(Button.PointerPressedEvent, OnButtonPressed);

            // Инициализация RippleEffect для кнопок
            if (newDocumentButton != null)
            {
                newDocumentButton.ApplyTemplate();
                _newDocumentRippleEffect = newDocumentButton.GetTemplateChildren().OfType<Ellipse>().FirstOrDefault(e => e.Name == "RippleEffect");
            }

            if (exitButton != null)
            {
                exitButton.ApplyTemplate();
                _exitRippleEffect = exitButton.GetTemplateChildren().OfType<Ellipse>().FirstOrDefault(e => e.Name == "RippleEffect");
            }

            // Анимация градиента
            if (this.Background is LinearGradientBrush backgroundGradient)
            {
                _ = AnimateGradientAsync(backgroundGradient);
            }

            // Анимация заголовка
            var headerText = this.FindControl<TextBlock>("HeaderText");
            if (headerText != null)
            {
                headerText.Opacity = 1; // Запускаем анимацию появления
                _ = AnimateHeaderText(headerText);
            }

            // Инициализация частиц
            var particleCanvas = this.FindControl<Canvas>("ParticleCanvas");
            if (particleCanvas != null)
            {
                for (int i = 0; i < ParticleCount; i++)
                {
                    var particle = new Particle(particleCanvas);
                    _particles.Add(particle);
                    particleCanvas.Children.Add(particle.Ellipse);
                }

                // Запускаем анимацию частиц
                _ = AnimateParticlesAsync();

                // Отслеживаем движение курсора
                this.PointerMoved += (s, e) =>
                {
                    _mousePosition = e.GetPosition(particleCanvas);
                };
            }
        }

        // Эффект волны при нажатии на кнопку
        private void OnButtonPressed(object? sender, PointerPressedEventArgs e)
        {
            Ellipse? ripple = null;
            if (sender == this.FindControl<Button>("NewDocumentButton"))
            {
                ripple = _newDocumentRippleEffect;
            }
            else if (sender == this.FindControl<Button>("ExitButton"))
            {
                ripple = _exitRippleEffect;
            }

            if (ripple != null)
            {
                ripple.Width = 0;
                ripple.Height = 0;
                ripple.Opacity = 0.5;

                // Анимация волны
                ripple.Width = 300;
                ripple.Height = 300;
                ripple.Opacity = 0;
            }
        }

        // Анимация текста заголовка (эффект печатной машинки)
        private async Task AnimateHeaderText(TextBlock headerText)
        {
            string fullText = "Document Generator";
            headerText.Text = "";
            foreach (char c in fullText)
            {
                headerText.Text += c;
                await Task.Delay(100);
            }
        }

        // Анимация градиента фона
        private async Task AnimateGradientAsync(LinearGradientBrush gradient)
        {
            var color1Start = Avalonia.Media.Color.Parse("#063d2c");
            var color1End = Avalonia.Media.Color.Parse("#0a7557");
            var color2Start = Avalonia.Media.Color.Parse("#125e47");
            var color2End = Avalonia.Media.Color.Parse("#1a9b82");

            double duration = 5000; // 5 секунд
            double elapsed = 0;
            bool forward = true;

            while (_isGradientAnimating)
            {
                double t = elapsed / duration;
                if (!forward) t = 1 - t;

                // Линейная интерполяция цветов
                var color1 = InterpolateColor(color1Start, color1End, t);
                var color2 = InterpolateColor(color2Start, color2End, t);

                gradient.GradientStops[0].Color = color1;
                gradient.GradientStops[1].Color = color2;
                gradient.GradientStops[2].Color = color1;

                elapsed += 16; // ~60 FPS
                if (elapsed >= duration)
                {
                    elapsed = 0;
                    forward = !forward;
                }

                await Task.Delay(16);
            }
        }

        // Вспомогательный метод для интерполяции цветов
        private Avalonia.Media.Color InterpolateColor(Avalonia.Media.Color start, Avalonia.Media.Color end, double t)
        {
            byte r = (byte)(start.R + (end.R - start.R) * t);
            byte g = (byte)(start.G + (end.G - start.G) * t);
            byte b = (byte)(start.B + (end.B - start.B) * t);
            byte a = (byte)(start.A + (end.A - start.A) * t);
            return new Avalonia.Media.Color(a, r, g, b);
        }

        // Класс для частиц
        private class Particle
        {
            public Ellipse Ellipse { get; }
            private double _x, _y;
            private double _speedX, _speedY;
            private readonly Canvas _canvas;
            private readonly Random _random = new Random();

            public Particle(Canvas canvas)
            {
                _canvas = canvas;
                Ellipse = new Ellipse
                {
                    Width = 5,
                    Height = 5
                };
                Ellipse.Classes.Add("particle");

                // Случайно выбираем сторону появления: 0 - сверху, 1 - справа, 2 - снизу, 3 - слева
                int side = _random.Next(4);
                switch (side)
                {
                    case 0: // Сверху
                        _x = _random.NextDouble() * canvas.Bounds.Width;
                        _y = 0;
                        _speedX = _random.NextDouble() * 2 - 1;
                        _speedY = _random.NextDouble() * 1 + 0.5; // Движение вниз
                        break;
                    case 1: // Справа
                        _x = canvas.Bounds.Width;
                        _y = _random.NextDouble() * canvas.Bounds.Height;
                        _speedX = -(_random.NextDouble() * 1 + 0.5); // Движение влево
                        _speedY = _random.NextDouble() * 2 - 1;
                        break;
                    case 2: // Снизу
                        _x = _random.NextDouble() * canvas.Bounds.Width;
                        _y = canvas.Bounds.Height;
                        _speedX = _random.NextDouble() * 2 - 1;
                        _speedY = -(_random.NextDouble() * 1 + 0.5); // Движение вверх
                        break;
                    case 3: // Слева
                        _x = 0;
                        _y = _random.NextDouble() * canvas.Bounds.Height;
                        _speedX = _random.NextDouble() * 1 + 0.5; // Движение вправо
                        _speedY = _random.NextDouble() * 2 - 1;
                        break;
                }

                Canvas.SetLeft(Ellipse, _x);
                Canvas.SetTop(Ellipse, _y);
            }

            public void Update(Point mousePosition)
            {
                _x += _speedX;
                _y += _speedY;

                // Если частица вышла за границы, создаём её заново с одной из сторон
                if (_x < -10 || _x > _canvas.Bounds.Width + 10 || _y < -10 || _y > _canvas.Bounds.Height + 10)
                {
                    int side = _random.Next(4);
                    switch (side)
                    {
                        case 0: // Сверху
                            _x = _random.NextDouble() * _canvas.Bounds.Width;
                            _y = 0;
                            _speedX = _random.NextDouble() * 2 - 1;
                            _speedY = _random.NextDouble() * 1 + 0.5;
                            break;
                        case 1: // Справа
                            _x = _canvas.Bounds.Width;
                            _y = _random.NextDouble() * _canvas.Bounds.Height;
                            _speedX = -(_random.NextDouble() * 1 + 0.5);
                            _speedY = _random.NextDouble() * 2 - 1;
                            break;
                        case 2: // Снизу
                            _x = _random.NextDouble() * _canvas.Bounds.Width;
                            _y = _canvas.Bounds.Height;
                            _speedX = _random.NextDouble() * 2 - 1;
                            _speedY = -(_random.NextDouble() * 1 + 0.5);
                            break;
                        case 3: // Слева
                            _x = 0;
                            _y = _random.NextDouble() * _canvas.Bounds.Height;
                            _speedX = _random.NextDouble() * 1 + 0.5;
                            _speedY = _random.NextDouble() * 2 - 1;
                            break;
                    }
                }

                // Реакция на курсор
                double dx = mousePosition.X - _x;
                double dy = mousePosition.Y - _y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < 100)
                {
                    _x -= dx * 0.02;
                    _y -= dy * 0.02;
                }

                // Обновление позиции
                Canvas.SetLeft(Ellipse, _x);
                Canvas.SetTop(Ellipse, _y);

                // Случайное мерцание
                if (_random.NextDouble() < 0.05)
                {
                    Ellipse.Opacity = 0.5;
                    Ellipse.Width = 5;
                    Ellipse.Height = 5;
                }
            }
        }

        // Анимация частиц
        private async Task AnimateParticlesAsync()
        {
            while (_isRunning)
            {
                foreach (var particle in _particles)
                {
                    particle.Update(_mousePosition);
                }
                await Task.Delay(16); // ~60 FPS
            }
        }

        // Обработчики нажатий кнопок
        private void NewDocumentButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            this.Close();
        }

        private void NewFormButton_Click(object? sender, RoutedEventArgs e)
        {
            var newForm = _serviceProvider.GetRequiredService<NewForm>();
            newForm.Show();
        }

        private void ExitButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _isRunning = false; // Останавливаем анимацию частиц
            _isGradientAnimating = false; // Останавливаем анимацию градиента
        }
    }
}
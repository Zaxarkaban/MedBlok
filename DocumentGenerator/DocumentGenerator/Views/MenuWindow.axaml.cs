using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocumentGenerator.Models;
using DocumentGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
namespace DocumentGenerator
{
    public partial class MenuWindow : Window
    {
        private enum NebulaKind { Cloud, Ribbon, Accent }

        private enum BlackHoleSpawnScaleKind
        {
            Random,
            Full,
            /// <summary>Фиксированный «средний» масштаб (пасхалка: удержание только ПКМ).</summary>
            FixedMedium
        }

        private const int ParticleCount = 36;
        private const int FarStarCount = 156;
        private const int NearStarCount = 114;
        private const int DustCount = 144;
        private const int FlareStarCount = 5;
        private const double ParallaxMaxPx = 31;
        private const double StarFieldPadding = 56;

        private const double BlackHoleStarFeedK = 0.0088;

        /// <summary>Вклад одного метеора в рост ЧД — в 5 раз сильнее прежнего (/10); сейчас половина вклада условной звезды массы 1.</summary>
        private const double BlackHoleMeteorFeedAmount = BlackHoleStarFeedK * 0.5;

        /// <summary>Диаметр непрозрачного чёрного ядра в единицах <see cref="BlackHoleState.BaseHorizonR"/> (как в разметке дыры).</summary>
        private const double BlackHoleSolidCoreDiameterOverHorizon = 1.78;

        /// <summary>Инерционная масса метеора в модели гравитации (меньше — быстрее ускоряется к ЧД, чем звёзды).</summary>
        private const double MeteorGravMass = 0.26;

        /// <summary>Скорость затухания скорости метеора, 1/с (вязкое сопротивление).</summary>
        private const double MeteorDragPerSecond = 0.52;

        /// <summary>Вероятность появления чёрной дыры за один «бросок» (~каждые 2–4 с). Было 0.5; снижено в 20 раз → ~2.5% за бросок.</summary>
        private const double BlackHoleSpawnChance = 0.005;

        // N-body для чёрных дыр (взаимное притяжение + орбиты вокруг центра масс).
        private const int MaxBlackHoles = 2;
        /// <summary>Минимальный Plummer‑epsilon для пары (px): только анти‑деление на ноль.</summary>
        private const double BlackHolePairSofteningFloorPx = 1.8;
        /// <summary>Доля меньшего горизонта → epsilon; меньше = ближе к 1/r², но нужен мелкий шаг.</summary>
        private const double BlackHolePairSofteningOverMinHorizon = 0.32;
        private const double BlackHoleMutualAttractionK = 9;
        /// <summary>
        /// Степень p в ядре (r²+ε²)^{-p/2}: при p=3 это обычное «1/r²». Меньше p — сила на больших r сильнее (медленнее угасание).
        /// </summary>
        private const double BlackHoleMutualDistancePower = 3;
        /// <summary>Мягкая подстройка пары к круговой орбите при сближении: усиление тангенциального ускорения, 1/с.</summary>
        private const double BlackHolePairCircStabilizePerSec = 0.77;
        /// <summary>На расстоянии ≥ этого (px) подстройка орбиты почти выключена; при меньшем r плавно нарастает.</summary>
        private const double BlackHolePairCircBlendFarPx = 1000;
        /// <summary>Не больше доли расстояния «за шаг» при оценке скорости (Courant‑подобный лимит).</summary>
        private const double BlackHolePairCourantFrac = 0.07;
        private const double BlackHolePairCourantMinSpeed = 95;
        /// <summary>Для ровно двух дыр: чуть сильнее Plummer‑сглаживание, чтобы не «втыкаться» радиально из‑за дискрета.</summary>
        private const double BlackHoleTwoBodySofteningFloorPx = 2.75;
        private const double BlackHoleTwoBodySofteningOverMinHorizon = 0.42;
        /// <summary>Для 3 дыр: убрать накопленный численный дрейф центра масс (импульсы мыши почти не трогает).</summary>
        private const double BlackHoleManyBodyComVelocityDamp = 0.12;
        private const double BlackHoleAccelCap = 2.5e6; // только анти‑NaN; не резать нормальные орбиты
        private const double BlackHoleVelCap = 8000;
        /// <summary>Отражение от края поля: близко к 1, иначе орбиты у границы быстро «гасятся».</summary>
        private const double BlackHoleBounceDamping = 0.992;
        private const double BlackHoleSpawnScaleMin = 0.08; // "очень-очень малая"
        /// <summary>Всегда один и тот же размер при удержании только правой кнопки (10 с).</summary>
        private const double BlackHoleEasterRightHoldScale = 0.5;

        // Управление мышью: drag по дыре даёт один импульс при отпускании.
        private const double BlackHoleDragImpulseMax = 100; // px/s (базовый, дальше модификатор кнопки)
        private const double BlackHoleDragPixelsForMax = 95; // px drag для максимального импульса
        private const double BlackHoleDragDeadzonePx = 3; // игнор мелких дрожаний
        private const double BlackHolePickMinRadius = 18; // чтобы маленькие дыры можно было хватать

        private readonly IServiceProvider _serviceProvider;
        private readonly IThemeService _themeService;

        private readonly List<Particle> _particles = new();
        private readonly List<StarEntry> _starsFar = new();
        private readonly List<StarEntry> _starsNear = new();
        private readonly List<StarEntry> _dust = new();
        private readonly List<CrossFlareStar> _flares = new();
        private readonly List<NebulaBlob> _nebulaBlobs = new();
        private readonly List<PointerTrailSpark> _pointerTrail = new();
        private readonly List<MeteorInstance> _meteors = new();
        private readonly Random _rng = new();

        private Point _lastTrailSamplePos = new(-1e9, -1e9);

        private MenuThemePalette _palette = MenuThemePalette.ForMode(AppThemeMode.Default);

        private int _particleGeneration;
        private int _gradientGeneration;

        private bool _isRunning = true;
        private bool _isGradientAnimating;
        private Point _mousePosition;

        private Ellipse? _newDocumentRippleEffect;
        private Ellipse? _exitRippleEffect;

        private double _parallaxTargetX;
        private double _parallaxTargetY;
        private double _panX;
        private double _panY;

        private TranslateTransform? _tfFar;
        private TranslateTransform? _tfNebula;
        private TranslateTransform? _tfDust;
        private TranslateTransform? _tfNear;
        private TranslateTransform? _tfBgMicro;

        private double _nebulaPulsePhase;
        private double _nextAutoMeteorMs;
        private StarEntry? _signalStar;
        private double _signalTime;
        private double _signalCooldown = 5000;

        private DispatcherTimer? _spaceFrameTimer;

        private Size _particlesLayoutSize;
        private Size _spaceLayoutSize;

        private readonly List<BlackHoleState> _blackHoles = new();
        private double _nextBlackHoleRollMs;

        // Пасхалка: удержание ЛКМ/ПКМ 10 секунд почти без движения → чёрная дыра (только Space).
        private DispatcherTimer? _blackHoleEasterTimer;
        private bool _blackHoleHoldActive;
        /// <summary>True если пасхалка начата удержанием только ПКМ — спавним дыру фиксированного среднего размера.</summary>
        private bool _blackHoleEasterSpawnFixedMedium;
        private Avalonia.Point _blackHoleHoldStartWindow;
        private Avalonia.Point _blackHoleHoldPosNear;

        private BlackHoleState? _blackHoleDragTarget;
        /// <summary>Начало drag импульса в координатах BlackHoleCanvas (как Cx/Cy), с учётом параллакса.</summary>
        private Avalonia.Point _blackHoleDragStartBhCanvas;
        private bool _blackHoleDragActive;
        private bool _blackHoleDragRightButton;

        public MenuWindow()
        {
            InitializeComponent();
            AppBranding.ApplyWindowBranding(this);
            Resources["ThemeMainButtonOpacity"] = 0.9;
        }

        public MenuWindow(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
            _themeService = serviceProvider.GetRequiredService<IThemeService>();
            _themeService.ThemeChanged += OnThemeServiceThemeChanged;
            Opened += (_, _) => AppWindowSetup.ExpandToWorkAreaHeight(this);

            void Attach(Panel? p, ref TranslateTransform? t)
            {
                if (p == null) return;
                t = new TranslateTransform();
                p.RenderTransform = t;
            }

            Attach(this.FindControl<Panel>("ParallaxFarHost"), ref _tfFar);
            Attach(this.FindControl<Panel>("ParallaxNebulaHost"), ref _tfNebula);
            Attach(this.FindControl<Panel>("ParallaxDustHost"), ref _tfDust);
            Attach(this.FindControl<Panel>("ParallaxNearHost"), ref _tfNear);

            var bg = this.FindControl<Border>("BackgroundFillLayer");
            if (bg != null)
            {
                _tfBgMicro = new TranslateTransform();
                bg.RenderTransform = _tfBgMicro;
            }

            WireButtons();
            WirePointer();
            PointerPressed += OnWindowPointerPressedForMeteor;
            PointerPressed += OnWindowPointerPressedForBlackHoleEaster;
            PointerReleased += OnWindowPointerReleasedForBlackHoleEaster;
            PointerCaptureLost += OnWindowPointerCaptureLostForBlackHoleEaster;
            PointerPressed += OnWindowPointerPressedForBlackHoleImpulse;
            PointerReleased += OnWindowPointerReleasedForBlackHoleImpulse;
            PointerCaptureLost += OnWindowPointerCaptureLostForBlackHoleImpulse;

            Opened += (_, _) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ApplyTheme();
                    RunHeaderIntro();
                }, DispatcherPriority.Loaded);
            };

            SizeChanged += (_, _) => EnsureBackgroundLayersSized();
        }

        private void OnThemeServiceThemeChanged(AppThemeMode _)
        {
            Dispatcher.UIThread.Post(ApplyTheme);
        }

        private void WireButtons()
        {
            this.FindControl<Button>("NewDocumentButton")?.AddHandler(Button.ClickEvent, NewDocumentButton_Click);
            this.FindControl<Button>("NewFormButton")?.AddHandler(Button.ClickEvent, NewFormButton_Click);
            this.FindControl<Button>("ExitButton")?.AddHandler(Button.ClickEvent, ExitButton_Click);
            this.FindControl<Button>("AnalysisButton")?.AddHandler(Button.ClickEvent, AnalysisButton_Click);
            this.FindControl<Button>("EditorButton")?.AddHandler(Button.ClickEvent, EditorButton_Click);
            this.FindControl<Button>("UserProgramsButton")?.AddHandler(Button.ClickEvent, UserProgramsButton_Click);
            this.FindControl<Button>("ThemeSettingsButton")?.AddHandler(Button.ClickEvent, ThemeSettingsButton_Click);

            var newDocumentButton = this.FindControl<Button>("NewDocumentButton");
            var exitButton = this.FindControl<Button>("ExitButton");
            newDocumentButton?.AddHandler(Button.PointerPressedEvent, OnButtonPressed);
            exitButton?.AddHandler(Button.PointerPressedEvent, OnButtonPressed);

            if (newDocumentButton != null)
            {
                newDocumentButton.ApplyTemplate();
                _newDocumentRippleEffect = newDocumentButton.GetVisualDescendants().OfType<Ellipse>()
                    .FirstOrDefault(e => string.Equals(e.Name, "RippleEffect", StringComparison.Ordinal));
            }

            if (exitButton != null)
            {
                exitButton.ApplyTemplate();
                _exitRippleEffect = exitButton.GetVisualDescendants().OfType<Ellipse>()
                    .FirstOrDefault(e => string.Equals(e.Name, "RippleEffect", StringComparison.Ordinal));
            }
        }

        private void WirePointer()
        {
            PointerMoved += (s, e) =>
            {
                var pos = e.GetPosition(this);
                var nx = (pos.X / Math.Max(Bounds.Width, 1) - 0.5) * 2;
                var ny = (pos.Y / Math.Max(Bounds.Height, 1) - 0.5) * 2;
                _parallaxTargetX = -nx * ParallaxMaxPx;
                _parallaxTargetY = -ny * ParallaxMaxPx;

                var particleCanvas = this.FindControl<Canvas>("ParticleCanvas");
                if (particleCanvas != null)
                    _mousePosition = e.GetPosition(particleCanvas);

                TrySpawnPointerTrailSpark(e);

                if (_blackHoleHoldActive)
                {
                    var dx = pos.X - _blackHoleHoldStartWindow.X;
                    var dy = pos.Y - _blackHoleHoldStartWindow.Y;
                    if (dx * dx + dy * dy > 18 * 18)
                        CancelBlackHoleHold();
                }
            };
        }

        private void OnWindowPointerPressedForBlackHoleImpulse(object? sender, PointerPressedEventArgs e)
        {
            if (_palette.Mode != AppThemeMode.Space) return;
            if (_blackHoles.Count == 0) return;
            if (e.Source is Visual src && IsDescendantOfButton(src)) return;

            var pp = e.GetCurrentPoint(this);
            var left = pp.Properties.IsLeftButtonPressed;
            var right = pp.Properties.IsRightButtonPressed;
            if (!left && !right) return;

            if (!TryPickBlackHoleForPointer(e, out var bh))
                return;

            var bhCv = this.FindControl<Canvas>("BlackHoleCanvas");
            if (bhCv == null) return;

            _blackHoleDragTarget = bh;
            _blackHoleDragStartBhCanvas = e.GetPosition(bhCv);
            _blackHoleDragActive = true;
            _blackHoleDragRightButton = right && !left;
        }

        private void OnWindowPointerReleasedForBlackHoleImpulse(object? sender, PointerReleasedEventArgs e)
        {
            if (!_blackHoleDragActive || _blackHoleDragTarget == null)
                return;

            var bhCv = this.FindControl<Canvas>("BlackHoleCanvas");
            if (bhCv == null)
            {
                _blackHoleDragActive = false;
                _blackHoleDragTarget = null;
                _blackHoleDragRightButton = false;
                return;
            }

            var end = e.GetPosition(bhCv);
            var dx = end.X - _blackHoleDragStartBhCanvas.X;
            var dy = end.Y - _blackHoleDragStartBhCanvas.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len >= BlackHoleDragDeadzonePx)
            {
                var k = Math.Clamp(len / BlackHoleDragPixelsForMax, 0, 1);
                // ЛКМ: в 5 раз слабее. ПКМ: в 2 раза слабее.
                var btnMul = _blackHoleDragRightButton ? 0.5 : 0.2;
                var imp = BlackHoleDragImpulseMax * btnMul * k;
                _blackHoleDragTarget.Vx += (dx / len) * imp;
                _blackHoleDragTarget.Vy += (dy / len) * imp;
            }

            _blackHoleDragActive = false;
            _blackHoleDragTarget = null;
            _blackHoleDragRightButton = false;
        }

        private void OnWindowPointerCaptureLostForBlackHoleImpulse(object? sender, PointerCaptureLostEventArgs e)
        {
            _blackHoleDragActive = false;
            _blackHoleDragTarget = null;
            _blackHoleDragRightButton = false;
        }

        private void OnWindowPointerPressedForBlackHoleEaster(object? sender, PointerPressedEventArgs e)
        {
            if (_palette.Mode != AppThemeMode.Space) return;
            if (_blackHoles.Count >= MaxBlackHoles) return;

            if (e.Source is Visual src && IsDescendantOfButton(src)) return;

            var pp = e.GetCurrentPoint(this);
            var left = pp.Properties.IsLeftButtonPressed;
            var right = pp.Properties.IsRightButtonPressed;
            if (!left && !right) return;

            var near = this.FindControl<Canvas>("StarsNearCanvas");
            if (near == null) return;

            _blackHoleHoldActive = true;
            _blackHoleEasterSpawnFixedMedium = right && !left;
            _blackHoleHoldStartWindow = e.GetPosition(this);
            _blackHoleHoldPosNear = e.GetPosition(near);

            _blackHoleEasterTimer?.Stop();
            _blackHoleEasterTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _blackHoleEasterTimer.Tick += OnBlackHoleEasterTick;
            _blackHoleEasterTimer.Start();
        }

        private void OnWindowPointerReleasedForBlackHoleEaster(object? sender, PointerReleasedEventArgs e) =>
            CancelBlackHoleHold();

        private void OnWindowPointerCaptureLostForBlackHoleEaster(object? sender, PointerCaptureLostEventArgs e) =>
            CancelBlackHoleHold();

        private void CancelBlackHoleHold()
        {
            _blackHoleHoldActive = false;
            if (_blackHoleEasterTimer != null)
            {
                _blackHoleEasterTimer.Stop();
                _blackHoleEasterTimer.Tick -= OnBlackHoleEasterTick;
                _blackHoleEasterTimer = null;
            }
        }

        private void OnBlackHoleEasterTick(object? sender, EventArgs e)
        {
            if (_blackHoleEasterTimer != null)
            {
                _blackHoleEasterTimer.Stop();
                _blackHoleEasterTimer.Tick -= OnBlackHoleEasterTick;
                _blackHoleEasterTimer = null;
            }

            if (!_blackHoleHoldActive) return;
            _blackHoleHoldActive = false;

            if (_palette.Mode != AppThemeMode.Space) return;
            if (_blackHoles.Count >= MaxBlackHoles) return;

            // Пасхалка: ЛКМ — случайный размер; только ПКМ — всегда одинаковая средняя дыра.
            TrySpawnBlackHoleAt(_blackHoleHoldPosNear,
                _blackHoleEasterSpawnFixedMedium ? BlackHoleSpawnScaleKind.FixedMedium : BlackHoleSpawnScaleKind.Random);
        }

        private void TrySpawnPointerTrailSpark(PointerEventArgs e)
        {
            if (!_palette.ShowStarsAndParallax) return;
            if (e.Source is Visual src && IsDescendantOfButton(src)) return;

            var cv = this.FindControl<Canvas>("PointerTrailCanvas");
            if (cv == null || cv.Width <= 1 || cv.Height <= 1) return;

            var p = e.GetPosition(cv);
            if (p.X < -8 || p.Y < -8 || p.X > cv.Width + 8 || p.Y > cv.Height + 8) return;

            var dx = p.X - _lastTrailSamplePos.X;
            var dy = p.Y - _lastTrailSamplePos.Y;
            if (dx * dx + dy * dy < 28) return;
            _lastTrailSamplePos = p;

            const int maxSparks = 42;
            while (_pointerTrail.Count >= maxSparks)
            {
                var drop = _pointerTrail[0];
                _pointerTrail.RemoveAt(0);
                cv.Children.Remove(drop.Ellipse);
            }

            var sz = 4.5 + _rng.NextDouble() * 5.5;
            var initialOp = 0.38 + _rng.NextDouble() * 0.28;
            var dot = new Ellipse
            {
                Width = sz,
                Height = sz,
                Fill = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(210, 255, 255, 255), 0),
                        new GradientStop(Color.FromArgb(100, 200, 225, 255), 0.42),
                        new GradientStop(Color.FromArgb(0, 140, 190, 255), 1)
                    }
                },
                IsHitTestVisible = false,
                Opacity = initialOp,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
            };
            Canvas.SetLeft(dot, p.X - sz * 0.5);
            Canvas.SetTop(dot, p.Y - sz * 0.5);
            cv.Children.Add(dot);
            _pointerTrail.Add(new PointerTrailSpark
            {
                Ellipse = dot,
                AgeMs = 0,
                InitialOpacity = initialOp,
                Vx = 0,
                Vy = 0
            });
        }

        private void UpdatePointerTrail(double dtMs)
        {
            var cv = this.FindControl<Canvas>("PointerTrailCanvas");
            if (cv == null || _pointerTrail.Count == 0) return;

            var dtSec = dtMs / 1000.0;
            const double lifeMs = 480;
            const double sparkMass = 0.52;

            for (var i = _pointerTrail.Count - 1; i >= 0; i--)
            {
                var sp = _pointerTrail[i];
                sp.AgeMs += dtMs;
                var t = sp.AgeMs / lifeMs;
                if (t >= 1)
                {
                    cv.Children.Remove(sp.Ellipse);
                    _pointerTrail.RemoveAt(i);
                    continue;
                }

                if (_blackHoles.Count > 0)
                {
                    var cx = Canvas.GetLeft(sp.Ellipse) + sp.Ellipse.Width * 0.5;
                    var cy = Canvas.GetTop(sp.Ellipse) + sp.Ellipse.Height * 0.5;
                    var bh = NearestBlackHoleWindow(cx, cy);
                    var (bhx, bhy) = BlackHoleCenterWindow(bh);
                    var h = EffectiveHorizon(bh);
                    var rBlack = BlackHoleBlackDiskOuterRadius(h);

                    var soft = h * 0.55;
                    var rx = bhx - cx;
                    var ry = bhy - cy;
                    var distSq = rx * rx + ry * ry + soft * soft;
                    var d = Math.Sqrt(distSq);
                    if (d > 1e-5)
                    {
                        var gm = EffectiveGravParameter(bh);
                        var inv = 1.0 / (d * distSq);
                        var ax = gm * rx * inv / sparkMass;
                        var ay = gm * ry * inv / sparkMass;
                        sp.Vx += ax * dtSec;
                        sp.Vy += ay * dtSec;
                    }

                    var cx1 = cx + sp.Vx * dtSec;
                    var cy1 = cy + sp.Vy * dtSec;
                    if (SegmentIntersectsFilledDisk(cx, cy, cx1, cy1, bhx, bhy, rBlack))
                    {
                        cv.Children.Remove(sp.Ellipse);
                        _pointerTrail.RemoveAt(i);
                        continue;
                    }

                    Canvas.SetLeft(sp.Ellipse, cx1 - sp.Ellipse.Width * 0.5);
                    Canvas.SetTop(sp.Ellipse, cy1 - sp.Ellipse.Height * 0.5);
                }

                var fade = (1 - t) * (1 - t);
                sp.Ellipse.Opacity = sp.InitialOpacity * fade;
                var sc = 0.72 + 0.28 * fade;
                sp.Ellipse.RenderTransform = new ScaleTransform(sc, sc);
            }
        }

        private void ClearPointerTrail()
        {
            var cv = this.FindControl<Canvas>("PointerTrailCanvas");
            if (cv != null)
                cv.Children.Clear();
            _pointerTrail.Clear();
            _lastTrailSamplePos = new Point(-1e9, -1e9);
        }

        private void OnWindowPointerPressedForMeteor(object? sender, PointerPressedEventArgs e)
        {
            if (!_palette.ShowStarsAndParallax) return;
            if (e.Source is not Visual v || IsDescendantOfButton(v))
                return;

            // Если клик по чёрной дыре — это управление импульсом, а не метеор.
            if (_palette.Mode == AppThemeMode.Space && _blackHoles.Count > 0)
            {
                if (TryPickBlackHoleForPointer(e, out _))
                    return;
            }

            var meteorCanvas = this.FindControl<Canvas>("MeteorCanvas");
            if (meteorCanvas == null) return;

            var pos = e.GetPosition(meteorCanvas);
            SpawnMeteorFromClick(pos);
        }

        private static bool IsDescendantOfButton(Visual? v)
        {
            while (v != null)
            {
                if (v is Button) return true;
                v = v.GetVisualParent();
            }

            return false;
        }

        private void SpawnMeteorFromClick(Point p)
        {
            if (_blackHoles.Count > 0)
            {
                var bh = NearestBlackHoleWindow(p.X, p.Y);
                var (bhx, bhy) = BlackHoleCenterWindow(bh);
                var dx = p.X - bhx;
                var dy = p.Y - bhy;
                var h = EffectiveHorizon(bh);
                var rBlack = BlackHoleBlackDiskOuterRadius(h);
                if (dx * dx + dy * dy <= rBlack * rBlack)
                {
                    FeedBlackHole(bh, BlackHoleMeteorFeedAmount);
                    return;
                }
            }

            var angle = _rng.NextDouble() * Math.PI * 2;
            var dir = new Vector(Math.Cos(angle), Math.Sin(angle));
            SpawnBallisticMeteor(p, dir, 0.75);
        }

        private void SpawnMeteorFromEdge()
        {
            var w = Bounds.Width;
            var h = Bounds.Height;
            if (w <= 0 || h <= 0) return;

            var edge = _rng.Next(4);
            Point start;
            Vector inward;
            switch (edge)
            {
                case 0:
                    start = new Point(_rng.NextDouble() * w, -8);
                    inward = new Vector((_rng.NextDouble() - 0.5) * 0.35, 1);
                    break;
                case 1:
                    start = new Point(w + 8, _rng.NextDouble() * h);
                    inward = new Vector(-1, (_rng.NextDouble() - 0.5) * 0.35);
                    break;
                case 2:
                    start = new Point(_rng.NextDouble() * w, h + 8);
                    inward = new Vector((_rng.NextDouble() - 0.5) * 0.35, -1);
                    break;
                default:
                    start = new Point(-8, _rng.NextDouble() * h);
                    inward = new Vector(1, (_rng.NextDouble() - 0.5) * 0.35);
                    break;
            }

            var len = Math.Sqrt(inward.X * inward.X + inward.Y * inward.Y);
            if (len > 1e-6)
                inward = new Vector(inward.X / len, inward.Y / len);
            SpawnBallisticMeteor(start, inward, 1);
        }

        private void SpawnBallisticMeteor(Point start, Vector dir, double lengthScale)
        {
            var meteorCanvas = this.FindControl<Canvas>("MeteorCanvas");
            if (meteorCanvas == null) return;

            const int trailSegments = 5;
            var speed = (240 + _rng.NextDouble() * 180) * (0.65 + 0.35 * lengthScale);
            var vx = dir.X * speed;
            var vy = dir.Y * speed;
            var hist = new Point[trailSegments + 1];
            for (var i = 0; i < hist.Length; i++)
                hist[i] = start;

            var lines = new Line[trailSegments];
            for (var i = 0; i < trailSegments; i++)
            {
                lines[i] = new Line
                {
                    Stroke = Brushes.White,
                    StrokeThickness = 2.4 - i * 0.38,
                    StrokeLineCap = PenLineCap.Round,
                    Opacity = 0.85 - i * 0.12
                };
                meteorCanvas.Children.Add(lines[i]);
            }

            _meteors.Add(new MeteorInstance
            {
                Segments = lines,
                Canvas = meteorCanvas,
                HeadX = start.X,
                HeadY = start.Y,
                Vx = vx,
                Vy = vy,
                Hist = hist,
                AgeSec = 0,
                CurveSign = _rng.Next(2) == 0 ? -1 : 1
            });
        }

        private async void ThemeSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = _serviceProvider.GetRequiredService<ThemeSettingsWindow>();
            await dlg.ShowDialog(this);
        }

        private void EnsureBackgroundLayersSized()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

            var current = new Size(Bounds.Width, Bounds.Height);

            var particleCanvas = this.FindControl<Canvas>("ParticleCanvas");
            var far = this.FindControl<Canvas>("StarsFarCanvas");
            var near = this.FindControl<Canvas>("StarsNearCanvas");
            var dust = this.FindControl<Canvas>("DustCanvas");
            var nebulaCanvas = this.FindControl<Canvas>("NebulaCanvas");
            var meteorCanvas = this.FindControl<Canvas>("MeteorCanvas");
            var trailCanvas = this.FindControl<Canvas>("PointerTrailCanvas");
            var blackHoleCanvas = this.FindControl<Canvas>("BlackHoleCanvas");

            if (particleCanvas != null)
            {
                particleCanvas.Width = Bounds.Width;
                particleCanvas.Height = Bounds.Height;
            }

            void SizeStarCanvas(Canvas? cv)
            {
                if (cv == null) return;
                var pad = StarFieldPadding;
                cv.Width = Bounds.Width + pad * 2;
                cv.Height = Bounds.Height + pad * 2;
                cv.Margin = new Thickness(-pad, -pad, 0, 0);
            }

            SizeStarCanvas(far);
            SizeStarCanvas(near);
            SizeStarCanvas(dust);
            SizeStarCanvas(blackHoleCanvas);

            if (nebulaCanvas != null)
            {
                nebulaCanvas.Width = Bounds.Width;
                nebulaCanvas.Height = Bounds.Height;
            }

            if (meteorCanvas != null)
            {
                meteorCanvas.Width = Bounds.Width;
                meteorCanvas.Height = Bounds.Height;
            }

            if (trailCanvas != null)
            {
                trailCanvas.Width = Bounds.Width;
                trailCanvas.Height = Bounds.Height;
            }

            if (_palette.ShowParticles &&
                (_particles.Count == 0 || LayoutSizeChanged(_particlesLayoutSize, current)))
            {
                if (_particles.Count > 0)
                    _particleGeneration++;
                ClearParticles();
                BuildParticles();
                _ = AnimateParticlesAsync(_particleGeneration);
                _particlesLayoutSize = current;
            }

            if (_palette.ShowStarsAndParallax &&
                (_starsNear.Count + _starsFar.Count == 0 || LayoutSizeChanged(_spaceLayoutSize, current)))
            {
                ClearStars();
                BuildAllSpaceLayers();
                ApplyNebulaBlurEffect();
                _spaceLayoutSize = current;
            }

        }

        private static bool LayoutSizeChanged(Size last, Size current)
        {
            if (last.Width <= 0 || last.Height <= 0) return true;
            return Math.Abs(last.Width - current.Width) > 0.5 || Math.Abs(last.Height - current.Height) > 0.5;
        }

        private void ApplyTheme()
        {
            _particleGeneration++;
            _gradientGeneration++;

            _palette = MenuThemePalette.ForMode(_themeService.CurrentTheme);

            Background = new SolidColorBrush(_palette.WindowBackgroundOpaque);

            if (_palette.Mode == AppThemeMode.Space)
                Classes.Add("space-glass");
            else
                Classes.Remove("space-glass");

            Resources["ThemeBtnNormal"] = _palette.ButtonNormal;
            Resources["ThemeBtnHover"] = _palette.ButtonHover;
            Resources["ThemeBtnPressed"] = _palette.ButtonPressed;
            Resources["ThemeHeaderForeground"] = new SolidColorBrush(_palette.HeaderForeground);
            Resources["ThemeParticleFill"] = new SolidColorBrush(_palette.ParticleColor);
            Resources["ThemeMainButtonOpacity"] = _palette.Mode switch
            {
                AppThemeMode.Default => 0.9,
                AppThemeMode.Colorful => 0.95,
                _ => 0.93
            };

            if (_palette.Mode == AppThemeMode.Space)
            {
                Resources["ThemeSpaceBtnBorder"] = new SolidColorBrush(Color.FromArgb(90, 210, 225, 255));
                Resources["ThemeSpaceBtnBorderHover"] = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
            }

            ApplyGearResources();
            ApplyAtmosphereLayer();
            ApplyBrandLogo();
            ApplyHeaderSpaceEffect();

            var bgLayer = this.FindControl<Border>("BackgroundFillLayer");
            if (bgLayer != null)
            {
                bgLayer.Background = CreateBackgroundGradient(_palette.BackgroundGradientStops);
            }

            _isGradientAnimating = _palette.AnimateBackgroundGradient;
            if (_palette.AnimateBackgroundGradient && bgLayer?.Background is LinearGradientBrush lg)
            {
                var gen = _gradientGeneration;
                _ = AnimateGradientAsync(lg, gen);
            }

            ClearParticles();
            ClearStars();

            if (_palette.ShowParticles)
            {
                BuildParticles();
                var gen = _particleGeneration;
                _ = AnimateParticlesAsync(gen);
                if (Bounds.Width > 0 && Bounds.Height > 0)
                    _particlesLayoutSize = new Size(Bounds.Width, Bounds.Height);
            }
            else
                _particlesLayoutSize = default;

            if (_palette.ShowStarsAndParallax)
            {
                BuildAllSpaceLayers();
                ApplyNebulaBlurEffect();
                if (Bounds.Width > 0 && Bounds.Height > 0)
                    _spaceLayoutSize = new Size(Bounds.Width, Bounds.Height);
                _nextAutoMeteorMs = 8000 + _rng.NextDouble() * 12000;
                _nebulaPulsePhase = _rng.NextDouble() * Math.PI * 2;
                _signalCooldown = 2500 + _rng.Next(5000);
                _nextBlackHoleRollMs = 1500 + _rng.NextDouble() * 1200;
                StartSpaceTimer();
            }
            else
            {
                _spaceLayoutSize = default;
                ApplyNebulaBlurEffect();
                StopSpaceTimer();
                ResetParallax();
                ClearPointerTrail();
                DestroyBlackHoles();
            }
        }

        private void ApplyAtmosphereLayer()
        {
            var atm = this.FindControl<Border>("AtmosphereLayer");
            if (atm == null) return;

            if (_palette.ShowStarsAndParallax)
            {
                atm.IsVisible = true;
                atm.Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(120, 15, 23, 42), 0),
                        new GradientStop(Color.FromArgb(55, 30, 58, 120), 0.45),
                        new GradientStop(Color.FromArgb(0, 3, 7, 18), 1)
                    }
                };
            }
            else
            {
                atm.IsVisible = false;
                atm.Background = null;
            }
        }

        private void ApplyBrandLogo()
        {
            var logo = this.FindControl<Image>("BrandLogo");
            if (logo == null)
                return;

            AppBranding.ApplyMenuLogo(logo, _palette.Mode);
        }

        private void ApplyHeaderSpaceEffect()
        {
            var header = this.FindControl<TextBlock>("HeaderText");
            var logo = this.FindControl<Image>("BrandLogo");

            if (_palette.Mode == AppThemeMode.Space)
            {
                var glow = new DropShadowEffect
                {
                    BlurRadius = 28,
                    Color = Color.Parse("#7c8cff"),
                    OffsetX = 0,
                    OffsetY = 0,
                    Opacity = 0.5
                };

                if (header != null)
                    header.Effect = glow;

                if (logo != null)
                {
                    logo.Effect = new DropShadowEffect
                    {
                        BlurRadius = 22,
                        Color = Color.Parse("#2ED1B5"),
                        OffsetX = 0,
                        OffsetY = 0,
                        Opacity = 0.45
                    };
                }
            }
            else
            {
                if (header != null)
                    header.Effect = null;

                if (logo != null)
                    logo.Effect = null;
            }
        }

        private void ApplyNebulaBlurEffect()
        {
            var nebulaCanvas = this.FindControl<Canvas>("NebulaCanvas");
            if (nebulaCanvas == null) return;
            nebulaCanvas.Effect = _palette.ShowStarsAndParallax ? new BlurEffect { Radius = 28 } : null;
        }

        private void ApplyGearResources()
        {
            Color bg, bgHover, fg, border;
            switch (_palette.Mode)
            {
                case AppThemeMode.Colorful:
                    bg = Color.FromArgb(230, 255, 255, 255);
                    bgHover = Color.FromArgb(255, 255, 255, 255);
                    fg = Color.Parse("#0f4c5c");
                    border = Color.FromArgb(200, 125, 211, 252);
                    break;
                case AppThemeMode.Space:
                    bg = Color.FromArgb(100, 79, 70, 229);
                    bgHover = Color.FromArgb(150, 99, 102, 241);
                    fg = Colors.White;
                    border = Color.FromArgb(130, 199, 210, 254);
                    break;
                default:
                    bg = Color.FromArgb(95, 255, 255, 255);
                    bgHover = Color.FromArgb(140, 255, 255, 255);
                    fg = Color.Parse("#063d2c");
                    border = Color.FromArgb(85, 255, 255, 255);
                    break;
            }

            Resources["ThemeGearBackground"] = new SolidColorBrush(bg);
            Resources["ThemeGearBackgroundHover"] = new SolidColorBrush(bgHover);
            Resources["ThemeGearForeground"] = new SolidColorBrush(fg);
            Resources["ThemeGearBorder"] = new SolidColorBrush(border);
        }

        private static LinearGradientBrush CreateBackgroundGradient(IReadOnlyList<Color> stops)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
            };
            if (stops.Count >= 3)
            {
                brush.GradientStops.Add(new GradientStop(stops[0], 0));
                brush.GradientStops.Add(new GradientStop(stops[1], 0.5));
                brush.GradientStops.Add(new GradientStop(stops[2], 1));
            }

            return brush;
        }

        private void ClearParticles()
        {
            var canvas = this.FindControl<Canvas>("ParticleCanvas");
            if (canvas == null) return;
            canvas.Children.Clear();
            _particles.Clear();
        }

        private void BuildParticles()
        {
            var canvas = this.FindControl<Canvas>("ParticleCanvas");
            if (canvas == null || Bounds.Width <= 0) return;

            canvas.Width = Bounds.Width;
            canvas.Height = Bounds.Height;
            if (canvas.Width <= 0) return;

            var rnd = new Random(42);
            for (var i = 0; i < ParticleCount; i++)
            {
                var p = new Particle(canvas, rnd);
                _particles.Add(p);
                canvas.Children.Add(p.Ellipse);
            }
        }

        private void ClearStars()
        {
            DestroyBlackHoles();

            foreach (var c in new[] { "StarsFarCanvas", "StarsNearCanvas", "DustCanvas" })
            {
                var cv = this.FindControl<Canvas>(c);
                if (cv != null)
                {
                    cv.Children.Clear();
                    cv.Margin = default;
                }
            }

            _starsFar.Clear();
            _starsNear.Clear();
            _dust.Clear();
            _flares.Clear();

            var nebulaCanvas = this.FindControl<Canvas>("NebulaCanvas");
            if (nebulaCanvas != null)
                nebulaCanvas.Children.Clear();
            _nebulaBlobs.Clear();

            var meteorCv = this.FindControl<Canvas>("MeteorCanvas");
            if (meteorCv != null)
            {
                meteorCv.Children.Clear();
                _meteors.Clear();
            }

            _signalStar = null;
            _signalTime = 0;
        }

        private void BuildAllSpaceLayers()
        {
            BuildStarLayer("StarsFarCanvas", _starsFar, FarStarCount, false, true);
            BuildStarLayer("StarsNearCanvas", _starsNear, NearStarCount, true, true);
            BuildStarLayer("DustCanvas", _dust, DustCount, false, false);
            BuildCrossFlares();
            BuildNebula();
        }

        private void BuildStarLayer(string canvasName, List<StarEntry> list, int count, bool allowWarm, bool allowSignal)
        {
            var canvas = this.FindControl<Canvas>(canvasName);
            if (canvas == null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

            var pad = StarFieldPadding;
            canvas.Width = Bounds.Width + pad * 2;
            canvas.Height = Bounds.Height + pad * 2;
            canvas.Margin = new Thickness(-pad, -pad, 0, 0);

            var rnd = new Random(canvasName.GetHashCode() + 13);
            for (var i = 0; i < count; i++)
            {
                var roll = rnd.NextDouble();
                Ellipse e;
                double oMin, oMax;
                IBrush fill;
                var size = rnd.Next(1, 4);

                if (roll < 0.12 && size >= 2)
                {
                    e = new Ellipse
                    {
                        Width = size + 1,
                        Height = size + 1,
                        Fill = new RadialGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Color.FromArgb(255, 255, 255, 255), 0),
                                new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                            }
                        }
                    };
                    oMin = 0.35;
                    oMax = 0.92;
                }
                else if (roll < 0.28 && allowWarm)
                {
                    fill = new SolidColorBrush(Color.FromRgb(255, 236, 210));
                    e = new Ellipse { Width = size, Height = size, Fill = fill };
                    oMin = 0.22;
                    oMax = 0.72;
                }
                else if (roll < 0.5)
                {
                    fill = new SolidColorBrush(Color.FromRgb(200, 218, 255));
                    e = new Ellipse { Width = size, Height = size, Fill = fill };
                    oMin = 0.18;
                    oMax = 0.68;
                }
                else
                {
                    fill = new SolidColorBrush(Colors.White);
                    e = new Ellipse { Width = size, Height = size, Fill = fill };
                    oMin = 0.15;
                    oMax = 0.62;
                }

                var phaseSpeed = 0.018 + rnd.NextDouble() * 0.055;
                Canvas.SetLeft(e, rnd.NextDouble() * canvas.Width);
                Canvas.SetTop(e, rnd.NextDouble() * canvas.Height);
                canvas.Children.Add(e);

                list.Add(new StarEntry
                {
                    Ellipse = e,
                    Phase = rnd.NextDouble() * Math.PI * 2,
                    PhaseSpeed = phaseSpeed,
                    OpacityMin = oMin,
                    OpacityMax = oMax,
                    CanSignal = allowSignal && rnd.NextDouble() < 0.12
                });
            }
        }

        private void BuildCrossFlares()
        {
            var canvas = this.FindControl<Canvas>("StarsNearCanvas");
            if (canvas == null || Bounds.Width <= 0) return;

            const double hostSize = 36;
            var cx = hostSize * 0.5;

            var rayBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(210, 255, 252, 248), 0.46),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                }
            };

            for (var f = 0; f < FlareStarCount; f++)
            {
                var host = new Canvas { Width = hostSize, Height = hostSize };
                var softRays = new Ellipse[4];
                for (var r = 0; r < 4; r++)
                {
                    softRays[r] = new Ellipse
                    {
                        Width = 3.6,
                        Height = 16,
                        Fill = rayBrush,
                        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        RenderTransform = new RotateTransform(r * 90),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(softRays[r], cx - 1.8);
                    Canvas.SetTop(softRays[r], cx - 8);
                    host.Children.Add(softRays[r]);
                }

                var core = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop(Color.FromArgb(255, 255, 255, 255), 0),
                            new GradientStop(Color.FromArgb(150, 255, 248, 240), 0.42),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        }
                    },
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(core, cx - 3.5);
                Canvas.SetTop(core, cx - 3.5);
                host.Children.Add(core);

                Canvas.SetLeft(host, _rng.NextDouble() * Math.Max(0, canvas.Width - hostSize));
                Canvas.SetTop(host, _rng.NextDouble() * Math.Max(0, canvas.Height - hostSize));
                canvas.Children.Add(host);

                _flares.Add(new CrossFlareStar
                {
                    Host = host,
                    Core = core,
                    SoftRays = softRays,
                    Phase = _rng.NextDouble() * Math.PI * 2,
                    PhaseSpeed = 0.006 + _rng.NextDouble() * 0.0085
                });
            }
        }

        private void BuildNebula()
        {
            var canvas = this.FindControl<Canvas>("NebulaCanvas");
            if (canvas == null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

            canvas.Width = Bounds.Width;
            canvas.Height = Bounds.Height;
            _nebulaBlobs.Clear();
            canvas.Children.Clear();

            void AddCloud(int idx)
            {
                var w = 200 + _rng.Next(280);
                var tints = new[]
                {
                    Color.FromArgb(50, 99, 102, 241),
                    Color.FromArgb(45, 168, 85, 247),
                    Color.FromArgb(42, 244, 114, 182)
                };
                var brush = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(tints[_rng.Next(tints.Length)], 0),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                    }
                };
                var border = new Border
                {
                    Width = w,
                    Height = w,
                    CornerRadius = new CornerRadius(w * 0.5),
                    Background = brush,
                    Opacity = 0.2 + _rng.NextDouble() * 0.18
                };
                var left = -w * 0.35 + _rng.NextDouble() * (Bounds.Width + w * 0.45);
                var top = -w * 0.35 + _rng.NextDouble() * (Bounds.Height + w * 0.45);
                Canvas.SetLeft(border, left);
                Canvas.SetTop(border, top);
                canvas.Children.Add(border);
                _nebulaBlobs.Add(new NebulaBlob(border, left, top, NebulaKind.Cloud,
                    0.0014 + idx * 0.0002, 0.0010 + idx * 0.00015, 16, 12));
            }

            for (var i = 0; i < 5; i++)
                AddCloud(i);

            for (var i = 0; i < 4; i++)
            {
                var w = 320 + _rng.Next(200);
                var h = 28 + _rng.Next(22);
                var brush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(0, 80, 120, 200), 0),
                        new GradientStop(Color.FromArgb(38, 120, 160, 255), 0.5),
                        new GradientStop(Color.FromArgb(0, 80, 120, 200), 1)
                    }
                };
                var border = new Border
                {
                    Width = w,
                    Height = h,
                    CornerRadius = new CornerRadius(h * 0.5),
                    Background = brush,
                    Opacity = 0.14 + _rng.NextDouble() * 0.1,
                    RenderTransform = new RotateTransform(_rng.Next(-35, 35))
                };
                var left = -w * 0.2 + _rng.NextDouble() * Bounds.Width;
                var top = _rng.NextDouble() * Bounds.Height;
                Canvas.SetLeft(border, left);
                Canvas.SetTop(border, top);
                canvas.Children.Add(border);
                _nebulaBlobs.Add(new NebulaBlob(border, left, top, NebulaKind.Ribbon,
                    0.0028, 0.0016, 22, 16));
            }

            for (var i = 0; i < 3; i++)
            {
                var turquoise = i % 2 == 0;
                var c = turquoise
                    ? Color.FromArgb(58, 45, 212, 196)
                    : Color.FromArgb(52, 160, 90, 255);
                var w = 140 + _rng.Next(100);
                var brush = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(c, 0),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                    }
                };
                var border = new Border
                {
                    Width = w,
                    Height = w,
                    CornerRadius = new CornerRadius(w * 0.5),
                    Background = brush,
                    Opacity = 0.18 + _rng.NextDouble() * 0.12
                };
                var left = _rng.NextDouble() * (Bounds.Width - w * 0.3);
                var top = _rng.NextDouble() * (Bounds.Height - w * 0.3);
                Canvas.SetLeft(border, left);
                Canvas.SetTop(border, top);
                canvas.Children.Add(border);
                _nebulaBlobs.Add(new NebulaBlob(border, left, top, NebulaKind.Accent,
                    0.0035, 0.0024, 20, 14));
            }
        }

        private void DestroyBlackHoles()
        {
            var cv = this.FindControl<Canvas>("BlackHoleCanvas");
            cv?.Children.Clear();
            _blackHoles.Clear();
            ResetOrbitalVelocities();
        }

        private static (double wx, double wy) BlackHoleCenterWindow(BlackHoleState bh) =>
            (bh.Cx - StarFieldPadding, bh.Cy - StarFieldPadding);

        private IEnumerable<BlackHoleState> ActiveBlackHoles() => _blackHoles;

        private BlackHoleState NearestBlackHoleNear(double x, double y)
        {
            // x,y в координатах StarsNearCanvas.
            var best = _blackHoles[0];
            var bestD = double.MaxValue;
            for (var i = 0; i < _blackHoles.Count; i++)
            {
                var bh = _blackHoles[i];
                var dx = bh.Cx - x;
                var dy = bh.Cy - y;
                var d = dx * dx + dy * dy;
                if (d < bestD)
                {
                    bestD = d;
                    best = bh;
                }
            }

            return best;
        }

        private BlackHoleState NearestBlackHoleWindow(double x, double y)
        {
            // x,y в координатах окна (MeteorCanvas и pointer trail).
            var best = _blackHoles[0];
            var bestD = double.MaxValue;
            for (var i = 0; i < _blackHoles.Count; i++)
            {
                var bh = _blackHoles[i];
                var (bhx, bhy) = BlackHoleCenterWindow(bh);
                var dx = bhx - x;
                var dy = bhy - y;
                var d = dx * dx + dy * dy;
                if (d < bestD)
                {
                    bestD = d;
                    best = bh;
                }
            }

            return best;
        }

        /// <summary>Попадание по ядру в координатах <see cref="BlackHoleCanvas"/> (совпадают с Cx/Cy), с учётом параллакса.</summary>
        private bool TryPickBlackHoleForPointer(PointerEventArgs e, out BlackHoleState bh)
        {
            var cv = this.FindControl<Canvas>("BlackHoleCanvas");
            if (cv == null || _blackHoles.Count == 0)
            {
                bh = null!;
                return false;
            }

            return TryPickBlackHoleAtBhCanvasPoint(e.GetPosition(cv), out bh);
        }

        private bool TryPickBlackHoleAtBhCanvasPoint(Point pBh, out BlackHoleState bh)
        {
            // Попадание по "чёрному диску" (ядру), чтобы drag был намеренным.
            bh = _blackHoles[0];
            var bestD = double.MaxValue;
            var hit = false;

            for (var i = 0; i < _blackHoles.Count; i++)
            {
                var b = _blackHoles[i];
                var dx = pBh.X - b.Cx;
                var dy = pBh.Y - b.Cy;
                var dSq = dx * dx + dy * dy;

                var coreR = BlackHoleBlackDiskOuterRadius(EffectiveHorizon(b));
                var r = Math.Max(coreR, BlackHolePickMinRadius + coreR * 0.25);
                if (dSq <= r * r && dSq < bestD)
                {
                    bestD = dSq;
                    bh = b;
                    hit = true;
                }
            }

            return hit;
        }

        /// <summary>Масштаб для гравитации, ореола и поглощения: не отстаёт от визуала при лерпе GrowthDisplay.</summary>
        private static double GrowthForPhysics(BlackHoleState bh) =>
            Math.Max(bh.GrowthDisplay, bh.GrowthTarget);

        private static double EffectiveHorizon(BlackHoleState bh) => bh.BaseHorizonR * GrowthForPhysics(bh);

        private static double EffectiveGravParameter(BlackHoleState bh) =>
            bh.BaseGravParameter * Math.Pow(GrowthForPhysics(bh), 2.08);

        /// <summary>Взаимная динамика дыр: только <see cref="BlackHoleState.GrowthTarget"/>.
        /// Лерп <see cref="BlackHoleState.GrowthDisplay"/> к таргету менял бы массу каждый кадр и разрушал бы орбиты.</summary>
        private static double GrowthForMutualPhysics(BlackHoleState bh) =>
            Math.Max(1e-9, bh.GrowthTarget);

        private static double EffectiveHorizonMutual(BlackHoleState bh) =>
            bh.BaseHorizonR * GrowthForMutualPhysics(bh);

        private static double EffectiveGravParameterMutual(BlackHoleState bh) =>
            bh.BaseGravParameter * Math.Pow(GrowthForMutualPhysics(bh), 2.08);

        private static void FeedBlackHole(BlackHoleState bh, double normalizedMassDelta)
        {
            bh.GrowthTarget += normalizedMassDelta;
        }

        /// <summary>Внешний радиус чёрного диска (совпадает с визуальным краем ядра).</summary>
        private static double BlackHoleBlackDiskOuterRadius(double effH) =>
            effH * (BlackHoleSolidCoreDiameterOverHorizon * 0.5);

        /// <summary>Точка или отрезок пересекает заполненный круг — поглощение без пропуска на большой скорости.</summary>
        private static bool SegmentIntersectsFilledDisk(
            double x0, double y0, double x1, double y1, double cx, double cy, double r)
        {
            var rSq = r * r;
            var ax = x0 - cx;
            var ay = y0 - cy;
            var bx = x1 - cx;
            var by = y1 - cy;
            if (ax * ax + ay * ay <= rSq || bx * bx + by * by <= rSq)
                return true;
            var dx = bx - ax;
            var dy = by - ay;
            var lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-18)
                return false;
            var t = Math.Clamp(-(ax * dx + ay * dy) / lenSq, 0, 1);
            var px = ax + t * dx;
            var py = ay + t * dy;
            return px * px + py * py <= rSq;
        }

        private static void RemoveMeteorInstance(MeteorInstance m)
        {
            foreach (var ln in m.Segments)
                m.Canvas.Children.Remove(ln);
        }

        private void TickMeteors(double dtSec)
        {
            var canvas = this.FindControl<Canvas>("MeteorCanvas");
            if (canvas == null || _meteors.Count == 0) return;

            var w = canvas.Width;
            var hWin = canvas.Height;
            const double escapeMargin = 48;
            var bhPresent = _blackHoles.Count > 0;

            for (var i = _meteors.Count - 1; i >= 0; i--)
            {
                var m = _meteors[i];
                var ox = m.HeadX;
                var oy = m.HeadY;
                var cs = m.CurveSign * 0.014;
                var c = Math.Cos(cs);
                var s = Math.Sin(cs);
                var nvx = m.Vx * c - m.Vy * s;
                var nvy = m.Vx * s + m.Vy * c;
                m.Vx = nvx;
                m.Vy = nvy;

                if (bhPresent)
                {
                    // Для метеоров берём ближайшую дыру (чтобы не получалась неестественная "прямая" тяга в суммарный вектор).
                    var bh = NearestBlackHoleWindow(m.HeadX, m.HeadY);
                    var (bhx, bhy) = BlackHoleCenterWindow(bh);
                    var rx = bhx - m.HeadX;
                    var ry = bhy - m.HeadY;
                    var h = EffectiveHorizon(bh);
                    var soft = h * 0.55;
                    var distSq = rx * rx + ry * ry + soft * soft;
                    var d = Math.Sqrt(distSq);
                    if (d > 1e-5)
                    {
                        var gm = EffectiveGravParameter(bh);
                        var inv = 1.0 / (d * distSq);
                        m.Vx += gm * rx * inv / MeteorGravMass * dtSec;
                        m.Vy += gm * ry * inv / MeteorGravMass * dtSec;
                    }
                }

                var drag = Math.Exp(-MeteorDragPerSecond * dtSec);
                m.Vx *= drag;
                m.Vy *= drag;

                m.HeadX += m.Vx * dtSec;
                m.HeadY += m.Vy * dtSec;
                m.AgeSec += dtSec;

                if (bhPresent)
                {
                    var bh = NearestBlackHoleWindow(m.HeadX, m.HeadY);
                    var (bhx, bhy) = BlackHoleCenterWindow(bh);
                    var h = EffectiveHorizon(bh);
                    var rBlack = BlackHoleBlackDiskOuterRadius(h);
                    if (SegmentIntersectsFilledDisk(ox, oy, m.HeadX, m.HeadY, bhx, bhy, rBlack))
                    {
                        FeedBlackHole(bh, BlackHoleMeteorFeedAmount);
                        RemoveMeteorInstance(m);
                        _meteors.RemoveAt(i);
                        continue;
                    }
                }

                if (m.HeadX < -escapeMargin || m.HeadX > w + escapeMargin ||
                    m.HeadY < -escapeMargin || m.HeadY > hWin + escapeMargin)
                {
                    RemoveMeteorInstance(m);
                    _meteors.RemoveAt(i);
                    continue;
                }

                for (var k = m.Hist.Length - 1; k > 0; k--)
                    m.Hist[k] = m.Hist[k - 1];
                m.Hist[0] = new Point(m.HeadX, m.HeadY);

                double fade;
                if (bhPresent)
                    fade = 1.0;
                else
                {
                    fade = m.AgeSec > 2.35 ? Math.Clamp(1 - (m.AgeSec - 2.35) / 1.25, 0, 1) : 1.0;
                    if (fade <= 0.02)
                    {
                        RemoveMeteorInstance(m);
                        _meteors.RemoveAt(i);
                        continue;
                    }
                }

                for (var j = 0; j < m.Segments.Length; j++)
                {
                    m.Segments[j].StartPoint = m.Hist[j + 1];
                    m.Segments[j].EndPoint = m.Hist[j];
                    var segOp = (0.2 + 0.16 * (m.Segments.Length - j)) * fade;
                    m.Segments[j].Opacity = Math.Clamp(segOp, 0, 1);
                    var alpha = (byte)(220 * fade);
                    m.Segments[j].Stroke = new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255));
                }
            }
        }

        private void ResetOrbitalVelocities()
        {
            foreach (var s in _starsFar)
            {
                s.OrbVx = 0;
                s.OrbVy = 0;
            }

            foreach (var s in _starsNear)
            {
                s.OrbVx = 0;
                s.OrbVy = 0;
            }

            foreach (var s in _dust)
            {
                s.OrbVx = 0;
                s.OrbVy = 0;
            }

            foreach (var fl in _flares)
            {
                fl.OrbVx = 0;
                fl.OrbVy = 0;
            }
        }

        private void TrySpawnBlackHole()
        {
            if (_blackHoles.Count >= MaxBlackHoles) return;

            var bhCanvas = this.FindControl<Canvas>("BlackHoleCanvas");
            var near = this.FindControl<Canvas>("StarsNearCanvas");
            if (bhCanvas == null || near == null || near.Width <= 40 || near.Height <= 40) return;

            var cw = near.Width;
            var ch = near.Height;
            var minDim = Math.Min(cw, ch);
            var margin = minDim * 0.14;
            var cx = margin + _rng.NextDouble() * (cw - 2 * margin);
            var cy = margin + _rng.NextDouble() * (ch - 2 * margin);

            TrySpawnBlackHoleAt(new Avalonia.Point(cx, cy), BlackHoleSpawnScaleKind.Random);
        }

        private void TrySpawnBlackHoleAt(Avalonia.Point posNear, BlackHoleSpawnScaleKind scaleKind)
        {
            if (_blackHoles.Count >= MaxBlackHoles) return;

            var bhCanvas = this.FindControl<Canvas>("BlackHoleCanvas");
            var near = this.FindControl<Canvas>("StarsNearCanvas");
            if (bhCanvas == null || near == null || near.Width <= 40 || near.Height <= 40) return;

            var cw = near.Width;
            var ch = near.Height;
            var minDim = Math.Min(cw, ch);

            var scale = scaleKind switch
            {
                BlackHoleSpawnScaleKind.Random => SampleBlackHoleSpawnScale(),
                BlackHoleSpawnScaleKind.Full => 1.0,
                BlackHoleSpawnScaleKind.FixedMedium => BlackHoleEasterRightHoldScale,
                _ => 1.0
            };
            var distortR = minDim * 0.248 * scale;
            var horizonR = minDim * 0.088 * scale;

            var margin = minDim * 0.14 + distortR * 0.85;
            var cx = Math.Clamp(posNear.X, margin, cw - margin);
            var cy = Math.Clamp(posNear.Y, margin, ch - margin);

            var root = new Canvas
            {
                Width = distortR * 2,
                Height = distortR * 2,
                IsHitTestVisible = false
            };
            var lx = distortR;
            var ly = distortR;

            void AddRing(Border b, double diameter)
            {
                b.Width = b.Height = diameter;
                b.CornerRadius = new CornerRadius(diameter * 0.5);
                Canvas.SetLeft(b, lx - diameter * 0.5);
                Canvas.SetTop(b, ly - diameter * 0.5);
                root.Children.Add(b);
            }

            var outerBrush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 40, 60, 120), 0),
                    new GradientStop(Color.FromArgb(32, 120, 170, 255), 0.2),
                    new GradientStop(Color.FromArgb(0, 30, 50, 90), 0.34),
                    new GradientStop(Color.FromArgb(48, 220, 240, 255), 0.46),
                    new GradientStop(Color.FromArgb(72, 100, 140, 220), 0.54),
                    new GradientStop(Color.FromArgb(0, 20, 30, 60), 0.66),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                }
            };
            var outer = new Border
            {
                Background = outerBrush,
                Opacity = 0.88,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                IsHitTestVisible = false
            };
            AddRing(outer, distortR * 2.12);

            var einBrush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(0, 200, 230, 255), 0.52),
                    new GradientStop(Color.FromArgb(200, 255, 255, 255), 0.72),
                    new GradientStop(Color.FromArgb(0, 180, 210, 255), 0.81),
                    new GradientStop(Color.FromArgb(0, 40, 60, 100), 0.94),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                }
            };
            var einstein = new Border
            {
                Background = einBrush,
                Opacity = 0.86,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                IsHitTestVisible = false
            };
            AddRing(einstein, horizonR * 2.55);

            var shellBrush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(255, 2, 3, 12), 0),
                    new GradientStop(Color.FromArgb(255, 4, 6, 18), 0.38),
                    new GradientStop(Color.FromArgb(200, 8, 10, 26), 0.48),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.58),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                }
            };
            var shell = new Border
            {
                Background = shellBrush,
                Opacity = 0.97,
                IsHitTestVisible = false
            };
            AddRing(shell, horizonR * 2.08);

            var core = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 2)),
                Opacity = 1,
                IsHitTestVisible = false
            };
            AddRing(core, horizonR * BlackHoleSolidCoreDiameterOverHorizon);

            Canvas.SetLeft(root, cx - distortR);
            Canvas.SetTop(root, cy - distortR);
            bhCanvas.Children.Add(root);

            var gravParameter = minDim * minDim * 0.065 * (scale * scale);

            var bh = new BlackHoleState
            {
                Root = root,
                Cx = cx,
                Cy = cy,
                BaseHorizonR = horizonR,
                BaseDistortR = distortR,
                BaseGravParameter = gravParameter,
                GrowthDisplay = 1,
                GrowthTarget = 1,
                TimeMs = 0,
                EinsteinRing = einstein,
                OuterDistortion = outer,
                SpinAngle = _rng.NextDouble() * 360,
                Vx = 0,
                Vy = 0
            };
            root.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            root.RenderTransform = new ScaleTransform(1, 1);
            einstein.RenderTransform = new RotateTransform(bh.SpinAngle);
            outer.RenderTransform = new RotateTransform(-bh.SpinAngle * 0.28);

            _blackHoles.Add(bh);

            // Небольшая "раскрутка" звёзд вокруг новой дыры, чтобы не было мгновенного падения.
            SeedBlackHoleOrbits(gravParameter, horizonR, cx, cy);
        }

        private double SampleBlackHoleSpawnScale()
        {
            // Смещаем распределение в сторону малых дыр, но иногда разрешаем крупные.
            var u = _rng.NextDouble();
            var biased = Math.Pow(u, 2.35);
            return BlackHoleSpawnScaleMin + (1.0 - BlackHoleSpawnScaleMin) * biased;
        }

        private void SeedBlackHoleOrbits(double gm, double horizonR, double cx, double cy)
        {
            void SeedStar(StarEntry s)
            {
                var sx = Canvas.GetLeft(s.Ellipse) + s.Ellipse.Width * 0.5;
                var sy = Canvas.GetTop(s.Ellipse) + s.Ellipse.Height * 0.5;
                var px = sx - cx;
                var py = sy - cy;
                var dist = Math.Sqrt(px * px + py * py);
                if (dist < horizonR * 2.2)
                {
                    s.OrbVx = 0;
                    s.OrbVy = 0;
                    return;
                }

                var rEff = Math.Max(dist, horizonR * 2.8);
                var vCirc = Math.Sqrt(gm / rEff) * (0.82 + _rng.NextDouble() * 0.12);
                var tx = -py / dist;
                var ty = px / dist;
                s.OrbVx = tx * vCirc;
                s.OrbVy = ty * vCirc;
            }

            foreach (var s in _starsFar)
                SeedStar(s);
            foreach (var s in _starsNear)
                SeedStar(s);
            foreach (var s in _dust)
                SeedStar(s);

            const double hostSize = 36;
            var hc = hostSize * 0.5;
            foreach (var fl in _flares)
            {
                var sx = Canvas.GetLeft(fl.Host) + hc;
                var sy = Canvas.GetTop(fl.Host) + hc;
                var px = sx - cx;
                var py = sy - cy;
                var dist = Math.Sqrt(px * px + py * py);
                if (dist < horizonR * 2.2)
                {
                    fl.OrbVx = 0;
                    fl.OrbVy = 0;
                    continue;
                }

                var rEff = Math.Max(dist, horizonR * 2.8);
                var vCirc = Math.Sqrt(gm / rEff) * (0.84 + _rng.NextDouble() * 0.1);
                fl.OrbVx = (-py / dist) * vCirc;
                fl.OrbVy = (px / dist) * vCirc;
            }
        }

        private static double StarInertialMass(StarEntry s)
        {
            var a = Math.Max(0.8, s.Ellipse.Width * s.Ellipse.Height);
            return 0.72 + Math.Pow(a, 1.15) * 0.11;
        }

        private void TickBlackHoles(double dt)
        {
            // Рандом-спавн продолжает работать, даже если есть дыры (до MaxBlackHoles).
            _nextBlackHoleRollMs -= dt;
            if (_nextBlackHoleRollMs <= 0)
            {
                _nextBlackHoleRollMs = 2200 + _rng.NextDouble() * 1800;
                if (_blackHoles.Count < MaxBlackHoles && _rng.NextDouble() < BlackHoleSpawnChance)
                    TrySpawnBlackHole();
            }

            if (_blackHoles.Count == 0)
                return;

            var dtSec = dt / 1000.0;
            TickBlackHoleBodies(dtSec);

            // Визуал/рост/спин для каждой дыры.
            for (var i = 0; i < _blackHoles.Count; i++)
                TickBlackHoleVisual(_blackHoles[i], dt);

            // Звёзды/пыль/флееры закручиваются вокруг дыр (сумма полей), как раньше без "мгновенного падения".
            ApplyBlackHolesGravityToStars(dtSec);
        }

        private void TickBlackHoleVisual(BlackHoleState? bh, double dt)
        {
            if (bh == null) return;

            bh.TimeMs += dt;
            var gd = bh.GrowthDisplay;
            gd += (bh.GrowthTarget - gd) * 0.074;
            bh.GrowthDisplay = gd;
            bh.Root.RenderTransform = new ScaleTransform(gd, gd);

            var spin = 0.31;
            bh.SpinAngle += spin;
            bh.EinsteinRing.RenderTransform = new RotateTransform(bh.SpinAngle);
            if (bh.OuterDistortion != null)
                bh.OuterDistortion.RenderTransform = new RotateTransform(-bh.SpinAngle * 0.26);
        }

        /// <summary>Множитель на компоненты ускорения ∝ r̂/(r²+ε²)^{p/2}; p=3 соответствует 1/r².</summary>
        private static double BlackHoleMutualInvRPow(double r2) =>
            Math.Pow(r2, -0.5 * BlackHoleMutualDistancePower);

        private static void ApplyBlackHolePairCircStabilization(
            ref double arx,
            ref double ary,
            double rx,
            double ry,
            double rvx,
            double rvy)
        {
            var rg2 = rx * rx + ry * ry;
            if (rg2 < 4) return;

            var rg = Math.Sqrt(rg2);
            var blend = Math.Clamp(1.0 - rg / BlackHolePairCircBlendFarPx, 0.0, 1.0);
            if (blend < 0.02) return;

            var utx = -ry / rg;
            var uty = rx / rg;
            var vTan = rvx * utx + rvy * uty;
            var aMag = Math.Sqrt(arx * arx + ary * ary);
            if (aMag < 1e-8) return;

            var vCirc = Math.Sqrt(Math.Max(0, aMag * rg));
            var hAng = rx * rvy - ry * rvx;
            var sign = hAng >= 0 ? 1.0 : -1.0;
            if (Math.Abs(hAng) < 1e-4 && Math.Abs(vTan) < 1e-4)
                sign = 1.0;

            var vTgt = sign * vCirc;
            var corr = BlackHolePairCircStabilizePerSec * (vTgt - vTan) * blend;
            var cap = 900.0 * blend;
            if (corr > cap) corr = cap;
            if (corr < -cap) corr = -cap;

            arx += corr * utx;
            ary += corr * uty;
        }

        private void AccumulateBlackHoleMutualAccelerations(Span<double> ax, Span<double> ay)
        {
            for (var i = 0; i < _blackHoles.Count; i++)
            {
                ax[i] = 0;
                ay[i] = 0;
            }

            for (var i = 0; i < _blackHoles.Count; i++)
            for (var j = i + 1; j < _blackHoles.Count; j++)
            {
                var a = _blackHoles[i];
                var b = _blackHoles[j];
                var dx = b.Cx - a.Cx;
                var dy = b.Cy - a.Cy;

                var ha = EffectiveHorizonMutual(a);
                var hb = EffectiveHorizonMutual(b);
                var eps = Math.Max(
                    BlackHolePairSofteningFloorPx,
                    BlackHolePairSofteningOverMinHorizon * Math.Min(ha, hb));

                var r2 = dx * dx + dy * dy + eps * eps;
                var invPow = BlackHoleMutualInvRPow(r2);

                var muA = EffectiveGravParameterMutual(a);
                var muB = EffectiveGravParameterMutual(b);
                var k = BlackHoleMutualAttractionK;

                ax[i] += k * muB * dx * invPow;
                ay[i] += k * muB * dy * invPow;
                ax[j] -= k * muA * dx * invPow;
                ay[j] -= k * muA * dy * invPow;
            }
        }

        /// <summary>
        /// Ровно две дыры: интегрируем относительный вектор <c>r = r₂ − r₁</c> как одну задачу
        /// <c>d²r/dt² = −K·(μ₁+μ₂)·r / (r²+ε²)^{3/2}</c>, затем восстанавливаем позиции вокруг центра масс.
        /// Сила ∝ r/(r²+ε²)^{p/2} с p &lt; 3 чуть сильнее на дальности; при сближении — лёгкая подстройка к круговой скорости.
        /// </summary>
        private void IntegrateBlackHolePairAboutCom(double dtSec, double cw, double ch)
        {
            var bh0 = _blackHoles[0];
            var bh1 = _blackHoles[1];
            var mu0 = EffectiveGravParameterMutual(bh0);
            var mu1 = EffectiveGravParameterMutual(bh1);
            var mSum = mu0 + mu1;
            if (mSum < 1e-18)
                return;

            var invM = 1.0 / mSum;
            var k = BlackHoleMutualAttractionK;

            var rcx = (mu0 * bh0.Cx + mu1 * bh1.Cx) * invM;
            var rcy = (mu0 * bh0.Cy + mu1 * bh1.Cy) * invM;
            var vcx = (mu0 * bh0.Vx + mu1 * bh1.Vx) * invM;
            var vcy = (mu0 * bh0.Vy + mu1 * bh1.Vy) * invM;

            var rx = bh1.Cx - bh0.Cx;
            var ry = bh1.Cy - bh0.Cy;
            var rvx = bh1.Vx - bh0.Vx;
            var rvy = bh1.Vy - bh0.Vy;

            var h0m = EffectiveHorizonMutual(bh0);
            var h1m = EffectiveHorizonMutual(bh1);
            var eps = Math.Max(
                BlackHoleTwoBodySofteningFloorPx,
                BlackHoleTwoBodySofteningOverMinHorizon * Math.Min(h0m, h1m));

            var subDt = Math.Clamp(dtSec, 0, 0.06);
            var d = Math.Sqrt(rx * rx + ry * ry);
            var minPairDist = d < 1e-9 ? 220 : d;
            var vr = Math.Sqrt(rvx * rvx + rvy * rvy);
            var v0 = Math.Sqrt(bh0.Vx * bh0.Vx + bh0.Vy * bh0.Vy);
            var v1 = Math.Sqrt(bh1.Vx * bh1.Vx + bh1.Vy * bh1.Vy);
            var maxSpeed = Math.Max(Math.Max(v0, v1), vr);
            var vScale = Math.Max(maxSpeed, vr * 0.88);
            var hSuggest = BlackHolePairCourantFrac * minPairDist /
                           Math.Max(vScale, BlackHolePairCourantMinSpeed);
            hSuggest = Math.Clamp(hSuggest, 0.00035, 0.0020);
            var steps = (int)Math.Ceiling(subDt / hSuggest);
            steps = Math.Clamp(steps, 56, 300);
            var h = subDt / steps;

            for (var s = 0; s < steps; s++)
            {
                var r2 = rx * rx + ry * ry + eps * eps;
                var invPow = BlackHoleMutualInvRPow(r2);
                var arx = -k * mSum * rx * invPow;
                var ary = -k * mSum * ry * invPow;
                ApplyBlackHolePairCircStabilization(ref arx, ref ary, rx, ry, rvx, rvy);

                var aMag = Math.Sqrt(arx * arx + ary * ary);
                if (aMag > BlackHoleAccelCap)
                {
                    var kk = BlackHoleAccelCap / aMag;
                    arx *= kk;
                    ary *= kk;
                }

                rvx += arx * (h * 0.5);
                rvy += ary * (h * 0.5);
                rx += rvx * h;
                ry += rvy * h;
                rcx += vcx * h;
                rcy += vcy * h;

                r2 = rx * rx + ry * ry + eps * eps;
                invPow = BlackHoleMutualInvRPow(r2);
                arx = -k * mSum * rx * invPow;
                ary = -k * mSum * ry * invPow;
                ApplyBlackHolePairCircStabilization(ref arx, ref ary, rx, ry, rvx, rvy);
                aMag = Math.Sqrt(arx * arx + ary * ary);
                if (aMag > BlackHoleAccelCap)
                {
                    var kk = BlackHoleAccelCap / aMag;
                    arx *= kk;
                    ary *= kk;
                }

                rvx += arx * (h * 0.5);
                rvy += ary * (h * 0.5);
            }

            bh0.Cx = rcx - mu1 * invM * rx;
            bh0.Cy = rcy - mu1 * invM * ry;
            bh1.Cx = rcx + mu0 * invM * rx;
            bh1.Cy = rcy + mu0 * invM * ry;
            bh0.Vx = vcx - mu1 * invM * rvx;
            bh0.Vy = vcy - mu1 * invM * rvy;
            bh1.Vx = vcx + mu0 * invM * rvx;
            bh1.Vy = vcy + mu0 * invM * rvy;

            var vCap = Math.Sqrt(bh0.Vx * bh0.Vx + bh0.Vy * bh0.Vy);
            if (vCap > BlackHoleVelCap)
            {
                var kk = BlackHoleVelCap / vCap;
                bh0.Vx *= kk;
                bh0.Vy *= kk;
            }

            vCap = Math.Sqrt(bh1.Vx * bh1.Vx + bh1.Vy * bh1.Vy);
            if (vCap > BlackHoleVelCap)
            {
                var kk = BlackHoleVelCap / vCap;
                bh1.Vx *= kk;
                bh1.Vy *= kk;
            }

            ConstrainBlackHoleToBounds(bh0, cw, ch);
            ConstrainBlackHoleToBounds(bh1, cw, ch);
        }

        private void DampBlackHolesComVelocity(double factor)
        {
            if (_blackHoles.Count < 2 || factor <= 0) return;

            double mSum = 0;
            double px = 0;
            double py = 0;
            for (var i = 0; i < _blackHoles.Count; i++)
            {
                var bh = _blackHoles[i];
                var m = EffectiveGravParameterMutual(bh);
                mSum += m;
                px += m * bh.Vx;
                py += m * bh.Vy;
            }

            if (mSum < 1e-18) return;

            var vcx = px / mSum;
            var vcy = py / mSum;
            for (var i = 0; i < _blackHoles.Count; i++)
            {
                var bh = _blackHoles[i];
                bh.Vx -= factor * vcx;
                bh.Vy -= factor * vcy;
            }
        }

        private void TickBlackHoleBodies(double dtSec)
        {
            // N-body: дыры притягивают друг друга и вращаются вокруг центра масс.
            var near = this.FindControl<Canvas>("StarsNearCanvas");
            if (near == null || near.Width <= 40 || near.Height <= 40) return;

            var cw = near.Width;
            var ch = near.Height;

            // Если дыра одна — она не ускоряется (нет других дыр). Скорость сохраняется (без демпфирования),
            // меняется только от пользовательских импульсов и ограничений по границам.
            if (_blackHoles.Count == 1)
            {
                var bh = _blackHoles[0];
                bh.Cx += bh.Vx * dtSec;
                bh.Cy += bh.Vy * dtSec;
                ConstrainBlackHoleToBounds(bh, cw, ch);
                return;
            }

            // Две дыры: приведённые координаты + явный центр масс (см. IntegrateBlackHolePairAboutCom).
            // Три и больше: классический leapfrog по парам + лёгкое гашение дрейфа импульса центра масс.
            if (_blackHoles.Count == 2)
            {
                IntegrateBlackHolePairAboutCom(dtSec, cw, ch);
            }
            else
            {
                var subDt = Math.Clamp(dtSec, 0, 0.06);

                double minPairDist = double.MaxValue;
                double maxSpeed = 0;
                double maxRelSpeed = 0;
                for (var i = 0; i < _blackHoles.Count; i++)
                {
                    var bh = _blackHoles[i];
                    var vm = Math.Sqrt(bh.Vx * bh.Vx + bh.Vy * bh.Vy);
                    if (vm > maxSpeed) maxSpeed = vm;
                }

                for (var i = 0; i < _blackHoles.Count; i++)
                for (var j = i + 1; j < _blackHoles.Count; j++)
                {
                    var a = _blackHoles[i];
                    var b = _blackHoles[j];
                    var dx = b.Cx - a.Cx;
                    var dy = b.Cy - a.Cy;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < minPairDist) minPairDist = d;

                    var rvx = a.Vx - b.Vx;
                    var rvy = a.Vy - b.Vy;
                    var vr = Math.Sqrt(rvx * rvx + rvy * rvy);
                    if (vr > maxRelSpeed) maxRelSpeed = vr;
                }

                if (minPairDist > 1e8 || minPairDist < 1e-9)
                    minPairDist = 220;

                var vScale = Math.Max(maxSpeed, maxRelSpeed * 0.85);
                var hSuggest = BlackHolePairCourantFrac * minPairDist /
                               Math.Max(vScale, BlackHolePairCourantMinSpeed);
                hSuggest = Math.Clamp(hSuggest, 0.00038, 0.0020);
                var steps = (int)Math.Ceiling(subDt / hSuggest);
                steps = Math.Clamp(steps, 48, 280);
                var h = subDt / steps;

                Span<double> ax = stackalloc double[_blackHoles.Count];
                Span<double> ay = stackalloc double[_blackHoles.Count];

                for (var s = 0; s < steps; s++)
                {
                    AccumulateBlackHoleMutualAccelerations(ax, ay);

                    for (var i = 0; i < _blackHoles.Count; i++)
                    {
                        var aMag = Math.Sqrt(ax[i] * ax[i] + ay[i] * ay[i]);
                        if (aMag > BlackHoleAccelCap)
                        {
                            var kk = BlackHoleAccelCap / aMag;
                            ax[i] *= kk;
                            ay[i] *= kk;
                        }
                    }

                    for (var i = 0; i < _blackHoles.Count; i++)
                    {
                        var bh = _blackHoles[i];
                        bh.Vx += ax[i] * (h * 0.5);
                        bh.Vy += ay[i] * (h * 0.5);
                    }

                    for (var i = 0; i < _blackHoles.Count; i++)
                    {
                        var bh = _blackHoles[i];
                        bh.Cx += bh.Vx * h;
                        bh.Cy += bh.Vy * h;
                    }

                    AccumulateBlackHoleMutualAccelerations(ax, ay);

                    for (var i = 0; i < _blackHoles.Count; i++)
                    {
                        var aMag = Math.Sqrt(ax[i] * ax[i] + ay[i] * ay[i]);
                        if (aMag > BlackHoleAccelCap)
                        {
                            var kk = BlackHoleAccelCap / aMag;
                            ax[i] *= kk;
                            ay[i] *= kk;
                        }
                    }

                    for (var i = 0; i < _blackHoles.Count; i++)
                    {
                        var bh = _blackHoles[i];
                        bh.Vx += ax[i] * (h * 0.5);
                        bh.Vy += ay[i] * (h * 0.5);

                        var v = Math.Sqrt(bh.Vx * bh.Vx + bh.Vy * bh.Vy);
                        if (v > BlackHoleVelCap)
                        {
                            var kk = BlackHoleVelCap / v;
                            bh.Vx *= kk;
                            bh.Vy *= kk;
                        }

                        ConstrainBlackHoleToBounds(bh, cw, ch);
                    }
                }

                DampBlackHolesComVelocity(BlackHoleManyBodyComVelocityDamp);
            }

            // Слияние: когда горизонты событий соприкасаются (или чуть глубже).
            if (_blackHoles.Count > 1)
            {
                for (var i = _blackHoles.Count - 1; i >= 0; i--)
                for (var j = i - 1; j >= 0; j--)
                {
                    var a = _blackHoles[i];
                    var b = _blackHoles[j];
                    var dx = b.Cx - a.Cx;
                    var dy = b.Cy - a.Cy;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    var hA = EffectiveHorizon(a);
                    var hB = EffectiveHorizon(b);
                    if (d > hA + hB)
                        continue;

                    // Сливаем в большую: импульс сохраняем, масса и радиусы заметно растут.
                    var big = hA >= hB ? a : b;
                    var small = ReferenceEquals(big, a) ? b : a;

                    // Точка соприкосновения горизонтов (в coords StarsNearCanvas).
                    var ux = dx / Math.Max(1e-6, d);
                    var uy = dy / Math.Max(1e-6, d);
                    var contactCx = big.Cx + ux * hA;
                    var contactCy = big.Cy + uy * hA;

                    var mBig = Math.Max(1e-6, big.BaseGravParameter);
                    var mSmall = Math.Max(1e-6, small.BaseGravParameter);
                    var mNew = mBig + mSmall;

                    // Импульс (центр масс) — чтобы не было "телепорта" скорости.
                    big.Vx = (big.Vx * mBig + small.Vx * mSmall) / mNew;
                    big.Vy = (big.Vy * mBig + small.Vy * mSmall) / mNew;

                    // Радиусы и гравпараметр — сильно больше (как просили).
                    var scaleUp = Math.Sqrt(mNew / mBig);
                    big.BaseHorizonR *= scaleUp;
                    big.BaseDistortR *= scaleUp;
                    big.BaseGravParameter = mNew;

                    // Визуальный рост через GrowthTarget, чтобы было заметно.
                    big.GrowthTarget += 0.75 + 0.45 * Math.Min(2.0, small.GrowthTarget);

                    // Эпичный, но не кринжовый эффект: "гравитационная волна" + мягкая вспышка.
                    var wx = contactCx - StarFieldPadding;
                    var wy = contactCy - StarFieldPadding;
                    SpawnBlackHoleMergeFx(wx, wy);
                    PushStarsFromShock(contactCx, contactCy);

                    if (small.Root.Parent is Canvas cnv)
                        cnv.Children.Remove(small.Root);
                    _blackHoles.Remove(small);
                    ResetOrbitalVelocities();
                    break;
                }
            }
        }

        private void ConstrainBlackHoleToBounds(BlackHoleState bh, double cw, double ch)
        {
            // Ограничиваем область (мягко, с отскоком). Координаты: StarsNearCanvas.
            var r = bh.BaseDistortR * GrowthForPhysics(bh);
            var margin = StarFieldPadding + r * 0.35;
            if (bh.Cx < margin) { bh.Cx = margin; bh.Vx = Math.Abs(bh.Vx) * BlackHoleBounceDamping; }
            if (bh.Cy < margin) { bh.Cy = margin; bh.Vy = Math.Abs(bh.Vy) * BlackHoleBounceDamping; }
            if (bh.Cx > cw - margin) { bh.Cx = cw - margin; bh.Vx = -Math.Abs(bh.Vx) * BlackHoleBounceDamping; }
            if (bh.Cy > ch - margin) { bh.Cy = ch - margin; bh.Vy = -Math.Abs(bh.Vy) * BlackHoleBounceDamping; }

            Canvas.SetLeft(bh.Root, bh.Cx - bh.BaseDistortR);
            Canvas.SetTop(bh.Root, bh.Cy - bh.BaseDistortR);
        }

        private void SpawnBlackHoleMergeFx(double x, double y)
        {
            var fx = this.FindControl<Canvas>("BlackHoleMergeFxCanvas");
            if (fx == null) return;

            var w = Math.Max(1, Bounds.Width);
            var h = Math.Max(1, Bounds.Height);

            // 0) Краткое затемнение "космоса" (атмосфера/звёзды) синхронно.
            var dim = new Border
            {
                Width = w,
                Height = h,
                IsHitTestVisible = false,
                Opacity = 0.0,
                Background = new SolidColorBrush(Color.FromArgb(210, 0, 0, 0))
            };
            Canvas.SetLeft(dim, 0);
            Canvas.SetTop(dim, 0);
            fx.Children.Add(dim);

            // 1) Мягкая радиальная вспышка на весь экран.
            var flash = new Border
            {
                Width = w,
                Height = h,
                IsHitTestVisible = false,
                Opacity = 0.0,
                Background = new RadialGradientBrush
                {
                    Center = new RelativePoint(x / w, y / h, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(x / w, y / h, RelativeUnit.Relative),
                    Radius = 1.65,
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
                        new GradientStop(Color.FromArgb(65, 185, 220, 255), 0.18),
                        new GradientStop(Color.FromArgb(38, 110, 175, 255), 0.40),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.70),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                    }
                }
            };
            Canvas.SetLeft(flash, 0);
            Canvas.SetTop(flash, 0);
            fx.Children.Add(flash);

            // 2) Расширяющееся кольцо — "гравитационная волна".
            var ring = new Ellipse
            {
                Width = 180,
                Height = 180,
                StrokeThickness = 2.5,
                Stroke = new SolidColorBrush(Color.FromArgb(150, 200, 235, 255)),
                IsHitTestVisible = false,
                Opacity = 0.0,
                Effect = new BlurEffect { Radius = 12 },
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RenderTransform = new ScaleTransform(0.35, 0.35)
            };
            Canvas.SetLeft(ring, x - ring.Width * 0.5);
            Canvas.SetTop(ring, y - ring.Height * 0.5);
            fx.Children.Add(ring);

            _ = AnimateMergeFxAsync(fx, dim, flash, ring);
        }

        private async Task AnimateMergeFxAsync(Canvas fx, Border dim, Border flash, Ellipse ring)
        {
            try
            {
                // Инертно: плавный подъём, медленное расширение кольца, длинный хвост.
                dim.Opacity = 0.0;
                flash.Opacity = 0.0;
                ring.Opacity = 0.0;

                const int steps = 80;
                for (var i = 0; i <= steps; i++)
                {
                    var t = i / (double)steps;
                    var ease = 1 - Math.Pow(1 - t, 2.2);
                    var s = 0.35 + 7.0 * ease;
                    if (ring.RenderTransform is ScaleTransform st)
                    {
                        st.ScaleX = s;
                        st.ScaleY = s;
                    }

                    // затемнение быстро поднимается и держится чуть-чуть, затем уходит
                    var dimPulse = t < 0.22 ? (t / 0.22) : (t < 0.48 ? 1.0 : Math.Clamp(1 - (t - 0.48) / 0.52, 0, 1));
                    dim.Opacity = 0.20 * dimPulse;

                    // вспышка: мягкая, длинный хвост
                    flash.Opacity = 0.46 * (t < 0.18 ? (t / 0.18) : Math.Pow(1 - t, 1.05));

                    // кольцо: медленно и эпично
                    ring.Opacity = 0.70 * Math.Pow(1 - t, 0.9);

                    await Task.Delay(16);
                }
            }
            finally
            {
                fx.Children.Remove(ring);
                fx.Children.Remove(flash);
                fx.Children.Remove(dim);
            }
        }

        private void PushStarsFromShock(double cxNear, double cyNear)
        {
            // "ударная волна" расталкивает звёзды — мягко и в больших масштабах.
            const double baseKick = 190.0; // px/s импульс на старте, дальше по спаданию
            const double radius = 720.0;

            void KickList(List<StarEntry> list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    var sx = Canvas.GetLeft(s.Ellipse) + s.Ellipse.Width * 0.5;
                    var sy = Canvas.GetTop(s.Ellipse) + s.Ellipse.Height * 0.5;
                    var dx = sx - cxNear;
                    var dy = sy - cyNear;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < 1e-4 || d > radius) continue;
                    var fall = Math.Pow(1 - d / radius, 1.15);
                    var kick = baseKick * fall;
                    s.OrbVx += (dx / d) * kick;
                    s.OrbVy += (dy / d) * kick;
                }
            }

            KickList(_starsFar);
            KickList(_starsNear);
            KickList(_dust);

            const double hostSize = 36;
            var hc = hostSize * 0.5;
            for (var i = 0; i < _flares.Count; i++)
            {
                var fl = _flares[i];
                var sx = Canvas.GetLeft(fl.Host) + hc;
                var sy = Canvas.GetTop(fl.Host) + hc;
                var dx = sx - cxNear;
                var dy = sy - cyNear;
                var d = Math.Sqrt(dx * dx + dy * dy);
                if (d < 1e-4 || d > radius) continue;
                var fall = Math.Pow(1 - d / radius, 1.10);
                var kick = baseKick * 0.70 * fall;
                fl.OrbVx += (dx / d) * kick;
                fl.OrbVy += (dy / d) * kick;
            }
        }

        private void ApplyBlackHolesGravityToStars(double dtSec)
        {
            const double vCap = 420;
            if (_blackHoles.Count == 0) return;

            void IntegrateList(List<StarEntry> list)
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var s = list[i];
                    var sx = Canvas.GetLeft(s.Ellipse) + s.Ellipse.Width * 0.5;
                    var sy = Canvas.GetTop(s.Ellipse) + s.Ellipse.Height * 0.5;
                    var sx0 = sx;
                    var sy0 = sy;
                    sx += s.OrbVx * dtSec;
                    sy += s.OrbVy * dtSec;

                    // Поглощение: если пересёк ядро любой дыры — кормим именно её.
                    var absorbed = false;
                    for (var k = 0; k < _blackHoles.Count; k++)
                    {
                        var bh = _blackHoles[k];
                        var rBlack = BlackHoleBlackDiskOuterRadius(EffectiveHorizon(bh));
                        if (!SegmentIntersectsFilledDisk(sx0, sy0, sx, sy, bh.Cx, bh.Cy, rBlack))
                            continue;
                        FeedBlackHole(bh, StarInertialMass(s) * BlackHoleStarFeedK);
                        if (s.Ellipse.Parent is Canvas cnv)
                            cnv.Children.Remove(s.Ellipse);
                        if (_signalStar == s)
                            _signalStar = null;
                        list.RemoveAt(i);
                        absorbed = true;
                        break;
                    }
                    if (absorbed)
                        continue;

                    var mass = StarInertialMass(s);
                    var ax = 0.0;
                    var ay = 0.0;
                    for (var k = 0; k < _blackHoles.Count; k++)
                    {
                        var bh = _blackHoles[k];
                        var gm = EffectiveGravParameter(bh);
                        var effH = EffectiveHorizon(bh);
                        var soft = effH * 0.55;
                        var srx = bh.Cx - sx;
                        var sry = bh.Cy - sy;
                        var sdistSq = srx * srx + sry * sry + soft * soft;
                        var sdist = Math.Sqrt(sdistSq);
                        if (sdist < 1e-5) sdist = 1e-5;
                        var invDistCubed2 = 1.0 / (sdist * sdistSq);
                        ax += gm * srx * invDistCubed2 / mass;
                        ay += gm * sry * invDistCubed2 / mass;
                    }

                    s.OrbVx += ax * dtSec;
                    s.OrbVy += ay * dtSec;

                    var spd = Math.Sqrt(s.OrbVx * s.OrbVx + s.OrbVy * s.OrbVy);
                    if (spd > vCap)
                    {
                        var k = vCap / spd;
                        s.OrbVx *= k;
                        s.OrbVy *= k;
                    }

                    Canvas.SetLeft(s.Ellipse, sx - s.Ellipse.Width * 0.5);
                    Canvas.SetTop(s.Ellipse, sy - s.Ellipse.Height * 0.5);
                    s.Ellipse.RenderTransform = null;
                }
            }

            IntegrateList(_starsFar);
            IntegrateList(_starsNear);
            IntegrateList(_dust);

            const double hostSize = 36;
            var hc = hostSize * 0.5;
            const double flareMass = 14.5;
            for (var i = _flares.Count - 1; i >= 0; i--)
            {
                var fl = _flares[i];
                var sx = Canvas.GetLeft(fl.Host) + hc;
                var sy = Canvas.GetTop(fl.Host) + hc;
                var fsx0 = sx;
                var fsy0 = sy;
                sx += fl.OrbVx * dtSec;
                sy += fl.OrbVy * dtSec;

                var absorbed = false;
                for (var k = 0; k < _blackHoles.Count; k++)
                {
                    var bh = _blackHoles[k];
                    var flareRBlack = BlackHoleBlackDiskOuterRadius(EffectiveHorizon(bh));
                    if (!SegmentIntersectsFilledDisk(fsx0, fsy0, sx, sy, bh.Cx, bh.Cy, flareRBlack))
                        continue;
                    FeedBlackHole(bh, 0.054);
                    if (fl.Host.Parent is Canvas cnv)
                        cnv.Children.Remove(fl.Host);
                    _flares.RemoveAt(i);
                    absorbed = true;
                    break;
                }
                if (absorbed) continue;

                var ax = 0.0;
                var ay = 0.0;
                for (var k = 0; k < _blackHoles.Count; k++)
                {
                    var bh = _blackHoles[k];
                    var gm = EffectiveGravParameter(bh);
                    var effH = EffectiveHorizon(bh);
                    var soft = effH * 0.55;
                    var frx = bh.Cx - sx;
                    var fry = bh.Cy - sy;
                    var fdistSq = frx * frx + fry * fry + soft * soft;
                    var fdist = Math.Sqrt(fdistSq);
                    if (fdist < 1e-5) fdist = 1e-5;
                    var invDistCubed = 1.0 / (fdist * fdistSq);
                    ax += gm * frx * invDistCubed / flareMass;
                    ay += gm * fry * invDistCubed / flareMass;
                }

                fl.OrbVx += ax * dtSec;
                fl.OrbVy += ay * dtSec;

                var fspd = Math.Sqrt(fl.OrbVx * fl.OrbVx + fl.OrbVy * fl.OrbVy);
                if (fspd > vCap)
                {
                    var k = vCap / fspd;
                    fl.OrbVx *= k;
                    fl.OrbVy *= k;
                }

                Canvas.SetLeft(fl.Host, sx - hc);
                Canvas.SetTop(fl.Host, sy - hc);
            }
        }

        private sealed class BlackHoleState
        {
            public Canvas Root { get; init; } = null!;
            public double Cx, Cy;
            public double Vx, Vy;
            public double BaseHorizonR;
            public double BaseDistortR;
            public double BaseGravParameter;
            public double GrowthDisplay;
            public double GrowthTarget;
            public double TimeMs;
            public Border EinsteinRing { get; init; } = null!;
            public Border? OuterDistortion;
            public double SpinAngle;
        }

        private void StartSpaceTimer()
        {
            if (_spaceFrameTimer != null) return;

            _spaceFrameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _spaceFrameTimer.Tick += SpaceFrameTick;
            _spaceFrameTimer.Start();
        }

        private void StopSpaceTimer()
        {
            if (_spaceFrameTimer == null) return;
            _spaceFrameTimer.Stop();
            _spaceFrameTimer.Tick -= SpaceFrameTick;
            _spaceFrameTimer = null;
        }

        private void SpaceFrameTick(object? sender, EventArgs e)
        {
            if (!_palette.ShowStarsAndParallax) return;

            const double follow = 0.047;
            _panX += (_parallaxTargetX - _panX) * follow;
            _panY += (_parallaxTargetY - _panY) * follow;

            if (_tfFar != null)
            {
                _tfFar.X = _panX * 0.24;
                _tfFar.Y = _panY * 0.24;
            }

            if (_tfNebula != null)
            {
                _tfNebula.X = _panX * 0.4;
                _tfNebula.Y = _panY * 0.4;
            }

            if (_tfDust != null)
            {
                _tfDust.X = _panX * 0.66;
                _tfDust.Y = _panY * 0.66;
            }

            if (_tfNear != null)
            {
                _tfNear.X = _panX;
                _tfNear.Y = _panY;
            }

            if (_tfBgMicro != null)
            {
                _tfBgMicro.X = Math.Clamp(-_panX * 0.1975, -4.375, 4.375);
                _tfBgMicro.Y = Math.Clamp(-_panY * 0.1975, -4.375, 4.375);
            }

            TickBlackHoles(16);

            UpdatePointerTrail(16);

            TickMeteors(0.016);

            var nebulaCanvas = this.FindControl<Canvas>("NebulaCanvas");
            if (nebulaCanvas != null)
            {
                _nebulaPulsePhase += 0.00232;
                nebulaCanvas.Opacity = 0.93 + 0.065 * Math.Sin(_nebulaPulsePhase);
            }

            void TwinkleList(IReadOnlyList<StarEntry> list)
            {
                foreach (var s in list)
                {
                    s.Phase += s.PhaseSpeed;
                    var wave = 0.5 + 0.5 * Math.Sin(s.Phase);
                    var baseOp = s.OpacityMin + (s.OpacityMax - s.OpacityMin) * wave;
                    if (_signalStar == s && _signalTime > 0)
                    {
                        var g1 = Math.Exp(-Math.Pow((_signalTime - 0.38) * 11, 2));
                        var g2 = Math.Exp(-Math.Pow((_signalTime - 0.78) * 11, 2));
                        baseOp = Math.Clamp(baseOp * (1 + (g1 + g2) * 0.95), 0, 1);
                    }

                    s.Ellipse.Opacity = baseOp;
                }
            }

            TwinkleList(_starsFar);
            TwinkleList(_starsNear);
            TwinkleList(_dust);

            foreach (var fl in _flares)
            {
                fl.Phase += fl.PhaseSpeed;
                var s = 0.5 + 0.5 * Math.Sin(fl.Phase);
                s *= s;
                fl.Core.Opacity = 0.02 + 0.96 * s;
                foreach (var ray in fl.SoftRays)
                    ray.Opacity = 0.015 + 0.78 * s;
            }

            foreach (var n in _nebulaBlobs)
            {
                n.PhaseX += n.DriftSpeedX * (n.Kind == NebulaKind.Cloud ? 1 : n.Kind == NebulaKind.Ribbon ? 1.35f : 1.6f);
                n.PhaseY += n.DriftSpeedY * (n.Kind == NebulaKind.Accent ? 1.4f : 1f);
                var dx = Math.Sin(n.PhaseX) * n.AmpX;
                var dy = Math.Cos(n.PhaseY * 0.74) * n.AmpY;
                Canvas.SetLeft(n.Visual, n.BaseLeft + dx);
                Canvas.SetTop(n.Visual, n.BaseTop + dy);
            }

            _signalCooldown -= 16;
            if (_signalCooldown <= 0 && _signalStar == null)
            {
                var candidates = _starsFar.Concat(_starsNear).Where(s => s.CanSignal).ToList();
                if (candidates.Count > 0)
                {
                    _signalStar = candidates[_rng.Next(candidates.Count)];
                    _signalTime = 0;
                    _signalCooldown = 6000 + _rng.Next(14000);
                }
                else
                    _signalCooldown = 2000;
            }

            if (_signalStar != null)
            {
                _signalTime += 0.016;
                if (_signalTime >= 1.85)
                {
                    _signalStar = null;
                    _signalTime = 0;
                }
            }

            _nextAutoMeteorMs -= 16;
            if (_nextAutoMeteorMs <= 0)
            {
                SpawnMeteorFromEdge();
                _nextAutoMeteorMs = 20000 + _rng.NextDouble() * 20000;
            }
        }

        // (Физика звёзд сохранена по духу: вязкость, мягкое ядро, поглощение по траектории.
        // Теперь учитывается несколько дыр, но без "прямого падения" — орбитальные скорости остаются ключевыми.)

        private void ResetParallax()
        {
            _parallaxTargetX = _parallaxTargetY = 0;
            _panX = _panY = 0;
            foreach (var t in new[] { _tfFar, _tfNebula, _tfDust, _tfNear, _tfBgMicro })
            {
                if (t != null)
                {
                    t.X = 0;
                    t.Y = 0;
                }
            }
        }

        private void RunHeaderIntro()
        {
            var headerText = this.FindControl<TextBlock>("HeaderText");
            var logo = this.FindControl<Image>("BrandLogo");
            if (logo != null)
                logo.Opacity = 1;

            if (headerText != null)
            {
                headerText.Opacity = 1;
                _ = AnimateHeaderText(headerText);
            }
        }

        private void OnButtonPressed(object? sender, PointerPressedEventArgs e)
        {
            Ellipse? ripple = null;
            if (sender == this.FindControl<Button>("NewDocumentButton"))
                ripple = _newDocumentRippleEffect;
            else if (sender == this.FindControl<Button>("ExitButton"))
                ripple = _exitRippleEffect;

            if (ripple != null)
            {
                ripple.Width = 0;
                ripple.Height = 0;
                ripple.Opacity = 0.5;
                ripple.Width = 400;
                ripple.Height = 400;
                ripple.Opacity = 0;
            }
        }

        private async Task AnimateHeaderText(TextBlock headerText)
        {
            var fullText = AppBranding.AppName;
            headerText.Text = "";
            foreach (var c in fullText)
            {
                headerText.Text += c;
                await Task.Delay(85);
            }
        }

        private async Task AnimateGradientAsync(LinearGradientBrush gradient, int generation)
        {
            var a = _palette.BackgroundGradientStops;
            var b = _palette.BackgroundGradientStopsAlt;
            if (a.Length < 3 || b.Length < 3) return;

            double duration = 5000;
            double elapsed = 0;
            var forward = true;

            while (_isGradientAnimating && generation == _gradientGeneration && _palette.AnimateBackgroundGradient)
            {
                double t = elapsed / duration;
                if (!forward) t = 1 - t;

                gradient.GradientStops[0].Color = InterpolateColor(a[0], b[0], t);
                gradient.GradientStops[1].Color = InterpolateColor(a[1], b[1], t);
                gradient.GradientStops[2].Color = InterpolateColor(a[2], b[2], t);

                elapsed += 16;
                if (elapsed >= duration)
                {
                    elapsed = 0;
                    forward = !forward;
                }

                await Task.Delay(16);
            }
        }

        private static Color InterpolateColor(Color start, Color end, double t)
        {
            byte r = (byte)(start.R + (end.R - start.R) * t);
            byte g = (byte)(start.G + (end.G - start.G) * t);
            byte bl = (byte)(start.B + (end.B - start.B) * t);
            byte al = (byte)(start.A + (end.A - start.A) * t);
            return new Color(al, r, g, bl);
        }

        private sealed class PointerTrailSpark
        {
            public Ellipse Ellipse { get; init; } = null!;
            public double AgeMs;
            public double InitialOpacity;
            public double Vx;
            public double Vy;
        }

        private sealed class MeteorInstance
        {
            public Line[] Segments = null!;
            public Canvas Canvas = null!;
            public double HeadX;
            public double HeadY;
            public double Vx;
            public double Vy;
            public Point[] Hist = null!;
            public double AgeSec;
            public int CurveSign;
        }

        private sealed class StarEntry
        {
            public Ellipse Ellipse { get; init; } = null!;
            public double Phase;
            public double PhaseSpeed;
            public double OpacityMin;
            public double OpacityMax;
            public bool CanSignal;
            public double OrbVx;
            public double OrbVy;
        }

        private sealed class CrossFlareStar
        {
            public Canvas Host { get; init; } = null!;
            public Ellipse Core { get; init; } = null!;
            public Ellipse[] SoftRays { get; init; } = null!;
            public double Phase;
            public double PhaseSpeed;
            public double OrbVx;
            public double OrbVy;
        }

        private sealed class NebulaBlob
        {
            public Border Visual { get; }
            public double BaseLeft;
            public double BaseTop;
            public NebulaKind Kind;
            public double DriftSpeedX;
            public double DriftSpeedY;
            public double AmpX;
            public double AmpY;
            public double PhaseX;
            public double PhaseY;

            public NebulaBlob(Border visual, double baseLeft, double baseTop, NebulaKind kind,
                double driftX, double driftY, double ampX, double ampY)
            {
                Visual = visual;
                BaseLeft = baseLeft;
                BaseTop = baseTop;
                Kind = kind;
                DriftSpeedX = driftX;
                DriftSpeedY = driftY;
                AmpX = ampX;
                AmpY = ampY;
                PhaseX = Random.Shared.NextDouble() * Math.PI * 2;
                PhaseY = Random.Shared.NextDouble() * Math.PI * 2;
            }
        }

        private sealed class Particle
        {
            public Ellipse Ellipse { get; }
            private double _x, _y;
            private double _speedX, _speedY;
            private readonly Canvas _canvas;
            private readonly Random _random;

            public Particle(Canvas canvas, Random random)
            {
                _canvas = canvas;
                _random = random;
                Ellipse = new Ellipse { Width = 5, Height = 5 };
                Ellipse.Classes.Add("particle");

                var side = _random.Next(4);
                switch (side)
                {
                    case 0:
                        _x = _random.NextDouble() * canvas.Width;
                        _y = 0;
                        _speedX = _random.NextDouble() * 2 - 1;
                        _speedY = _random.NextDouble() + 0.5;
                        break;
                    case 1:
                        _x = canvas.Width;
                        _y = _random.NextDouble() * canvas.Height;
                        _speedX = -(_random.NextDouble() + 0.5);
                        _speedY = _random.NextDouble() * 2 - 1;
                        break;
                    case 2:
                        _x = _random.NextDouble() * canvas.Width;
                        _y = canvas.Height;
                        _speedX = _random.NextDouble() * 2 - 1;
                        _speedY = -(_random.NextDouble() + 0.5);
                        break;
                    default:
                        _x = 0;
                        _y = _random.NextDouble() * canvas.Height;
                        _speedX = _random.NextDouble() + 0.5;
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

                if (_x < -10 || _x > _canvas.Width + 10 || _y < -10 || _y > _canvas.Height + 10)
                    Respawn();

                var dx = mousePosition.X - _x;
                var dy = mousePosition.Y - _y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < 100)
                {
                    _x -= dx * 0.02;
                    _y -= dy * 0.02;
                }

                Canvas.SetLeft(Ellipse, _x);
                Canvas.SetTop(Ellipse, _y);

                if (_random.NextDouble() < 0.05)
                {
                    Ellipse.Opacity = 0.5;
                    Ellipse.Width = 5;
                    Ellipse.Height = 5;
                }
            }

            private void Respawn()
            {
                var side = _random.Next(4);
                switch (side)
                {
                    case 0:
                        _x = _random.NextDouble() * _canvas.Width;
                        _y = 0;
                        _speedX = _random.NextDouble() * 2 - 1;
                        _speedY = _random.NextDouble() * 0.5 + 0.5;
                        break;
                    case 1:
                        _x = _canvas.Width;
                        _y = _random.NextDouble() * _canvas.Height;
                        _speedX = -(_random.NextDouble() * 0.5 + 0.5);
                        _speedY = _random.NextDouble() * 2 - 1;
                        break;
                    case 2:
                        _x = _random.NextDouble() * _canvas.Width;
                        _y = _canvas.Height;
                        _speedX = _random.NextDouble() * 2 - 1;
                        _speedY = -(_random.NextDouble() * 0.5 + 0.5);
                        break;
                    default:
                        _x = 0;
                        _y = _random.NextDouble() * _canvas.Height;
                        _speedX = _random.NextDouble() * 0.5 + 0.5;
                        _speedY = _random.NextDouble() * 2 - 1;
                        break;
                }
            }
        }

        private async Task AnimateParticlesAsync(int generation)
        {
            while (_isRunning && _palette.ShowParticles && generation == _particleGeneration)
            {
                foreach (var particle in _particles)
                    particle.Update(_mousePosition);
                await Task.Delay(16);
            }
        }

        private void NewDocumentButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            Close();
        }

        private void NewFormButton_Click(object? sender, RoutedEventArgs e)
        {
            var newForm = _serviceProvider.GetRequiredService<NewForm>();
            newForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            newForm.Show();
            Close();
        }

        private void AnalysisButton_Click(object? sender, RoutedEventArgs e)
        {
            var analysisView = _serviceProvider.GetRequiredService<AnalysisView>();
            analysisView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            analysisView.Show();
            Close();
        }

        private void EditorButton_Click(object? sender, RoutedEventArgs e)
        {
            var editorWindow = _serviceProvider.GetRequiredService<EditorWindow>();
            editorWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            editorWindow.Show();
            Close();
        }

        private void UserProgramsButton_Click(object? sender, RoutedEventArgs e)
        {
            var userProgramsWindow = _serviceProvider.GetRequiredService<UserProgramsWindow>();
            userProgramsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            userProgramsWindow.Show();
            Close();
        }

        private void ExitButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _isRunning = false;
            _isGradientAnimating = false;
            _themeService.ThemeChanged -= OnThemeServiceThemeChanged;
            PointerPressed -= OnWindowPointerPressedForMeteor;
            StopSpaceTimer();
            ClearPointerTrail();
            DestroyBlackHoles();
        }
    }
}

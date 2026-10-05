using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BuddyCron;
using BuddyCron.Objects;
using BuddyCron.Profiles;

namespace RaidBro
{
    public class OverlaySnapshot
    {
        public bool IsRunning;
        public bool IsPaused;
        public bool HasTarget;
        public string TargetName = "[No Target]";
        public float TargetDistance;
        public int TargetHealthPercent;
        public AutoTargetMode TargetMode = AutoTargetMode.Nearest;
        public bool UseAutoTargeting = true;
        public bool UseAutoFace = true;
        public bool UseSmartPull = true;
    }

    public class OverlayWindow : Window
    {
        private static OverlayWindow _instance;
        private static readonly object _syncLock = new object();
        private static volatile OverlaySnapshot _currentSnapshot = new OverlaySnapshot();

        private static readonly SolidColorBrush _brushStopped = CreateFrozenBrush(Color.FromArgb(220, 80, 80, 80));
        private static readonly SolidColorBrush _brushPaused = CreateFrozenBrush(Color.FromArgb(220, 230, 126, 34));
        private static readonly SolidColorBrush _brushRunning = CreateFrozenBrush(Color.FromArgb(220, 46, 204, 113));
        private static readonly SolidColorBrush _brushHpGreen = CreateFrozenBrush(Color.FromArgb(240, 46, 204, 113));
        private static readonly SolidColorBrush _brushHpYellow = CreateFrozenBrush(Color.FromArgb(240, 241, 196, 15));
        private static readonly SolidColorBrush _brushHpRed = CreateFrozenBrush(Color.FromArgb(240, 231, 76, 60));

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private readonly TextBlock _statusBadge;
        private readonly Border _statusBadgeBorder;
        private readonly TextBlock _targetText;
        private readonly TextBlock _distanceText;
        private readonly ProgressBar _healthBar;
        private readonly TextBlock _healthPercentText;
        private readonly TextBlock _togglesText;
        private readonly DispatcherTimer _timer;

        public static OverlayWindow Instance
        {
            get
            {
                lock (_syncLock)
                {
                    return _instance;
                }
            }
        }

        public static void UpdateSnapshot(HeroLocalPlayer me, HeroCharacter target, bool isPaused)
        {
            try
            {
                var s = new OverlaySnapshot
                {
                    IsRunning = TreeRoot.IsRunning,
                    IsPaused = isPaused,
                    TargetMode = ATBSettings.Instance.TargetMode,
                    UseAutoTargeting = ATBSettings.Instance.UseAutoTargeting,
                    UseAutoFace = ATBSettings.Instance.UseAutoFace,
                    UseSmartPull = ATBSettings.Instance.UseSmartPull
                };

                if (target != null && target.IsValid && !target.IsDead)
                {
                    s.HasTarget = true;
                    s.TargetName = target.Name ?? "[Unknown]";
                    s.TargetDistance = target.Distance;
                    s.TargetHealthPercent = Math.Clamp((int)target.HealthPercent, 0, 100);
                }

                _currentSnapshot = s;
            }
            catch
            {
                // Fail-soft on torn reads
            }
        }

        public static void SetStoppedSnapshot()
        {
            try
            {
                var s = _currentSnapshot;
                if (s != null)
                {
                    s.IsRunning = false;
                }
                else
                {
                    _currentSnapshot = new OverlaySnapshot { IsRunning = false };
                }
            }
            catch { }
        }

        private static void UpdateStoppedSnapshot()
        {
            if (TreeRoot.IsRunning)
                return;

            try
            {
                if (!Core.IsInGame)
                {
                    _currentSnapshot = new OverlaySnapshot { IsRunning = false };
                    return;
                }

                using (Core.Memory.TemporaryCacheState(false))
                using (Core.Memory.AcquireFrame(false))
                {
                    var me = Core.Player;
                    var target = me?.Target;
                    UpdateSnapshot(me, target, RaidBro.IsPaused);
                    if (_currentSnapshot != null)
                        _currentSnapshot.IsRunning = false;
                }
            }
            catch
            {
                _currentSnapshot = new OverlaySnapshot { IsRunning = false };
            }
        }

        public static void EnsureStarted()
        {
            if (!ATBSettings.Instance.UseOverlay)
                return;

            var app = Application.Current;
            if (app == null) return;

            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new System.Action(EnsureStarted));
                return;
            }

            lock (_syncLock)
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new OverlayWindow();
                    _instance.Show();
                }
            }
        }

        public static void CloseOverlay()
        {
            var app = Application.Current;
            if (app == null) return;

            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new System.Action(CloseOverlay));
                return;
            }

            lock (_syncLock)
            {
                if (_instance != null)
                {
                    try
                    {
                        _instance.Close();
                    }
                    catch { }
                    _instance = null;
                }
            }
        }

        public static void UpdateStatus()
        {
            var app = Application.Current;
            if (app == null) return;

            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new System.Action(UpdateStatus));
                return;
            }

            _instance?.RefreshUI();
        }

        public OverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Width = 240;
            Height = 84;
            Left = Math.Max(0, ATBSettings.Instance.OverlayLeft);
            Top = Math.Max(0, ATBSettings.Instance.OverlayTop);

            var themeColor = GetThemeColor(ATBSettings.Instance.Theme);

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb((byte)(ATBSettings.Instance.OverlayOpacity * 255), 18, 18, 24)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(160, themeColor.R, themeColor.G, themeColor.B)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 6, 10, 6),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 12,
                    ShadowDepth = 2,
                    Opacity = 0.7
                }
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header + status
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Target info + HP
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Mode indicators

            // Row 0: Header + Status Badge
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "⚡ ATB Assist",
                Foreground = new SolidColorBrush(themeColor),
                FontWeight = FontWeights.Bold,
                FontSize = ATBSettings.Instance.OverlayFontSize,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            _statusBadge = new TextBlock
            {
                Text = "STOPPED",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _statusBadgeBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                Background = new SolidColorBrush(Color.FromArgb(200, 60, 60, 60)),
                Child = _statusBadge
            };
            Grid.SetColumn(_statusBadgeBorder, 1);
            headerGrid.Children.Add(_statusBadgeBorder);

            Grid.SetRow(headerGrid, 0);
            rootGrid.Children.Add(headerGrid);

            // Row 1: Target row with Health Bar
            var targetGrid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _targetText = new TextBlock
            {
                Text = "[No Target]",
                Foreground = Brushes.WhiteSmoke,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_targetText, 0);
            targetGrid.Children.Add(_targetText);

            _distanceText = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(Color.FromArgb(220, 180, 180, 180)),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_distanceText, 1);
            targetGrid.Children.Add(_distanceText);

            var hpGrid = new Grid { Margin = new Thickness(0, 1, 0, 3) };
            _healthBar = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Foreground = new SolidColorBrush(Color.FromArgb(240, 46, 204, 113)),
                Background = new SolidColorBrush(Color.FromArgb(160, 40, 40, 40)),
                BorderThickness = new Thickness(0)
            };
            hpGrid.Children.Add(_healthBar);

            _healthPercentText = new TextBlock
            {
                Text = "",
                Foreground = Brushes.White,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            hpGrid.Children.Add(_healthPercentText);

            var row1Stack = new StackPanel();
            row1Stack.Children.Add(targetGrid);
            row1Stack.Children.Add(hpGrid);
            Grid.SetRow(row1Stack, 1);
            rootGrid.Children.Add(row1Stack);

            // Row 2: Mode Indicators
            _togglesText = new TextBlock
            {
                Text = "[🎯 Nearest] [👁️ Face: ON] [⚔️ Pull: ON]",
                Foreground = new SolidColorBrush(Color.FromArgb(200, 160, 160, 170)),
                FontSize = 9.5,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei")
            };
            Grid.SetRow(_togglesText, 2);
            rootGrid.Children.Add(_togglesText);

            mainBorder.Child = rootGrid;
            Content = mainBorder;

            // Dragging support
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };

            LocationChanged += (s, e) =>
            {
                ATBSettings.Instance.OverlayLeft = Left;
                ATBSettings.Instance.OverlayTop = Top;
            };

            // Context Menu
            ContextMenu = CreateContextMenu();

            // Refresh timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += (s, e) => RefreshUI();
            _timer.Start();

            Closed += (s, e) =>
            {
                _timer.Stop();
                lock (_syncLock)
                {
                    if (_instance == this)
                        _instance = null;
                }
            };
        }

        private ContextMenu CreateContextMenu()
        {
            var menu = new ContextMenu();

            var miSettings = new MenuItem { Header = "⚙️ Botbase Settings..." };
            miSettings.Click += (s, e) => RaidBro.ShowSettingsWindow();
            menu.Items.Add(miSettings);

            menu.Items.Add(new Separator());

            var miPause = new MenuItem { Header = "⏸️ Pause / Resume (快捷键暂停/继续)" };
            miPause.Click += (s, e) => ATBHotkeys.TriggerPause();
            menu.Items.Add(miPause);

            var miTarget = new MenuItem { Header = "🎯 Switch Target Mode (切换目标模式)" };
            miTarget.Click += (s, e) => ATBHotkeys.TriggerCycleTargetMode();
            menu.Items.Add(miTarget);

            var miFace = new MenuItem { Header = "👁️ Toggle Auto-Face (切换自动面向)" };
            miFace.Click += (s, e) => ATBHotkeys.TriggerToggleAutoFace();
            menu.Items.Add(miFace);

            var miPull = new MenuItem { Header = "⚔️ Toggle Smart-Pull (切换智能开怪)" };
            miPull.Click += (s, e) => ATBHotkeys.TriggerToggleSmartPull();
            menu.Items.Add(miPull);

            menu.Items.Add(new Separator());

            var miReset = new MenuItem { Header = "🔄 Reset Position (重置位置)" };
            miReset.Click += (s, e) =>
            {
                Left = 100;
                Top = 100;
                ATBSettings.Instance.OverlayLeft = 100;
                ATBSettings.Instance.OverlayTop = 100;
            };
            menu.Items.Add(miReset);

            var miClose = new MenuItem { Header = "❌ Close Overlay (关闭悬浮窗)" };
            miClose.Click += (s, e) => Close();
            menu.Items.Add(miClose);

            return menu;
        }

        public void RefreshUI()
        {
            var isRunning = TreeRoot.IsRunning;
            var isPaused = RaidBro.IsPaused;

            // Check if hide overlay when running option is on
            if (ATBSettings.Instance.HideOverlayWhenRunning && isRunning && !isPaused)
            {
                if (Visibility != Visibility.Collapsed)
                    Visibility = Visibility.Collapsed;
                return;
            }

            if (Visibility != Visibility.Visible)
                Visibility = Visibility.Visible;

            // When bot is stopped, sample target safely via soft lock on UI thread (no bot thread running)
            if (!isRunning)
            {
                UpdateStoppedSnapshot();
            }

            var snapshot = _currentSnapshot ?? new OverlaySnapshot { IsRunning = isRunning, IsPaused = isPaused };

            // Status Badge
            if (!snapshot.IsRunning)
            {
                _statusBadge.Text = "STOPPED";
                _statusBadgeBorder.Background = _brushStopped;
            }
            else if (snapshot.IsPaused)
            {
                _statusBadge.Text = "PAUSED";
                _statusBadgeBorder.Background = _brushPaused;
            }
            else
            {
                _statusBadge.Text = "RUNNING";
                _statusBadgeBorder.Background = _brushRunning;
            }

            // Target information from thread-safe snapshot
            if (snapshot.HasTarget)
            {
                _targetText.Text = snapshot.TargetName;
                _distanceText.Text = $"{snapshot.TargetDistance:F1}m";

                var hp = snapshot.TargetHealthPercent;
                _healthBar.Value = hp;
                _healthPercentText.Text = $"{hp}%";

                if (hp > 50)
                    _healthBar.Foreground = _brushHpGreen;
                else if (hp > 25)
                    _healthBar.Foreground = _brushHpYellow;
                else
                    _healthBar.Foreground = _brushHpRed;
            }
            else
            {
                _targetText.Text = "[No Target]";
                _distanceText.Text = "";
                _healthBar.Value = 0;
                _healthPercentText.Text = "";
            }

            // Mode toggles
            var s = ATBSettings.Instance;
            var targetModeStr = snapshot.UseAutoTargeting ? snapshot.TargetMode.ToString() : "OFF";
            var faceStr = snapshot.UseAutoFace ? "ON" : "OFF";
            var pullStr = snapshot.UseSmartPull ? "ON" : "OFF";
            _togglesText.Text = $"[🎯 {targetModeStr}] [👁️ Face: {faceStr}] [⚔️ Pull: {pullStr}]";
        }

        public static Color GetThemeColor(OverlayTheme theme)
        {
            return theme switch
            {
                OverlayTheme.Cyan => Color.FromRgb(0, 229, 255),
                OverlayTheme.Emerald => Color.FromRgb(0, 230, 118),
                OverlayTheme.Amber => Color.FromRgb(255, 214, 0),
                OverlayTheme.Crimson => Color.FromRgb(255, 23, 68),
                OverlayTheme.Violet => Color.FromRgb(224, 64, 251),
                _ => Color.FromRgb(0, 229, 255)
            };
        }
    }
}

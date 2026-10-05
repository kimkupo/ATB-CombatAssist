using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WinFormsKeys = System.Windows.Forms.Keys;

namespace RaidBro
{
    public class SettingsWindow : Window
    {
        private readonly CheckBox _chkAutoTarget;
        private readonly ComboBox _cbTargetMode;
        private readonly Slider _sliderMaxDistance;
        private readonly TextBlock _lblMaxDistance;
        private readonly CheckBox _chkTargetOnlyInCombat;
        private readonly CheckBox _chkAutoSwitchDead;
        private readonly CheckBox _chkAutoFace;
        private readonly CheckBox _chkSmartPull;
        private readonly CheckBox _chkPartyCombat;

        private readonly HotkeyRecorderControl _hkStartStop;
        private readonly HotkeyRecorderControl _hkPause;
        private readonly HotkeyRecorderControl _hkTargetMode;
        private readonly HotkeyRecorderControl _hkAutoFace;
        private readonly HotkeyRecorderControl _hkSmartPull;

        private readonly CheckBox _chkUseOverlay;
        private readonly CheckBox _chkHideOverlayRunning;
        private readonly Slider _sliderOpacity;
        private readonly TextBlock _lblOpacity;
        private readonly ComboBox _cbTheme;
        private readonly CheckBox _chkToast;

        public SettingsWindow()
        {
            Title = "ATB - Combat Assist Settings";
            Width = 530;
            Height = 490;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 30));
            Foreground = Brushes.WhiteSmoke;
            FontFamily = new FontFamily("Segoe UI, Microsoft YaHei");

            var s = ATBSettings.Instance;

            var rootGrid = new Grid { Margin = new Thickness(14) };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tabs
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // ==================== Header ====================
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 32, 32, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(12, 8, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(6)
            };

            var headerStack = new StackPanel();
            var titleText = new TextBlock
            {
                Text = "⚡ ATB - Combat Assist 高级输出协助",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255))
            };
            var subtitleText = new TextBlock
            {
                Text = "全自动目标选择 · 全局快捷键暂停/启动 · HUD 悬浮状态窗 · 智能开怪面向",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            headerStack.Children.Add(titleText);
            headerStack.Children.Add(subtitleText);
            headerBorder.Child = headerStack;
            Grid.SetRow(headerBorder, 0);
            rootGrid.Children.Add(headerBorder);

            // ==================== Tabs ====================
            var tabControl = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 36)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 60, 60, 80)),
                Foreground = Brushes.WhiteSmoke
            };

            // ---------- Tab 1: Combat ----------
            var tabCombat = new TabItem { Header = "⚔️ 战斗协助 (Combat)" };
            var scrollCombat = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panelCombat = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };

            _chkAutoTarget = CreateCheckBox("启用智能自动选怪 (Auto-Targeting)", s.UseAutoTargeting);
            panelCombat.Children.Add(_chkAutoTarget);

            var gridMode = new Grid { Margin = new Thickness(22, 6, 0, 8) };
            gridMode.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            gridMode.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblMode = new TextBlock { Text = "选怪目标策略:", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.LightGray };
            Grid.SetColumn(lblMode, 0);
            gridMode.Children.Add(lblMode);

            _cbTargetMode = new ComboBox { Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            _cbTargetMode.Items.Add("Nearest (最近敌人)");
            _cbTargetMode.Items.Add("LowestHp (血量最低 / 集火减员)");
            _cbTargetMode.Items.Add("HighestHp (血量最高 / 精英首领)");
            _cbTargetMode.Items.Add("TankAssist (协助坦克目标)");
            _cbTargetMode.Items.Add("None (仅手动选怪)");
            _cbTargetMode.SelectedIndex = (int)s.TargetMode;
            Grid.SetColumn(_cbTargetMode, 1);
            gridMode.Children.Add(_cbTargetMode);
            panelCombat.Children.Add(gridMode);

            var gridDist = new Grid { Margin = new Thickness(22, 0, 0, 8) };
            gridDist.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            gridDist.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridDist.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

            var lblDist = new TextBlock { Text = "最大选怪距离:", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.LightGray };
            Grid.SetColumn(lblDist, 0);
            gridDist.Children.Add(lblDist);

            _sliderMaxDistance = new Slider { Minimum = 5, Maximum = 60, Value = s.MaxTargetDistance, VerticalAlignment = VerticalAlignment.Center, SmallChange = 1, LargeChange = 5 };
            Grid.SetColumn(_sliderMaxDistance, 1);
            gridDist.Children.Add(_sliderMaxDistance);

            _lblMaxDistance = new TextBlock { Text = $"{s.MaxTargetDistance:F0}m", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
            _sliderMaxDistance.ValueChanged += (sender, e) => _lblMaxDistance.Text = $"{e.NewValue:F0}m";
            Grid.SetColumn(_lblMaxDistance, 2);
            gridDist.Children.Add(_lblMaxDistance);
            panelCombat.Children.Add(gridDist);

            _chkTargetOnlyInCombat = CreateCheckBox("仅选择处于战斗中的敌人 (Target Only In-Combat)", s.TargetOnlyInCombat);
            _chkTargetOnlyInCombat.Margin = new Thickness(22, 0, 0, 6);
            panelCombat.Children.Add(_chkTargetOnlyInCombat);

            _chkAutoSwitchDead = CreateCheckBox("当前目标死亡时自动切换下一目标 (Switch on Death)", s.AutoSwitchDeadTarget);
            _chkAutoSwitchDead.Margin = new Thickness(22, 0, 0, 10);
            panelCombat.Children.Add(_chkAutoSwitchDead);

            _chkAutoFace = CreateCheckBox("自动面向目标 (Auto-Face Target)", s.UseAutoFace);
            panelCombat.Children.Add(_chkAutoFace);

            _chkSmartPull = CreateCheckBox("智能开怪起手 (Smart Pull: 选中目标在距离内即开始输出)", s.UseSmartPull);
            panelCombat.Children.Add(_chkSmartPull);

            _chkPartyCombat = CreateCheckBox("小队进战检测 (队友进战时也激活输出循环)", s.CheckPartyMemberCombat);
            panelCombat.Children.Add(_chkPartyCombat);

            scrollCombat.Content = panelCombat;
            tabCombat.Content = scrollCombat;
            tabControl.Items.Add(tabCombat);

            // ---------- Tab 2: Hotkeys ----------
            var tabHotkeys = new TabItem { Header = "⌨️ 快捷键设置 (Hotkeys)" };
            var panelHotkeys = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };

            var tipText = new TextBlock
            {
                Text = "点击输入框后按下任意快捷键组合 (如 F10, Shift+X)，按 Esc 清除绑定:",
                Foreground = Brushes.Silver,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 12)
            };
            panelHotkeys.Children.Add(tipText);

            _hkStartStop = new HotkeyRecorderControl(s.StartStopKey, s.StartStopModifier);
            panelHotkeys.Children.Add(CreateHotkeyRow("启动 / 停止 Bot (Start/Stop):", _hkStartStop));

            _hkPause = new HotkeyRecorderControl(s.PauseKey, s.PauseModifier);
            panelHotkeys.Children.Add(CreateHotkeyRow("暂停 / 继续输出 (Pause/Resume):", _hkPause));

            _hkTargetMode = new HotkeyRecorderControl(s.TargetModeKey, s.TargetModeModifier);
            panelHotkeys.Children.Add(CreateHotkeyRow("循环切换选怪模式 (Cycle Target):", _hkTargetMode));

            _hkAutoFace = new HotkeyRecorderControl(s.AutoFaceKey, s.AutoFaceModifier);
            panelHotkeys.Children.Add(CreateHotkeyRow("开关自动面向 (Toggle Auto-Face):", _hkAutoFace));

            _hkSmartPull = new HotkeyRecorderControl(s.SmartPullKey, s.SmartPullModifier);
            panelHotkeys.Children.Add(CreateHotkeyRow("开关智能开怪 (Toggle Smart-Pull):", _hkSmartPull));

            var btnResetHotkeys = new Button
            {
                Content = "🔄 恢复默认快捷键",
                Width = 140,
                Height = 26,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 8, 0, 0)
            };
            btnResetHotkeys.Click += (sender, e) =>
            {
                _hkStartStop.SetHotkey(WinFormsKeys.F10, ModifierKeys.None);
                _hkPause.SetHotkey(WinFormsKeys.X, ModifierKeys.Shift);
                _hkTargetMode.SetHotkey(WinFormsKeys.T, ModifierKeys.Shift);
                _hkAutoFace.SetHotkey(WinFormsKeys.F, ModifierKeys.Shift);
                _hkSmartPull.SetHotkey(WinFormsKeys.P, ModifierKeys.Shift);
            };
            panelHotkeys.Children.Add(btnResetHotkeys);

            tabHotkeys.Content = panelHotkeys;
            tabControl.Items.Add(tabHotkeys);

            // ---------- Tab 3: Overlay & Toast ----------
            var tabOverlay = new TabItem { Header = "🖥️ 悬浮窗 & 提示 (HUD / Toast)" };
            var panelOverlay = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };

            _chkUseOverlay = CreateCheckBox("启用 HUD 状态悬浮窗 (Show Floating Status Bar)", s.UseOverlay);
            panelOverlay.Children.Add(_chkUseOverlay);

            _chkHideOverlayRunning = CreateCheckBox("运行时自动隐藏 (仅暂停/停止时显示悬浮窗)", s.HideOverlayWhenRunning);
            _chkHideOverlayRunning.Margin = new Thickness(22, 4, 0, 10);
            panelOverlay.Children.Add(_chkHideOverlayRunning);

            var gridOpacity = new Grid { Margin = new Thickness(22, 0, 0, 10) };
            gridOpacity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            gridOpacity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridOpacity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

            var lblOp = new TextBlock { Text = "悬浮窗不透明度:", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.LightGray };
            Grid.SetColumn(lblOp, 0);
            gridOpacity.Children.Add(lblOp);

            _sliderOpacity = new Slider { Minimum = 0.3, Maximum = 1.0, Value = s.OverlayOpacity, SmallChange = 0.05, LargeChange = 0.1, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_sliderOpacity, 1);
            gridOpacity.Children.Add(_sliderOpacity);

            _lblOpacity = new TextBlock { Text = $"{s.OverlayOpacity * 100:F0}%", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
            _sliderOpacity.ValueChanged += (sender, e) => _lblOpacity.Text = $"{e.NewValue * 100:F0}%";
            Grid.SetColumn(_lblOpacity, 2);
            gridOpacity.Children.Add(_lblOpacity);
            panelOverlay.Children.Add(gridOpacity);

            var gridTheme = new Grid { Margin = new Thickness(22, 0, 0, 12) };
            gridTheme.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            gridTheme.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblTheme = new TextBlock { Text = "悬浮窗主题色彩:", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.LightGray };
            Grid.SetColumn(lblTheme, 0);
            gridTheme.Children.Add(lblTheme);

            _cbTheme = new ComboBox { Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            _cbTheme.Items.Add("Cyan (科技青蓝)");
            _cbTheme.Items.Add("Emerald (翡翠鲜绿)");
            _cbTheme.Items.Add("Amber (琥珀金黄)");
            _cbTheme.Items.Add("Crimson (赤焰绯红)");
            _cbTheme.Items.Add("Violet (幻夜魅紫)");
            _cbTheme.SelectedIndex = (int)s.Theme;
            Grid.SetColumn(_cbTheme, 1);
            gridTheme.Children.Add(_cbTheme);
            panelOverlay.Children.Add(gridTheme);

            _chkToast = CreateCheckBox("启用快捷键屏幕悬浮提示 (Toast Notifications)", s.UseToastMessages);
            panelOverlay.Children.Add(_chkToast);

            var btnResetPos = new Button
            {
                Content = "🔄 重置悬浮窗屏幕位置",
                Width = 160,
                Height = 26,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 14, 0, 0)
            };
            btnResetPos.Click += (sender, e) =>
            {
                s.OverlayLeft = 100;
                s.OverlayTop = 100;
                if (OverlayWindow.Instance != null)
                {
                    OverlayWindow.Instance.Left = 100;
                    OverlayWindow.Instance.Top = 100;
                }
            };
            panelOverlay.Children.Add(btnResetPos);

            tabOverlay.Content = panelOverlay;
            tabControl.Items.Add(tabOverlay);

            Grid.SetRow(tabControl, 1);
            rootGrid.Children.Add(tabControl);

            // ==================== Bottom Buttons ====================
            var buttonGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var btnSave = new Button
            {
                Content = "✔ 保存并应用 (Save & Apply)",
                Width = 180,
                Height = 30,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(0, 168, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnSave.Click += (sender, e) => SaveAndApply();
            Grid.SetColumn(btnSave, 1);
            buttonGrid.Children.Add(btnSave);

            var btnCancel = new Button
            {
                Content = "关闭 (Close)",
                Width = 90,
                Height = 30,
                Margin = new Thickness(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (sender, e) => Close();
            Grid.SetColumn(btnCancel, 2);
            buttonGrid.Children.Add(btnCancel);

            Grid.SetRow(buttonGrid, 2);
            rootGrid.Children.Add(buttonGrid);

            Content = rootGrid;
        }

        private static CheckBox CreateCheckBox(string text, bool isChecked)
        {
            return new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Foreground = Brushes.WhiteSmoke,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 4),
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private static Grid CreateHotkeyRow(string label, HotkeyRecorderControl control)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 5) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = label,
                Foreground = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            Grid.SetColumn(control, 1);
            grid.Children.Add(control);

            return grid;
        }

        private void SaveAndApply()
        {
            var s = ATBSettings.Instance;

            // Combat settings
            s.UseAutoTargeting = _chkAutoTarget.IsChecked ?? true;
            s.TargetMode = (AutoTargetMode)Math.Max(0, _cbTargetMode.SelectedIndex);
            s.MaxTargetDistance = (float)_sliderMaxDistance.Value;
            s.TargetOnlyInCombat = _chkTargetOnlyInCombat.IsChecked ?? false;
            s.AutoSwitchDeadTarget = _chkAutoSwitchDead.IsChecked ?? true;
            s.UseAutoFace = _chkAutoFace.IsChecked ?? true;
            s.UseSmartPull = _chkSmartPull.IsChecked ?? true;
            s.CheckPartyMemberCombat = _chkPartyCombat.IsChecked ?? true;

            // Hotkeys
            s.StartStopKey = _hkStartStop.SelectedKey;
            s.StartStopModifier = _hkStartStop.SelectedModifiers;

            s.PauseKey = _hkPause.SelectedKey;
            s.PauseModifier = _hkPause.SelectedModifiers;

            s.TargetModeKey = _hkTargetMode.SelectedKey;
            s.TargetModeModifier = _hkTargetMode.SelectedModifiers;

            s.AutoFaceKey = _hkAutoFace.SelectedKey;
            s.AutoFaceModifier = _hkAutoFace.SelectedModifiers;

            s.SmartPullKey = _hkSmartPull.SelectedKey;
            s.SmartPullModifier = _hkSmartPull.SelectedModifiers;

            // Overlay & Toast
            s.UseOverlay = _chkUseOverlay.IsChecked ?? true;
            s.HideOverlayWhenRunning = _chkHideOverlayRunning.IsChecked ?? false;
            s.OverlayOpacity = _sliderOpacity.Value;
            s.Theme = (OverlayTheme)Math.Max(0, _cbTheme.SelectedIndex);
            s.UseToastMessages = _chkToast.IsChecked ?? true;

            s.Save();

            // Re-apply hotkeys immediately
            ATBHotkeys.ReRegisterAll();

            // Overlay management
            if (s.UseOverlay)
                OverlayWindow.EnsureStarted();
            else
                OverlayWindow.CloseOverlay();

            OverlayWindow.UpdateStatus();

            ToastWindow.Show("⚡ ATB 设置已保存并生效", Color.FromRgb(0, 229, 255));
            Close();
        }
    }
}

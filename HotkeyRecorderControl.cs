using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinFormsKeys = System.Windows.Forms.Keys;

namespace raidcombat
{
    public class HotkeyRecorderControl : Border
    {
        private readonly TextBlock _textBlock;
        private bool _isRecording;

        public WinFormsKeys SelectedKey { get; private set; }
        public ModifierKeys SelectedModifiers { get; private set; }

        public event Action<WinFormsKeys, ModifierKeys> HotkeyChanged;

        public HotkeyRecorderControl(WinFormsKeys initialKey, ModifierKeys initialModifiers)
        {
            SelectedKey = initialKey;
            SelectedModifiers = initialModifiers;

            Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 38));
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 80, 80, 100));
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(5);
            Padding = new Thickness(10, 5, 10, 5);
            Focusable = true;
            Cursor = Cursors.Hand;
            MinHeight = 32;

            _textBlock = new TextBlock
            {
                Text = FormatHotkey(SelectedKey, SelectedModifiers),
                Foreground = Brushes.WhiteSmoke,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                FontFamily = new FontFamily("Consolas, Segoe UI, Microsoft YaHei"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Child = _textBlock;

            PreviewMouseDown += (s, e) =>
            {
                Focus();
                StartRecording();
                e.Handled = true;
            };

            LostFocus += (s, e) => StopRecording();

            PreviewKeyDown += OnPreviewKeyDown;
        }

        public void SetHotkey(WinFormsKeys key, ModifierKeys modifiers)
        {
            SelectedKey = key;
            SelectedModifiers = modifiers;
            _textBlock.Text = FormatHotkey(key, modifiers);
        }

        private void StartRecording()
        {
            _isRecording = true;
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255));
            Background = new SolidColorBrush(Color.FromArgb(220, 40, 40, 55));
            _textBlock.Text = "Press keys... (Esc to clear)";
            _textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255));
        }

        private void StopRecording()
        {
            _isRecording = false;
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 80, 80, 100));
            Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 38));
            _textBlock.Text = FormatHotkey(SelectedKey, SelectedModifiers);
            _textBlock.Foreground = Brushes.WhiteSmoke;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecording) return;
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Escape to clear
            if (key == Key.Escape)
            {
                SelectedKey = WinFormsKeys.None;
                SelectedModifiers = ModifierKeys.None;
                StopRecording();
                HotkeyChanged?.Invoke(SelectedKey, SelectedModifiers);
                return;
            }

            // Ignore bare modifier presses
            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            var modifiers = Keyboard.Modifiers;
            var winFormsKey = (WinFormsKeys)KeyInterop.VirtualKeyFromKey(key);

            SelectedKey = winFormsKey;
            SelectedModifiers = modifiers;

            StopRecording();
            HotkeyChanged?.Invoke(SelectedKey, SelectedModifiers);
        }

        public static string FormatHotkey(WinFormsKeys key, ModifierKeys modifiers)
        {
            if (key == WinFormsKeys.None)
                return "[None]";

            var sb = new StringBuilder();
            if ((modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl + ");
            if ((modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift + ");
            if ((modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt + ");
            if ((modifiers & ModifierKeys.Windows) != 0) sb.Append("Win + ");

            sb.Append(key.ToString());
            return sb.ToString();
        }
    }
}

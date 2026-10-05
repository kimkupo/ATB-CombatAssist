using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace RaidBro
{
    public class ToastWindow : Window
    {
        private static ToastWindow _activeToast;

        public static void Show(string message, Color accentColor)
        {
            if (!ATBSettings.Instance.UseToastMessages)
                return;

            var app = Application.Current;
            if (app == null)
                return;

            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new Action(() => Show(message, accentColor)));
                return;
            }

            try
            {
                if (_activeToast != null && _activeToast.IsLoaded)
                {
                    _activeToast.Close();
                    _activeToast = null;
                }

                var toast = new ToastWindow(message, accentColor);
                _activeToast = toast;
                toast.Show();
            }
            catch
            {
                // Fallback safe: ignore any window creation failures
            }
        }

        private ToastWindow(string message, Color accentColor)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            IsHitTestVisible = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 20, 20, 25)),
                BorderBrush = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 10, 20, 10),
                Effect = new DropShadowEffect
                {
                    Color = accentColor,
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.6
                }
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(textBlock);
            border.Child = stack;
            Content = border;

            Opacity = 0.0;

            Loaded += (s, e) =>
            {
                // Position near upper-middle of screen
                Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2.0;
                Top = SystemParameters.PrimaryScreenHeight * 0.22;

                var fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(150)));
                BeginAnimation(OpacityProperty, fadeIn);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(250)));
                    fadeOut.Completed += (os, oe) =>
                    {
                        Close();
                        if (_activeToast == this)
                            _activeToast = null;
                    };
                    BeginAnimation(OpacityProperty, fadeOut);
                };
                timer.Start();
            };
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using BuddyCron;
using BuddyCron.Objects;
using Reborn.Utilities;

namespace RaidCombat
{
    public static class QueueNotificationManager
    {
        #region Win32 FlashWindow API

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_CAPTION = 1;
        private const uint FLASHW_TRAY = 2;
        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMER = 4;
        private const uint FLASHW_TIMERNOFG = 12; // Flash continuously until window comes to foreground

        #endregion

        private static Timer _monitorTimer;
        private static bool _isMonitoring;
        private static bool _isAlertActive;
        private static int _pollLock;
        private static DateTime _lastAlertTime = DateTime.MinValue;

        public static bool IsQueuePoppedState => _isAlertActive;

        public static void StartMonitor()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitorTimer = new Timer(OnPollTimer, null, 1000, 500);
            Logging.Write("[ATB] Queue Pop Monitor started (排队状态栏闪烁监控已启动)");
        }

        public static void StopMonitor()
        {
            _isMonitoring = false;
            try
            {
                _monitorTimer?.Dispose();
                _monitorTimer = null;
            }
            catch { }

            if (_isAlertActive)
            {
                _isAlertActive = false;
                StopFlashingGameWindow();
            }
        }

        public static void Pulse()
        {
            // Can be called during bot pulse for immediate tick response
            PollQueueStatus();
        }

        private static void OnPollTimer(object state)
        {
            if (!_isMonitoring)
                return;

            PollQueueStatus();
        }

        private static void PollQueueStatus()
        {
            if (Interlocked.CompareExchange(ref _pollLock, 1, 0) != 0)
                return;

            try
            {
                bool isPopped = CheckIsQueuePopped();

                if (isPopped)
                {
                    if (!_isAlertActive)
                    {
                        // Transitioned from normal -> popped!
                        _isAlertActive = true;
                        _lastAlertTime = DateTime.UtcNow;
                        TriggerAlert();
                    }
                    else
                    {
                        // Already active: ensure continuous flashing if window is still not foreground
                        if (ATBSettings.Instance.FlashTaskbarOnQueuePop && ATBSettings.Instance.ContinuousFlashOnQueuePop)
                        {
                            var gameHwnd = GetGameWindowHandle();
                            if (gameHwnd != IntPtr.Zero && GetForegroundWindow() != gameHwnd)
                            {
                                // Keep flashing active if needed
                                if ((DateTime.UtcNow - _lastAlertTime).TotalSeconds >= 3)
                                {
                                    FlashGameWindow(true);
                                    _lastAlertTime = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (_isAlertActive)
                    {
                        // Transitioned from popped -> normal (accepted, declined, or timed out)
                        _isAlertActive = false;
                        StopFlashingGameWindow();
                        Logging.Write("[ATB] 副本/战场排队状态已重置，停止状态栏闪烁。");
                    }
                }
            }
            catch
            {
                // Fail-soft: never allow background monitoring to crash the bot
            }
            finally
            {
                Interlocked.Exchange(ref _pollLock, 0);
            }
        }

        private static bool CheckIsQueuePopped()
        {
            try
            {
                if (!Core.IsInitialized || Core.Memory == null)
                    return false;

                // 1. Check MATCHMAKING node (Warzones, Arenas, Galactic Starfighter)
                try
                {
                    IntPtr mmNode = Gom.LookupNode((ulong)GomNodeId.MATCHMAKING);
                    if (mmNode != IntPtr.Zero)
                    {
                        if (Gom.TryGetField<ulong>(mmNode, GomFieldId.mmsPendingMatchExpirationTime, out var expUlong) && expUlong > 0)
                            return true;

                        if (Gom.TryGetField<double>(mmNode, GomFieldId.mmsPendingMatchExpirationTime, out var expDouble) && expDouble > 0)
                            return true;

                        if (Gom.TryGetField<ulong>(mmNode, GomFieldId.mmsPendingMatchmakerId, out var mmId) && mmId != 0)
                            return true;
                    }
                }
                catch { }

                // 2. Check GROUPFINDER node (Flashpoints, Operations, Uprisings)
                try
                {
                    IntPtr gfNode = Gom.LookupNode((ulong)GomNodeId.GROUPFINDER);
                    if (gfNode != IntPtr.Zero)
                    {
                        if (Gom.TryGetField<grpFinderCharacterStateEnum>(gfNode, GomFieldId.grpFinderCharacterState, out var gfState))
                        {
                            if (gfState == grpFinderCharacterStateEnum.Pending)
                                return true;
                        }
                        else if (Gom.TryGetField<int>(gfNode, GomFieldId.grpFinderCharacterState, out var gfStateInt))
                        {
                            if (gfStateInt == (int)grpFinderCharacterStateEnum.Pending) // 3
                                return true;
                        }
                    }
                }
                catch { }

                // 3. Check Player / Character node
                try
                {
                    IntPtr playerNode = Gom.GetPlayerNode();
                    if (playerNode != IntPtr.Zero)
                    {
                        // Character state in group finder
                        if (Gom.TryGetField<grpFinderCharacterStateEnum>(playerNode, GomFieldId.grpFinderCharacterState, out var pState))
                        {
                            if (pState == grpFinderCharacterStateEnum.Pending)
                                return true;
                        }
                        else if (Gom.TryGetField<int>(playerNode, GomFieldId.grpFinderCharacterState, out var pStateInt))
                        {
                            if (pStateInt == (int)grpFinderCharacterStateEnum.Pending)
                                return true;
                        }

                        // Ready check pending
                        if (Gom.TryGetField<grpReadyCheckStatus>(playerNode, GomFieldId.grpReadyCheckStatus, out var rcStatus))
                        {
                            if (rcStatus == grpReadyCheckStatus.Pending)
                                return true;
                        }

                        // Active instance / phase entry popup
                        if (Gom.TryGetField<ulong>(playerNode, GomFieldId.phsActiveInstancePopupID, out var popupId) && popupId != 0)
                            return true;

                        if (Gom.TryGetField<bool>(playerNode, GomFieldId.phsActiveInstancePopup, out var hasPopup) && hasPopup)
                            return true;
                    }
                }
                catch { }
            }
            catch { }

            return false;
        }

        private static void TriggerAlert()
        {
            var s = ATBSettings.Instance;

            Logging.Write("[ATB] 🔔 副本/战场排队成功！正在触发游戏状态栏闪烁提醒...");

            // 1. Flash game window in taskbar
            if (s.FlashTaskbarOnQueuePop)
            {
                FlashGameWindow(s.ContinuousFlashOnQueuePop);
            }

            // 2. Play notification sound
            if (s.SoundOnQueuePop)
            {
                try
                {
                    SystemSounds.Exclamation.Play();
                }
                catch { }
            }

            // 3. Show screen toast message
            if (s.ToastOnQueuePop)
            {
                ToastWindow.Show("🔔 副本/战场排队成功！请尽快确认进入！", Colors.Aqua);
            }
        }

        public static void TestAlert()
        {
            Logging.Write("[ATB] 🔔 [测试] 触发排队成功状态栏闪烁与声音提醒测试...");

            // Flash game window
            FlashGameWindow(true);

            // Play alert sound
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch { }

            // Show Toast
            ToastWindow.Show("🔔 [测试] 游戏状态栏闪烁提醒测试成功！", Colors.Aqua);
        }

        public static IntPtr GetGameWindowHandle()
        {
            try
            {
                var proc = Core.Memory?.Process;
                if (proc != null && proc.MainWindowHandle != IntPtr.Zero)
                    return proc.MainWindowHandle;
            }
            catch { }

            try
            {
                var swtor = Process.GetProcessesByName("swtor").FirstOrDefault();
                if (swtor != null && swtor.MainWindowHandle != IntPtr.Zero)
                    return swtor.MainWindowHandle;
            }
            catch { }

            return IntPtr.Zero;
        }

        public static bool FlashGameWindow(bool continuous = true)
        {
            var hwnd = GetGameWindowHandle();
            if (hwnd == IntPtr.Zero)
                return false;

            var fi = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = hwnd,
                dwFlags = continuous ? (FLASHW_ALL | FLASHW_TIMERNOFG) : FLASHW_ALL,
                uCount = continuous ? 0u : 5u,
                dwTimeout = 0
            };

            return FlashWindowEx(ref fi);
        }

        public static bool StopFlashingGameWindow()
        {
            var hwnd = GetGameWindowHandle();
            if (hwnd == IntPtr.Zero)
                return false;

            var fi = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = hwnd,
                dwFlags = FLASHW_STOP,
                uCount = 0,
                dwTimeout = 0
            };

            return FlashWindowEx(ref fi);
        }
    }
}

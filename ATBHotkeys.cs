using System;
using System.Windows.Media;
using BuddyCron.Profiles;
using Reborn.Utilities;
using WinFormsKeys = System.Windows.Forms.Keys;

namespace RaidCombat
{
    public static class ATBHotkeys
    {
        private const string HkStartStop = "ATB_StartStop";
        private const string HkPause = "ATB_Pause";
        private const string HkTargetMode = "ATB_TargetMode";
        private const string HkAutoFace = "ATB_AutoFace";
        private const string HkSmartPull = "ATB_SmartPull";

        private static bool _startStopRegistered;

        /// <summary>
        /// Registers the Start/Stop hotkey globally so user can start/stop the bot anytime.
        /// </summary>
        public static void RegisterStartStopHotkey()
        {
            var s = ATBSettings.Instance;
            if (s.StartStopKey == WinFormsKeys.None)
                return;

            try
            {
                if (_startStopRegistered)
                    HotkeyManager.Unregister(HkStartStop);

                HotkeyManager.Register(HkStartStop, s.StartStopKey, s.StartStopModifier, hk => TriggerStartStop());
                _startStopRegistered = true;
                Logging.Write($"[ATB] Registered Start/Stop Hotkey: {HotkeyRecorderControl.FormatHotkey(s.StartStopKey, s.StartStopModifier)}");
            }
            catch (Exception ex)
            {
                Logging.Write($"[ATB] Failed to register Start/Stop hotkey: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers combat-time hotkeys (Pause, Target mode, Auto-face, Smart-pull).
        /// </summary>
        public static void RegisterCombatHotkeys()
        {
            var s = ATBSettings.Instance;

            try
            {
                UnregisterCombatHotkeys();

                if (s.PauseKey != WinFormsKeys.None)
                {
                    HotkeyManager.Register(HkPause, s.PauseKey, s.PauseModifier, hk => TriggerPause());
                    Logging.Write($"[ATB] Registered Pause Hotkey: {HotkeyRecorderControl.FormatHotkey(s.PauseKey, s.PauseModifier)}");
                }

                if (s.TargetModeKey != WinFormsKeys.None)
                {
                    HotkeyManager.Register(HkTargetMode, s.TargetModeKey, s.TargetModeModifier, hk => TriggerCycleTargetMode());
                    Logging.Write($"[ATB] Registered Target Mode Hotkey: {HotkeyRecorderControl.FormatHotkey(s.TargetModeKey, s.TargetModeModifier)}");
                }

                if (s.AutoFaceKey != WinFormsKeys.None)
                {
                    HotkeyManager.Register(HkAutoFace, s.AutoFaceKey, s.AutoFaceModifier, hk => TriggerToggleAutoFace());
                    Logging.Write($"[ATB] Registered Auto-Face Hotkey: {HotkeyRecorderControl.FormatHotkey(s.AutoFaceKey, s.AutoFaceModifier)}");
                }

                if (s.SmartPullKey != WinFormsKeys.None)
                {
                    HotkeyManager.Register(HkSmartPull, s.SmartPullKey, s.SmartPullModifier, hk => TriggerToggleSmartPull());
                    Logging.Write($"[ATB] Registered Smart-Pull Hotkey: {HotkeyRecorderControl.FormatHotkey(s.SmartPullKey, s.SmartPullModifier)}");
                }

            }
            catch (Exception ex)
            {
                Logging.Write($"[ATB] Error registering combat hotkeys: {ex.Message}");
            }
        }

        public static void UnregisterCombatHotkeys()
        {
            try
            {
                HotkeyManager.Unregister(HkPause);
                HotkeyManager.Unregister(HkTargetMode);
                HotkeyManager.Unregister(HkAutoFace);
                HotkeyManager.Unregister(HkSmartPull);
            }
            catch { }
        }

        public static void UnregisterAll()
        {
            UnregisterCombatHotkeys();
            try
            {
                HotkeyManager.Unregister(HkStartStop);
                _startStopRegistered = false;
            }
            catch { }
        }

        public static void ReRegisterAll()
        {
            UnregisterAll();
            RegisterStartStopHotkey();
            RegisterCombatHotkeys();
        }

        // ==================== Hotkey Actions ====================

        public static void TriggerStartStop()
        {
            try
            {
                if (TreeRoot.IsRunning)
                {
                    TreeRoot.Stop("ATB Hotkey pressed");
                    ToastWindow.Show("⚡ ATB Stopped", Colors.OrangeRed);
                    Logging.Write("[ATB] Bot stopped via hotkey.");
                }
                else
                {
                    TreeRoot.Start();
                    ToastWindow.Show("⚡ ATB Started", Colors.LimeGreen);
                    Logging.Write("[ATB] Bot started via hotkey.");
                }
                OverlayWindow.UpdateStatus();
            }
            catch (Exception ex)
            {
                Logging.Write($"[ATB] Start/Stop hotkey error: {ex.Message}");
            }
        }

        public static void TriggerPause()
        {
            RaidCombat.IsPaused = !RaidCombat.IsPaused;
            if (RaidCombat.IsPaused)
            {
                ToastWindow.Show("⏸️ ATB Paused!", Colors.Gold);
                Logging.Write("[ATB] Combat Assist Paused!");
            }
            else
            {
                ToastWindow.Show("▶️ ATB Resumed!", Colors.LimeGreen);
                Logging.Write("[ATB] Combat Assist Resumed!");
            }
            OverlayWindow.UpdateStatus();
        }

        public static void TriggerCycleTargetMode()
        {
            var s = ATBSettings.Instance;
            var next = s.TargetMode switch
            {
                AutoTargetMode.None => AutoTargetMode.Nearest,
                AutoTargetMode.Nearest => AutoTargetMode.LowestHp,
                AutoTargetMode.LowestHp => AutoTargetMode.HighestHp,
                AutoTargetMode.HighestHp => AutoTargetMode.TankAssist,
                AutoTargetMode.TankAssist => AutoTargetMode.None,
                _ => AutoTargetMode.Nearest
            };

            s.TargetMode = next;
            s.UseAutoTargeting = (next != AutoTargetMode.None);
            s.Save();

            ToastWindow.Show($"🎯 Target: {next}", Colors.Cyan);
            Logging.Write($"[ATB] Target Mode switched to: {next}");
            OverlayWindow.UpdateStatus();
        }

        public static void TriggerToggleAutoFace()
        {
            var s = ATBSettings.Instance;
            s.UseAutoFace = !s.UseAutoFace;
            s.Save();

            ToastWindow.Show(s.UseAutoFace ? "👁️ Auto-Face: ON" : "👁️ Auto-Face: OFF", Colors.Cyan);
            Logging.Write($"[ATB] Auto-Face toggled: {s.UseAutoFace}");
            OverlayWindow.UpdateStatus();
        }

        public static void TriggerToggleSmartPull()
        {
            var s = ATBSettings.Instance;
            s.UseSmartPull = !s.UseSmartPull;
            s.Save();

            ToastWindow.Show(s.UseSmartPull ? "⚔️ Smart-Pull: ON" : "⚔️ Smart-Pull: OFF", Colors.Cyan);
            Logging.Write($"[ATB] Smart-Pull toggled: {s.UseSmartPull}");
            OverlayWindow.UpdateStatus();
        }
    }
}

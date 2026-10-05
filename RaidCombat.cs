using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using BuddyCron;
using BuddyCron.Inheritables;
using BuddyCron.Managers;
using BuddyCron.Navigation;
using BuddyCron.Objects;
using BuddyCron.Settings;
using Reborn.Behaviors.Treesharp;
using Reborn.Utilities;
using Action = Reborn.Behaviors.Treesharp.Action;

namespace raidcombat
{
    /// <summary>
    /// Legacy settings wrapper for backward compatibility with any older configs.
    /// </summary>
    public class RaidBroSettings
    {
        public static RaidBroSettings Instance { get; } = new RaidBroSettings();

        public bool CheckPartyMemberCombat
        {
            get => ATBSettings.Instance.CheckPartyMemberCombat;
            set => ATBSettings.Instance.CheckPartyMemberCombat = value;
        }

        public Keys PauseKey
        {
            get => ATBSettings.Instance.PauseKey;
            set => ATBSettings.Instance.PauseKey = value;
        }
    }

    /// <summary>
    /// Backward-compatibility alias for RaidBro
    /// </summary>
    public static class RaidBro
    {
        public static bool IsPaused
        {
            get => RaidCombat.IsPaused;
            set => RaidCombat.IsPaused = value;
        }

        public static void ShowSettingsWindow() => RaidCombat.ShowSettingsWindow();
    }

    public class RaidCombat : BotBase
    {
        public override string Name => "raidcombat";
        public override bool WantButton => true;
        public override bool RequiresProfile => false;
        public override PulseFlags PulseFlags => PulseFlags.All;
        public override bool IsAutonomous => false;

        public static bool IsPaused { get; set; }

        private Composite _root;
        public override Composite Root => _root;

        public override void Pulse()
        {
            if (ATBSettings.Instance.UseOverlay)
            {
                var me = Core.Player;
                OverlayWindow.UpdateSnapshot(me, me?.Target, IsPaused);
            }
        }

        private static SettingsWindow _activeSettingsWindow;

        public override void Initialize()
        {
            // Register global Start/Stop hotkey on startup so user can start/stop anytime
            ATBHotkeys.RegisterStartStopHotkey();

            // Prepare overlay if enabled
            if (ATBSettings.Instance.UseOverlay)
            {
                OverlayWindow.EnsureStarted();
            }
        }

        public override void OnButtonPress()
        {
            ShowSettingsWindow();
        }

        public static void ShowSettingsWindow()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new System.Action(ShowSettingsWindow));
                return;
            }

            try
            {
                if (_activeSettingsWindow != null && _activeSettingsWindow.IsLoaded)
                {
                    _activeSettingsWindow.Activate();
                    return;
                }

                _activeSettingsWindow = new SettingsWindow();
                _activeSettingsWindow.Closed += (s, e) => _activeSettingsWindow = null;
                _activeSettingsWindow.Show();
                _activeSettingsWindow.Activate();
            }
            catch (Exception ex)
            {
                Logging.Write($"[ATB] Failed to open settings window: {ex.Message}");
            }
        }

        public override void Start()
        {
            Navigator.PlayerMover = new NullMover();
            Navigator.NavigationProvider = new NullProvider();

            IsPaused = false;

            // Register all hotkeys
            ATBHotkeys.RegisterStartStopHotkey();
            ATBHotkeys.RegisterCombatHotkeys();

            // Ensure overlay is active
            if (ATBSettings.Instance.UseOverlay)
            {
                OverlayWindow.UpdateSnapshot(Core.Player, Core.Player?.Target, IsPaused);
                OverlayWindow.EnsureStarted();
            }
            OverlayWindow.UpdateStatus();

            // Build behavior tree
            _root = new PrioritySelector(
                // Pulse targeting and auto-face each tick (fails so tree continues)
                new Action(r =>
                {
                    ATBTargeting.Pulse();
                    return RunStatus.Failure;
                }),
                // Rotation execution gated by !IsPaused and CanEngage
                new Decorator(r => !IsPaused && CanEngage(r), CombatLogic())
            );

            Logging.Write("[ATB] Starting raidcombat (ATB Engine Active)");
            ToastWindow.Show("⚡ raidcombat 已启动", System.Windows.Media.Colors.LimeGreen);
        }

        private static bool CanEngage(object r)
        {
            var me = Core.Player;
            if (me == null || me.IsDead)
                return false;

            var target = me.Target;
            if (!ATBTargeting.IsValidEnemy(target))
                return false;

            // In combat: engage immediately
            if (me.InCombat || (ATBSettings.Instance.CheckPartyMemberCombat && PartyInCombat))
            {
                return true;
            }

            // Out-of-combat: Smart Pull allows engaging target in range
            if (ATBSettings.Instance.UseSmartPull)
            {
                return target.Distance <= ATBSettings.Instance.MaxTargetDistance;
            }

            return false;
        }

        private static Composite CombatLogic()
        {
            return new PrioritySelector(
                new HookExecutor("PreCombatBuff"),
                new HookExecutor("Heal"),
                new HookExecutor("CombatBuff"),
                new HookExecutor("Pull"),
                new HookExecutor("Combat")
            );
        }

        public override void Stop()
        {
            _root = null;
            ATBHotkeys.UnregisterCombatHotkeys();
            OverlayWindow.SetStoppedSnapshot();
            OverlayWindow.UpdateStatus();
            Logging.Write("[ATB] Stopping raidcombat");
        }

        public override void OnShutdown()
        {
            ATBHotkeys.UnregisterAll();
            OverlayWindow.CloseOverlay();
        }

        public static bool PartyInCombat
        {
            get
            {
                try
                {
                    var me = Core.Me;
                    if (me == null)
                        return false;

                    if (me.Companion != null && me.Companion.InCombat)
                        return true;

                    ulong groupId = me.GroupId;
                    if (groupId == 0)
                        return false;

                    foreach (HeroPlayer player in HeroObjectManager.Players)
                    {
                        if (player.NodeId == me.NodeId)
                            continue;

                        if (player.GroupId == groupId && player.InCombat)
                            return true;
                    }
                }
                catch
                {
                    // Out-of-process GOM reads are fail-soft
                }
                return false;
            }
        }
    }
}

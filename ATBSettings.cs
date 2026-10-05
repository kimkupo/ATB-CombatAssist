using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using BuddyCron.Settings;
using Reborn.Utilities.Settings;

namespace RaidBro
{
    public enum AutoTargetMode
    {
        None,
        Nearest,
        LowestHp,
        HighestHp,
        TankAssist
    }

    public enum OverlayTheme
    {
        Cyan,
        Emerald,
        Amber,
        Crimson,
        Violet
    }

    public class ATBSettings : JsonSettings
    {
        private static ATBSettings _instance;
        public static ATBSettings Instance => _instance ?? (_instance = new ATBSettings());

        public ATBSettings()
            : base(Path.Combine(CharacterSettings.CharacterSettingsDirectory, "ATBSettings.json"))
        {
        }

        // ==================== Hotkeys ====================
        private Keys _startStopKey = Keys.F10;
        [DefaultValue(Keys.F10)]
        public Keys StartStopKey
        {
            get => _startStopKey;
            set { if (_startStopKey != value) { _startStopKey = value; Save(); } }
        }

        private ModifierKeys _startStopModifier = ModifierKeys.None;
        [DefaultValue(ModifierKeys.None)]
        public ModifierKeys StartStopModifier
        {
            get => _startStopModifier;
            set { if (_startStopModifier != value) { _startStopModifier = value; Save(); } }
        }

        private Keys _pauseKey = Keys.X;
        [DefaultValue(Keys.X)]
        public Keys PauseKey
        {
            get => _pauseKey;
            set { if (_pauseKey != value) { _pauseKey = value; Save(); } }
        }

        private ModifierKeys _pauseModifier = ModifierKeys.Shift;
        [DefaultValue(ModifierKeys.Shift)]
        public ModifierKeys PauseModifier
        {
            get => _pauseModifier;
            set { if (_pauseModifier != value) { _pauseModifier = value; Save(); } }
        }

        private Keys _targetModeKey = Keys.T;
        [DefaultValue(Keys.T)]
        public Keys TargetModeKey
        {
            get => _targetModeKey;
            set { if (_targetModeKey != value) { _targetModeKey = value; Save(); } }
        }

        private ModifierKeys _targetModeModifier = ModifierKeys.Shift;
        [DefaultValue(ModifierKeys.Shift)]
        public ModifierKeys TargetModeModifier
        {
            get => _targetModeModifier;
            set { if (_targetModeModifier != value) { _targetModeModifier = value; Save(); } }
        }

        private Keys _autoFaceKey = Keys.F;
        [DefaultValue(Keys.F)]
        public Keys AutoFaceKey
        {
            get => _autoFaceKey;
            set { if (_autoFaceKey != value) { _autoFaceKey = value; Save(); } }
        }

        private ModifierKeys _autoFaceModifier = ModifierKeys.Shift;
        [DefaultValue(ModifierKeys.Shift)]
        public ModifierKeys AutoFaceModifier
        {
            get => _autoFaceModifier;
            set { if (_autoFaceModifier != value) { _autoFaceModifier = value; Save(); } }
        }

        private Keys _smartPullKey = Keys.P;
        [DefaultValue(Keys.P)]
        public Keys SmartPullKey
        {
            get => _smartPullKey;
            set { if (_smartPullKey != value) { _smartPullKey = value; Save(); } }
        }

        private ModifierKeys _smartPullModifier = ModifierKeys.Shift;
        [DefaultValue(ModifierKeys.Shift)]
        public ModifierKeys SmartPullModifier
        {
            get => _smartPullModifier;
            set { if (_smartPullModifier != value) { _smartPullModifier = value; Save(); } }
        }

        // ==================== Combat & Target Settings ====================
        private bool _useAutoTargeting = true;
        [DefaultValue(true)]
        public bool UseAutoTargeting
        {
            get => _useAutoTargeting;
            set { if (_useAutoTargeting != value) { _useAutoTargeting = value; Save(); } }
        }

        private AutoTargetMode _targetMode = AutoTargetMode.Nearest;
        [DefaultValue(AutoTargetMode.Nearest)]
        public AutoTargetMode TargetMode
        {
            get => _targetMode;
            set { if (_targetMode != value) { _targetMode = value; Save(); } }
        }

        private float _maxTargetDistance = 30f;
        [DefaultValue(30f)]
        public float MaxTargetDistance
        {
            get => _maxTargetDistance;
            set { if (_maxTargetDistance != value) { _maxTargetDistance = value; Save(); } }
        }

        private bool _targetOnlyInCombat = false;
        [DefaultValue(false)]
        public bool TargetOnlyInCombat
        {
            get => _targetOnlyInCombat;
            set { if (_targetOnlyInCombat != value) { _targetOnlyInCombat = value; Save(); } }
        }

        private bool _autoSwitchDeadTarget = true;
        [DefaultValue(true)]
        public bool AutoSwitchDeadTarget
        {
            get => _autoSwitchDeadTarget;
            set { if (_autoSwitchDeadTarget != value) { _autoSwitchDeadTarget = value; Save(); } }
        }

        private bool _useAutoFace = true;
        [DefaultValue(true)]
        public bool UseAutoFace
        {
            get => _useAutoFace;
            set { if (_useAutoFace != value) { _useAutoFace = value; Save(); } }
        }

        private bool _useSmartPull = true;
        [DefaultValue(true)]
        public bool UseSmartPull
        {
            get => _useSmartPull;
            set { if (_useSmartPull != value) { _useSmartPull = value; Save(); } }
        }

        private bool _checkPartyMemberCombat = true;
        [DefaultValue(true)]
        public bool CheckPartyMemberCombat
        {
            get => _checkPartyMemberCombat;
            set { if (_checkPartyMemberCombat != value) { _checkPartyMemberCombat = value; Save(); } }
        }

        // ==================== Overlay Settings ====================
        private bool _useOverlay = true;
        [DefaultValue(true)]
        public bool UseOverlay
        {
            get => _useOverlay;
            set { if (_useOverlay != value) { _useOverlay = value; Save(); } }
        }

        private bool _hideOverlayWhenRunning = false;
        [DefaultValue(false)]
        public bool HideOverlayWhenRunning
        {
            get => _hideOverlayWhenRunning;
            set { if (_hideOverlayWhenRunning != value) { _hideOverlayWhenRunning = value; Save(); } }
        }

        private double _overlayOpacity = 0.85;
        [DefaultValue(0.85)]
        public double OverlayOpacity
        {
            get => _overlayOpacity;
            set { if (Math.Abs(_overlayOpacity - value) > 0.001) { _overlayOpacity = value; Save(); } }
        }

        private double _overlayFontSize = 13.0;
        [DefaultValue(13.0)]
        public double OverlayFontSize
        {
            get => _overlayFontSize;
            set { if (Math.Abs(_overlayFontSize - value) > 0.001) { _overlayFontSize = value; Save(); } }
        }

        private double _overlayLeft = 100.0;
        [DefaultValue(100.0)]
        public double OverlayLeft
        {
            get => _overlayLeft;
            set { if (Math.Abs(_overlayLeft - value) > 0.1) { _overlayLeft = value; Save(); } }
        }

        private double _overlayTop = 100.0;
        [DefaultValue(100.0)]
        public double OverlayTop
        {
            get => _overlayTop;
            set { if (Math.Abs(_overlayTop - value) > 0.1) { _overlayTop = value; Save(); } }
        }

        private OverlayTheme _theme = OverlayTheme.Cyan;
        [DefaultValue(OverlayTheme.Cyan)]
        public OverlayTheme Theme
        {
            get => _theme;
            set { if (_theme != value) { _theme = value; Save(); } }
        }

        private bool _useToastMessages = true;
        [DefaultValue(true)]
        public bool UseToastMessages
        {
            get => _useToastMessages;
            set { if (_useToastMessages != value) { _useToastMessages = value; Save(); } }
        }
    }
}

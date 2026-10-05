# BuddyCron ATB — Combat Assist BotBase

[English](#english) | [中文说明](#中文说明)

---

## English

**ATB (Active Time Battle) Combat Assist** is a high-performance, semi-automated combat routine engine designed for BuddyCron / RebornBuddy architecture. It gives players full control over movement and positioning while automating combat rotations, target acquisition, facing, and status monitoring.

### 🌟 Key Features

- **Semi-Autonomous Control**: Retains 100% player manual movement and mechanics handling while automating offensive and defensive rotations.
- **Global Hotkey System**:
  - Global hotkeys work even when the game or bot window is running in the background.
  - Interactive Hotkey Recorder for full keybind customization (`Ctrl`, `Alt`, `Shift` + any key).
  - Built-in conflict prevention and instant unbinding.
- **Smart Target Acquisition (ATB Targeting)**:
  - Configurable targeting priorities: `Nearest`, `Lowest HP` (execute priority), `Highest HP` (boss focus), and `Tank Assist`.
  - Auto-Face: Automatically pivots player character toward the active target to prevent cast failures.
  - Auto-Switch: Instantly purges dead targets and acquires the next valid enemy within milliseconds.
  - Smart Pull: Automatically initiates combat on valid targets when engaged.
- **Modern Floating HUD Overlay (WPF)**:
  - Sleek dark glassmorphic design displaying target name, HP gauge, distance, and current combat status.
  - **Zero-Lock Snapshot Architecture**: Utilizes a producer-consumer snapshot pattern. Renders purely in memory on the UI thread without querying underlying game memory or thread-unsafe collections, preventing deadlocks or UI freezing.
  - Optional auto-hide during active combat pulses.
- **Toast Notification Engine**: Non-intrusive on-screen notifications for hotkey triggers and status changes.

### ⌨️ Default Keybindings

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `F10` | Start / Stop Bot | Master toggle for bot execution |
| `Shift + X` | Pause / Resume Combat | Instantly halts or resumes ability execution |
| `Shift + T` | Cycle Targeting Mode | Switches between Nearest, Lowest HP, Highest HP, Tank Assist, and OFF |
| `Shift + F` | Toggle Auto-Face | Enables / disables automatic facing towards the current target |
| `Shift + P` | Toggle Smart Pull | Enables / disables auto-initiating combat |

### 🛠️ Architecture & Performance

- **Targeting Engine**: Safe enemy filtering via `CanAttack`, `IsDead`, and reaction checks.
- **Thread Safety**: Snapshot isolation model separating high-frequency Bot pulse threads (`30 TPS`) from WPF Dispatcher UI rendering.

---

## 中文说明

**BuddyCron ATB (Active Time Battle) 输出辅助模式** 是专为 BuddyCron 打造的半自动智能战斗引擎。让玩家在保留 100% 手动走位与副本机制处理自由度的同时，获得全自动技能循环、智能索敌、自动面向与低开销悬浮 HUD 状态监控。

### 🌟 核心特性

- **半自动战术辅助**：玩家全权掌控走位与机制规避，后台高效执行最优技能输出与减伤回血循环。
- **免切屏全局热键**：
  - 游戏与辅助后台运行时依然生效。
  - 内置快捷键捕获录制组件，支持 `Ctrl`、`Alt`、`Shift` 任意组合键自定义。
  - 防冲突检测与一键清除。
- **智能索敌与面向引擎**：
  - 多样化索敌优先级：最近目标（Nearest）、斩杀集火（Lowest HP）、攻坚精英（Highest HP）、协助坦克（Tank Assist）与手动模式（OFF）。
  - 自动面向（Auto-Face）：施法时自动朝向当前有效目标，解决背对目标导致的技能施放失败。
  - 自动切怪：目标死亡瞬间（`< 200ms`）清理目标并转火下一合法敌人。
  - 智能开怪（Smart Pull）：接近敌人后自动进入战斗序列。
- **极简暗黑 WPF 悬浮 HUD**：
  - 实时显示当前目标名称、血条百分比、距离与运行状态。
  - **零锁快照架构（Producer-Consumer Snapshot）**：采用不可变内存快照，UI 线程不调用任何非线程安全的底层内存字典，彻底杜绝死锁与界面假死。
- **屏幕发光通知窗（Toast）**：热键切换状态时在屏幕正下方弹出胶囊式半透明反馈。

### ⌨️ 默认快捷键

| 快捷键 | 功能 | 说明 |
| :--- | :--- | :--- |
| `F10` | 启动 / 停止 Bot | 主开关控制 |
| `Shift + X` | 暂停 / 恢复输出 | 瞬时切断或恢复技能轮转 |
| `Shift + T` | 轮换索敌策略 | 在 最近 / 最低血量 / 最高血量 / 协助坦克 / 关闭 间切换 |
| `Shift + F` | 开关自动面向 | 开启或关闭自动朝向目标 |
| `Shift + P` | 开关智能开怪 | 开启或关闭进入射程自动开怪 |

---

### 📦 安装与配置

1. 将 `RaidCombat` 目录放置在 BuddyCron 的 `BotBases/` 文件夹下。
2. 启动 BuddyCron，在主界面下拉菜单中选择 **RaidCombat**。
3. 点击 **Bot Config** 进入现代化配置窗口进行快捷键与 HUD 参数定制。

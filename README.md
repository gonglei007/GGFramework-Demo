# GGFramework-Demo
应用 GGFramework 框架开发的 demo 工程，演示框架各模块（Core / GGResource / GGNetwork / GGUI / GGUser）如何在真实 Unity 工程中集成与使用。

## 环境

- 适用 Unity 版本：**Unity 6000.3.13f1**
- 框架依赖：经 UPM 引用 GGFramework.Core 单一源，`unity_project/Packages/manifest.json` 中锁定：

  ```json
  "com.gonglei.ggframework.core": "https://github.com/gonglei007/GGFramework-Core.git#v0.1.1"
  ```

  Core 的 Runtime 程序集 `autoReferenced: true`，`Assembly-CSharp` 中的脚本（如各 `*System` 单例）可直接解析 `Singleton<T>` 等类型，工程内不再保留本地副本。

## 工程组成（`unity_project/Assets/ggsource/`）

- **`LuaFramework/`** —— 位于 ToLua 之上的 Lua 框架层：`Common` / `ConstDefine` / `Controller` / `Framework` / `Manager` / `Network` / `ObjectPool` / `Utility` / `View` 及入口 `Main.cs`。
- **`Scripts/`** —— C# 运行时与系统层，按职责分目录：
  - `Systems/`：各功能单例系统（`ResourceSystem` / `NetworkSystem` / `UISystem` / `UserSystem` / `DownloadSystem` / `GameSystem` / `LuaSystem` / `SoundSystem` / `PackageSystem` / `ExtractSystem` 等），均继承自 Core 的 `Singleton<T>`。
  - `UI/`、`User/`（SDK 接入）、`Network/`、`Resource/`、`Platform/`、`Hotfix/`、`Utils/`、`VFX/`、`Animation/`、`Editor/` 等。

## 关于「副本对齐」（ADR-0003）

按 GGFramework Phase 3 的 ADR-0003，`GameDebugger.cs`、`ZipUtil.cs` 等含第三方耦合的工具**不纳入 Core**，改为以「副本对齐」方式保留在各消费方仓库内：消除无意义漂移、修复缺陷，同时**保留有意的行为分叉**。

本工程内的 `Scripts/Utils/GameDebugger.cs` 即为这样一份副本，与框架默认版本存在有意分叉：

- 不自动挂载 AFPSCounter FPS 计数器（`AFPSCounter.AddToScene()` 等相关代码已注释）；
- 不注册全局日志回调（`Application.logMessageReceived += ...` 已注释）。

> 详细的副本对齐范围与决策依据见 `GGFramework-Docs` 仓库的 ADR-0003。

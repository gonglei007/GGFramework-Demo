# GGFramework-Demo
应用 GGFramework 框架开发的 demo 工程，演示框架各模块（Core / GGResource / GGNetwork / GGUI / GGUser）如何在真实 Unity 工程中集成与使用。

## 快速开始

1. 安装 Git 并单独克隆本仓库：`git clone https://github.com/gonglei007/GGFramework-Demo.git`。无需克隆 Core 或其他模块仓库。
2. 用 **Unity 6000.3.13f1** 打开 `unity_project`，等待导入完成。
3. 选择菜单 **GGFramework → Demo → Play Showcase**，或打开 `Assets/FrameworkShowcase/Scenes/FrameworkShowcase.unity` 后播放。
4. 按侧栏依次体验 Core、资源、HTTP、UI、用户 SDK 与 Lua；每页都有操作指引、状态与回调记录。
5. 资源页的真实 AssetBundle 示例先执行 **Build Sample Bundles**。之后可用 **Run Smoke Tests** 验证主要流程。

默认构建入口已设为体验大厅。无需旧业务服务器、热更包或渠道账号；本机 HTTP 服务随播放启停。模拟支付仅产生本地事件。

完整操作步骤、API 对照、测试命令和接入边界见 [开发者体验指南](documents/showcase-guide.md)。

## 环境

- 适用 Unity 版本：**Unity 6000.3.13f1**
- 框架依赖：经 UPM 引用公开 GitHub 仓库中的 GGFramework.Core，`unity_project/Packages/manifest.json` 固定到已发布的 **v0.3.0** 标签：

  ```json
  "com.gonglei.ggframework.core": "https://github.com/gonglei007/GGFramework-Core.git#v0.3.0"
  ```

  Git 需位于 PATH 中；首次导入需要访问 GitHub 和 Unity 包服务。Unity Package Manager 自动下载固定版本的 Core，`packages-lock.json` 记录实际解析的提交，无需本地 Core 路径。

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

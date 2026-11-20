# GGFramework-Demo

GGFramework 的 Unity 集成演示与开发者体验大厅，覆盖 Core、资源与 AssetBundle、HTTP、UI 导航、用户 SDK 和 Lua 规则。

[![Unity 6000.3.13f1](https://img.shields.io/badge/Unity-6000.3.13f1-222222?logo=unity)](https://github.com/gonglei007/GGFramework-Demo/blob/main/unity_project/ProjectSettings/ProjectVersion.txt) [![Core dependency v0.3.0](https://img.shields.io/badge/Core_dependency-v0.3.0-2563eb)](https://github.com/gonglei007/GGFramework-Core/tree/v0.3.0) [![Branch main](https://img.shields.io/badge/Branch-main-64748b)](https://github.com/gonglei007/GGFramework-Demo/tree/main)

## 项目导航

**Demo 体验** · [架构文档](https://github.com/gonglei007/GGFramework-Docs) · [Core](https://github.com/gonglei007/GGFramework-Core) · [GGResource](https://github.com/gonglei007/GGFramework-GGResource) · [GGNetwork](https://github.com/gonglei007/GGFramework-GGNetwork) · [GGUI](https://github.com/gonglei007/GGFramework-GGUI) · [GGUser](https://github.com/gonglei007/GGFramework-GGUser)

> **体验入口**：本仓库可单独克隆，使用 Unity 菜单 **GGFramework → Demo → Play Showcase** 打开功能大厅。[操作指南](https://github.com/gonglei007/GGFramework-Demo/blob/main/documents/showcase-guide.md) · [验收记录](documents/showcase-verification.md)。

## 模块关系

Demo 通过公开 GitHub UPM 依赖固定使用 **Core v0.3.0**，可单独克隆。GGResource、GGNetwork、GGUI、GGUser 的集成代码沿用工程内既有源码副本；独立模块的新 API 和后端演进以各模块仓库为准，升级独立仓库不会自动同步这些副本。

完整依赖图、工程锁定版本和源码副本边界由[架构文档中心](https://github.com/gonglei007/GGFramework-Docs#模块关系)统一维护。

## 快速开始

1. 安装 Git 并单独克隆本仓库：`git clone https://github.com/gonglei007/GGFramework-Demo.git`。无需克隆 Core 或其他模块仓库。
2. 用 **Unity 6000.3.13f1** 打开 `unity_project`，等待导入完成。
3. 选择菜单 **GGFramework → Demo → Play Showcase**，或打开 `Assets/FrameworkShowcase/Scenes/FrameworkShowcase.unity` 后播放。
4. 按侧栏依次体验 Core、资源、HTTP、UI、用户 SDK 与 Lua；每页都有操作指引、状态与回调记录。
5. 资源页的真实 AssetBundle 示例先执行 **Build Sample Bundles**。之后可用 **Run Smoke Tests** 验证主要流程。

默认构建入口已设为体验大厅。无需旧业务服务器、热更包或渠道账号；本机 HTTP 服务随播放启停。模拟支付仅产生本地事件。

完整操作步骤、API 对照、测试命令和接入边界见 [开发者体验指南](documents/showcase-guide.md)。

## 2026-11-20 实跑回归

Windows 11 / Unity 6000.3.13f1：Core 66/66、Editor 冒烟 28/28、Windows x64 Mono Player 冒烟 27/27；新增生命周期每轮 35 项，关闭 Domain Reload 连续两轮及 Player 均通过。[运行证据](documents/verification-2026-11-20/results.json) · [复现入口与未覆盖项](documents/showcase-guide.md#生命周期与重复播放回归)。本次修复大厅 UI 归属清理、内置 Loxodon 重复播放标记及三个旧 NGUI Prefab 的脚本引用。

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

## 自动化 Unity 回归与 CI

共享执行器与一次性 runner 启动说明见 [tools/ci/README.md](tools/ci/README.md)，本仓契约为 [ci/unity-regression.json](ci/unity-regression.json)。分支 push 自动创建真实 Unity 回归 job；使用已激活的 Windows Unity 6000.3.13f1 一次性 runner，每轮结束后注销，不安装常驻服务。PR 的 GitHub 托管契约检查与真实 Unity 结果分别显示，未启动 runner 时不能算作 Unity 通过。

Demo CI 覆盖稳定 Core 66 项、Editor 冒烟、关闭 Domain Reload 连续两次完整退出、专属资源/池/事件与 UI 归属清理、Prefab 实际导入，以及 Windows Mono Player 冒烟与生命周期。artifact 提供准确 SHA、逐项结果和报告摘要；完整授权日志不公开上传。其他模块按固定 Demo SHA 复用脚本，并保持各自运行时依赖。

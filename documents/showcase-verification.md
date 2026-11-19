# 体验大厅验收记录

验证日期：2026-11-19（Asia/Shanghai）。环境：Windows 11、Unity 6000.3.13f1、Windows x64 Mono Development Player。

| 检查 | 结果 | 本机原始报告（不入库） |
| --- | --- | --- |
| Demo Editor 编译 | 成功 | `unity_project/Logs/showcase-compile-final.log` |
| Core EditMode | 66 / 66 通过 | `unity_project/Logs/showcase-core-editmode.xml` |
| GGUser EditMode | 71 / 71 通过 | 同级 GGUser 工程的 `Logs/showcase-user-editmode-final.xml` |
| GGUser PlayMode | 11 / 11 通过 | 同级 GGUser 工程的 `Logs/showcase-user-playmode.xml` |
| Demo PlayMode 集成冒烟 | 28 / 28 通过 | `unity_project/Logs/showcase-smoke-results.txt` |
| Windows Player 构建 | 成功 | `unity_project/Logs/showcase-player-build-final.log` |
| Windows Player 集成冒烟 | 27 / 27 通过（不含编辑器 Simulation） | `unity_project/Logs/player-visual-verification/player-smoke-results.txt` |
| 1280 × 800 界面截图检查 | 入门、Core、资源、UI、用户页面可显示 | `unity_project/Logs/player-visual-verification/showcase-*.png` |

自动检查覆盖事件取消订阅、状态守卫、对象身份复用、协程与后台回调、资源引用计数和缺失资源恢复、
Simulation、真实 AssetBundle 清单加载、UI 页面返回及窗口清理、登录失败重试、支付三种结果、稳定 UID、
真实回环 HTTP 的成功 / 业务错误 / 503 / 取消与恢复，以及 ToLua 规则执行。

本次修复了测试中暴露的任务超时处置与后台循环退出问题、UI 空对话框与字典遍历问题、
SDK 回调重复和事件顺序问题、GGUser 构造阶段访问 Unity 路径 API，以及旧 ToLua 绑定阻塞 Player 编译的问题。

隐藏窗口的图形截图会出现黑帧，因此最终视觉检查使用可见的交互窗口；保存的预览来自实际 Windows Player。
Unity Search 启动索引器会记录既有 `ArgumentOutOfRangeException`，旧插件仍有弃用 API 警告；
这些没有阻止本次编译、测试与 Player 构建。未覆盖公网服务、真实渠道 SDK、移动设备、WebGL 或生产热更新发布链路。

重跑方法见 [开发者体验指南](showcase-guide.md#自动验证)。

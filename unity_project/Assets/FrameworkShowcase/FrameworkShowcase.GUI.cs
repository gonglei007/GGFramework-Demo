using System;
using GGFramework.Core;
using UnityEngine;

namespace GGFramework.Demo
{
    public partial class FrameworkShowcase
    {
        private readonly string[] tabs = { "开始体验", "Core 基础", "资源管理", "HTTP 网络", "UI 导航", "用户 / SDK", "Lua 脚本" };
        private int selected;
        private Vector2 contentScroll;
        private Vector2 logScroll;
        private GUIStyle titleStyle, headingStyle, textStyle, buttonStyle, smallStyle;
        private Font font;
        private string luaSource = "local coins = 128\nreturn 'Lua reward: ' .. (coins * 2)";
        public string LuaResult { get; private set; } = "尚未执行";

        private void PrepareStyles()
        {
            if (titleStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Arial" }, 16);
            titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 27, fontStyle = FontStyle.Bold };
            headingStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 21, fontStyle = FontStyle.Bold, wordWrap = true };
            textStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, wordWrap = true, richText = false };
            smallStyle = new GUIStyle(textStyle) { fontSize = 13 };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = 15, padding = new RectOffset(14, 14, 10, 10), wordWrap = true };
        }

        private void OnGUI()
        {
            if (!ready) return;
            PrepareStyles();
            // Maintain readable controls at small Game view sizes, with scrolling for all content.
            float scale = Mathf.Clamp(Screen.width / 1200f, 0.65f, 1.6f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            Fill(new Rect(0, 0, width, height), new Color(0.055f, 0.075f, 0.12f));
            GUILayout.BeginArea(new Rect(24, 16, width - 48, 70));
            GUILayout.Label("GGFramework  /  开发者体验大厅", titleStyle);
            GUILayout.Label("本地运行 · 真实 API · 操作与结果可追踪", smallStyle);
            GUILayout.EndArea();

            Fill(new Rect(16, 94, 184, height - 110), new Color(0.09f, 0.12f, 0.18f));
            GUILayout.BeginArea(new Rect(26, 104, 164, height - 130));
            for (int i = 0; i < tabs.Length; i++)
            {
                GUI.backgroundColor = i == selected ? new Color(0.18f, 0.65f, 0.75f) : Color.white;
                if (GUILayout.Button(tabs[i], buttonStyle)) { selected = i; contentScroll = Vector2.zero; }
                GUILayout.Space(6);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.Label("Core 0.3.0\nUnity 6000.3.13f1\nSDK：离线模拟", smallStyle);
            GUILayout.EndArea();

            float logHeight = Mathf.Clamp(height * 0.25f, 125, 210);
            GUILayout.BeginArea(new Rect(220, 94, width - 244, height - 120 - logHeight));
            contentScroll = GUILayout.BeginScrollView(contentScroll);
            try
            {
                switch (selected)
                {
                    case 0: DrawWelcome(); break;
                    case 1: DrawCore(); break;
                    case 2: DrawResources(); break;
                    case 3: DrawNetwork(); break;
                    case 4: DrawNavigation(); break;
                    case 5: DrawUser(); break;
                    case 6: DrawLua(); break;
                }
            }
            finally { GUILayout.EndScrollView(); GUILayout.EndArea(); }

            Fill(new Rect(220, height - logHeight - 16, width - 244, logHeight), new Color(0.08f, 0.105f, 0.16f));
            GUILayout.BeginArea(new Rect(232, height - logHeight - 8, width - 268, logHeight - 16));
            GUILayout.BeginHorizontal();
            GUILayout.Label("操作记录", headingStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空", buttonStyle, GUILayout.Width(70))) messages.Clear();
            GUILayout.EndHorizontal();
            logScroll = GUILayout.BeginScrollView(logScroll);
            foreach (string line in messages) GUILayout.Label(line, smallStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        private static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }
        private void Heading(string title, string explanation)
        {
            GUILayout.Label(title, headingStyle);
            GUILayout.Space(8);
            GUILayout.Label(explanation, textStyle);
            GUILayout.Space(14);
        }
        private void Button(string label, Action action, bool enabled = true)
        {
            bool previous = GUI.enabled;
            GUI.enabled = enabled;
            if (GUILayout.Button(label, buttonStyle))
            {
                try { action(); }
                catch (Exception e) { Log("操作失败: " + e.Message); }
            }
            GUI.enabled = previous;
        }
        private void DrawWelcome()
        {
            Heading("从一个操作开始，观察框架怎样响应", "建议依次体验：Core → 资源 → HTTP → UI → 用户。每页给出操作顺序与预期结果，下方记录实际回调。");
            GUILayout.Label("01  Core：发布金币事件，解锁状态转移；取出与归还同一池对象。\n\n02  资源：异步读取本地配置，再次加载命中缓存，释放两个引用。\n\n03  HTTP：成功、业务失败、503、取消与手动重试。\n\n04  UI：主页 → 背包 → 详情窗口 → 返回，观察页面栈与窗口归属。\n\n05  SDK：登录失败后重试、稳定 UID、模拟支付三种结果、登出再登录。\n\n06  Lua：修改计算规则，执行脚本并获取 C# 返回值。", textStyle);
            GUILayout.Space(16);
            Button("开始：Core 基础", () => selected = 1);
            GUILayout.Label("示例数据与服务仅用于教学。AssetBundle 打包 / 热更新、Pomelo 服务端协议与真实渠道 SDK 的接入边界见仓库体验指南。", smallStyle);
        }
        private void DrawCore()
        {
            Heading("Core：事件、状态、对象复用与线程", "先在金币为 0 时进入 Running（守卫拒绝），发布金币事件后再进入；关闭订阅后发布，金币保持不变。");
            GUILayout.Label("金币：" + Coins + "    订阅：" + (subscribed ? "开启" : "关闭") + "    状态：" + Machine.Current, textStyle);
            GUILayout.BeginHorizontal();
            Button("发布金币 +1", PublishCoin);
            Button(subscribed ? "取消订阅" : "恢复订阅", () => SubscribeCoins(!subscribed));
            Button("进入 Running", () => ChangeFlow(Flow.Running));
            Button("回到 Idle", () => ChangeFlow(Flow.Idle));
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
            GUILayout.Label("对象池：使用中 " + ActiveCount + " / 空闲 " + InactiveCount + "。归还后再次取出，比较记录中的 instanceID。", textStyle);
            GUILayout.BeginHorizontal();
            Button("取出对象", () => SpawnToken(), ActiveCount < 8);
            Button("归还对象", ReleaseToken, ActiveCount > 0);
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            Button("后台计算 → 主线程回调", RunBackground, !backgroundBusy);
            Button("协程延迟完成", RunCoroutine, !coroutineBusy);
            GUILayout.EndHorizontal();
            GUILayout.Label("完成：后台 " + BackgroundCompletions + " / 协程 " + CoroutineCompletions, textStyle);
            GUILayout.Label("可写路径：" + PersistentPathResolver.Best + "\n格式化：" + FormatUtil.DataSize(1572864), smallStyle);
        }
        private void DrawResources()
        {
            Heading("资源：异步加载、缓存与引用计数", "加载一次引用为 1；再加载一次复用缓存、引用为 2；释放一次资源仍存在；再释放一次移除缓存。缺失资源会显示可恢复的失败状态。");
            GUILayout.Label(ResourceStatus + "\n当前引用：" + (CachedResource == null ? 0 : CachedResource.refCount), textStyle);
            GUILayout.BeginHorizontal();
            Button("加载 / 获取引用", () => LoadResource(), !ResourceBusy);
            Button("释放一个引用", ReleaseResource, CachedResource != null && !ResourceBusy);
            Button("加载缺失资源", () => LoadResource(true), !ResourceBusy);
            GUILayout.EndHorizontal();
            if (CachedResource != null) GUILayout.Label(((TextAsset)CachedResource.assetObject).text, textStyle);
            GUILayout.Space(14);
            GUILayout.Label("此例使用本地 Resources 后端和 Demo ResourceSystem 的缓存 API，不依赖已打包 AssetBundle 清单。Bundle 模拟加载与打包流程见体验指南。", smallStyle);
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
#if UNITY_EDITOR
            Button("Simulation 模拟加载", LoadSimulation, !bundleBusy);
#endif
            Button("加载真实 AssetBundle", LoadSampleBundle, !bundleBusy);
            GUILayout.EndHorizontal();
            GUILayout.Label(BundleResult, smallStyle);
        }
        private void DrawNetwork()
        {
            Heading("HTTP：本机服务与异常交互", "请求经过 Demo HttpNetworkSystem → TaskSystem → BestHTTP → 本机 TCP HTTP 服务。选择 503 后取消，确认不会再次请求；也可手动重试。");
            GUILayout.Label(ServerAddress + "\n请求状态：" + (NetworkBusy ? "等待响应…" : "空闲") + "    返回码：" + NetworkCode, textStyle);
            GUILayout.BeginHorizontal();
            bool enabled = server != null && !NetworkBusy && !HasNetworkDialog;
            Button("成功 200", () => SendRequest("success"), enabled);
            Button("业务失败 409", () => SendRequest("business-error"), enabled);
            Button("服务不可用 503", () => SendRequest("unavailable"), enabled);
            GUILayout.EndHorizontal();
            Button("取消当前请求", CancelRequest, NetworkBusy);
            if (HasNetworkDialog)
            {
                GUILayout.Space(12);
                GUILayout.Label(dialogMessage, textStyle);
                GUILayout.BeginHorizontal();
                Button("重试", () => ResolveNetworkDialog(true), retryDecision != null && NetworkCode == 503);
                Button("取消 / 关闭", () => ResolveNetworkDialog(false));
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("服务仅监听 127.0.0.1 随机端口；退出播放时关闭。WebGL 不支持本机 TCP fixture。", smallStyle);
        }
        private void DrawNavigation()
        {
            Heading("UI：页面栈与页面所属窗口", "所有操作调用真实 UISystem / UIPage / UIWindow。进入背包、打开详情后返回主页，背包及其窗口应同时隐藏。重复进入、返回不应累积页面。");
            GUILayout.BeginHorizontal();
            Button("进入背包", OpenInventory);
            Button("打开详情窗口", ShowDetails);
            Button("关闭所有窗口", HideDetails);
            Button("返回主页", Back);
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
            GUILayout.Label("当前页：" + CurrentPage.Name + "\n背包在栈中：" + ui.IsPageInStack("Inventory") + "\n详情窗口：" + (WindowShowing ? "显示" : "隐藏"), textStyle);
            GUILayout.Box(CurrentPage.Name == "Home" ? "主页  /  Home\n欢迎来到开发者体验大厅" : "背包  /  Inventory\n金币 × " + Coins, buttonStyle, GUILayout.MinHeight(90));
            if (WindowShowing) GUILayout.Box("详情窗口  /  Details\n所属页面：" + window.Parent.Name, buttonStyle, GUILayout.MinHeight(70));
        }
        private void DrawUser()
        {
            Heading("用户：可替换 SDK 与完整回调", "模拟 SDK 注入 UserSystem，编辑器和桌面 Player 都能体验。登录失败后按登录重试；支付按钮只产生本地事件。UID 保存在独立示例目录，重启后保持一致。");
            GUILayout.Label("UID：" + SimulatedSDK.UID + "\n登录状态：" + (LoggedIn ? "已登录" : "未登录") + "    事件数：" + UserEventCount, textStyle);
            GUILayout.BeginHorizontal();
            Button("登录", () => Login(), !LoggedIn);
            Button("注入一次登录失败", () => Login(true), !LoggedIn);
            Button("登出", Logout, LoggedIn);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Button("支付成功", () => Pay(ShowcaseSDK.PaymentResult.Success), LoggedIn);
            Button("支付失败", () => Pay(ShowcaseSDK.PaymentResult.Failed), LoggedIn);
            Button("支付取消", () => Pay(ShowcaseSDK.PaymentResult.Canceled), LoggedIn);
            GUILayout.EndHorizontal();
            GUILayout.Label("上次事件：" + LastUserEvent + "\n模拟订单 demo-order，金额 0；不会访问渠道 SDK 或支付服务。", smallStyle);
        }
        private void DrawLua()
        {
            Heading("Lua：修改规则并读取返回值", "在独立 ToLua 状态执行一段规则脚本；修改金币与倍率后重新执行。此例无需启动旧 GameSystem，也不加载业务 Lua 包。");
            luaSource = GUILayout.TextArea(luaSource, new GUIStyle(GUI.skin.textArea) { font = font, fontSize = 16 }, GUILayout.MinHeight(110));
            Button("执行 Lua", RunLua);
            GUILayout.Label(LuaResult, textStyle);
            GUILayout.Label("需要匹配平台的 ToLua 原生插件。此例展示运行时规则变更；正式热更新还需版本清单、下载校验与回滚。", smallStyle);
        }
        public void RunLua()
        {
            try
            {
                using (var lua = new LuaInterface.LuaState())
                {
                    // Generic DoString evaluates a return value without loading Unity Lua modules.
                    LuaResult = lua.DoString<string>(luaSource, "ShowcaseRule") ?? "脚本没有返回值";
                }
                Log(LuaResult);
            }
            catch (Exception e) { LuaResult = "Lua 执行失败：" + e.Message; Log(LuaResult); }
        }
    }
}

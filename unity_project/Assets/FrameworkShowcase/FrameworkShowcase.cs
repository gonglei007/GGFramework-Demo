using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GGFramework.Core;
using GGFramework.GGTask;
using UnityEngine;
using BestHTTP;

namespace GGFramework.Demo
{
    /// <summary>Independent entry point; deliberately does not initialize GameSystem or its live services.</summary>
    public partial class FrameworkShowcase : MonoBehaviour
    {
        public enum Flow { Idle, Running, Paused }
        public struct CoinEvent { public int Amount; }
        public readonly EventBus Events = new EventBus();
        public readonly StateMachine<Flow> Machine = new StateMachine<Flow>();
        public int Coins { get; private set; }
        public int BackgroundCompletions { get; private set; }
        public int CoroutineCompletions { get; private set; }
        public bool ResourceBusy { get; private set; }
        public string ResourceStatus { get; private set; } = "尚未加载";
        public bool NetworkBusy { get; private set; }
        public int NetworkCode { get; private set; }
        public int UserEventCount { get; private set; }
        public UserSystem.Events LastUserEvent { get; private set; }
        public ShowcaseSDK SimulatedSDK { get; private set; }
        public string ServerAddress { get { return server == null ? "回环服务不可用" : server.Address; } }
        public int ActiveCount { get { return active.Count; } }
        public int InactiveCount { get { return pool.CountInactive; } }
        public ResourceObject CachedResource { get { return resources.GetObject(ResourceSystem.Tag.Other, "ShowcaseConfig"); } }
        public UIPage CurrentPage { get { return ui.GetCurrentPage(); } }
        public bool WindowShowing { get { return window != null && window.IsShowing(); } }

        private readonly List<string> messages = new List<string>();
        private readonly List<GameObject> active = new List<GameObject>();
        private GameObjectPool pool;
        private ResourceSystem resources;
        private Loxodon.Framework.Bundles.LocalResources localResources;
        private UISystem ui;
        private UserSystem user;
        private HttpNetworkSystem http;
        private ShowcaseServer server;
        private HTTPRequest currentRequest;
        private UIWindow window;
        private bool subscribed;
        private bool backgroundBusy;
        private bool coroutineBusy;
        private Action<bool> retryDecision;
        private string dialogMessage;
        private bool ready;
        private bool disposed;

        private void Awake()
        {
            // Also supports entering Play Mode repeatedly with domain reload disabled.
            SingletonLifetime.IsQuitting = false;
            MainThreadDispatcher.Initialize();
            var dispatcher = MainThreadDispatcher.Instance;
            resources = ResourceSystem.Instance;
            resources.Group = "showcase";
            localResources = new Loxodon.Framework.Bundles.LocalResources();
            var root = new GameObject("Showcase UI Root");
            root.tag = "UIRoot";
            root.transform.SetParent(transform, false);
            ui = UISystem.Instance;
            CreatePage("Home");
            CreatePage("Inventory");
            window = CreateUI<UIWindow>("Details", UISystem.UILayer.Window);
            ui.PresentPage("Home");

            var template = new GameObject("Pooled token template");
            template.transform.SetParent(transform, false);
            template.SetActive(false);
            pool = new GameObjectPool(template, transform, preload: 3, maxSize: 8);
            Machine.Configure(Flow.Running, onEnter: () => Log("FSM: 进入 Running"));
            Machine.AddTransitionGuard(Flow.Idle, Flow.Running, () => Coins >= 1);
            Machine.ChangeState(Flow.Idle);
            SubscribeCoins(true);
            user = UserSystem.Instance;
            SimulatedSDK = new ShowcaseSDK(user);
            user.ConfigureSDK(SimulatedSDK);
            user.onEvent += OnUserEvent;
            user.Init();
            http = HttpNetworkSystem.Instance;
            http.onWaiting += OnWaiting;
            http.onResponseError += OnHttpError;
            http.onDialog = OnNetworkDialog;
            try { server = new ShowcaseServer(); }
            catch (Exception e) { Log("无法启动回环服务: " + e.Message); }
            ready = true;
            Log("体验大厅已就绪。所有示例只使用本机数据与回环 HTTP。");
        }

        private void Update() { Machine.Update(Time.deltaTime); }

        public void Log(string message)
        {
            messages.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            if (messages.Count > 60) messages.RemoveAt(messages.Count - 1);
        }

        public void SubscribeCoins(bool value)
        {
            if (subscribed == value) return;
            subscribed = value;
            if (value) Events.Subscribe<CoinEvent>(OnCoins);
            else Events.Unsubscribe<CoinEvent>(OnCoins);
        }
        private void OnCoins(CoinEvent e) { Coins += e.Amount; Log("EventBus: 金币更新为 " + Coins); }
        public void PublishCoin() { Events.Publish(new CoinEvent { Amount = 1 }); }
        public bool ChangeFlow(Flow state)
        {
            bool changed = Machine.ChangeState(state);
            Log(changed ? "FSM: " + state : "FSM: 转移被拒绝（状态相同或金币不足）");
            return changed;
        }
        public int SpawnToken()
        {
            var token = pool.Get();
            active.Add(token);
            Log("Pool.Get: instanceID=" + token.GetInstanceID());
            return token.GetInstanceID();
        }
        public void ReleaseToken()
        {
            if (active.Count == 0) return;
            var token = active[active.Count - 1];
            active.RemoveAt(active.Count - 1);
            Log("Pool.Release: instanceID=" + token.GetInstanceID());
            pool.Release(token);
        }
        public void RunBackground()
        {
            if (backgroundBusy) return;
            backgroundBusy = true;
            // Singleton creation happens on Unity's main thread, not inside the worker.
            TaskSystem.Instance.QueueJob(() =>
            {
                string digest = FormatUtil.Md5Hex("GGFramework");
                int worker = Thread.CurrentThread.ManagedThreadId;
                MainThreadDispatcher.Post(() =>
                {
                    if (disposed) return;
                    backgroundBusy = false;
                    BackgroundCompletions++;
                    Log("Task → 主线程: worker=" + worker + ", main=" + Thread.CurrentThread.ManagedThreadId + ", MD5=" + digest);
                });
                return null;
            });
        }
        public void RunCoroutine()
        {
            if (coroutineBusy) return;
            coroutineBusy = true;
            CoroutineUtil.DoCoroutine(Delay(), () =>
            {
                if (disposed) return;
                coroutineBusy = false;
                CoroutineCompletions++;
                Log("CoroutineUtil: 延迟任务完成");
            });
        }
        private IEnumerator Delay() { yield return new WaitForSecondsRealtime(0.3f); }

        public void LoadResource(bool missing = false)
        {
            if (ResourceBusy) return;
            StartCoroutine(LoadConfig(missing));
        }
        private IEnumerator LoadConfig(bool missing)
        {
            if (!missing && CachedResource != null)
            {
                resources.Retain(ResourceSystem.Tag.Other, "ShowcaseConfig");
                ResourceStatus = "缓存命中；引用 +1";
                Log(ResourceStatus);
                yield break;
            }
            ResourceBusy = true;
            ResourceStatus = "异步加载中…";
            var request = localResources.LoadAssetAsync<TextAsset>(missing ? "FrameworkShowcase/Missing" : "FrameworkShowcase/Config");
            while (!request.IsDone) yield return null;
            ResourceBusy = false;
            var text = request.Exception == null ? request.Result : null;
            if (text == null) { ResourceStatus = "资源不存在；可继续加载正常资源"; Log(ResourceStatus); yield break; }
            // Cache an owned runtime copy. Never pass an imported project asset to DestroyObject.
            var owned = new TextAsset(text.text);
            resources.SetObject(resources.Group, ResourceSystem.Tag.Other, new ResourceObject("resources", "ShowcaseConfig", owned));
            resources.Retain(ResourceSystem.Tag.Other, "ShowcaseConfig");
            ResourceStatus = "GGResource LocalResources → ResourceSystem 缓存成功";
            Log(ResourceStatus);
        }
        public void ReleaseResource()
        {
            resources.DestroyObject(ResourceSystem.Tag.Other, "ShowcaseConfig");
            ResourceStatus = CachedResource == null ? "引用归零，缓存已释放" : "已释放一个引用，资源仍可使用";
            Log(ResourceStatus);
        }

        private T CreateUI<T>(string name, UISystem.UILayer layer) where T : UIBase
        {
            var go = new GameObject(name);
            go.transform.SetParent(GameObject.FindWithTag("UIRoot").transform.Find(layer.ToString()), false);
            var component = go.AddComponent<T>();
            component.Name = name;
            go.SetActive(false);
            ui.SetUIBase(layer, name, component);
            component.OnLoad();
            return component;
        }
        private void CreatePage(string name) { CreateUI<UIPage>(name, UISystem.UILayer.Page); }
        public void OpenInventory()
        {
            if (!ui.IsPageInStack("Inventory")) ui.PresentPage("Inventory");
            Log("UISystem: 当前页=" + CurrentPage.Name);
        }
        public void Back()
        {
            if (CurrentPage != null && CurrentPage.Name != "Home") ui.DismissPage();
            Log("UISystem: 返回 " + (CurrentPage == null ? "空栈" : CurrentPage.Name));
        }
        public void ShowDetails() { ui.ShowWindow("Details"); Log("Window 归属=" + window.Parent.Name); }
        public void HideDetails() { ui.HideAllWindows(); Log("所有窗口已关闭"); }

        public void SendRequest(string route)
        {
            if (NetworkBusy || server == null) return;
            NetworkBusy = true;
            NetworkCode = 0;
            Log("HTTP POST " + route);
            currentRequest = http.PostWebRequest(server.Address, route, "{}", HttpNetworkSystem.ExceptionAction.ConfirmRetry, response =>
            {
                if (disposed) return;
                NetworkBusy = false;
                NetworkCode = Convert.ToInt32(response["code"]);
                Log("HTTP JSON: " + response);
            });
        }
        private void OnWaiting(bool value) { NetworkBusy = value; }
        private void OnHttpError(int code) { NetworkCode = code; Log("HTTP 状态码=" + code); }
        private void OnNetworkDialog(string title, string message, bool canRetry, Action<bool> decision)
        {
            dialogMessage = message;
            retryDecision = canRetry ? decision : null;
            NetworkBusy = false;
            Log("网络反馈: " + message);
        }
        public bool HasNetworkDialog { get { return dialogMessage != null; } }
        public void ResolveNetworkDialog(bool retry)
        {
            var decision = retryDecision;
            retryDecision = null;
            dialogMessage = null;
            Log(retry ? "手动重试" : "取消重试");
            decision?.Invoke(retry);
        }
        public void CancelRequest()
        {
            currentRequest?.Abort();
            NetworkBusy = false;
            ResolveNetworkDialog(false);
        }
        private void OnUserEvent(UserSystem.States state, UserSystem.Events evt, object value)
        {
            LastUserEvent = evt;
            UserEventCount++;
            Log("UserSystem: " + state + " / " + evt + " / " + value);
        }
        public void Login(bool fail = false) { SimulatedSDK.FailNextLogin = fail; user.Login(); }
        public void Logout() { user.SDKLogout(); }
        public bool LoggedIn { get { return user.HasLogined(); } }
        public void Pay(ShowcaseSDK.PaymentResult result)
        {
            SimulatedSDK.NextPayment = result;
            user.SDKPay(new UserSystem.Order { id = "demo-order", productID = "demo-coins", price = 0, name = "模拟金币" });
        }

        private void OnDestroy()
        {
            disposed = true;
            StopAllCoroutines();
            currentRequest?.Abort();
            server?.Dispose();
            localResources?.Dispose();
            if (user != null) user.onEvent -= OnUserEvent;
            if (http != null)
            {
                http.onWaiting -= OnWaiting;
                http.onResponseError -= OnHttpError;
                http.onDialog = null;
            }
            Events.Clear();
            if (pool != null) { foreach (var token in active) pool.Release(token); pool.Clear(); }
            if (resources != null)
                while (CachedResource != null) resources.DestroyObject(ResourceSystem.Tag.Other, "ShowcaseConfig");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MonsterLove.StateMachine;
using LuaFramework;
using SimpleJson;
using CsAsync;
using GGFramework;

/**
 * 框架层的游戏系统。负责整个游戏的主线流程控制，不处理具化的游戏逻辑、配置等等。
 * 暂定为全球同服的桌游类框架。
 * 主要流程包括：初始化、数据加载、TODO......
 */
public class GameSystem : Singleton<GameSystem>
{
    public const string ResourceGroup_Framework = "framework"; 
    public Action Event_Update;

    // 游戏系统的状态枚举
    public enum States
    {
        None = 0,
        Init = 1000,            // 初始化各个系统
        InitNetwork,            // 初始化网络
        Extract,                // 解包
        HotfixTables,           // 热更数据表
        HotfixAssets,           // 热更资源
        InitFramework,          // 初始化游戏框架
        LoadTables,             // 加载数据表
        StartStages = 2000,     // 开始游戏各阶段。
    }

    private StateMachine<States> fsm;
    private bool frameworkLoaded = false;

    private void Awake()
    {
        // 监听APP异常
        Application.logMessageReceived += (string logString, string stackTrace, LogType type) =>
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                //TODO: GL - 捕获产品异常，做相应的处理。比如：发后台日志；弹消息框？
                // Init状态完成（UI初始化）之后才能调用弹框。
                if (fsm != null && fsm.State > States.Init) {
                    //UISystem.Instance.PopDialog("client_error", logString);
                }
            }
        };
        Global.Init();
        GameDebugger.Instance.Init();
        BetterStreamingAssets.Initialize();
        NativeLuaBridge.Instance.Init();
        UserSystem.Instance.Init();

        // 初始化状态机。此处为游戏的起始点。
        fsm = StateMachine<States>.Initialize(this, States.None);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // Start is called before the first frame update
    void Start()
    {
        TaskSystem.Instance.Init();
        //初始化Http网络模块
        HttpNetworkSystem instance = HttpNetworkSystem.Instance;
        // 开始状态机。
        //fsm.ChangeState(States.Init);
        //fsm.Changed += (States state) => {
        //    GameDebugger.sPushLog("[lua]State Entered->" + state.ToString());
        //    Util.CallMethod("Game", "OnStateChanged", state);          //状态切换
        //};
        //fsm.Exited += (States state) => {
        //    GameDebugger.sPushLog("[lua]State Exited->" + state.ToString());
        //    Util.CallMethod("Game", "OnStateExited", state);          //状态切换
        //};
    }

    // Update is called once per frame
    void Update()
    {
        NetworkSystem.Instance.OnUpdate();
        if (HttpNetworkSystem.Instance != null)
        {
            HttpNetworkSystem.Instance.Update();
        }
        if (DownloadSystem.Instance != null) {
            DownloadSystem.Instance.Update();
        }
        this.Event_Update?.Invoke();
    }

    public static void PostSystemWebRequest(string httpAddress, string command, JsonObject paramObject, Action<JsonObject> callback = null)
    {
        HttpNetworkSystem.Instance.PostWebRequest(httpAddress, command, paramObject, HttpNetworkSystem.ExceptionAction.ConfirmRetry, callback);
    }

    public static void PostSystemServiceRequest(string service, string command, JsonObject paramObject, Action<JsonObject> callback = null)
    {
        HttpNetworkSystem.Instance.PostServiceRequest(service, command, paramObject, HttpNetworkSystem.ExceptionAction.ConfirmRetry, callback);
    }

    /// <summary>
    /// 根据分组返回本地的数据版本号：table、asset。
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public static string GetLocalVersion(string group) {
        return PackageSystem.LoadVersionValue(group);
    }

    /// <summary>
    /// 准备网络。请求客户端所需的一些信息
    /// </summary>
    /// <param name="callback"></param>
    private void PrepareNetwork(Action callback) {
        callback();return;
        JsonObject paramObject = new JsonObject();
        paramObject.Add("deviceId", Global.GetDeviceUID());
        paramObject.Add("channel", Global.GetChannel());
        paramObject.Add("version", Global.GetClientVersion());
        GameSystem.PostSystemWebRequest(Global.productAddress, "getServiceList", paramObject, (JsonObject response) =>
        {
            Debug.Log("http post:getServiceList:" + response.ToString());
            int code = Convert.ToInt32(response["code"]);

            if (NetworkConst.CODE_OK == code)
            {
                // 设置屏幕调试信息开关。
                if (response.ContainsKey("DebugEnable"))
                {
                    GameDebugger.Enable = true;// (Convert.ToInt32(response["DebugEnable"]) > 0);
                }
                // 设置服务地址。
                if (response.ContainsKey("productService")) {
                    Global.productAddress = response.ContainsKey("productService").ToString();
                }
                // 更新服务
                try {
                    JsonArray serviceList = response["services"] as JsonArray;
                    foreach (JsonObject service in serviceList) {
                        string protocol = service["protocol"].ToString();
                        if (protocol == "http" || protocol == "https")
                        {
                            HttpNetworkSystem.Instance.SetServiceUrl(service["type"].ToString(), service["address"].ToString());
                        }
                        else if(protocol == "tcp"){
                            NetworkSystem.Instance.SetServiceInfo(service["type"].ToString(), service["address"].ToString());
                        }
                    }
                }
                catch (Exception e) {
                    Debug.LogError("Request getServiceList failed!!!" + e.ToString());
                    throw e;
                }
                // 更新提醒。
                try
                {
                    string remoteVersion = null;
                    string downloadUrl = null;
                    bool forceUpdate = false;
                    if (response.ContainsKey("clientVersion"))
                    {
                        JsonObject clientVersionConfig = response["clientVersion"] as JsonObject;
                        if (clientVersionConfig.ContainsKey("version"))
                        {
                            remoteVersion = clientVersionConfig["version"].ToString();
                        }
                        if (clientVersionConfig.ContainsKey("downUrl"))
                        {
                            downloadUrl = clientVersionConfig["downUrl"].ToString();
                        }
                        if (clientVersionConfig.ContainsKey("isForceUpdate"))
                        {
                            forceUpdate = Convert.ToInt32(clientVersionConfig["isForceUpdate"]) == 1;
                        }

                        if (remoteVersion != null && downloadUrl != null)
                        {
                            CheckAppUpdate(remoteVersion, downloadUrl, forceUpdate, (bool needUpdate) =>
                            {
                                if (!needUpdate)
                                {
                                    callback();
                                    return;
                                }
                            });
                        }
                        else {
                            callback();
                        }
                    }
                    else {
                        callback();
                        //GL - 测试代码：什么也不做。
                        //TODO: GL - 下面代码测试完删除！
                        //CheckAppUpdate(remoteVersion, downloadUrl, forceUpdate, (bool needUpdate) =>
                        //{
                        //    if (!needUpdate)
                        //    {
                        //        callback();
                        //        return;
                        //    }
                        //});
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw e;
                }
                finally {
                    // 有异常不影响流程推进。
                }
            }
            else
            {
                string message = response["msg"].ToString();
                UISystem.Instance.PopDialog("Network Error!", message, (int)UIDialog.Option.Yes, (UIDialog.Option option) => {
                    if (option == UIDialog.Option.Yes)
                    {
                        ChangeState(States.InitNetwork);
                    }
                });
            }
        });
    }

    /// <summary>
    /// 检查APP是否需要更新。如果要更新，提醒用户去下载更新。
    /// </summary>
    /// <param name="remoteVersion"></param>
    /// <param name="downloadUrl"></param>
    /// <param name="forceUpdate"></param>
    private void CheckAppUpdate(string remoteVersion, string downloadUrl, bool forceUpdate, Action<bool> callback) {
        Debug.Assert(remoteVersion != null, "版本号字段不能为null！");
        Debug.Assert(downloadUrl != null, "下载地址不能为null！");
        int newVersionInt = Convert.ToInt32(remoteVersion.Replace(".", ""));
        string localVersion = Global.GetClientVersion();
        int localVersionInt = Convert.ToInt32(localVersion.Replace(".", ""));
        if (newVersionInt != localVersionInt)
        {
            if (forceUpdate)
            {
                UISystem.Instance.PopDialog(TextSystem.GetText("app_update"), TextSystem.GetText("force_update_desc"), (int)UIDialog.Option.Yes, (UIDialog.Option option) =>
                {
                    DoAppUpdate(downloadUrl);
                    callback(true);
                });
            }
            else
            {
                UISystem.Instance.PopDialog(TextSystem.GetText("app_update"), TextSystem.GetText("unforce_update_desc"), (int)(UIDialog.Option.Yes | UIDialog.Option.No), (UIDialog.Option option) =>
                {
                    if (option == UIDialog.Option.Yes)
                    {
                        DoAppUpdate(downloadUrl);
                        callback(true);
                    }
                    else
                    {
                        callback(false);
                    }
                });
            }
        }
        else {
            callback(false);
        }
    }

    // 打开更新链接，关闭进程。
    private void DoAppUpdate(string downloadUrl) {
        Application.OpenURL(downloadUrl);
        Application.Quit();
    }

    /**
     * 加载通用资源
     */
    private void LoadFrameworkResources(Action<Exception> callback)
    {
        GameDebugger.sPushLog(">Start loading framework resources...");
        Debug.Assert(callback != null, "callback will not be null!!!");
        Waterfall waterfall = new Waterfall();
        waterfall.AddTask((TaskCallback tcallback) =>
        {
            // 加载本地化文本
            TextSystem.Load((bool result) => {
                if (result == false) {
                    GameDebugger.sPushLog("Load text(localization) failed!");
                }
                tcallback.DoCallback(null);
            });
        });
        //waterfall.AddTask((TaskCallback tcallback) =>
        //{
        //    // 加载资源配表
        //    GameDebugger.sPushLog("Load Framework Tables");
        //    DataSystem.LoadData("FrameworkResources", (Dictionary<string, JsonObject> tableData) => {
        //        tcallback.DoCallback(null);
        //    });
        //});
        waterfall.Start((Exception e) =>
        {
            // 加载UI资源
            UISystem.Instance.LoadFrameworkUIs((Exception le)=> {
                GameDebugger.sPushLog(">Finish loading framework resources...");
                callback(le);
            });
        });
    }

    private bool InitFramework() {
        UISystem.Instance.CreateFrameworkUIs();
        // 框架UI创建出来后，让网络模块支持UI绑定。
        HttpNetworkSystem.Instance.onDialog = (string title, string message, bool retry, Action<bool> callback)=> {
            UISystem.Instance.PopDialog(title, message, (int)UIDialog.Option.Yes, (UIDialog.Option option) => {
                //TODO: GL - 根据异常响应类型，决定做出哪种回调。
                if (option == UIDialog.Option.Yes)
                {
                    // 重试
                    callback(retry);
                }
                else {
                    // 忽略
                    callback(retry);
                }
            });
        };
        HttpNetworkSystem.Instance.onWaiting = (bool show)=> {
            UISystem.Instance.ShowWaiting(show);
        };
        DownloadSystem.Instance.onDialog = (string title, string message, bool retry, Action<bool> callback) => {
            UISystem.Instance.PopDialog(title, message, (int)UIDialog.Option.Yes, (UIDialog.Option option) => {
                //TODO: GL - 根据异常响应类型，决定做出哪种回调。
                if (option == UIDialog.Option.Yes)
                {
                    // 重试
                    callback(retry);
                }
                else
                {
                    // 忽略
                    callback(retry);
                }
            });
        };

        PackageSystem.Instance.onDialog = (string title, string message, bool forceConfirm, Action<bool> callback) => {
            int option = (int)(UIDialog.Option.Yes | UIDialog.Option.No);
            if (forceConfirm) {
                option = (int)UIDialog.Option.Yes;
            }
            UISystem.Instance.PopDialog(title, message, option, (UIDialog.Option response) => {
                //TODO: GL - 根据异常响应类型，决定做出哪种回调。
                if (response == UIDialog.Option.Yes)
                {
                    // 重试
                    callback(true);
                }
                else
                {
                    // 忽略
                    callback(false);
                }
            });
        };
        PackageSystem.Instance.OnProcess = (int progress, int total, PackageSystem.RunState state) =>
        {
            //UILoading uiLoading = UISystem.Instance.GetLoadingUI();
            //uiLoading.SetPercent((float)progress/(float)total);
            //uiLoading.SetInfo(string.Format("下载文件中......|已下载:{0}\t总文件数:{1}", progress, total));
        };

        return true;
    }

    public States GetCurrentState() {
        Debug.Assert(fsm != null, "this.fsm could not be null!");
        return fsm.State;
    }

    /// <summary>
    /// 改变游戏的状态。
    /// </summary>
    /// <param name="state"></param>
    public void ChangeState(States state) {
        GameDebugger.sPushLog("准备切换状态:" + state.ToString());
        fsm.ChangeState(state);
    }

    // ----------------------------------- 状态处理 ----------------------------------- //

    private void None_Update() {
        ChangeState(States.Init);
    }

    private void Init_Enter() {
        Debug.Log("Init_Enter"); //DataSystem.LoadAllData("", null, null);return;
        //GL - 注意下面这些系统的初始化顺序，不能随意调整！！！
        ExtractSystem.Instance.Init(this.OnExtractFinished);
        // 加载Framework的默认资源
        ResourceSystem.Instance.Group = ResourceGroup_Framework;
        ResourceSystem.Instance.Init();
        LoadFrameworkResources((Exception e)=> {
            if (e != null) {
                Debug.LogError(e);
                return;
            }
            //Debug.LogWarning("----------------------");
            InitFramework();
            //UISystem.Instance.CreateFrameworkUIs();
            LoadingHandler.Instance.Init();
            //打开加载页面
            UISystem.Instance.ShowLoading("UILoading");

            //bool resourceSystemInitialed = ResourceSystem.Instance.Init();
            //if (resourceSystemInitialed == false)
            //{
            //    throw new Exception("ResourceSystem init failed!!!");
            //}
            StageSystem.Instance.onEnterStage += (string stage) =>
            {
                ResourceSystem.Instance.Group = stage;
            };
            frameworkLoaded = true;
        });
    }

    private void Init_Update()
    {
        if (frameworkLoaded) {
            // 创建/实例化Framework游戏对象。
            ChangeState(States.InitNetwork);
        }
    }

    private void Init_Exit()
    {
        Debug.Log("Init_Exit");
    }

    private void InitNetwork_Enter()
    {
        PrepareNetwork(()=> {
            // 网络准备好了之后开始释放资源。
            ChangeState(States.Extract);
        });
    }

    private void InitNetwork_Update()
    {
    }

    private void InitNetwork_Exit()
    {
    }

    private void Extract_Enter() {
        Debug.Log("Extract_Enter");
        ExtractSystem.Instance.Run1();
    }

    private void Extract_Update()
    {
    }

    private void Extract_Exit()
    {
        Debug.Log("Extract_Exit");
    }

    private void HotfixTables_Enter()
    {
        PackageSystem.Instance.Run("table", (bool updated, Exception e) =>
        {
            if (e != null)
            {
                //TODO: GL - 弹出提示框，让用户重试。
            }
            else
            {
                //TODO: GL - 更新UI信息
                //ChangeState(States.InitFramework);
                ChangeState(States.HotfixAssets);
            }
        });
    }

    private void HotfixTables_Update()
    {
    }

    private void HotfixTables_Exit()
    {
    }

    private void HotfixAssets_Enter()
    {
        PackageSystem.Instance.Run("asset", (bool updated, Exception e) =>
        {
            if (e != null)
            {
                //TODO: GL - 弹出提示框，让用户重试。
            }
            else
            {
                //TODO: GL - 更新UI信息
                ChangeState(States.InitFramework);
            }
        });
    }

    private void HotfixAssets_Update()
    {
    }

    private void HotfixAssets_Exit()
    {
    }

    private void InitFramework_Enter()
    {
        // 初始化Lua框架及系统
        LuaSystem.Instance.Init();
#if XXX
        ResourceSystem.Instance.Init();
        ResourceSystem.Instance.Reset();
        ResourceSystem.Instance.AddResourceItem(new ResourceItem(null, ResourceSystem.Tag.UI, Global.FrameworkBundleName, 0));
        ResourceSystem.Instance.StartLoading(
            (Exception e) => {
                // 创建/实例化Framework游戏对象。
                // 先销毁默认的资源。
                UISystem.Instance.DestroyFrameworkUIs();
                UISystem.Instance.CreateFrameworkUIs();
                //InitFramework();
                // 清理Framework资源。
                ResourceSystem.Instance.ClearObjects(ResourceGroup_Framework);
                ChangeState(States.LoadTables);
            },
            (Exception e) => {
                //ChangeState(States.Run);
            });
#else
        ChangeState(States.LoadTables);
#endif
    }

    private void InitFramework_Update()
    {
    }

    private void InitFramework_Exit()
    {
    }

    private void LoadTables_Enter()
    {
        // 热更完之后，加载所有的数据表。
        DataSystem.LoadAllData("", (string data) => { }, (Exception e) => {
            TextSystem.Load((bool result) => {
                if (result == false)
                {
                    GameDebugger.sPushLog("Load text(localization) failed!");
                }
                ChangeState(States.StartStages);
            });
        });
    }

    private void LoadTables_Update()
    {
    }

    private void LoadTables_Exit()
    {
    }

    private void StartStages_Enter() {
        StageSystem.Instance.Begin();
    }

    private void StartStages_Update() {
        //StageSystem.Instance.Update();
    }

    private void StartStages_Exit() {
    }

    private void OnExtractFinished() {
        ChangeState(States.HotfixTables);
    }

    private void OnPackageFinished(bool updated, Exception e) {
        if (e != null)
        {
            //TODO: GL - 弹出提示框，让用户重试。
        }
        else {
            //TODO: GL - 更新UI信息
            ChangeState(States.InitFramework);
        }
    }
}

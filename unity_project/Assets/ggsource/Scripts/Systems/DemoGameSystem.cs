using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MonsterLove.StateMachine;
using LuaFramework;
using SimpleJson;
using CsAsync;

public class DemoGameSystem : Singleton<DemoGameSystem>
{
    public const string ResourceGroup_Framework = "framework";
    public Action Event_Update;

    // 游戏系统的状态枚举
    public enum States
    {
        None = 0,
        Init = 1000,            // 初始化各个系统
        InitNetwork,            // 初始化网络
        HotfixTables,           // 热更数据表
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
                if (fsm != null && fsm.State > States.Init)
                {
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
        fsm.ChangeState(States.Init);
        fsm.Changed += (States state) =>
        {
            GameDebugger.sPushLog("[lua]State Entered->" + state.ToString());
            Util.CallMethod("Game", "OnStateChanged", state);          //状态切换
        };
        fsm.Exited += (States state) =>
        {
            GameDebugger.sPushLog("[lua]State Exited->" + state.ToString());
            Util.CallMethod("Game", "OnStateExited", state);          //状态切换
        };
    }

    // Update is called once per frame
    void Update()
    {
        NetworkSystem.Instance.OnUpdate();
        if (HttpNetworkSystem.Instance != null)
        {
            HttpNetworkSystem.Instance.Update();
        }
        if (DownloadSystem.Instance != null)
        {
            DownloadSystem.Instance.Update();
        }
        this.Event_Update?.Invoke();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonsterLove.StateMachine;
using LuaFramework;

/// <summary>
/// 阶段系统。
/// 资源加载和实例化等都是按照Stage来控制的。
/// 常规（预制）的阶段包括：Home、Lobby、Room。
/// </summary>
public class StageSystem : Singleton<StageSystem>
{
    public const string Home = "home";
    public const string Lobby = "lobby";
    public const string Room = "room";
    public class StageData {
        public StageData(string name, string luaEntry, string luaUpdateModule) {
            this.name = name;
            this.luaEntry = luaEntry;
            this.luaUpdateModule = luaUpdateModule;
        }
        public string name = null;
        public string luaEntry = null;
        public string luaUpdateModule = null;
    };

    public enum States {
        None = 0,
        LoadLua,
        LoadResource,
        InitStage,
        Run,
    };

    public Action<string> onEnterStage = null;
    public Action<string> onExitStage = null;
    private Dictionary<string, StageData> stages = new Dictionary<string, StageData>(10);
    private string currentStage = null;
    private string previousStage = null;
    private bool showLoading = false;
    private StateMachine<States> fsm = null;

    public string CurrentStage {
        get {
            return currentStage;
        }
    }

    public string PreviousStage {
        get {
            return previousStage;
        }
    }

    void Awake()
    {
        Init();
        onExitStage += (string stage) => {
            ResourceSystem.Instance.ClearResources();
        };
        //fsm = StateMachine<States>.Initialize(this);
        fsm = StateMachine<States>.Initialize(this, States.None);
        fsm.Changed += (States state) => {
            GameDebugger.sPushLog("[lua]State Entered->" + state.ToString());
            try
            {
                if (currentStage != null)
                {
                    StageData stageData = stages[currentStage];
                    if (stageData != null && stageData.luaUpdateModule != null)
                    {
                        Util.CallMethod(stageData.luaUpdateModule, "OnStateChanged", state);
                    }
                }
                //Util.CallMethod("Game", "OnStateChanged", state);          //状态切换
            }
            catch (Exception e) {
                Debug.LogError(new Exception(currentStage + "-" + e.ToString()));
            }
        };
        fsm.Exited += (States state) => {
            GameDebugger.sPushLog("[lua]State Exited->" + state.ToString());
            try
            {
                if (currentStage != null)
                {
                    StageData stageData = stages[currentStage];
                    if (stageData != null && stageData.luaUpdateModule != null)
                    {
                        Util.CallMethod(stageData.luaUpdateModule, "OnStateExited", state);
                    }
                }
                //Util.CallMethod("Game", "OnStateExited", state);          //状态切换
            }
            catch (Exception e) {
                Debug.LogError(new Exception(currentStage + "-" + e.ToString()));
            }
        };
    }

    public void Init()
    {
        // 初始（默认）Stages
        //TODO: GL - 从配表获取入口信息。
        AddStage(Home, "Stage/HomeEntry", "HomeEntry");
        AddStage(Lobby, "Stage/LobbyEntry", "LobbyEntry");
        AddStage(Room, "Stage/RoomEntry", "RoomEntry");
    }

    public void Begin()
    {
        //TODO: GL - 从配表获取入口信息。
        ChangeStage(Home, true);
    }

    public void Begin(string stageName) {
        ChangeStage(stageName, true);
    }

    /// <summary>
    /// 如有新的配置，替换掉老的。
    /// </summary>
    /// <param name="name"></param>
    /// <param name="luaEntry"></param>
    public void AddStage(string name, string luaEntry, string luaUpdateModule) {
        stages[name] = new StageData(name, luaEntry, luaUpdateModule);
    }

    public bool ContainStage(string name) {
        return stages.ContainsKey(name);
    }

    /// <summary>
    /// TODO: 协程处理！！！
    /// </summary>
    /// <param name="stage"></param>
    public void ChangeStage(string stage, bool showLoading = false) {
        this.showLoading = showLoading;
        if (currentStage == stage) {
            GameDebugger.sPushLog("已在此Stage中了:" + stage);
            return;
        }
        previousStage = currentStage;
        GameDebugger.sPushLog("开始Stage:" + stage);
        ChangeState(States.None, ()=> {
            if (onExitStage != null && currentStage != null)
            {
                onExitStage(currentStage);
            }
            currentStage = stage;
            EnterStage(stage);
            if (ContainStage(stage))
            {
                if (onEnterStage != null)
                {
                    onEnterStage(stage);
                }
            }
        });
    }

    /// <summary>
    /// 开始一个阶段（启动一个Stage的状态机）。
    /// </summary>
    /// <param name="stage"></param>
    public void EnterStage(string stage) {
        if (ContainStage(stage)) {
            //fsm = StateMachine<States>.Initialize(this, States.None);
            fsm.ChangeState(States.LoadLua);
        }
    }

    public void ExitStage(string stage)
    {
    }

    public States GetCurrentState()
    {
        Debug.Assert(fsm != null, "this.fsm could not be null!");
        if(fsm == null || fsm.State == default)
        {
            return States.None;
        }
        return fsm.State;
    }

    public void ChangeState(States state, Action onStateChanged = null)
    {
        CoroutineUtil.DoCoroutine(DoChangeState(state, onStateChanged));
    }

    private IEnumerator DoChangeState(States state, Action onStateChanged = null) {
        GameDebugger.sPushLog(string.Format("切换Stage状态:{0}-{1}", currentStage, state.ToString()));
        yield return new WaitForEndOfFrame();
        if (fsm != null)
        {
            fsm.ChangeState(state);
        }
        if (onStateChanged != null) {
            onStateChanged();
        }
    }

    private void None_Enter()
    {
        //ChangeState(States.LoadLua);
    }

    private void None_Update()
    {
    }

    private void None_Exit()
    {
    }

    private void LoadLua_Enter()
    {
        try
        {
            if (showLoading)
            {
                UISystem.Instance.ShowLoading("UILoading");
            }
            if (stages.ContainsKey(currentStage))
            {
                StageData stageData = stages[currentStage];
                LuaSystem.Instance.LaunchLua(stageData.luaEntry);
                // 在lua入口代码中，设置各种程序配置，比如：要加载的资源的配置。
                if (stageData != null && stageData.luaUpdateModule != null)
                {
                    Util.CallMethod(stageData.luaUpdateModule, "Init");
                }
            }
            ChangeState(States.LoadResource);
        }
        catch (Exception e) {
            string errorInfo = "Not call Begin() method yet!!! | " + e.ToString();
            Debug.LogError(e);
        }
    }

    private void LoadLua_Update()
    {
    }

    private void LoadLua_Exit()
    {
    }

    private void LoadResource_Enter()
    {
        // 调用资源加载
        ResourceSystem.Instance.StartLoading(
            (Exception e)=> {
                // 初始化Stage内容。
                ChangeState(States.InitStage);
            },
            (Exception e) => {
                //ChangeState(States.Run);
            });
    }

    private void LoadResource_Update()
    {
        // 如果初始的（Stage）资源加载完毕，就要开始实例化对象。
        // ChangeState(States.InitStage);
    }

    private void LoadResource_Exit()
    {
    }

    private void InitStage_Enter()
    {
        // 实例化资源
        // ChangeState(States.Run);
    }

    private void InitStage_Update()
    {
    }

    private void InitStage_Exit()
    {
    }

    private void Run_Enter()
    {
        if (showLoading)
        {
            UISystem.Instance.HideLoading();
        }
    }

    private void Run_Update() {
        //Debug.LogWarningFormat("[X]->stage:{0} state:{1}", currentStage, fsm.State.ToString());
        // 对应的Stage执行对应的Lua脚本Update。
        if (currentStage != null) {
            StageData stageData = stages[currentStage];
            if (stageData != null && stageData.luaUpdateModule != null) {
                Util.CallMethod(stageData.luaUpdateModule, "Update", Time.deltaTime);
            }
        }
    }

}

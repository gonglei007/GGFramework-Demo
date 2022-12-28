//#define LOGIN_FAILED_DEBUG
//#define QUICK_SDK
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaInterface;
using MonsterLove.StateMachine;
using SimpleJson;

/// <summary>
/// 用于对接SDK的用户系统。包括登录、支付等等。
/// </summary>
public class UserSystem : Singleton<UserSystem>
{
    public enum States
    {
        None,
        SDKIniting,    // SDK初始化中
        SDKInited,     // SDK初始化完成
        SDKLogining,   // SDK账户登录中
        SDKLogined,    // SDK账户已登录
        //ServerAuthing,    // 游戏服授权中
        //ServerAuthed,     // 游戏服已授权
        //ServerLogining,   // 游戏服登录
        //ServerLogined,    // 游戏服已登录
    }

    public enum Events
    {
        SDKInitSuccess,
        SDKInitFailed,
        SDKLoginSuccess,
        SDKLoginFailed,
        SDKLogoutSuccess,
        SDKPaySuccess,
        SDKPayFailed,
        SDKPayCancel,
        //ServerAuthSuccess,
        //ServerAuthFailed,
        //ServerLoginSuccess,
        //ServerLoginFailed,
    }

    public class Event {
        public Event(Events e, object v) {
            this.e = e;
            this.v = v;
        }
        public Events e;
        public object v;
    };

    public class User {

        public User() {
#if UNITY_EDITOR
            isDev = true;
#endif
        }

        public User(string uid, string token, string account, string channel) {
            SetUser(uid, token, account, channel);
#if UNITY_EDITOR
            isDev = true;
#endif
        }

        public void SetUser(string uid, string token, string account, string channel) {
            this.uid = uid;
            this.token = token;
            this.account = account;
            this.channel = channel;
        }

        public string uid = null;
        public string token = null;
        public string account = null;
        public string channel = null;
        public bool isDev = false;
    };

    public class Order {
        public Order()
        {
#if UNITY_EDITOR
            isDev = true;
#endif
        }

        public string id = null;        // 订单ID
        public string productID = null; // 商品ID
        public string name = "商品x";      // 商品名称
        public string desc = "";      // 商品描述
        public float price = 1.0f;      // 商品价格
        public int amount = 1;          // 购买商品数量
        public int number = 1;          // 此商品的资源（元宝）数量
        public string url = "";       // 回调接口
        public string extra = "";     // 扩展数据

        public bool isDev = false;      // 是否为开发版

        public string GetJsonString() {
            JsonObject jsonObject = new JsonObject();
            jsonObject["id"] = this.id;
            jsonObject["productID"] = this.productID;
            jsonObject["name"] = this.name;
            jsonObject["desc"] = this.desc;
            jsonObject["price"] = this.price;
            jsonObject["amount"] = this.amount;
            jsonObject["number"] = this.number;
            jsonObject["url"] = this.url;
            jsonObject["extra"] = this.extra;
            return jsonObject.ToString();
        }
    };
    public class UserData
    {
        public enum Type
        {
            TNone = -1,
            TCreateRole,
            TEnterGame,
            TLevelUp,
        };

        public Type type = Type.TNone;
        public string roleID = "";
        public string roleName = "";
        public string roleLevel = "1";
        public long roleCreateTime = 0;
        public int vipLevel = 0;
        public string groupName = "";
        public int serverID = 0;
        public string serverName = "";

        public UserData()
        {
        }

        public UserData(UserData.Type type)
        {
            this.type = type;
        }

        public string GetJsonString()
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject["type"] = (int)this.type;
            jsonObject["roleID"] = this.roleID;
            jsonObject["roleName"] = this.roleName;
            jsonObject["roleLevel"] = this.roleLevel;
            jsonObject["roleCreateTime"] = this.roleCreateTime;
            jsonObject["vipLevel"] = this.vipLevel;
            jsonObject["groupName"] = this.groupName;
            jsonObject["serverID"] = this.serverID;
            jsonObject["serverName"] = this.serverName;
            return jsonObject.ToString();
        }
    };


    public Action<string, string, bool, Action<bool>> onDialog = null;
    public Action<States, Events, object> onEvent;
    public LuaFunction onEventX;

    private StateMachine<States> fsm;
    private Dictionary<States, Event> stateEvents = new Dictionary<States, Event>();
    //private Action loginCallback = null;
    private bool readyLogin = false;
    public User user = null;

    public User UserInfo{
        get {
            return user;
        }
    }

    private ISDK sdk = null;

    public ISDK SDK {
        get {
            return this.sdk;
        }
    }

    private void Awake()
    {
        fsm = StateMachine<States>.Initialize(this, States.None);
        fsm.Changed += (States state) =>
        {
            Debug.LogFormat("进入状态:{0}", state.ToString());
            if (stateEvents.ContainsKey(state)) {
                Event eventItem = stateEvents[state];
                TriggerEvent(state, eventItem.e, eventItem.v);
            }
        };
        sdk = SDKFactory.CreateSDK(SDKFactory.SDKTarget.NativeSDK);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //------------------ 操作 --------------------
    public void Init()
    {
        readyLogin = false;
    }

    /// <summary>
    /// 登录SDK。完成所有验证和登录流程。
    /// </summary>
    public void Login()
    {
        readyLogin = true;
        switch (fsm.State) {
            case States.None:
                //CallSDKInitSuccess("Editor init success!");
                ChangeState(States.SDKIniting);
                break;
            case States.SDKIniting:
                SDKInit();
                break;
            case States.SDKInited:
            case States.SDKLogining:
                SDKLogin();
                break;
            default:
                break;
        }
    }

    public bool HasLogined()
    {
        return fsm.State == States.SDKLogined;
    }

    public void SDKInit()
    {
        if (sdk != null)
        {
            sdk.Init();
        }

#if UNITY_EDITOR
        CallSDKInitSuccess("Editor init success!");
#endif
    }

    public void SDKLogin() 
    {
        if (sdk != null)
        {
            sdk.Login();
        }
#if UNITY_EDITOR
        CallSDKLoginSuccess(new User(UserSystem.LoadUID(), "", "", Global.GetChannel()));
#endif
    }

    public void SDKLogout() {
        sdk.Logout();
    }

    public void SDKQuit() {
        sdk.Quit();
    }

    public void SDKPay(Order order, UserData userData=null) {
        sdk.Pay(order, userData);
#if UNITY_EDITOR
        CallSDKPaySuccess("");
#endif
    }

    //------------------ SDK回调处理 --------------------
    public void SetEvent(States state, Event e) {
        stateEvents[state] = e;
    }

    public void TriggerEvent(States state, Events e, object param) {
        if (onEvent != null)
        {
            onEvent(state, e, param);
        }
        if (onEventX != null)
        {
            onEventX.Call(state, e, param);
        }
    }

    public void CallSDKInitSuccess(string ret)
    {
        Debug.Log("初始化成功:" + ret);
        SetEvent(States.SDKLogining, new Event(Events.SDKInitSuccess, ret));
        if (readyLogin) {
            UserSystem.Instance.ChangeState(UserSystem.States.SDKLogining);
            //TriggerEvent(States.SDKIniting, Events.SDKInitSuccess, ret);
        }
    }

    public void CallSDKInitFailed(string ret)
    {
        Debug.LogWarning("初始化失败:" + ret);
        SetEvent(States.SDKIniting, new Event(Events.SDKInitFailed, ret));
        UserSystem.Instance.ChangeState(UserSystem.States.SDKIniting, 1.0f);
        //TriggerEvent(States.SDKIniting, Events.SDKInitFailed, ret);
    }

    static int failedCount = 2;
    public void CallSDKLoginSuccess(User ret)
    {
#if LOGIN_FAILED_DEBUG
        if (failedCount-- > 0){
            CallSDKInitFailed("xxx");return;
        }
#endif
        Debug.Log("登录成功:" + ret.ToString());
        this.user = ret;
        SetEvent(States.SDKLogined, new Event(Events.SDKLoginSuccess, ret.uid));
        UserSystem.Instance.ChangeState(UserSystem.States.SDKLogined);
        //TriggerEvent(States.SDKLogining, Events.SDKLoginSuccess, ret);
    }

    public void CallSDKLoginFailed(string ret)
    {
        Debug.LogWarning("登录失败:" + ret);
        SetEvent(States.SDKLogining, new Event(Events.SDKLoginFailed, ret));
        UserSystem.Instance.ChangeState(UserSystem.States.SDKLogining, 1.0f);
        //TriggerEvent(States.SDKLogining, Events.SDKLoginFailed, ret);
    }

    public void CallSDKLogoutSuccess(string ret) {
        Debug.LogWarning("登出成功:" + ret);
        SetEvent(States.SDKLogining, new Event(Events.SDKLogoutSuccess, ret));
        UserSystem.Instance.ChangeState(UserSystem.States.SDKInited);
        //TriggerEvent(States.SDKLogined, Events.SDKLogoutSuccess, ret);
    }

    public void CallSDKPaySuccess(string ret)
    {
        Debug.LogWarning("支付成功:" + ret);
        TriggerEvent(fsm.State, Events.SDKPaySuccess, ret);
    }

    public void CallSDKPayFailed(string ret)
    {
        Debug.LogWarning("支付失败:" + ret);
        TriggerEvent(fsm.State, Events.SDKPayFailed, ret);
    }

    public void CallSDKPayCancel(string ret)
    {
        Debug.LogWarning("支付取消:" + ret);
        TriggerEvent(fsm.State, Events.SDKPayCancel, ret);
    }

    IEnumerator DelayChangeState(States iState, float iDelay)
    {
        yield return new WaitForSeconds(iDelay);
        fsm.ChangeState(iState);
    }

    //------------------ 状态机处理 --------------------
    public void ChangeState(States state, float delay = 0.0f)
    {
        Debug.LogFormat("切换用户状态:{0}=>{1}", fsm.State.ToString(), state.ToString());

        CoroutineUtil.DoCoroutine(DelayChangeState(state, delay));
        //GameDebugger.sPushLog("切换用户状态:" + state.ToString());
        //fsm.ChangeState(state);
    }

    private void None_Update()
    {
        //ChangeState(States.SDKIniting);
    }

    private void SDKIniting_Enter()
    {
        SDKInit();
    }

    private void SDKIniting_Update()
    {
    }

    private void SDKIniting_Exit()
    {
    }

    private void SDKInited_Enter()
    {
    }

    private void SDKInited_Update()
    {
    }

    private void SDKInited_Exit()
    {
    }

    private void SDKLogining_Enter()
    {
        SDKLogin();
    }

    private void SDKLogining_Update()
    {
    }

    private void SDKLogining_Exit()
    {
    }

    private void SDKLogined_Enter()
    {
    }

    private void SDKLogined_Update()
    {
    }

    private void SDKLogined_Exit()
    {
    }

    private static void SaveUID(string uid)
    {
        string file = Global.PersistentDataPath + "/" + "uid.txt";
        StreamWriter writer = new StreamWriter(file, false);
        writer.Write(uid);
        writer.Close();
    }

    private static string LoadUID()
    {
        string uid = "000000";
        string file = Global.PersistentDataPath + "/" + "uid.txt";
        if (File.Exists(file))
        {
            StreamReader reader = new StreamReader(file);
            uid = reader.ReadLine();
            reader.Close();
        }
        else {
            uid = UnityEngine.Random.Range(10000, 100000).ToString();
            SaveUID(uid);
        }
        return uid;
    }
}

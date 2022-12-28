using UnityEngine;
using System.Collections;
using LuaFramework;
using System.Collections.Generic;

public class Base : MonoBehaviour {
    private AppFacade m_Facade;
    private LuaManager m_LuaMgr;

    private ResourceManager m_ResMgr;
    private NetworkManager m_NetMgr;
    private SoundManager m_SoundMgr;
    private TimerManager m_TimerMgr;
    private ThreadManager m_ThreadMgr;
    private ObjectPoolManager m_ObjectPoolMgr;
    private GameSystem mGameSystem;
    private StageSystem mStageSystem;
    private ResourceSystem mResourceSystem;
    private UISystem mUISystem;
    private VFXSystem mVFXSystem;
    private NetworkSystem mNetworkSystem;

    /// <summary>
    /// 注册消息
    /// </summary>
    /// <param name="view"></param>
    /// <param name="messages"></param>
    protected void RegisterMessage(IView view, List<string> messages) {
        if (messages == null || messages.Count == 0) return;
        Controller.Instance.RegisterViewCommand(view, messages.ToArray());
    }

    /// <summary>
    /// 移除消息
    /// </summary>
    /// <param name="view"></param>
    /// <param name="messages"></param>
    protected void RemoveMessage(IView view, List<string> messages) {
        if (messages == null || messages.Count == 0) return;
        Controller.Instance.RemoveViewCommand(view, messages.ToArray());
    }

    protected AppFacade facade {
        get {
            if (m_Facade == null) {
                m_Facade = AppFacade.Instance;
            }
            return m_Facade;
        }
    }

    protected LuaManager LuaManager {
        get {
            if (m_LuaMgr == null) {
                m_LuaMgr = facade.GetManager<LuaManager>(ManagerName.Lua);
            }
            return m_LuaMgr;
        }
    }

    protected ResourceManager ResManager {
        get {
            if (m_ResMgr == null) {
                m_ResMgr = facade.GetManager<ResourceManager>(ManagerName.Resource);
            }
            return m_ResMgr;
        }
    }

    protected NetworkManager NetManager {
        get {
            if (m_NetMgr == null) {
                m_NetMgr = facade.GetManager<NetworkManager>(ManagerName.Network);
            }
            return m_NetMgr;
        }
    }

    protected SoundManager SoundManager {
        get {
            if (m_SoundMgr == null) {
                m_SoundMgr = facade.GetManager<SoundManager>(ManagerName.Sound);
            }
            return m_SoundMgr;
        }
    }
    
    protected TimerManager TimerManager {
        get {
            if (m_TimerMgr == null) {
                m_TimerMgr = facade.GetManager<TimerManager>(ManagerName.Timer);
            }
            return m_TimerMgr;
        }
    }

    protected ThreadManager ThreadManager {
        get {
            if (m_ThreadMgr == null) {
                m_ThreadMgr = facade.GetManager<ThreadManager>(ManagerName.Thread);
            }
            return m_ThreadMgr;
        }
    }

    protected ObjectPoolManager ObjPoolManager {
        get {
            if (m_ObjectPoolMgr == null) {
                m_ObjectPoolMgr = facade.GetManager<ObjectPoolManager>(ManagerName.ObjectPool);
            }
            return m_ObjectPoolMgr;
        }
    }

    protected GameSystem GameSystem
    {
        get
        {
            if (mGameSystem == null)
            {
                mGameSystem = facade.GetManager<GameSystem>(ManagerName.GameSystem);
            }
            return mGameSystem;
        }
    }

    protected StageSystem StageSystem {
        get {
            if (mStageSystem == null) {
                mStageSystem = facade.GetManager<StageSystem>(ManagerName.StageSystem);
            }
            return mStageSystem;
        }
    }

    protected ResourceSystem ResourceSystem {
        get {
            if (mResourceSystem == null) {
                mResourceSystem = facade.GetManager<ResourceSystem>(ManagerName.ResourceSystem);
            }
            return mResourceSystem;
        }
    }

    protected UISystem UISystem
    {
        get
        {
            if (mUISystem == null)
            {
                mUISystem = facade.GetManager<UISystem>(ManagerName.UISystem);
            }
            return mUISystem;
        }
    }

    protected NetworkSystem NetworkSystem
    {
        get
        {
            if (mNetworkSystem == null)
            {
                mNetworkSystem = facade.GetManager<NetworkSystem>(ManagerName.NetworkSystem);
            }
            return mNetworkSystem;
        }
    }

    protected VFXSystem VFXSystem
    {
        get
        {
            if (mVFXSystem == null)
            {
                mVFXSystem = facade.GetManager<VFXSystem>(ManagerName.VFXSystem);
            }
            return mVFXSystem;
        }
    }
}

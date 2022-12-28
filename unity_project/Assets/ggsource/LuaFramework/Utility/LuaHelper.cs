using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using LuaInterface;
using System;
using SimpleJson;
using DG.Tweening;

namespace LuaFramework {
    public static class LuaHelper {

        /// <summary>
        /// getType
        /// </summary>
        /// <param name="classname"></param>
        /// <returns></returns>
        public static System.Type GetType(string classname) {
            Assembly assb = Assembly.GetExecutingAssembly();  //.GetExecutingAssembly();
            System.Type t = null;
            t = assb.GetType(classname); ;
            if (t == null) {
                t = assb.GetType(classname);
            }
            return t;
        }

        /// <summary>
        /// 面板管理器
        /// </summary>
        public static PanelManager GetPanelManager() {
            return AppFacade.Instance.GetManager<PanelManager>(ManagerName.Panel);
        }

        /// <summary>
        /// 资源管理器
        /// </summary>
        public static ResourceManager GetResManager() {
            return AppFacade.Instance.GetManager<ResourceManager>(ManagerName.Resource);
        }

        /// <summary>
        /// 网络管理器
        /// </summary>
        public static NetworkManager GetNetManager() {
            return AppFacade.Instance.GetManager<NetworkManager>(ManagerName.Network);
        }

        /// <summary>
        /// 音乐管理器
        /// </summary>
        public static SoundManager GetSoundManager() {
            return AppFacade.Instance.GetManager<SoundManager>(ManagerName.Sound);
        }

        public static GameSystem GetGameSystem()
        {
            return AppFacade.Instance.GetManager<GameSystem>(ManagerName.GameSystem);
        }

        public static StageSystem GetStageSystem() {
            return AppFacade.Instance.GetManager<StageSystem>(ManagerName.StageSystem);
        }

        public static ResourceSystem GetResourceSystem() {
            return AppFacade.Instance.GetManager<ResourceSystem>(ManagerName.ResourceSystem);
        }

        public static SoundSystem GetSoundSystem()
        {
            return AppFacade.Instance.GetManager<SoundSystem>(ManagerName.SoundSystem);
        }

        public static UISystem GetUISystem() {
            return AppFacade.Instance.GetManager<UISystem>(ManagerName.UISystem);
        }

        public static NetworkSystem GetNetworkSystem()
        {
            return AppFacade.Instance.GetManager<NetworkSystem>(ManagerName.NetworkSystem);
        }

        public static VFXSystem GetVFXSystem()
        {
            return AppFacade.Instance.GetManager<VFXSystem>(ManagerName.VFXSystem);
        }

        public static UserSystem GetUserSystem()
        {
            return AppFacade.Instance.GetManager<UserSystem>(ManagerName.UserSystem);
        }

        public static Action Action(LuaFunction func) {
            Action action = () => {
                func.Call();
            };
            return action;
        }

        public static void DoUIPanelAlpha(UIPanel targetPanel,float source,float target,float time,Action finishCallBack = null)
        {
            targetPanel.alpha = source;
            DOTween.To(() => source, (x) =>
              {
                  targetPanel.alpha = x;
              }, target, time).OnComplete(()=>
              {
                  if (finishCallBack != null)
                  {
                      finishCallBack();
                  }
              });
        }

        public static Action<JsonObject> ActionParam(LuaFunction func)
        {
            Action<JsonObject> action = (JsonObject msg) => 
            {
                if (func != null)
                {
                    func.Call(msg.ToString());
                }
            };
            return action;
        }

        public static Action<string> ActionString(LuaFunction func)
        {
            Action<string> action = (string str) =>
            {
                if (func != null)
                {
                    func.Call(str);
                }
            };
            return action;
        }

        public static UIEventListener.VoidDelegate VoidDelegate(LuaFunction func) {
            UIEventListener.VoidDelegate action = (go) => {
                func.Call(go);
            };
            return action;
        }

        /// <summary>
        /// pbc/pblua函数回调
        /// </summary>
        /// <param name="func"></param>
        public static void OnCallLuaFunc(LuaByteBuffer data, LuaFunction func) {
            if (func != null) func.Call(data);
            Debug.LogWarning("OnCallLuaFunc length:>>" + data.buffer.Length);
        }

        /// <summary>
        /// cjson函数回调
        /// </summary>
        /// <param name="data"></param>
        /// <param name="func"></param>
        public static void OnJsonCallFunc(string data, LuaFunction func) {
            Debug.LogWarning("OnJsonCallback data:>>" + data + " lenght:>>" + data.Length);
            if (func != null) func.Call(data);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaFramework;
using LuaInterface;

public class LuaInject {

    private static string postName = "_HF";
    private static LuaState lua = null;
    private static LuaState Lua {
        get {
            if (lua == null) {
                lua = new LuaState();
            }
            return lua;
        }
    }

    private static Dictionary<string, string> hotfixLuaFiles = new Dictionary<string, string>();
    private static Dictionary<string, string> luaCache = new Dictionary<string,string>();
    public static void AddHotfixLuaFile(string className, string luaFile){
        hotfixLuaFiles.Add(className, luaFile);
    }

    public static bool HasPatch(string className, string luaFunc)
    {
        if (hotfixLuaFiles.ContainsKey(className))
        {
            if (AppFacade.Instance == null)
            {
                return false;
            }
            LuaManager luaManager = AppFacade.Instance.GetManager<LuaManager>(ManagerName.Lua);
            GameManager gameManager = AppFacade.Instance.GetManager<GameManager>(ManagerName.Game);
            if (luaManager == null || gameManager == null || !GameManager.Initialed)
            {
                return false;
            }

            string luaFile = hotfixLuaFiles[className];

            //Debug.Log("检查lua文件缓存");
            if (!luaCache.ContainsKey(luaFile)){
                Debug.Log("缓存lua文件");
                luaManager.DoFile(luaFile);
                luaCache[luaFile] = "";
            }
            bool luaFuncExit = luaManager.GetFunction(className + postName + "." + luaFunc, false) != null;
            return luaFuncExit;
            /*
            lua.DoFile(luaFile);
            LuaFunction func = lua.GetFunction(luaFunc);
            if (func != null) {
                return true;
            }
             */
            /*
            if (
                luaFunc == "DoTest" ||
                luaFunc == "LuaInjectorTest" ||
                luaFunc == "LuaIgnoreInjectorTest"
                )
            {
                Debug.LogFormat("[True]HasPatch-file:{0}-func:{1}", luaFile, luaFunc);
                return true;
            }
            */
        }
        //Debug.LogFormat("[False]HasPatch-file:{0}-func:{1}", luaFile, luaFunc);
        return false;
    }

    public static object CallPatch(string luaFile, string luaFunc, params object[] args)
    {
        //Debug.Log(string.Format("Do Lua Patch: {0}:{1}", luaFile, luaFunc));
        // TODO 此处写你的lua函数调用, 并传入参数.
        //        return Lua.DoFile(luaFile).Call(luaFunc, args);
        if (AppFacade.Instance == null)
        {
            return null;
        }
        LuaManager luaManager = AppFacade.Instance.GetManager<LuaManager>(ManagerName.Lua);
        GameManager gameManager = AppFacade.Instance.GetManager<GameManager>(ManagerName.Game);
        if (luaManager == null || gameManager == null || !GameManager.Initialed)
        {
            return null;
        }

        object[] ret = luaManager.CallFunction(luaFile + postName + '.' + luaFunc, args);
        if (ret.Length > 0) {
            //Debug.Log("Lua Call result:" + ret[0].ToString());
            return ret[0];
            /*
             // 调试测试代码。
            LuaTable temp = ret[0] as LuaTable;
            Debug.Log("Lua Call result->:" + temp.ToString());
            Debug.Log("Lua Call result->0:" + temp[1].ToString());
            Debug.Log("Lua Call result->1:" + temp[2].ToString());
             */
        }
        //LuaFramework.Util.CallMethod(luaFile, luaFunc, args);
        return null;
    }
}

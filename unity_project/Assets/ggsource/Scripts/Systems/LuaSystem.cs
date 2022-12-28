using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaFramework;

public class LuaSystem : Singleton<LuaSystem>
{
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
    class WatchInfo
    {
        FileSystemWatcher watcher;
        string fileter;
        List<string> changeFiles;
        System.Action<string> changeAction;
        string dirPath;

        public WatchInfo(string _dirPath, string _filter, System.Action<string> _changeAction)
        {
            watcher = new FileSystemWatcher();
            changeFiles = new List<string>();
            fileter = _filter;
            dirPath = _dirPath;
            changeAction = _changeAction;
            CreateWatcher(watcher, dirPath, changeFiles, fileter);
        }

        public void UpdateLs()
        {
            if (changeFiles.Count > 0 && null != changeAction)
            {
                foreach (var changeFile in changeFiles)
                {
                    changeAction(changeFile);
                }
                changeFiles.Clear();
            }
        }

        private void AddToLs(List<string> ls, string elem)
        {
            if (!ls.Contains(elem))
            {
                ls.Add(elem);
            }
        }
        private void CreateWatcher(FileSystemWatcher watcher, string path, List<string> changeLs, string fileFilter)
        {
            CreateWatcher(watcher, path, fileFilter, (object source, FileSystemEventArgs e) =>
            {
                AddToLs(changeLs, e.FullPath);
            }, (object source, RenamedEventArgs e) => {
                AddToLs(changeLs, e.FullPath);
            });
        }
        private void CreateWatcher(FileSystemWatcher watcher, string path, string fileFilter, FileSystemEventHandler onChanged, RenamedEventHandler onRenamed)
        {
            watcher.Path = Path.GetFullPath(path);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = fileFilter;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += onChanged;
            watcher.Created += onChanged;
            watcher.Deleted += onChanged;
            watcher.Renamed += onRenamed;
            watcher.EnableRaisingEvents = true;
        }

    }
    private List<WatchInfo> watchInfos = new List<WatchInfo>();
#endif
    private bool initialed = false;
    public bool Initialed
    {
        get
        {
            return initialed;
        }
    }

    private void Start()
    {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
        watchInfos.Add(new WatchInfo(LuaConst.luaDir, "*.lua", (changeCode) => {
            string luaDirStr = LuaConst.luaDir;
#if UNITY_EDITOR_WIN
            luaDirStr = LuaConst.luaDir.Replace("/", "\\");
            string str = changeCode.Replace(luaDirStr + "\\", "").Replace("\\", ".").Replace(".lua", "");
#else
            string str = changeCode.Replace(luaDirStr + "/", "").Replace("/", ".").Replace(".lua", "");
#endif
            Debug.LogWarning("修改的脚本:" + str);
            LuaFramework.Util.CallMethod("LuaRefreshScript", "RefreshLua", str);
        }));
#endif
        }

    void LateUpdate()
    {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
        foreach (var watcher in watchInfos)
        {
            watcher.UpdateLs();
        }
#endif
    }

    public void Init()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = AppConst.GameFrameRate;
        Util.Init();

        AppFacade.Instance.StartUp();   //启动游戏
        LuaManager luaManager = AppFacade.Instance.GetManager<LuaManager>(ManagerName.Lua);
        luaManager.InitStart();
    }

    public void LaunchLua(string luaFile) {
        LuaManager luaManager = AppFacade.Instance.GetManager<LuaManager>(ManagerName.Lua);
        luaManager.DoFile(luaFile);            //加载游戏
    }
}

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using LuaFramework;

public class LuaBuilder {

    static List<string> paths = new List<string>();
    static List<string> files = new List<string>();

    /// <summary>
    /// 构建 Lua Bundles
    /// </summary>
    public static void BuildNotJitBundles(RuntimePlatform platform)
    {
		/*
        ClearAllLuaFiles();
        CreateStreamDir(GetOS());

#if !UNITY_5
        string tempDir = CreateStreamDir("Lua");
#else
        string tempDir = Application.dataPath + "/temp/Lua";

        if (!File.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }        
#endif
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, tempDir);

        AssetDatabase.Refresh();
        List<string> dirs = new List<string>();
        GetAllDirs(tempDir, dirs);

#if UNITY_5        
        for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, tempDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/temp/Lua");
        }

        BuildLuaBundle(null, "Assets/temp/Lua");

        AssetDatabase.SaveAssets();        
        string output = string.Format("{0}/{1}", Application.streamingAssetsPath, GetOS());        
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.DeterministicAssetBundle, EditorUserBuildSettings.activeBuildTarget);

        //Directory.Delete(Application.dataPath + "/temp/", true);
#else
        for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, tempDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/StreamingAssets/Lua");
        }

        BuildLuaBundle(null, "Assets/StreamingAssets/Lua");
        Directory.Delete(Application.streamingAssetsPath + "/Lua/", true);
#endif            
        AssetDatabase.Refresh();
		 */
    }

    /// <summary>
    /// 为指定平台构建Lua资源。
    /// 要更新package.json。
    /// </summary>
    /// <param name="target"></param>
    public static void BuildLuaAssets(BuildTarget target, string targetPath)
    {
        AssetDatabase.Refresh();

        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }

        if (AppConst.LuaBundleMode)
        {
            //TODO: GL - 将来可以考虑实现。
            HandleBundle(target, targetPath);
        }
        else
        {
            HandleLuaFile(targetPath);
        }

        AssetDatabase.Refresh();
    }

    static void HandleBundle(BuildTarget target, string targetPath)
    {
        BuildLuaBundles(target);
        string luaPath = targetPath;// AssetBuilder.LuasOutputPath;// AppDataPath + "/StreamingAssets/lua/";
        string[] luaPaths = { AppDataPath + "/LuaFramework/lua/",
                              AppDataPath + "/LuaFramework/Tolua/Lua/" };

        for (int i = 0; i < luaPaths.Length; i++)
        {
            paths.Clear(); files.Clear();
            string luaDataPath = luaPaths[i].ToLower();
            GO.FileUtil.Recursive(luaDataPath, paths, files, ".meta");
            //Recursive(luaDataPath);
            foreach (string f in files)
            {
                if (f.EndsWith(".meta") || f.EndsWith(".lua")) continue;
                string newfile = f.Replace(luaDataPath, "");
                string path = Path.GetDirectoryName(luaPath + newfile);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string destfile = path + "/" + Path.GetFileName(f);
                File.Copy(f, destfile, true);
            }
        }
    }

    static void BuildLuaBundles(BuildTarget target)
    {
        ClearAllLuaFiles();
        CreateStreamDir("lua/");
        CreateStreamDir(AppConst.LuaTempDir);

        string dir = Global.StreamingAssetsPath;
        if (!File.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string streamDir = Application.dataPath + "/" + AppConst.LuaTempDir;
        CopyLuaBytesFiles(CustomSettings.luaDir, streamDir);
        CopyLuaBytesFiles(CustomSettings.FrameworkPath + "/ToLua/Lua", streamDir);

        AssetDatabase.Refresh();
        string[] dirs = Directory.GetDirectories(streamDir, "*", SearchOption.AllDirectories);

        for (int i = 0; i < dirs.Length; i++)
        {
            string str = dirs[i].Remove(0, streamDir.Length);
            BuildLuaBundle(str, target);
        }

        BuildLuaBundle(null, target);
        Directory.Delete(streamDir, true);
        AssetDatabase.Refresh();
    }

    static void BuildLuaBundle(string dir)
    {
        BuildLuaBundle(dir, EditorUserBuildSettings.activeBuildTarget);
    }
    static void BuildLuaBundle(string dir, BuildTarget target)
    {
        BuildAssetBundleOptions options = BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets |
                                          BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.UncompressedAssetBundle;
        string path = "Assets/" + AppConst.LuaTempDir + dir;
        string[] files = Directory.GetFiles(path, "*.lua.bytes");
        List<Object> list = new List<Object>();
        string bundleName = "lua.unity3d";
        if (dir != null)
        {
            dir = dir.Replace('\\', '_').Replace('/', '_');
            bundleName = "lua_" + dir.ToLower() + AppConst.ExtName;
        }
        for (int i = 0; i < files.Length; i++)
        {
            Object obj = AssetDatabase.LoadMainAssetAtPath(files[i]);
            list.Add(obj);
        }

        if (files.Length > 0)
        {
            string output = Application.streamingAssetsPath + "/lua/" + bundleName;
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            BuildPipeline.BuildAssetBundle(null, list.ToArray(), output, options, target);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// 处理Lua文件
    /// </summary>
    static void HandleLuaFile(string luaPath)
    {
        //string luaPath = AssetBuilder.LuasOutputPath;// Path.Combine(Global.StreamingAssetsPath, "lua");
        //string luaPath = Path.Combine(AppDataPath, "StreamingAssets/lua");
        UnityEngine.Debug.LogWarning("luaPath:" + luaPath);

        //----------复制Lua文件----------------
        if (!Directory.Exists(luaPath))
        {
            Directory.CreateDirectory(luaPath);
        }
        Directory.Delete(luaPath, true);
        string[] luaPaths = { AppDataPath + "/" + AppConst.AppName + "/lua/",
                              AppDataPath + "/" + AppConst.AppName + "/Tolua/Lua/" };

        for (int i = 0; i < luaPaths.Length; i++)
        {
            paths.Clear(); files.Clear();
            string luaDataPath = luaPaths[i].ToLower();
            //UnityEngine.Debug.LogWarning("luaDataPath:" + luaDataPath);
            //Recursive(luaDataPath);
            GO.FileUtil.Recursive(luaDataPath, paths, files, ".meta");
            int n = 0;
            foreach (string f in files)
            {
                if (f.EndsWith(".meta")) continue;
                string newfile = f.Replace(luaDataPath, "");
                string newpath = Path.Combine(luaPath, newfile);
                //UnityEngine.Debug.LogWarning("luaPath:" + luaPath);
                string path = Path.GetDirectoryName(newpath);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                if (File.Exists(newpath))
                {
                    File.Delete(newpath);
                }
                if (AppConst.LuaByteMode)
                {
                    EncodeLuaFile(f, newpath);
                }
                else
                {
                    //UnityEngine.Debug.LogWarning("f:" + f);
                    //UnityEngine.Debug.LogWarning("newfile:" + newfile);
                    File.Copy(f, newpath, true);
                }
                UpdateProgress(n++, files.Count, newpath);
            }
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    static void ClearAllLuaFiles()
    {
        string osPath = Application.streamingAssetsPath + "/" + LuaConst.osDir;

        if (Directory.Exists(osPath))
        {
            string[] files = Directory.GetFiles(osPath, "Lua*.unity3d");

            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }

        string path = osPath + "/Lua";

        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        path = Application.dataPath + "/Resources/Lua";

        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        path = Global.StreamingAssetsPath + "/" + LuaConst.osDir + "/Lua";

        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    static void CreateStreamDir(string dir)
    {
        dir = Application.streamingAssetsPath + "/" + dir;

        if (!File.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    static void CopyLuaBytesFiles(string sourceDir, string destDir, bool appendext = true)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }

        string[] files = Directory.GetFiles(sourceDir, "*.lua", SearchOption.AllDirectories);
        int len = sourceDir.Length;

        if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\')
        {
            --len;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, len);
            string dest = destDir + str;
            if (appendext) dest += ".bytes";
            string dir = Path.GetDirectoryName(dest);
            Directory.CreateDirectory(dir);

            if (AppConst.LuaByteMode)
            {
                LuaBuilder.EncodeLuaFile(files[i], dest);
            }
            else
            {
                File.Copy(files[i], dest, true);
            }
        }
    }

    static void BuildDataFiles(BuildTarget target)
    {
        return;
        Object mainAsset = null;        //主素材名，单个
        Object[] addis = null;     //附加素材名，多个
        string assetfile = string.Empty;  //素材文件名

    }

    static void UpdateProgress(int progress, int progressMax, string desc)
    {
        string title = "Processing...[" + progress + " - " + progressMax + "]";
        float value = (float)progress / (float)progressMax;
        EditorUtility.DisplayProgressBar(title, desc, value);
    }

    public static void EncodeLuaFile(string srcFile, string outFile)
    {
        if (!srcFile.ToLower().EndsWith(".lua"))
        {
            File.Copy(srcFile, outFile, true);
            return;
        }
        bool isWin = true;
        string luaexe = string.Empty;
        string args = string.Empty;
        string exedir = string.Empty;
        string currDir = Directory.GetCurrentDirectory();
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            isWin = true;
            luaexe = "luajit32.exe";
            args = "-b \"" + srcFile + "\" \"" + outFile + "\"";
            exedir = AppDataPath.Replace("assets", "") + "Luajit/";
        }
        //else if (Application.platform == RuntimePlatform.OSXEditor)
        //{
        //    isWin = false;
        //    luaexe = "./luac";
        //    args = "-o " + "\"" + outFile + "\" \"" + srcFile + "\"";
        //    exedir = AppDataPath.Replace("assets", "") + "LuaEncoder/luavm/";
        //}
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            isWin = false;
            luaexe = "./luajit";
            args = "-b \"" + srcFile + "\" \"" + outFile + "\"";
            exedir = AppDataPath.Replace("assets", "") + "LuaEncoder/luajit_mac/";
        }
        Directory.SetCurrentDirectory(exedir);
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = luaexe;
        info.Arguments = args;
        info.WindowStyle = ProcessWindowStyle.Hidden;
        info.UseShellExecute = isWin;
        info.ErrorDialog = true;
        Util.Log(info.FileName + " " + info.Arguments);

        Process pro = Process.Start(info);
        pro.WaitForExit();
        Directory.SetCurrentDirectory(currDir);
    }

    /// <summary>
    /// 数据目录
    /// </summary>
    static string AppDataPath
    {
        get { return Application.dataPath.ToLower(); }
    }
}

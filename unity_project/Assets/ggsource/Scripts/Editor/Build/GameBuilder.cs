using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LuaFramework;
using System.Xml.Linq;
//using AssetBundles;
using ICSharpCode.SharpZipLib.Zip;

/// <summary>
/// 游戏打包生产线。
/// 1. 做bundle并打包（用于热更新），包括Lua，assets等。
/// 2. 更新bundle数据。TODO：问题：保内数据版本号从哪里获得？
/// </summary>
public class GameBuilder
{
    [MenuItem("Game Builder/1.Build Table", false, 78)]
    public static void BuildTable() {
        TableBuilder.BuildTables();
    }

    [MenuItem("Game Builder/2.Build Lua", false, 80)]
    public static void BuildLua()
    {
        BuildTarget target = BuildTarget.Android;
        string luaPath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.LuasOutputPath);
        LuaBuilder.BuildLuaAssets(target, luaPath);
    }

    [MenuItem("Game Builder/3.Build Bundle/Android", false, 82)]
    public static void BuildBundle_Android()
    {
        BuildTarget target = BuildTarget.Android;
        AssetBuilder.BuildBundles(target, Loxodon.Framework.Bundles.BundleUtil.GetReadOnlyDirectory());
        //AssetBuilder.GenerateAllAssetToBundle(target);
    }

    [MenuItem("Game Builder/3.Build Bundle/IOS", false, 82)]
    public static void BuildBundle_IOS()
    {
        BuildTarget target = BuildTarget.iOS;
        AssetBuilder.BuildBundles(target, Loxodon.Framework.Bundles.BundleUtil.GetReadOnlyDirectory());
        //AssetBuilder.GenerateAllAssetToBundle(target);
    }

    [MenuItem("Game Builder/4.Build Plist", false, 84)]
    public static void BuildPList()
    {
        string outputPath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.BuildOutputPath);
        string[] assetPaths = { Global.StreamingAssetsPath,  outputPath};
        string[] assetResources = { "bundles", "lua" };
        string newFilePath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.BuildOutputPath + "/" + Global.AssetPlistName);
        AssetBuilder.BuildResourcePList(assetPaths, assetResources, newFilePath);
        string inPackageFilePath = Path.Combine(Application.streamingAssetsPath, Global.AssetPlistName);
        File.Copy(newFilePath, inPackageFilePath, true);
    }

    [MenuItem("Game Builder/5.Zip Asset", false, 86)]
    public static void ZipAsset()
    {
        // bundle资源和lua文件。
        AssetBuilder.ZipAssets();
    }

    [MenuItem("Game Builder/Build All/Android", false, 88)]
    public static void BuildAllAndroid()
    {
        BuildAll(BuildTarget.Android);
    }

    [MenuItem("Game Builder/Build All/IOS", false, 88)]
    public static void BuildAllIOS()
    {
        BuildAll(BuildTarget.iOS);
    }

    [MenuItem("Game Builder/Build Hotfix Assets/Android", false, 110)]
    public static void BuildHotfixAssetsAndroid()
    {
        BuildHotfixAssets(BuildTarget.Android);
    }

    [MenuItem("Game Builder/Build Hotfix Assets/IOS", false, 112)]
    public static void BuildHotfixAssetsIOS()
    {
        BuildHotfixAssets(BuildTarget.iOS);
    }

    /// <summary>
    /// 构建用于发布的热更数据。
    /// </summary>
    /// <param name="target"></param>
    public static void BuildHotfixAssets(BuildTarget target)
    {
        string gitPath = Path.Combine(System.Environment.CurrentDirectory, "../../");
        gitPath = EditorUtility.OpenFolderPanel("AssetBundle Folder", gitPath, string.Empty);
        if (string.IsNullOrEmpty(gitPath)) {
            // dialog
            EditorUtility.DisplayDialog("异常", "非法目录", "OK");
            return;
        }
        // 先检查是否是个git目录
        if (!Directory.Exists(Path.Combine(gitPath, "../.git")))
        {
            // dialog
            EditorUtility.DisplayDialog("异常", "这不是一个git目录！", "OK");
            return;
        }
        LuaBuilder.BuildLuaAssets(target, Path.Combine(gitPath, "lua"));
        AssetBuilder.BuildBundles(target, Path.Combine(gitPath, "bundles"));
        string outputPath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.BuildOutputPath);
        string[] assetPaths = { gitPath, gitPath };
        string[] assetResources = { "bundles", "lua" };
        string newFilePath = Path.Combine(gitPath, Global.AssetPlistName);
        AssetBuilder.BuildResourcePList(assetPaths, assetResources, newFilePath);
    }

    /// <summary>
    /// 构建用于打包的数据。
    /// </summary>
    /// <param name="target"></param>
    public static void BuildAll(BuildTarget target)
    {
        // 清理streamingassets目录
        ClearStreamingAssetsPath();
        TableBuilder.BuildTables();
        string luaPath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.LuasOutputPath);
        LuaBuilder.BuildLuaAssets(target, luaPath);
        AssetBuilder.BuildBundles(target, Loxodon.Framework.Bundles.BundleUtil.GetReadOnlyDirectory());
        string outputPath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.BuildOutputPath);
        string[] assetPaths = { Global.StreamingAssetsPath, outputPath };
        string[] assetResources = { "bundles", "lua" };
        string newFilePath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.BuildOutputPath + "/" + Global.AssetPlistName);
        UnityEngine.Debug.LogWarning("newFilePath:" + newFilePath);
        AssetBuilder.BuildResourcePList(assetPaths, assetResources, newFilePath);
        string inPackageFilePath = Path.Combine(Application.streamingAssetsPath, Global.AssetPlistName);
        File.Copy(newFilePath, inPackageFilePath);
        AssetBuilder.ZipAssets();
    }

    private static void ClearStreamingAssetsPath() {
        if (Directory.Exists(Application.streamingAssetsPath))
        {
            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
        }

        Directory.CreateDirectory(Application.streamingAssetsPath);
    }

    /// <summary>
    /// 清理用于热更发布的git仓库。
    /// </summary>
    //private static bool ClearHotfixGitRepo(string gitPath) {
    //    // 先检查是否是个git目录
    //    if (!Directory.Exists(Path.Combine(gitPath, ".git"))) {
    //        return false;
    //    }
    //    // 如果是git仓库，则清楚非.git目录的数据。
    //}

    //    [MenuItem("Game Builder/Asset Builder/Build Android Bundles", false, 100)]
    //    public static void BuildAndroidAssets(){
    //        AssetBuilder.GenerateAllAssetToBundle(BuildTarget.Android);
    //    }

    //    [MenuItem("Game Builder/Asset Builder/Build iPhone Bundles", false, 101)]
    //    public static void BuildiOSAssets(){
    //        BuildTarget target;
    //#if UNITY_5
    //        target = BuildTarget.iOS;
    //#else
    //        target = BuildTarget.iOS;
    //#endif
    //        AssetBuilder.GenerateAllAssetToBundle(target);
    //    }

    //    [MenuItem("Game Builder/Package Builder/Build Android Package(Dev)", false, 120)]
    //    public static void BuildAndroidPackageDev(){
    //        BuildPackage(BuildTarget.Android, AssetBuilder.TargetType.Development);
    //    }

    //    [MenuItem("Game Builder/Package Builder/Build Android Package(Dis)", false, 121)]
    //    public static void BuildAndroidPackageDis()
    //    {
    //        BuildPackage(BuildTarget.Android, AssetBuilder.TargetType.Distribution);
    //    }

    //    [MenuItem("Game Builder/Package Builder/Build iPhone Package(Dev)", false, 125)]
    //    public static void BuildiOSPackageDev(){
    //        BuildTarget target;
    //#if UNITY_5
    //        target = BuildTarget.iOS;
    //#else
    //        target = BuildTarget.iOS;
    //#endif
    //        BuildPackage(target, AssetBuilder.TargetType.Development);
    //    }
    //#if TODO_do_we_need_it
    //    [MenuItem("Game Builder/Package Builder/Build IOS Vest Package(Dev)", false, 125)]
    //    public static void BuildiOSVestPackageDev()
    //    {
    //        BuildTarget target;
    //#if UNITY_5
    //        target = BuildTarget.iOS;
    //#else
    //        target = BuildTarget.iOS;
    //#endif
    //        AssetBuilder.BuildVestPackage(target, AssetBuilder.TargetType.Development);
    //    }

    //    [MenuItem("Game Builder/Package Builder/Build Android Vest Package(Dev)", false, 125)]
    //    public static void BuildAndroidVestPackageDev()
    //    {
    //        AssetBuilder.BuildVestPackage(BuildTarget.Android, AssetBuilder.TargetType.Development);
    //    }
    //#endif  // TODO_do_we_need_it

    //    [MenuItem("Game Builder/Package Builder/Build iPhone Package(Dis)", false, 126)]
    //    public static void BuildiOSPackageDis()
    //    {
    //        BuildTarget target;
    //#if UNITY_5
    //        target = BuildTarget.iOS;
    //#else
    //        target = BuildTarget.iOS;
    //#endif
    //        BuildPackage(target, AssetBuilder.TargetType.Distribution);
    //    }

    //public static void BuildAssets(BuildTarget target, string svnPath) {
    //    // Build Bundle
    //    AssetBuilder.GenerateAllAssetToBundle(target);
    //    // Build Lua
    //    LuaBuilder.BuildLuaAssets(target);
    //    // 生成数据清单
    //    string[] assetPaths = { AssetBuilder.AssetBundlesOutputPath, Path.Combine(Application.dataPath, Global.StreamingAssetsPath) };
    //    string[] assetResources = { AssetBuilder.AssetBundlesOutputPath, Path.Combine(Application.dataPath, AssetBuilder.LuasOutputPath) };
    //    string newFilePath = Path.Combine(System.Environment.CurrentDirectory, AssetBuilder.BuildOutputPath + "/" + Global.AssetPlistName);
    //    AssetBuilder.BuildResourcePList(assetPaths, assetResources, newFilePath);
    //    // 将数据资源复制到svn仓库下，准备发布。
    //    //string[] assetList = { Utility.AssetBundlesOutputPath, "lua", Global.AssetPlistName };
    //    //AssetBuilder.SyncToSVN(assetList, svnPath);
    //}

    //public static void BuildPackagePipeline(BuildTarget target, string svnPath) {
    //    EditorUtility.DisplayProgressBar("Build", "Build Package...", 0.1f);
    //    // 准备表
    //    //TableBuilder.BuildTables(target, svnPath);
    //    BuildAssets(target, "");

    //    // 打包数据资源到streamingAssets下，首次安装释放。
    //    AssetBuilder.ZipAssets();
    //    //AssetBuilder.CopyBundles(target);

    //    EditorUtility.DisplayDialog("完成", "构建游戏数据完成", "确认");
    //    EditorUtility.ClearProgressBar();
    //}

    /// <summary>
    /// 构建资源更新包。从编辑器数据目录（Persistent）打包压缩到StreamingAssets目录下。
    /// 用于游戏包解压和提交到热更服务器。
    /// </summary>
//    public static void BuildPackage(BuildTarget target, AssetBuilder.TargetType targetType)
//    {
//        string  svnPath = GetArg("-svnPath");
//        if(svnPath == ""){
//            EditorUtility.DisplayDialog("错误", "未能获得svn路径参数!", "OK");
//            return;
//        }
//        //TODO: GL - 发布版先删除Library目录。
//        /*
//        if (targetType == AssetBuilder.TargetType.Distribution) {
//            string libraryPath = Path.Combine(Application.dataPath, "../Library");
//            DebugGO.UnityEngineDebugLogWarning("delete library:" + libraryPath);
//            if (Directory.Exists(libraryPath))
//            {
//                Directory.Delete(libraryPath, true);
//            }
//        } return;
//        */
//        BuildPackagePipeline(target, svnPath);
//    }

//    public static void BuildPackageCommand()
//    {
//        string outputParam = GetArg("-output");
//        string targetParam = GetArg("-target");

//        BuildTarget target = BuildTarget.Android;
//        if (targetParam != null && targetParam.Equals("ios"))
//        {
//#if UNITY_5
//            target = BuildTarget.iOS;
//#else
//            target = BuildTarget.iOS;
//#endif
//        }

//        BuildPackagePipeline(target, outputParam);
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    [MenuItem("Game Builder/Asset Builder/Build Android Lua", false, 130)]
//    public static void BuildAndroidLua() {
//        LuaBuilder.BuildLuaAssets(BuildTarget.Android);
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    [MenuItem("Game Builder/Asset Builder/Build iPhone Lua", false, 131)]
//    public static void BuildiPhoneLua() {
//        BuildTarget target;
//#if UNITY_5
//        target = BuildTarget.iOS;
//#else
//        target = BuildTarget.iOS;
//#endif
//        LuaBuilder.BuildLuaAssets(target);
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    [MenuItem("Game Builder/Asset Builder/Build Windows Lua", false, 192)]
//    public static void BuildWindowsLua() {
//        LuaBuilder.BuildLuaAssets(BuildTarget.StandaloneWindows);
//    }

//    [MenuItem("Game Builder/Scene Builder/Build MainScene Android", false, 210)]
//    public static void BuildMainSceneAndroid()
//    {
//        string[] levels = { "Assets/Scenes/MainScene.unity" };
//        BuildPipeline.BuildStreamedSceneAssetBundle(levels, Application.streamingAssetsPath + "/" + "Launcher.unity3d", BuildTarget.Android);
//    }

//    [MenuItem("Game Builder/Scene Builder/Build MainScene iOS", false, 211)]
//    public static void BuildMainSceneiOS()
//    {
//        string[] levels = { "Assets/Scenes/MainScene.unity" };
//        BuildPipeline.BuildStreamedSceneAssetBundle(levels, Application.streamingAssetsPath + "/" + "Launcher.unity3d", BuildTarget.iOS);
//    }

    public static void ArgTest(){
        var args = System.Environment.GetCommandLineArgs();
        foreach(string item in args){
            //System.Console.WriteLine("======" + item);
        }
        DebugGO.UnityEngineDebugLogWarning("platform:" + GetArg("-platform"));
        DebugGO.UnityEngineDebugLogWarning("output:" + GetArg("-output"));
    }

    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LuaFramework;
using JsonFx.Json;
using System.Xml.Linq;
using Loxodon.Framework.Attributes;
using Loxodon.Framework.Security.Cryptography;
//using Loxodon.Framework.Bundles;
using Loxodon.Framework.Bundles.Editors;
using ICSharpCode.SharpZipLib.Zip;

/// <summary>
/// 用于构建生成资源。包括（美术、UI等）内容的Bundle和Lua。
/// 生成到StreamingAssets。
/// </summary>
public class AssetBuilder : EditorWindow {
    public enum TargetType { 
        Development,
        Distribution
    }
    //public static string platform = string.Empty;
    static List<string> paths = new List<string>();
    static List<string> files = new List<string>();

    public static string BuildOutputPath = "output";
    public static string AssetBundlesOutputPath = BuildOutputPath + "/AssetBundles";
    public static string LuasOutputPath = BuildOutputPath + "/lua";

    ///-----------------------------------------------------------
    //static string[] exts = { ".txt", ".xml", ".lua", ".assetbundle", ".json" };
    //static bool CanCopy(string ext) {   //能不能复制
    //    foreach (string e in exts) {
    //        if (ext.Equals(e)) return true;
    //    }
    //    return false;
    //}
#if TODO_Do_we_need_it
    public static List<string> vestBundleList = new List<string>();
    public static void BuildVestPackage(BuildTarget target, AssetBuilder.TargetType targetType)
    {
        AssetBuilder.GenerateAllAssetToBundle(target);
        AssetBuilder.BuildLuaAssets(target);

        string vestBundleConfig = Application.dataPath + "/Editor/vestpackagebundle.xml";

        XDocument doc_vest = XDocument.Load(vestBundleConfig);
        XElement vest_root = doc_vest.Root;
        XElement vestAssetBundleNode = vest_root.Element("vestAssetBundle");
        IEnumerable<XElement> vestAssetBundleList = vestAssetBundleNode.Elements("Item");

        string noneedZipConfig = Application.dataPath + "/Editor/noneedbundle.xml";

        XDocument noneed_doc = XDocument.Load(noneedZipConfig);
        XElement noneed_root = noneed_doc.Root;
        XElement noneedZipNode = noneed_root.Element("NoNeedZip");
        IEnumerable<XElement> noneedZipList = noneedZipNode.Elements("Item");

        if (Directory.Exists(Application.streamingAssetsPath))
        {
            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
        }

        Directory.CreateDirectory(Application.streamingAssetsPath);

        string localAssetBundlePath = Application.persistentDataPath + "/AssetBundles/";
        string streamingAssetBundlePath = Application.streamingAssetsPath + "/AssetBundles/";

        string localPlatformBundlePath = localAssetBundlePath + Utility.GetPlatformName();
        string targetPlatformBundlePath = streamingAssetBundlePath + Utility.GetPlatformName();

        if (!Directory.Exists(targetPlatformBundlePath))
            Directory.CreateDirectory(targetPlatformBundlePath);

        string manifestConfigPath = localPlatformBundlePath + "/" + Utility.GetPlatformName();

        AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(manifestConfigPath);
        AssetBundleManifest manifest = manifestAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        vestBundleList.Clear();

        foreach (XElement item in vestAssetBundleList)
        {
            string bundlename = item.Attribute("Name").Value;

            string[] dependencies = manifest.GetAllDependencies(bundlename);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string sourceDependFile = localPlatformBundlePath + "/" + dependencies[i];
                string targetDependFile = targetPlatformBundlePath + "/" + dependencies[i];
                if (!vestBundleList.Contains(dependencies[i]))
                    vestBundleList.Add(dependencies[i]);
                if (!vestBundleList.Contains(dependencies[i] + ".manifest"))
                    vestBundleList.Add(dependencies[i] + ".manifest");
            }
            string sourceBundleFile = localPlatformBundlePath + "/" + bundlename;
            string targetBundleFile = targetPlatformBundlePath + "/" + bundlename;
            if (!vestBundleList.Contains(bundlename))
                vestBundleList.Add(bundlename);
            if (!vestBundleList.Contains(bundlename + ".manifest"))
                vestBundleList.Add(bundlename + ".manifest");
        }

        manifestAssetBundle.Unload(true);

        for (int i = 0; i < vestBundleList.Count; i++)
        {
            if (File.Exists(localPlatformBundlePath + "/" + vestBundleList[i]))
                File.Copy(localPlatformBundlePath + "/" + vestBundleList[i], targetPlatformBundlePath + "/" + vestBundleList[i]);
            else
            {
                UnityEngine.Debug.LogFormat("{0}文件不存在", localPlatformBundlePath + "/" + vestBundleList[i]);
            }
        }

        File.Copy(localPlatformBundlePath + "/" + Utility.GetPlatformName(), targetPlatformBundlePath + "/" + Utility.GetPlatformName());
        File.Copy(localPlatformBundlePath + "/" + Utility.GetPlatformName() + ".manifest", targetPlatformBundlePath + "/" + Utility.GetPlatformName() + ".manifest");

        AssetBuilder.GenerateFileTxt(noneedZipList);

        File.Copy(Application.persistentDataPath + "/files.txt", Application.streamingAssetsPath + "/files.txt");
        FileUtil.CopyFileOrDirectory(Application.persistentDataPath + "/lua", Application.streamingAssetsPath + "/lua");

        FastZip fzip = new FastZip();

        fzip.CreateZip(Application.streamingAssetsPath + "/" + Global.VestPackageName, Application.streamingAssetsPath, true, "+files.txt;+\\\\AssetBundles;+\\\\lua;-\\\\Temp");

#if UNITY_EDITOR_WIN
        fzip.CreateZip(Application.streamingAssetsPath + "/" + Global.VestPackageName, Application.streamingAssetsPath, true, "+files.txt;+\\\\AssetBundles;+\\\\lua;-\\\\Temp");
#else
        fzip.CreateZip(Application.streamingAssetsPath + "/" + Global.VestPackageName, Application.streamingAssetsPath, true, "+files.txt;+/AssetBundles;+/lua;-/Temp");
#endif

        if (Directory.Exists(Application.streamingAssetsPath + "/lua"))
        {
            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath + "/lua");
        }

        if (Directory.Exists(Application.streamingAssetsPath + "/AssetBundles"))
        {
            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath + "/AssetBundles");
        }

        if (File.Exists(Application.streamingAssetsPath + "/files.txt"))
        {
            File.Delete(Application.streamingAssetsPath + "/files.txt");
        }

        foreach (string item in vestBundleList)
        {
            string fileName = localPlatformBundlePath + "/" + item;

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        if (File.Exists(localPlatformBundlePath + "/" + Utility.GetPlatformName()))
        {
            File.Delete(localPlatformBundlePath + "/" + Utility.GetPlatformName());
        }

        if (File.Exists(localPlatformBundlePath + "/" + Utility.GetPlatformName() + ".manifest"))
        {
            File.Delete(localPlatformBundlePath + "/" + Utility.GetPlatformName() + ".manifest");
        }

        foreach (XElement item in noneedZipList)
        {
            string bundlename = item.Attribute("Name").Value;
            string fileName = localPlatformBundlePath + "/" + bundlename;

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
                File.Delete(fileName + ".manifest");
            }
        }

        fzip.CreateZip(Application.streamingAssetsPath + "/" + Global.PackageName, localPlatformBundlePath, true, "");

        AssetDatabase.Refresh();
    }
#endif  //TODO_Do_we_need_it

    /// <summary>
    /// 生成所有Asset的bundle数据。
    /// 1. 将bundle生成到Persistent目录，用于编辑器运行加载数据（方便调试）。
    /// 2. 将bundle数据打包到streamingassets中，用于打包到游戏安装包中（减少新包发布后的网络更新）。
    /// </summary>
    /// <param name="pBuildTarget">目标设备</param>
	public static void GenerateAllAssetToBundle(BuildTarget pBuildTarget)
	{
#if delete_it_later
        AssetDatabase.Refresh();
        //string outputPath = Application.streamingAssetsPath + Utility.AssetBundlesOutputPath + Utility.GetPlatformForAssetBundles(pBuildTarget);
        EditorUserBuildSettings.SwitchActiveBuildTarget(pBuildTarget);
        string assetBundlePath = AssetBuilder.AssetBundlesOutputPath;// Path.Combine(Global.StreamingAssetsPath, Utility.AssetBundlesOutputPath);
        if (Directory.Exists(assetBundlePath))
        {
            Directory.Delete(assetBundlePath, true);
        }
        string outputPath = Path.Combine(assetBundlePath, Utility.GetPlatformName());
        UnityEngine.Debug.LogWarning(string.Format("生成bundle资源到：{0}", outputPath));

		EnsurePathExist(outputPath);
		BuildPipeline.BuildAssetBundles(outputPath, 
			BuildAssetBundleOptions.ForceRebuildAssetBundle
			| BuildAssetBundleOptions.ChunkBasedCompression//用于使用LZ4格式进行压缩，5.3新增。(比默认压缩方式，压缩率低，解压速度快)
			| BuildAssetBundleOptions.DeterministicAssetBundle,//用于为资源维护固定ID，默认开启
			pBuildTarget);

		AssetDatabase.Refresh();
#endif  // delete_it_later
    }

    public static void BuildBundles(BuildTarget target, string targetPath) {
        string path = AssetBuilder.AssetBundlesOutputPath;// Utility.AssetBundlesOutputPath;
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle;
        options |= BuildAssetBundleOptions.ChunkBasedCompression;
        options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
        options |= BuildAssetBundleOptions.DeterministicAssetBundle;//用于为资源维护固定ID，默认开启

        List<IBundleModifier> bundleModifierChain = CreateBundleModifierChain();
        BundleBuilder builder = new BundleBuilder();
        builder.Build(path, target, options, "1.0.0", bundleModifierChain);

        CopyTo(path, target, "1.0.0", targetPath);
    }

    protected static List<IBundleModifier> CreateBundleModifierChain()
    {
        List<IBundleModifier> bundleModifierChain = new List<IBundleModifier>();

        bundleModifierChain.Add(new PublishBundleModifier(bundleInfo => true));

        //if (this.Encryption)
        //{
        //    Func<BundleInfo, bool> filter = null;
        //    switch (this.FilterType)
        //    {
        //        case EncryptionFilterType.All:
        //            filter = bundle =>
        //            {
        //                return true;
        //            };
        //            break;
        //        case EncryptionFilterType.RegularExpression:
        //            filter = bundle =>
        //            {
        //                return Regex.IsMatch(bundle.FullName, this.filterExpression);
        //            };
        //            break;
        //        case EncryptionFilterType.BundleNameList:
        //            string[] bundles = Regex.Split(this.bundleNames, @"(\s)", RegexOptions.IgnorePatternWhitespace);
        //            filter = bundle =>
        //            {
        //                return Array.IndexOf(bundles, bundle.FullName) >= 0;
        //            };
        //            break;
        //    }

        //    IStreamEncryptor encryptor = this.GetEncryptor();
        //    bundleModifierChain.Add(new CryptographBundleModifier(encryptor, filter));
        //}

        //if (this.useHashFilename)
            bundleModifierChain.Add(new HashFilenameBundleModifier());

        return bundleModifierChain;
    }

    public static void CopyTo(string output, BuildTarget target, string dataVersion, string targetPath)
    {
        try
        {
            AssetDatabase.StartAssetEditing();
            BundleBuilder builder = new BundleBuilder();

            DirectoryInfo src = new DirectoryInfo(builder.GetVersionOutput(output, target, dataVersion));
            DirectoryInfo dest = new DirectoryInfo(targetPath);// );

            if (dest.Exists)
                dest.Delete(true);
            if (!dest.Exists)
                dest.Create();

            Loxodon.Framework.Bundles.BundleManifest manifest = builder.CopyAssetBundleAndManifest(src, dest);
            if (manifest != null)
                UnityEngine.Debug.LogFormat("Copy AssetBundles success.");

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogFormat("Copy AssetBundles failure. Error:{0}", e);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    /// <summary>
    /// 要进zip包的内容包括：lua脚本，版本文件，清单文件等。
    /// </summary>
    public static void ZipAssets()
    {
        AssetDatabase.StartAssetEditing();
        //using (ZipFile zip = new ZipFile())
        {
            //string assetsPath = Path.Combine(GetPersitentDataPath.PersistentDataPath, Utility.GetPlatformForAssetBundles(target));
            string assetsPath = AssetBuilder.BuildOutputPath;// Global.StreamingAssetsPath;
            if (!Directory.Exists(assetsPath))
            {
                string errorMessage = string.Format("路径不存在!路径：{0}", assetsPath);
                EditorUtility.DisplayDialog("异常", errorMessage, "确认");
                return;
            }
            string targetZipPath = Path.Combine(Application.dataPath, "StreamingAssets");
            string targetZipFile = Path.Combine(targetZipPath, Global.PackageName);
            if (File.Exists(targetZipFile))
            {
                File.Delete(targetZipFile);
            }
            if (!Directory.Exists(targetZipPath))
            {
                Directory.CreateDirectory(targetZipPath);
            }
            //FileStream fs = File.Open(targetZipFile, FileMode.CreateNew);
#if PACK_BUNDLES
            string      pathAssetBundles = Path.Combine(assetsPath, Utility.AssetBundlesOutputPath);
            ZipEntry    entryAssetBundles = new ZipEntry(pathAssetBundles);
#endif
            //string      pathLua = Path.Combine(assetsPath, "lua");
            //ZipEntry    entryLua = new ZipEntry(pathLua);
            //EditorUtility.DisplayDialog("调试", targetZipFile + " | " + Directory.Exists(targetZipFile), "确认");
            UnityEngine.Debug.Log("target-" + targetZipFile);
            UnityEngine.Debug.Log("asset-" + assetsPath);
            FastZip fzip = new FastZip();
            //if (target == BuildTarget.iOS)
            if (false)
            {
#if UNITY_EDITOR_WIN
                string fileFilter = string.Format("+{0};+{1};+\\\\AssetBundles;+\\\\lua;-\\\\Temp", Global.AssetPlistName, Global.TablePlistName);
                fzip.CreateZip(targetZipFile, assetsPath, true, fileFilter);
#else
                string fileFilter = string.Format("+{0};+{1};+/AssetBundles/;+/lua/;-/Temp/", Global.AssetPlistName, Global.TablePlistName);
                fzip.CreateZip(targetZipFile, assetsPath, true, fileFilter);
#endif
            }
            else
            {
#if UNITY_EDITOR_WIN
                string fileFilter = string.Format("+{0};+{1};+\\\\lua;-\\\\Temp", Global.AssetPlistName, Global.TablePlistName);
                fzip.CreateZip(targetZipFile, assetsPath, true, fileFilter);
#else
                string fileFilter = string.Format("+{0};+{1};+/lua/;-/Temp/", Global.AssetPlistName, Global.TablePlistName);
                //string fileFilter = string.Format("+{0};+/lua/;-/Temp/", Global.AssetPlistName);
                fzip.CreateZip(targetZipFile, assetsPath, true, fileFilter);
#endif
            }
            /*
                        ZipFile zip = ZipFile.Create(targetZipFile);
                        zip.BeginUpdate();
                        //zip.AddDirectory(pathAssetBundles);
                        //zip.AddDirectory(pathLua);
                        zip.Add(entryAssetBundles);
                        zip.Add(entryLua);
                        zip.Add(Path.Combine(assetsPath, Global.LocalPlistName), Global.LocalPlistName);
                        zip.CommitUpdate();
                        zip.Close();
                        //fs.Close();
            */
            /*
			zip.AddFile(Path.Combine(assetsPath, Global.LocalPlistName), "");
			zip.AddDirectory(Path.Combine(assetsPath, Utility.AssetBundlesOutputPath), Utility.AssetBundlesOutputPath);
			zip.AddDirectory(Path.Combine(assetsPath, "lua"), "lua");
			zip.Save(Path.Combine(targetZipPath, Global.PackageName));
            */
        }
        AssetDatabase.StopAssetEditing();
    }

    private static void EnsurePathExist(string path)
	{
		if (Directory.Exists(path) == false)
		{
			Directory.CreateDirectory(path);
		}
	}

    /// <summary>
    /// 同步两个文件夹中的所有文件，
    /// 多余的文件将被删除
    /// </summary>
    private static void SyncFiles(string sourceDirectoryPath, string destDirectoryPath)
    {
		EnsurePathExist(destDirectoryPath);

        Directory.Delete(destDirectoryPath, true);
        
        CopyDirectory(sourceDirectoryPath, destDirectoryPath);

    }

    /// <summary>
    /// 同步多个目录到一个目录下
    /// 多余的文件将被删除
    /// </summary>
    private static void SyncDirectorys(string destDirectoryPath, string[] sourceDirectoryPathArray)
    {
        if (!string.IsNullOrEmpty(destDirectoryPath) && sourceDirectoryPathArray != null)
        {
			EnsurePathExist(destDirectoryPath);

            Directory.Delete(destDirectoryPath, true);
			
            for (int i = 0; i < sourceDirectoryPathArray.Length; i++)
            {
                CopyDirectory(sourceDirectoryPathArray[i], destDirectoryPath);
            }
        }

    }

    private static void CopyDirectory(string pSourceDirectory, string pTargetDirectory) 
    {
        if (!Directory.Exists(pSourceDirectory)) 
        {
            return;
        }

        string sourceDirectory = pSourceDirectory.Replace(@"\", @"/");
        string targetDirectory = pTargetDirectory.Replace(@"\", @"/");

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        DirectoryInfo sourceInfo = new DirectoryInfo(sourceDirectory);
        FileInfo[] fileInfo = sourceInfo.GetFiles();

        foreach (FileInfo fiTemp in fileInfo) 
        {
            UnityEngine.Debug.Log("Copy: " + fiTemp.Name + "  From : " + sourceDirectory + "   To : " + targetDirectory);
            File.Copy(sourceDirectory + "/" + fiTemp.Name, targetDirectory + "/" + fiTemp.Name, true);
        }

        DirectoryInfo[] diInfo = sourceInfo.GetDirectories();

        foreach (DirectoryInfo diTemp in diInfo) 
        {
            string sourcePath = diTemp.FullName.Replace(@"\", @"/");
            string targetPath = sourcePath.Replace(sourceDirectory, targetDirectory);
            Directory.CreateDirectory(targetPath);
            CopyDirectory(sourcePath,targetPath);
        }
    }

    /// <summary>
    /// 更新Package信息文件。
    /// Package中记录了版本信息、Hash值。
    /// </summary>
    //public static void UpdatePackageInfoFile(){
        /*
        string resPath = AppDataPath + "/StreamingAssets/";
        string newFilePath = resPath + "package.json";
        FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);
        JsonReader reader = new JsonReader(dataStr);
        YBRewardData[] datas = reader.Deserialize<YBRewardData[]>();
        dict = new Dictionary<int, YBRewardData>(datas.Length);
         */
    //}

    public static void BuildResourcePList(string[] resPaths, string[] subPaths, string newFilePath) {
        AssetDatabase.StartAssetEditing();
        UnityEngine.Debug.LogWarning("==== BuildFileIndex ====");
        //string resPath = Global.StreamingAssetsPath;
        ///----------------------创建文件列表-----------------------
        // 先收集所有要处理的文件。
        paths.Clear(); files.Clear();
        /*
        string assetBundlesPath = Path.Combine(resPath, Utility.AssetBundlesOutputPath);
        string luaPath = Path.Combine(resPath, "lua");
        Recursive(assetBundlesPath);
        if (!Directory.Exists(luaPath))
        {
            Directory.CreateDirectory(luaPath);
        }
        Recursive(luaPath);
        */
        //        foreach (string subPath in subPaths) {
        string plistText = "";
        for(int i=0; i<resPaths.Length; ++i) {
            string resPath = resPaths[i];
            string subPath = subPaths[i];
            plistText += BuildPListText(resPath, subPath);

            //string fullPath = Path.Combine(resPath, subPath);
            //if (Directory.Exists(fullPath)) {
            //    //Recursive(fullPath);
            //    GO.FileUtil.Recursive(fullPath, paths, files, ".meta");
            //}
        }

        //string newFilePath = Path.Combine(resPath, Global.LocalPlistName);

        // 如果文件已经存在，先删除它。
        if (File.Exists(newFilePath)) File.Delete(newFilePath);

        // 开始创建清单文件。
        FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);


        //for (int i = 0; i < files.Count; i++)
        //{
        //    string file = files[i];
        //    string ext = Path.GetExtension(file);
        //    if (file.EndsWith(".meta") || file.Contains(".DS_Store")) continue;
        //    string value = file.Replace(resPath, string.Empty);

        //    FileInfo fileInfo = new FileInfo(file);
        //    string md5 = Util.md5file(file);

        //    string size = fileInfo.Length.ToString();
        //    sw.WriteLine(value + "|" + md5 + "|" + size);
        //}
        sw.Write(plistText);
        sw.Close(); fs.Close();
        AssetDatabase.StopAssetEditing();
    }

    public static string BuildPListText(string path, string subPath) {
        path = path.Replace('\\', '/');
        subPath = subPath.Replace('\\', '/');
        string result = "";

        paths.Clear(); files.Clear();

        string fullPath = Path.Combine(path, subPath);
        if (Directory.Exists(fullPath))
        {
            //Recursive(fullPath);
            GO.FileUtil.Recursive(fullPath, paths, files, ".meta");
        }
        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i];
            string ext = Path.GetExtension(file);
            if (file.EndsWith(".meta") || file.Contains(".DS_Store")) continue;
            string value = file.Replace(path, string.Empty);

            FileInfo fileInfo = new FileInfo(file);
            string md5 = Util.md5file(file);

            string size = fileInfo.Length.ToString();
            result += value + "|" + md5 + "|" + size + "\n";
        }
        return result;
    }

    //  public static void GenerateFileTxt(IEnumerable<XElement> filterBundle)
    //  {
    //      BuildFileIndex(filterBundle);
    //  }

    //  //TODO:GL - 这个是个旧方法，之后应该可以不用了（确认好GenerateFileTxt不用之后）。
    //  static void BuildFileIndex(IEnumerable<XElement> filterBundle = null) {
    //      UnityEngine.Debug.LogWarning("==== BuildFileIndex ====");
    //      //string resPath = Path.Combine(AppDataPath, "StreamingAssets");
    //      string resPath = Global.StreamingAssetsPath;
    //      ///----------------------创建文件列表-----------------------
    //      string newFilePath = Path.Combine(resPath, Global.AssetPlistName);
    //      UnityEngine.Debug.LogWarning("newFilePath:" + newFilePath);
    //      if (File.Exists(newFilePath)) File.Delete(newFilePath);

    //      paths.Clear(); files.Clear();
    //      string assetBundlesPath = Path.Combine(resPath, Utility.AssetBundlesOutputPath);
    //      string luaPath = Path.Combine(resPath, "lua");
    //      Recursive(assetBundlesPath);
    //if (!Directory.Exists(luaPath)) {
    //	Directory.CreateDirectory(luaPath);
    //}
    //      Recursive(luaPath);

    //      FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
    //      StreamWriter sw = new StreamWriter(fs);
    //      for (int i = 0; i < files.Count; i++) {
    //          string file = files[i];
    //          string ext = Path.GetExtension(file);
    //          if (file.EndsWith(".meta") || file.Contains(".DS_Store")) continue;
    //          string value = file.Replace(resPath, string.Empty);
    //          if (filterBundle != null)
    //          {
    //              if (FilterBundle(value, filterBundle))
    //                  continue;
    //          }

    //          FileInfo fileInfo = new FileInfo(file);
    //          string md5 = Util.md5file(file);

    //          string size = fileInfo.Length.ToString();
    //          sw.WriteLine(value + "|" + md5 + "|" + size);
    //      }
    //      sw.Close(); fs.Close();
    //  }

    //private static bool FilterBundle(string fileName, IEnumerable<XElement> filterList)
    //{
    //    foreach (XElement item in filterList)
    //    {
    //        string bundlename = item.Attribute("Name").Value;

    //        string bundleManifest = bundlename + ".manifest";

    //        string[] macths = fileName.Split('/');

    //        string matchBundleName = macths[macths.Length - 1];

    //        if (matchBundleName.Equals(bundlename) || matchBundleName.Equals(bundlename + ".manifest"))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    /// <summary>
    /// Android平台复制Assetbunldes到StreamingAssets下
    /// </summary>
    /// <param name="target"></param>
    //public static void CopyBundles(BuildTarget target)
    //{
    //    //if (target != BuildTarget.iOS)
    //    {
    //        string assetsPath = Path.Combine(Global.PersistentDataPath, Utility.AssetBundlesOutputPath);
    //        string targetPath = Path.Combine(Application.dataPath, "StreamingAssets/" + Utility.AssetBundlesOutputPath);
    //        string oldPath = "/private" + targetPath;
    //        if (Directory.Exists(oldPath))
    //        {
    //            Directory.Delete(oldPath, true);
    //        }
    //        GO.FileUtil.DirectoryCopy(assetsPath, targetPath, true, () => { });
    //    }
    //}

    //  [MenuItem("Game Builder/Asset Builder/Build Protobuf-lua-gen File")]
    //  public static void BuildProtobufFile() {
    //      if (!AppConst.ExampleMode) {
    //          UnityEngine.Debug.LogError("若使用编码Protobuf-lua-gen功能，需要自己配置外部环境！！");
    //          return;
    //      }
    //string dir = AppDataPath + "/" + AppConst.AppName + "/Lua/3rd/pblua";
    //UnityEngine.Debug.Log(dir);
    //      paths.Clear(); files.Clear(); Recursive(dir);

    //      string protoc = "d:/protobuf-2.4.1/src/protoc.exe";
    //      string protoc_gen_dir = "\"d:/protoc-gen-lua/plugin/protoc-gen-lua.bat\"";

    //      foreach (string f in files) {
    //          string name = Path.GetFileName(f);
    //          string ext = Path.GetExtension(f);
    //          if (!ext.Equals(".proto")) continue;

    //          ProcessStartInfo info = new ProcessStartInfo();
    //          info.FileName = protoc;
    //          info.Arguments = " --lua_out=./ --plugin=protoc-gen-lua=" + protoc_gen_dir + " " + name;
    //          info.WindowStyle = ProcessWindowStyle.Hidden;
    //          info.UseShellExecute = true;
    //          info.WorkingDirectory = dir;
    //          info.ErrorDialog = true;
    //          Util.Log(info.FileName + " " + info.Arguments);

    //          Process pro = Process.Start(info);
    //          pro.WaitForExit();
    //      }
    //      AssetDatabase.Refresh();
    //  }

    public static void ClearDirectory(string path) {
        System.IO.DirectoryInfo di = new DirectoryInfo(path);

        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    //GL - 放到svn中执行了。
#if TO_DELETE
    public static void SyncToSVN(string[] sourcePaths, string svnPath){
        System.Console.WriteLine(svnPath);
        if (svnPath == null || svnPath == "" || !Directory.Exists(svnPath)){
            string errorMessage = string.Format("svn路径异常!路径：{0}\n请用命令行build package!", svnPath);
            EditorUtility.DisplayDialog("异常", errorMessage, "确认");
            return;
        }
        System.Console.WriteLine(string.Format("svn:{0}", svnPath));

        string assetsPath = Global.StreamingAssetsPath;
        if (!Directory.Exists(assetsPath)){
            string errorMessage = string.Format("数据源路径不存在!!!路径：{0}", assetsPath);
            EditorUtility.DisplayDialog("Error", errorMessage, "OK");
            return;
        }

        //ArrayList sources = new ArrayList();
        //foreach (string sourcePath in sourcePaths) {
        //    if (Directory.Exists(sourcePath)) {
        //        sources.Add(Path.Combine(assetsPath, sourcePath));
        //    }
        //}
        //sources.Add(Path.Combine(assetsPath, Utility.AssetBundlesOutputPath));
        //sources.Add(Path.Combine(assetsPath, "lua"));
        //string[] sourcePathList = sources.ToArray(typeof(string)) as string[];
        //foreach(string item in sourcePathList){
        //    System.Console.WriteLine("sources:" + item);
        //}

        string targetPath = svnPath;//Path.Combine(svnPath, "assets");
        // 先清空目录。
        ClearDirectory(targetPath);
        //SyncDirectorys(targetPath, sourcePathList);
        foreach (string sourcePath in sourcePaths)
        {
            string source = Path.Combine(assetsPath, sourcePath);
            string target = Path.Combine(targetPath, sourcePath);
            if (Directory.Exists(source))
            {
                SyncFiles(source, target);
            }
            else if (File.Exists(source)) {
                File.Copy(source, target, true);
            }
        }
        //SyncFiles(Path.Combine(assetsPath, Utility.AssetBundlesOutputPath), Path.Combine(targetPath, Utility.AssetBundlesOutputPath));
        //SyncFiles(Path.Combine(assetsPath, "lua"), Path.Combine(targetPath, "lua"));
        //File.Copy(Path.Combine(assetsPath, Global.AssetPlistName), Path.Combine(targetPath, Global.AssetPlistName), true);
    }
#endif  // TO_DELETE
}

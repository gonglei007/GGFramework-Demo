#if UNITY_EDITOR
    //#define LOAD_FROM_PROJECT
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using LuaInterface;
using SimpleJson;
#if UNITY_EDITOR
using UnityEditor;
#endif
using CsAsync;

/**
 * 数据管理系统。
 * 负责表（Json）数据的加载、获取等。数据在Excel中制作，由生产线导出成Json。
 * 优先读取PersistentDataPath下的资源，如果找不到，则加载StreamingAssets下的资源。
 */
public class DataSystem: Singleton<DataSystem>
{
    public static string LoadPath = Application.persistentDataPath + "/";
    private static Dictionary<string, Dictionary<string, JsonObject>> dataTables = new Dictionary<string, Dictionary<string, JsonObject>>();
    private static Dictionary<string, string> textTables = new Dictionary<string, string>();

    public static string LoadTextAsset(string dataPath)
    {
        Debug.Log("加载PersistantDataPath:" + dataPath);
        //从外部解压数据中加载数据，即从目录GetPersitentDataPath.PersistentDataPath加载
        string filePath = string.Format("{0}{1}{2}", LoadPath, dataPath, ".json");
        if (File.Exists(dataPath) == false)
        {
            return "";
        }
        return File.ReadAllText(dataPath);
    }

    public static IEnumerator LoadTableFromStreaming(string dataPath, Action<string> callback = null)
    {
        Debug.Log("加载StreamingDataPath:" + dataPath);
        Uri dataUri = new Uri(dataPath);
        UnityWebRequest www = UnityWebRequest.Get(dataUri);
        yield return www.SendWebRequest();

        if (string.IsNullOrEmpty(www.error))
        {
            byte[] data = www.downloadHandler.data;
            string textData = Encoding.UTF8.GetString(data);
            if (callback != null)
            {
                callback(textData);
            }
        }
        else
        {
            Debug.LogError(www.error + "\n" + dataPath);
        }
        yield return 0;
    }

    public static Dictionary<string, JsonObject> GetData(string name) {
        Dictionary<string, JsonObject> dataTable = null;
        if (!dataTables.ContainsKey(name))
        {
            throw new Exception("No table data!-" + name);
        }
        else {
            dataTable = dataTables[name];
        }
        return dataTable;
    }

    public static Dictionary<string, string> GetTextTables() {
        return textTables;
    }

    /**
     * 获取一个表文件的加载路径。这个路径跟程序运行的环境有关。
     */
    private static string GetTableFilePathByName(string name, out bool hasHotfixFile) {
        string tablePath = null;
        hasHotfixFile = false;
#if LOAD_FROM_PROJECT
        DirectoryInfo info = new DirectoryInfo(Global.TableEditorPath);
        FileInfo[] fileInfo = info.GetFiles(name, SearchOption.AllDirectories);
        if (fileInfo.Length <= 0)
        {
            throw new Exception("Not found this file:" + name);
        }
        else
        {
            FileInfo file = fileInfo[0];
            tablePath = file.FullName.Substring(info.FullName.Length - Global.TableEditorPath.Length);
        }
#else
        if (Directory.Exists(Global.TableHotfixPath)) {
            DirectoryInfo hotfixInfo = new DirectoryInfo(Global.TableHotfixPath);
            FileInfo[] hotfixFileInfo = hotfixInfo.GetFiles(name, SearchOption.AllDirectories);
            if (hotfixFileInfo.Length > 0)
            {
                FileInfo file = hotfixFileInfo[0];
                tablePath = file.FullName;
                hasHotfixFile = true;
            }
        }
        // 如果没有热更数据，就从包体中（streamingassets下）查找。
        if(!hasHotfixFile)
        {
            string[] fileList = BetterStreamingAssets.GetFiles(Global.TablePackagePath.Substring(Global.StreamingAssetsPath.Length), name, SearchOption.AllDirectories);
            if (fileList.Length <= 0) {
                throw new Exception("Not found this file:" + name);
            }
            else
            {
#if UNITY_EDITOR || UNITY_IOS
                tablePath = Global.StreamingAssetsPath + "/" + fileList[0];
#else
                tablePath = Global.StreamingAssetsPath+ fileList[0];
#endif
                //TODO: GL - 要正确的处理UNITY_EDITOR || UNITY_IOS定义。
                tablePath = Global.StreamingAssetsPath + "/" + fileList[0];

            }
            /*
            DirectoryInfo packageInfo = new DirectoryInfo(Global.TablePackagePath);
            FileInfo[] packageFileInfo = packageInfo.GetFiles(name, SearchOption.AllDirectories);
            if (packageFileInfo.Length <= 0)
            {
                throw new Exception("Not found this file:" + name);
            }
            else {
                FileInfo file = packageFileInfo[0];
                tablePath = file.FullName;
            }
            */
        }
#endif
                return tablePath;
    }

    /**
     * 加载（表）数据（Json）的接口。优先从本地存储地址查找（PersistentDataPath）
     */
    public static void LoadTableData(string relativePath, Action<string> callback)
    {
        string data = null;
        bool hasHotfixFile = false;
        string tablePath = GetTableFilePathByName(relativePath, out hasHotfixFile);
#if LOAD_FROM_PROJECT
        TextAsset tableData = AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath);
        if (tableData == null)
        {
            throw new Exception("Load table failed!-" + tablePath);
        }
        data = tableData.text;
        if (callback != null)
        {
            callback(data);
        }
#else
        if (hasHotfixFile)
        {
            data = DataSystem.LoadTextAsset(tablePath);
            if (callback != null) {
                callback(data);
            }
        }
        else {
            Debug.LogWarning("->>>"+tablePath);
            CoroutineUtil.DoCoroutine(DataSystem.LoadTableFromStreaming(tablePath, (string loadData)=> {
                data = loadData;
                if (callback != null)
                {
                    callback(data);
                }
            }));
        }
#endif
    }

    public static void LoadTableDataX(string relativePath, LuaFunction callback)
    {
        DataSystem.LoadTableData(relativePath, (data) => {
            if (callback != null) {
                callback.Call(data);
            }
        });
    }

    public static void LoadData(string relatedPath, Action<Dictionary<string, JsonObject>> callback) {
        Debug.Assert(callback != null, "callback will not be null!");
        string fileName = GeneratePath(relatedPath);
        //Debug.LogWarning("->" + fileName);
        try
        {
            DataSystem.LoadTableData(fileName, (string text) => {
                Dictionary<string, JsonObject> tableData = ParseTableText(relatedPath, text);
                callback(tableData);
            });
        }
        catch (Exception e)
        {
            GameDebugger.sPushLog("Load table failed!-" + e.ToString());
        }
    }

    public static void LoadDataX(string relatedPath, LuaFunction callback)
    {
        Debug.Assert(callback != null, "callback will not be null!");
        string fileName = GeneratePath(relatedPath);
        //Debug.LogWarning("->" + fileName);
        try
        {
            DataSystem.LoadTableData(fileName, (string text) => {
                //Debug.LogWarning("++" + text);
                Dictionary<string, JsonObject> tableData = ParseTableText(relatedPath, text);
                callback.Call(text);
            });
        }
        catch (Exception e) {
            GameDebugger.sPushLog("Load table failed!-"+e.ToString());
        }
    }

    public static void LoadAllData(string relatedPath, Action<string> onLoaded, Action<Exception> onFinish) {
        Debug.Log("开始加载所有数据表!-" + relatedPath);
        Dictionary<string, string> tableSet = new Dictionary<string, string>();
#if LOAD_FROM_PROJECT
        // 编辑器版本只在Assets/Hotfix.Tables目录下去遍历
        string path = Path.Combine(Global.TableEditorPath, relatedPath);
        DirectoryInfo info = new DirectoryInfo(path);
        FileInfo[] fileInfo = info.GetFiles("*.json", SearchOption.AllDirectories);
        int i = 0;
        Whilst whilst = new Whilst(
            // 测试
            () => {
                return i < fileInfo.Length;
            },
            // 执行
            (Action<Exception> callback) => {
                FileInfo file = fileInfo[i];
                string tableName = Path.GetFileNameWithoutExtension(file.FullName);
                string tablePath = file.FullName.Substring(info.FullName.Length-path.Length);
                TextAsset tableData = AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath);
                if (tableData == null)
                {
                    throw new Exception("Load table failed!-" + tablePath);
                }
                string data = tableData.text;
                ParseTableText(tableName, data);
                if (onLoaded != null)
                {
                    onLoaded(data);
                }
                ++i;
                callback(null);
            },
            // 结果
            (Exception e) => {
                if (e != null)
                {
                    GameDebugger.sPushLog(e.ToString());
                }
                else
                {
                    GameDebugger.sPushLog("Finish loading tables!");
                }
                if (onFinish != null) {
                    onFinish(e);
                }
            });
#else
        // 移动版本同时优先在Persistent下找，之后在StreamingAssets目录下找。并且不重复记录。
        if (Directory.Exists(Global.TableHotfixPath)) {
            string hotfixPath = Path.Combine(Global.TableHotfixPath, relatedPath);
            Debug.Log("开始加载Persistent下的数据表!-" + hotfixPath);
            DirectoryInfo hotfixInfo = new DirectoryInfo(hotfixPath);
            FileInfo[] hotfixFileInfo = hotfixInfo.GetFiles("*.json", SearchOption.AllDirectories);
            foreach (FileInfo file in hotfixFileInfo)
            {
                string tableName = Path.GetFileNameWithoutExtension(file.FullName);
                string tablePath = file.FullName;
                string textData = DataSystem.LoadTextAsset(tablePath);
                Debug.LogWarning("[P]>>>" + tablePath);
                ParseTableText(tableName, textData);
                if (onLoaded != null)
                {
                    onLoaded(textData);
                }
                tableSet[tableName] = tablePath;
            }
        }

        //string packagePath = Path.Combine(Global.TablePackagePath, relatedPath);
        //string tablesPathInStreaming = Global.TablePackagePath.Substring(Global.StreamingAssetsPath.Length);
        //string[] fileList = null;
        //try
        //{
        //    fileList = BetterStreamingAssets.GetFiles(tablesPathInStreaming, "*.json", SearchOption.AllDirectories);
        //}
        //catch (Exception e)
        //{
        //    Debug.LogWarning(e.ToString());
        //}
        Debug.Log("开始加载StreamingAssets下的数据表!");
        GetTablesInStreamingAssets((string[] fileList)=> {
            Debug.Log("已经获取加载文件的清单!-"+fileList.Length.ToString());
            //DirectoryInfo packageInfo = new DirectoryInfo(packagePath);
            //FileInfo[] packageFileInfo = packageInfo.GetFiles("*.json", SearchOption.AllDirectories);
            if (fileList == null)
            {

#if UNITY_EDITOR
                Debug.LogWarning("正式出包前在StreamingAssets下生成Tables目录和文件，确保可以不更新配表也能进行游戏。");
#else
                Debug.LogError("出包前在StreamingAssets下生成Tables目录和文件，确保可以不更新配表也能进行游戏。");
#endif
                if (onFinish != null)
                {
                    onFinish(null);
                }
                return;
            }
            foreach (string tfile in fileList)
            {
                Debug.LogWarning("--->遍历获得表文件:" + tfile);
            }

            //Debug.LogWarning("测试HeroStarData加载");
            //string testFileName = Global.StreamingAssetsPath + "/" + "Tables/Hero/HeroStarData.json";
            //CoroutineUtil.DoCoroutine(DataSystem.LoadTableFromStreaming(testFileName, (string textData) => {
            //    Debug.LogWarning("HeroStarData加载结果>>>" + testFileName);
            //}));
            //Debug.LogWarning("HeroStarData加载完成调用");

            LoadAllDataFromFileList(fileList, tableSet, onLoaded, onFinish);
        });
#endif
    }

    public static void LoadAllDataFromFileList(string[] fileList, Dictionary<string, string> ignoreTables, Action<string> onLoaded, Action<Exception> onFinish) {
        int i = 0;
        Whilst whilst = new Whilst(
            // 测试
            () => {
                //return i < packageFileInfo.Length;
                return i < fileList.Length;
            },
            // 执行
            (Action<Exception> callback) =>
            {
                //FileInfo file = packageFileInfo[i];
#if UNITY_EDITOR || UNITY_IOS
                string fileName = Global.StreamingAssetsPath + "/" + fileList[i];
#else
                string fileName = Global.StreamingAssetsPath + fileList[i];
#endif
                // 如果一个表
                //string tableName = Path.GetFileNameWithoutExtension(file.FullName);
                //string tablePath = file.FullName;
                string tableName = Path.GetFileNameWithoutExtension(fileName);
                string tablePath = fileName;
                ++i;
                if (ignoreTables.ContainsKey(tableName))
                {
                    callback(null);
                }
                else
                {
                    Debug.LogWarning("[S+]>>>" + tablePath);
                    CoroutineUtil.DoCoroutine(DataSystem.LoadTableFromStreaming(tablePath, (string textData) => {
                        //Debug.LogWarning("[S-]>>>" + tablePath);
                        ParseTableText(tableName, textData);
                        if (onLoaded != null)
                        {
                            onLoaded(textData);
                        }
                        callback(null);
                    }));
                }
            },
            // 结果
            (Exception e) => {
                if (e != null)
                {
                    GameDebugger.sPushLog(e.ToString());
                }
                else
                {
                    GameDebugger.sPushLog("Finish loading tables!");
                }
                if (onFinish != null)
                {
                    onFinish(e);
                }
            });
    }

    private static Dictionary<string, JsonObject> ParseTableText(string name, string text) {
        Dictionary<string, JsonObject> data = new Dictionary<string, JsonObject>();
        try
        {
            Debug.LogWarning("===>ParseTableText________" + name);
            JsonArray jsonArray = SimpleJson.SimpleJson.DeserializeObject<JsonArray>(text);
            for (int index = 0; index < jsonArray.Count; ++index)
            {
                JsonObject jsonRow = jsonArray[index] as JsonObject;
                string ID = jsonRow["ID"].ToString();
                data[ID] = jsonRow;
            }
            textTables[name] = text;
            dataTables[name] = data;
        }
        catch (Exception e) {
            Debug.LogErrorFormat("{0}表有错误，请检查一下！-{1}", name, e.ToString());
        }
        return data;
    }

    private static void GetTablesInStreamingAssets(Action<string[]> callback) {
        string plistFile = Path.Combine(Global.StreamingAssetsPath, Global.TablePlistName);
        CoroutineUtil.DoCoroutine(GO.FileUtil.LoadPlistFromStreamingAssets(plistFile,
            (Dictionary<string, GO.FileUtil.FileMeta> result)=> {
                List<string> tableFiles = new List<string>(100);
                foreach (var kvp in result)
                {
                    GO.FileUtil.FileMeta fileMeta = kvp.Value;
                    tableFiles.Add(fileMeta.path);
                }
                callback(tableFiles.ToArray());
            }
            ));
    }

    public static string GeneratePath(string relatedPath) {
        string realPath = relatedPath;
        if (relatedPath.Length > 5 && (relatedPath.Substring(relatedPath.Length - 4) == ".json"))
        {
            realPath = relatedPath.Substring(1, relatedPath.Length - 5);
        }
        string fileName = string.Format("{0}.json", realPath);
        return fileName;
    }
}

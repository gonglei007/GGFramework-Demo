#if UNITY_EDITOR
    #define   DOWNLOAD_ON             // 打开下载功能。
#endif
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using SimpleJson;
using System.Net;
using System.Net.Sockets;
//using CI.HttpClient;
//using GOLib.Tool;
using CsAsync;
using System.Security.Cryptography;
using GO;
using GGFramework;

/** 
 * （资源）热更新包管理。包括 下载、更新等。【注意】-首次安装解包。之后一直都从网络更新。
 * 功能是否开放跟当前客户端版本相关，开放热更有哪些版本是在后台帐号服配置的。
 * TODO:GL - 下载要支持断点续传。
 * TODO:GL - 要有状态存储，因为任意状态下（下载中、解包中...）都可能中断，要保证能够恢复状态。
 * 调试开关：HOTFIX_ON。开发版（Unity Editor版）可以通过打开这个宏来打开热更流程。发布版（手机版）默认是打开的。
 * 编辑器版：不走更新流程，直接从编辑目录加载资源。通过宏开关来控制EXTRACT_ON、DOWNLOAD_ON、DOWNLOAD_TABLES_ON。
 * Global.PackageName只在首次安装、启动的时候释放出来，之后就算有游戏包更新，也不再释放了，而是从服务器检查文件变化。
 */
public class PackageSystem : Singleton<PackageSystem> {
    public enum RunState {
        None = -1,
        Init,                   // [text-0:准备解压]
        FetchInfo,			    // [text-1:获取网络信息] 从服务器获取版本信息
        PrepareAssetDownload,   // [text-2:准备下载更新资源] 准备下载
        DownloadAssets,			// [text-3:准备下载更新资源] 下载Assets更新包
        VerifyAssets,           // [text-4:校验资源] 校验Assets.TODO:GL - 需要校验下载的数据
        SyncAssetFiles,	        // [text-5:同步新资源] 从下载包中解压资源
        Finish,                 // [text-6:初始化游戏] 
        StateCount              // 状态数量
    };

    RunState currentState = RunState.None;
    public RunState CurrentState {
        get {
            return this.currentState;
        }
    }

    public string localVersion = "1";
    public string remoteVersion = "1";
    //RemotePackageInfo[]		remotePackageList = new RemotePackageInfo[20];
    //public int remoteTableVersion = 1;

    public Action<string, string, bool, Action<bool>> onDialog = null;
    public Action<bool, Exception> OnFinish;            // bool: 数据是否有更新
    public Action<RunState> OnStateChange;
    public Action<int, int, RunState> OnProcess;
    public Action<string> OnLocalVersionChanged;
    //public Action<int> Event_DataVersionChanged;

    string downloadUrl = null;
    //string downDataTabelUrl = null;
    string plistName = null;
    string[] enableClientVersions = null;
    bool isHotfixOn = false;
    //bool isTableHotfixOn = false;
    ArrayList downloadFiles = new ArrayList();
    //string downloadTableZipFile = "";
    JsonObject verifyInfo = null;

    private string groupName = null;
    private bool isSyncFinish = false;
    private bool isDownloadFinish = false;

    private int downloadNumber = 0;
    private int downloadTotal = 0;

    // 调试用，统计时间
    float stateTimer = 0.0f;
    Dictionary<RunState, float> stateDurationTable = new Dictionary<RunState, float>();

    public string LocalVersion {
        get {
            return localVersion;
        }
        set {
            if (localVersion != value && OnLocalVersionChanged != null) {
                OnLocalVersionChanged(value);
            }
            localVersion = value;
        }
    }
    public string GroupName
    {
        get
        {
            return groupName;
        }
    }


    public int DownloadNumber
    {
        get
        {
            return downloadNumber;
        }
    }

    public int DownloadTotal
    {
        get
        {
            return downloadTotal;
        }
    }

    string TempPath {
        get {
            string path = Path.Combine(Global.PersistentDataPath, "Temp/");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }

    string DownloadTempPath {
        get {
            return Path.Combine(TempPath, "Download/");
        }
    }

    // Update is called once per frame
    void Update() {
        //stateTimer += Time.fixedDeltaTime;//.deltaTime;
        NetworkTest.Update();
        ProcessState();
    }

    private void Init(string groupName, Action<bool, Exception> onFinish)
    {
        this.groupName = groupName;
        this.localVersion = LoadVersionValue(this.groupName);
        this.localVersion = (this.localVersion == string.Empty) ? "1" : this.localVersion;
        stateTimer = Time.realtimeSinceStartup;
        stateDurationTable[RunState.None] = 0.0f;
        downloadNumber = 0;
        downloadTotal = 0;
        if (onFinish != null)
        {
            this.OnFinish = onFinish;
        }
    }

    public static void SaveVersionValue(string groupName, string version) {
        string versionFile = Global.PersistentDataPath + "/" + groupName + "_version.txt";
        StreamWriter writer = new StreamWriter(versionFile, false);
        writer.Write(version);
        writer.Close();
    }

    public static string LoadVersionValue(string groupName) {
        string version = "0";
        string versionFile = Global.PersistentDataPath + "/" + groupName + "_version.txt";
        if (File.Exists(versionFile)) {
            StreamReader reader = new StreamReader(versionFile);
            version = reader.ReadLine();
            reader.Close();
        }
        return version;
    }

    /// <summary>
    /// 获取本地数据（表）的版本。
    /// </summary>
    /// <returns></returns>
    public int GetLocalDataVersion()
    {
        string versionFile = Global.PersistentDataPath + "/" + Global.DataVersionFileName;
        if (File.Exists(versionFile))
        {
            int localVersion = Convert.ToInt32(File.ReadAllText(versionFile));
            return localVersion;
        }
        return 1;
    }

    private void UpdateLocalVersion() {
        this.LocalVersion = this.remoteVersion;
        SaveVersionValue(this.groupName, this.remoteVersion);
    }

    /// <summary>
    /// 运行包管理，启动状态。
    /// 1.检查ipa包内的资源是否需要解压。
    /// 	- 检查 & 解压 & 校验（失败了提示异常并退出）
    /// 2.检查是否需要远程更新解包【循环N次】。
    /// 	- 检查 & 下载 & 校验（失败了弹出重试提醒，点击重试）
    /// 	- 检查 & 解压 & 校验（失败了提示异常并退出）
    /// </summary>
    public void Run(string groupName, Action<bool, Exception> onFinish = null)
    {
        this.Init(groupName, onFinish);
        onFinish(true, null);return;
        ChangeState(RunState.Init);
    }

    IEnumerator EnterState(RunState state)
    {
        GameDebugger.Instance.PushLog(string.Format("PackageSystem::Enter State:{0}", state.ToString()));
        yield return new WaitForEndOfFrame();
        switch (state) {
            case RunState.Init:
                if (OnProcess != null)
                {
                    OnProcess(1, 1, this.currentState);
                }

                ChangeState(RunState.FetchInfo);
                break;
            case RunState.FetchInfo:
                DoFetchInfo();
                break;
            case RunState.PrepareAssetDownload:
                PrepareAssetDownload();
                break;
            case RunState.DownloadAssets:
                CoroutineUtil.DoCoroutine(DoDownloadAssets());
                break;
            case RunState.SyncAssetFiles:
                DoSyncFiles();
                break;
                /*
            case RunState.DownloadTables:
                DoDownloadTables();
                //routineTool.DoCoroutine(DoDownloadTables());
                break;
            case RunState.ExtractTables:
                DoExtractTableZip();
                break;
            case RunState.VerifyTables:
                DoVerifyTables();
                break;
                */
            case RunState.Finish:
                if (OnProcess != null)
                {
                    OnProcess(1, 1, this.currentState);
                }
                if (OnFinish != null) {
                    OnFinish(isHotfixOn, null);
                }
                break;
            default:
                break;
        }
    }

    void ProcessState()
    {
        switch (currentState) {
            case RunState.Init:
                break;
            case RunState.DownloadAssets:
                if (isDownloadFinish) {
                    ChangeState(RunState.SyncAssetFiles);
                    isDownloadFinish = false;
                }
                break;
            default:
                break;
        }
    }

    void ExitState(RunState state)
    {
        GameDebugger.Instance.PushLog(string.Format("PackageSystem::Exit State:{0} ——————————({1}s)", state.ToString(), stateDurationTable[state]));
        switch (state) {
            case RunState.Init:
                break;
            default:
                break;
        }
    }

    public void ChangeState(RunState state, bool force = false) {
        //DoChangeState(state);
        StartCoroutine(DoChangeState(state, force));
    }

    //public void DoChangeState(RunState state)
    public IEnumerator DoChangeState(RunState state, bool force = false)
    {
        //yield return new WaitForEndOfFrame();
        if (force || state != currentState)
        {
            stateDurationTable[currentState] = Time.realtimeSinceStartup - stateTimer;
            stateTimer = Time.realtimeSinceStartup;
            ExitState(currentState);
            currentState = state;
            if (OnStateChange != null) {
                OnStateChange(state);
            }
            yield return new WaitForEndOfFrame();
            StartCoroutine(EnterState(currentState));
        }
    }

    private bool MatchVersion(string version, string pattern) {
        const int versionCount = 3;
        string[] versionItems = version.Split('.');
        string[] patternItems = pattern.Split('.');
        if (versionItems.Length != versionCount || patternItems.Length != versionCount)
        {
            Debug.LogError(string.Format("Illegal client version format!!!version:{0} - pattern:{1}", version, pattern));
            return false;
        }
        bool matched = true;
        for (int i = 0; i < versionCount; ++i) {
            string versionItem = versionItems[i];
            string patternItem = patternItems[i];
            if (!patternItem.Equals("*") && !patternItem.Equals(versionItem)) {
                matched = false;
                break;
            }
        }
        return matched;
    }

    /// <summary>
    /// 检查版本，判断是否需要热更。
    /// TODO: 0.0.0 - 不进行检查，都
    /// </summary>
    /// <returns></returns>
    private bool CheckVersionEnable(JsonArray enableClientVersionList) {
        bool enable = false;
        string version = Global.GetClientVersion();
        for (int i = 0; i < enableClientVersionList.Count; ++i)
        {
            string enableVersion = enableClientVersionList[i].ToString();
            if (MatchVersion(version, enableVersion))
            {
                enable = true;
                break;
            }
        }
        return enable;
    }

    private bool CheckVersionChanged() {
        return this.localVersion != this.remoteVersion;
    }

    private void GetAssetInfoComplete(JsonObject response, Action<Exception> callback)
    {
        JsonArray dataUrlList = null;
        //JsonArray dataTableUrlList = null;
        string[] dataUrls = null;
        //string[] dataTableUrls = null;

        int code = Convert.ToInt32(response["code"]);

        if (NetworkConst.CODE_OK == code)
        {
            JsonObject data = response["data"] as JsonObject;
            // bundle assets 信息
            //downloadUrl = data["dataServerUrl"].ToString();
            plistName = data["plistName"].ToString();
            JsonArray enableClientVersionList = data["enableClientVersions"] as JsonArray;
            this.remoteVersion = data["version"].ToString();

            // 讀取數據鏈接列表
            dataUrlList = data["dataUrlList"] as JsonArray;
            dataUrls = Array.ConvertAll(dataUrlList.ToArray(), x => x.ToString());
            if (dataUrls.Length < 1) {
                callback(new Exception("Cannot get download url!!!"));
                return;
            }

            this.downloadUrl = dataUrls[0];
            //dataTableUrlList = data["dataVersionUrlList"] as JsonArray;
            //dataTableUrls = Array.ConvertAll(dataTableUrlList.ToArray(), x => x.ToString());
            isHotfixOn = CheckVersionEnable(enableClientVersionList);
            // 检查远程和本地的版本差异。
            // GL - 不检查版本变化，版本信息只用于显示，要根据实际文件清单来决定是否更新，防止异常发生。
            //isHotfixOn &= CheckVersionChanged();
#if UNITY_EDITOR
            if (this.groupName.Equals("asset")) {
                isHotfixOn &= GameSetting.IsAssetHotfixOn;
            }
            else if (this.groupName.Equals("table")) {
                isHotfixOn &= GameSetting.IsTableHotfixOn;
            }
#endif  //UNITY_EDITOR
            Debug.LogWarningFormat("本次的{0}资源是否需要进行更新？-{1}", groupName, isHotfixOn);
            callback(null);
            // 数据表版本信息
            //            remoteTableVersion = Convert.ToInt32(data["dataVersion"]);
            //            int localVersion = GetLocalDataVersion();
            //            isTableHotfixOn = remoteTableVersion != localVersion;
            //#if DOWNLOAD_TABLES_ON
            //            isTableHotfixOn = true;
            //#endif
            /*
                        NetworkTest.FindBestNetwork(dataUrls, (string assetUrl) =>
                        {
                            Debug.Log("寻找最快的AssetUrl");
                            for (int i = 0; i < dataUrls.Length; i++)
                            {
                                Debug.Log("服务器返回的AssetUrl：" + dataUrls[i]);
                            }

                            // 如果url為null，就保持老接口的數據鏈接
                            if (assetUrl != null)
                            {
                                downloadUrl = assetUrl;
                                Debug.Log("找到最快的AssetUrl:" + downloadUrl);
                                GameDebugger.Instance.PushLogFormat("The fastest assetbudnle url:{0}", assetUrl);
                            }

                            callback();
                        }, false);
            */
        }
        else
        {
            //TODO: GL -使用新的UI系统。
           // GameLauncher.ShowDialog
           // (
           //         TextSystem.GetText(NetworkConst.ERROR_MESSAGE),
           //         null,
           //         ConfirmStyle.YesNo, XiaoChanStyle.normal,
           //         (bool ret) =>
           //         {
           //             if (ret)
           //             {
           //                 GONetworkManager.Instance.GetAssetsInfo((JsonObject result) =>
           //                 {
           //                     GetAssetInfoComplete(result, callback);
           //                 });
           //             }
           //             else
           //                 Application.Quit();
           //         },
           //         null,
           //         TextManager.GetText("retry"),
           //         TextManager.GetText("quit"),
           //         false
           //);
        }
    }
    /*
    private void StartDownloadVerifyFile()
    {
        if (isTableHotfixOn)
        {
            DownloadVerifyFile((e) =>
            {
                //TODO:HintBox.Hide();
                if (e != null)
                {
                    Debug.LogError(e.ToString());
                }
                else
                {
                    Debug.Log("All fetch done!");
                    ChangeState(RunState.PrepareAssetDownload);
                }
            });
        }
        else
        {
            //TODO:HintBox.Hide();
            Debug.Log("All fetch done!");
            ChangeState(RunState.PrepareAssetDownload);
        }
    }
    */
    /// <summary>
    /// 获取服务器信息。主要是版本及更新包信息。
    /// </summary>
    public void DoFetchInfo(){
        // 获取当前版本。
        if (OnProcess != null)
        {
            OnProcess(1, 1, this.currentState);
        }
        //TODO:HintBox.Pop(TextManager.GetText("FetchUpdateInfo"));

        // 从服务器请求下载的更新描述信息。发送：package version, client version, channel。
        // 获取assets服务器信息
        //TODO: GL - 拉取资源信息。
        ///*
        JsonObject jsonParams = new JsonObject();
        jsonParams["deviceId"] = Global.GetDeviceUID();
        jsonParams["channel"] = Global.GetChannel();
        jsonParams["version"] = Global.GetClientVersion();
        jsonParams["platform"] = Global.GetPlatform();
        jsonParams["group"] = this.groupName;
        Debug.Log("GetHotfixInfo request:"+jsonParams.ToString());
        GameSystem.PostSystemServiceRequest(Global.VersionServiceName, "GetHotfixInfo", jsonParams, (JsonObject data) =>
        {
             Debug.Log("GetAssetsInfoFinish --------------- " + data.ToString());
             GetAssetInfoComplete(data,(Exception e)=>
             {
                 if (e != null)
                 {
                     //TODO: GL - 弹出异常窗口，重试。
                     GameDebugger.sPushLog("获取热更信息失败！！！"+e.ToString());
                 }
                 else {
                     ChangeState(RunState.PrepareAssetDownload);
                 }
                 //if (tableUrls.Length == 1)
                 //{
                 //    downDataTabelUrl = tableUrls[0];
                 //    StartDownloadVerifyFile();
                 //}
                 //else
                 //{
                 //    NetworkTest.FindBestNetwork(tableUrls, (fastUrl) =>
                 //    {
                 //        downDataTabelUrl = fastUrl;
                 //        StartDownloadVerifyFile();
                 //        GameDebugger.Instance.PushLogFormat("The fastest dataTable url:{0}", downDataTabelUrl);
                 //    }, false);
                 //}
             });
         });
         //*/
	}
/*
    /// <summary>
    /// 下载热更表校验文件并检查。
    /// </summary>
    /// <param name="callback"></param>
    private void DownloadVerifyFile(Action<Exception> callback)
    {
        Debug.Log("开始下载热更校验文件");
        string verifyFileUrl = downDataTabelUrl + remoteTableVersion + "/" + Global.TableVerifyFile;
        string verifyFilePath = Path.Combine(Global.PersistentDataPath, Global.TableVerifyFile);
        Uri verifyFileUri = new Uri(verifyFileUrl);
		Downloader.RequestItem item = new Downloader.RequestItem(verifyFileUri.ToString(), verifyFilePath, (float p) => 
        {

        }, null, (string com) =>
        {
			Downloader.Instance.ClearDownLoad();
            //StreamReader reader = null;
            string verifyData = File.ReadAllText(verifyFilePath);
            try
            {
                // 检查文件。
                //reader = new StreamReader(verifyFilePath);
                //verifyData = reader.ReadToEnd();
                verifyInfo = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(verifyData);
                callback(null);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Parse verify data error : " + verifyData);
                //TODO: GL - 使用新的UI系统
                //GameLauncher.ShowDialog(TextManager.GetText("data_error")+ e.Message, null, ConfirmStyle.Yes, XiaoChanStyle.normal,
                //    // 确定
                //    (bool ret) =>
                //    {
                //        GONetworkManager.instance.ActiveGame(GameStateGroup.PackageState, (int)PackingStateRecord.ReDownloadVerifyFile);
                //        DownloadVerifyFile(callback);
                //    },
                //    // 关闭
                //    () =>
                //    {

                //    },
                //    TextManager.GetText("retry"),
                //    ""
                //);
            }
            //finally
            //{
            //    if (reader != null)
            //    {
            //        reader.Close();
            //    }
            //}
        });
		Downloader.Instance.AddDownLoadItem(item);
    }
*/
    /// <summary>
    /// 准备下载信息。
    /// </summary>
    public void PrepareAssetDownload(){
        if (OnProcess != null)
        {
            OnProcess(1, 1, this.currentState);
        }
        // 跳过assets热更。
//#if !DOWNLOAD_ON
//        ChangeState(RunState.Finish);
//        return;
//#endif
        if (!isHotfixOn) {
            ChangeState(RunState.Finish);
            return;
        }
        Waterfall waterfall = new Waterfall();

		// 清空temp目录
        waterfall.AddTask((TaskCallback callback) =>
        {
		    if(Directory.Exists(this.DownloadTempPath)){
			    Directory.Delete(this.DownloadTempPath, true);
		    }
		    Directory.CreateDirectory(this.DownloadTempPath);
            callback.DoCallback(null);
        });

        string plistFilePath = Path.Combine(this.DownloadTempPath, plistName);
        string localPlistPath = Path.Combine(Global.PersistentDataPath, plistName);

        // 下载服务端文件清单。
        waterfall.AddTask((TaskCallback callback) =>
        {
            Uri downloadUri = new Uri(downloadUrl);
            Uri plistUri = new Uri(downloadUri, plistName);
			GameDebugger.Instance.PushLog(string.Format("[download]-\n{0}\n{1}\nexist:{2}", plistUri.ToString(), plistFilePath, File.Exists(plistFilePath)));
            DownloadSystem.Instance.RequestDownload(plistUri.ToString(), plistFilePath, (float p) =>
            {

            }, null, (string com) =>
            {
                DownloadSystem.Instance.ClearDownLoad();
                callback.DoCallback(null);
            });
        });

        int length = 0;
        this.downloadTotal = 0;
        this.downloadNumber = 0;
        // 比较本地清单，生成欲下载文件清单。
        waterfall.AddTask((TaskCallback callback) =>
        {
            // 比较本地清单，生成欲下载文件清单。
            this.downloadTotal = GenerateDownloadFileList(plistFilePath, localPlistPath, out length);
            GameDebugger.Instance.PushLog(string.Format("生成文件更新清单:{0}", this.downloadTotal));
            if (this.downloadTotal < 0)
            {
                // 生成清单异常。
                if (onDialog != null)
                {
                    onDialog(TextSystem.GetText("update-error"), TextSystem.GetText("plist-error"), true, (bool retry) =>
                    {
                        ChangeState(RunState.PrepareAssetDownload, true);
                        return;
                    });
                }
            }
            else {
                Debug.Assert(callback != null);
                if (callback == null)
                {
                    Debug.LogWarning("callback is null!!!");
                }
                callback.DoCallback(null);
            }
        });
        waterfall.Start((Exception e) =>
        {
            Debug.Log("准备资源流程结束");
            if (e != null)
            {
                // 有异常重新准备下载。
                //TODO: GL - 弹框确认，重试！
                ChangeState(RunState.PrepareAssetDownload, true);
            }
            else if (this.downloadTotal > 0)
            {
                //检查4G
                bool is4G = NetworkUtil.Is4GNetwork();
                GameDebugger.Instance.PushLog(string.Format("准备下载-是否为4G:{0}", is4G.ToString()));
                if (is4G && this.groupName.Equals("asset"))
                {
                    string fileSizeString = NetworkUtil.GetFileLengthString(length);
                    // 弹出下载更新提醒。点击确定，继续进入下载状态；点击取消，强制退出游戏。
                    GameDebugger.Instance.PushLog(string.Format("准备确认框"));
                    if (onDialog != null) {
                        onDialog(
                            TextSystem.GetText("update"), 
                            string.Format(TextSystem.GetText("update_prompt"), this.downloadTotal.ToString(), fileSizeString), 
                            false, 
                            (bool result) =>
                        {
                            if (result)
                            {
                                ChangeState(RunState.DownloadAssets);
                            }
                            else
                            {
#if !UNITY_EDITOR
							    Application.Quit();
#else
                                UnityEditor.EditorApplication.isPlaying = false;
#endif
                            }
                        });
                    }
                    //TODO: GL - 用新的UI系统替换！！！
                    /*
                    GameLauncher.ShowDialog(string.Format(TextManager.GetText("update_prompt"), downloadCount.ToString(), fileSizeString), TextManager.GetText("update"), 
                        ConfirmStyle.YesNo, XiaoChanStyle.normal,
                        (bool result) =>
                    {
                        if (result)
                        {
                            GONetworkManager.instance.ActiveGame(GameStateGroup.PackageState, (int)PackingStateRecord.DownloadAssetsWith4G);
                            ChangeState(RunState.DownloadAssets);
                        }
                        else
                        {
#if !UNITY_EDITOR
							Application.Quit();
#else
                            UnityEditor.EditorApplication.isPlaying = false;
#endif
                        }
                    },null,TextManager.GetText("button_confirm"),TextManager.GetText("button_cancel"),false);
                    */
                }
                else
                {
                    // 不詢問，直接下載。
                    GameDebugger.Instance.PushLog(string.Format("非4G直接下载！"));
                    ChangeState(RunState.DownloadAssets);
                }
            }
            else
            {
                // 没有需要下载的assets文件，直接结束。
                // 再更新一下本地版本，确保即使本地文件跟远程都一致的情况下，也更新一下本地的数据版本号。
                UpdateLocalVersion();
                GameDebugger.Instance.PushLog(string.Format("无资源需要更新！"));
                ChangeState(RunState.Finish);
            }
        });
	}

	/// <summary>
	/// 下载更新包
	/// </summary>
	public IEnumerator DoDownloadAssets(){
		if(downloadFiles.Count == 0){
			Debug.Log("没有文件需要更新！");
			ChangeState(RunState.Finish);
			//yield return null;
			yield break;
		}
		Debug.LogWarningFormat("将下载更新{0}个文件！", downloadFiles.Count);
		isDownloadFinish = false;
		int downloadCount = 0;

        if (downloadFiles.Count > 0)
        {
            for (int i = 0; i < downloadFiles.Count; i++)
            {
                string downloadFile = downloadFiles[i].ToString();
                string writeFilePath = this.DownloadTempPath.TrimEnd('/') + "/" + downloadFile.TrimStart('/');
                Uri fileUri = new Uri(downloadUrl.TrimEnd('/') + "/" + downloadFile.TrimStart('/'));
                Debug.Log("下载文件：" + fileUri.ToString());
                Debug.Log("到：" + writeFilePath.ToString());
                DownloadSystem.Instance.RequestDownload(fileUri.ToString(), writeFilePath, (float p) => { }, null, (string com) =>
                {
                    this.downloadNumber++;
                    if (OnProcess != null)
                    {
                        OnProcess(downloadCount + 1, downloadFiles.Count, this.currentState);
                    }

                    Debug.Log("下载成功!\n" + com);
                    isDownloadFinish = ++downloadCount == downloadFiles.Count;

                    if (isDownloadFinish)
                    {
                        DownloadSystem.Instance.ClearDownLoad();
                    }
                });
            }
        }
        else
        {
            isDownloadFinish = true;
        }
    }

	/// <summary>
	/// 将下载的文件从临时目录移动（覆盖）到本地存储目录。 
	/// </summary>
	public void DoSyncFiles(){
		//将下载数据同步到存储路径。
        string targetPath = Global.PersistentDataPath;
        string sourcePlistFile = Path.Combine(this.DownloadTempPath, plistName);
        string targetPlistFile = Path.Combine(Global.PersistentDataPath, plistName);
        //if (!File.Exists(targetPlistFile)) {
        //    targetPlistFile = Path.Combine(Global.StreamingAssetsPath, plistName);
        //}
        GO.FileUtil.SyncFiles(plistName, sourcePlistFile, this.DownloadTempPath, targetPlistFile, targetPath, (int progress, int totalCount) =>
        {
            if (OnProcess != null)
            {
                OnProcess(progress, totalCount, this.currentState);
            }
        });
		string sourceFilePath;
		string targetFilePath;
		/*
		for(int i=0; i<downloadFiles.Count; ++i){
			string filePath = downloadFiles[i].ToString();
			sourcePath = this.DownloadTempPath + filePath;
			targetPath = Global.PersistentDataPath + filePath;
			GameDebugger.Instance.PushLogFormat("[sync]source:" + sourcePath);
			GameDebugger.Instance.PushLogFormat("[sync]target:" + targetPath);
			string targetDir = Path.GetDirectoryName(targetPath);
			Debug.Log("[sync]-target dir:" + targetDir);
			if (Directory.Exists(targetDir) == false)
			{
				Directory.CreateDirectory(targetDir);
			}
			File.Copy(sourcePath, targetPath, true);
		}
 		*/
		//覆盖files.txt文件。
		sourceFilePath = Path.Combine(this.DownloadTempPath, this.plistName);
		targetFilePath = Path.Combine(Global.PersistentDataPath, this.plistName);
        Debug.LogFormat("[sync]-\nfrom:{0}\nto:{1}", sourceFilePath, targetFilePath);
        File.Copy(sourceFilePath, targetFilePath, true);

        UpdateLocalVersion();

        GameDebugger.sPushLog("数据更新完成->" + groupName + " Version:" + this.remoteVersion);
        ChangeState(RunState.Finish);
	}
/*
    public void DoDownloadTables() {
        if (!isTableHotfixOn) {
            ChangeState(RunState.Finish);
            return;
        }
        string dataFileUrl = downDataTabelUrl + remoteTableVersion + "/" + Global.TablePackageName;
        downloadTableZipFile = Global.PersistentDataPath + "/" + Global.TablePackageName;
		Downloader.RequestItem item = new Downloader.RequestItem(dataFileUrl.ToString(), downloadTableZipFile, (float p) => { }, null, (string com) =>
        {
			Downloader.Instance.ClearDownLoad();
            Debug.LogFormat("下载完成{0}", downloadTableZipFile);
            ChangeState(RunState.ExtractTables);
        });
		Downloader.Instance.AddDownLoadItem(item);
    }

    /// <summary>
    /// 释放数据表压缩包
    /// </summary>
    public void DoExtractTableZip() {
        Debug.Log("[package-extract-table]-" + downloadTableZipFile);
        CommonTools.ExtractZipFile(
            downloadTableZipFile, 
            null, 
            Global.PersistentDataPath + "/unzip", 
            (bool result) =>{
                if (!result)
                {
                    //TODO: GL - 用新的UI系统替换！
                    //GameLauncher.ShowDialog(
                    //    TextManager.GetText("extract_failed") + "\n" + downloadTableZipFile, 
                    //    null, 
                    //    ConfirmStyle.Yes, 
                    //    XiaoChanStyle.normal,
                    //    // 确定
                    //    (bool ret) =>
                    //    {
                    //        GONetworkManager.instance.ActiveGame(GameStateGroup.PackageState, (int)PackingStateRecord.ReDownloadTables);
                    //        ChangeState(RunState.DownloadTables);
                    //    },
                    //    // 关闭
                    //    () =>
                    //    {
                    //    },
                    //    TextSystem.GetText("retry"),
                    //    ""
                    //);
                }
                else {
                    ChangeState(RunState.VerifyTables);
                }
            },
            (int count, int total) =>
            {
                if (OnProcess != null)
                {
                    OnProcess(count, total, this.currentState);
                }
            }
        );
    }

    public void DoVerifyTables() {
        Debug.Log("开始校验热更数据");

        string unzipDirectory = Global.PersistentDataPath + "/unzip";
        string[] allFilePath = Directory.GetFiles(unzipDirectory, "*.json", SearchOption.AllDirectories);
        MD5 md5 = MD5.Create();
        bool verifySucceed = true;

        if (verifyInfo != null)
        {
            //先将本地的数据表全部删除
            string oldTables = Global.PersistentDataPath + "/Tables";

            if (Directory.Exists(oldTables))
            {
                FileUtil.DeleteFolder(oldTables);
            }
        }

        //将从服务器上下载的数据表写入persistentDataPath + "/Tables"
        foreach (string filePath in allFilePath)
        {
            string fileMd5 = BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(filePath))).Replace("-", "").ToLower(); ;
            string verifyKey = filePath.Substring(unzipDirectory.Length + 1);
            verifyKey = verifyKey.Replace('\\', '/');

            if (verifyInfo.ContainsKey(verifyKey))
            {
                string verifyMd5 = verifyInfo[verifyKey].ToString();
                //从服务器上拉取的数据表验证文件(verifyServer.json)中的MD5和本地算出来的MD5一致就通过校验
                verifySucceed = fileMd5.Equals(verifyMd5);
                if (verifySucceed)
                {
                    //校验通过，复制数据
                    string destFileName = filePath.Replace("unzip", "Tables");

                    try
                    {
                        string directoryName = Path.GetDirectoryName(destFileName);
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        File.Copy(filePath, destFileName, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                //TODO: GL - 用新的UI系统！
                //GameLauncher.ShowDialog(TextManager.GetText("verify_server_data_failed"), null, ConfirmStyle.NoButton);
                verifySucceed = false;
                break;
            }
        }

        //不管校验成功还是失败，都是删除中间文件
        File.Delete(downloadTableZipFile);

        Directory.Delete(unzipDirectory, true);

        //校验通过成功，更新数据版本号
        if (verifySucceed)
        {
            string versionFile = Global.PersistentDataPath + "/" + Global.DataVersionFileName;
            File.WriteAllText(versionFile, remoteTableVersion.ToString());
            ChangeState(RunState.Finish);
        }
        //校验失败，重新下载（整包重新下载）
        else
        {
            ChangeState(RunState.DownloadTables);
        }
    }
*/
	// 生成下载文件清单。
	private int GenerateDownloadFileList(string newPlist, string oldPlist, out int length){
		int newCount = 0;
        length = 0;
		downloadFiles.Clear();
        try
        {
            Dictionary<string, GO.FileUtil.FileMeta> oldFileTable = GO.FileUtil.LoadPlist(oldPlist);
            Dictionary<string, GO.FileUtil.FileMeta> newFileTable = GO.FileUtil.LoadPlist(newPlist);
            foreach (var kvp in newFileTable)
            {
                GO.FileUtil.FileMeta newFileMeta = kvp.Value;
                bool needDownload = false;
#if UNITY_EDITOR
                // GL-如果是编辑期版本，则所有表文件都要更新。为了让做包的时候打包的表数据都保持最新。
                if (this.groupName.Equals("table"))
                {
                    needDownload = true;
                }
#else
                if (!oldFileTable.ContainsKey(kvp.Key))
                {
                    needDownload = true;
                }
                else
                {
                    GO.FileUtil.FileMeta oldFileMeta = oldFileTable[kvp.Key];
                    if (newFileMeta.hash != oldFileMeta.hash)
                    {
                        needDownload = true;
                    }
                }
#endif
                if (needDownload)
                {
                    downloadFiles.Add(kvp.Key);
                    newCount++;
                    length += newFileMeta.length;
                    //Debug.LogFormat("预下载文件\n{0}\n{1}", kvp.Key, kvp.Value);
                }
            }
        }
        catch (Exception e) {
            return -1;
        }

        return newCount;
	}
}

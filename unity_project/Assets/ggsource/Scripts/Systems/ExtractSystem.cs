#if UNITY_EDITOR
    #define   EXTRACT_ON              // 强制打开压缩包释放功能。
#endif
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using CsAsync;
using System.Security.Cryptography;

/** 
 * （资源）释放管理。【注意】-首次安装解包。之后一直都从网络更新。
 * 编辑器版：不走更新流程，直接从编辑目录加载资源。通过宏开关来控制EXTRACT_ON。
 * Global.PackageName只在首次安装、启动的时候释放出来，之后就算有游戏包更新，也不再释放了，而是从服务器检查文件变化。
 */
public class ExtractSystem : Singleton<ExtractSystem>
{

    //public const string localPlistName = "files.txt";
    public const string BUNDLE_VERSION_FILE_NAME = "app_version.txt";
    /*
    private static ExtractSystem instance;

    public static ExtractSystem Instance
	{
		get
		{
			return instance;
		}
	}
    */

	public enum RunState{
		None = -1,			
		Init,                   // [text-0:准备解压]
        Sync,                   // [text-1:同步（包中的）文件到本地]
		Copy,                   // [text-2:复制（Global.PackageName）文件到本地]
        ExtractApp,		    	// [text-3:解压资源（不消耗流量）] 从app中解压资源

        Finish,                 // [text-4:完成] 
        StateCount              // 状态数量
	};

    /// <summary>
    /// 熱更新建的信息。
    /// </summary>
    struct FileMeta {
        public string hash;
        public int length;
    };

	RunState		currentState = RunState.None;
    public RunState CurrentState{
        get {
            return this.currentState;
        }
    }

	public string 				assetVersion = "1";
	//RemotePackageInfo[]		remotePackageList = new RemotePackageInfo[20];
    public int                  remoteTableVersion = 1;

	public Action				OnFinish;
	public Action<RunState>		OnStateChange;
	public Action<int, int, RunState>		OnProcess;
    public Action<Exception>    OnException;

    // 解压线程
    private Thread  thread = null;
	private bool	isSyncFinish = false;

	// 调试用，统计时间
	float						stateTimer = 0.0f;
	Dictionary<RunState, float>	stateDurationTable = new Dictionary<RunState, float>();

    string assetZipPath = null;

    public string AssetVersion {
        get {
            return assetVersion;
        }
    }

	string TempPath{
		get{
			string path = Path.Combine(Global.PersistentDataPath, "Temp/");
			Debug.Log(path);
			if(!Directory.Exists(path)){
				Directory.CreateDirectory(path);
			}
			return path;
		}
	}

	string DownloadTempPath {
		get{
			return Path.Combine(TempPath, "Download/");
		}
	}

	string AssetsStoragePath{
		get{
			return Global.PersistentDataPath;
		}
	}

	void Awake(){
		//instance = this;
		stateTimer = Time.realtimeSinceStartup;
		stateDurationTable[RunState.None] = 0.0f;
	}

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		ProcessState();	
	}

    public void Init(Action onFinish = null, Action<RunState> onStateChange = null, Action<int, int, RunState> onProcess = null, Action<Exception> onException = null)
    {
        this.OnFinish = onFinish;
        this.OnStateChange = onStateChange;
        this.OnProcess = onProcess;
        this.OnException = onException;
    }

    /// <summary>
    /// 运行包管理，启动状态。
    /// 1.检查ipa包内的资源是否需要解压。
    /// 	- 检查 & 解压 & 校验（失败了提示异常并退出）
    /// 2.检查是否需要远程更新解包【循环N次】。
    /// 	- 检查 & 下载 & 校验（失败了弹出重试提醒，点击重试）
    /// 	- 检查 & 解压 & 校验（失败了提示异常并退出）
    /// </summary>
    public IEnumerator Run()
	{
		yield return new WaitForEndOfFrame();
		// 加载状态
		ChangeState(RunState.Init);
		//yield return new WaitForEndOfFrame();
		yield return null;
		//TODO: GL - 其实状态要有记录，能够在意外中断之后恢复进程。
		// 从存储的状态开始运行。
		//currentState = (RunState)PlayerProfile.LoadPMState();
	}

    /// <summary>
    /// 运行包管理，启动状态。
    /// 1.检查ipa包内的资源是否需要解压。
    /// 	- 检查 & 解压 & 校验（失败了提示异常并退出）
    /// 2.检查是否需要远程更新解包【循环N次】。
    /// 	- 检查 & 下载 & 校验（失败了弹出重试提醒，点击重试）
    /// 	- 检查 & 解压 & 校验（失败了提示异常并退出）
    /// </summary>
    public void Run1()
    {
        //Debug.LogWarning("Thread>>>>>>" + Thread.CurrentThread.ManagedThreadId);
        ChangeState(RunState.Init);
        //TODO: GL - 其实状态要有记录，能够在意外中断之后恢复进程。
        // 从存储的状态开始运行。
        //currentState = (RunState)PlayerProfile.LoadPMState();
    }

	IEnumerator EnterState(RunState state)
	{
        //GONetworkManager.Instance.ActiveGame(GameStateGroup.ExtractState, (int)state);
        GameDebugger.Instance.PushLog(string.Format("ExtractManager::Enter State:{0}", state.ToString()));
		yield return new WaitForEndOfFrame();
		switch(state){
			case RunState.Init:
                DoProcess(0, 1, this.currentState);
                ChangeState(RunState.Sync);
				break;
            case RunState.Sync:
				StartCoroutine(DoSync());
                break;
			case RunState.Copy:
				StartCoroutine(DoCopy());
				break;
			case RunState.ExtractApp:
                DoExtractApp();
				break;
            case RunState.Finish:
                DoProcess(1, 1, this.currentState);
				if(OnFinish != null){
					OnFinish();
				}
				break;
			default:
			break;
		}
	}

    void ProcessState()
	{
		switch(currentState){
			case RunState.Init:
    			break;
			case RunState.ExtractApp:
				if(isSyncFinish)
                {
                    if (thread != null)
                    {
                        thread.Abort();
                        thread = null;
                    }
          
                    ChangeState(RunState.Finish);
					isSyncFinish = false;
				}
	    		break;
			default:
		    	break;
		}
	}

	void ExitState(RunState state)
	{
		//GameDebugger.Instance.PushLog(string.Format("ExtractManager::Exit State:{0} ——————————({1}s)", state.ToString(), stateDurationTable[state]));
		switch(state){
			case RunState.Init:
				break;
			default:
				break;
		}
	}

	public void ChangeState(RunState state){
        //DoChangeState(state);
		StartCoroutine(DoChangeState(state));
	}

	//public void DoChangeState(RunState state)
	public IEnumerator DoChangeState(RunState state)
	{
		//yield return new WaitForEndOfFrame();
		if(state != currentState){
			stateDurationTable[currentState] = Time.realtimeSinceStartup - stateTimer;
			stateTimer = Time.realtimeSinceStartup;
			ExitState(currentState);
			currentState = state;
			if(OnStateChange != null){
				OnStateChange(state);
			}
			yield return new WaitForEndOfFrame();
			StartCoroutine(EnterState(currentState));
		}
	}

    private void DoProcess(int value, int total, RunState state) { 
        if(OnProcess != null){
            OnProcess(value, total, state);
        }
    }

    /// <summary>
    /// 获取本地assets（热更数据）的版本（app程序包的版本）。
    /// </summary>
    /// <returns></returns>
    public string GetLocalBundleVersion()
    {
        string versionFile = Global.PersistentDataPath + "/" + BUNDLE_VERSION_FILE_NAME;
        if (File.Exists(versionFile))
        {
            string localVersion = File.ReadAllText(versionFile);
            return localVersion;
        }
        return "";
    }

    /// <summary>
    /// 检查是否需要从游戏包中解压数据到本地路径
    /// 如果是手机版，则先要检查是否已经释放过资源包（通过是否有files.txt文件来判断），如果没有释放过则先释放此文件。
    /// </summary>
    /// <returns></returns>
    public bool CheckNeedExtract()
	{
#if UNITY_EDITOR
        // 在编辑器非模拟模式下，不需要去释放压缩包。
        if (!Loxodon.Framework.Bundles.SimulationSetting.IsSimulationMode) {
            return false;
        }
#endif
        string localPlistPath = Path.Combine(AssetsStoragePath, Global.AssetPlistName);
        if (!File.Exists(localPlistPath))
        {
            return true;
        }

        string localBundleVersion = GetLocalBundleVersion();

        if (localBundleVersion != Global.GetClientVersion())
        {
            return true;
        }

        // --------
        return false;
        //GL - 以下逻辑先不走了，先只靠比较bundle版本是否变化来决定是否释放app中的资源包。
        // --------
		// 先检查Persistent目录，是否存在已解压数据。
		// 解析游戏包中（StreamingAssets）的版本号。
        int packageVersion = Convert.ToInt32((Resources.Load("PackageVersion") as TextAsset).text);


		string localVersionFile = "";//PERSISTENT_DATA_PATH + "/" + VERSION_FILE_NAME;

		// 如果本地不存在版本文件或者版本号低于游戏包中的版本，则需要进行解包操作。
		if (File.Exists(localVersionFile))
		{
			// 读取手机本地存储的版本号。
			int localVersion = Convert.ToInt32(File.ReadAllText(localVersionFile));
            bool needExtract = packageVersion > localVersion;
			return needExtract;
		}
		return true;
	}

    /**
     * 同步包中（跟本地不同的）文件到本地。
     * GL - ！！！用这个方案：
     * 不做真正的包内文件同步！
     * 把本地的bundle文件都清除掉！
     * 因为新的游戏包的内容是相对更新一些的，所以可以先用本地的，如果跟热更服上的有差异再进行更新。
     */
    public IEnumerator DoSync()
    {
        if (CheckNeedExtract())
        {
            string hotfixPath = Path.Combine(Global.PersistentDataPath, Global.HotfixPath);
            if (Directory.Exists(hotfixPath))
            {
                Directory.Delete(hotfixPath, true);
            }
#if !UNITY_EDITOR
            foreach (string filePath in Directory.GetFiles(Global.PersistentDataPath, "*", SearchOption.AllDirectories))
            {
                File.Delete(filePath);
            }
#endif
        }
        ChangeState(RunState.Copy);
        yield return new WaitForEndOfFrame();
    }

    /// <summary>
    /// 找到资源包路径,该方法只适用IOS,Android不支持DirectoryInfo类对StreamingAssets的访问
    /// </summary>
    /// <returns></returns>
    public void FindAssetZipPath(DirectoryInfo di)
    {
        FileInfo[] fis = di.GetFiles();
        for (int i = 0; i < fis.Length; i++)
        {
            if (fis[i].Name == Global.PackageName)
            {
                assetZipPath = fis[i].FullName;
            }
        }
        DirectoryInfo[] dis = di.GetDirectories();
        for (int j = 0; j < dis.Length; j++)
        {
            FindAssetZipPath(dis[j]);
        }
    }

    /**
     * 复制Global.PackageName到本地，用于下一步释放。
     */
    public IEnumerator DoCopy()
    {
#if UNITY_EDITOR
        //if (Loxodon.Framework.Bundles.SimulationSetting.IsSimulationMode)
        //{
        //    ChangeState(RunState.Finish);
        //    yield break;
        //}
        //else
        {
            string assetsZipFile = Path.Combine(Global.StreamingAssetsPath, Global.PackageName);

            if (!Loxodon.Framework.Bundles.SimulationSetting.IsSimulationMode && !File.Exists(assetsZipFile))
            {
                Debug.LogErrorFormat("因为未开启模拟,需要将{0}拷贝到本地目录,目前streamingAssetsPath/{0}不存在,请执行一下Package Builder命令", Global.PackageName);
                yield break;
            }
        }
#endif
        //GameDebugger.Instance.PushLog("[unzip]-start");
        // 检查是否为首次安装（之前未做过接包操作）。如果未解包，就将StreamingAssets(只读)中的数据解压复制到Persistent中。
        // 同时只有iOS版才需要复制资源包（Global.PackageName）到本地。
        if (CheckNeedExtract()){
            //if(CheckNeedExtract() && Application.platform == RuntimePlatform.IPhonePlayer){
            // 开始解包。从app路径（streamingassets）解压到手机存档路径（perssistent）


            assetZipPath = Path.Combine(Global.StreamingAssetsPath, Global.PackageName);


            if (string.IsNullOrEmpty(assetZipPath))
            {
                Debug.LogErrorFormat("{0} is not find", Global.PackageName);
                yield break;
            }
            string	storagePath = Global.PersistentDataPath;//Path.Combine("file://", GetPersitentDataPath.PersistentDataPath);
            ///*
            //GL - 第一步：复制Global.PackageName过去
            //GL【注意】，要先将zip文件复制到persistentDataPath目录下再进行解压，因为Zip模块无法直接从StreamingAssetsPath读取释放数据。
            string moveToFile = Path.Combine(Global.PersistentDataPath, Global.PackageName);
			//GameDebugger.Instance.PushLog(string.Format("[unzip]-copy:\nfrom:{0}\nto:{1}", packageFilePath, moveToFile));
            string password = Global.UnzipKey;
            if (Application.platform != RuntimePlatform.IPhonePlayer && password != null && password.Length != 0)
            {
                GameDebugger.Instance.PushLog("[error]-非苹果平台不应该有解压密码！");
            }
            yield return GO.FileUtil.SyncFilesFromStreaming2Persistent(
                assetZipPath, 
                moveToFile,
                password,
                (int offset, int length) =>
                {
                    DoProcess((int)offset, (int)length, currentState);
                }, 
                () => {
                    ChangeState(RunState.ExtractApp);
                }
                );
        }
        else
        {
            ChangeState(RunState.ExtractApp);
        }
        yield return new WaitForEndOfFrame();
    }

	/// <summary>
	/// 执行解包操作（步骤）。不能从streamingassets下直接解压数据，先要将zip文件下载到persistent目录下；
	/// 检查是否是首次安装（游戏包中的资源文件是否已经解压）
	/// </summary>
	//public IEnumerator DoExtractApp(){
	public void DoExtractApp()
    {
        if (CheckNeedExtract())
        {
            if (thread != null)
            {
                thread.Abort();
                thread = null;
            }

           thread = new Thread(StartExtract);
           thread.IsBackground = true;
           thread.Start();
        }
        else
        {
            isSyncFinish = true;
        }
	}

    /**
     * iOS版本要从压缩包释放资源。（为了多包时的数据加密）
     */
    private void StartExtract() {
        // 开始解包。从app路径（streamingassets）解压到手机存档路径（perssistent）
        string storagePath = Global.PersistentDataPath;//Path.Combine("file://", GetPersitentDataPath.PersistentDataPath);

        string moveToFile = Path.Combine(Global.PersistentDataPath, Global.PackageName);
        try
        {
            string password = null;// PlatformInterfaces.GetUnzipKey();
            //按目前使用xor加密的方案，zip文件不用密码了。PlatformInterfaces.GetUnzipKey用于xor解密。
            ZipUtil.ExtractGZipFile(moveToFile, password, storagePath,
                (bool result) =>
                {
                    if (!result)
                    {
                        string errorMessage = "extract_failed" + "\n" + moveToFile;
                        GameDebugger.Instance.PushLog(errorMessage);
                        if (this.OnException != null) {
                            this.OnException(new Exception(errorMessage));
                        }
                    }
                    else
                    {
                        string bundleVersionFile = Path.Combine(Global.PersistentDataPath, BUNDLE_VERSION_FILE_NAME);
                        File.WriteAllText(bundleVersionFile, Global.GetClientVersion());
                    }
                    isSyncFinish = result;
                },
                (int count, int total) =>
                {
                    DoProcess(count, total, this.currentState);
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}

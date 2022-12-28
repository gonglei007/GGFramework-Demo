using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 子线程兼容的配置信息。
 */
public class Global
{
    public enum Modes {
        Debug,
        Release
    }
    //public static string productUrl = "games.gotechgames.com";       // 产品服务Url
    //public static int productPort = 3001;                            // 产品服务端口
    public static string productAddress = "http://games.gotechgames.com:3002/{0}";   // 产品服务的地址
    public static string HttpSecretKey = "gosecret98b36e97159eaa9cbf8aaa35";
    public static string HotfixPath = "hotfix";
    public static string PackageName = "assets.zip";
    public static string VestPackageName = "vestAssets.zip";
    public const string AssetPlistName = "files.txt";
    public const string TablePlistName = "tables.txt";
    public const string TablePackageName = "achieve.zip";
    public const string DataVersionFileName = "DataVersion.txt";
    public const string TableVerifyFile = "verifyServer.json";
    public const string FrameworkBundleName = "uiframework";
    public static string PersistentDataPath = Application.persistentDataPath;
    public static string StreamingAssetsPath = Application.streamingAssetsPath;
    public static string AppVersion = Application.version;
    public static string UnzipKey = "";
    public static string TableEditorPath = "Assets/Hotfix.Tables/";
    public static string TablePackagePath = Application.streamingAssetsPath + "/Tables/";
    public static string TableHotfixPath = Application.persistentDataPath + "/Tables/";
    public static string VersionServiceName = "version";
    public static string AndroidInterfaceName = "com.go.common.NativeInterface";
    public static Modes Mode = Modes.Release;

    //private static string channel = "android-zhishang-uc-asdk-1-android";//"android-zhishang-ayouxi-asdk-1-android";
    private static string channel = "dev";//"android-zhishang-ayouxi-asdk-1-android";
    private static string android_channel = "gg-android";
    private static string ios_channel = "gg-ios";
    private static string version = "0.1.0";
    private static string deviceID = "";
    private static string deviceUID = null;

    public static void Init()
    {
        //PersistentDataPath = Application.persistentDataPath;
        //StreamingAssetsPath = Application.streamingAssetsPath;
        //ClientVersion = PlatformInterfaces.GetClientVersion();
        //UnzipKey = PlatformInterfaces.GetUnzipKey();
    }

    /// <summary>
    /// 获取客户端版本
    /// </summary>
    /// <returns></returns>
    public static string GetClientVersion() {
        return AppVersion;
        ////TODO: GL - move all code to PlatformInterface class!!!
        //if (Application.platform == RuntimePlatform.IPhonePlayer)
        //{
        //    //TODO: version = iOSInterfaces.CallGetAppVersion();
        //}
        //else if (Application.platform == RuntimePlatform.Android) {
        //    //TODO: version = AndroidInterfaces.CallGetAppVersion();
        //}
        //return version;
    }

    /// <summary>
    /// 获取完整版本信息。客户端版本+资源版本+表版本
    /// </summary>
    /// <returns></returns>
    public static string GetFullVersion() {
        string result = "Ver:x.x.x";
        result = string.Format("Ver{0}-a{1}-d{2}", GetClientVersion(), GameSystem.GetLocalVersion("asset"), GameSystem.GetLocalVersion("table"));
        return result;
    }

    public static string GetChannel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return android_channel;
        //channel = NativeLuaBridge.Instance.UnityCallNative("GetChannel", null);
#endif
#if UNITY_IOS && !UNITY_EDITOR
        return ios_channel;
#endif
        return channel;
    }

    public static string GetPlatform() {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            return "ios";
        }
        else if (Application.platform == RuntimePlatform.Android) {
            return "android";
        }
        return "android";
    }

    /// <summary>
    /// 获取设备唯一标识。iOS的是取IDFA，注意送审时的勾选。
    /// </summary>
    /// <returns></returns>
    public static string GetDeviceUID()
    {
        if (deviceUID == null)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                //deviceUID = iOSInterfaces.CallGetDeviceUID();
                deviceUID = SystemInfo.deviceUniqueIdentifier;
            }
            else
            {
                deviceUID = SystemInfo.deviceUniqueIdentifier;
            }
        }

        return deviceUID;
    }
}

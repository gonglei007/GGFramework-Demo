using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using ThinkingAnalytics;
using LuaFramework;

public static class IPManager
{
    public static string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
}

/// <summary>
/// 前端只记录最基本的数据（必须在前端记录的那些）。其它数据在后端记录。
/// TODO: 用户量大了后端要改成LogBug方式记录。
/// </summary>
public class StatisticSystem : Singleton<StatisticSystem>, IDynamicSuperProperties
{
    // 动态公共属性接口
    public Dictionary<string, object> GetDynamicSuperProperties()
    {
        return new Dictionary<string, object>() {
           {"KEY_DYNAMIC_Time", DateTime.Now},
           {"KEY_UTCTime", DateTime.UtcNow},
       };
    }

    public void Init()
    {
        // 设置 Distinct ID
        //ThinkingAnalyticsAPI.Identify(SystemInfo.deviceUniqueIdentifier);
        //ThinkingAnalyticsAPI.Identify(ThinkingAnalyticsAPI.GetDeviceId());

        // 清除公共事件属性
        ThinkingAnalyticsAPI.ClearSuperProperties();

        // 开启自动采集启动和关闭事件
        ThinkingAnalyticsAPI.EnableAutoTrack(AUTO_TRACK_EVENTS.ALL);

        Debug.Log("TA.TAExample - current disctinct ID is: " + ThinkingAnalyticsAPI.GetDistinctId());
        Debug.Log("TA.TAExample - the device ID is: " + ThinkingAnalyticsAPI.GetDeviceId());

        // 设置动态公共属性，传 this 是因为 this 实现了 IDynamicSuperProperties
        ThinkingAnalyticsAPI.SetDynamicSuperProperties(this);

        // Track 简单事件
        Scene scene = SceneManager.GetActiveScene();
        ThinkingAnalyticsAPI.Track("unity_start", new Dictionary<string, object>() {
            {"SCENE_NAME", scene.name}
        });

        if (scene.name == "scene1")
        {
            // 设置 SuperProperties
            Dictionary<string, object> superProperties = new Dictionary<string, object>()
            {
                {"super_date", DateTime.Now.AddDays(1)},
                {"super_string", "B1"},
                {"super_bool", true},
                {"super_number", 100}
            };
            ThinkingAnalyticsAPI.SetSuperProperties(superProperties);
            Dictionary<string, object> response = ThinkingAnalyticsAPI.GetSuperProperties();

            // 测试公共事件属性返回值
            if (response != null)
            {
                foreach (KeyValuePair<string, object> kv in response)
                {
                    if (kv.Value is DateTime)
                    {
                        Debug.LogWarning("TA.TAExample - Returned super property date: " + ((DateTime)kv.Value).ToString("yyyy-MM-dd"));
                    }
                    if (kv.Value is bool)
                    {
                        Debug.LogWarning("TA.TAExample - Returned super property bool: " + Convert.ToBoolean(kv.Value));
                    }

                    Debug.LogWarning("TA.TAExample - Returned super property: " + kv.Key + ": " + kv.Value);
                }

            }
        }

        ThinkingAnalyticsAPI.Flush();
    }

    public void Login() {
        UserSystem.UserData userData = UserManager.Instance.GetUserData();

        Dictionary<string, object> superProperties = new Dictionary<string, object>()
        {
            { "SERVER", userData.serverID},
            { "SERVER_NAME", userData.serverName},
            { "CHANNEL", Global.GetChannel()},
        };
        ThinkingAnalyticsAPI.SetSuperProperties(superProperties);
        ThinkingAnalyticsAPI.Login(userData.roleID);
        ThinkingAnalyticsAPI.UserSet(new Dictionary<string, object>() {
            { "SDKACCOUNT", UserSystem.Instance.UserInfo.account},
            { "SDKUID", UserSystem.Instance.UserInfo.uid}
        });
        ThinkingAnalyticsAPI.Flush();
    }

    public void SendEvent(string eventName)
    {
        ThinkingAnalyticsAPI.Track(eventName, new Dictionary<string, object>() {
            { "ip",  IPManager.GetLocalIPAddress()},
            { "deviceId", Global.GetDeviceUID()},
            { "channel",  Global.GetChannel()},
            { "version",  Global.GetClientVersion()}
        });
    }
    public void SendEvent(string eventName, string pName, object pValue)
    {
        ThinkingAnalyticsAPI.Track(eventName, new Dictionary<string, object>() {
            {pName, pValue},
            {"ip",  IPManager.GetLocalIPAddress()},
            { "deviceId", Global.GetDeviceUID()},
            { "channel",  Global.GetChannel()},
            { "version",  Global.GetClientVersion()}
        });
    }
}

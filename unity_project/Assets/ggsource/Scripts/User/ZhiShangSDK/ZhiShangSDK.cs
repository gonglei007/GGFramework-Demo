using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;

public class ZhiShangSDK: ISDK
{
    public ZhiShangSDK() {
        Debug.Log("ZhiShangSDK-事件监听");
        NativeLuaBridge.onNativeCallUnity += HandleSDKCallback;
    }

    public void HandleSDKCallback(string methodName, string jsonString)
    {
        Debug.LogFormat("HandleSDKCallback => method:{0}  param:{1}", methodName, jsonString);
        //UISystem.Instance.PopDialog(methodName, jsonString);
        switch (methodName) {
            case "InitSuccess":
                UserSystem.Instance.CallSDKInitSuccess("");
                break;
            case "InitFailed":
                UserSystem.Instance.CallSDKInitFailed("");
                break;
            case "LoginSuccess":
                //GL - 解析参数，填入uid，传给后端。
                try
                {
                    JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
                    UserSystem.User user = new UserSystem.User();
                    user.uid = jsonObject["accountid"].ToString();
                    user.channel = Global.GetChannel();
                    UserSystem.Instance.CallSDKLoginSuccess(user);
                }
                catch (Exception e)
                {
                    UISystem.Instance.PopDialog("登录异常", "解析参数有异常:" + e.ToString() + "" + jsonString);
                    throw e;
                }
                break;
            case "LoginFailed":
                UserSystem.Instance.CallSDKLoginFailed("");
                break;
            case "PaySuccess":
                UserSystem.Instance.CallSDKPaySuccess("");
                break;
            case "PayFailed":
                try
                {
                    JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
                    UserSystem.Instance.CallSDKPayFailed(jsonObject["customorgerid"].ToString());
                }
                catch (Exception e) {
                    UISystem.Instance.PopDialog("支付失败", "解析参数有异常:" + jsonString);
                    throw e;
                }
                break;
            case "PayCancel":
                {
                    JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
                    UserSystem.Instance.CallSDKPayCancel(jsonObject["customorgerid"].ToString());
                }
               
                break;
            case "LogoutSuccess":
                {
                    Application.Quit();
                }
                break;
            case "GameQuitDlg":
                {
                    UISystem.Instance.PopDialog(TextSystem.GetText("quit_dlg_title"),
                        TextSystem.GetText("quit_dlg_desc"),
                        (int)(UIDialog.Option.Yes | UIDialog.Option.No), (UIDialog.Option option) => {
                        if (option == UIDialog.Option.Yes)
                        {
                            Application.Quit();
                        }
                    });
                }
                break;
            default:
                break;
        }
    }

    public void Init() {
        NativeLuaBridge.Instance.UnityCallNative("Init", null);
    }

    public void Login() {
        NativeLuaBridge.Instance.UnityCallNative("Login", null);
    }

    public void Logout() {
        NativeLuaBridge.Instance.UnityCallNative("Logout", null);
    }

    public void Quit()
    {
        NativeLuaBridge.Instance.UnityCallNative("SdkQuit", null);
    }

    public void Pay(UserSystem.Order order, UserSystem.UserData userData = null) {
        NativeLuaBridge.Instance.UnityCallNative("Pay", order.GetJsonString());
    }

    public void SubmitData(string jsonData)
    {
#if SIMULATION_MODE_SDK
#else
        NativeLuaBridge.Instance.UnityCallNative("SubmitData", jsonData);
#endif
    }
}

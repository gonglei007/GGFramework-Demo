using System;
using System.IO;
using UnityEngine;

namespace GGFramework.Demo
{
    // Explicitly installed by the showcase, never selected by the production SDK factory.
    public sealed class ShowcaseSDK : ISDK
    {
        public enum PaymentResult { Success, Failed, Canceled }
        private readonly UserSystem owner;
        public bool FailNextLogin;
        public PaymentResult NextPayment;
        public string UID { get; private set; }

        public ShowcaseSDK(UserSystem owner)
        {
            this.owner = owner;
            string directory = Path.Combine(Application.persistentDataPath, "GGFrameworkShowcase");
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "demo-uid.txt");
            UID = File.Exists(file) ? File.ReadAllText(file).Trim() : "";
            if (UID.Length == 0)
            {
                UID = "demo-" + Guid.NewGuid().ToString("N").Substring(0, 12);
                File.WriteAllText(file, UID);
            }
        }

        public void Init() { owner.CallSDKInitSuccess("Offline simulator ready"); }
        public void Login()
        {
            if (FailNextLogin)
            {
                FailNextLogin = false;
                owner.CallSDKLoginFailed("Injected login failure; press Login to retry");
            }
            else owner.CallSDKLoginSuccess(new UserSystem.User(UID, "demo-token", "Developer", "showcase"));
        }
        public void Logout() { owner.CallSDKLogoutSuccess("Offline logout"); }
        public void Quit() { }
        public void SubmitData(string jsonData) { Debug.Log("[Showcase SDK] " + jsonData); }
        public void Pay(UserSystem.Order order, UserSystem.UserData userData = null)
        {
            if (!owner.HasLogined()) { owner.CallSDKPayFailed("Login required"); return; }
            switch (NextPayment)
            {
                case PaymentResult.Failed: owner.CallSDKPayFailed(order.id); break;
                case PaymentResult.Canceled: owner.CallSDKPayCancel(order.id); break;
                default: owner.CallSDKPaySuccess(order.id); break;
            }
        }
    }
}

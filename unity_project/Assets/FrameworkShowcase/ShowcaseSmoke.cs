using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.Demo
{
    /// <summary>Runs against the real scene and APIs in Play Mode, without clicking GUI controls.</summary>
    public static class ShowcaseSmoke
    {
        private static void Check(bool value, string name, List<string> passed)
        {
            if (!value) throw new Exception("FAIL: " + name);
            passed.Add("PASS: " + name);
            Debug.Log("[Showcase smoke] PASS: " + name);
        }

        public static IEnumerator Run(FrameworkShowcase demo, Action<bool, string> finished)
        {
            var passed = new List<string>();
            var stack = new Stack<IEnumerator>();
            stack.Push(Exercise(demo, passed));
            while (stack.Count > 0)
            {
                object current = null;
                bool next;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception e) { finished(false, string.Join("\n", passed) + "\n" + e); yield break; }
                if (!next) { stack.Pop(); continue; }
                if (current is IEnumerator child) stack.Push(child);
                else yield return current;
            }
            finished(true, string.Join("\n", passed));
        }

        private static IEnumerator Until(Func<bool> condition, string description)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(description);
                yield return null;
            }
        }

        private static IEnumerator Exercise(FrameworkShowcase demo, List<string> passed)
        {
            Check(!demo.ChangeFlow(FrameworkShowcase.Flow.Running), "FSM guard rejects without coins", passed);
            demo.PublishCoin();
            Check(demo.Coins == 1, "Typed event delivers payload", passed);
            demo.SubscribeCoins(false);
            demo.PublishCoin();
            Check(demo.Coins == 1, "Unsubscribe stops delivery", passed);
            demo.SubscribeCoins(true);
            Check(demo.ChangeFlow(FrameworkShowcase.Flow.Running), "FSM guard permits after event", passed);
            int token = demo.SpawnToken();
            demo.ReleaseToken();
            Check(demo.SpawnToken() == token, "Pool reuses same instance", passed);
            demo.ReleaseToken();
            demo.RunBackground();
            demo.RunCoroutine();
            yield return Until(() => demo.BackgroundCompletions == 1 && demo.CoroutineCompletions == 1, "Async completion");
            Check(GGFramework.Core.MainThreadDispatcher.IsMainThread, "Async completion observed on main thread", passed);

            demo.LoadResource();
            yield return Until(() => !demo.ResourceBusy, "Resource load");
            Check(demo.CachedResource != null && demo.CachedResource.refCount == 1, "GGResource loads bundled fixture", passed);
            var cached = demo.CachedResource;
            demo.LoadResource();
            Check(ReferenceEquals(cached, demo.CachedResource) && cached.refCount == 2, "Resource cache preserves identity and retains", passed);
            demo.ReleaseResource();
            Check(demo.CachedResource != null && cached.refCount == 1, "Shared resource survives first release", passed);
            demo.ReleaseResource();
            Check(demo.CachedResource == null, "Final release removes cached resource", passed);
            demo.LoadResource(true);
            yield return Until(() => !demo.ResourceBusy, "Missing resource");
            Check(demo.CachedResource == null && demo.ResourceStatus.Contains("不存在"), "Missing resource is recoverable", passed);
#if UNITY_EDITOR
            demo.LoadSimulation();
            yield return Until(() => !demo.BundleBusy, "Simulation resources");
            Check(demo.BundleResult.StartsWith("Simulation 加载成功"), "Simulation loads source asset: " + demo.BundleResult, passed);
#endif
            demo.LoadSampleBundle();
            yield return Until(() => !demo.BundleBusy, "Real AssetBundle");
            Check(demo.BundleResult.StartsWith("AssetBundle 加载成功"), "Manifest and real AssetBundle: " + demo.BundleResult, passed);

            demo.OpenInventory();
            demo.OpenInventory();
            demo.ShowDetails();
            Check(demo.CurrentPage.Name == "Inventory" && demo.WindowShowing, "Page stack and window open", passed);
            demo.HideDetails();
            Check(!demo.WindowShowing, "HideAllWindows tolerates dictionary removal", passed);
            demo.ShowDetails();
            demo.Back();
            Check(demo.CurrentPage.Name == "Home" && !demo.WindowShowing, "Back restores page and hides owned window without dialog", passed);

            demo.Login(true);
            yield return Until(() => demo.LastUserEvent == UserSystem.Events.SDKLoginFailed, "Injected login failure");
            Check(!demo.LoggedIn, "Login failure delivered", passed);
            demo.Login();
            yield return Until(() => demo.LoggedIn, "Login retry");
            // fsm.Changed broadcasts after entering the target state.
            yield return null;
            Check(demo.LastUserEvent == UserSystem.Events.SDKLoginSuccess, "Login retry reaches success", passed);
            int beforePay = demo.UserEventCount;
            demo.Pay(ShowcaseSDK.PaymentResult.Success);
            Check(demo.LastUserEvent == UserSystem.Events.SDKPaySuccess && demo.UserEventCount == beforePay + 1, "Payment success emits once", passed);
            demo.Pay(ShowcaseSDK.PaymentResult.Failed);
            Check(demo.LastUserEvent == UserSystem.Events.SDKPayFailed, "Payment failure delivered", passed);
            demo.Pay(ShowcaseSDK.PaymentResult.Canceled);
            Check(demo.LastUserEvent == UserSystem.Events.SDKPayCancel, "Payment cancellation delivered", passed);
            string uid = demo.SimulatedSDK.UID;
            demo.Logout();
            yield return Until(() => !demo.LoggedIn, "Logout");
            Check(demo.LastUserEvent == UserSystem.Events.SDKLogoutSuccess, "Logout callback delivered", passed);
            demo.Login();
            yield return Until(() => demo.LoggedIn, "Relogin");
            Check(demo.SimulatedSDK.UID == uid, "UID stable on relogin", passed);

            demo.SendRequest("success");
            yield return Until(() => demo.NetworkCode == 200, "HTTP success");
            Check(!demo.NetworkBusy, "Real loopback HTTP 200", passed);
            demo.SendRequest("business-error");
            yield return Until(() => demo.NetworkCode == 409, "HTTP business failure");
            Check(demo.HasNetworkDialog, "Business error feedback", passed);
            demo.ResolveNetworkDialog(false);
            demo.SendRequest("unavailable");
            yield return Until(() => demo.NetworkCode == 503 && demo.HasNetworkDialog, "HTTP 503");
            demo.ResolveNetworkDialog(false);
            yield return new WaitForSecondsRealtime(0.3f);
            Check(!demo.NetworkBusy && !demo.HasNetworkDialog, "Cancel retry leaves HTTP idle", passed);
            demo.SendRequest("success");
            yield return Until(() => demo.NetworkCode == 200, "HTTP recovery");
            Check(!demo.HasNetworkDialog, "HTTP recovers after failure", passed);
            demo.RunLua();
            Check(demo.LuaResult == "Lua reward: 256", "ToLua executes editable rule and returns result", passed);
        }
    }
}

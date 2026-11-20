#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using GGFramework.Core;
using UnityEngine;

namespace GGFramework.Demo
{
    /// <summary>Real Play Mode checks; owns only the showcase it creates/destroys.</summary>
    public static class ShowcaseLifecycleChecks
    {
        public static IEnumerator Run(FrameworkShowcase demo, Action<bool, string> finished)
        {
            var passed = new List<string>();
            var errors = new List<string>();
            bool expectedLuaError = false;
            Application.LogCallback listener = (message, trace, type) =>
            {
                if (!expectedLuaError && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                    && !trace.Contains("UnityEditor.Search")) errors.Add(message + "\n" + trace);
            };
            Application.logMessageReceived += listener;
            var stack = new Stack<IEnumerator>();
            stack.Push(Exercise(demo, passed, value => expectedLuaError = value));
            Exception failure = null;
            while (stack.Count > 0)
            {
                object current = null;
                bool next;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; break; }
                if (!next) { stack.Pop(); continue; }
                if (current is IEnumerator child) stack.Push(child);
                else yield return current;
            }
            Application.logMessageReceived -= listener;
            if (failure == null && errors.Count > 0) failure = new Exception("Unexpected runtime errors:\n" + string.Join("\n", errors));
            string header = "Unity=" + Application.unityVersion + "; platform=" + Application.platform + "; editor=" + Application.isEditor;
            finished(failure == null, header + "\n" + string.Join("\n", passed) + (failure == null ? "" : "\n" + failure));
        }

        private static void Check(bool condition, string name, List<string> passed, string evidence = "")
        {
            if (!condition) throw new Exception("FAIL: " + name + "; " + evidence);
            string line = "PASS: " + name + (evidence.Length == 0 ? "" : "; " + evidence);
            passed.Add(line);
            Debug.Log("[Showcase lifecycle] " + line);
        }

        private static IEnumerator Until(Func<bool> condition, string name)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(name);
                yield return null;
            }
        }

        private static void LuaSource(FrameworkShowcase demo, string source)
        {
            typeof(FrameworkShowcase).GetField("luaSource", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(demo, source);
        }

        private static int OwnedBundles()
        {
            return AssetBundle.GetAllLoadedAssetBundles().Count(bundle => bundle.name == "showcaseconfig");
        }

        private static IEnumerator Exercise(FrameworkShowcase demo, List<string> passed, Action<bool> expectLuaError)
        {
            Check(demo != null, "Showcase initialized", passed);
            var ui = UISystem.Instance;
            var resources = ResourceSystem.Instance;
            var events = demo.Events;
            Check(!demo.ChangeFlow(FrameworkShowcase.Flow.Running), "Guard rejects without coins", passed);
            demo.SubscribeCoins(false);
            demo.PublishCoin();
            Check(demo.Coins == 0, "Unsubscribed coin event has no effect", passed);
            demo.SubscribeCoins(true);
            demo.SubscribeCoins(true);
            demo.PublishCoin();
            Check(demo.Coins == 1 && events.SubscriberCount<FrameworkShowcase.CoinEvent>() == 1, "Restored subscription delivers once", passed);
            Check(demo.ChangeFlow(FrameworkShowcase.Flow.Running), "Guard permits after coin event", passed);
            Check(!demo.ChangeFlow(FrameworkShowcase.Flow.Running), "Repeated same-state entry rejected", passed);

            LuaSource(demo, "local coins = 128\nreturn 'Lua reward: ' .. (coins * 3)");
            demo.RunLua();
            Check(demo.LuaResult == "Lua reward: 384" && demo.Coins == 1, "Edited Lua rule returns value without changing C# coins", passed, demo.LuaResult);
            LuaSource(demo, "counter = (counter or 0) + 1; return tostring(counter)");
            demo.RunLua();
            Check(demo.LuaResult == "1", "First temporary Lua VM starts fresh", passed);
            demo.RunLua();
            Check(demo.LuaResult == "1", "Next temporary Lua VM has no prior globals", passed);
            LuaSource(demo, "local =");
            expectLuaError(true);
            demo.RunLua();
            expectLuaError(false);
            Check(demo.LuaResult.StartsWith("Lua 执行失败"), "Lua syntax error is reported", passed);
            LuaSource(demo, "local coins = 128\nreturn 'Lua reward: ' .. (coins * 2)");
            demo.RunLua();
            Check(demo.LuaResult == "Lua reward: 256", "Lua executes again after syntax error", passed);

            var source = Resources.Load<TextAsset>("FrameworkShowcase/Config");
            demo.LoadResource();
            yield return Until(() => !demo.ResourceBusy, "Initial resource load");
            var cached = demo.CachedResource;
            Check(cached != null && cached.refCount == 1, "First load owns one runtime copy", passed,
                "cached=" + (cached == null ? "null" : cached.refCount.ToString()) + ", status=" + demo.ResourceStatus);
            UnityEngine.Object owned = cached.assetObject;
            Check(!ReferenceEquals(source, owned), "Imported source and runtime copy differ", passed);
            demo.LoadResource();
            Check(ReferenceEquals(cached, demo.CachedResource) && cached.refCount == 2, "Second load retains the same cache identity", passed);
            demo.ReleaseResource();
            Check(cached.refCount == 1 && owned != null, "First release preserves other owner", passed);
            demo.ReleaseResource();
            yield return null;
            Check(demo.CachedResource == null && owned == null, "Final release removes cache and destroys runtime TextAsset", passed);
            Check(source != null && source.text.Length > 0, "Imported source survives runtime-copy release", passed);
            demo.LoadResource(true);
            yield return Until(() => !demo.ResourceBusy, "Missing resource");
            Check(demo.CachedResource == null && demo.ResourceStatus.Contains("不存在"), "Missing resource reaches failure terminal state", passed);
            demo.LoadResource();
            yield return Until(() => !demo.ResourceBusy, "Recovery after missing resource");
            Check(demo.CachedResource != null && demo.CachedResource.refCount == 1, "Normal resource reload works after missing asset", passed);

            int bundlesBefore = OwnedBundles();
            demo.LoadSampleBundle();
            yield return Until(() => !demo.BundleBusy, "Real sample Bundle");
            Check(demo.BundleResult.StartsWith("AssetBundle 加载成功"), "Real local Bundle reads configuration", passed);
            yield return null;
            yield return null;
            Check(OwnedBundles() == bundlesBefore, "Temporary Bundle loader leaves no additional sample Bundle", passed,
                "before=" + bundlesBefore + ", after=" + OwnedBundles());

            demo.OpenInventory();
            demo.ShowDetails();
            Check(demo.CurrentPage.Name == "Inventory" && demo.WindowShowing, "Owned page and window are active before close", passed);
            var foreignHost = new GameObject("Unrelated owner UI probe");
            var foreignWindow = foreignHost.AddComponent<UIWindow>();
            ui.SetUIBase(UISystem.UILayer.Window, "ForeignLifecycleProbe", foreignWindow);
            demo.SpawnToken();
            demo.SpawnToken();
            var active = ((List<GameObject>)typeof(FrameworkShowcase).GetField("active", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(demo)).ToArray();
            var oldAsset = demo.CachedResource.assetObject;
            demo.LoadResource();
            var address = new Uri(demo.ServerAddress);
            int background = demo.BackgroundCompletions;
            int coroutine = demo.CoroutineCompletions;
            int coins = demo.Coins;
            demo.RunBackground();
            demo.RunCoroutine();
            UnityEngine.Object.Destroy(demo.gameObject);
            yield return null;
            yield return null;
            Check(active.All(item => item == null), "Borrowed pool instances are destroyed with their owner", passed);
            Check(oldAsset == null && resources.GetObject(ResourceSystem.Tag.Other, "ShowcaseConfig") == null,
                "Exit releases all exclusive showcase cache holds", passed);
            Check(events.SubscriberCount<FrameworkShowcase.CoinEvent>() == 0, "Exit removes owned event subscriptions", passed);
            events.Publish(new FrameworkShowcase.CoinEvent { Amount = 7 });
            Check(demo.Coins == coins, "Publishing on retired bus cannot update old owner", passed);
            yield return new WaitForSecondsRealtime(0.5f);
            Check(demo.BackgroundCompletions == background && demo.CoroutineCompletions == coroutine,
                "Late background/coroutine results cannot update destroyed owner", passed);
            bool connected = false;
            try { using (var socket = new TcpClient()) { socket.Connect(address.Host, address.Port); connected = true; } }
            catch (SocketException) { }
            Check(!connected, "Exit closes the loopback listener", passed);
            Check(ReferenceEquals(ui.GetUIBase(UISystem.UILayer.Page, "Home"), null)
                && ReferenceEquals(ui.GetUIBase(UISystem.UILayer.Page, "Inventory"), null)
                && ReferenceEquals(ui.GetUIBase(UISystem.UILayer.Window, "Details"), null),
                "Exit removes owned UI registrations", passed);
            Check(ui.GetCurrentPage() == null, "Exit empties the owned page route", passed);
            Check(ReferenceEquals(ui.GetUIBase(UISystem.UILayer.Window, "ForeignLifecycleProbe"), foreignWindow),
                "Exit preserves another owner's UI registration", passed);
            ui.RemoveOwnedUI(UISystem.UILayer.Window, "ForeignLifecycleProbe", foreignWindow);
            UnityEngine.Object.Destroy(foreignHost);

            var reopenedHost = new GameObject("Reopened showcase lifecycle probe");
            var reopened = reopenedHost.AddComponent<FrameworkShowcase>();
            yield return null;
            Check(reopened.CurrentPage != null && reopened.CurrentPage.Name == "Home", "Showcase can reopen in the same Play Mode session", passed);
            var page = ui.GetPage("Home");
            Check(page.transform.IsChildOf(reopened.transform), "Reopened UI stays under its owner root", passed);
            reopened.PublishCoin();
            Check(reopened.Coins == 1 && reopened.Events.SubscriberCount<FrameworkShowcase.CoinEvent>() == 1,
                "Reopened owner has exactly one coin subscription", passed);
            UnityEngine.Object.Destroy(reopenedHost);
            yield return null;
            yield return null;
            Check(ui.GetCurrentPage() == null && ReferenceEquals(ui.GetUIBase(UISystem.UILayer.Page, "Home"), null),
                "Second exit leaves no owned UI route or registration", passed);
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RunFromPlayerArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--showcase-lifecycle") < 0) return;
            Application.runInBackground = true;
            string output = Path.Combine(Application.persistentDataPath, "GGFrameworkShowcase", "lifecycle");
            int index = Array.IndexOf(args, "--showcase-output");
            if (index >= 0 && index + 1 < args.Length) output = args[index + 1];
            Directory.CreateDirectory(output);
            MainThreadDispatcher.Instance.StartCoroutine(Run(UnityEngine.Object.FindFirstObjectByType<FrameworkShowcase>(), (success, report) =>
            {
                File.WriteAllText(Path.Combine(output, "lifecycle-results.txt"), (success ? "SUCCESS\n" : "FAILURE\n") + report);
                Application.Quit(success ? 0 : 1);
            }));
        }
#endif
    }
}
#endif

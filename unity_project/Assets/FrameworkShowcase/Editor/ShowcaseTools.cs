using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGFramework.Demo.Editor
{
    [InitializeOnLoad]
    public static class ShowcaseTools
    {
        public const string ScenePath = "Assets/FrameworkShowcase/Scenes/FrameworkShowcase.unity";
        private const string SmokeKey = "GGFramework.ShowcaseSmoke";
        private const string ExitKey = "GGFramework.ShowcaseSmoke.Exit";
        private static double deadline;

        static ShowcaseTools()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (SessionState.GetBool(SmokeKey, false))
            {
                deadline = EditorApplication.timeSinceStartup + 100;
                EditorApplication.update += Watchdog;
            }
        }

        [MenuItem("GGFramework/Demo/Open Showcase")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("GGFramework/Demo/Play Showcase")]
        public static void Play()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("GGFramework/Demo/Run Smoke Tests")]
        public static void RunSmokeTests()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before running smoke tests.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "ShowcaseBundles/manifest.json")))
                throw new InvalidOperationException("Run GGFramework/Demo/Build Sample Bundles before smoke tests.");
            SessionState.SetBool(SmokeKey, true);
            SessionState.SetInt(ExitKey, 1);
            deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update -= Watchdog;
            EditorApplication.update += Watchdog;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("GGFramework/Demo/Build Sample Bundles")]
        public static void BuildSampleBundles()
        {
            BuildSampleBundlesForTarget(EditorUserBuildSettings.activeBuildTarget);
        }

        private static void BuildSampleBundlesForTarget(BuildTarget target)
        {
            string directory = Path.Combine(Application.streamingAssetsPath, "ShowcaseBundles");
            Directory.CreateDirectory(directory);
            var builds = new[] { new AssetBundleBuild { assetBundleName = "showcaseconfig", assetNames = new[] { FrameworkShowcase.BundleAsset } } };
            var unityManifest = BuildPipeline.BuildAssetBundles(directory, builds, BuildAssetBundleOptions.ChunkBasedCompression, target);
            if (unityManifest == null) throw new Exception("Sample AssetBundle build failed.");
            string bundleFile = Path.Combine(directory, "showcaseconfig");
            BuildPipeline.GetCRCForAssetBundle(bundleFile, out uint crc);
            var info = new Loxodon.Framework.Bundles.BundleInfo("showcaseconfig", "", unityManifest.GetAssetBundleHash("showcaseconfig"),
                crc, new FileInfo(bundleFile).Length, "showcaseconfig", true, new[] { FrameworkShowcase.BundleAsset }, new string[0]);
            var manifest = new Loxodon.Framework.Bundles.BundleManifest(new System.Collections.Generic.List<Loxodon.Framework.Bundles.BundleInfo> { info }, "1.0.0");
            File.WriteAllText(Path.Combine(directory, "manifest.json"), manifest.ToJson());
            Debug.Log("Sample bundles built for " + target + " at " + directory);
        }

        [MenuItem("GGFramework/Demo/Build Windows Showcase")]
        public static void BuildWindowsShowcase()
        {
            BuildSampleBundlesForTarget(BuildTarget.StandaloneWindows64);
            Directory.CreateDirectory("output/showcase");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "output/showcase/GGFrameworkDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Showcase Player build failed: " + report.summary.result);
        }

        private static void Watchdog()
        {
            if (!SessionState.GetBool(SmokeKey, false)) { EditorApplication.update -= Watchdog; return; }
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > deadline) Finish(false, "Smoke watchdog timeout (100s).");
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(SmokeKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.isPaused = false;
                Application.runInBackground = true;
                var demo = UnityEngine.Object.FindFirstObjectByType<FrameworkShowcase>();
                if (demo == null) { Finish(false, "Showcase scene failed to initialize."); return; }
                demo.StartCoroutine(ShowcaseSmoke.Run(demo, Finish));
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(SmokeKey, false);
                EditorApplication.update -= Watchdog;
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(ExitKey, 1));
            }
        }

        private static void Finish(bool success, string report)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/showcase-smoke-results.txt", (success ? "SUCCESS\n" : "FAILURE\n") + report);
            SessionState.SetInt(ExitKey, success ? 0 : 1);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.isPlaying = false;

        }
    }
}

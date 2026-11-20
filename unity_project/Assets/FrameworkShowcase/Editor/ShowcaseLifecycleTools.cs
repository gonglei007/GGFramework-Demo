using System;
using System.IO;
using GGFramework.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGFramework.Demo.Editor
{
    [InitializeOnLoad]
    public static class ShowcaseLifecycleTools
    {
        private const string Pending = "GGFramework.ShowcaseLifecycle.Pending";
        private const string ExitCode = "GGFramework.ShowcaseLifecycle.Exit";
        private const string Cycles = "GGFramework.ShowcaseLifecycle.Cycles";
        private const string Completed = "GGFramework.ShowcaseLifecycle.Completed";
        private const string Restore = "GGFramework.ShowcaseLifecycle.Restore";
        private const string Options = "GGFramework.ShowcaseLifecycle.Options";
        private const string Enabled = "GGFramework.ShowcaseLifecycle.Enabled";
        private static double deadline;

        static ShowcaseLifecycleTools()
        {
            EditorApplication.playModeStateChanged += Changed;
            if (SessionState.GetBool(Pending, false))
            {
                deadline = EditorApplication.timeSinceStartup + 120;
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("GGFramework/Demo/Run Lifecycle Checks")]
        public static void Run()
        {
            Begin(1, false);
        }

        [MenuItem("GGFramework/Demo/Run Lifecycle Checks Twice Without Domain Reload")]
        public static void RunWithoutDomainReload()
        {
            ShowcasePrefabChecks.Run();
            Begin(2, true);
        }

        private static void Begin(int cycles, bool disableDomainReload)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before lifecycle checks.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "ShowcaseBundles/manifest.json")))
                throw new InvalidOperationException("Build Sample Bundles before lifecycle checks.");
            SessionState.SetInt(Cycles, cycles);
            SessionState.SetInt(Completed, 0);
            SessionState.SetBool(Restore, disableDomainReload);
            if (disableDomainReload)
            {
                SessionState.SetBool(Enabled, EditorSettings.enterPlayModeOptionsEnabled);
                SessionState.SetInt(Options, (int)EditorSettings.enterPlayModeOptions);
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            }
            SessionState.SetBool(Pending, true);
            SessionState.SetInt(ExitCode, 1);
            Enter();
        }

        private static void Enter()
        {
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(ShowcaseTools.ScenePath);
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false)) { EditorApplication.update -= Tick; return; }
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > deadline) Finish(false, "Lifecycle watchdog exceeded 120s.");
        }

        private static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Application.runInBackground = true;
                MainThreadDispatcher.Instance.StartCoroutine(ShowcaseLifecycleChecks.Run(UnityEngine.Object.FindFirstObjectByType<FrameworkShowcase>(), Finish));
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                int completed = SessionState.GetInt(Completed, 0) + 1;
                SessionState.SetInt(Completed, completed);
                if (SessionState.GetInt(ExitCode, 1) == 0 && completed < SessionState.GetInt(Cycles, 1))
                {
                    EditorApplication.delayCall += Enter;
                    return;
                }
                if (SessionState.GetBool(Restore, false))
                {
                    EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Options, 0);
                    EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Enabled, false);
                    SessionState.SetBool(Restore, false);
                    File.AppendAllText("Logs/showcase-lifecycle-results.txt", "\nEditor settings restored; completed Play Mode exits=" + completed + "\n");
                }
                SessionState.SetBool(Pending, false);
                EditorApplication.update -= Tick;
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(ExitCode, 1));
            }
        }

        private static void Finish(bool success, string report)
        {
            Directory.CreateDirectory("Logs");
            string result = (success ? "SUCCESS\n" : "FAILURE\n") + report;
            File.WriteAllText("Logs/showcase-lifecycle-results.txt", result);
            File.WriteAllText("Logs/showcase-lifecycle-cycle-" + (SessionState.GetInt(Completed, 0) + 1) + ".txt", result);
            SessionState.SetInt(ExitCode, success ? 0 : 1);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.isPlaying = false;
        }
    }
}

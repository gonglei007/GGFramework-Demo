using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace GGFramework.Demo
{
    public partial class FrameworkShowcase
    {
        private void Start()
        {
#if !UNITY_EDITOR
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--showcase-smoke") >= 0)
            {
                Application.runInBackground = true;
                StartCoroutine(RunPlayerSmoke());
            }
#endif
        }

        private IEnumerator RunPlayerSmoke()
        {
            string output = Path.Combine(Application.persistentDataPath, "GGFrameworkShowcase", "verification");
            string[] args = Environment.GetCommandLineArgs();
            int outputIndex = Array.IndexOf(args, "--showcase-output");
            if (outputIndex >= 0 && outputIndex + 1 < args.Length) output = args[outputIndex + 1];
            Directory.CreateDirectory(output);
            bool success = false;
            string report = "Smoke runner did not finish.";
            yield return ShowcaseSmoke.Run(this, (passed, text) => { success = passed; report = text; });
            File.WriteAllText(Path.Combine(output, "player-smoke-results.txt"), (success ? "SUCCESS\n" : "FAILURE\n") + report);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                // The legacy project has a splash screen. Capturing before it finishes yields black frames.
                float deadline = Time.realtimeSinceStartup + 15;
                while (!UnityEngine.Rendering.SplashScreen.isFinished && Time.realtimeSinceStartup < deadline) yield return null;
                foreach (int tab in new[] { 0, 1, 2, 4, 5 })
                {
                    selected = tab;
                    contentScroll = Vector2.zero;
                    yield return new WaitForSecondsRealtime(0.2f);
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "showcase-" + tab + ".png"));
                    yield return null;
                }
                // Let asynchronous screenshot file writes finish before exiting the Player.
                yield return new WaitForSecondsRealtime(0.5f);
            }
            Application.Quit(success ? 0 : 1);
        }
    }
}

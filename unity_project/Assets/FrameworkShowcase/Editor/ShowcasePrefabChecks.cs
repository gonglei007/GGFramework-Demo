using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGFramework.Demo.Editor
{
    public static class ShowcasePrefabChecks
    {
        public static void Run()
        {
            var report = new System.Text.StringBuilder("Unity=" + Application.unityVersion + "\n");
            foreach (string name in new[] { "UISwitch_AllComplete", "UISwitch_in", "UISwitch_out" })
            {
                string path = "Assets/Hotfix.Bundles/Framework/Prefab/UIFx/" + name + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new Exception("Cannot import " + path);
                int missing = 0;
                foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                int sprites = prefab.GetComponentsInChildren<UISprite>(true).Length;
                int widgets = 0;
                foreach (var widget in prefab.GetComponentsInChildren<UIWidget>(true))
                    if (widget.GetType() == typeof(UIWidget)) widgets++;
                if (missing != 0 || sprites != 8 || widgets != 1)
                    throw new Exception(path + ": missing=" + missing + ", sprites=" + sprites + ", widgets=" + widgets);
                report.AppendLine("PASS: " + name + "; missing=0; UISprite=8; plain UIWidget=1");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/showcase-prefab-results.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }
}

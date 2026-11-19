using System;
using System.Collections;
using System.IO;
using Loxodon.Framework.Bundles;
using UnityEngine;
using Path = System.IO.Path;

namespace GGFramework.Demo
{
    public partial class FrameworkShowcase
    {
        public const string BundleAsset = "Assets/FrameworkShowcase/Resources/FrameworkShowcase/Config.json";
        public string BundleResult { get; private set; } = "先用菜单 GGFramework/Demo/Build Sample Bundles 生成当前平台示例包。";
        private bool bundleBusy;
        public bool BundleBusy { get { return bundleBusy; } }

        public void LoadSampleBundle()
        {
            if (bundleBusy) return;
            string directory = Path.Combine(Application.streamingAssetsPath, "ShowcaseBundles");
            string manifestFile = Path.Combine(directory, "manifest.json");
            if (!File.Exists(manifestFile)) { BundleResult = "示例 Bundle 尚未构建，请先运行 Build Sample Bundles。"; Log(BundleResult); return; }
            StartCoroutine(LoadBundleConfig(directory, manifestFile));
        }
        private IEnumerator LoadBundleConfig(string directory, string manifestFile)
        {
            bundleBusy = true;
            BundleManifest manifest;
            try { manifest = new BundleManifestLoader().Load(manifestFile); }
            catch (Exception e) { bundleBusy = false; BundleResult = "清单读取失败：" + e.Message; yield break; }
            using (var loader = new BundleResources(new SimplePathInfoParser("@"),
                new BundleManager(manifest, new FileAsyncBundleLoaderBuilder(new Uri(Path.GetFullPath(directory) + Path.DirectorySeparatorChar)))))
            {
                var result = loader.LoadAssetAsync<TextAsset>("showcaseconfig@" + BundleAsset);
                while (!result.IsDone) yield return null;
                BundleResult = result.Exception != null ? "Bundle 加载失败：" + result.Exception.Message
                    : result.Result == null ? "Bundle 内未找到配置" : "AssetBundle 加载成功：" + result.Result.text;
            }
            bundleBusy = false;
            Log(BundleResult);
        }

#if UNITY_EDITOR
        public void LoadSimulation()
        {
            if (!bundleBusy) StartCoroutine(LoadSimulationConfig());
        }
        private IEnumerator LoadSimulationConfig()
        {
            bundleBusy = true;
            using (var loader = new SimulationResources(new SimplePathInfoParser("@")))
            {
                var result = loader.LoadAssetAsync<TextAsset>("showcaseconfig@" + BundleAsset);
                while (!result.IsDone) yield return null;
                BundleResult = result.Exception != null ? "Simulation 失败：" + result.Exception.Message
                    : result.Result == null ? "Simulation 未找到配置" : "Simulation 加载成功：" + result.Result.text;
            }
            bundleBusy = false;
            Log(BundleResult);
        }
#endif
    }
}

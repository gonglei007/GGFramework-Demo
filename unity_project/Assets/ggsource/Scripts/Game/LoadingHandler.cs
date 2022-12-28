using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingHandler : Singleton<LoadingHandler>
{
    Dictionary<int, RangeInt> percentRangeMap = new Dictionary<int, RangeInt>();

    private void Awake()
    {
        percentRangeMap.Add((int)GameSystem.States.Init, new RangeInt(0, 1));
        percentRangeMap.Add((int)GameSystem.States.InitNetwork, new RangeInt(1, 3));
        percentRangeMap.Add((int)GameSystem.States.Extract, new RangeInt(4, 6));
        percentRangeMap.Add((int)GameSystem.States.HotfixTables, new RangeInt(10, 20));
        percentRangeMap.Add((int)GameSystem.States.HotfixAssets, new RangeInt(30, 50));
        percentRangeMap.Add((int)GameSystem.States.InitFramework, new RangeInt(80, 2));
        percentRangeMap.Add((int)GameSystem.States.LoadTables, new RangeInt(82, 3));

        percentRangeMap.Add((int)GameSystem.States.StartStages + (int)StageSystem.States.LoadLua, new RangeInt(85, 2));
        percentRangeMap.Add((int)GameSystem.States.StartStages + (int)StageSystem.States.LoadResource, new RangeInt(87, 3));
        percentRangeMap.Add((int)GameSystem.States.StartStages + (int)StageSystem.States.InitStage, new RangeInt(90, 10));
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UILoading loading = UISystem.Instance.GetLoadingUI();
        if (loading == null || !loading.IsShowing()) {
            return;
        }
        int gamePhase = (int)GameSystem.Instance.GetCurrentState();
        int stagePhase = (int)StageSystem.Instance.GetCurrentState();
        int phaseID = 0;
        float phasePercent = 0.0f;
        string mainInfo = "";
        string subInfo = "......";
        switch (GameSystem.Instance.GetCurrentState()) {
            case GameSystem.States.Init:
            case GameSystem.States.InitNetwork:
            case GameSystem.States.Extract:
            case GameSystem.States.InitFramework:
            case GameSystem.States.LoadTables:
                phaseID = gamePhase;
                phasePercent = 1.0f;
                break;
            case GameSystem.States.HotfixTables:
            case GameSystem.States.HotfixAssets:
                if (PackageSystem.Instance.DownloadTotal > 0) {
                    phasePercent = (float)PackageSystem.Instance.DownloadNumber / (float)PackageSystem.Instance.DownloadTotal;
                }
                phaseID = gamePhase;
                if (PackageSystem.Instance.DownloadTotal > 0)
                {
                    subInfo = string.Format("{0}({1}/{2})", TextSystem.GetText(string.Format("hotfix_text_{0}", (int)PackageSystem.Instance.CurrentState)), PackageSystem.Instance.DownloadNumber, PackageSystem.Instance.DownloadTotal);
                }
                else
                {
                    subInfo = TextSystem.GetText(string.Format("hotfix_text_{0}", (int)PackageSystem.Instance.CurrentState));
                }
                break;
            case GameSystem.States.StartStages:
                switch (StageSystem.Instance.GetCurrentState()) {
                    case StageSystem.States.LoadResource:
                        if (ResourceSystem.Instance.LoadTotal > 0)
                        {
                            phasePercent = (float)ResourceSystem.Instance.LoadNumber / (float)ResourceSystem.Instance.LoadTotal;
                        }
                        phaseID = gamePhase + stagePhase;
                        subInfo = string.Format("({0}/{1})", ResourceSystem.Instance.LoadNumber, ResourceSystem.Instance.LoadTotal);
                        break;
                    case StageSystem.States.LoadLua:
                    case StageSystem.States.InitStage:
                        phaseID = gamePhase + stagePhase;
                        break;
                    default:
                        break;
                }
            break;
        default:
            break;

        }
        mainInfo = TextSystem.GetText("loading_text_" + phaseID.ToString());
        SetPhaseProgress(phaseID, phasePercent);
        SetPhaseText(mainInfo + " | " + subInfo);
    }

    public void Init()
    {
    }

    public void SetPhaseProgress(int phase, float percent) {
        float progressValue = 0.0f;
        RangeInt rangeValue;
        if (percentRangeMap.TryGetValue(phase, out rangeValue)) {
            progressValue = ((float)rangeValue.start + (float)rangeValue.length * percent) * 0.01f;
            UILoading loading = UISystem.Instance.GetLoadingUI();
            if (loading != null) {
                loading.SetPercent(progressValue);
            }
        }
    }

    public void SetPhaseText(string text) {
        UILoading loading = UISystem.Instance.GetLoadingUI();
        if (loading != null)
        {
            loading.SetInfo(text);
        }
    }
}

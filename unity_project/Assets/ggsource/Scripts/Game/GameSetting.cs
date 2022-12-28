#if UNITY_EDITOR
using UnityEditor;

public class GameSetting
{
    private const string kTableHotfixInEditor = "GO::TableHotfixInEditor";
    private const string kAssetHotfixInEditor = "GO::AssetHotfixInEditor";

    public static bool IsTableHotfixOn
    {
        get { return EditorPrefs.GetBool(kTableHotfixInEditor, true); }
        set { EditorPrefs.SetBool(kTableHotfixInEditor, !EditorPrefs.GetBool(kTableHotfixInEditor)); }
    }

    public static bool IsAssetHotfixOn
    {
        get { return EditorPrefs.GetBool(kAssetHotfixInEditor, false); }
        set { EditorPrefs.SetBool(kAssetHotfixInEditor, !EditorPrefs.GetBool(kAssetHotfixInEditor)); }
    }
}
#endif

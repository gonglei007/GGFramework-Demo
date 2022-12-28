using UnityEditor;

public class GameSettingMenu
{
    private const string kTableHotfixMode = "Game Setting/Table Hotfix On";
    private const string kAssetHotfixMode = "Game Setting/Asset Hotfix On";

    [MenuItem(kTableHotfixMode)]
    public static void ToggleTableHotfixMode()
    {
        GameSetting.IsTableHotfixOn = !GameSetting.IsTableHotfixOn;
    }

    [MenuItem(kTableHotfixMode, true)]
    public static bool ToggleTableHotfixModeValidate()
    {
        Menu.SetChecked(kTableHotfixMode, GameSetting.IsTableHotfixOn);
        return true;
    }

    [MenuItem(kAssetHotfixMode)]
    public static void ToggleAssetHotfixMode()
    {
        GameSetting.IsAssetHotfixOn = !GameSetting.IsAssetHotfixOn;
    }

    [MenuItem(kAssetHotfixMode, true)]
    public static bool ToggleAssetHotfixModeValidate()
    {
        Menu.SetChecked(kAssetHotfixMode, GameSetting.IsAssetHotfixOn);
        return true;
    }
}

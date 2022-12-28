using UnityEngine;
using UnityEditor;
using System.IO;

[CanEditMultipleObjects, CustomEditor(typeof(DefaultAsset))]
public class LuaInspector : Editor
{
    private GUIStyle m_TextStyle;

    public override void OnInspectorGUI()
    {
        if (this.m_TextStyle == null)
        {
            this.m_TextStyle = "ScriptText";
        }
        bool enabled = GUI.enabled;
        GUI.enabled = true;
        string assetPath = AssetDatabase.GetAssetPath(target);
        if (assetPath.EndsWith(".lua"))
        {
            string luaFile = File.ReadAllText(assetPath);
            string text;
            if (base.targets.Length > 1)
            {
                text = Path.GetFileName(assetPath);
            }
            else
            {
                string requireString = assetPath;
                int start = requireString.IndexOf("Assets/LuaGame/Lua/") + "Assets/LuaGame/Lua/".Length;
                int end = requireString.LastIndexOf(".lua");
                requireString = requireString.Substring(start, end - start);
                requireString = string.Format("require \"{0}\"", requireString);
                var style = new GUIStyle();
                style.fontSize = 20;
                Rect rect2 = GUILayoutUtility.GetRect(new GUIContent(requireString), style);
                rect2.x = 0f;
                EditorGUI.TextArea(rect2, requireString);
                text = luaFile;
                if (text.Length > 7000)
                {
                    text = text.Substring(0, 7000) + "...\n\n<...etc...>";
                }
            }
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), this.m_TextStyle);
            rect.x = 0f;
            rect.y -= 3f;
            rect.width = EditorGUIUtility.currentViewWidth + 1f;
            GUI.Box(rect, text, this.m_TextStyle);
        }
        GUI.enabled = enabled;
    }
}
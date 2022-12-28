using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public class ExtendTools : MonoBehaviour {
    /// <summary>
    /// clear unity console window log
    /// </summary>
    [MenuItem("GOEditorTool/Clear Console %#w")]
    static void ClearConsole()
    {
        var logEntries = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }

    static Component[] componentsList;
    [MenuItem("GameObject/Copy Components/Copy %&c", priority = 0)]
    static void CopyComponents()
    {
        var selected = Selection.activeGameObject;
        componentsList = selected.GetComponents<Component>();
        EditorUtility.DisplayDialog("拷贝组件", "拷贝组件成功", "OK");
    }

    /// <summary>
    /// 存在组件则直接粘贴  否则创建后再粘贴
    /// </summary>
    [MenuItem("GameObject/Copy Components/Paste  %&v", priority = 1)]
    static void PasteComponents()
    {
        var selected = Selection.activeGameObject;
        Component[] mComps = selected.GetComponents<Component>();

        for (int i = 0; i < componentsList.Length; i++)
        {
            Component comp = componentsList[i];
            Component tmpComp = CheckContainComp(mComps, comp) ? selected.GetComponent(comp.GetType()) : selected.AddComponent(comp.GetType());

            UnityEditorInternal.ComponentUtility.CopyComponent(comp);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(tmpComp);
        }
        EditorUtility.DisplayDialog("粘贴组件", "粘贴组件成功", "OK");
    }

    /// <summary>
    /// 判断是否目标物体是否存在该组件
    /// </summary>
    /// <param name="list"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    static bool CheckContainComp(Component[] list, Component c)
    {
        foreach (var item in list)
        {
            if(item.GetType().Name == c.GetType().Name)
            {
                return true;
            }
        }
        return false;
    }

    [MenuItem("GameObject/Copy Components/Copy Children", priority = 0)]
    static void CopyChildrenComponents()
    {
        var selected = Selection.activeGameObject;

        //var logEntries = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
        //var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        //clearMethod.Invoke(null, null);
    }

    [MenuItem("GameObject/Debug Children Count", priority = 0)]
    static void DebugChildrenCount()
    {
        Debug.Log("Children Count: " + Selection.activeGameObject.transform.childCount);
    }
}

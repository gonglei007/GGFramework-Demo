using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneSaveTool : SaveAssetsProcessor
{

    const string SCENE_HOME = "Assets/Editor/Prefab/SceneUI";
    static string[] EXCEPTED_SCENES = new string[] { "LoadingUI"};

    [MenuItem("GOEditorTool/Save Current Scene Prefab %e")]
    static void SaveCurrentScenePrefab()
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name.Contains("MainScene"))
        {
            var result = EditorUtility.DisplayDialog("警告：正在主场景中进行操作", 
@"当前场景为主场景（MainScene），若要继续操作，将会保存所有位于LayerN下的一级对象到各自对应的LayerN目录
未成年人请在家长的指导下进行此操作", 
                  "我要继续", "好害怕，不做了");

            if (!result)
            {
                return;
            }
        }


        var root = GameObject.Find("UIRoot/Main Camera");
        if (root == null)
        {
            bool result = EditorUtility.DisplayDialog("警告", "没有找到UIRoot/Main Camera，将搜索附有UIRoot的Component作为根节点", "继续", "中断");
            if (!result)
            {
                return;
            }
            root = Component.FindObjectOfType<UIRoot>().gameObject;
        }

        for (int i = 0; i < root.transform.childCount; i++)
        {
            var layerObj = root.transform.GetChild(i).gameObject;

            Debug.Log("Find layer: " + layerObj.name);

            for (int j = 0; j < layerObj.transform.childCount; j++)
            {
                var sceneObj = layerObj.transform.GetChild(j).gameObject;

                Debug.Log("Save scene object as prefab: " + sceneObj.name);

                if (!IsExceptedScene(sceneObj.name))
                {
                    SaveScenePrefab(layerObj.name, sceneObj);
                }
            }
        }

    }

    static bool IsExceptedScene(string sceneName)
    {
        for (int i = 0; i < EXCEPTED_SCENES.Length; i++)
        {
            if (EXCEPTED_SCENES[i] == sceneName || (EXCEPTED_SCENES[i] + "Scene" == sceneName))
            {
                return true;
            }
        }
        return false;
    }

    static string[] OnWillSaveAssets(string[] paths)
    {
        Debug.Log("OnWillSaveAssets");

        var currentScene = EditorSceneManager.GetActiveScene();

        foreach (string path in paths)
        {

            if (path.Contains(currentScene.name + ".unity"))
            {
                if (!IsExceptedScene(currentScene.name))
                {

                    //var result = EditorUtility.DisplayDialog("保存确认", "是否需要保存或更新场景物体为Prefab", "保存", "取消");
                    //if (result)
                    //{                
                    //    //todo
                    //    Debug.LogWarning("SaveCurrentScenePrefab will not work");

                    //    SaveCurrentScenePrefab();
                    //}
                }
            }
        }
        return paths;
    }

    static void SaveScenePrefab(string layerName, GameObject uiObject)
    {
        if (string.IsNullOrEmpty(layerName) || uiObject == null)
        {
            return;
        }

        var layerDirPath = string.Format("{0}/{1}",SCENE_HOME, layerName);

        if (!AssetDatabase.IsValidFolder(layerDirPath))
        {
            AssetDatabase.CreateFolder(SCENE_HOME, layerName);
        }

        var sceneDirPath = string.Format("{0}/{1}", layerDirPath, uiObject.name);

        if (!AssetDatabase.IsValidFolder(sceneDirPath))
        {
            AssetDatabase.CreateFolder(layerDirPath, uiObject.name);

            var importer = AssetImporter.GetAtPath(sceneDirPath);

            importer.assetBundleName = uiObject.name.ToLower();
            importer.SaveAndReimport();
        }

        var prefabPath = string.Format("{0}/{1}.prefab", sceneDirPath, uiObject.name);

        GameObject resultPrefab = PrefabUtility.CreatePrefab(prefabPath, uiObject, ReplacePrefabOptions.Default);

        if (resultPrefab != null)
        {
            //对生成的prefab作些后期处理
            //这里主要是要去掉MakeLuaPanel脚本，防止可能出现的一堆脚本丢失警告
            //想了想，在这里去掉脚本没啥问题，因为东西都存在场景里，但是对一些单独的prefab来说，现在并不方便在制作后立刻去除MakeLuaPanel脚本，
            //因为这样会不方便后续的对prefab绑定关系的更新，所以为了统一，暂且这里也注释掉了
            //var makeLuaPanelComps = resultPrefab.GetComponentsInChildren<MakeLuaPanel>(true);
            //if (makeLuaPanelComps != null)
            //{
            //    for (int i = 0; i < makeLuaPanelComps.Length; i++)
            //    {
            //        Component.DestroyImmediate(makeLuaPanelComps[i], true);
            //    }
            //}
            //EditorUtility.SetDirty(resultPrefab);
        }
    }
}

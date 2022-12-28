using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public enum BundleNameModifyType
{
    OnlyDictionary,
    OnlyObject,
}

public enum BundleNameType
{
    AddBundleName,
    ClearBundleName,
}

public class NameAssetBundle : EditorWindow
{
    public string sourcePath;
    const string AssetBundlesOutputPath = "Assets/StreamingAssets";

    private BundleNameModifyType type = BundleNameModifyType.OnlyDictionary;
    private BundleNameType nameType = BundleNameType.AddBundleName;
    private Object target;
    private AssetImporter assetImporter;

    private Vector2 scrollPosition = Vector2.zero;
    private List<string> resultList = new List<string>();
    private string resultStr;
    private Object selectedObj;

    private bool addBundleName;
    private bool includeChild;
    private bool useSelfNameAsBundleName = true;

    private string bundleName;

    private string prefix;
    private string suffix;


    [MenuItem("GOEditorTool/AssetBundle/NameBundle %Q")]
    public static void NameBundle()
    {
        NameAssetBundle myWindow = (NameAssetBundle)EditorWindow.GetWindow(typeof(NameAssetBundle), false, "NameBundle", true);
        myWindow.Show();
    }

    public void OnGUI()
    {
        GUILayout.Width(100);

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        target = EditorGUILayout.ObjectField("target: ", target, typeof(Object), true) as Object;
        EditorGUILayout.LabelField(string.Format("path: {0}", AssetDatabase.GetAssetPath(target)));
        sourcePath = AssetDatabase.GetAssetPath(selectedObj);
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        GUILayout.Space(10);
        nameType = (BundleNameType)EditorGUILayout.EnumPopup("Type", nameType);
        if (nameType == BundleNameType.AddBundleName)
        {
            prefix = EditorGUILayout.TextField("Prefix", prefix);
            suffix = EditorGUILayout.TextField("Suffix", suffix);
        }
        includeChild = EditorGUILayout.BeginToggleGroup("IncludeChild", includeChild);

        GUILayout.BeginHorizontal();
        type = (BundleNameModifyType)EditorGUILayout.EnumPopup("ModifyType", type);
        EditorGUILayout.EndToggleGroup();
        GUILayout.EndHorizontal();

        if (nameType == BundleNameType.AddBundleName)
        {
            useSelfNameAsBundleName = EditorGUILayout.ToggleLeft("UseObjName", useSelfNameAsBundleName);
            if (!useSelfNameAsBundleName && !includeChild)
            {
                bundleName = EditorGUILayout.TextField("BundleName", bundleName);
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Commit"))
            {
                resultStr = "";
                if (includeChild)
                {
                    string tmpPath = AssetDatabase.GetAssetPath(target);

                    if (Directory.Exists(tmpPath))
                    {
                        DirectoryInfo direction = new DirectoryInfo(tmpPath);

                        if (type == BundleNameModifyType.OnlyDictionary)
                        {
                            DirectoryInfo[] tmpDirections = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
                            if (tmpDirections.Length > 0)
                            {
                                for (int i = 0; i < tmpDirections.Length; i++)
                                {
                                    string tmpName = tmpDirections[i].FullName.Substring(tmpDirections[i].FullName.IndexOf("Asset"));
                                    assetImporter = AssetImporter.GetAtPath(tmpName);
                                    if (useSelfNameAsBundleName)
                                    {
                                        assetImporter.assetBundleName = string.Format("{0}{1}{2}", prefix, tmpDirections[i].Name.ToLower(), suffix);
                                        bundleName = string.Format("{0}{1}{2}", prefix, tmpDirections[i].Name.ToLower(), suffix);
                                    }
                                    else
                                    {
                                        assetImporter.assetBundleName = string.Format("{0}{1}{2}", prefix, bundleName.ToLower(), suffix);
                                    }
                                    EditorUtility.SetDirty(assetImporter);

                                    Debug.LogWarning(string.Format("set <{0}>'s bundleMame to <{1}> secceed! ", tmpDirections[i].Name, bundleName));
                                }
                                EditorUtility.DisplayDialog("Modify Secceed", "succeed!!  count: " + tmpDirections.Length, "confrim");
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Searching Error", "Can not find sub directionary: ", "confrim");
                            }
                        }
                        else
                        {
                            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                for (int i = 0; i < files.Length; i++)
                                {
                                    if (!files[i].Name.EndsWith(".meta"))
                                    {
                                        string tmpPathStr = files[i].FullName.Substring(files[i].FullName.IndexOf("Asset"));
                                        //TODO cww：去掉bundleName的后缀名，临时方法，后续优化
                                        //string tmpNameStr2 = files[i].Name.TrimEnd(".png".ToCharArray());
                                        Debug.Log(tmpPathStr);
                                        string[] tempStrList = tmpPathStr.Split(new char[] { '\\' });
                                        string tmpNameStr2 = tempStrList[tempStrList.Length - 1].Split(new char[] { '.' })[0];
                                        //files[i].Name.Substring(files[i].Name.IndexOf("."));
                                        //tmpNameStr2 = files[i].Name.TrimEnd(".wav".ToCharArray());

                                        assetImporter = AssetImporter.GetAtPath(tmpPathStr);
                                        if (useSelfNameAsBundleName)
                                        {
                                            assetImporter.assetBundleName = string.Format("{0}{1}{2}", prefix, tmpNameStr2.ToLower(), suffix);
                                            bundleName = string.Format("{0}{1}{2}", prefix, tmpNameStr2.ToLower(), suffix);
                                        }
                                        else
                                        {
                                            assetImporter.assetBundleName = string.Format("{0}{1}{2}", prefix, bundleName.ToLower(), suffix);
                                        }
                                        EditorUtility.SetDirty(assetImporter);

                                        Debug.LogWarning(string.Format("set <{0}>'s bundleMame to <{1}> secceed! ", files[i].Name, tmpNameStr2));
                                    }
                                }
                                EditorUtility.DisplayDialog("Modify Secceed", "succeed!!", "confrim");
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Searching Error", "Can not find target file!", "confrim");
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Setting Error", "Can not find directionary: " + tmpPath, "confrim");
                    }
                }
                else
                {
                    string tmpPath = AssetDatabase.GetAssetPath(target);
                    assetImporter = AssetImporter.GetAtPath(tmpPath);

                    if (useSelfNameAsBundleName)
                    {
                        assetImporter.assetBundleName = string.Format("{0}{1}{2}", prefix, target.name.ToLower(), suffix);
                        bundleName = string.Format("{0}{1}{2}", prefix, target.name.ToLower(), suffix);
                    }
                    else
                    {
                        assetImporter.assetBundleName = string.Format("{0}{1}{2}", prefix, bundleName.ToLower(), suffix);
                    }
                    EditorUtility.SetDirty(assetImporter);

                    EditorUtility.DisplayDialog("Modify Succeed", "Modify target: " + target.name + " to:  " + bundleName.ToLower() + " secceed!", "confrim");
                    Debug.LogWarning(string.Format("set <{0}>'s bundleMame to <{1}> secceed! ", tmpPath, target.name));
                }
            }
        }
        else
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Clear"))
            {
                resultStr = "";
                if (includeChild)
                {
                    string tmpPathStr = AssetDatabase.GetAssetPath(target);

                    if (Directory.Exists(tmpPathStr))
                    {
                        DirectoryInfo direction = new DirectoryInfo(tmpPathStr);

                        if (type == BundleNameModifyType.OnlyDictionary)
                        {
                            DirectoryInfo[] tmpDirections = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
                            for (int i = 0; i < tmpDirections.Length; i++)
                            {
                                string tmpName = tmpDirections[i].FullName.Substring(tmpDirections[i].FullName.IndexOf("Asset"));
                                assetImporter = AssetImporter.GetAtPath(tmpName);

                                assetImporter.assetBundleName = "";
                                EditorUtility.SetDirty(assetImporter);

                                Debug.LogWarning(string.Format("set <{0}>'s bundleMame to <{1}> secceed! ", tmpDirections[i].Name, "empty"));
                            }
                        }
                        else
                        {
                            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);

                            for (int i = 0; i < files.Length; i++)
                            {
                                if (!files[i].Name.EndsWith(".meta"))
                                {
                                    string tmpPathStr2 = files[i].FullName.Substring(files[i].FullName.IndexOf("Asset"));

                                    assetImporter = AssetImporter.GetAtPath(tmpPathStr2);

                                    assetImporter.assetBundleName = "";
                                    EditorUtility.SetDirty(assetImporter);

                                    Debug.LogWarning(string.Format("set <{0}>'s bundleMame to <{1}> secceed! ", files[i].Name, "empty"));
                                }
                            }
                        }

                        EditorUtility.DisplayDialog("Modify Succeed", "Modify Succeed", "Confirm");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Searching Error", "No Such Dictionary!!", "Confirm");
                    }
                }
                else
                {
                    string tmpPathStr = AssetDatabase.GetAssetPath(target);
                    assetImporter = AssetImporter.GetAtPath(tmpPathStr);

                    assetImporter.assetBundleName = "";
                    EditorUtility.SetDirty(assetImporter);

                    EditorUtility.DisplayDialog("Modify Succeed", "Modify Succeed", "Confirm");
                    Debug.LogWarning(string.Format("set <{0}>'s bundleMame to <{1}> secceed! ", tmpPathStr, "empty"));
                }

                AssetDatabase.RemoveUnusedAssetBundleNames();
            }
        }

        GUILayout.Space(20);
        if (GUILayout.Button("Clear UnusedBundleName"))
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            EditorUtility.DisplayDialog("Succeed", "Succeed", "Confirm");
        }
        GUILayout.EndVertical();
    }
}

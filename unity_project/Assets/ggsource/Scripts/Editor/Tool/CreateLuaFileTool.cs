using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class CreateLuaFileTool : EditorWindow {

    public enum LuaFileType
    {
        mainPageFile,
        subPageFile,
    }

    LuaFileType luaFileType = LuaFileType.mainPageFile;
    string className = "MainPageTemplate";
    string outputPath = "";

    [MenuItem("Assets/Create/CreateLuaFile", priority = 10)]
    public static void Create()
    {
        CreateLuaFileTool fileToolWindow = EditorWindow.GetWindow<CreateLuaFileTool>();
        fileToolWindow.Show();
    }

    private void OnGUI()
    {
        Space(2);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Crate Lua File");
        Space(2);
        className = EditorGUILayout.TextField("className: ", className);
        string tmpOutputPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        tmpOutputPath = RemovePathExtension(tmpOutputPath);

        Space(2);

        outputPath = EditorGUILayout.TextField("OutPutPath:", tmpOutputPath + className + ".lua");
        GUIContent content = new GUIContent();
        Space(2);
        luaFileType = (LuaFileType)EditorGUILayout.EnumPopup("FileType", luaFileType);

        Space(2);

        if (GUILayout.Button("Create", GUILayout.Width(100)))
        {
            bool contain = CheckContainLuaFile();
            if (!contain)
            {
                CreateLuaPage();
            }
            else
            {
                if(EditorUtility.DisplayDialog("创建lua文件", "目标目录下存在同名文件,是否覆盖!!!!!!","覆盖", "不覆盖"))
                {
                    CreateLuaPage();
                }
            }
        }


        EditorGUILayout.EndVertical();
    }

    string RemovePathExtension(string path)
    {
        string finalPath = path;
        if(path != null && path != "")
        {
            if(path.Contains("."))
            {
                finalPath = path.Replace(Path.GetFileName(path), "");
            }
            else
            {
                finalPath += "/";
            }
        }
        return finalPath;
    }

    void CreateLuaPage()
    {
        Debug.LogWarning("Application.dataPath " + Application.dataPath);
        string templatePath = string.Format(Application.dataPath + "/LuaGame/Lua/View/"
            + (luaFileType == LuaFileType.mainPageFile ? "MainPageTemplate.lua" : "SubPageTemplate.lua"));
        string fileText;
        using (StreamReader reader = new StreamReader(templatePath))
        {
            fileText = reader.ReadToEnd();
        }

        if (fileText == null)
        {
            return;
        }

        string templateClassName = "MainPageTemplate";
        switch (luaFileType)
        {
            case LuaFileType.mainPageFile:
                templateClassName = "MainPageTemplate";
                break;
            case LuaFileType.subPageFile:
                templateClassName = "SubPageTemplate";
                break;
        }
        fileText = fileText.Replace(templateClassName, className);
        string tmpPath = Application.dataPath;

        string tmpOutputPath = string.Format(Application.dataPath + outputPath.Replace("Assets", ""));
        using (StreamWriter writer = new StreamWriter(tmpOutputPath, false, Encoding.UTF8))
        {
            writer.Write(fileText);
        }
        AssetDatabase.Refresh();
    }

    bool CheckContainLuaFile()
    {
        return (File.Exists(string.Format(Application.dataPath + outputPath.Replace("Assets", ""))));
    }

    void Space(int count)
    {
        for (int i = 0; i < count; i++)
        {
            EditorGUILayout.Space();
        }
    }
}

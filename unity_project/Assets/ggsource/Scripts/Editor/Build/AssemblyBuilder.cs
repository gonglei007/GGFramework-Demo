using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssemblyBuilder
{
    public static string ParseArg(string argName) {
        string argValue = "";
        var args = System.Environment.GetCommandLineArgs();
        for(int i=0; i<args.Length; ++i)
        {
            string arg = args[i];
            Debug.Log(arg.ToString());
            if ((arg == argName || arg.Substring(1) == argName) && (i+1 <args.Length))
            {
                argValue = args[i + 1];
            }
        }
        return argValue;
    }

    /// <summary>
    /// 构建Assembly（DLL），导出到发布目录。
    /// </summary>
    [MenuItem("Assembly/Build Android", false, 80)]
    public static void BuildAssmbly() {
        string target = Application.dataPath + "/../../../output/";
        string sourcePath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies/");
        string sourceFile = "GGFramework.dll";
        Debug.Log("Start building assembly...From:" + Path.Combine(sourcePath, sourceFile) + " To:" + Path.Combine(target, sourceFile));
        File.Copy(Path.Combine(sourcePath, sourceFile), Path.Combine(target, sourceFile), true);
    }

    public static void BuildAssembly() {
        string target = ParseArg("targetPath");
        string sourcePath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies/");
        string sourceFile = "GGFramework.dll";
        Debug.Log("Start building assembly...From:"+ Path.Combine(sourcePath, sourceFile) + " To:"+ Path.Combine(target, sourceFile));
        File.Copy(Path.Combine(sourcePath, sourceFile), Path.Combine(target, sourceFile), true);
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace GOEditorLib
{
    /// <summary>
    /// 用于生成AssetBundle的工具类
    /// </summary>
    public class AssetBundleEditorTool : MonoBehaviour
    {
        private static string ASSETBUNDLE_EXTENSTION = ".unity3d";

        /// <summary>
        /// 将一个文件单独打包
        /// </summary>
        /// <param name="filePath">以Assets开头的相对路径（文件在Unity工程中）</param>
        /// <param name="outputPath">输出AssetBundle路径</param>
        /// <param name="buildTarget">针对的平台，如Android或iOS</param>
        /// <param name="buildOption">生成AssetBundle的配置选项</param>
        /// <param name="pushPopDependency">是否压栈并弹栈依赖关系</param>
        public static void BuildSingleFileIntoBundle(string filePath, string outputPath, BuildTarget buildTarget,
            BuildAssetBundleOptions buildOption)
        {
            AssetBundleBuild abb = new AssetBundleBuild();
            abb.assetBundleName = "aaa";
            abb.assetNames = new string[2] { "Assets/Editor/Prefab/SceneUI/Layer1/BossUI/BossUI.prefab", "Assets/Editor/Prefab/SceneUI/Layer1/BossUI/HelpObj.prefab" };

            string unityAssetPath = filePath;
            Object obj = AssetDatabase.LoadAssetAtPath(unityAssetPath, typeof(Object));
            string targetFileName = outputPath ;



            BuildPipeline.BuildAssetBundles(targetFileName, new AssetBundleBuild[1] { abb }, buildOption,buildTarget);
          //  AssetDatabase.Refresh();
        }

        /// <summary>
        /// 将resPath下的所有文件打到一个AssetBundle里(将忽略后缀为.DS_Store或.meta的文件，以及文件名为dummy的文件）
        /// </summary>
        /// <param name="dirInfo">要打包的资源路径的DirectoryInfo对象</param>
        /// <param name="outputPath">输出AssetBundle路径</param>
        /// <param name="buildTarget">针对的平台，如Android或iOS</param>
        /// <param name="buildOption">生成AssetBundle的配置选项</param>
        /// <param name="pushPopDependency">是否压栈并弹栈依赖关系</param>
        public static void BuildAllFileIntoOneBundle(DirectoryInfo dirInfo, string outputPath, BuildTarget buildTarget,
            BuildAssetBundleOptions buildOption)
        {
            List<FileInfo> fileInfoList = new List<FileInfo>(256);
            string targetFileName = outputPath + dirInfo.Name + ASSETBUNDLE_EXTENSTION;

            GetAllSubFileInfos(dirInfo, fileInfoList, true);

            List<Object> objList = new List<Object>(fileInfoList.Count);

            foreach (FileInfo fileInfo in fileInfoList)
            {
                string fullPath = fileInfo.FullName;
                string unityAssetPath = fullPath.Substring(fullPath.LastIndexOf("Assets"));
                Object obj = AssetDatabase.LoadAssetAtPath(unityAssetPath, typeof (Object));
                if (obj != null)
                {
                    objList.Add(obj);
                }
            }

			Debug.Log("fileInfoList.Count: " + fileInfoList.Count);
			Debug.Log("dirInfo: " + dirInfo);
            BuildPipeline.BuildAssetBundle(objList[0], objList.ToArray(), targetFileName, buildOption, buildTarget);
        }

        /// <summary>
        /// 将resPath下的所有文件打到一个AssetBundle里(将忽略后缀为.DS_Store或.meta的文件，以及文件名为dummy的文件）
        /// </summary>
        /// <param name="resPath">要打包的资源路径(是文件夹的完整路径)</param>
        /// <param name="outputPath">输出AssetBundle路径</param>
        /// <param name="buildTarget">针对的平台，如Android或iOS</param>
        /// <param name="buildOption">生成AssetBundle的配置选项</param>
        /// <param name="pushPopDependency">是否压栈并弹栈依赖关系</param>
        public static void BuildAllFileIntoOneBundle(string resPath, string outputPath, BuildTarget buildTarget,
            BuildAssetBundleOptions buildOption, bool pushPopDependency = false)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(resPath);
            BuildAllFileIntoOneBundle(dirInfo, outputPath, buildTarget, buildOption);
        }

        /// <summary>
        /// 给fileName文件打包到AssetBundle里
        /// </summary>
        /// <param name="fileName">要打包的文件名</param>
        /// <param name="relativePath">要打包的文件相对于资源目录的子路径</param>
        /// <param name="outputPath">输出AssetBundle路径</param>
        /// <param name="buildTarget">针对的平台，如Android或iOS</param>
        /// <param name="buildOption">生成AssetBundle的配置选项</param>
        /// <param name="pushPopDependency">是否压栈并弹栈依赖关系</param>
        public static void BuildFileIntoBundle(string fileName, string relativePath, string outputPath, BuildTarget buildTarget, BuildAssetBundleOptions buildOption) 
        {
            string unityAssetPath = fileName.Substring(fileName.LastIndexOf("Assets"));
            string targetFileName = Path.ChangeExtension(outputPath + relativePath, ASSETBUNDLE_EXTENSTION);
            string targetFilePath = Path.GetDirectoryName(targetFileName);

            if (Directory.Exists(targetFilePath) == false)
            {
                Directory.CreateDirectory(targetFilePath);
            }

            /* Debug Info
            Debug.Log(string.Format("File Extension : {0}\n" +
                          "resPath : {1}\n" +
                          "fullPath : {2}\n" +
                          "unityAssetPath : {3}\n" +
                          "relativePath : {4}\n" +
                          "targetFileName : {5}\n" +
                          "targetFilePath : {6}\n",
                            fileInfo.Extension,
                            resPath,
                            fullPath,
                            unityAssetPath,
                            relativePath,
                            targetFileName,
                            targetFilePath));
             */

            Object obj = AssetDatabase.LoadAssetAtPath(unityAssetPath, typeof(Object));

            BuildPipeline.BuildAssetBundle(obj, new Object[] { obj }, targetFileName, buildOption, buildTarget);
        }


        /// <summary>
        /// 将resPath下的所有文件打到独立的AssetBundle里(将忽略后缀为.DS_Store或.meta的文件，以及文件名为dummy的文件）
        /// </summary>
        /// <param name="resPath">要打包的资源路径(是文件夹的完整路径)</param>
        /// <param name="outputPath">输出AssetBundle路径</param>
        /// <param name="buildTarget">针对的平台，如Android或iOS</param>
        /// <param name="buildOption">生成AssetBundle的配置选项</param>
        /// <param name="pushPopDependency">是否压栈并弹栈依赖关系</param>
        public static void BuildAllFileIntoSeperatedBundle(string resPath, string outputPath, BuildTarget buildTarget, BuildAssetBundleOptions buildOption, bool pushPopDependency = false)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(resPath);
            List<FileInfo> fileInfoList = new List<FileInfo>(256);

            GetAllSubFileInfos(dirInfo, fileInfoList, true);

            List<Object> objList = new List<Object>(fileInfoList.Count);

            foreach (FileInfo fileInfo in fileInfoList)
            {
                string fullPath = fileInfo.FullName;
                string relativePath = fullPath.Substring(resPath.Length);
                AssetBundleEditorTool.BuildFileIntoBundle(fullPath, relativePath, outputPath, buildTarget, buildOption);
            }
        }

        /// <summary>
        /// 将resPath路径下的第一级子文件与子文件夹打到独立的AssetBundle里(将忽略后缀为.DS_Store或.meta的文件，以及文件名为dummy的文件）
        /// </summary>
        /// <param name="resPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="buildTarget"></param>
        /// <param name="buildOption"></param>
        /// <param name="pushPopDependency"></param>
        public static void BuildAllFirstLevelSubFolderAndFileIntoSeperateBundle(string resPath, string outputPath,
            BuildTarget buildTarget, BuildAssetBundleOptions buildOption)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(resPath);
            List<FileInfo> fileInfoList = new List<FileInfo>(256);
            List<DirectoryInfo> dirInfoList = new List<DirectoryInfo>(256);

            GetAllSubFileInfos(dirInfo, fileInfoList, false);
            GetAllSubDirectoryInfos(dirInfo, dirInfoList, false);

            foreach (FileInfo fileInfo in fileInfoList)
            {
                BuildSingleFileIntoBundle(fileInfo.FullName, outputPath, buildTarget, buildOption);
            }

            foreach (DirectoryInfo subDirInfo in dirInfoList)
            {
                BuildAllFileIntoOneBundle(subDirInfo, outputPath, buildTarget, buildOption);
            }
        }

        private static void GetAllSubFileInfos(DirectoryInfo dirInfo, List<FileInfo> fileInfoList, bool recursive)
        {
            FileInfo[] fileInfos = dirInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                if (fileInfo.Extension.Equals(".DS_Store") || fileInfo.Extension.Equals(".meta") ||
                    fileInfo.Name.Equals("dummy"))
                {
                    continue;
                }
                fileInfoList.Add(fileInfo);
            }
            if (recursive)
            {
                DirectoryInfo[] subDirInfos = dirInfo.GetDirectories();
                foreach (DirectoryInfo subDirInfo in subDirInfos)
                {
                    GetAllSubFileInfos(subDirInfo, fileInfoList, recursive);
                }
            }
        }

        private static void GetAllSubDirectoryInfos(DirectoryInfo dirInfo, List<DirectoryInfo> dirInfoList, bool recursive)
        {
            DirectoryInfo[] subDirInfos = dirInfo.GetDirectories();
            dirInfoList.AddRange(subDirInfos);
            if (recursive)
            {
                foreach (DirectoryInfo subDirInfo in subDirInfos)
                {
                    GetAllSubDirectoryInfos(subDirInfo, dirInfoList, recursive);
                }
            }
        }
    }
}
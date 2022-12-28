using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GO {
    public static class FileUtil
    {

        /// <summary>
        /// 熱更新建的信息。
        /// </summary>
        public struct FileMeta
        {
            public string path;
            public string hash;
            public int length;
        };


        /// <summary>
        /// 同步文件目录。同步的依据是plist(files.txt)文件
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="targetDir"></param>
        public static void SyncFiles(string plistName, string sourcePlistFile, string sourceDir, string targetPlistFile, string targetDir, Action<int, int> OnProcess)
        {
            List<string> addFiles = new List<string>();
            List<string> delFiles = new List<string>();
            Dictionary<string, FileMeta> sourceFileTable = LoadPlist(sourcePlistFile);
            Dictionary<string, FileMeta> targetFileTable = LoadPlist(targetPlistFile);
            // 找出需要删除的文件，即新的文件列表中已不存在的文件。
            foreach (var kvp in targetFileTable)
            {
                if (!sourceFileTable.ContainsKey(kvp.Key))
                {
                    delFiles.Add(kvp.Key);
                }
            }
            // 找出需要添加/更新的文件
            foreach (var kvp in sourceFileTable)
            {
                FileMeta sourceFileMeta = kvp.Value;
                if (!targetFileTable.ContainsKey(kvp.Key))
                {
                    addFiles.Add(kvp.Key);
                }
                else
                {
                    FileMeta targetFileMeta = targetFileTable[kvp.Key];
                    if (sourceFileMeta.hash != targetFileMeta.hash)
                    {
                        addFiles.Add(kvp.Key);
                    }
                }
            }
            SyncFiles(addFiles.ToArray(), delFiles.ToArray(), sourceDir, targetDir, OnProcess);
        }

        /**
         * 加载文件清单文件。
         */
        public static Dictionary<string, FileMeta> LoadPlist(string plistPath)
        {
            Dictionary<string, FileMeta> fileTable = new Dictionary<string, FileMeta>();
            if (File.Exists(plistPath)) {
                string[] files = File.ReadAllLines(plistPath);
                for (int i = 0; i < files.Length; ++i)
                {
                    string fileLine = files[i];
                    string[] keyValue = fileLine.Split('|');
                    FileMeta fileMeta = new FileMeta();
                    fileMeta.path = keyValue[0];
                    fileMeta.hash = keyValue[1];
                    if (keyValue.Length > 2)
                    {
                        fileMeta.length = Convert.ToInt32(keyValue[2]);
                    }
                    fileTable[keyValue[0]] = fileMeta;
                }
            }
            return fileTable;
        }

        /**
         * 加载文件清单文件。从StreamingAssets下。
         */
        public static IEnumerator LoadPlistFromStreamingAssets(string plistPath, Action<Dictionary<string, FileMeta>> OnFinish)
        {
            Debug.Log("加载StreamingAssets下的plist:" + plistPath);
            Dictionary<string, FileMeta> fileTable = new Dictionary<string, FileMeta>();
            string[] files = null;

            Uri plistUri = new Uri(plistPath);
            UnityWebRequest www = UnityWebRequest.Get(plistUri);
            yield return www.SendWebRequest();

            try
            {
                if (string.IsNullOrEmpty(www.error))
                {
                    byte[] data = www.downloadHandler.data;
                    string textData = Encoding.UTF8.GetString(data);
                    files = textData.Split('\n');
                    for (int i = 0; i < files.Length; ++i)
                    {
                        string fileLine = files[i];
                        string[] keyValue = fileLine.Split('|');
                        if (keyValue.Length > 2)
                        {
                            FileMeta fileMeta = new FileMeta();
                            fileMeta.path = keyValue[0];
                            fileMeta.hash = keyValue[1];
                            fileMeta.length = Convert.ToInt32(keyValue[2]);
                            fileTable[keyValue[0]] = fileMeta;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally {
                if (OnFinish != null)
                {
                    OnFinish(fileTable);
                }
            }
            yield return 0;
        }

        /// <summary>
        /// 同步文件。
        /// TODO: GL-挪到工具类中去。
        /// </summary>
        /// <param name="files"></param>
        /// <param name="target"></param>
        private static void SyncFiles(string[] addFiles, string[] delFiles, string sourceDir, string targetDir, Action<int, int> OnProcess)
        {
            int progress = 1;
            int totalCount = addFiles.Length + delFiles.Length;
            string sourcePath;
            string targetPath;
            for (int i = 0; i < delFiles.Length; ++i)
            {
                string filePath = delFiles[i];
                targetPath = targetDir + filePath;
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                    GameDebugger.Instance.PushLogFormat("[sync]-delete:" + filePath);
                    if (OnProcess != null)
                    {
                        OnProcess(progress, totalCount);
                    }
                }
            }
            for (int i = 0; i < addFiles.Length; ++i)
            {
                string filePath = addFiles[i].ToString();
                sourcePath = sourceDir + filePath;
                targetPath = targetDir + filePath;
                //GameDebugger.Instance.PushLogFormat("[sync]target:" + targetPath);
                string targetFullDir = Path.GetDirectoryName(targetPath);
                if (Directory.Exists(targetFullDir) == false)
                {
                    Debug.Log("[sync]-target dir:" + targetFullDir);
                    Directory.CreateDirectory(targetFullDir);
                }
                GameDebugger.Instance.PushLogFormat("[sync]-add/update:" + filePath);
                if (File.Exists(sourcePath))
                    File.Copy(sourcePath, targetPath, true);
                if (OnProcess != null)
                {
                    OnProcess(progress, totalCount);
                }
            }
        }
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, Action OnProcess)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
                if (OnProcess != null)
                {
                    OnProcess();
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, OnProcess);
                }
            }
        }

        /**
         * 从Streaming同步数据到Persistent。
         * @param fromFile: 从哪个文件。
         * @param toFile: 到哪个文件。
         * @param xorKey: 异或加解密Key。如果是null或者""，则不做异或加解密。
         */
        public static IEnumerator SyncFilesFromStreaming2Persistent(string fromFile, string toFile, string xorKey=null, Action<int, int> DoProcess=null, Action OnFinish=null)
        { 
			WWW www = null;
			byte[]	data = null;
			bool	isMobile = fromFile.Contains ("://") || fromFile.Contains (":///");
			if(isMobile){
				www = new WWW(fromFile);
				yield return www;
				Debug.Log("yield done");
				//File.WriteAllBytes(moveToFile, www.bytes);
				data = www.bytes;
			}
			else{
				data = File.ReadAllBytes(fromFile);
			}

			//yield return new WaitForEndOfFrame();
 
			Stream	stream = new MemoryStream(data);
			using(FileStream output = new FileStream( toFile, FileMode.Create, FileAccess.Write ))
			{
				byte[] buffer = new byte[ 32 * 1024 ]; //a32KB-sizedbufferisthemostefficient
				int bytesRead;
                long length = stream.Length;
                long offset = 0;
				
				while( (bytesRead = stream.Read( buffer, 0, buffer.Length ) ) > 0 ) {
                    Byte[] xorBuffer = EncodeUtil.XorEncryptionData(buffer, xorKey);
                    offset += bytesRead;
                    output.Write(xorBuffer, 0, bytesRead);
                    if (DoProcess != null) {
                        DoProcess((int)offset, (int)length);
                    }
					//yield return null;
				}
				output.Flush();
			}

			Debug.Log("file copy done");
			if(isMobile){
				www.Dispose ();
				www = null;
			}
            if(OnFinish != null){
                OnFinish();
            }
            yield return new WaitForEndOfFrame();
        }

        /**
         * 删除指定目录的所有内容。GL-未完、未测，先不要用。
         */
        public static void DeleteFolder(string folder) {
            foreach (var directory in Directory.GetDirectories(folder))
            {
                DirectoryInfo data_dir = new DirectoryInfo(directory);
                data_dir.Delete(true);
            }

            foreach (var file in Directory.GetFiles(folder))
            {
                FileInfo file_info = new FileInfo(file);
                file_info.Delete();
            }
        }

        public static void Recursive(string[] paths, out List<string> pathList, out List<string> fileList, string filter = ".meta") {
            pathList = new List<string>();
            fileList = new List<string>();
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    Recursive(path, pathList, fileList, filter);
                }
            }
            //subPaths = pathList.ToArray();
            //dirs = fileList.ToArray();
        }

        /// <summary>
        /// 遍历目录及其子目录
        /// </summary>
        public static void Recursive(string path, List<string> pathList, List<string> fileList, string filter)
        {
            string[] names = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);
            foreach (string filename in names)
            {
                string ext = Path.GetExtension(filename);
                if (ext.Equals(filter)) continue;
                fileList.Add(filename.Replace('\\', '/'));
            }
            foreach (string dir in dirs)
            {
                pathList.Add(dir.Replace('\\', '/'));
                Recursive(dir, pathList, fileList, filter);
            }
        }
    }   //class end
}   //namespace end

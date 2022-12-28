using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// TODO: 从PersistentPath下取Table，放到StreamingAssets目录下。
/// </summary>
public class TableBuilder : EditorWindow
{
    // 从persistentPath下的Table目录复制到streamingassetsPath下的Table目录。
    public static void BuildTables()
    {
        AssetDatabase.StartAssetEditing();
        // Build Table。
        TableBuilder.CopyTables();
        // 生成数据清单
        //string[] tableResources = { "Tables" };
        //AssetBuilder.BuildResourcePList(Global.StreamingAssetsPath, tableResources, Global.TablePlistName);
        // 将数据资源复制到svn仓库下，准备发布。
        //string[] tableList = { "Tables", Global.TablePlistName };
        //AssetBuilder.SyncToSVN(tableList, svnPath);
        AssetDatabase.StopAssetEditing();
    }

    public static void CopyTables()
    {
        //AssetDatabase.Refresh();
        //if (UnityEditor.EditorUtility.DisplayDialog("TODO",
        //    "为了让游戏包中也有一份表格数据，将来实现一下这个方法。\n只要打开表格热更，做包前可以暂不执行此操作。", "Yes"))
        //{
        //}
        //if (target != BuildTarget.iOS)
        {
            string assetsPath = Path.Combine(Global.PersistentDataPath, "Tables");
            string targetPath = Path.Combine(Application.dataPath, "StreamingAssets/Tables");
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            GO.FileUtil.DirectoryCopy(assetsPath, targetPath, true, () => { });

            string plistFile = Path.Combine(Global.PersistentDataPath, Global.TablePlistName);
            string plistTargetFile = Path.Combine(AssetBuilder.BuildOutputPath, Global.TablePlistName);
            if (File.Exists(plistFile)) {
                File.Copy(plistFile, plistTargetFile, true);
            }
            // StreamingAssets下也要放一个tables.txt
            string inPackageFilePath = Path.Combine(Application.streamingAssetsPath, Global.TablePlistName);
            File.Copy(plistFile, inPackageFilePath, true);
        }
        //AssetDatabase.Refresh();
    }
}

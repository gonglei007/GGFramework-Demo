#if UNITY_EDITOR
    #define LOAD_FROM_PROJECT
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonFx.Json;

public class TextSystem
{
    public class TextData
    {
        public string ID;

        public string text;
    }

    public static string textDataCache = null;
    private static Dictionary<string, TextData> dict = new Dictionary<string, TextData>();

    public static string GetText(string key)
	{                    
		if (key != null)
		{
			TextData textData = TextSystem.GetTextData(key);
			if(textData != null){
				key = textData.text;
                if (key == "/")
                {
                    key = "";
                }
			}
        }
		return key;
	}

	public static string GetText(string key, params string[] paramList)
	{
		if (paramList.Length == 1)
		{
			return string.Format(GetText(key), paramList[0]);
		}
		else if (paramList.Length == 2)
		{
			return string.Format(GetText(key), paramList[0], paramList[1]);
		}
		else if (paramList.Length == 3)
		{
			return string.Format(GetText(key), paramList[0], paramList[1], paramList[2]);
		}
		else if (paramList.Length == 4)
		{
			return string.Format(GetText(key), paramList[0], paramList[1], paramList[2], paramList[3]);
		}
		else if (paramList.Length == 5)
		{
			return string.Format(GetText(key), paramList[0], paramList[1], paramList[2], paramList[3], paramList[4]);
		}
		else if (paramList.Length == 6)
		{
			return string.Format(GetText(key), paramList[0], paramList[1], paramList[2], paramList[3], paramList[4], paramList[5]);
		}
		else if (paramList.Length == 7)
		{
			return string.Format(GetText(key), paramList[0], paramList[1], paramList[2], paramList[3], paramList[4], paramList[5], paramList[6]);
		}

		return GetText(key);
	}
	
	public static bool HasKey(string key)
	{
		if (key != null)
		{
			TextData textData = TextSystem.GetTextData(key);
			return textData != null;
		}
		return false;
	}

    public static void Clear() {
        if (dict != null) {
            dict.Clear();
        }
    }

    public static IEnumerator LoadTextDataFromStreaming(string dataPath, Action<string> callback = null)
    {
        yield return DataSystem.LoadTableFromStreaming(dataPath, (string textData)=> {
            textDataCache = textData;
        });
    }

    /**
     * 解析（json）文本。
     */
    private static void ParseTextData(string textData) {
        Debug.Assert(textData != null, "text data illegal!!!");
        TextSystem.Clear();
        JsonReader reader = new JsonReader(textData);
        TextData[] datas = reader.Deserialize<TextData[]>();
        dict = new Dictionary<string, TextData>(datas.Length);
        foreach (TextData data in datas)
        {
            if (dict.ContainsKey(data.ID))
            {
                string warningInfo = "The same key already exists in the TextDataDict: " + data.ID;
                GameDebugger.sPushLog(warningInfo);
                Debug.LogWarning(warningInfo);
            }
            dict.Add(data.ID, data);
        }
    }

    public static void Load(Action<bool> callback)
    {
        bool result = false;
        try
        {
            string fileName = DataSystem.GeneratePath("TextData");

            DataSystem.LoadTableData(fileName, (textData) =>
            {
                if (textData != null)
                {
                    TextSystem.ParseTextData(textData);
                    result = true;
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
        finally {
            callback(result);
        }
    }

    private static TextData GetTextData(string key)
    {
        if (dict == null)
        {
            string errorInfo = "Text table haven't been loaded!";
            GameDebugger.sPushLog(errorInfo);
            throw new Exception(errorInfo);
        }
        TextData data = null;
        if (dict != null)
        {
            dict.TryGetValue(key, out data);
        }
        return data;
    }
}

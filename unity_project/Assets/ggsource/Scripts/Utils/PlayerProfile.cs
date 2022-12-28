using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using GOLib.Tool;


public class PlayerProfileBase {


	public static void ClearProfile()
	{
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();
	}

	public static bool HasKey(string key)
	{
		string encrypedKey = SecurityUtil.EncryptString(key);
		return PlayerPrefs.HasKey(encrypedKey);
	}
	
	
	public static void SaveBoolValue(string key, bool finalValue = false)
	{
		SaveIntValue(key, finalValue ? 1 : 0);
	}
	
	public static void SaveIntValue(string key, int finalValue = 0)
	{
		SaveStringValue(key, finalValue.ToString());
	}

	public static void SaveLongValue(string key, long finalValue = 0)
	{
		SaveStringValue(key, finalValue.ToString());
	}

    public static void SaveVersionValue(string key, string value = "")
    {
        SaveStringValue(key, value);
    }

    public static string LoadVersionValue(string key)
    {
        return LoadStringValue(key);
    }
	
	public static void SaveStringValue(string key, string finalValue = "")
	{
		string encrypedKey = SecurityUtil.EncryptString(key);
		string encryptedValue = CombineEncryptKeyAndValue(key, finalValue);
		PlayerPrefs.SetString(encrypedKey, encryptedValue);
		PlayerPrefs.Save();
	}
	
	public static bool LoadBoolValue(string key, bool defaultValue = false)
	{
		string savedString = LoadStringValue(key);
		if (string.IsNullOrEmpty(savedString) == false)
		{
			int realValue = int.Parse(savedString);
			return (realValue == 1);
		}
		else
		{				
			SaveBoolValue(key, defaultValue);
			return defaultValue;
		}
	}
	
	public static int LoadIntValue(string key, int defaultValue = 1)
	{
		string savedString = LoadStringValue(key);
		if (string.IsNullOrEmpty(savedString) == false)
		{
			int realValue = int.Parse(savedString);
			return realValue;
		}
		else
		{				
			SaveIntValue(key, defaultValue);
			return defaultValue;
		}
	}

	public static long LoadLongValue(string key, long defaultValue = 1)
	{
		string savedString = LoadStringValue(key);
		if (string.IsNullOrEmpty(savedString) == false)
		{
			long realValue = long.Parse(savedString);
			return realValue;
		}
		else
		{				
			SaveLongValue(key, defaultValue);
			return defaultValue;
		}
	}
	
	public static string LoadStringValue(string key)
	{
		string encrypedKey = SecurityUtil.EncryptString(key);
		if (PlayerPrefs.HasKey(encrypedKey))
		{
			string savedString = PlayerPrefs.GetString(encrypedKey);
			string[] decryptStrings = SplitDecryptKeyAndValue(savedString);
			if (decryptStrings != null && decryptStrings.Length == 2)
			{
				if (decryptStrings[0].Equals(key))
				{
					return decryptStrings[1];
				}
				else
				{
					return String.Empty;
				}
			}
			else
			{
				return String.Empty;
			}
		}
		else
		{
			return String.Empty;
		}
	}
	
	public static string CombineEncryptKeyAndValue(string key, string val)
	{
		return SecurityUtil.EncryptString(key + "@" + val);
	}
	
	public static string[] SplitDecryptKeyAndValue(string encryptString)
	{
		if (string.IsNullOrEmpty(encryptString) == false)
		{
			string decryptedString = SecurityUtil.DecryptString(encryptString);
			return decryptedString.Split(new char[]{'@'});
		}
		else
		{
			return null;
		}
	}
}

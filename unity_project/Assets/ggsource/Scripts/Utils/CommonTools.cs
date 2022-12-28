using System;
using UnityEngine;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
//using AssetBundles;
using System.Collections.Generic;
//using GOLib;

public static class CommonTools
{
	/// <summary>
	/// 将时间点（毫秒）转化成“月日时分”字符串，是时间点
	/// </summary>
	/// <param name="ms"></param>
	/// <returns></returns>
	public static string TimeToFormat(long ms, bool showMonth = true, bool showDay = true, bool showMinute = true, bool showSecond = false, bool showTimeUnit = true)
	{
		string timeStr = "";
		DateTime dateTime1 = new DateTime(1970, 1, 1).ToLocalTime();
		DateTime dateTime = dateTime1.AddMilliseconds(ms);

        //if (BaseNetworkManager.GetChannelName().Contains("japan"))
        //{
        //    if (showSecond)
        //    {
        //        timeStr = dateTime.ToString("yyyy/MM/dd(ddd) HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("ja-JP"));
        //    }
        //    else
        //    {
        //        timeStr = dateTime.ToString("yyyy/MM/dd(ddd) HH:mm", System.Globalization.CultureInfo.CreateSpecificCulture("ja-JP"));
        //    }
        //}
        //else
        {
			if (showTimeUnit)
			{
				if (showMonth)
				{
					timeStr = string.Format("{0}{1}{2}", timeStr, dateTime.Month, TextSystem.GetText("month"));
				}
				if (showDay)
				{
					timeStr = string.Format("{0}{1}{2}", timeStr, dateTime.Day, TextSystem.GetText("day"));
				}
				timeStr = string.Format("{0} ", timeStr);
				timeStr = string.Format("{0}{1}{2}", timeStr, dateTime.Hour, TextSystem.GetText("hour"));
				if (showMinute)
				{
					timeStr = string.Format("{0}{1:00}{2}", timeStr, dateTime.Minute, TextSystem.GetText("minute"));
				}
				if (showSecond)
				{
					timeStr = string.Format("{0}{1}{2}", timeStr, dateTime.Second, TextSystem.GetText("second"));
				}
			}
			else
			{
				string tempStr = "";
				if (showMonth || showDay)
				{
					tempStr = "yyyy/MM/dd";
				}

				timeStr = string.Format("{0}{1}", timeStr, dateTime.ToString(string.Format("{0}  {1}", tempStr, "HH:mm:ss")));
			}
        }

		return timeStr;
	}

    /// <summary>
    /// 将时间段（毫秒）转化成“天、小时、分”，不显示时间单位（即00:32:25格式），showSecond表示是否显示秒；因为是时间段，如果showDay为false，会将天折算成小时
    /// </summary>
    /// <param name="intervalMS">单位：毫秒</param>
    /// <param name="showZero">是否显示值为0的元素</param>
    /// <returns></returns>
    public static string TimeToFormat2(long intervalMS, bool showDay = true, bool showSecond = false, bool showZero = true)
	{
		string timeStr = "";

		intervalMS /= 1000;//转成单位：秒
		int days = (int)(intervalMS / ((60 * 60) * 24));
		int hours = (int)(intervalMS % ((60 * 60) * 24)) / (60 * 60);
		int minutes = (int)((intervalMS % ((60 * 60) * 24)) % (60 * 60)) / 60;
		int seconds = (int)(intervalMS % 60);

		bool getNoZero = false;
		if (showDay && (days > 0 || showZero))
		{//TextSystem.GetText("tian")                  
            timeStr = string.Format("{0:00}{1:00}:{2}", timeStr, days, "");
			getNoZero = true;
		}
		if (hours > 0 || showZero || getNoZero)
		{//TextSystem.GetText("xiaoshi")

			if (showDay == false)
			{
				hours += days * 24;
			}

            timeStr = string.Format("{0:00}{1:00}:{2}", timeStr, hours, "");
			getNoZero = true;
		}
		if (minutes > 0 || showZero || getNoZero)
		{//TextSystem.GetText("minute")
			
            timeStr = string.Format("{0:00}{1:00}:{2}", timeStr, minutes, "");
			getNoZero = true;
		}

		if (showSecond)
		{
			if (seconds > 0 || showZero || getNoZero)
			{//TextSystem.GetText("second")

                if (timeStr == "")
                {
                    timeStr = string.Format("{0:00}00:{1:00}", timeStr, seconds);
                }
                else
                {
                    timeStr = string.Format("{0:00}{1:00}", timeStr, seconds);
                }                                
			}
		}
        else
        {
            //不显示秒之后还有两个点所以去掉了
            return timeStr.Substring(0, timeStr.Length - 1);
        }

		return timeStr;
	}

    /// <summary>
    /// 将时间段（秒）转化成“天、小时、分”，显示时间单位（天、时、分等）
    /// showSecond   -->  是否显示秒，
    /// showDay      -->  是否显示天数
    /// showSecond   -->  是否显示秒数
    /// </summary>
    /// <returns></returns>
    public static string TimeToFormat3(long intervalS, bool showDay, bool showSecond = false, bool showZero = true)
    {
        string timeStr = "";
        
        int days = (int)(intervalS / ((60 * 60) * 24));
        int hours = 24;
        if(showDay)
        {
            hours = (int)(intervalS % ((60 * 60) * 24)) / (60 * 60);
        }
        else
        {
            hours = (int)(intervalS) / (60 * 60);
        }

        int minutes = (int)((intervalS % ((60 * 60) * 24)) % (60 * 60)) / 60;
        int seconds = (int)(intervalS % 60);

        bool getNoZero = false;
        if ((days > 0 || showZero) && showDay)
        {
            timeStr = string.Format("{0}{1}{2}", timeStr, days, TextSystem.GetText("tian"));
            getNoZero = true;
        }
        if (hours > 0 || showZero || getNoZero)
        {
            timeStr = string.Format("{0}{1}{2}", timeStr, hours, TextSystem.GetText("xiaoshi"));
            getNoZero = true;
        }
        if (minutes > 0 || showZero || getNoZero)
        {
            timeStr = string.Format("{0}{1}{2}", timeStr, minutes, TextSystem.GetText("minute"));
            getNoZero = true;
        }

        if (showSecond)
        {
            if (seconds > 0 || showZero || getNoZero)
            {
                timeStr = string.Format("{0}{1}{2}", timeStr, seconds, TextSystem.GetText("second"));
            }
        }

        return timeStr;
    }

	/// <summary>
	/// 清理字符串，去除空格，制表符，回车等
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string CleanStr(string str)
	{
		return str.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "").Replace(" ", "");
	}

	/// <summary>
	/// Md5s the sum.
	/// </summary>
	/// <returns>The sum.</returns>
	/// <param name="strToEncrypt">String to encrypt.</param>
	public static string Md5Sum(string strToEncrypt)
	{
		System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
		byte[] bytes = ue.GetBytes(strToEncrypt);
		
		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);
		
		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";
		
		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}
		
		return hashString.PadLeft(32, '0');
	}


    /// <summary>
    /// 根据总数量和每页，计算页数
    /// </summary>
    /// <param name="totalSize"></param>
    /// <param name="countPerPage"></param>
    /// <returns></returns>

    public static int ComputePageCount(int totalSize, int countPerPage)
	{
		int pageCount = totalSize / countPerPage;
		if (totalSize % countPerPage != 0)
		{
			pageCount++;
		}

		return pageCount;
	}
    
    
    /// <summary>
    ///正则表达式判断字符是否为纯数字的扩展函数
    /// </summary>
    /// <returns></returns>
    public static bool IsNumeric(this string value)
    {
        return Regex.IsMatch(value, @"^[+-]?/d*[.]?/d*$");
    }

    /// <summary>
    ///数字转汉字显示
    /// </summary>
    /// <returns></returns>
    public static string NumberToChinese(int number)
    {
        string res ;
        string str = number.ToString();
        string str1 = "";
        bool isEmpty = false;
        for (int i = 0; i < str.Length; i++)
        {
            res = str[i].ToString();
            switch (res)
            {
                case "1":
                    res = TextSystem.GetText("1");
                    break;
                case "2":
                    res = TextSystem.GetText("2");
                    break;
                case "3":
                    res = TextSystem.GetText("3");
                    break;
                case "4":
                    res = TextSystem.GetText("4");
                    break;
                case "5":
                    res = TextSystem.GetText("5");
                    break;
                case "6":
                    res = TextSystem.GetText("6");
                    break;
                case "7":
                    res = TextSystem.GetText("7");
                    break;
                case "8":
                    res = TextSystem.GetText("8");
                    break;
                case "9":
                    res = TextSystem.GetText("9");
                    break;
                case "0":
                    res = TextSystem.GetText("word_0");
                    break;
                default:
                    break;
            }
            str1 += res;
            if (str.Length - i > 1 && res != TextSystem.GetText("word_0"))
            {
                switch (str.Length - i)
                {
                    case 2:
                        res = TextSystem.GetText("10");
                        break;
                    case 3:
                        res = TextSystem.GetText("word_hundred");
                        break;
                    case 4:
                        res = TextSystem.GetText("word_thousand");
                        break;
                    //case 5:
                    //    res = "万";
                    //    break;
                    //case 6:
                    //    res = "十万";
                    //    break;
                    //case 7:
                    //    res = "百万";
                    //    break;
                    default:
                        continue;
                }
                str1 += res;
            }
        }
        str = "";
        for (int i = 0; i < str1.Length; i++)
        {
            if (str1[i] == TextSystem.GetText("word_0").ToCharArray()[0])
            {
                if (isEmpty)
                {
                    continue;
                }
                isEmpty = true;
            }
            else
            {
                isEmpty = false;
            }
            str += str1[i];
        }
        if (str[str.Length - 1] == TextSystem.GetText("word_0").ToCharArray()[0])
        {
            str = str.Substring(0,str.Length - 1);
        }
        return str;
    }

    /// <summary>
    /// 为序列的某一部分进行划分排序
    /// </summary>
    /// <param name="arr"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public static void QuickSort(int[] arr, int start, int end)
    {
        if (start < end)
        {
            int s = start;
            int e = end;
            // true:e可以移动 false:s可以移动
            bool move = true;

            while (s < e)
            {
                // 左>右
                if (arr[s] > arr[e])
                {
                    int temp = arr[s];
                    arr[s] = arr[e];
                    arr[e] = temp;
                    move = !move;
                }
                if (move)
                {
                    e--;
                }
                else
                {
                    s++;
                }
            }
            // 为刚才划分的左半部分再次划分
            QuickSort(arr, start, e - 1);
            // 为刚才划分的右半部分再次划分
            QuickSort(arr, s + 1, end);
        }
    }

	public static void SaveStringToFile(string str, string path)
	{
		FileStream fs = File.OpenWrite(path);
		Byte[] codeText = new UTF8Encoding(true).GetBytes(str);
		fs.SetLength(0);
		fs.Flush();
		fs.Write(codeText, 0, codeText.Length);
		fs.Flush();
		fs.Close();
	}

	public static string LoadStringFromFile(string path)
	{
		string str = "";
		if (File.Exists(path))
		{
			FileStream fs = File.OpenRead(path);
			byte[] bBuffer = new byte[1000000];
			fs.Read(bBuffer, 0, 1000000);
			fs.Close();
			str = System.Text.Encoding.Default.GetString(bBuffer);
		}
		return str;		
	}

    /**
     * 定时器。定时触发一次Action。
     */
    public static void SetTimeout(double interval, Action action)
    {
        System.Timers.Timer timer = new System.Timers.Timer(interval);
        timer.Elapsed += delegate(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Enabled = false;
            action();
        };
        timer.Enabled = true;
    }

    /// <summary>
	/// 将0-255这样表示颜色的东东转换为0-1表示的东东
	/// </summary>
	public static Color ColorByteToFloat(byte r, byte g, byte b, byte a = 255)
    {
        //---- 以下这块是生成的代码，不要手工改动！ ----
        if (LuaInject.HasPatch("Constant", "ColorByteToFloat"))
        {
            object ret = LuaInject.CallPatch("Constant", "ColorByteToFloat", new object[] {
                r,
                g,
                b,
                a
            });
            return (Color)(ret);
        }
        //---- 以上这块是生成的代码，不要手工改动！ ----
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
}

/// <summary>
/// 用于统计各部分耗时，分析性能消耗
/// </summary>
public class SimpleProfile
{
    /*
	static Dictionary<string, GOLib.ExtendType.Pair<long, long>> timerDict = new Dictionary<string,GOLib.ExtendType.Pair<long,long>>(10);

	public static void Init()
	{
		timerDict.Clear();
	}

	//注意Start和End必须成对出现
	public static void Start(string name)
	{
		if (timerDict.ContainsKey(name) == false)
		{
			timerDict.Add(name, new GOLib.ExtendType.Pair<long,long>());
		}
		timerDict[name].d1 = DateTime.Now.Ticks;
	}

	public static void End(string name)
	{
		if (timerDict.ContainsKey(name) == false)
		{
			timerDict.Add(name, new GOLib.ExtendType.Pair<long, long>());
		}
		timerDict[name].d2 = DateTime.Now.Ticks;
	}

	public static void Output()
	{
		foreach (KeyValuePair<string, GOLib.ExtendType.Pair<long, long>> kvp in timerDict)
		{
			Debug.Log(string.Format("Simple Profile:: '{0}' use time: {1} ms", kvp.Key, (kvp.Value.d2 - kvp.Value.d1) / 10000));
		}
	}
    */
}

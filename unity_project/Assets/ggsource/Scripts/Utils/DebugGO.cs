using System;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using System.IO;
using System.Text;
using System.IO.Compression;

//正式做版本时 【依次】 执行下面两个全局替换（不要全字匹配）即可注释掉所有log，做好版本将所有替换都还原即可（不要上传，log在开发时需要）
//1.UnityEngine.Debug.Log 替换成 DebugGO.UnityEngineDebugLog
//2.Debug.Log 替换成 DebugGO.DebugLog
public class DebugGO
{
	public static void DebugLog(object str)
	{
	}

	public static void DebugLog(object message, UnityEngine.Object context)
	{
	}

	public static void DebugLogError(object str)
	{
	}

	public static void DebugLogError(object message, UnityEngine.Object context)
	{
	}

	public static void DebugLogErrorFormat(string format, params object[] args)
	{
	}

	public static void DebugLogErrorFormat(object message, UnityEngine.Object context)
	{
	}

	public static void DebugLogWarning(object str)
	{
	}

	public static void DebugLogWarning(object message, UnityEngine.Object context)
	{
	}

	public static void DebugLogException(Exception exception)
	{
	}

	public static void DebugLogException(Exception exception, UnityEngine.Object context)
	{
	}

	public static void DebugLogFormat(string format, params object[] args)
	{
	}

	public static void DebugLogWarningFormat(string format, params object[] args)
	{
	}

	public static void DebugLogWarningFormat(UnityEngine.Object context, string format, params object[] args)
	{
	}


	public static void UnityEngineDebugLog(object str)
	{
	}

	public static void UnityEngineDebugLogError(object str)
	{
	}

	public static void UnityEngineDebugLogWarning(object str)
	{
	}

	public static void UnityEngineDebugLogException(Exception exception)
	{
	}

	public static void UnityEngineDebugLogException(Exception exception, UnityEngine.Object context)
	{
	}

	public static void UnityEngineDebugLogWarningFormat(string format, params object[] args)
	{
	}

	public static void UnityEngineDebugLogWarningFormat(UnityEngine.Object context, string format, params object[] args)
	{
	}

	//Debugger
	public static void DebuggerLog(object message){}
	public static void DebuggerLog(string str){}
	public static void DebuggerLog(string str, object arg0){}
	public static void DebuggerLog(string str, params object[] param){}
	public static void DebuggerLog(string str, object arg0, object arg1){}
	public static void DebuggerLog(string str, object arg0, object arg1, object arg2){}
	public static void DebuggerLog(object message, UnityEngine.Object context)
	{
	}

	public static void DebuggerLogError(object message){}
	public static void DebuggerLogError(string str){}
	public static void DebuggerLogError(string str, object arg0){}
	public static void DebuggerLogError(string str, params object[] param){}
	public static void DebuggerLogError(string str, object arg0, object arg1){}
	public static void DebuggerLogError(string str, object arg0, object arg1, object arg2){}

	public static void DebuggerLogError(object message, UnityEngine.Object context)
	{
	}

	public static void DebuggerLogErrorFormat(string format, params object[] args)
	{
	}

	public static void DebuggerLogErrorFormat(object message, UnityEngine.Object context)
	{
	}

	public static void DebuggerLogWarning(object message){}
	public static void DebuggerLogWarning(string str){}
	public static void DebuggerLogWarning(string str, object arg0){}
	public static void DebuggerLogWarning(string str, params object[] param){}
	public static void DebuggerLogWarning(string str, object arg0, object arg1){}
	public static void DebuggerLogWarning(string str, object arg0, object arg1, object arg2){}
	public static void DebuggerLogWarning(object message, UnityEngine.Object context)
	{
	}

	public static void DebuggerLogException(Exception exception)
	{
	}

	public static void DebuggerLogException(Exception exception, UnityEngine.Object context)
	{
	}

	public static void DebuggerLogFormat(string format, params object[] args)
	{
	}

	public static void DebuggerLogWarningFormat(string format, params object[] args)
	{
	}

	public static void DebuggerLogWarningFormat(UnityEngine.Object context, string format, params object[] args)
	{
	}


	public static void UnityEngineDebuggerLog(object str)
	{
	}

	public static void UnityEngineDebuggerLogError(object str)
	{
	}

	public static void UnityEngineDebuggerLogWarning(object str)
	{
	}

	public static void UnityEngineDebuggerLogException(Exception exception)
	{
	}

	public static void UnityEngineDebuggerLogException(Exception exception, UnityEngine.Object context)
	{
	}

	public static void UnityEngineDebuggerLogWarningFormat(string format, params object[] args)
	{
	}

	public static void UnityEngineDebuggerLogWarningFormat(UnityEngine.Object context, string format, params object[] args)
	{
	}
}

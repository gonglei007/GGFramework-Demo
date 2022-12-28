using System;
using System.Collections;
using System.Collections.Generic;

/**
 * 代码处理工具，用于代码混淆。
 * 动态运行混淆，调用分支跳转。
 * 静态代码混淆，加入无用方法的调用。
 */
public class CodeTool {

    static CodeTool						instance;
	static int[]						randomInts = new int[30];
	static int							randomIndex = 0;
	static List<int>					numList = new List<int>(11);
	static Dictionary<int, int>			numDict = new Dictionary<int, int>(11);
	private int							dummyValue = 0;

    public static CodeTool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CodeTool();
				CreateRandInt();
            }
            return instance;
        }
    }

	private static void CreateRandInt()
	{
		Random r = new Random();
		for (int i = 0; i < 30; i++)
		{
			randomInts[i] = r.Next(1, 11);
		}
		for (int i = 0; i < 11; i++)
        {
            numList.Add(i);
            numDict.Add(i, i + 1);
        }
	}

	private static int GetRandomByIndex()
	{
		randomIndex++;
		if (randomIndex >= 30)
		{
			randomIndex = 0;
		}
		return randomInts[randomIndex];
	}


    public bool GetSomeFlag()
    {
		int index = GetRandomByIndex();

        if (index % 2 == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public void DoSomething()
    {
		int index = GetRandomByIndex();

        if (index == 0)
        {
			numDict[index] = dummyValue;
			int num = GetRandomByIndex();
			numList[index] = num - numDict[index];
        }
        else if (index == 1)
        {
			int m = 9;
			dummyValue = m + 1;
        }
        else if (index == 2)
        {
            int num = 10;
            num &= dummyValue;
			dummyValue = num;
        }
        else if (index == 3)
        {
            int[] numArr = new int[] { 5 };
            for (int i = 0; i < numArr.Length; i++)
            {
                numArr[i] = i;
            }
        }
        else if (index == 4)
        {
            numDict[index] = dummyValue %  numList[index];
			numList[index] = dummyValue;
        }
        else if (index == 5)
        {
            bool bo = true;
            if (bo)
            {
                bo = false;
            }
            else
            {
                bo = true;
            }
        }
        else if (index == 6)
        {
			numList[index] &= 16;
			int count = numList[index];
        }
        else if (index == 7)
        {
			List<int> list1 = new List<int>();
            list1.Add(dummyValue);
			list1.Add(1);
            int count1 = list1.Count;
        }
        else if (index == 8)
        {
			int m = dummyValue;
			numList[index] = m & numDict[index];
			dummyValue = numList[index] ++;
        }
        else if (index == 9)
        {
			Dictionary<int, int> tempDict = new Dictionary<int,int>(3);
			int m = dummyValue + 1;
			tempDict[0] = m ^ 10036;
			tempDict[1] = m;
			tempDict[2] = tempDict[0] + tempDict[1];
        }
    }


    public void DoAnotherthing()
    {
		int index = GetRandomByIndex();

        if (index == 0)
        {
            int newRand = GetRandomByIndex();
			numDict[index] = dummyValue * newRand;
			numList[index] = dummyValue & newRand;
        }
        else if (index == 1)
        {
            string s = "flags";
			if (dummyValue > 100)
			{
				s = s.Substring(1);
			}
			else
			{
				s = s.Substring(2);
			}
        }
        else if (index == 2)
        {
            int num = 10;
            int num2 = 21;
			dummyValue = (num & dummyValue) + num2;
        }
        else if (index == 3)
        {
            int[] numArr = new int[] { 5 };
            for (int i = 0; i < numArr.Length; i++)
            {
                numArr[i] = i;
            }
        }
        else if (index == 4)
        {
            int m = 5;
			int n = 6;
			int a = m * n;
			m = a + n;
        }
        else if (index == 5)
        {
            int a = 1;
            int b = 2;
            int c = a ^ b;
            b = a ^ c;
            a = b ^ c;
        }
        else if (index == 6)
        {
            List<int> numList = new List<int>();
            numList.Add(5);
            int count = numList.Count;
        }
        else if (index == 7)
        {
            Dictionary<string, int> dic1 = new Dictionary<string, int>();
            dic1.Add("1", 1);
            int count1 = dic1.Count;
        }
        else if (index == 8)
        {
            string s = "123".Substring(1);
        }
        else if (index == 9)
        {
            int num3 = 10;
            numList[index] = numDict[0];
			numDict[index] = num3 + numList[index];
        }
    }

    public void SetDummy(int value) {
        this.dummyValue = value;
    }
}

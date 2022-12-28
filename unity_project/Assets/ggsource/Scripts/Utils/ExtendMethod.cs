using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtendMethod
{
    /// <summary>
    /// 加强版Find,可以用root/xxx[2]/aaa/sss这样的形式来表示重名节点
    /// </summary>
    /// <param name="trans"></param>
    /// <returns></returns>
    public static Transform FindEx(this Transform root, string path)
    {
        if (root == null)
        {
            throw new NullReferenceException("root cannot be null");
        }

        if (path == string.Empty)
        {
            return root;
        }
        else if (path == null)
        {
            return null;
        }

        string[] pathArray = path.Split('/');

        if (root != null)
        {
            Transform result = root;            

            for (int i = 0; i < pathArray.Length && result != null; i++)
            {
                string subPath = pathArray[i];
                if (subPath.Length > 0)
                {

                    if (subPath == "..")
                    {
                        result = result.parent;
                        if (result == null)
                        {
                            return null;
                        }
                    }
                    else if (subPath[subPath.Length - 1] != ']')
                    {
                        result = FindChildTransform(result, subPath, 0);
                    }
                    else
                    {
                        int childIndexStart = subPath.LastIndexOf("[", subPath.Length - 2);

                        string childIndexString = subPath.Substring(childIndexStart + 1, subPath.Length - 2 - childIndexStart);

                        result = FindChildTransform(result, subPath.Substring(0, childIndexStart), Convert.ToInt32(childIndexString));
                    }
                }
                else
                {
                    if (i > 0)
                    {
                        Debug.LogWarning("FindEx : path参数格式不正确 - " + path);
                    }
                }
            }

            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 修改Vector的Y数值
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="xValue"></param>
    /// <returns></returns>
    public static void SetYValue(this Transform vector, string isLocal, float yValue)
    {
        if(isLocal.Contains("local"))
        {
            vector.localPosition = new Vector3 (vector.transform.localPosition.x, yValue, vector.transform.localPosition.z);
        }
        else
        {
            vector.position = new Vector3 (vector.transform.localPosition.x, yValue, vector.transform.localPosition.z);
        }
    }


    /// <summary>
    /// 修改Vector的Y数值
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="xValue"></param>
    /// <returns></returns>
    public static void SetXValue (this Transform vector, string isLocal, float xValue)
    {
        if (isLocal.ToLower ().Contains ("local"))
        {
            vector.localPosition = new Vector3 (xValue, vector.transform.localPosition.y, vector.transform.localPosition.z);
        }
        else
        {
            vector.position = new Vector3 (xValue, vector.transform.localPosition.y, vector.transform.localPosition.z);
        }
    }

    /// <summary>
    /// 修改Vector的XY数值
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="xValue"></param>
    /// <returns></returns>
    public static void SetXYValue (this Transform vector, string isLocal, float xValue, float yValue)
    {
        SetXValue (vector, isLocal, xValue);
        SetYValue (vector, isLocal, yValue);
    }

    public static void SetPosition(this Transform trans, float x, float y, float z, string isLocal = "local")
    {
        if (isLocal.Contains("local"))
        {
            trans.localPosition = new Vector3(x, y, z);
        }
        else
        {
            trans.position = new Vector3(x, y, z);
        }
    }

    public static void ResetLocalPositionToOne(this Transform trasform)
    {
        trasform.localPosition = Vector3.one;
    }

    /// <summary>
    /// childIndex : 如果有同名子物体，则找出childIndex + 1个子物体，第从0开始
    /// </summary>
    private static Transform FindChildTransform(Transform parent, string childName, int childIndex)
    {
        Transform child = null;
        if (parent != null)
        {
            int repeat = -1;
            for (int i = 0; i < parent.childCount; i++)
            {
                child = parent.GetChild(i);
                if (child.name.Length == childName.Length && child.name == childName)
                {
                    repeat += 1;
                    if (repeat == childIndex)
                    {
                        return child;
                    }
                }
            }
        }
        //Debug.LogWarning("><>>>> " + parent.name + "  " + childName + "  " + childIndex);
        return null;
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaInterface;

[RequireComponent(typeof(UIWrapContent))]
public class WrapContentCallBack : MonoBehaviour
{
    public LuaFunction luaCallBack;

    private UIWrapContent _wrapConetent;
    private void Awake()
    {
        _wrapConetent = GetComponent<UIWrapContent>();

        _wrapConetent.onInitializeItem = (go, index, realIndex) =>
        {
            if (luaCallBack != null)
            {
                luaCallBack.Call(go,index,realIndex);
            }
        };
    }

    public void SetLuaCallback(LuaFunction luaCallBack)
    {
        this.luaCallBack = luaCallBack;
    }
}

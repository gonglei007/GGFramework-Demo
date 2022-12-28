using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaInterface;

public class Object2D : MonoBehaviour
{
    public LuaFunction onTrigger = null;

    void OnTriggerEnter2D(Collider2D collider) {
        //Debug.LogWarning("有物体发生碰撞！！！");
        if (onTrigger != null) {
            onTrigger.Call(collider);
        }
    }
}

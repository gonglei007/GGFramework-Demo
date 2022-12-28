using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using DG.Tweening;

public enum CheckType
{
    SceneCard = 1,
    EnemyPlayer = 2,
    MyPlayer = 3,
};

public class ObjectTouchEvent : MonoBehaviour,IPointerClickHandler
{
    public CheckType checkType = CheckType.SceneCard;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (checkType == CheckType.SceneCard)
        {
            LuaFramework.Util.CallMethod("BattleSceneView", "SceneCardOnClick", transform);
        }
        else if (checkType == CheckType.EnemyPlayer)
        {
            LuaFramework.Util.CallMethod("BattleSceneView", "EnemyPlayerOnClick", transform);
        }
        else if (checkType == CheckType.MyPlayer)
        {
            LuaFramework.Util.CallMethod("BattleSceneView", "MyPlayerOnClick", transform);
        }
    }
}

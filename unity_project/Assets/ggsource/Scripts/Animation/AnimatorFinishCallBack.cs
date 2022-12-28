using LuaInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorFinishCallBack : MonoBehaviour
{
    private LuaFunction animationFinishCallBack;
    private Animator m_Animator = null;
    private RuntimeAnimatorController m_runtimeAnimatorController = null;
    private AnimationClip[] clips = null;
    private LuaFunction assignAnimaFinishCallBack;
    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        // 获取运行时运行时动画器控制器  
        m_runtimeAnimatorController = m_Animator.runtimeAnimatorController;
        //获取含有的动画片段  
        clips = m_runtimeAnimatorController.animationClips;

        AnimationEvent animaFinishEvent = new AnimationEvent();
        animaFinishEvent.functionName = "AnimationFinishCallBack";
        animaFinishEvent.time = clips[0].length;
        clips[0].AddEvent(animaFinishEvent);
    }

    public void ClearAllEvents()
    {
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].events = default;
        }
    }

    public void SetAssignAnimationEvent(string animaName,LuaFunction luaFuc,float eventTime = 0)
    {
        assignAnimaFinishCallBack = luaFuc;
        for (int i = 0; i < clips.Length; i++)
        {
            if (animaName == clips[i].name)
            {
                AnimationEvent animationEvent = new AnimationEvent();
                animationEvent.functionName = "AssignAnimationEvent";
                if (eventTime == 0)
                {
                    animationEvent.time = clips[i].length;
                }
                else
                {
                    animationEvent.time = eventTime;
                }
                clips[i].AddEvent(animationEvent);
            }
        }
    }

    public void AssignAnimationEvent()
    {
        if (assignAnimaFinishCallBack != null)
        {
            assignAnimaFinishCallBack.Call();
        }
    }

    public void SetAnimationFinishCallBack(LuaFunction luaFunction)
    {
        animationFinishCallBack = luaFunction;
    }

    void AnimationFinishCallBack()
    {
        if (animationFinishCallBack != null)
        {
            animationFinishCallBack.Call();
        }
    }
}

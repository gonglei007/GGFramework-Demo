using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationClipEventX : MonoBehaviour
{
    //private Animator m_Animator = null;
    //private RuntimeAnimatorController m_runtimeAnimatorController = null;
    //private AnimationClip[] clips = null;

    void Start()
    {
        //m_Animator = GetComponent<Animator>();
        // 获取运行时运行时动画器控制器  
        //m_runtimeAnimatorController = m_Animator.runtimeAnimatorController;
        //获取含有的动画片段  
        //clips = m_runtimeAnimatorController.animationClips;

        //根据动画名称设置对应的事件  
        //for (int i = 0; i < clips.Length; i++)
        //{
            //AnimationEvent animationEvent = new AnimationEvent();
            //animationEvent.functionName = "TriggerEvent";
            //animationEvent.time = clips[i].length;
            //clips[i].AddEvent(animationEvent);
            //switch (clips[i].name)
            //    {
            //        case "HeroSkillShow":
            //            {
            //                AnimationEvent RequestStartCard_Func = new AnimationEvent();
            //                RequestStartCard_Func.functionName = "HeroSkillShow";
            //                RequestStartCard_Func.time = clips[i].length;
            //                clips[i].AddEvent(RequestStartCard_Func);
            //            }
            //            break;
            //    }
        //}
        //m_Animator.Rebind();
    }

    void TriggerEvent(string luaParam)
    {
        Debug.LogFormat("触发动画事件->{0}", luaParam);
        string[] luaParams = luaParam.Split('.');
        if (luaParams.Length >= 2) {
            LuaFramework.Util.CallMethod(luaParams[0], luaParams[1], this.gameObject, transform.parent.name);
        }
    }
}

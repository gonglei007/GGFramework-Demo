using LuaInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D场景上的演员，有模型，有表现(动画等)。
/// </summary>
[RequireComponent(typeof(Animator))]//必须有Animator 组件
public class Actor : MonoBehaviour
{
    LuaFunction _OnUpdate;
    public MeshRenderer[] meshRenderers;
    public SkinnedMeshRenderer[] skinnedMeshes;
    private Animator _Animator;
    private string animType = null;
    private void Awake()
    {
        _Animator = GetComponent<Animator>();
    }

    public Animator GetAnimator()
    {
        return _Animator;
    }

    /// <summary>
    /// 播放一个指定名称的动画。
    /// </summary>
    /// <param name="animType">动作类型，比如Idle、Attack、Dead</param>
    /// <param name="animID">动作ID，同类型动作可能有多个ID，此参数指定具体是哪个ID的动作。</param>
    public void PlayAnimation(string animType, int animID) 
    {
        this.animType = animType;
        string animName = string.Format("{0}_{1}", animType, animID);
        _Animator.SetBool(animName, true);
    }

    /// <summary>
    /// 停止当前动画
    /// </summary>
    /// <param name="animType"></param>
    /// <param name="animID"></param>
    public void StopAnimation(string animType,int animID)
    {
        this.animType = string.Empty;
        string animName = string.Format("{0}_{1}", animType, animID);
        _Animator.SetBool(animName, false);
    }
    
    /// <summary>
    /// 注册lua回调
    /// </summary>
    /// <param name="onUpdate"></param>
    public void SetLuaFunction(LuaFunction onUpdate)
    {
        _OnUpdate = onUpdate;
    }

    private void Update()
    {
        if (_OnUpdate != null)
        {
            _OnUpdate.Call(Time.deltaTime);
        }
    }
}

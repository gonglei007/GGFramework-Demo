using LuaInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsStopCallBack : MonoBehaviour
{
    private LuaFunction luaFunc;
    public void SetLuaFunc(LuaFunction func)  
    {
        luaFunc = func;
    }

    private void OnEnable()
    {
        float stopTime = ParticleSystemLength(transform);

        StartCoroutine(StartPlay(stopTime));
    }

    IEnumerator StartPlay(float stopTime)
    {
        yield return new WaitForSeconds(stopTime);
        OnFinish();
    }

    public void OnFinish()
    {
        if (luaFunc != null)
            luaFunc.Call();

        gameObject.SetActive(false);
    }

    float ParticleSystemLength(Transform transform)
    {
        ParticleSystem[] particleSystems = transform.GetComponentsInChildren<ParticleSystem>();
        float maxDuration = 0;
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps.emission.enabled)
            {
                if (ps.main.loop)
                {
                    return -1f;
                }
                float dunration = 0f;
                if (ps.emissionRate <= 0)
                {
                    dunration = ps.startDelay + ps.startLifetime;
                }
                else
                {
                    dunration = ps.startDelay + Mathf.Max(ps.duration, ps.startLifetime);
                }
                if (dunration > maxDuration)
                {
                    maxDuration = dunration;
                }
            }
        }
        return maxDuration;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonFX : FXBase
{
    private Transform attacher;
    private float delayTime;
    private float duration;
    private Action callback = null;
    private TimerSystem.Timer timer;

    public void Init(float delayTime, float duration, Vector3 pos, Transform binder = null, bool flip = false, Action callback = null)
    {
        this.delayTime = delayTime;
        this.duration = duration;
        this.binder = binder;
        this.callback = callback;
        if (pos != null)
        {
            this.transform.position = pos;
        }
        if (flip)
        {
            //todo: 特效翻转的逻辑
        }
    }
    public override void Play()
    {
        timer = TimerSystem.Instance.CreateTimerAndStart(delayTime, 1, PlayVFX);
        Debug.Log("____________Play___________");
    }
    private void PlayVFX()
    {
        this.gameObject.SetActive(true);
        timer = TimerSystem.Instance.CreateTimerAndStart(duration, 1, () =>
        {
            this.gameObject.SetActive(false);
            Restart();
            if (callback != null)
            {
                callback();
            }
        });
    }
    public override void Stop()
    {
        if (timer != null)
        {
            timer.Stop();
        }
        this.gameObject.SetActive(false);
        Restart();
    }
    private void Restart()
    {
        this.delayTime = 0;
        this.duration = 0;
        this.attacher = null;
        this.callback = null; 
        this.timer = null;
        this.binder = null;
    }
    public override void Update()
    {
        if (true)
        {

        }
    }
}

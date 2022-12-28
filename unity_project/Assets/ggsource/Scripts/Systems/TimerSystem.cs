using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// 计时器管理
public class TimerSystem : Singleton<TimerSystem>
{
    // 计时器。
    /*
    等待多久执行后续操作，重复多少次(0~无数次)
    开始计时，结束计时
    */
    public class Timer
    {
        // 等待周期
        public float During;
        // 循环次数(负数表示无限循环)
        public int Repeat;
        public Action Callback;

        // 从计时开始到现在，一共经历了多久
        float _passedTime;
        int _repeated;

        public void Start()
        {
            _passedTime = 0;
            _repeated = 0;

            TimerSystem.Instance.AllTimerLoop += Loop;

        }

        public void Stop()
        {
            TimerSystem.Instance.AllTimerLoop -= Loop;
        }

        // 循环，deltaTime:两次调用Loop的时间间隔
        public void Loop(float deltaTime)
        {
            _passedTime += deltaTime;
            if (_passedTime >= During)
            {
                _passedTime -= During; // 计时重置

                _repeated++; // 限制循环次数
                if ((Repeat >= 0) && (_repeated > Repeat))
                {
                    Stop();
                    return;
                }
                try
                {
                    if (Callback != null) { Callback(); }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
            }
        }
    }

    public event Action<float> AllTimerLoop;

    //创建一个待启动的计时器
    public Timer CreateTimer(float during, int repeat, Action callback)
    {
        var t = new Timer();
        t.During = during;
        t.Repeat = repeat;
        t.Callback = callback;

        return t;
    }

    //创建一个立即启动的计时器
    public Timer CreateTimerAndStart(float during, int repeat, Action callback)
    {
        var timer = CreateTimer(during, repeat, callback);
        timer.Start();
        return timer;
    }

    //每帧执行callback方法
    public Timer CreateFrameTimer(Action callback)
    {
        return CreateTimer(-1, -1, callback);
    }

    public void Loop(float deltaTime)
    {
        if (AllTimerLoop != null)
        { AllTimerLoop(deltaTime); }
    }


   
}
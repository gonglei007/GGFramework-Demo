using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[RequireComponent(typeof(AudioSource))]
public class SoundSystem : Singleton<SoundSystem>
{
    #region 背景音乐播放
    private AudioClip PreBGMAudio;//上一次播放的背景音乐

    private AudioSource BGMAudioSource;//背景音乐播放器

    private static float bgmVolume = 0.5f;

    private Dictionary<string, AudioClip> SoundCache;//声音缓存

    public static float BGMVolume //背景音乐声音大小
    {
        get
        {
            return bgmVolume;
        }

        set
        {
            bgmVolume = value;

            if (Instance.BGMAudioSource != null)
                Instance.BGMAudioSource.volume = bgmVolume;
        }
    }

    public virtual bool BGMMute
    {
        get
        {
            return Instance.BGMAudioSource.mute;
        }

        set
        {
            if (Instance.BGMAudioSource != null)
            {
                Instance.BGMAudioSource.mute = value;
            }
        }
    }

    private void Awake()
    {
        BGMAudioSource = GetComponent<AudioSource>();
        SoundCache = new Dictionary<string, AudioClip>();
        BGMAudioSource.volume = 0;
        BGMAudioSource.loop = true;
        BGMAudioSource.spatialBlend = 0;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="audioName">背景音乐名称</param>
    /// <param name="isNeedFade">是否需要淡入淡出</param>
    /// <param name="fadeOut">淡出上一个背景音乐的时间</param>
    /// <param name="fadeIn">淡入背景音乐的时间</param>
    /// <param name="delay">延迟播放时间</param>
    public void PlayBGM(string audioName,bool isNeedFade = true, float fadeOut = 1, float fadeIn = 1, float delay = 0)
    {
        if (isNeedFade)
        {
            DoPlayBGMFade(audioName, fadeOut, fadeIn, delay);
        }
        else
        {
            DoPlayBGM(audioName);
        }
    }

    /// <summary>
    /// 直接播放背景音乐,不设置淡入淡出
    /// </summary>
    /// <param name="audioName"></param>
    private void DoPlayBGM(string audioName)
    {
        AudioClip bgmClip = null;
        
        if (SoundCache.ContainsKey(audioName))
        {
            bgmClip = SoundCache[audioName];
            StartBGMPlay(bgmClip);
        }
        else
        {
            ResourceSystem.Instance.LoadFloatAssetAsync(audioName, (Object bgm) =>
            {
                if(bgm == null)
                    return;
                bgmClip = (AudioClip)bgm;

                if (!SoundCache.ContainsKey(audioName))
                {
                    SoundCache.Add(audioName,bgmClip);
                }

                StartBGMPlay(bgmClip);
            },false);
        }
    }

    private void StartBGMPlay(AudioClip bgm)
    {
        if (BGMAudioSource.isPlaying)
        {
            BGMAudioSource.Stop();
        }

        BGMAudioSource.clip = bgm;
        PreBGMAudio = bgm;
        BGMAudioSource.Play();
        BGMAudioSource.volume = BGMVolume;
    }

    /// <summary>
    /// 淡入淡出播放背景音乐
    /// </summary>
    /// <param name="audioName"></param>
    /// <param name="fadeOut"></param>
    /// <param name="fadeIn"></param>
    /// <param name="delay"></param>
    private void DoPlayBGMFade(string audioName, float fadeOut, float fadeIn, float delay)
    {
        AudioClip bgmClip = null;
        if (SoundCache.ContainsKey(audioName))
        {
            bgmClip = SoundCache[audioName];
            StartCoroutine(DoPlayFade(bgmClip));
        }
        else
        {
            ResourceSystem.Instance.LoadFloatAssetAsync(audioName, (Object bgm) =>
            {
                if(bgm == null)
                    return;
                bgmClip = (AudioClip)bgm;
                if (!SoundCache.ContainsKey(audioName))
                {
                    SoundCache.Add(audioName,bgmClip);
                }

                StartCoroutine(DoPlayFade(bgmClip));
            },false);
        }
    }

    private IEnumerator DoPlayFade(AudioClip bgmClip, float fadeOut = 1, float fadeIn = 1, float delay = 0)
    {
        if (BGMAudioSource.isPlaying && BGMAudioSource.clip == bgmClip)
        {
            yield break;
        }

        float time1 = Time.time;

        if (PreBGMAudio != null)
        {
            //淡出上一个背景音乐
            yield return StartCoroutine(StartFadeOut(fadeOut));
        }

        float time2 = Time.time - time1;

        if (delay > time2)
        {
            yield return new WaitForSeconds(delay - time2);
        }

        BGMAudioSource.clip = bgmClip;
        PreBGMAudio = bgmClip;
        BGMAudioSource.Play();

        //淡入下一个背景音乐
        yield return StartCoroutine(StartFadeIn(fadeIn));
    }

    /// <summary>
    ///  背景音乐淡出
    /// </summary>
    /// <param name="fadeOut"></param>
    /// <returns></returns>
    private IEnumerator StartFadeOut(float fadeOut)
    {
        float time = 0;

        while (time <= fadeOut)
        {
            if (time != 0)
            {
                BGMAudioSource.volume = Mathf.Lerp(BGMVolume, 0, time / fadeOut);
            }
            time += Time.deltaTime;
            yield return 1;
        }

        BGMAudioSource.volume = 0;
    }

    /// <summary>
    /// 背景音乐淡入
    /// </summary>
    /// <param name="fadeIn"></param>
    /// <returns></returns>
    private IEnumerator StartFadeIn(float fadeIn)
    {
        float time = 0;
        while (time <= fadeIn)
        {
            if (time != 0)
            {
                BGMAudioSource.volume = Mathf.Lerp(0, BGMVolume, time / fadeIn);
            }
            time += Time.deltaTime;
            yield return 1;
        }
        BGMAudioSource.volume = BGMVolume;
    }
    #endregion

    #region 音效播放
    //音效缓存列表
    private List<AudioInfo> AudioList = new List<AudioInfo>();

    //音效声音大小
    public static float SoundVolume { get; set; } = 0.5f;
    private bool audioMute = false;
    
    /// <summary>
    /// 播放的音效默认不会有位置信息,不会有衰减
    /// </summary>
    /// <param name="audioName"></param>
    public void PlayAudio(string audioName,bool isLoop = false)
    {
        //Debug.Log("audioName__________" + audioName);
        PlayAudioEffect(audioName, Vector3.zero,false, isLoop);
    }

    /// <summary>
    /// 播放有位置信息的音效,声音会随着离播放点的距离衰减
    /// </summary>
    /// <param name="audioName"></param>
    /// <param name="pos"></param>
    public void PlayAudio(string audioName, Vector3 pos, bool isLoop = false)
    {
        PlayAudioEffect(audioName, pos, true, isLoop);
    }
    
    public virtual bool AudioMute
    {
        get
        {
            return audioMute;
        }
        set
        {
            audioMute = value;
            for (int i = AudioList.Count - 1; i >= 0; i--)
            {
                if (AudioList[i] != null)
                {
                    AudioList[i].Mute = audioMute;
                }
            }
            if (audioMute == true)
            {
                SoundVolume = 0;
            }
            else
            {
                SoundVolume = 0.5f;
            }
        }
    }

    /// <summary>
    /// 停止并且销毁所有音效
    /// </summary>
    public void StopAllAudio()
    {
        for (int i = AudioList.Count - 1; i >= 0; i--)
        {
            AudioList[i].Destroy();
        }

        AudioList.Clear();
    }

    public void StopOneAudio(string auidoName)
    {
        for (int i = AudioList.Count - 1; i >= 0; i--)
        {
            if (AudioList[i].AudioName == auidoName)
            {
                UnLoadSound(auidoName);
                AudioList[i].Destroy();
                AudioList.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="auidoEffectName"></param>
    private void PlayAudioEffect(string audioName,Vector3 pos,bool is3D = false, bool isLoop = false)
    {
        AudioClip audioClip = null;
        
        if (SoundCache.ContainsKey(audioName))
        {
            audioClip = SoundCache[audioName];
            StartSoundPlay(audioName, audioClip, pos, is3D, isLoop);
        }
        else
        {
            Debug.LogWarning("加载音频:" + audioName);
            ResourceSystem.Instance.LoadFloatAssetAsync(audioName, (Object sound) =>
            {
                if(sound == null)
                    return;
                audioClip = (AudioClip)sound;
                if (!SoundCache.ContainsKey(audioName))
                {
                    SoundCache.Add(audioName,audioClip);
                }

                StartSoundPlay(audioName, audioClip, pos, is3D, isLoop);
            },false);
        }
    }

    private void StartSoundPlay(string audioName,AudioClip soundClip,Vector3 pos,bool is3D, bool isLoop = false)
    {
        //首先去找现在缓存列表中能使用的音效
        AudioInfo audioInfo = FindCanUseAudio(audioName);

        if (audioInfo != null)
        {
            audioInfo.AudioName = soundClip.name;
            audioInfo.CurAudioSource.clip = soundClip;
            audioInfo.CurAudioSource.volume = SoundVolume;
            audioInfo.CurAudioSource.loop = isLoop;
            audioInfo.Play(pos, is3D);
        }
        else//没有能用的就创建一个新的
        {
            RemoveOverSound();

            audioInfo = new AudioInfo(soundClip, gameObject, isLoop);

            AudioList.Add(audioInfo);

            audioInfo.Play(pos, is3D);
        }
    }

    /// <summary>
    /// 移除过期音效,保证同时存在的音效最多不大于8
    /// </summary>
    private void RemoveOverSound()
    {
        //如果列表中的个数超过了8个，则先把已经播放完毕的移除掉
        if (AudioList.Count >= 8)
        {
            for (int i = AudioList.Count - 1; i >= 0; i--)
            {
                if (Time.time > AudioList[i].PlayEndTime)
                {
                    AudioInfo audioInfo = AudioList[i];
                    AudioList.Remove(audioInfo);
                    audioInfo.Destroy();
                }
            }
        }

        //如果列表中的个数仍然超过8个，则把最早结束的移除掉，直到少于8个
        while (AudioList.Count >= 8)
        {
            AudioInfo info1 = AudioList[0];
            foreach (AudioInfo info2 in AudioList)
            {
                if (info1.PlayEndTime > info2.PlayEndTime)
                {
                    info1 = info2;
                }
            }
            AudioList.Remove(info1);
            info1.Destroy();
        }
    }

    /// <summary>
    /// 找到此时可以使用的AudioInfo
    /// </summary>
    /// <param name="audioName"></param>
    /// <returns></returns>
    private AudioInfo FindCanUseAudio(string audioName)
    {
        foreach (AudioInfo audioInfo in AudioList)
        {
            //如果_AudioList里面缓存的音效已经有播放完毕的就直接返回
            if (Time.time > audioInfo.PlayEndTime)
            {
                return audioInfo;
            }
        }

        List<AudioInfo> infoArray = new List<AudioInfo>();

        foreach (AudioInfo audioInfo in AudioList)
        {
            if (audioInfo.AudioName == audioName)
            {
                infoArray.Add(audioInfo);
            }
        }

        //要播放的音效在缓存中不存在或者只存在一个的情况下,就重新创建一个AudioInfo
        if (infoArray.Count <= 1)
        {
            infoArray = null;
            return null;
        }

        //要播放的音效在缓存中已经不止一个了
        AudioInfo SameAudio = infoArray[0];
        for (int i = 0; i < infoArray.Count; i++)
        {
            //找到离结束时间最近的那个更换成需要播放的音效
            if (SameAudio.PlayEndTime > infoArray[i].PlayEndTime)
            {
                SameAudio = infoArray[i];
            }
        }

        infoArray = null;
        return SameAudio;
    }

    /// <summary>
    /// 释放单一声音缓存
    /// </summary>
    /// <param name="soundName"></param>
    public void UnLoadSound(string soundName)
    {
        if (SoundCache.ContainsKey(soundName))
        {
            AudioClip cache = SoundCache[soundName];
            cache.UnloadAudioData();
           
            SoundCache.Remove(soundName);
        }
    }

    /// <summary>
    /// 释放所有声音缓存
    /// </summary>
    public void ClearSoundCache()
    {
        foreach (var sound in SoundCache.Values)
        {
            sound.UnloadAudioData();
        }
        
        SoundCache.Clear();
    }
}

/// <summary>
/// 音效播放单元
/// </summary>
public class AudioInfo
{
    public AudioSource CurAudioSource;//音频播放器
    public string AudioName;//音效名称字符串
    public float PlayEndTime = 0;//播放结束时刻
    public bool Is3D;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="audioclip">音效</param>
    /// <param name="root">对应的GameObject的父物体</param>
    public AudioInfo(AudioClip audioclip, GameObject root = null, bool isLoop = false)
    {
        AudioName = audioclip.name;

        GameObject obj = new GameObject("Audio_" + AudioName);
        if (root != null)
        {
            obj.transform.parent = root.transform;
        }

        Object.DontDestroyOnLoad(obj);

        CurAudioSource = obj.AddComponent<AudioSource>();
        CurAudioSource.loop = isLoop;
        CurAudioSource.volume = SoundSystem.SoundVolume;//音量大小(0-1)
        CurAudioSource.rolloffMode = AudioRolloffMode.Linear;//按距离衰减模式---线性衰减
        CurAudioSource.minDistance = 30;//按距离衰减模式---起始衰减距离(当AudioSource到AudioListener之间的距离，大于这个数值时才开始音量衰减）
        CurAudioSource.maxDistance = 200;//按距离衰减模式---结束衰减距离(当AudioSource到AudioListener之间的距离，小于这个数值就听不到了)
        CurAudioSource.clip = audioclip;
        CurAudioSource.panStereo = 0.0f;
        CurAudioSource.spatialBlend = 0.0f;
    }

    public void Destroy()
    {
        //停止播放音效
        Stop();

        //把对应的GameObject也释放掉
        Object.Destroy(this.CurAudioSource.gameObject);
    }

    /// <summary>
    /// 在指定的坐标点处，开始播放音效
    /// </summary>
    /// <param name="pos">3D场景内的一个坐标点，要求在这个点处播放声音（备注:u3d的3d声音跟声源的位置有关系，越远则声音越小）</param>
    public void Play(Vector3 pos, bool is3D = false)
    {
        //记录下播放结束时刻
        PlayEndTime = Time.time + CurAudioSource.clip.length;

        CurAudioSource.gameObject.transform.position = pos;
        CurAudioSource.Stop();

        //开始播放
        CurAudioSource.spatialBlend = is3D ? 1 : 0;
        CurAudioSource.Play();
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void Stop()
    {
        CurAudioSource.Stop();
        PlayEndTime = 0f;
    }

    public virtual bool Mute
    {
        get
        {
            return CurAudioSource.mute;
        }

        set
        {
            if (CurAudioSource != null)
            {
                CurAudioSource.mute = value;
            }
        }
    }
}
#endregion

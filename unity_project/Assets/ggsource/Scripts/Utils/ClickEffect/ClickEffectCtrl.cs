using PathologicalGames;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickEffectCtrl:MonoBehaviour
{
    public static ClickEffectCtrl Instance;
 
    private SpawnPool m_EffectPool;

    private Dictionary<string, Transform> m_EffectDic;

    [SerializeField]
    private GameObject clickPrefab;

    [SerializeField]
    private Camera camera;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        if (m_EffectPool == null)
            m_EffectPool = PoolManager.Pools.Create("ClickPool");

        if (m_EffectDic == null)
            m_EffectDic = new Dictionary<string, Transform>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlayClick((Transform click) =>
            {
                Vector3 mousePos = Input.mousePosition;

                Vector3 worldPos = camera.ScreenToWorldPoint(mousePos);
                click.position = worldPos;
                StartCoroutine(DespawnEffect(click, 0.5f));
            });
        }
    }

    IEnumerator DespawnEffect(Transform effectTrans, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        //当特效播放完毕后就将特效回池
        m_EffectPool.Despawn(effectTrans);
    }

    private void PlayClick(Action<Transform> OnComplete)
    {
        if (clickPrefab == null)
        {
            return;
        }
        if (!m_EffectDic.ContainsKey("click"))
        {
            m_EffectDic["click"] = clickPrefab.transform;
            PrefabPool effectPool = new PrefabPool(clickPrefab.transform);
            effectPool.preloadAmount = 0;
            //是否开启缓存池自动清理,比如preloadAmount是5,但是刷出来8个怪,多余的3个就会自动清理掉,防止逻辑上的不严谨造成的刷怪泛滥
            effectPool.cullDespawned = true;
            //自动清理时始终保留几个不清理
            effectPool.cullAbove = 5;
            //多长时间清理一次
            effectPool.cullDelay = 2;
            //清理的时候每次清理几个
            effectPool.cullMaxPerPass = 2;
            m_EffectPool.CreatePrefabPool(effectPool);
            if (OnComplete != null)
            {
                OnComplete(m_EffectPool.Spawn(m_EffectDic["click"]));
            }
        }
        else
        {
            if (OnComplete != null)
            {
                OnComplete(m_EffectPool.Spawn(m_EffectDic["click"]));
            }
        }
    }
}

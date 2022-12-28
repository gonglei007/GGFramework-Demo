using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MarchingBytes;

/// <summary>
/// 特效系统。Init 和 LoadResourceAndReady都调用完毕本系统才能开始工作。
/// 对象池子管理。
/// </summary>
public class VFXSystem : Singleton<VFXSystem>
{
    private int globalIndex = 0;
    public class VFXInfo
    {
        public VFXInfo(string name) {
            this.name = name;
        }

        public string name = "";        // 效果名称。【重要】prefab资源的名称也要是这个名字。
    }

    public EasyObjectPool fxObjectPool;        //效果对象池子
    private VFXInfo[] vfxInfoList = null;
    private Dictionary<string, GameObject> activeFXObjectMap = new Dictionary<string, GameObject>();     // 活跃的效果对象的ID映射表。

    /// <summary>
    /// 初始化特效系統。
    /// </summary>
    /// <param name="resourcePath">存放特效prefab的路径</param>
    /// <param name="vfxInfoList">特效信息的列表。基于此列表加载并初始化特效信息和对象池子</param>
    public bool Init(VFXInfo[] vfxInfoList) {
        this.vfxInfoList = vfxInfoList;
        bool result = false;
        if (vfxInfoList == null || vfxInfoList.Length <= 0) {
            throw new System.Exception("vfxInfos shouldn't been empty!!!");
        }
        PoolInfo[] poolInfoList = new PoolInfo[vfxInfoList.Length];
        for (int i = 0; i < vfxInfoList.Length; ++i) {
            VFXInfo vfxInfo = vfxInfoList[i];
            PoolInfo poolInfo = new PoolInfo();
            poolInfo.poolName = vfxInfo.name;
            ResourceObject prefabObj = ResourceSystem.Instance.GetObject(ResourceSystem.Tag.VFX, vfxInfo.name);
            if (prefabObj != null)
            {
                GameObject vfxObject = UnityEngine.Object.Instantiate(prefabObj.assetObject) as GameObject;
                poolInfo.prefab = vfxObject;
            }
            poolInfo.prefab = null;
            poolInfo.poolSize = 0;
            poolInfo.fixedSize = false;
        }
        fxObjectPool.poolInfo = poolInfoList;
        fxObjectPool.gameObject.SetActive(true);
        result = true;
        return result;
    }

    /**
     * 根据初始化的各个特效的名称加载对应的prefab（用于后续对象的实例化）。
     * 加载后对象池准备激活。
     * TODO: 不需要这个方法了。
     */
    public void Build(UnityEngine.Object prefab) {
        for (int i = 0; i < vfxInfoList.Length; ++i)
        {
            VFXInfo vfxInfo = vfxInfoList[i];
            ResourceObject prefabObj = ResourceSystem.Instance.GetObject(ResourceSystem.Tag.VFX, vfxInfo.name);
            if (prefabObj != null)
            {
                GameObject vfxObject = UnityEngine.Object.Instantiate(prefabObj.assetObject) as GameObject;
                fxObjectPool.poolInfo[i].prefab = vfxObject;
            }
        }
        fxObjectPool.gameObject.SetActive(true);
    }

    /**
     * 播放一个指定名字的特效。
     * @param vfxName: 效果名称。
     * @param delayTime: 延迟播放时间。
     * @param duration: 播放持续时间。如果是0， 则表示循环播放。
     * @param pos: 播放位置。
     * @param binder: 效果绑定对象。
     * @param flip: 是否翻转。
     */
    public string PlayCommonVFX(string vfxName, float delayTime, float duration, Vector3 pos, Transform binder = null, bool flip = false)
    {
        globalIndex = globalIndex++ % 100000;
        string fxID = vfxName + globalIndex.ToString(); // vfxName + globalIndex
        GameObject fxObject = fxObjectPool.GetObjectFromPool(vfxName, Vector3.zero, Quaternion.identity);
        CommonFX fx = fxObject.GetComponent<FXBase>() as CommonFX;
        activeFXObjectMap[fxID] = fxObject;
        fx.Init(delayTime, duration, pos, binder, flip);
        fx.Play();
        return fxID;
    }

    public void RemoveCommonVFX(string fxID)
    {
        if (activeFXObjectMap.ContainsKey(fxID))
        {
            GameObject fxObject = activeFXObjectMap[fxID];
            FXBase fxb = fxObject.GetComponent<FXBase>();
            fxb.Stop();
            activeFXObjectMap.Remove(fxID);
            fxObjectPool.ReturnObjectToPool(fxObject);
        }
        else
        {
            Debug.Log("No such key: " + fxID);
        }
    }

    /**
     * 播放数字（文本）上升的特效
     */
    public void PlayDigitRaisedVFX(int num, Vector3 pos, bool showBg, float fontSize, float angle, float duration)
    {
    }

    //TODO: 播放文字特效，将来需要的时候制作。
    public void PlayTextVFX(string text)
    {
    }
}

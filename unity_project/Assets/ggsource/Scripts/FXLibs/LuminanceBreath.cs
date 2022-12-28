// author : Xie Haolan 2020/2/10

using UnityEngine;

/// <summary>
/// 物体（材质）亮度呼吸效果。 
/// </summary>
public class LuminanceBreath : MonoBehaviour
{
    public Material glowMat;
    public float breathSpeed = 1f;

    void Update()
    {
        glowMat.SetFloat("_Luminance", (Mathf.Sin(Time.time * breathSpeed) + 1f) * 0.5f);
    }
}

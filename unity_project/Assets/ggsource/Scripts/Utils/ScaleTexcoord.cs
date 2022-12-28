using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UISprite))]
public class ScaleTexcoord : MonoBehaviour
{
    private float widthRate;
    private float heightRate;
    private float xOffsetRate;
    private float yOffsetRate;
    private UISprite sprite;
 
    void Awake()
    {
        sprite = GetComponent<UISprite>();
        SetScaleTexcoord();
    }

    public void SetScaleTexcoord()
    {
        widthRate = sprite.GetAtlasSprite().width * 1.0f / sprite.atlas.spriteMaterial.mainTexture.width;
        heightRate = sprite.GetAtlasSprite().height * 1.0f / sprite.atlas.spriteMaterial.mainTexture.height;
        xOffsetRate = sprite.GetAtlasSprite().x * 1.0f / sprite.atlas.spriteMaterial.mainTexture.width;
        yOffsetRate = (sprite.atlas.spriteMaterial.mainTexture.height-(sprite.GetAtlasSprite().y + sprite.GetAtlasSprite().height)) * 1.0f / sprite.atlas.spriteMaterial.mainTexture.height;
        
        sprite.material.SetFloat("_WidthRate", widthRate);
        sprite.material.SetFloat("_HeightRate", heightRate);
        sprite.material.SetFloat("_XOffset", xOffsetRate);
        sprite.material.SetFloat("_YOffset", yOffsetRate);
    }
}

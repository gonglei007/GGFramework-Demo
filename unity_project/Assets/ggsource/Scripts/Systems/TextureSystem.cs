using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using LuaInterface;

/// <summary>
/// 图片信息单元
/// </summary>
public class TextureInfo
{
    public string Group;

    public int RefCount;
    public string TextureName;

    public void AddRefCount()
    {
        RefCount++;
    }

    public void ReduceRefCount()
    {
        RefCount--;
    }
}

public class SpriteData : TextureInfo
{
    public Sprite MainSprite;
    public SpriteData(string group, Sprite sprite, string spriteName)
    {
        Group = group;
        MainSprite = sprite;
        TextureName = spriteName;
    }

    public void UnLoad()
    {
        RefCount = 0;
        TextureName = null;
        if (MainSprite != null)
        {
            Resources.UnloadAsset(MainSprite);
            MainSprite = null;
        }
    }
}
public class Texture2DData : TextureInfo
{
    public Texture2D MainTexture;

    public Texture2DData(string group, Texture2D texture, string texName)
    {
        Group = group;
        MainTexture = texture;
        TextureName = texName;
    }

    public void UnLoad()
    {
        RefCount = 0;
        TextureName = null;
        if (MainTexture != null)
        {
            Resources.UnloadAsset(MainTexture);
            MainTexture = null;
        }
    }
}

public class TextureData : TextureInfo
{
    /// <summary>
    /// 主资源
    /// </summary>
    public Texture MainTexture;

    public TextureData(string group, Texture texture, string texName)
    {
        Group = group;
        MainTexture = texture;
        TextureName = texName;
    }

    public void UnLoad()
    {
        RefCount = 0;
        TextureName = null;
        if (MainTexture != null)
        {
            Resources.UnloadAsset(MainTexture);
            MainTexture = null;
        }
    }
}

/// <summary>
/// 管理贴图。
/// 引用计数。
/// </summary>
public class TextureSystem : Singleton<TextureSystem>
{
    //public enum TextureGroup
    //{
    //    Room,//房间里的图片组
    //    MyHero,//房间外的图片组(武将界面)
    //    ItemView,//ItemView中的显示的Texture
    //}
    
    /// <summary>
    /// Texture2D图片缓存,主要使用在UI中
    /// </summary>
    private Dictionary<string, Texture2DData> Texture2DCache = new Dictionary<string, Texture2DData>();
    
    /// <summary>
    /// Texture图片缓存,主要使用在模型贴图的变化上
    /// </summary>
    private Dictionary<string, TextureData> TextureCache = new Dictionary<string, TextureData>();


    /// <summary>
    /// Sprite图片缓存,主要使用在模型贴图的变化上
    /// </summary>
    private Dictionary<string, SpriteData> SpriteCache = new Dictionary<string, SpriteData>();


    /// <summary>
    /// 加载一张Sprite资源
    /// </summary>
    /// <param name="textureName"></param>
    /// <param name="group"></param>
    /// <param name="callback"></param>
    public void LoadSprite(string spriteName, string group, Action<Sprite> callback)
    {
        if (SpriteCache.ContainsKey(spriteName))
        {
            SpriteData spriteData = SpriteCache[spriteName];
            spriteData.AddRefCount();
            if (callback != null)
            {
                callback(spriteData.MainSprite);
            }
        }
        else
        {
            ResourceSystem.Instance.LoadFloatAssetAsync(spriteName, (UnityEngine.Object tex) =>
            {
                if (!SpriteCache.ContainsKey(spriteName))
                {
                    Texture2D loadTexture = (Texture2D)tex;

                    Sprite loadSprite = Sprite.Create(loadTexture, new Rect(0, 0, loadTexture.width, loadTexture.height), new Vector2(0, 0));
                    SpriteData newSpriteData = new SpriteData(group, loadSprite, spriteName);
                    newSpriteData.AddRefCount();
                    SpriteCache.Add(spriteName, newSpriteData);
                    if (callback != null)
                    {
                        callback(newSpriteData.MainSprite);
                    }
                }
                else
                {
                    SpriteData spriteData = SpriteCache[spriteName];
                    spriteData.AddRefCount();
                    if (callback != null)
                    {
                        callback(spriteData.MainSprite);
                    }
                }
            });
        }
    }

    /// <summary>
    /// 加载一张Sprite资源(给lua使用)
    /// </summary>
    /// <param name="textureName"></param>
    /// <param name="group"></param>
    /// <param name="callback"></param>
    public void LoadSpriteX(string spriteName, string group, LuaFunction callback)
    {
        LoadSprite(spriteName, group, (Sprite spr) =>
        {
            if (callback != null)
            {
                callback.Call(spr);
            }
        });
    }


    /// <summary>
    /// 卸载单一的Sprite资源
    /// </summary>
    /// <param name="textureName"></param>
    /// <param name="force"></param>
    public void UnLoadSprite(string spriteName, bool force = false)
    {
        if (!SpriteCache.ContainsKey(spriteName))
            return;
        SpriteData unloadData = SpriteCache[spriteName];
        if (force)
        {
            unloadData.UnLoad();
            SpriteCache.Remove(spriteName);
        }
        else
        {
            unloadData.ReduceRefCount();
            if (unloadData.RefCount <= 0)
            {
                unloadData.UnLoad();
                SpriteCache.Remove(spriteName);
            }
        }
    }


    /// <summary>
    /// 卸载一组Sprite资源
    /// </summary>
    /// <param name="group">组名</param>
    /// <param name="force">是否强制卸载,强制卸载会立即把内存中的图片卸载掉</param>
    public void UnLoadSameGroupSprite(string group, bool force = false)
    {
        SpriteData[] spriteArray = SpriteCache.Values.ToArray();

        for (int i = 0; i < spriteArray.Length; i++)
        {
            SpriteData spriteData = spriteArray[i];
            if (spriteData.Group == group)
            {
                UnLoadSprite(spriteData.TextureName, force);
            }
        }
    }

/// <summary>
/// 加载一张Texture2D资源
/// </summary>
/// <param name="textureName"></param>
/// <param name="group"></param>
/// <param name="callback"></param>
public void LoadTexture2D(string textureName, string group, Action<Texture2D> callback)
 {
        if (Texture2DCache.ContainsKey(textureName))
        {
            Texture2DData loadTex2D = Texture2DCache[textureName];
            loadTex2D.AddRefCount();
            if (callback != null)
            {
                callback(loadTex2D.MainTexture);
            }
        }
        else
        {
            ResourceSystem.Instance.LoadFloatAssetAsync(textureName, (UnityEngine.Object tex) =>
            {
                if (!Texture2DCache.ContainsKey(textureName))
                {
                    Texture2D loadTexture = (Texture2D)tex;
                    Texture2DData newTex2DData = new Texture2DData(group, loadTexture, textureName);
                    newTex2DData.AddRefCount();
                    Texture2DCache.Add(textureName, newTex2DData);
                    if (callback != null)
                    {
                        callback(newTex2DData.MainTexture);
                    }
                }
                else
                {
                    Texture2DData loadTex2D = Texture2DCache[textureName];
                    loadTex2D.AddRefCount();
                    if (callback != null)
                    {
                        callback(loadTex2D.MainTexture);
                    }
                }
            });
        }
    }
    
   
   /// <summary>
   /// 加载一张Texture资源
   /// </summary>
   /// <param name="textureName"></param>
   /// <param name="group"></param>
   /// <param name="callback"></param>
   public void LoadTexture(string textureName, string group, Action<Texture> callback)
   {
       if (TextureCache.ContainsKey(textureName))
       {
           TextureData loadTexData = TextureCache[textureName];
           loadTexData.AddRefCount();
           if (callback != null)
           {
               callback(loadTexData.MainTexture);
           }
       }
       else
       {
           ResourceSystem.Instance.LoadFloatAssetAsync(textureName, (UnityEngine.Object tex) =>
           {
               if (!TextureCache.ContainsKey(textureName))
               {
                   Texture loadTexture = (Texture)tex;
                   TextureData newTexInfo = new TextureData(group, loadTexture, textureName);
                   newTexInfo.AddRefCount();
                   TextureCache.Add(textureName, newTexInfo);
                   if (callback != null)
                   {
                       callback(newTexInfo.MainTexture);
                   }
               }
               else
               {
                   TextureData loadTexData = TextureCache[textureName];
                   loadTexData.AddRefCount();
                   if (callback != null)
                   {
                       callback(loadTexData.MainTexture);
                   }
               }
           });
       }
   }

   /// <summary>
   /// 加载一张Texture2D资源(给lua使用)
   /// </summary>
   /// <param name="textureName"></param>
   /// <param name="group"></param>
   /// <param name="callback"></param>
   public void LoadTexture2DX(string textureName, string group, LuaFunction callback)
   {
       LoadTexture2D(textureName, group, (Texture2D texture) =>
       {
           if (callback != null)
           {
               callback.Call(texture);
           }
       });
   }

   /// <summary>
   /// 加载一张Texture资源(给lua使用)
   /// </summary>
   /// <param name="textureName"></param>
   /// <param name="group"></param>
   /// <param name="callback"></param>
   public void LoadTextureX(string textureName, string group, LuaFunction callback)
   {
       LoadTexture(textureName, group, (Texture texture) =>
       {
           if (callback != null)
           {
               callback.Call(texture);
           }
       });
   }

   /// <summary>
   /// 卸载单一的Texture2D资源
   /// </summary>
   /// <param name="textureName"></param>
   /// <param name="force"></param>
   public void UnLoadTexture2D(string textureName, bool force = false)
   {
       if(!Texture2DCache.ContainsKey(textureName))
           return;
       Texture2DData unLoadTexData = Texture2DCache[textureName];
       if (force)
       {
           unLoadTexData.UnLoad();
           Texture2DCache.Remove(textureName);
       }
       else
       {
           unLoadTexData.ReduceRefCount();
           if (unLoadTexData.RefCount <= 0)
           {
               unLoadTexData.UnLoad();
               Texture2DCache.Remove(textureName);
           }
       }
   }

   /// <summary>
   /// 卸载单一的Texture资源
   /// </summary>
   /// <param name="textureName">是否强制卸载,强制卸载会立即把内存中的图片卸载掉</param>
   public void UnLoadTexture(string textureName, bool force = false)
   {
       if(!TextureCache.ContainsKey(textureName))
           return;
       TextureData unLoadTexData = TextureCache[textureName];
       if (force)
       {
           unLoadTexData.UnLoad();
           TextureCache.Remove(textureName);
       }
       else
       {
           unLoadTexData.ReduceRefCount();
           if (unLoadTexData.RefCount <= 0)
           {
               unLoadTexData.UnLoad();
               TextureCache.Remove(textureName);
           }
       }
   }

   /// <summary>
   /// 卸载一组Texture2D资源
   /// </summary>
   /// <param name="group"></param>
   /// <param name="force"></param>
   public void UnLoadSameGroupTexture2D(string group, bool force = false)
   {
       Texture2DData[] tex2DArray = Texture2DCache.Values.ToArray();

       for (int i = 0; i < tex2DArray.Length; i++)
       {
           Texture2DData curTexData = tex2DArray[i];
           if (curTexData.Group == group)
           {
               UnLoadTexture2D(curTexData.TextureName,force);
           }
       }
   }

   /// <summary>
    /// 卸载一组Texture资源
    /// </summary>
    /// <param name="group">组名</param>
    /// <param name="force">是否强制卸载,强制卸载会立即把内存中的图片卸载掉</param>
   public void UnLoadSameGroupTexture(string group, bool force = false)
   {
       TextureData[] texArray = TextureCache.Values.ToArray();

       for (int i = 0; i < texArray.Length; i++)
       {
           TextureData curTex = texArray[i];
           if (curTex.Group == group)
           {
               UnLoadTexture(curTex.TextureName,force);
           }
       }
   }
}

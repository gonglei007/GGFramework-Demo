using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FXBase : MonoBehaviour
{
    protected Transform binder = null;

    public virtual void Play() { }
    public virtual void Stop() { }
    public virtual void Update()
    {
        //TODO: GL - 根据binder重新运算本对象的transformation.
        if (binder != null)
        {
            this.transform.position = binder.transform.position;
        }
    }
}

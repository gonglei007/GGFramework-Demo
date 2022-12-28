using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PackableBaseClass : MonoBehaviour, IPackable
{
    public virtual void Pack() { }

    public virtual void Unpack() { }
    public virtual void Active() { }
    public virtual void UnActive() { }
    public virtual void Active_1() { }
    public virtual void UnActive_1() { }
    public virtual void Active_2() { }
    public virtual void UnActive_2() { }
    protected Animator _Animator;
    // Start is called before the first frame update
    void Start()
    {
        _Animator = GetComponent<Animator>();
    }
    protected virtual void Update()
    {
        if (Input.GetKeyDown("w"))
            Unpack();
        else if (Input.GetKeyDown("s"))
            Pack();
        else if (Input.GetKeyDown("a"))
            Active();
        else if (Input.GetKeyDown("d"))
            UnActive();
        else if (Input.GetKeyDown("left"))
            Active_1();
        else if (Input.GetKeyDown("right"))
            UnActive_1();
        else if (Input.GetKeyDown("up"))
            Active_2();
        else if (Input.GetKeyDown("down"))
            UnActive_2();
    }
}

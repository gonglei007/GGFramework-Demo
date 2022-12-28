using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Packable
{
    public float fPackTime;
    public float fUnpackTime;
    public PackableBaseClass _Packable;
}
public class PackInfo : PackableBaseClass
{
    [SerializeField]
    private List<Packable> listPackable;

    private Coroutine _Coroutine;
    private int _CurPlayingID = 0;
    public override void Pack()
    {
        if (_Coroutine != null)
            StopCoroutine(_Coroutine);
        _Coroutine = StartCoroutine("PackRoutine");
    }
    IEnumerator UnPackRoutine()
    {
        for(; _CurPlayingID < listPackable.Count; _CurPlayingID++)
        {
            listPackable[_CurPlayingID]._Packable.Unpack();
            yield return new WaitForSeconds(listPackable[_CurPlayingID].fUnpackTime);
        }
        //if (_CurPlayingID >= listPackable.Count) _CurPlayingID = listPackable.Count - 1; 
    }
    public override void Unpack()
    {
        if(_Coroutine != null)
            StopCoroutine(_Coroutine);
        _Coroutine = StartCoroutine("UnPackRoutine");
    }
    IEnumerator PackRoutine()
    {
        for (; --_CurPlayingID >= 0; )
        {
            listPackable[_CurPlayingID]._Packable.Pack();
            yield return new WaitForSeconds(listPackable[_CurPlayingID].fPackTime);
        }
        if (_CurPlayingID < 0) _CurPlayingID = 0;
    }

}

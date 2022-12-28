using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonEventTest : MonoBehaviour
{
    public GameObject go;
    // Start is called before the first frame update
    void Start()
    {
        UIEventListener.Get(go).onClick += OnButtonClickActive;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnButtonClickActive(GameObject go)
    {
        Debug.LogWarning("!!!Button on click!!!");
    }
}

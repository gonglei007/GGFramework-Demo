using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    private Vector3 position;
    private Vector3 lastPosition;
    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 acceleration;
    private float time;
    private float lastTime;
    private bool isActive = true;

    public Vector3 Acceleration {
        get {
            return acceleration;
        }
    }

    public bool IsActive {
        get {
            return isActive;
        }
        set {
            isActive = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!this.IsActive) {
            return;
        }
        lastTime = time;
        time = Time.deltaTime;
        bool isValid = UpdatePosition(this.gameObject.transform.position);
        if (isValid)
        {
            UpdateRotationFX();
        }
    }

    void OnGUI()
    {
        return;
        Rect posRect = new Rect(new Vector2(10, 10), new Vector2(300, 40));
        Rect velRect = new Rect(new Vector2(10, 50), new Vector2(300, 40));
        Rect accRect = new Rect(new Vector2(10, 90), new Vector2(300, 40));
        Rect objRect = new Rect(new Vector2(10, 130), new Vector2(300, 40));
        GUI.TextField(posRect, "Position:"+position.ToString());
        GUI.TextField(velRect, "Velocity:" + velocity.ToString());
        GUI.TextField(accRect, "Acceleration:" + acceleration.ToString());
        GUI.TextField(objRect, "ObjectRotation:" + this.gameObject.transform.eulerAngles.ToString());
    }

    /**
     * 更新坐标点
     */
    public bool UpdatePosition(Vector3 position) {
        lastPosition = this.position;
        this.position = position;
        float tempTime = (time - lastTime);
        if (tempTime == 0)
        {
            return false;
        }
        this.velocity = this.ComputeVelocity(tempTime);

        this.acceleration = this.ComputeAcceleration(tempTime);

        return true;
    }

    public void UpdateRotationFX()
    {
        var angles = gameObject.transform.localEulerAngles;
        angles.Set(180f, 0f, 0f);
        Vector3 accNormal = acceleration.normalized;
        const float maxAngle = 15f;
        float ratationX = maxAngle * accNormal.y;
        float ratationY = maxAngle * -accNormal.x;
       
        Vector3 ratation = new Vector3(ratationX, ratationY, 0f);
        this.gameObject.transform.localEulerAngles = ratation;
        //this.gameObject.transform.eulerAngles = Quaternion.Euler(180f, 0f, 0f).eulerAngles;// new Vector3(180f, 0f, 0f);
        return;
        //Quaternion rotation = new Quaternion();
        Vector3 rotation = new Vector3();
        rotation.Set(180f, 0f, 0f);

        this.gameObject.transform.eulerAngles = rotation;// Quaternion.Euler(180, 0, 0);
    }

    private Vector3 ComputeVelocity(float time)
    {
        Vector3 velocity = (position - lastPosition) / time;
        return velocity;
    }

    private Vector3 ComputeAcceleration(float time)
    {
        Vector3 acceleration = (velocity - lastVelocity) / time;
        return acceleration;
    }

}

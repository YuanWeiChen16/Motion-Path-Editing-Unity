using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class cameraMove : MonoBehaviour
{
    public GameObject Cam;

    public GameObject Berserker;

    public int mode = 0;//0 =>tracking //1 =>Free

    public float R = 0.5f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {



        //move tracking

        Vector3 CP = Cam.GetComponent<Transform>().position;
        Vector3 BP = Berserker.GetComponent<Transform>().position;
        Vector3 Temp = new Vector3(0, 0, 0);
        float ARC = R *2* 3.1415926f;
        Temp.x = Mathf.Cos(ARC) - Mathf.Sin(ARC);
        Temp.z = Mathf.Sin(ARC) + Mathf.Cos(ARC);
        Temp *= 5;
        Temp.y = 1;
        Cam.GetComponent<Transform>().position = BP + Temp;

        //eye tracking
        Cam.GetComponent<Transform>().LookAt(Berserker.GetComponent<Transform>().position, new Vector3(0, 1, 0));

    }


    public void ModeTOTracking()
    {


    }

    public void ModeTOFollow()
    {



    }

    public void MoveSlider(float a)
    {
        R = a;
    }

}

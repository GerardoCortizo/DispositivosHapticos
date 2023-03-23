﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using TMPro;

public class HapticManager : MonoBehaviour {
    private const float k = 9000000000; // N*m2/C2
    public float dist;
    public float force;

    private Transform start;
    private Transform end;
    public float maxWidth;
    public float minWidth;

    public LineRenderer lineRenderer;
    Vector3[] newPositions = new Vector3[2];

    public GameObject arrow_0;
    public GameObject arrow_1;
    private Transform objTransform_arrow0;
    private Transform objTransform_arrow1;

    public TextMeshProUGUI FText;

    // plugin import
    private IntPtr myHapticPlugin;
    // haptic thread
    private Thread myHapticThread;
    // a flag to indicate if the haptic simulation currently running
    private bool hapticThreadIsRunning;
    // haptic devices in the scene
    public GameObject[] hapticCursors;
    HapticInteractionPoint[] myHIP = new HapticInteractionPoint[16];

    // haptic workspace
    public float workspace = 100.0f;
    // number of haptic devices detected
    private int hapticDevices;
    // position [m] of each haptic device
    private Vector3[] position = new Vector3[16];
    private Vector3[] force_vec = new Vector3[2];
    private Vector3[] unit_vec = new Vector3[2];
    private Quaternion[] orientation = new Quaternion[16];
    // state of haptic device buttons
    private bool[] button0 = new bool[16];
    private bool[] button1 = new bool[16];
    private bool[] button2 = new bool[16];
    private bool[] button3 = new bool[16];

    // Use this for initialization
    void Start () {
        arrow_0 = GameObject.Find("Arrow0");
        arrow_1 = GameObject.Find("Arrow1");
        objTransform_arrow0 = arrow_0.transform;
        objTransform_arrow1 = arrow_1.transform;

        Debug.Log("Starting Haptic Devices");
        myHapticPlugin = HapticPluginImport.CreateHapticDevices();
        hapticDevices = HapticPluginImport.GetHapticsDetected(myHapticPlugin);
        if (hapticDevices > 0)
        {
            Debug.Log("Haptic Devices Found: " + HapticPluginImport.GetHapticsDetected(myHapticPlugin).ToString());
            Debug.Log("Arreglo: " + hapticDevices.ToString());
            for (int i = 0; i < hapticDevices; i++)
            {
                myHIP[i] = (HapticInteractionPoint)hapticCursors[i].GetComponent(typeof(HapticInteractionPoint));
            }
        }
        else
        {
            Debug.Log("Haptic Devices cannot be found");
            Application.Quit();
        }
        hapticThreadIsRunning = true;
        myHapticThread = new Thread(HapticThread);
        myHapticThread.Priority = System.Threading.ThreadPriority.Highest;
        myHapticThread.Start();
    }

    // Update is called once per frame
    void Update () {
        FText.text = "Fuerza: " + Math.Round(force, 2).ToString() + " N";

        float scalar = 1.0f;
        if (myHIP[0].charge * myHIP[1].charge < 0)
        {
            scalar = -1.0f;

        }
        else
        {
            scalar = 1.0f;
        }

        Vector3 newScale0 = new Vector3(Math.Abs(objTransform_arrow0.localScale.x) * scalar, objTransform_arrow0.localScale.y, objTransform_arrow0.localScale.z);
        Vector3 newScale1 = new Vector3(Math.Abs(objTransform_arrow1.localScale.x) * scalar, objTransform_arrow1.localScale.y, objTransform_arrow1.localScale.z);

        objTransform_arrow0.localScale = newScale0;
        objTransform_arrow1.localScale = newScale1;

        force_vec[0] = new Vector3(myHIP[0].position.x - myHIP[1].position.x, myHIP[0].position.y - myHIP[1].position.y, myHIP[0].position.z - myHIP[1].position.z);
        force_vec[1] = new Vector3(myHIP[1].position.x - myHIP[0].position.x, myHIP[1].position.y - myHIP[0].position.y, myHIP[1].position.z - myHIP[0].position.z);

        Vector3 mH0P = myHIP[1].position;
        Vector3 mH1P = myHIP[0].position;
        float normal0 = Mathf.Pow(((Mathf.Pow(mH0P.x, 2) + Mathf.Pow(mH0P.y, 2) + Mathf.Pow(mH0P.z, 2))), 0.5f);
        unit_vec[0] = new Vector3((mH0P.x / normal0), (mH0P.y / normal0), (mH0P.z / normal0)); 

        newPositions[0] = myHIP[0].position; // First point at (0, 0, 0)
        //newPositions[1] = unit_vec[0]; // Second point at (1, 1, 1)
        newPositions[1] = myHIP[1].position;

        lineRenderer.SetPositions(newPositions);
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    // OnDestroy is called when closing application
    void OnDestroy() {
        // close haptic thread
        EndHapticThread();
        // delete haptic plugin
        HapticPluginImport.DeleteHapticDevices(myHapticPlugin);
        Debug.Log("Application ended correctly");
    }

    // Thread for haptic device handling
    void HapticThread() {

        while (hapticThreadIsRunning)
        {
            for (int i = 0; i < hapticDevices; i++)
            {
                // get haptic positions and convert them into scene positions
                position[i] = workspace * HapticPluginImport.GetHapticsPositions(myHapticPlugin, i);
                orientation[i] = HapticPluginImport.GetHapticsOrientations(myHapticPlugin, i);

                // get haptic buttons
                button0[i] = HapticPluginImport.GetHapticsButtons(myHapticPlugin, i, 1);
                button1[i] = HapticPluginImport.GetHapticsButtons(myHapticPlugin, i, 2);
                button2[i] = HapticPluginImport.GetHapticsButtons(myHapticPlugin, i, 3);
                button3[i] = HapticPluginImport.GetHapticsButtons(myHapticPlugin, i, 4);

                // calculate distance to sphere
                Vector3 hapticVec = myHIP[i].position;

                if (button3[i] && myHIP[i].charge < 3)
                {
                    myHIP[i].charge += 0.001f;
                }
                if (button1[i] && myHIP[i].charge > -3)
                {
                    myHIP[i].charge -= 0.001f;
                }
            }
            

            dist = Vector3.Distance(myHIP[0].position, myHIP[1].position);
            force = k * myHIP[0].charge * myHIP[1].charge / (2 * dist * dist * 1000000);
            if (dist < 250)
            {
                force = 0;
            }

            Vector3 interaction_1 = force_vec[0];
            interaction_1 = force * interaction_1;

            Vector3 interaction_2 = force_vec[1];
            interaction_2 = force * interaction_2;
            
            
            if (dist > 150)
            {
                HapticPluginImport.SetHapticsForce(myHapticPlugin, 0, interaction_1);
                HapticPluginImport.SetHapticsForce(myHapticPlugin, 1, interaction_2);
            }


            HapticPluginImport.UpdateHapticDevices(myHapticPlugin, 0);
            HapticPluginImport.UpdateHapticDevices(myHapticPlugin, 1);

        }
    }

    // Closes the thread that was created
    void EndHapticThread()
    {
        hapticThreadIsRunning = false;
        Thread.Sleep(100);

        // variables for checking if thread hangs
        bool isHung = false; // could possibely be hung during shutdown
        int timepassed = 0;  // how much time has passed in milliseconds
        int maxwait = 10000; // 10 seconds
        Debug.Log("Shutting down Haptic Thread");
        try
        {
            // loop until haptic thread is finished
            while (myHapticThread.IsAlive && timepassed <= maxwait)
            {
                Thread.Sleep(10);
                timepassed += 10;
            }

            if (timepassed >= maxwait)
            {
                isHung = true;
            }
            // Unity tries to end all threads associated or attached
            // to the parent threading model, if this happens, the 
            // created one is already stopped; therefore, if we try to 
            // abort a thread that is stopped, it will throw a mono error.
            if (isHung)
            {
                Debug.Log("Haptic Thread is hung, checking IsLive State");
                if (myHapticThread.IsAlive)
                {
                    Debug.Log("Haptic Thread object IsLive, forcing Abort mode");
                    myHapticThread.Abort();
                }
            }
            Debug.Log("Shutdown of Haptic Thread completed.");
        }
        catch (Exception e)
        {
            // lets let the user know the error, Unity will end normally
            Debug.Log("ERROR during OnApplicationQuit: " + e.ToString());
        }
    }

    public int GetHapticDevicesFound()
    {
        return hapticDevices;
    }

    public Vector3 GetPosition(int numHapDev)
    {
        return position[numHapDev];
    }

    public Quaternion GetOrientation(int numHapDev)
    {
        return orientation[numHapDev];
    }

    public bool GetButtonState(int numHapDev, int button)
    {
        bool temp;
        switch (button)
        {
            case 1:
                temp = button1[numHapDev];
                break;
            case 2:
                temp = button2[numHapDev];
                break;
            case 3:
                temp = button3[numHapDev];
                break;
            default:
                temp = button0[numHapDev];
                break;
        }
        return temp;
    }

    public float GetHapticDeviceInfo(int numHapDev, int parameter)
    {
        // Haptic info variables
        // 0 - m_maxLinearForce
        // 1 - m_maxAngularTorque
        // 2 - m_maxGripperForce 
        // 3 - m_maxLinearStiffness
        // 4 - m_maxAngularStiffness
        // 5 - m_maxGripperLinearStiffness;
        // 6 - m_maxLinearDamping
        // 7 - m_maxAngularDamping
        // 8 - m_maxGripperAngularDamping

        float temp;
        switch (parameter)
        {
            case 1:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 1);
                break;
            case 2:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 2);
                break;
            case 3:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 3);
                break;
            case 4:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 4);
                break;
            case 5:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 5);
                break;
            case 6:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 6);
                break;
            case 7:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 7);
                break;
            case 8:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 8);
                break;
            default:
                temp = (float)HapticPluginImport.GetHapticsDeviceInfo(myHapticPlugin, numHapDev, 0);
                break;
        }
        return temp;
    }
}

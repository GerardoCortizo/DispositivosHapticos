﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class HapticManager : MonoBehaviour {
    // plugin import
    private IntPtr myHapticPlugin;
    // haptic thread
    private Thread myHapticThread;
    // a flag to indicate if the haptic simulation currently running
    private bool hapticThreadIsRunning;
    // haptic devices in the scene
    public GameObject[] hapticCursors;
    HapticInteractionPoint[] myHIP = new HapticInteractionPoint[16];
    public SphereManager mySphere;

    // haptic workspace
    public float workspace = 100.0f;
    // number of haptic devices detected
    private int hapticDevices;
    // position [m] of each haptic device
    private Vector3[] position = new Vector3[16];
    private Quaternion[] orientation = new Quaternion[16];
    // state of haptic device buttons
    private bool[] button0 = new bool[16];
    private bool[] button1 = new bool[16];
    private bool[] button2 = new bool[16];
    private bool[] button3 = new bool[16];
    private bool attract = true;

    // Use this for initialization
    void Start () {
        // inizialization of Haptic Plugin
        Debug.Log("Starting Haptic Devices");
        // check if haptic devices libraries were loaded
        myHapticPlugin = HapticPluginImport.CreateHapticDevices();
        hapticDevices = HapticPluginImport.GetHapticsDetected(myHapticPlugin);
        if (hapticDevices > 0)
        {
            Debug.Log("Haptic Devices Found: " + HapticPluginImport.GetHapticsDetected(myHapticPlugin).ToString());
            for (int i = 0; i < hapticDevices; i++)
            {
                myHIP[i] = (HapticInteractionPoint)hapticCursors[i].GetComponent(typeof(HapticInteractionPoint));
            }
            mySphere = (SphereManager)GetComponent(typeof(SphereManager));
        }
        else
        {
            Debug.Log("Haptic Devices cannot be found");
            Application.Quit();
        }
        // setting the haptic thread
        hapticThreadIsRunning = true;
        myHapticThread = new Thread(HapticThread);
        // set priority of haptic thread
        myHapticThread.Priority = System.Threading.ThreadPriority.Highest;
        // starting the haptic thread
        myHapticThread.Start();
    }

    // Update is called once per frame
    void Update () {
        // Exit application
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
                Vector3 sphereVec = myHIP[i].spherePosition;
                Vector3 hap2sph;

                if (button3[i])
                {
                    myHIP[i].charge += 0.001f;
                }
                if (button1[i])
                {
                    myHIP[i].charge -= 0.001f;
                }

                hap2sph = new Vector3(sphereVec.x - hapticVec.x, sphereVec.y - hapticVec.y, sphereVec.z - hapticVec.z);

                // Toggling vector direction
                if (myHIP[i].charge < 0)
                {
                    hap2sph = hap2sph * -1;
                }

                // Managing attraction or repulsion
                Vector3 interaction = hap2sph;
                interaction = myHIP[i].charge * interaction;
                
                if(Vector3.Distance(hapticVec, sphereVec) >= 0.0)
                {
                    HapticPluginImport.SetHapticsForce(myHapticPlugin, i, interaction);
                }




                // Drawing ray
                Debug.DrawRay(hapticVec,hap2sph, Color.green, 0.02f, false);

                HapticPluginImport.UpdateHapticDevices(myHapticPlugin, i);
            }
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

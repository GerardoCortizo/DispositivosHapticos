using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HapticInteractionPoint : MonoBehaviour {

    private const float k = 9000000000; // N*m2/C2

    public GameObject mySphere;
    public float distHapticSphere;
    public float force;

    // establish Haptic Manager and IHIP objects
    public GameObject hapticManager;
    public GameObject IHIP;
    public Text posText;
    public Text rotText;
    public Text distText;

    // get haptic device information from the haptic manager
    private HapticManager myHapticManager;
    
    // haptic device number
    public int hapticDevice;
    // haptic device variables
    public Vector3 position;
    public Vector3 spherePosition;
    private Quaternion orientation;
    private bool button0;
    private bool button1;
    private bool button2;
    private bool button3;
    public float charge; // C
    public float mass;
    private Material material;
    private Rigidbody rigidBody;

    // Called when the script instance is being loaded
    void Awake() {
        position = new Vector3(0, 0, 0);
        spherePosition = new Vector3(0, 0, 0);
        button0 = false;
        button1 = false;
        button2 = false;
        button3 = false;
        material = IHIP.GetComponent<Renderer>().material;
        rigidBody = GetComponent<Rigidbody>();
    }

    // Use this for initialization
    void Start () {
        myHapticManager = (HapticManager)hapticManager.GetComponent(typeof(HapticManager));
	}
	
	// Update is called once per frame
	void Update () {

        // get haptic device to be used
        int hapticsFound = myHapticManager.GetHapticDevicesFound();
        hapticDevice = (hapticDevice > -1 && hapticDevice < hapticsFound) ? hapticDevice : hapticsFound - 1;

        // get haptic device variables
        position = myHapticManager.GetPosition(hapticDevice);
        spherePosition = new Vector3(0, 0, 0);
        posText.text = "Position: " + position.ToString();
        orientation = myHapticManager.GetOrientation(hapticDevice);
        button0 = myHapticManager.GetButtonState(hapticDevice, 0);
        button1 = myHapticManager.GetButtonState(hapticDevice, 1);
        button2 = myHapticManager.GetButtonState(hapticDevice, 2);
        button3 = myHapticManager.GetButtonState(hapticDevice, 3);

        // update haptic device mass
        mass = (mass > 0) ? mass : 0.0f;
        rigidBody.mass = mass;

        // calculate distance to sphere
        distHapticSphere = Vector3.Distance(position, mySphere.transform.position);
        rotText.text = "Distance: " + distHapticSphere.ToString();

        // calculating force
        force = k * (charge * mySphere.GetComponent<Rigidbody>().mass) / (distHapticSphere * distHapticSphere);
        distText.text = "Force: " + force.ToString();

        // update positions of HIP and IHIP
        IHIP.transform.position = position;
        IHIP.transform.rotation = orientation;
        transform.position = position;
        transform.rotation = orientation;

        // change material color
        if (charge < 0)
        {
            material.color = Color.red;
        }
        else
        {
            material.color = Color.blue;
        }
    }
}


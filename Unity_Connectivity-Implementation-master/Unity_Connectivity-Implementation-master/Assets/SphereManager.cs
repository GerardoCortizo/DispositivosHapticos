using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereManager : MonoBehaviour
{ 
    public Vector3 position;
    public float charge;

    private void Start()
    {
        position = new Vector3(2, 0, 2);
        charge = 0.5f;
    }
}

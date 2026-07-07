using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    private Camera vrCamera;

void Start()
    {
        vrCamera = Camera.main;
    }

void Update()
{
    var trans = vrCamera.transform;
    Vector3 targetDirection = transform.position - trans.position;
        transform.rotation = Quaternion.LookRotation(targetDirection);
}
}

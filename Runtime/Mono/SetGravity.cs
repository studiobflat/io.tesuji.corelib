using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetGravity : MonoBehaviour
{
    public Vector3 gravity;
    private void Start()
    {
        Physics.gravity = gravity;
        Debug.Log($"Set Gravity: {gravity}");
    }
}

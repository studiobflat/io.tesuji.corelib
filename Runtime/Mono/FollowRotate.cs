using System;
using UnityEngine;

public class FollowRotate : MonoBehaviour
{
    public Transform target;
    public Transform source;
    public Quaternion offset = Quaternion.identity;
    public bool useLocal = false;
    public int updateOrder = 0;

    public bool lockY;
    public bool lockX;
    public bool lockZ;
    
    private void Awake()
    {
        Tesuji.TesujiUpdateManager.OnLateUpdate(LateUpdate2, updateOrder);
    }
    
    void LateUpdate2()
    {
        var myRot = (useLocal ? target.localRotation : target.rotation).eulerAngles;
        var targetRot = (useLocal ? source.localRotation : source.rotation).eulerAngles;

        if (lockX) targetRot.x = myRot.x;
        if (lockY) targetRot.y = myRot.y;
        if (lockZ) targetRot.z = myRot.z;

        if (useLocal)
        {
            target.localRotation = Quaternion.Euler(targetRot);
        }
        else
        {
            target.rotation = Quaternion.Euler(targetRot);
        }
    }
}
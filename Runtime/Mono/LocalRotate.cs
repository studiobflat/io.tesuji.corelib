using UnityEngine;

public class LocalRotate : MonoBehaviour
{
    public Vector3 speed;
    private Quaternion localRot = Quaternion.identity;
    
    void Update()
    {
        localRot *= Quaternion.Euler(speed.x, speed.y, speed.z);
        transform.localRotation = localRot;
    }
}

using System;
using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform target;
    public Transform source;
    public Vector3 offset;
    public bool useLocal = false;
    public int updateOrder = 0;
    [Range(0.1f, 1f)] public float easing = 0.1f;
    
    [Button] void ReadOffset()
    {
        if (useLocal)
        {
            offset = target.localPosition - source.localPosition;
        }
        else
        {
            offset = target.position - source.position;
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    private void Awake()
    {
        Tesuji.TesujiUpdateManager.OnLateUpdate(LateUpdate2, updateOrder);
    }

    private void OnDestroy()
    {
        Tesuji.TesujiUpdateManager.RemoveLateUpdate(LateUpdate2);
    }

    void LateUpdate2()
    {
        if (this == null || source == null || target == null) return;
        
        if (useLocal)
        {
            Vector3 tPos = source.localPosition + offset;
            target.localPosition = Vector3.Lerp(target.localPosition, tPos, easing);
        }
        else
        {
            Vector3 tPos = source.position + offset;
            target.position = Vector3.Lerp(target.position, tPos, easing);
        }
    }
}

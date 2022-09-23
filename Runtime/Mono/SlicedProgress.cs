using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlicedProgress : MonoBehaviour
{
    public RectTransform fill;
    
    [Range(0f, 1f)] public float progress;
    public float min = 0.1f;
    public bool isHorz = false;
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        Refresh();
    }
    #endif

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        var p = Mathf.Max(min, progress);
        fill.anchorMax = isHorz ? new Vector2(p, 1) : new Vector2(1, p);
    }
    
    // Compatible with image
    public float fillAmount
    {
        get => progress;
        set
        {
            progress = Mathf.Clamp01(value);
            Refresh();
        }
    }
}

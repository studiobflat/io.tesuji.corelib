using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TweenAlpha : MonoBehaviour
{
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    public Graphic target;
    public float speed = 10f;
    public bool blink = true;

    void Update()
    {
        if (!blink) return;
        Color c = target.color;
        c.a = Mathf.Abs(Mathf.Sin(speed * Time.time)) * (maxAlpha - minAlpha) + minAlpha;
        target.color = c;
    }

}

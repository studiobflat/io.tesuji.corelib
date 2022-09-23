using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public float press_scale = 0.9f;

	private Vector3 scale;

	public UnityEvent onHold;
	public UnityEvent onRelease;

	void Awake()
	{
		scale = transform.localScale;
	}
	
	public void OnPointerDown(PointerEventData eventData)
	{
		transform.localScale = scale * press_scale;
		if (onHold != null) onHold.Invoke();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		transform.localScale = scale;
		if (onRelease != null) onRelease.Invoke();
	}
}

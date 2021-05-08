using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIThrustBar : MonoBehaviour
{
	public RectTransform bar;
	public Transform pointer;
	private Vector2 startSize;
	
	public SSFlight flight;
	
	private float prevThrust = -1;
	private float prevLinearThrust = -1;
	
	private Vector3 ptrInitPos;
	private const float ptrLength = 220;

	void Start()
	{
		startSize = bar.sizeDelta;
		ptrInitPos = pointer.localPosition;
	}

	void Update()
	{
		if (Globals.paused) return;

		// if thrust changed, set pointer to new position
		if (prevThrust != flight.thrust)
		{
			prevThrust = Mathf.Clamp01(flight.thrust);
			pointer.localPosition = ptrInitPos + Vector3.up * ptrLength * prevThrust;
		}
		// if linear thrust changed, set bar size to new width
		if (prevLinearThrust != flight.linearThrust)
		{
			prevLinearThrust = flight.linearThrust;
			var sd = startSize;
			sd.y *= prevLinearThrust;
			bar.sizeDelta = sd;
		}
	}
}

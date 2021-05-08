using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIShieldBar : MonoBehaviour
{
	public Image barIm;
	public RectTransform bar;

	private Color defCol;
	public Color rechargeCol;

	public SSShield shield;

	private float prevShield = -1f;
	private Vector2 startSize;

	void Start()
	{
		defCol = barIm.color;
		startSize = bar.sizeDelta;
	}

	void Update()
	{
		if (Globals.paused) return;

		var s = 1f;
		// if shield active, set width to shield time left
		if (shield.Timer > 0) s = shield.Timer / SSShield.duration;
		// if shield recharging, set width to recharge progress percentage
		else if (shield.Delay > 0) s = 1f - shield.Delay / SSShield.rechargeDur;
		// update bar width
		if (prevShield != s)
		{
			prevShield = s;
			var sd = startSize;
			sd.x *= s;
			bar.sizeDelta = sd;
		}
		// update bar color
		barIm.color = shield.Delay > 0 && shield.Timer <= 0 ? rechargeCol : defCol;
	}
}

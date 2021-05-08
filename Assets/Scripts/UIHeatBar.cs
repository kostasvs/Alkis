using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHeatBar : MonoBehaviour
{
	public Image barIm;
	public RectTransform bar;

	private Color defCol;
	public Color overheatCol;

	public SSLaser laser;
	private float prevHeat = 0f;

	private Vector2 startSize;

	public float lerp = .1f;
	public float minChange = .01f;

	private float curHeat;

	private CanvasGroup cg;
	private Canvas canvas;
	private float fader;
	public float fadeDur = .25f;
	private AudioSource au;
	private bool prevOverheat = false;

	void Start()
	{
		defCol = barIm.color;
		startSize = bar.sizeDelta;

		cg = GetComponent<CanvasGroup>();
		canvas = GetComponent<Canvas>();
		au = GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		curHeat = laser.heatLevel;
	}

	void Update()
	{
		if (Globals.paused) return;

		// move curHeat towards laser's heatLevel
		if (curHeat != laser.heatLevel) curHeat = Mathf.MoveTowards(curHeat, laser.heatLevel,
			Mathf.Min(lerp * Mathf.Abs(laser.heatLevel - curHeat), minChange));
		
		// update bar width
		if (prevHeat != curHeat)
		{
			prevHeat = curHeat;
			var sd = startSize;
			sd.x *= curHeat;
			bar.sizeDelta = sd;
		}
		// update bar color depending on whether we are in overheat
		barIm.color = laser.inOverheat ? overheatCol : defCol;
		if (prevOverheat != laser.inOverheat)
		{
			// play overheat sound
			if (!prevOverheat) au.Play();
			prevOverheat = laser.inOverheat;
		}

		// fade out if heat is zero
		var prevFade = fader;
		fader = Mathf.Clamp01(fader + (laser.heatLevel == 0f ? -1f : 1f) * Time.deltaTime / fadeDur);
		if (prevFade != fader)
		{
			cg.alpha = fader;
			canvas.enabled = fader > 0;
		}
	}
}

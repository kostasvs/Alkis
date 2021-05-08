using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
	[Tooltip("normal healthbar")]
	public RectTransform bar;

	[Tooltip("previous-value healthbar (behind current one)")]
	public RectTransform lagBar;

	[Tooltip("previous-value lag, in full-widths/sec")]
	public float lagSpeed = 2f;
	private float lagHealth;
	private bool lagActive = false;

	public SSDeathAndSpawn ss;
	private float prevHealth = -1f;
	private Vector2 startSize;

	void Start()
	{
		startSize = bar.sizeDelta;
	}

	void Update()
	{
		if (Globals.paused) return;

		// check for health change
		if (prevHealth != ss.health)
		{
			prevHealth = ss.health;
			var sd = startSize;
			sd.x *= ss.health / ss.StartHealth;
			bar.sizeDelta = sd;
		}

		if (lagHealth > prevHealth)
		{
			// enable lag-bar and move it towards current value
			lagHealth = Mathf.MoveTowards(lagHealth, prevHealth, lagSpeed * Time.deltaTime);
			var sd = startSize;
			sd.x *= lagHealth / ss.StartHealth;
			lagBar.sizeDelta = sd;
			if (!lagActive)
			{
				lagBar.gameObject.SetActive(true);
				lagActive = true;
			}
		}
		else
		{
			// health increased, update lag-bar and hide it
			lagHealth = prevHealth;
			if (lagActive)
			{
				lagBar.gameObject.SetActive(false);
				lagActive = false;
			}
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDamageInd : MonoBehaviour
{
	[Tooltip("rectTransform to rotate towards attacker's screen position")]
	public RectTransform pivot;
	public Image indicator;

	private Color startCol;

	public float duration = .7f;
	private float timer;

	public AnimationCurve alphaCurve;

	public CamFollow cam;
	private Transform attacker;

	void Start()
	{
		startCol = indicator.color;
		cam.follow.GetComponent<SSDeathAndSpawn>().AssociateDamageInd(this);
	}

	private void OnDisable()
	{
		timer = 0f;
	}

	void Update()
	{
		if (Globals.paused && !Globals.gameover) return;
		timer -= Time.unscaledDeltaTime;
		if (timer <= 0f)
		{
			// hide
			enabled = false;
			indicator.enabled = false;
			return;
		}
		// fade out
		var c = startCol;
		c.a *= alphaCurve.Evaluate(timer / duration);
		indicator.color = c;

		// point towards attacker
		if (attacker && attacker.gameObject.activeSelf)
		{
			var delta = cam.follow.InverseTransformPoint(attacker.position);
			delta.y = delta.z;
			delta.z = 0f;
			pivot.up = delta;
		}
	}

	/// <summary>
	/// Indicate newly taken damage from given transform.
	/// </summary>
	/// <param name="from">Transform to point towards</param>
	public void TookDamage(Transform from)
	{
		attacker = from;
		timer = duration;
		enabled = true;
		indicator.enabled = true;
	}
}

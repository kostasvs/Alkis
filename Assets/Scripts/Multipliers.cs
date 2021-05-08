using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Multipliers : MonoBehaviour
{

	// reference to this singleton
	public static Multipliers me;

	[Tooltip("point multipliers per team")]
	public readonly int[] muls = new int[] { 1, 1 };
	/// <summary>
	/// kills till next multiplier increment, per team
	/// </summary>
	private readonly int[] killsToNextMul = new int[2];

	[Tooltip("kills required for first multiplier increment")]
	public int initKillsRequired = 50;

	[Tooltip("additional kills required on top of initial, per extra increment")]
	public int extraKillsRequiredPerIncr = 25;

	public Text[] textMuls;
	[Tooltip("text to prepend before multiplier number")]
	public string preText = "<size=25>x</size>";

	private AudioSource[] au;

	private readonly GameObject[] ghost = new GameObject[2];
	private readonly float[] phaseGhost = new float[2];
	private readonly Text[] textGhost = new Text[2];
	private readonly Transform[] trGhost = new Transform[2];
	[Tooltip("duration of expanding ghost effect for multiplier text")]
	public float ghostDur = .5f;
	[Tooltip("max scale of expanding ghost effect for multiplier text")]
	public float ghostScale = 2f;
	[Tooltip("scale curve of expanding ghost effect for multiplier text")]
	public AnimationCurve ghostAlphaCurve;

	void Awake()
	{

		// check for duplicate singleton
		if (me && me != this)
		{
			Debug.LogWarning("duplicate Multipliers");
			enabled = false;
			return;
		}
		me = this;

		// init kills requirements
		for (int i = 0; i < 2; i++) killsToNextMul[i] = initKillsRequired;

		au = GetComponents<AudioSource>();
	}

	private void Update()
	{

		if (Globals.paused) return;

		// get deltaTime, capped at fixedDeltaTime * maxSlowMo
		float dt = Mathf.Max(Time.deltaTime, Time.fixedDeltaTime * Globals.maxSlowMo);
		for (int i = 0; i < 2; i++) if (phaseGhost[i] > 0)
			{
				// process expanding ghost
				phaseGhost[i] -= dt / ghostDur;
				if (phaseGhost[i] <= 0) Destroy(ghost[i]);
				else
				{
					// scale up, fade out
					trGhost[i].localScale = Vector3.one * Mathf.Lerp(ghostScale, 1f, phaseGhost[i]);
					var c = textGhost[i].color;
					c.a = ghostAlphaCurve.Evaluate(phaseGhost[i]);
					textGhost[i].color = c;
				}
			}
	}

	/// <summary>
	/// Reset multipliers
	/// </summary>
	public static void ResetMe()
	{

		if (!me) return;
		for (int i = 0; i < 2; i++)
		{
			me.muls[i] = 1;
			me.killsToNextMul[i] = me.initKillsRequired;
			me.phaseGhost[i] = 0;
			if (me.ghost[i]) Destroy(me.ghost[i]);
		}
	}

	/// <summary>
	/// Mark a kill for given team, incrementing multiplier if eligible
	/// </summary>
	/// <param name="team">team that did the kill (0 or 1)</param>
	public static void AddKill(int team)
	{

		if (!Globals.playing) return;

		me.killsToNextMul[team]--;
		if (me.killsToNextMul[team] <= 0)
		{
			// increment multiplier
			me.killsToNextMul[team] = me.initKillsRequired + me.extraKillsRequiredPerIncr * (me.muls[team] - 1);
			me.muls[team]++;
			me.textMuls[team].text = me.preText + me.muls[team].ToString();
			me.au[team].Play();

			// create expanding ghost
			if (me.ghost[team]) Destroy(me.ghost[team]);
			var go = Instantiate(me.textMuls[team].gameObject, me.textMuls[team].transform.parent);
			var sh = go.GetComponent<Shadow>();
			if (sh) Destroy(sh);
			me.ghost[team] = go;
			me.trGhost[team] = go.transform;
			me.textGhost[team] = go.GetComponent<Text>();
			me.phaseGhost[team] = 1f;
		}
	}
}

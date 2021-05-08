using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSShield : MonoBehaviour
{
	private Collider[] colliders;

	public GameObject myShield;

	private Material mat;
	private Collider coll;

	/// <summary>
	/// duration of shield-active
	/// </summary>
	public const float duration = 5f;
	/// <summary>
	/// shield recharge delay
	/// </summary>
	public const float rechargeDur = 20f;
	/// <summary>
	/// activation transition delay
	/// </summary>
	public const float activationDur = .6f;

	/// <summary>
	/// shield-active timer
	/// </summary>
	public float Timer { get; private set; }
	/// <summary>
	/// delay until next possible activation
	/// </summary>
	public float Delay { get; private set; }

	// current transition phase (0 to 1, 0 = shield down, 1 = fully up)
	private float phase;

	// shader variables
	public const float minDissolve = -.2f;
	private float dissolve = 2f;
	public Color team2Col;
	public static int dissolveProperty = Shader.PropertyToID("_DissolveAmount");
	public const float passThres = .5f;
	public SSDeathAndSpawn death { get; private set; }

	private void Awake()
	{
		colliders = GetComponents<Collider>();
		if (!myShield)
		{
			enabled = false;
			return;
		}

		mat = myShield.GetComponent<Renderer>().material;
		coll = myShield.GetComponent<Collider>();
		//myShield.SetActive (false);
		death = GetComponent<SSDeathAndSpawn>();
	}

	private void Update()
	{
		if (Globals.paused) return;

		// delay
		if (Delay > 0) Delay -= Time.deltaTime;

		// transition phase
		var prevPhase = phase;
		if (Timer > 0)
		{
			Timer -= Time.deltaTime;
			phase = Mathf.Min(1f, phase + Time.deltaTime / activationDur);
		}
		else phase = Mathf.Max(0f, phase - Time.deltaTime / activationDur);
		
		if (prevPhase != phase)
		{
			// update shader
			SetDissolve(1f - phase);
			// toggle colliders
			if (prevPhase < passThres && phase >= passThres)
			{
				coll.enabled = true;
				foreach (var c in colliders) c.enabled = false;
			}
			else if (prevPhase >= passThres && phase < passThres)
			{
				coll.enabled = false;
				foreach (var c in colliders) c.enabled = true;
			}
			else if (prevPhase > 0f && phase == 0f)
			{
				// deactivate object
				myShield.SetActive(false);
			}
		}
	}

	/// <summary>
	/// returns if shield is fully armed and effective
	/// </summary>
	/// <returns>whether shield up</returns>
	public bool IsOn()
	{
		return myShield.activeSelf && coll.enabled;
	}

	/// <summary>
	/// update shader transition value
	/// </summary>
	/// <param name="val">current transition value (0 to 1)</param>
	void SetDissolve(float val)
	{
		if (dissolve == val) return;
		dissolve = val;
		mat.SetFloat(dissolveProperty, Mathf.Lerp(minDissolve, 1f, dissolve));
	}

	/// <summary>
	/// set our team (updates color and layer)
	/// </summary>
	/// <param name="team">team (0 or 1)</param>
	public void AssignTeam(int team)
	{
		if (!enabled) return;
		myShield.layer = SSTeam.shieldBaseLayer + team;
		if (team == 1) mat.color = team2Col;
	}

	/// <summary>
	/// activates shield, if not recharging
	/// </summary>
	public void Activate()
	{
		if (!enabled || Timer > 0 || Delay > 0) return;
		SetDissolve(1f);
		myShield.SetActive(true);
		Timer = duration;
		Delay = duration + rechargeDur;
	}

	/// <summary>
	/// deactivate shield (with fadeout transition)
	/// </summary>
	public void Deactivate()
	{

		//if (!enabled) return;
		Timer = 0f;
		//myShield.SetActive (false);
	}

	/// <summary>
	/// Initialize, hide and reset shield.
	/// </summary>
	public void ResetMe()
	{
		if (!enabled) return;
		Timer = 0f;
		phase = 0f;
		myShield.SetActive(false);
		Delay = 0f;
		coll.enabled = false;
		foreach (var c in colliders) c.enabled = true;
	}
}

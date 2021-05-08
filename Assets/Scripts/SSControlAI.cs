using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSControlAI : MonoBehaviour
{

	private SSFlight flight;
	private SSTeam team;
	private SSLaser laser;
	private Transform target;

	/// <summary>
	/// interval between checks for better targets
	/// </summary>
	public const float lockOnRenewInterval = .75f;

	/// <summary>
	/// factor of how much the target's velocity difference deteriorates his candidate score
	/// </summary>
	public const float lockOnSpeedConsideration = .01f;

	/// <summary>
	/// min roll difference from target below which we don't attempt to correct it
	/// </summary>
	public const float minRollOffset = 20f;
	/// <summary>
	/// max roll difference from target at which we apply max roll speed
	/// </summary>
	public const float maxRollOffset = 50f;
	/// <summary>
	/// max pitch difference from target at which we apply max pitch speed
	/// </summary>
	public const float maxPitchOffset = 50f;

	/// <summary>
	/// preferred target distance to keep
	/// </summary>
	//public const float followDist = 80f;

	/// <summary>
	/// time difference at which we try to reach target by modifying throttle
	/// </summary>
	public const float followDesiredETA = 1f;
	/// <summary>
	/// speed difference from desired ETA speed at which we apply max throttle offset
	/// </summary>
	public const float followSpeedTolerance = 20f;
	/// <summary>
	/// min thrust to maintain despite follow distance ETA
	/// </summary>
	public float maintainMinThrust = .5f;

	/// <summary>
	/// max angle from target below which we fire
	/// </summary>
	public const float maxFiringAngle = 30f;
	/// <summary>
	/// max distance from target below which we fire
	/// </summary>
	public const float maxFiringDist = 300f;

	/// <summary>
	/// percentage of time at which we fire (otherwise hold, to ensure cooldown)
	/// </summary>
	public const float fireDutyCycle = .75f;
	/// <summary>
	/// min duration of one firing duty cycle
	/// </summary>
	public const float fireCycleDurMin = 2f;
	/// <summary>
	/// max duration of one firing duty cycle
	/// </summary>
	public const float fireCycleDurMax = 4f;

	/// <summary>
	/// current firing duty cycle duration
	/// </summary>
	private float fireCycleDur;
	/// <summary>
	/// current firing duty cycle timer
	/// </summary>
	private float fireCycleTimer;

	[Tooltip("how many times we want this bot cloned (if > 0, this object will be deactivated afterwards)")]
	public int cloneMe = 0;
	[Tooltip("radius of sphere in which all clones will be scattered")]
	public float cloneMeatDist = 1000f;

	/// <summary>
	/// alternating variable for team of spawned clones
	/// </summary>
	public static int cloneTeamToggler = 0;

	private void Awake()
	{
		// create clones
		if (cloneMe > 0)
		{
			int c = cloneMe;
			cloneMe = 0;
			for (int i = 0; i < c; i++)
			{
				var go = Instantiate(gameObject,
					transform.position + cloneMeatDist * Random.insideUnitSphere,
					Random.rotation);
				// alternate clone's teams
				var t = go.GetComponent<SSTeam>();
				t.team = cloneTeamToggler;
				cloneTeamToggler = 1 - cloneTeamToggler;
			}
			// deactivate this object (was only used as template)
			gameObject.SetActive(false);
		}
		// init firing duty cycle
		fireCycleDur = Random.Range(fireCycleDurMin, fireCycleDurMax);
	}

	void Start()
	{
		flight = GetComponent<SSFlight>();
		team = GetComponent<SSTeam>();
		laser = GetComponent<SSLaser>();
	}

	private void OnEnable()
	{
		// repeated target checks
		InvokeRepeating(nameof(ChooseTarget), lockOnRenewInterval * Random.value,
			lockOnRenewInterval);
	}

	private void OnDisable()
	{
		// stop repeated target checks while inactive
		CancelInvoke();
	}

	void Update()
	{
		// hold attitude/speed while no target or game paused
		if (!target) return;
		if (Globals.paused) return;

		// get local position offset from target
		var delta = target.position - transform.position;
		var tdelta = transform.InverseTransformVector(delta);

		// roll to match target's X offset if needed
		if (Mathf.Abs(tdelta.x) > minRollOffset)
			flight.roll = (tdelta.x - Mathf.Sign(tdelta.x) * minRollOffset)
				/ (maxRollOffset - minRollOffset); // * (tdelta.y > 0f ? 1f : -1f);
		else flight.roll = 0f;

		// pitch to match target's Y offset
		flight.pitch = -tdelta.y / maxPitchOffset;

		// adjust thrust to match ETA to target
		var dmag = delta.magnitude;
		float speedForDesiredETA = (dmag - followDesiredETA) / followDesiredETA;
		flight.thrust = Mathf.Max(maintainMinThrust,
			(speedForDesiredETA - flight.velocity.magnitude) / followSpeedTolerance);

		// process firing duty cycle
		fireCycleTimer = (fireCycleTimer + Time.deltaTime) % fireCycleDur;

		// fire if target in range & angle and if firing duty cycle permits it
		laser.trigger = dmag < maxFiringDist && fireCycleTimer < fireDutyCycle * fireCycleDur
			&& Vector3.Angle(transform.forward, delta) < maxFiringAngle;
	}

	void ChooseTarget()
	{

		if (Globals.paused) return;

		Transform bestCandidate = null;
		float bestDist = Mathf.Infinity;
		// parse possible targets
		foreach (var ss in SSTeam.list)
		{
			// ignore if this is a teammate or myself
			if (ss.team != team.team && ss.gameObject.activeSelf)
			{
				// get scoring "distance" depending on distance, angle and velocity difference
				var delta = ss.transform.position - transform.position;
				float dist = delta.sqrMagnitude;
				dist *= 1f + Vector3.Angle(transform.forward, delta) / 180f;
				dist *= 1f + (ss.flight.velocity - flight.velocity).magnitude * lockOnSpeedConsideration;
				
				// choose this candidate if better that the last
				if (dist < bestDist)
				{
					bestCandidate = ss.transform;
					bestDist = dist;
				}
			}
		}
		// select best candidate, if any
		target = bestCandidate;
	}
}

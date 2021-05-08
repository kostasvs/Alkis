using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSLaser : MonoBehaviour
{
	private SSTeam team;
	private SSControlPlayer controlPlayer;

	public GameObject laserPrefab;
	public Transform[] barrels;
	private ParticleSystem[] particleSystems;

	public bool trigger;
	public float interval;
	private float timer;
	public float damage = .15f;

	[Tooltip("currently locked-on target")]
	public Transform lockOn;
	private float lastLockOnTime;
	public const float minLockOnRenewInterval = .4f;
	public const float maxLockOnAngle = 40f;
	public const float maxLockOnDistSqr = 600f * 600f;
	/// <summary>
	/// how much the angle difference deteriorates candidate score for a target
	/// </summary>
	public const float angleOffsetWeight = 3f;

	public bool canOverheat = true;
	/// <summary>
	/// level at which heat must drop to before gun becomes usable again
	/// </summary>
	public const float overheatRestoreLevel = .5f;
	public float heatPerShot = .04f;
	public float cooldownRate = .16f;
	public bool inOverheat { get; private set; }
	public float heatLevel { get; private set; }

	private AudioSource[] au;
	public AudioClip[] auClips;
	private int auLast = -1;
	private GameObject auParent;
	private float soundRangeSqr;

	void Awake()
	{
		team = GetComponent<SSTeam>();
		controlPlayer = GetComponent<SSControlPlayer>();

		particleSystems = new ParticleSystem[barrels.Length];
		for (int i = 0; i < barrels.Length; i++)
		{
			particleSystems[i] = barrels[i].GetComponent<ParticleSystem>();
		}

		var aupar = transform.Find("laserSound");
		if (aupar) auParent = aupar.gameObject;
		au = auParent.GetComponents<AudioSource>();
		soundRangeSqr = au[0].maxDistance * au[0].maxDistance;
	}

	private void OnEnable()
	{
		// zero heat on respawn
		heatLevel = 0f;
		inOverheat = false;
	}

	void FixedUpdate()
	{
		if (Globals.paused) return;

		// recheck lock-on targets at intervals
		if (controlPlayer && lastLockOnTime < Time.time - minLockOnRenewInterval)
		{
			lastLockOnTime = Time.time;
			RenewLockOn();
		}

		// fire with intervals, if not overheated
		if (timer > 0) timer -= Time.deltaTime;
		else if (trigger && !inOverheat)
		{
			timer += interval;
			// renew lock-on now
			if (lastLockOnTime < Time.time - minLockOnRenewInterval)
			{
				lastLockOnTime = Time.time;
				RenewLockOn();
			}
			// fire
			FireGuns();
		}
		// check heat level
		if (heatLevel > 0f)
		{
			// cooldown
			heatLevel = Mathf.Max(0f, heatLevel - cooldownRate * Time.deltaTime);
			// restore usability
			if (heatLevel < overheatRestoreLevel) inOverheat = false;
		}
	}

	/// <summary>
	/// check for a new best target and lock on to that one, if found
	/// </summary>
	public void RenewLockOn()
	{

		Transform bestCandidate = null;
		float bestDist = maxLockOnDistSqr;
		// parse possible targets
		foreach (var ss in SSTeam.list)
		{
			// ignore if this is a teammate or myself
			if (ss.team != team.team && ss.gameObject.activeSelf)
			{
				// get scoring "distance" depending on distance and angle
				var delta = ss.transform.position - transform.position;
				float dist = delta.sqrMagnitude;
				if (dist < bestDist)
				{
					float angle = Vector3.Angle(transform.forward, delta);
					dist *= 1f + angle / maxLockOnAngle * angleOffsetWeight;
				
					// choose this candidate if better that the last
					if (dist < bestDist && angle < maxLockOnAngle)
					{
						bestCandidate = ss.transform;
						bestDist = dist;
					}
				}
			}
		}
		// select best candidate, if any
		lockOn = bestCandidate;
	}

	/// <summary>
	/// fire guns (does not check for delay or overheat)
	/// </summary>
	public void FireGuns()
	{
		// fire guns
		for (int i = 0; i < barrels.Length; i++)
		{
			var go = Instantiate(laserPrefab, barrels[i].position, barrels[i].rotation);
			var laser = go.GetComponent<Laser>();
			laser.from = this;
			laser.team = team.team;
			laser.damage = damage;
			laser.lockOn = lockOn;
			if (particleSystems[i] != null) particleSystems[i].Play();
		}
		// add to heat
		heatLevel += heatPerShot;
		if (heatLevel >= 1f)
		{
			heatLevel = 1f;
			inOverheat = true;
		}
		// sound
		PlaySound();
	}

	/// <summary>
	/// play firing sound (will cycle through available clips)
	/// </summary>
	public void PlaySound()
	{
		if (auParent == null || !auParent.activeSelf) return;
		// check if within range
		float delta = (transform.position - CamFollow.me.myListener.position).sqrMagnitude;
		if (delta > soundRangeSqr) return;

		auLast = (auLast + 1) % au.Length;
		au[auLast].Stop();
		au[auLast].clip = auClips[Random.Range(0, auClips.Length)];
		au[auLast].Play();
	}
}

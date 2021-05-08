using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSDeathAndSpawn : MonoBehaviour
{
	[Tooltip("current health (normally between 0 and 1, can be more)")]
	public float health = 1f;
	public float StartHealth { get; private set; }
	
	[Tooltip("gameObjects to toggle along with ourselves when dying/respawning")]
	public GameObject[] toggles;
	private UIDamageInd damageInd;

	public GameObject explosionPrefab;
	public GameObject subExplosionPrefab;
	public const int subExplosionCount = 5;
	public GameObject explosionSoundPrefab;

	public GameObject spawnPrefab;
	private ParticleSystem mySpawnPS;

	public float respawnDelay = 4f;
	public float spawnDuration = 2f;
	public float spawnSpeed = 100f;
	public float spawnThrust = 1f;

	private SSFlight flight;
	private SSEngineSound engineSound;
	private SSShield shield;
	private SSTeam team;

	void Awake()
	{
		StartHealth = health;

		mySpawnPS = Instantiate(spawnPrefab).GetComponent<ParticleSystem>();
		flight = GetComponent<SSFlight>();
		engineSound = GetComponent<SSEngineSound>();
		shield = GetComponent<SSShield>();
		team = GetComponent<SSTeam>();
	}

	private void Start()
	{
		// initially reset to dead, respawn after randomized interval
		Die(true, Random.value);
	}

	/// <summary>
	/// Process received damage (may be negated if shield active).
	/// </summary>
	/// <param name="dmg">amount of damage (deducted from health)</param>
	/// <param name="from">responsible Transform to show with damage indicator (player only)</param>
	public void Damage(float dmg, Transform from)
	{
		if (!shield.IsOn())
		{
			// take damage, die if needed, otherwise try to activate shield
			health -= dmg;
			if (health <= 0f) Die();
			else if (shield != null) shield.Activate();
		}
		// show attacker with damage indicator
		if (from != null && damageInd != null) damageInd.TookDamage(from);
	}

	/// <summary>
	/// Die/reset this spaceship.
	/// </summary>
	/// <param name="asReset">whether this was a reset (no explosion, no points)</param>
	/// <param name="delayMultiplier">factor to multiply respawn delay with</param>
	public void Die(bool asReset = false, float delayMultiplier = 1f)
	{

		if (!gameObject.activeSelf)
		{
			// cancel previous respawn timer
			CancelInvoke();
			// respawn after given time
			if (delayMultiplier <= 0) BeginSpawn();
			else Invoke(nameof(BeginSpawn), respawnDelay * delayMultiplier);
			return;
		}

		// reset health
		health = StartHealth;
	
		if (!asReset)
		{
			// explode and add points and multiplier kill
			MakeExplosion();
			int mul = 1;
			if (Multipliers.me) mul = Multipliers.me.muls[1 - team.team];
			if (Domination.me) Domination.Score(1 - team.team, mul);
			if (Multipliers.me) Multipliers.AddKill(1 - team.team);
		}
	
		// reset relevant objects
		if (shield) shield.ResetMe();
		foreach (var go in toggles) go.SetActive(false);
	
		// respawn after given time
		if (delayMultiplier <= 0) BeginSpawn();
		else Invoke(nameof(BeginSpawn), respawnDelay * delayMultiplier);
	
		// hide object (Invoke will still run)
		gameObject.SetActive(false);
	}

	void MakeExplosion()
	{
		// make explosion
		Instantiate(explosionPrefab, transform.position, Quaternion.identity);
		
		// make sub-explosions
		for (int i = 0; i < subExplosionCount; i++)
		{
			var s = Instantiate(subExplosionPrefab, transform.position, Quaternion.identity);
			var ss = s.GetComponent<SubExplosionParticle>();
			ss.velocity = flight.velocity;
		}

		// play sound if within hearing range
		float delta = (transform.position - CamFollow.me.myListener.position).sqrMagnitude;
		if (delta > ExplosionSound.soundRangeSqr) return;
		Instantiate(explosionSoundPrefab, transform.position, Quaternion.identity);
	}

	/// <summary>
	/// position on starting vortex, play spawning effects, and spawn with delay
	/// </summary>
	void BeginSpawn()
	{
		// randomly position on spawn vortex
		var r = Random.value * 360f;
		transform.position = WarpHole.GetRandomPos(team.team, r);
		transform.rotation = WarpHole.me[team.team].transform.rotation;
		transform.Rotate(0, 0, r, Space.Self);

		// play sound
		mySpawnPS.transform.position = transform.position;
		mySpawnPS.Play();

		// if we have the camera, play sound and fast-zoom to spawn pos
		if (CamFollow.me.follow == transform)
		{
			CamFollow.me.focusChaseMode = true;
			CamFollow.me.PlaySpawnSound(0);
		}
		// spawn with delay
		Invoke(nameof(CompleteSpawn), spawnDuration);
	}

	void CompleteSpawn()
	{
		// activate object
		gameObject.SetActive(true);
		
		// reset controls
		if (flight)
		{
			flight.ZeroiseControls();
			flight.thrust = spawnThrust;
			flight.adaptedThrust = flight.thrust;
			flight.linearThrust = flight.thrust;
			flight.velocity = transform.forward * spawnSpeed;
			engineSound.RestartEngineSound();
		}

		// toggle relevant objects
		foreach (var go in toggles) go.SetActive(true);

		// if we have the camera, play sound
		if (CamFollow.me.follow == transform)
		{
			CamFollow.me.PlaySpawnSound(1);
		}
	}

	public void AssociateDamageInd(UIDamageInd ind)
	{
		damageInd = ind;
	}
}

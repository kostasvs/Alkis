using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{

	[Tooltip("cannon (player) that fired this laser")]
	public SSLaser from;

	[Tooltip("team (0 or 1)")]
	public int team;

	[Tooltip("speed in units/sec")]
	public float speed;

	[Tooltip("lifetime in secs")]
	public float lifetime;
	private float timer;

	[Tooltip("scale factor to apply as end of life reaches")]
	public float scaleUp = 1f;

	// hardcoded scale
	private static Vector3 initScale = new Vector3(150, 150, 100);

	/// <summary>
	/// layermask for each team
	/// </summary>
	private static int[] hitmask;

	public GameObject hitPrefab;

	public float damage;

	[Tooltip("transform to home onto")]
	public Transform lockOn;
	public const float lockOnTurnSpeed = 120f;

	void Awake()
	{

		// define hitmasks
		if (hitmask == null)
			hitmask = new int[] {
				LayerMask.GetMask ("SpaceShip2", "Shield2"),
				LayerMask.GetMask ("SpaceShip1", "Shield1")
			};

		transform.localScale = Vector3.zero;
	}

	private void Start()
	{

		// get team-assigned mesh
		var mf = GetComponent<MeshFilter>();
		if (!SSTeam.meshPairs.ContainsKey(mf.sharedMesh)) SSTeam.CreateMeshPair(mf.sharedMesh);
		if (team == 1) mf.sharedMesh = SSTeam.meshPairs[mf.sharedMesh];
	}

	private void FixedUpdate()
	{
		if (Time.timeScale == 1f) ExecUpdate();
	}
	private void Update()
	{
		if (Time.timeScale != 1f) ExecUpdate();
	}

	void ExecUpdate()
	{

		if (Globals.paused) return;

		// home onto target
		if (lockOn && lockOn.gameObject.activeSelf)
		{
			transform.rotation = Quaternion.RotateTowards(transform.rotation,
				Quaternion.LookRotation(lockOn.position - transform.position, transform.up),
				lockOnTurnSpeed * Time.deltaTime);
		}

		// collision detection
		var ray = new Ray(transform.position, transform.forward);
		if (Physics.Raycast(ray, out RaycastHit hit, speed * Time.deltaTime, hitmask[team]))
		{
			// apply damage if unshielded target hit
			var ss = hit.transform.GetComponent<SSDeathAndSpawn>();
			if (ss != null) ss.Damage(damage, from ? from.transform : null);
			else if (hit.transform.parent != null)
			{
				// if shield hit, apply damage to its owner (shield will negate it if active)
				var sh = hit.transform.parent.GetComponent<SSShield>();
				if (sh != null) sh.death.Damage(damage, from ? from.transform : null);
			}
			// destroy this laser
			Instantiate(hitPrefab, hit.point, Quaternion.identity);
			Destroy(gameObject);
		}
		else
		{
			// travel
			transform.position += transform.forward * (speed * Time.deltaTime);
			transform.localScale = initScale * (1f + scaleUp * timer / lifetime);
			// lifetime
			timer += Time.deltaTime;
			if (timer > lifetime) Destroy(gameObject);
		}
	}
}

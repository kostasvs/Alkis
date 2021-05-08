using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubExplosionParticle : MonoBehaviour
{
	public float durMin = 1.5f;
	public float durMax = 2f;
	float dur;

	public Vector3 velocity;

	public float scatterMin = 50f;
	public float scatterMax = 80f;

	public ParticleSystem[] particleSystems;

	public float linger;

	void Start()
	{
		// random initial velocity and lifetime
		velocity += Random.onUnitSphere * Random.Range(scatterMin, scatterMax);
		dur = Random.Range(durMin, durMax);
	}

	void Update()
	{
		if (Globals.paused) return;

		// velocity
		transform.position += velocity * Time.deltaTime;

		// lifetime
		if (dur > 0f)
		{
			dur -= Time.deltaTime;
			if (dur < 0f) PauseMe();
		}
		else
		{
			// linger before destroying
			linger -= Time.deltaTime;
			if (linger < 0f) Destroy(gameObject);
		}
	}

	/// <summary>
	/// Pause emission of particles.
	/// </summary>
	void PauseMe()
	{
		foreach (var p in particleSystems)
		{
			var e = p.emission;
			e.enabled = false;
		}
	}
}

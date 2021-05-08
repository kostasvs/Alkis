using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionSound : MonoBehaviour
{
	public const float soundRangeSqr = 600 * 600;
	public const float pitchVariation = .1f;

	void Start()
	{
		// play sound with random pitch variation
		var au = GetComponent<AudioSource>();
		au.pitch += Random.Range(-pitchVariation, pitchVariation);
		au.Play();
		// schedule deletion of this gameObject
		Invoke(nameof(DestroyMe), au.clip.length / au.pitch * 1.5f);
	}

	void DestroyMe()
	{
		Destroy(gameObject);
	}
}

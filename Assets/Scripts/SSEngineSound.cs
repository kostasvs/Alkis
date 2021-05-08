using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSEngineSound : MonoBehaviour
{
	private AudioSource auEngine;

	public const float auEnginePitchVariation = .2f;
	private bool hasEngineSound = false;
	public const float listenerCheckInterval = .25f;
	private float soundRangeSqr;

	private SSFlight flight;

	private void Start()
	{
		// get our audioSource
		auEngine = GetComponent<AudioSource>();

		// set random playback pos
		auEngine.time = Random.value * auEngine.clip.length;

		// play if enabled
		if (auEngine.enabled)
		{
			auEngine.Play();
			hasEngineSound = true;
		}

		// get parameters
		soundRangeSqr = auEngine.maxDistance * auEngine.maxDistance;
		flight = GetComponent<SSFlight>();

		// repeated listener range checks
		InvokeRepeating(nameof(CheckListener), listenerCheckInterval * Random.value,
			listenerCheckInterval);
	}

	/// <summary>
	/// toggle sound depending on whether we are within hearing distance
	/// </summary>
	void CheckListener()
	{
		if (!gameObject.activeSelf) return;
		if (Globals.paused) return;

		// check if within hearing distance
		float delta = (transform.position - CamFollow.me.myListener.position).sqrMagnitude;
		if (auEngine.isPlaying)
		{
			if (delta > soundRangeSqr) auEngine.Pause();
		}
		else
		{
			if (delta < soundRangeSqr) auEngine.Play();
		}
	}

	/// <summary>
	/// randomize pos/pitch and play sound (if within range)
	/// </summary>
	public void RestartEngineSound()
	{
		if (!hasEngineSound) return;
		
		// randomize current playback pos and pitch
		auEngine.time = Random.value * auEngine.clip.length;
		auEngine.pitch = 1f + auEnginePitchVariation * (flight.linearThrust - .5f);
		
		// check range
		CheckListener();
	}

	/// <summary>
	/// update sound pitch according to current thrust
	/// </summary>
	/// <param name="thrust">current thrust (0 to 1)</param>
	public void SetPitchToThrust(float thrust)
	{
		if (!hasEngineSound) return;
		auEngine.pitch = 1f + auEnginePitchVariation * (thrust - .5f);
	}
}

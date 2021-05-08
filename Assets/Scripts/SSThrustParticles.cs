using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSThrustParticles : MonoBehaviour
{
	private SSFlight flight;

	public ParticleSystem[] particleSystems;
	private ParticleSystem.MainModule[] mainModules;

	public float speedScaleUp;
	public float sizeScaleUp;

	private float[] initSpeed;
	private float[] initSize;
	private float prevThrust;

	void Start()
	{
		flight = GetComponent<SSFlight>();

		mainModules = new ParticleSystem.MainModule[particleSystems.Length];
		initSpeed = new float[particleSystems.Length];
		initSize = new float[particleSystems.Length];
		for (int i = 0; i < particleSystems.Length; i++)
		{
			mainModules[i] = particleSystems[i].main;
			initSpeed[i] = mainModules[i].startSpeedMultiplier;
			initSize[i] = mainModules[i].startSizeMultiplier;
		}
	}

	void Update()
	{
		if (Globals.paused) return;

		// update particle's start speed/size depending on thrust
		if (prevThrust != flight.linearThrust)
		{
			prevThrust = flight.linearThrust;
			for (int i = 0; i < particleSystems.Length; i++)
			{
				mainModules[i].startSpeedMultiplier = initSpeed[i] + speedScaleUp * prevThrust;
				mainModules[i].startSizeMultiplier = initSize[i] + sizeScaleUp * prevThrust;
			}
		}
	}
}

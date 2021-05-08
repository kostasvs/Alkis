using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSFlight : MonoBehaviour
{
	public float thrust;
	public float roll;
	public float pitch;
	public float yaw;

	private float rollSmooth;
	private float pitchSmooth;
	private float yawSmooth;
	[Tooltip("lerp factor with which to follow the controls")]
	public float smoothLerp = .1f;

	[Tooltip("accerelation at max thrust")]
	public float accelerationMax;
	[Tooltip("drag factor")]
	public float dragEfficiency = 0.9f;

	[Tooltip("in degrees/sec")]
	public float rollRate;
	[Tooltip("in degrees/sec")]
	public float pitchRate;
	[Tooltip("in degrees/sec")]
	public float yawRate;

	[Tooltip("current world velocity in units/sec")]
	public Vector3 velocity;
	
	/// <summary>
	/// current thrust, boosted to minimum required value to satisfy current yaw/pitch/roll maneuver
	/// </summary>
	[HideInInspector]
	public float adaptedThrust;
	/// <summary>
	/// linearly following version of adaptedThrust
	/// </summary>
	[HideInInspector]
	public float linearThrust;

	public const float efficientTurnMinSpeed = 30;
	public const float turnAdaptThrustTolerance = .1f * efficientTurnMinSpeed;
	public const float turnDeadzone = .01f;
	public const float thrustLinearRate = 1f;

	private SSEngineSound engineSound;

	private void Start()
	{
		engineSound = GetComponent<SSEngineSound>();
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

		float vmag = velocity.magnitude;
		float currentTurnEfficiency;

		// clamp controls
		thrust = Mathf.Clamp01(thrust);
		roll = Mathf.Clamp(roll, -1f, 1f);
		pitch = Mathf.Clamp(pitch, -1f, 1f);
		yaw = Mathf.Clamp(yaw, -1f, 1f);

		// smooth turns
		if (smoothLerp == 1f)
		{
			rollSmooth = roll;
			pitchSmooth = pitch;
			yawSmooth = yaw;
		}
		else
		{
			rollSmooth = Mathf.LerpUnclamped(rollSmooth, roll, smoothLerp);
			pitchSmooth = Mathf.LerpUnclamped(pitchSmooth, pitch, smoothLerp);
			yawSmooth = Mathf.LerpUnclamped(yawSmooth, yaw, smoothLerp);
		}

		// calculate minimum speed required for currently requested roll and/or pitch
		float efficientTurnRequiredSpeed = efficientTurnMinSpeed * Mathf.Max(Mathf.Abs(rollSmooth),
			Mathf.Abs(pitchSmooth), Mathf.Abs(yawSmooth));
		
		// calculate thrust required to attain efficientTurnRequiredSpeed
		adaptedThrust = Mathf.Max(thrust, Mathf.Clamp01(
			(efficientTurnRequiredSpeed - vmag) / turnAdaptThrustTolerance
			));
		
		// smooth thrust
		linearThrust = Mathf.MoveTowards(linearThrust, adaptedThrust, thrustLinearRate * Time.deltaTime);
		
		// set engine audio pitch according to thrust
		engineSound.SetPitchToThrust(linearThrust);

		// calculate effective pitch, roll, yaw as adapted by current speed
		float effectivePitch = 0, effectiveRoll = 0, effectiveYaw = 0;
		if (Mathf.Abs(pitchSmooth) > turnDeadzone)
		{
			currentTurnEfficiency = Mathf.Min(1, vmag / (efficientTurnMinSpeed * Mathf.Abs(pitchSmooth)));
			effectivePitch = pitchSmooth * currentTurnEfficiency;
		}
		if (Mathf.Abs(rollSmooth) > turnDeadzone)
		{
			currentTurnEfficiency = Mathf.Min(1, vmag / (efficientTurnMinSpeed * Mathf.Abs(rollSmooth)));
			effectiveRoll = rollSmooth * currentTurnEfficiency;
		}
		if (Mathf.Abs(yawSmooth) > turnDeadzone)
		{
			currentTurnEfficiency = Mathf.Min(1, vmag / (efficientTurnMinSpeed * Mathf.Abs(yawSmooth)));
			effectiveYaw = yawSmooth * currentTurnEfficiency;
		}

		// apply effective pitch / roll / yaw
		transform.Rotate(pitchRate * effectivePitch * Time.deltaTime,
			yawRate * effectiveYaw * Time.deltaTime,
			-rollRate * effectiveRoll * Time.deltaTime);

		// apply thrust and drag
		velocity += Mathf.Lerp(0f, accelerationMax, linearThrust) * Time.deltaTime * transform.forward;
		velocity *= dragEfficiency;

		// apply velocity to position
		transform.position += velocity * Time.deltaTime;
	}

	public void ZeroiseControls()
	{
		thrust = 0f;
		roll = 0f;
		pitch = 0f;
		yaw = 0f;
	}
}

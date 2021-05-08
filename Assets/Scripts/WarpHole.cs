using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpHole : MonoBehaviour
{
	public static readonly WarpHole[] me = new WarpHole[2];
	[Tooltip("index (team) of this warphole (0 or 1)")]
	public int index;
	
	/// <summary>
	/// local Z depth
	/// </summary>
	public const float depth = .12f;
	public AnimationCurve depthCurve;

	[Tooltip("in degrees/sec")]
	public float spinSpeed = 15f;
	[Tooltip("multiplier for spin speed of secondary vortex")]
	public float outerMultiplier = 1.25f;
	public Transform secondaryTr;

	void Awake()
	{
		// check for duplicate
		if (me[index] && me[index] != this)
		{
			Debug.LogWarning("duplicate WarpHole " + index);
			return;
		}
		me[index] = this;
	}

	void Update()
	{
		if (Globals.paused) return;

		// spin primary and secondary vortex
		transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
		secondaryTr.Rotate(0f, 0f, spinSpeed * outerMultiplier * Time.deltaTime);
	}

	/// <summary>
	/// Get a position on WarpHole of given time, at given angle, at a random distance from center.
	/// </summary>
	/// <param name="team">WarpHole team (0 or 1)</param>
	/// <param name="angle">Angle around WarpHole's local Z axis (in degrees)</param>
	/// <returns></returns>
	public static Vector3 GetRandomPos(int team, float angle)
	{
		var v = Random.value;
		Vector3 vv = (Quaternion.Euler(0, 0, angle) * Vector3.up) * (v * .5f);
		vv.z = -me[team].depthCurve.Evaluate(v) * depth;
		return me[team].transform.TransformPoint(vv);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.InputSystem;

public class SSControlPlayer : MonoBehaviour
{

	private SSFlight flight;
	private SSLaser laser;

	private float prevSetThrust = -1f;
	public float thrustSensitivity;

	public CamFollow camFollow;

	void Awake()
	{
		flight = GetComponent<SSFlight>();
		laser = GetComponent<SSLaser>();
	}

	private void Start()
	{
		IOMgr.Init();
		IOMgr.ReadConfig();

		// store last thrust
		prevSetThrust = IOMgr.GetControl(IOMgr.Control.Thrust_Set_To);
	}

	void Update()
	{
		if (Globals.paused) return;

		// legacy way, fixed bindings
		/*flight.thrust = Input.GetButton ("Jump") ? 1 : 0;
		flight.roll = Input.GetAxis ("Horizontal");
		flight.pitch = Input.GetAxis ("Vertical");*/

		// legacy way, configurable bindings
		var setThrust = IOMgr.GetControl(IOMgr.Control.Thrust_Set_To);
		if (prevSetThrust != setThrust)
		{
			// if "set thrust" value changed, get new value
			flight.thrust = setThrust;
			prevSetThrust = setThrust;
		}
		var deltaThrust = IOMgr.GetControl(IOMgr.Control.Thrust_Increase)
			- IOMgr.GetControl(IOMgr.Control.Thrust_Decrease);
		flight.thrust += thrustSensitivity * deltaThrust * Time.deltaTime;
		flight.roll = IOMgr.GetControl(IOMgr.Control.Roll_Right)
			- IOMgr.GetControl(IOMgr.Control.Roll_Left);
		flight.pitch = IOMgr.GetControl(IOMgr.Control.Nose_Up)
			- IOMgr.GetControl(IOMgr.Control.Nose_Down);
		laser.trigger = IOMgr.GetControl(IOMgr.Control.Fire_Guns) > .5f;
		if (camFollow) camFollow.SetCurrentView(IOMgr.GetControl(IOMgr.Control.Look_Back) > .5f ? 1 : 0);

		// new input system, abandoned as too buggy at the time
		/*var val = aaThrottleSet.ReadValue<float> ();
		if (prevSetThrust != val) {
			if (prevSetThrust != -1f) flight.thrust = val;
			prevSetThrust = val;
		}
		var deltaThrust = aaThrottle.ReadValue<float> ();
		flight.thrust = flight.thrust + thrustSensitivity * deltaThrust * Time.deltaTime;
		var move = aaMove.ReadValue<Vector2> ();
		flight.roll = move.x;
		flight.pitch = move.y;
		laser.trigger = aaFire.ReadValue<float> () > .5f;
		if (camFollow) camFollow.setCurrentView (aaLookBack.ReadValue<float> () > .5f ? 1 : 0);*/
	}

	public void OnPause()
	{
		if (!Globals.gameover) Globals.TogglePause(gameObject);
	}
}

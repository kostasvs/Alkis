using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{

	// reference to this singleton
	public static CamFollow me;

	[Tooltip("Transform to follow")]
	public Transform follow;

	// stored pos/rot offsets
	private readonly Vector3[] posOffset = new Vector3[2];
	private readonly Quaternion[] rotOffset = new Quaternion[2];
	private Quaternion rotOffsetDefault;

	[Tooltip("position lerp smoothing factor")]
	public float posLerp = .1f;

	[Tooltip("rotation lerp smoothing factor")]
	public float rotLerp = .1f;

	/// <summary>
	/// Current view mode (0 = normal, 1 = rear view)
	/// </summary>
	private int curView = 0;

	/// <summary>
	/// stores camera velocity to maintain it for a short while after player dies
	/// </summary>
	private Vector3 velocityAcquired;

	[Tooltip("stored velocity updating lerp factor")]
	public float velocityLerp = .2f;

	[Tooltip("velocity damping factor once player dies")]
	public float velocityDampen = .95f;

	// stored points to focus on
	private Vector3 focusPoint;
	private Vector3 focusUp;

	[Tooltip("whether we are currently chasing the stored focus mode")]
	public bool focusChaseMode;

	[Tooltip("focus point chasing lerp factor")]
	public float focusChaseLerp = .1f;

	[Tooltip("square of min distance to focus point that we want to reach")]
	public float focusChaseDistSqr = 30f * 30f;

	[Tooltip("separate audioListener transform (optional)")]
	public Transform myListener;

	[Tooltip("view spin speed in degrees/sec around player after gameover")]
	public float gameoverSpinSpeed = 10f;

	/// <summary>
	/// whether gameover screen is active
	/// </summary>
	private bool inEndSequence;

	/// <summary>
	/// stored point to pivot around during gameover screen
	/// </summary>
	private Vector3 endPivot;

	[Tooltip("distance from spin pivot during gameover screen")]
	public float distFromPivot = 50f;

	/// <summary>
	/// main menu camera position
	/// </summary>
	private Vector3 originPoint;
	/// <summary>
	/// main menu camera rotation
	/// </summary>
	private Quaternion originRot;

	[Tooltip("mouse sensitivity during middle-mouse free look")]
	public float freeLookRotSpeed = 5f;
	[Tooltip("returning lerp factor after middle-mouse free look")]
	public float freeLookRotLerp = .1f;

	/// <summary>
	/// free-look rotation to add to current rotation
	/// </summary>
	private Quaternion freeLookRot;

	/// <summary>
	/// freeLookRot (see above), in degrees
	/// </summary>
	private Vector3 freeLookRotVector;

	/// <summary>
	/// whether we are currently in free-look
	/// </summary>
	private bool inFreeLook;

	private AudioSource[] au;

	[Tooltip("audioClips for spawn (first = charging-up sound, second = launch sound)")]
	public AudioClip[] auSpawn = new AudioClip[2];

	private void Awake()
	{

		me = this;
		au = GetComponents<AudioSource>();
	}

	void Start()
	{

		// get default pos/rot offsets
		posOffset[0] = follow.InverseTransformVector(follow.position - transform.position);
		rotOffset[0] = transform.rotation * Quaternion.Inverse(follow.rotation);
		rotOffsetDefault = rotOffset[0];
		freeLookRot = rotOffsetDefault;

		// generate rear view pos/rot offsets
		posOffset[1] = posOffset[0];
		posOffset[1].z = -posOffset[1].z;
		rotOffset[1] = rotOffset[0] * Quaternion.Euler(0f, 180f, 0f);

		// initialize focus point and origin (mainmenu) point
		focusPoint = transform.position;
		focusUp = transform.up;
		originPoint = transform.position;
		originRot = transform.rotation;

		//if (myListener == null) GetComponent<AudioListener> ().enabled = true;
	}

	private void FixedUpdate()
	{
		// normal update
		if (Time.timeScale == 1f) ExecUpdate();
	}
	private void Update()
	{
		// update during slowmotion/gameover screen
		if (Time.timeScale != 1f && Globals.gameover) ExecUpdate();
	}

	void ExecUpdate()
	{

		if (Globals.paused)
		{
			if (Globals.gameover && Globals.me.freezeHoldTimer == 0f)
			{
				// match end sequence
				if (!inEndSequence)
				{
					// set pivot position to player if alive, otherwise to self
					inEndSequence = true;
					if (follow.gameObject.activeSelf) endPivot = follow.position;
					else endPivot = transform.position;
				}
				// spin around pivot
				transform.rotation = Quaternion.Euler(0, gameoverSpinSpeed * Time.unscaledTime, 0);
				transform.position = endPivot + transform.rotation * Vector3.back * distFromPivot;
			}
			// game is paused, exit here
			return;
		}
		inEndSequence = false;

		// free look with middle mouse btn
		bool mouseLook = Input.GetMouseButton(2);
		if (inFreeLook != mouseLook)
		{
			if (!mouseLook)
			{
				// reset look rotation
				freeLookRot = rotOffsetDefault;
				freeLookRotVector = Vector3.zero;
			}
			inFreeLook = mouseLook;
		}

		if (inFreeLook)
		{
			// update look rotation by mouse movement
			freeLookRotVector += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0)
				* freeLookRotSpeed;
			// generate new rotation by rotating default one
			freeLookRot = rotOffsetDefault * Quaternion.Euler(freeLookRotVector);
		}
		// smoothly follow free-look rotation
		rotOffset[0] = Quaternion.Slerp(rotOffset[0], freeLookRot, freeLookRotLerp);

		if (follow.gameObject.activeSelf)
		{
			// follow player
			if (Time.timeScale == 1f)
			{
				// lerp our position to player position plus offset
				var delta = posLerp * (follow.position - follow.TransformVector(posOffset[curView]) - transform.position);
				transform.position += delta;
				// lerp rotation to player rotation plus offset
				transform.rotation = Quaternion.Slerp(transform.rotation, follow.rotation * rotOffset[curView], rotLerp);
				// update velocity with added motion
				velocityAcquired += velocityLerp * (delta - velocityAcquired);
			}
			else
			{
				// game is in slowmotion/gameover, apply acquired velocity while damping it
				transform.position += velocityAcquired;
				velocityAcquired *= velocityDampen;
			}

			// set focus point to currently aimed point, at player's distance
			focusPoint = transform.position + transform.forward * (follow.position - transform.position).magnitude;
			focusUp = transform.up;
			// position myListener on myself if present 
			if (myListener != null) myListener.position = follow.position;
			// no focus chase
			focusChaseMode = false;
		}
		else
		{
			if (!Globals.playing)
			{
				// zoom to initial position (mainmenu mode)
				if (Time.timeScale == 1f)
				{
					transform.position = Vector3.Lerp(transform.position, originPoint, focusChaseLerp);
					transform.rotation = Quaternion.Lerp(transform.rotation, originRot, focusChaseLerp);
				}
				// no focus chase
				focusChaseMode = false;
				// reset velocityAcquired and focus point
				velocityAcquired = Vector3.zero;
				focusPoint = transform.position;
			}
			else if (focusChaseMode)
			{
				// zoom to new player position
				if (Time.timeScale == 1f)
				{
					var delta = follow.position - transform.position;
					if (delta.sqrMagnitude > focusChaseDistSqr)
						transform.position += focusChaseLerp * delta;
					transform.rotation = Quaternion.Lerp(transform.rotation,
						Quaternion.LookRotation(delta, follow.up), focusChaseLerp);
				}
			}
			else
			{
				// slowly stop
				if (Time.timeScale == 1f)
				{
					transform.position += velocityAcquired;
					velocityAcquired *= velocityDampen;
					// maintain look towards focus point
					if (focusPoint != transform.position) transform.LookAt(focusPoint, focusUp);
				}
				// position myListener on myself if present 
				if (myListener != null) myListener.position = transform.position;
			}
		}
	}

	/// <summary>
	/// set view mode
	/// </summary>
	/// <param name="view">0 = normal, 1 = rear view (more may be added later)</param>
	public void SetCurrentView(int view)
	{

		if (curView == view) return;
		// update mode
		curView = view;

		// instantly update camera pos/rot to match new viewmode
		transform.position = follow.position - follow.TransformVector(posOffset[curView]);
		transform.rotation = follow.rotation * rotOffset[curView];
	}

	/// <summary>
	/// play spawn sound
	/// </summary>
	/// <param name="index">0 = initial sound (charging), 1 = ready sound (launch)</param>
	public void PlaySpawnSound(int index)
	{
		au[1].Stop();
		au[1].clip = auSpawn[index];
		au[1].Play();
	}
}

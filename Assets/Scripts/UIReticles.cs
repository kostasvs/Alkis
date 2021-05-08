using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIReticles : MonoBehaviour
{
	[Tooltip("template reticle to use")]
	public GameObject reticlePrefab;
	/// <summary>
	/// pool of available reticles to use
	/// </summary>
	private GameObject[] pool;

	public Image lockon;
	private Transform lockonTr;

	private Transform prevLockon;

	public float lockonFadeDur = .25f;
	public float lockonFadeScale = 1.5f;
	public float lockonFadeRot = 45f;
	private float lockonFader;
	private Color lockonInitColor;
	private Vector3 lockonInitScale;
	private Quaternion lockonInitRot;
	private float lockonScaler = 1f;

	private readonly Dictionary<SSTeam, Transform> reticles = new Dictionary<SSTeam, Transform>(32);

	public Camera cam;
	public SSTeam myTeam;
	private Transform myTr;
	private SSLaser myLaser;
	
	const int maxParsesPerFrame = 4;
	private HashSet<SSTeam>.Enumerator enumerator;

	[Tooltip("localPosition offset for reticles")]
	public Vector2 offset = new Vector2(-.5f, -.5f);

	public float maxScale = 2f;
	public float maxScaleDist = 300f;
	public float minScale = 1f;
	public float minScaleDist = 900f;
	public const float maxRangeSqr = 4000f * 4000f;
	public int maxReticles = 32;

	void Awake()
	{
		lockonTr = lockon.transform;
		myTr = myTeam.transform;

		lockonInitColor = lockon.color;
		lockonInitScale = lockonTr.localScale;
		lockonInitRot = lockonTr.localRotation;

		myLaser = myTr.GetComponent<SSLaser>();

		// initialize reticles pool
		pool = new GameObject[maxReticles];
		for (int i = 0; i < maxReticles; i++)
		{
			pool[i] = Instantiate(reticlePrefab, reticlePrefab.transform.parent);
		}

		// get spaceships enumerator
		GetTargetsEnumerator ();
	}

	void LateUpdate()
	{
		if (Globals.paused) return;

		// parse enemies for reticle candidates
		for (int i = 0; i < maxParsesPerFrame; i++)
		{
			// try to get next target
			var t = GetNextTarget();
			if (t == null) return;

			// check if alive
			if (!t.gameObject.activeSelf) RemoveReticle(t);
			// check team
			else if (t.team == myTeam.team) RemoveReticle(t);
			// check range
			else
			{
				var tr = t.transform;
				if ((tr.position - myTr.position).sqrMagnitude > maxRangeSqr) RemoveReticle(t);
				// check viewpoint
				else
				{
					var v = cam.WorldToViewportPoint(tr.position);
					if (v.x < 0f || v.y < 0f || v.x > 1f || v.y > 1f || v.z < 0f) RemoveReticle(t);
					else AddReticle(t, false);
				}
			}
		}

		// update reticles if visible
		lockonScaler = -1f;
		foreach (var item in reticles)
		{
			// check viewpoint
			var tr = item.Key.transform;
			var v = cam.WorldToViewportPoint(tr.position);
			var go = item.Value.gameObject;
			if (v.x < 0f || v.y < 0f || v.x > 1f || v.y > 1f || v.z < 0f)
			{
				// screen pos out of bound
				if (go.activeSelf) go.SetActive(false);
				if (tr == myLaser.lockOn) lockonScaler = 0f;
				continue;
			}

			// show reticle on screen pos, with scale adapted by distance
			if (!go.activeSelf) go.SetActive(true);
			var scFactor = Mathf.Lerp(minScale, maxScale,
				(v.z - minScaleDist) / (maxScaleDist - minScaleDist));
			item.Value.localScale = Vector3.one * scFactor;
			v += (Vector3)offset;
			v.z = 0f;
			var pt = cam.ViewportToScreenPoint(v);
			item.Value.localPosition = pt;

			// position lockon reticle if this target is locked on
			if (tr == myLaser.lockOn)
			{
				lockonTr.localPosition = pt;
				lockonScaler = scFactor;
			}
		}

		// check whether we have a lockOn
		bool hasLock = myLaser.lockOn && myLaser.lockOn.gameObject.activeSelf;
		if (hasLock && lockonScaler == -1f)
		{
			// check if in viewport
			var tr = myLaser.lockOn;
			var v = cam.WorldToViewportPoint(tr.position);
			if (v.x < 0f || v.y < 0f || v.x > 1f || v.y > 1f || v.z < 0f) lockonScaler = 0f;
			else
			{
				// update lockon reticle position and size
				lockonScaler = Mathf.Lerp(minScale, maxScale,
					(v.z - minScaleDist) / (maxScaleDist - minScaleDist));
				v += (Vector3)offset;
				v.z = 0f;
				lockonTr.localPosition = cam.ViewportToScreenPoint(v);
			}
		}
		// fade lockon reticle in/out
		var pFader = lockonFader;
		lockonFader = Mathf.MoveTowards(lockonFader, hasLock ? 1f : 0f, Time.deltaTime / lockonFadeDur);
		if (prevLockon != myLaser.lockOn)
		{
			// new lockon, set fade to 0
			if (myLaser.lockOn) lockonFader = 0f;
			prevLockon = myLaser.lockOn;
		}
		if (lockonFader > 0f && lockonScaler > 0f)
		{
			lockon.enabled = true;
			if (pFader != lockonFader)
			{
				// update lockon alpha (fade)
				var fc = lockonInitColor;
				fc.a *= lockonFader;
				lockon.color = fc;
				// update lockon scale
				var fs = lockonInitScale;
				fs *= Mathf.Lerp(lockonFadeScale, 1f, lockonFader) * lockonScaler;
				lockonTr.localScale = fs;
				// update lockon rotation
				var fr = lockonInitRot;
				fr *= Quaternion.Euler(0, 0, Mathf.Lerp(lockonFadeRot, 1f, lockonFader));
				lockonTr.localRotation = fr;
			}
		}
		else lockon.enabled = false;
	}

	/// <summary>
	/// Get next target in the targets set. May return null.
	/// </summary>
	/// <returns>next target, or null.</returns>
	SSTeam GetNextTarget()
	{
		try
		{
			// get next target (will throw exception if HashSet was modified)
			if (!enumerator.MoveNext())
			{
				// reached end of set, reset enumerator
				GetTargetsEnumerator();
				// return null (don't immediately try again in case the list is empty)
				return null;
			}
		}
		catch (System.InvalidOperationException)
		{
			// HashSet was modified, reset enumerator and try again
			GetTargetsEnumerator();
			return GetNextTarget();
		}
		return enumerator.Current;
	}

	/// <summary>
	/// Resets/gets a new enumerator for spaceships.
	/// </summary>
	void GetTargetsEnumerator()
	{
		enumerator = SSTeam.list.GetEnumerator();
	}

	/// <summary>
	/// Release reticle for given spaceship, if it exists.
	/// </summary>
	/// <param name="t">Spaceship whose reticle to release.</param>
	void RemoveReticle(SSTeam t)
	{
		if (reticles.TryGetValue(t, out var r))
		{
			r.gameObject.SetActive(false);
			reticles.Remove(t);
		}
	}

	/// <summary>
	/// Add reticle on given spaceship, if it doesn't exist already.
	/// </summary>
	/// <param name="t">Spaceship to add reticle on</param>
	/// <param name="mandatory">whether to add regardless of distance check</param>
	void AddReticle(SSTeam t, bool mandatory)
	{
		if (reticles.ContainsKey(t)) return;

		if (reticles.Count >= maxReticles)
		{
			// find furthest reticle
			var maxDistSqr = 0f;
			SSTeam maxDistT = null;
			foreach (var tt in reticles)
			{
				var dd = (tt.Key.transform.position - myTr.position).sqrMagnitude;
				if (dd > maxDistSqr)
				{
					maxDistSqr = dd;
					maxDistT = tt.Key;
				}
			}
			// remove that reticle if farther than this, or if mandatory specified
			if (mandatory || maxDistSqr > (t.transform.position - myTr.position).sqrMagnitude)
				RemoveReticle(maxDistT);
			else return;
		}

		// add reticle
		GameObject go = pool[maxReticles - 1];
		for (int i = 0; i < maxReticles - 1; i++)
		{
			if (!reticles.ContainsValue(pool[i].transform))
			{
				go = pool[i];
				break;
			}
		}
		go.SetActive(true);
		reticles.Add(t, go.transform);
	}
}

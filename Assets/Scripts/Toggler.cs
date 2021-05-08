using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toggler : MonoBehaviour
{
	public GameObject[] gameObjects;
	public Renderer[] renderers;
	public Collider[] colliders;
	private bool state;

	void Update()
	{

		if (!Input.GetKeyDown(KeyCode.F2)) return;

		state = !state;
		foreach (var go in gameObjects)
		{
			go.SetActive(state);
		}
		foreach (var c in colliders)
		{
			c.enabled = state;
		}
		foreach (var r in renderers)
		{
			r.enabled = state;
		}
	}
}

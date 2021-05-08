using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowcaseRotator : MonoBehaviour {

	private float spd;
	private bool started = true;
	public float rotSpeed = 30f;

	void Update() {

		if (Input.GetKeyDown(KeyCode.Space)) started = !started;
		if (started) spd = Mathf.Min(rotSpeed, spd + Time.deltaTime * rotSpeed);
		transform.Rotate(0, spd * Time.deltaTime, 0);
	}
}

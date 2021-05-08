using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutputDebug : MonoBehaviour {

	public static OutputDebug me;
	public Text text;
	public SSFlight flight;

    void Awake() {

		me = this;
    }

    void Update() {

		text.text = flight.thrust + "\n" + flight.adaptedThrust + "\n" + flight.velocity.magnitude;
		//text.text = Input.GetAxisRaw ("J1A1").ToString ("F2") + "\n" + Input.GetAxisRaw ("J1A2").ToString ("F2");
	}
}

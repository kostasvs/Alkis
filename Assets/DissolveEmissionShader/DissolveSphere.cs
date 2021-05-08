using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolveSphere : MonoBehaviour {

    Material mat;

    private void Start() {
        mat = GetComponent<Renderer>().material;
    }

    private void Update() {
        mat.SetFloat("_DissolveAmount", Mathf.PingPong(Time.time, 1f));
    }
}
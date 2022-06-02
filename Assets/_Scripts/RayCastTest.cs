using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastTest : MonoBehaviour {
    RaycastHit hit;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey (KeyCode.Mouse0)) {
            if (Physics.Raycast (Camera.main.transform.position, Camera.main.ViewportPointToRay (new Vector3 (0.5f, 0.5f, 0f)).direction, out hit, Mathf.Infinity/*,layerMask.value*/))
                Debug.Log ("raycast hit " + hit.transform.name.ToString ());
        }
	}
}

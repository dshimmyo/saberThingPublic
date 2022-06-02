using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentToMainCamera : MonoBehaviour {
    public bool constrainToCameraRig;
    public GameObject cameraRig;
    public bool parentToMainCameraRoot = false;
	// Use this for initialization
	void Start () {
        if (parentToMainCameraRoot)
        {
            gameObject.GetComponent<Transform>().SetParent(Camera.main.transform.root);

        }
        else if (constrainToCameraRig)
        {
		  if (cameraRig != null)
            gameObject.GetComponent<Transform>().SetParent(cameraRig.GetComponent<Transform>());
        }
        else
        {
            gameObject.GetComponent<Transform>().SetParent(Camera.main.GetComponent<Transform>());
        }
        Destroy(this);

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

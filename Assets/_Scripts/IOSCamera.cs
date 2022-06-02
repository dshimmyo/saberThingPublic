using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOSCamera : MonoBehaviour {

    bool isIOSBuild = false;
    public bool isWithRemoteApp = false;
    float x,y;

	// Use this for initialization
	void Start () 
    {
        #if UNITY_IOS
            Debug.Log("IOS Build");
            isIOSBuild = true;
        #endif

        #if UNITY_EDITOR
            if (!isWithRemoteApp){
                Debug.Log("Just Kidding, Not IOS, without remote, in editor");
                isIOSBuild = false;
            }
        #endif 
        if (isIOSBuild) 
        {
            Input.gyro.enabled = true;
        }
	}




    void FixedUpdate () 
    {
        if (isIOSBuild) 
        {
            x = -Input.gyro.rotationRateUnbiased.x;
            y = -Input.gyro.rotationRateUnbiased.y;
            //z = -Input.gyro.rotationRateUnbiased.z;

            x = Mathf.Clamp (x, -90, 90);

            Camera.main.transform.Rotate (x, y, 0);

            if (Mathf.Abs (Camera.main.transform.rotation.eulerAngles.z) > 5){
                Vector3 camEuler = Camera.main.transform.rotation.eulerAngles;
                Camera.main.transform.rotation = Quaternion.Euler (camEuler.x, camEuler.y, 0f);
            }
        }
    }


}

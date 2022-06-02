using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XButton : MonoBehaviour {
    public GameObject SteamVRLeftController;
    public GameObject SteamVRRightController;
    public bool buttonXPress = false;
    public bool buttonAPress = false;
    private SteamVR_TrackedObject trackedObj; //steamvr 1.2.3, remove for steamvr 2.x
    private SteamVR_Controller.Device device; //steamvr 1.2.3, remove for steamvr 2.x
    private SteamVR_TrackedObject trackedObjLf; //steamvr 1.2.3, remove for steamvr 2.x
    private SteamVR_Controller.Device deviceLf; //steamvr 1.2.3, remove for steamvr 2.x
    private int deviceIndex = -1;
    private int deviceIndexLf = -1;
	void Update () {
		buttonAPress = false;
        buttonXPress = false;
        trackedObj = SteamVRRightController.GetComponent<SteamVR_TrackedObject> ();
        if (trackedObj != null) {
            deviceIndex = (int)trackedObj.index;
            if (deviceIndex > -1)
                device = SteamVR_Controller.Input (deviceIndex);
        }
        trackedObjLf = SteamVRLeftController.GetComponent<SteamVR_TrackedObject> ();
        if (trackedObjLf != null) {
            deviceIndexLf = (int)trackedObjLf.index;
            if (deviceIndexLf > -1)
                deviceLf = SteamVR_Controller.Input (deviceIndexLf);
        }            
        if (device != null){   
            if (device.GetPress(Valve.VR.EVRButtonId.k_EButton_A)){
                buttonAPress = true;
            }
        }
        if (deviceLf != null){    
            if (deviceLf.GetPress(Valve.VR.EVRButtonId.k_EButton_A)){
                buttonXPress = true;
            }
        }
	}
}

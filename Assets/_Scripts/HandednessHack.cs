using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve;
using Valve.VR;

public class HandednessHack : MonoBehaviour {
    [SerializeField] private GameObject SteamVRLeftController;
    [SerializeField] private GameObject SteamVRRightController;
    public static GameObject VirtualSteamVRLeftController;
    public static GameObject VirtualSteamVRRightController;
    public static bool flippedHands = false;
    private float flipTimer = 0;
    public float flipTime = 1;
    private bool isPressing = false;
    private bool isAlreadySwitched = false;
    SteamVR_TrackedObject trackedObj; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_Controller.Device device; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_TrackedObject trackedObjLf; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_Controller.Device deviceLf; //steamvr 1.2.3, remove for steamvr 2.x
    int deviceIndex = -1;
    int deviceIndexLf = -1;
    bool buttonAPress = false;
    bool buttonXPress = false;
    private static bool postSwitchPull;
    public GameObject GetLeftController() {
        return SteamVRLeftController;
    }
    void Start() {
        VirtualSteamVRLeftController = SteamVRLeftController;
        VirtualSteamVRRightController = SteamVRRightController;
    }

    public void ToggleHandedness() {
        flippedHands = !flippedHands;
    }
    public static void SetLeftHanded(bool state)
    {
        flippedHands = state;
    }

    public static bool GetPostSwitchSaberPull()
    {
        return postSwitchPull;
    }

    void Update() {

        if (flippedHands){
            trackedObj = SteamVRRightController.GetComponent<SteamVR_TrackedObject> ();
            if (trackedObj != null) {
                deviceIndex = (int)trackedObj.index;
                if (deviceIndex > -1)
                    device = SteamVR_Controller.Input (deviceIndex);
            }
        } else {
            trackedObj = SteamVRLeftController.GetComponent<SteamVR_TrackedObject> ();
            if (trackedObj != null) {
                deviceIndex = (int)trackedObj.index;
                if (deviceIndex > -1)
                    device = SteamVR_Controller.Input (deviceIndex);
            }            
        }

        buttonAPress = false;
        buttonXPress = false;
        if (device != null){
            if (flippedHands && device.GetPressDown(Valve.VR.EVRButtonId.k_EButton_A))
                Debug.Log("ButtonA");     
            if (device.GetPress(Valve.VR.EVRButtonId.k_EButton_A)){
                if (!flippedHands)
                    buttonXPress = true;
                else 
                    buttonAPress = true;
            }
        }
        if (deviceLf != null){
            if (!flippedHands && deviceLf.GetPressDown(Valve.VR.EVRButtonId.k_EButton_A)){
                Debug.Log("ButtonX");     
            if (deviceLf.GetPress(Valve.VR.EVRButtonId.k_EButton_A))
                buttonAPress = true;
            }
        }
        //problem of button A and X is solved. They register as the same button and should share the same behavior while using openvr 
        if (Input.GetKey(KeyCode.F) 
            || (!flippedHands && (Input.GetButton("HTCShoulderLf") || buttonXPress )) 
            || (flippedHands && (Input.GetButton("HTCShoulderRt") || buttonAPress )) )
        {
            flipTimer += Time.deltaTime;

            if (!isPressing)
            {
                isPressing = true;//start a new press
                flipTimer = 0;
            }
            if (flipTimer > flipTime && !isAlreadySwitched)
            {
                isAlreadySwitched = true;
                flippedHands = !flippedHands;
                flipTimer = 0;
                postSwitchPull = true;
                Debug.Log("postSwitchSaberPull on");

            }
        }
        else {
            flipTimer = 0;
            isAlreadySwitched = false;

        }
        if (flippedHands)
            {
                VirtualSteamVRLeftController = SteamVRRightController;
                VirtualSteamVRRightController = SteamVRLeftController;
            } else {
                VirtualSteamVRLeftController = SteamVRLeftController;
                VirtualSteamVRRightController = SteamVRRightController;
            }

        if (postSwitchPull)//monitor the opposing hand's input only until the button is released
        {
            if ((flippedHands && !(Input.GetButton("HTCShoulderLf") || buttonXPress))
                || (!flippedHands && !(Input.GetButton("HTCShoulderRt") || buttonAPress)))
            {
                postSwitchPull = false;
                Debug.Log("postSwitchSaberPull off");
            }
        }
    }
}

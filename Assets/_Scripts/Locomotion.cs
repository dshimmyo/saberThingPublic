using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class Locomotion : MonoBehaviour {

    //public enum controlSchemes {Keyboard, VRController, GearVR, IOS};
    //public controlSchemes controlScheme = controlSchemes.Keyboard;
    public enum walkDirectors {Camera,Controller};
    public walkDirectors walkDirector = walkDirectors.Camera;
    public float walkSpeed = .5f;
    public float turnSpeed = .5f;
    private float horizontalAxis = 0;
    private float verticalAxis = 0;
    private Transform CameraRoot;
    [SerializeField]
    private GameObject leftControllerGO;
    [SerializeField]
    private GameObject rightControllerGO;
    //private GameObject movementControllerGO;
    private GameObject controllerPointer;
    private string vrDeviceModel = "";
    public float degreesRotation = 90;
    bool isTurning = false;
    float turnMult = 1;
    float minTurn = 0;
    float maxTurn = 0;
    float turnTime = 0;
    float smooth = 20f;
    Quaternion target;// = Quaternion.Euler(tiltAroundX, 0, tiltAroundZ);
    [SerializeField] private bool flippedHands = false;
    string controllerSide = "left";
    public bool requirePress = false;
    [SerializeField] private bool isPressTest = false;
    private bool isRift = false;
    private bool isWMR = false;
    [SerializeField] private bool isIndex = false;
    private bool isKnuckles = false;

    //public bool useBothHands = true;
    public bool useOppHandStickRotation = true;
    private Transform _controllerPointerTransform;
    private Transform _cameraMainTransform;
    private Transform _cameraRootTransform;
    [SerializeField] private bool useTeleporter = false;
    private bool previousTrackpadLfPress = false;
    [SerializeField] GameObject VRTeleporterRtGO;
    [SerializeField] GameObject VRTeleporterLfGO;
    [SerializeField] private string renderModelName = "";

    void Start () {
        _cameraMainTransform = Camera.main.transform;
        CameraRoot = _cameraMainTransform.root;
        _cameraRootTransform = CameraRoot.transform;


        if (controllerPointer == null)
            controllerPointer = new GameObject("controllerPointer");
        if (UnityEngine.XR.XRDevice.isPresent)
            StartCoroutine(OculusTouchSetup()); 
        rightControllerGO = HandednessHack.VirtualSteamVRRightController;
        leftControllerGO = HandednessHack.VirtualSteamVRLeftController;
        _controllerPointerTransform = controllerPointer.transform;

        if (VRTeleporterLfGO != null)
        teleporterLf = VRTeleporterLfGO.GetComponent<VRTeleporter>();
        if (VRTeleporterRtGO != null)
        teleporterRt = VRTeleporterRtGO.GetComponent<VRTeleporter>();
    }

    IEnumerator OculusTouchSetup(){
        bool done = false;
        float timer = 0;
        while (!done){
            timer += Time.deltaTime;

            vrDeviceModel = UnityEngine.XR.XRDevice.model;

            //vive/index is default
            if (vrDeviceModel.StartsWith("Oculus")) {//was "Oculus Rift"
                isRift = true;
                if (_controllerPointerTransform != null)
                _controllerPointerTransform.Rotate(new Vector3(45, 0, 0));//45,4,0
                done = true;
            } else if (vrDeviceModel.StartsWith("Acer") ||
                    vrDeviceModel.Contains("Windows") ||
                    vrDeviceModel.Contains("Lenovo") ||
                    vrDeviceModel.Contains("HP") ||
                    vrDeviceModel.Contains("Samsung"))
            {
                isWMR = true;
            }
            else if (vrDeviceModel.StartsWith("Index"))
            {
                isIndex = true;
            }
            if (timer > 2)
                yield break;
            yield return null;
            //Debug.Log("does OculusTouchSetup() run forever?");
       }

    }

    string GetOtherSide(string side)
    {
        if (side == "left") return "right";
        else if (side == "right") return "left";
        
        return "right";//assume default to be left
    }

    VRTeleporter teleporterLf;
    VRTeleporter teleporterRt;
    // Update is called once per frame
    void Update() {
        if (rightControllerGO != HandednessHack.VirtualSteamVRRightController) {
            rightControllerGO = HandednessHack.VirtualSteamVRRightController;
            leftControllerGO = HandednessHack.VirtualSteamVRLeftController;
        }

        if (leftControllerGO != null)
        {
            SteamVR_RenderModel rendermodel = leftControllerGO.GetComponentInChildren<SteamVR_RenderModel>();
            if (rendermodel != null)
            {
                if (renderModelName != rendermodel.renderModelName)
                {
                    renderModelName = rendermodel.renderModelName;
                    if (renderModelName != null)
                    {
                        if (renderModelName.Contains("indexcontroller"))
                        {
                            isKnuckles = true;
                            //xRotationOffset = -15;
                            //yPositionOffset = .02f;
                        }
                        else
                        {
                            isKnuckles = false;
                            //xRotationOffset = 0;
                            //yPositionOffset = 0;
                        }
                    }
                }
            }
        }

        if (teleporterLf != null && teleporterRt != null)
        {
            bool isTeleportDn = false;
            bool isTeleportUp = false;

            if (controllerSide == "left")
            {
                if (isWMR)
                {
                    isTeleportDn = Input.GetButtonDown("WMR_LeftTrackpadPress");
                    isTeleportUp = Input.GetButtonUp("WMR_LeftTrackpadPress");
                }
                else if (isRift)
                {
                    isTeleportDn = Input.GetButtonDown("Vive_LeftTrackpadPress");
                    isTeleportUp = Input.GetButtonUp("Vive_LeftTrackpadPress");
                }
                else if (useTeleporter)
                {
                    isTeleportDn = Input.GetButtonDown("Vive_LeftTrackpadPress");
                    isTeleportUp = Input.GetButtonUp("Vive_LeftTrackpadPress");
                }
                if (isTeleportDn)
                {
                    teleporterLf.SetAngle(-30f);//seems good for wmr
                    teleporterLf.ToggleDisplay(true);
                    previousTrackpadLfPress = true;
                }
                if (isTeleportUp)
                {
                    teleporterLf.Teleport();
                    teleporterLf.ToggleDisplay(false);
                    previousTrackpadLfPress = false;
                }
            }
            else if (controllerSide == "right")
            {
                if (isWMR)
                {
                    isTeleportDn = Input.GetButtonDown("WMR_RightTrackpadPress");
                    isTeleportUp = Input.GetButtonUp("WMR_RightTrackpadPress");
                }
                else if (isRift)
                {
                    isTeleportDn = Input.GetButtonDown("Vive_RightTrackpadPress");
                    isTeleportUp = Input.GetButtonUp("Vive_RightTrackpadPress");
                }
                else if (useTeleporter)
                {
                    isTeleportDn = Input.GetButtonDown("Vive_RightTrackpadPress");
                    isTeleportUp = Input.GetButtonUp("Vive_RightTrackpadPress");
                }
                if (isTeleportDn)
                {
                    teleporterRt.SetAngle(-30f);//seems good for wmr
                    teleporterRt.ToggleDisplay(true);
                    previousTrackpadLfPress = true;
                }
                if (isTeleportUp)
                {
                    teleporterRt.Teleport();
                    teleporterRt.ToggleDisplay(false);
                    previousTrackpadLfPress = false;
                }
            }
        }

    }

    bool GetTrackpadPress(string side)
    {
        bool result = false;
        if (side == "left")
        {
            result = Input.GetButton("Vive_LeftTrackpadPress");
        }
        else if (side == "right") {
            result = Input.GetButton("Vive_RightTrackpadPress");
        }
        isPressTest = result;
        return result;

    }

    float GetControllerAxis(string side, string axis)//this is the most important method
    {
        float testAxis = 0;
        if (side == "left")
        {
            if (!(isRift || isWMR || isKnuckles))
                if (UnityEngine.XR.XRDevice.isPresent && requirePress && !GetTrackpadPress("left"))
                    return 0;
            testAxis = Input.GetAxis(axis);
            if (isWMR)
                if (testAxis < .2 && testAxis > -.2)
                    return 0;
            return Input.GetAxis(axis);//conveniently and hackily using the input manager
        }

        if (side == "right")
        {
            if (!(isRift || isWMR || isKnuckles))
                if (UnityEngine.XR.XRDevice.isPresent && requirePress && !GetTrackpadPress("right"))
                    return 0;

            if (axis == "Vertical")
                testAxis = Input.GetAxis("Oculus Touch Rt Thumb Vert");
            if (axis == "Horizontal")
                testAxis = Input.GetAxis("Oculus Touch Rt Thumb Horiz");
            if (isWMR)
                if (testAxis < .2 && testAxis > -.2)
                    return 0;
            return testAxis;
        }
        return 0;
    }

    bool GetControllerPalmSqueeze(string side) {
        // if (useBothHands){
        //     if (Input.GetAxis("Oculus Touch Left Palm Squeeze") > .5)
        //         return true;
        //     if (Input.GetAxis("Oculus Touch Right Palm Squeeze") > .5)
        //         return true;
        //     return false;
        // }
        if (side == "left")
        {
            if (Input.GetAxis("Oculus Touch Left Palm Squeeze") > .5)
                return true;
            else
                return false;
        }
        if (side == "right")
        {
            if (Input.GetAxis("Oculus Touch Right Palm Squeeze") > .5)
                return true;
            else
                return false;
        }
        return false;
    }
	void FixedUpdate () {
        flippedHands = HandednessHack.flippedHands;
        if (flippedHands)
            controllerSide = "right";
        else
            controllerSide = "left";

        if (!useTeleporter)
        {
            Vector3 walkForward;
            Vector3 walkRight;

            horizontalAxis = GetControllerAxis(controllerSide, "Horizontal");// Input.GetAxis("Horizontal");//using input manager means the left controller controls the movement//fix this! dks
            verticalAxis = GetControllerAxis(controllerSide, "Vertical");//Input.GetAxis("Vertical");
            // if (useBothHands)
            // {
            //     if (controllerSide == "right")
            //     {
            //         horizontalAxis += GetControllerAxis("left", "Horizontal");// Input.GetAxis("Horizontal");//using input manager means the left controller controls the movement//fix this! dks
            //         verticalAxis += GetControllerAxis("left", "Vertical");//Input.GetAxis("Vertical");
            //     }
            //     if (controllerSide == "left")
            //     {
            //         horizontalAxis += GetControllerAxis("right", "Horizontal");// Input.GetAxis("Horizontal");//using input manager means the left controller controls the movement//fix this! dks
            //         verticalAxis += GetControllerAxis("right", "Vertical");//Input.GetAxis("Vertical");
            //     }
            // }

            // if (Game.isUsingHandController)
            //     walkForward = _controllerPointerTransform.forward;
            // else
                walkForward = _cameraMainTransform.forward;
            walkForward *= walkSpeed;

            walkForward.y = 0;
            walkRight = _cameraMainTransform.right * walkSpeed;
            walkRight.y = 0;
            CameraRoot.position += verticalAxis * walkForward;
            float otherHorizontalAxis = GetControllerAxis(GetOtherSide(controllerSide), "Horizontal");
            if (Mathf.Abs(horizontalAxis) > .5 && !isTurning && GetControllerPalmSqueeze(controllerSide))
            {
                isTurning = true;
                turnMult = Mathf.Sign(horizontalAxis);
                minTurn = Mathf.Round(_cameraRootTransform.rotation.eulerAngles.y / degreesRotation) * degreesRotation;
                maxTurn = minTurn + (degreesRotation * turnMult);
                //turnTime = Time.time;
                target = Quaternion.Euler(0, maxTurn, 0);
            }
            else if (Mathf.Abs(horizontalAxis) > 0.1 && !GetControllerPalmSqueeze(controllerSide))
            {
                CameraRoot.position += horizontalAxis * walkRight;
            }

            if (Mathf.Abs(otherHorizontalAxis) > .5 && !isTurning) //alt-hand thumbstick turns now
            {
                isTurning = true;
                turnMult = Mathf.Sign(otherHorizontalAxis);
                minTurn = Mathf.Round(_cameraRootTransform.rotation.eulerAngles.y / degreesRotation) * degreesRotation;
                maxTurn = minTurn + (degreesRotation * turnMult);
                //turnTime = Time.time;
                target = Quaternion.Euler(0, maxTurn, 0);
            }

            if (isTurning)
            {
                Vector3 oldCameraPosition = _cameraMainTransform.position;//worldspace camera pos
                _cameraRootTransform.rotation = Quaternion.Slerp(_cameraRootTransform.rotation, target, Time.fixedDeltaTime * smooth);
                Vector3 cameraOffset = oldCameraPosition - _cameraMainTransform.position;
                _cameraRootTransform.position += cameraOffset;
                if (Mathf.Abs(_cameraRootTransform.rotation.eulerAngles.y - target.eulerAngles.y) < 1)
                    isTurning = false;
            }
        }


    }
}

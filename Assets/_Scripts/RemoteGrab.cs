using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using Valve;
using Valve.VR;

public class RemoteGrab : MonoBehaviour {
    public GameObject hand;
    public GameObject controller;
    private Transform _controllerTransform;
    public bool useController = false;
    public GameObject weapon;
    public GameObject saberGeo;
    private Renderer saberRen;
    private Rigidbody weaponRB;
    private Transform _weaponTransform;
    public float grabVelocity = 10f;
    public float grabVelocityClose = .01f;
    [SerializeField] private float tossAngularMultiplier = 1.0f;

    protected FixedJoint fj;
    private bool isPulling = false;
    private Vector3 prePullVelocity;
    private bool isSpace = false;
    private bool isSaberButtonPush = false;
    private bool isSpacePrevious=false;
    public bool bladeOn = false;
    private GameObject blade;
    private MeshRenderer bladeMeshRenderer;
    private Animator bladeAnimator;
    private lightSaberControl bladeControl;
    private Rigidbody rb;
    public static bool isHoldingSaberLf = false;
    public static bool isHoldingSaberRt = false;
    [SerializeField] private float tossVelThreshold = 2f; 
    [SerializeField] private float tossAVelThreshold = 30f; //rads per second (PI = 180 1rad approx 60 .5 30 .25 15
    private Vector3 forceDir;// = controller.transform.forward;
    private Vector3 forcePosition;// = controller.transform.position;
    [SerializeField] float frustumAngle = 45f;
    SteamVR_TrackedObject trackedObj; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_Controller.Device device; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_TrackedObject trackedObjLf; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_Controller.Device deviceLf; //steamvr 1.2.3, remove for steamvr 2.x
    int deviceIndex = -1;
    private bool isPreviousTriggerSqueeze = false;
    private float buttonTimer = 0;
    private float triggerTimer = 0;
    private bool isPreviousSense = false;
    private bool isCurrentSense = false;
    private AudioClip[] catchClips;
    private float tossingVelocityTimer = 0;
    [SerializeField] float tossReleaseTime = .1f;
    public enum pointerDirectionStyles {controllerForward,controllerAngledDown45,derivedArmDirection};
    [SerializeField] pointerDirectionStyles pointerDirectionStyle = pointerDirectionStyles.controllerForward;
    //new grab/toss code
    private Vector3 previousPos = Vector3.zero;
    private Quaternion previousRotation;
    private GameObject grabbedObject;
    [SerializeField] public bool alwaysGrab = false; //blade always in hand for easy mode
    [SerializeField] private bool flippedHands = false;
    string controllerSide = "right";
    bool isRift = false;
    bool isOculusQuest = false;
    bool isWMR = false;
    [SerializeField] bool isIndex = false;
    bool isKnuckles = false;
    bool isRecentlySwitchedHands = false;
    public bool isDirectional = true;//only grabs when the controller is aimed near the saber, turn off for easier gameplay
    public bool grabLock = false;//locking behavior that is too confusing to use
    Vector3 newVelocity = Vector3.zero;
    Vector3 newAngularVelocity = Vector3.zero;
    private bool xrDeviceConfirmed = false;
    [SerializeField] private bool switchBladeOffWhenDetached = true;
    JointConnection jc;
    [SerializeField] private string controllerName = "";
    private int controllerJoystickId = 0;
    [SerializeField] private float xRotationOffset = 0;
    [SerializeField] private float yPositionOffset = 0;
    [SerializeField] private float zPositionOffset = 0;
    [SerializeField] private string renderModelName = "";

    void Start ()
    {
        blade = BLINDED_AM_ME.MeshCut.GetBladeGameObject();
        if (blade)
        {
            bladeMeshRenderer = blade.GetComponent<MeshRenderer>();
            bladeAnimator = blade.GetComponent<Animator>();
            bladeControl = blade.GetComponent<lightSaberControl>();
        }

        if (saberGeo)
        {
            saberRen = saberGeo.GetComponent<Renderer>();
        }

        weaponRB = weapon.GetComponent<Rigidbody>();
        _weaponTransform = weapon.transform;
        _controllerTransform = controller.transform;
        if (UnityEngine.XR.XRDevice.isPresent && useController){
            hand = controller;
        } else {
            hand.transform.position = Camera.main.transform.position + Camera.main.transform.forward + (Camera.main.transform.right * .5f);
            hand.transform.Rotate(Vector3.up * -15f);
            hand.transform.SetParent(Camera.main.transform);
        }
        CapsuleCheck();
        if (!bladeOn)
            saberRen.material.SetColor ("_EmissionColor", Color.black);
        catchClips = Resources.LoadAll<AudioClip>("_Sounds/catchSounds");//Resources.LoadAll("Sounds/metalSounds", typeof(AudioClip)) as AudioClip[];
        controller = HandednessHack.VirtualSteamVRRightController;
        //Invoke("CheckJoysticks", 5);//don't know why this is here!

	}
    /*void CheckJoysticks(){

        print ("joysticks");
        string[] joysticks = UnityEngine.Input.GetJoystickNames();
        if (joysticks.Length < 1)
            Debug.Log("no joysticks in list");
        foreach (string item in joysticks )
        {
            print(item);
        }
    }*/

    /*
    bool GetControllerPalmSqueeze(string side) {
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
    */

    void CapsuleCheck()
    {
        CapsuleCollider cc;
        if (!(cc = hand.GetComponent<CapsuleCollider>()))
            cc = hand.AddComponent<CapsuleCollider>();
        
        cc.radius = .05f;
        cc.height = .2f;
        cc.isTrigger = true;
        cc.direction = 2;
        if (!hand.GetComponent<Rigidbody>())
            rb = hand.AddComponent<Rigidbody>();
        else
            rb = hand.GetComponent<Rigidbody>();
        if (UnityEngine.XR.XRDevice.isPresent && useController)
            rb.isKinematic = false;
        rb.useGravity = false;
    }

	IEnumerator nudge(Vector3 dir, float force, float time){
        if (weaponRB.velocity.sqrMagnitude < 4) {
            float timer = 0;
            weaponRB.velocity += Vector3.up * .5f;
            weaponRB.velocity += dir * .2f;

            while (timer < time) {
                timer += _deltaTime;
                weaponRB.AddForce (dir * force);
                yield return null;
            }
        }
        Invoke ("ResetSense", Random.Range (.25f, 2f));
    }

    void ResetSense()
    {
        isPreviousSense = false;
    }

    void CheckControllerVisibility() {
        if (!HandednessHack.VirtualSteamVRRightController.GetComponent<FixedJoint>() /*== null*/)
            SetControllerVisibility(HandednessHack.VirtualSteamVRRightController, true);
        else
            SetControllerVisibility(HandednessHack.VirtualSteamVRRightController, false);
        if (!HandednessHack.VirtualSteamVRLeftController.GetComponent<FixedJoint>() /*== null*/)
            SetControllerVisibility(HandednessHack.VirtualSteamVRLeftController, true);
        else
            SetControllerVisibility(HandednessHack.VirtualSteamVRLeftController, false);
    }

    bool GetForcePullInput() {
        bool buttonAPress = false;

        if (device != null) {
            //Debug.Log("type Valve.VR.EVRButtonId.k_EButton_A = " + (string) myTypeStr);
            if (device.GetPressDown(Valve.VR.EVRButtonId.k_EButton_A))
                Debug.Log("ButtonA");
            if (device.GetPress(Valve.VR.EVRButtonId.k_EButton_A))
                buttonAPress = true;
        }
        //k_EButton_System = 0,
        //k_EButton_ApplicationMenu = 1,
        //k_EButton_Grip = 2,
        //k_EButton_DPad_Left = 3,
        //k_EButton_DPad_Up = 4,
        //k_EButton_DPad_Right = 5,
        //k_EButton_DPad_Down = 6,
        //k_EButton_A = 7,
        //k_EButton_ProximitySensor = 31,
        //k_EButton_Axis0 = 32,
        //k_EButton_Axis1 = 33,
        //k_EButton_Axis2 = 34,
        //k_EButton_Axis3 = 35,
        //k_EButton_Axis4 = 36,
        //k_EButton_SteamVR_Touchpad = 32,
        //k_EButton_SteamVR_Trigger = 33,
        //k_EButton_Dashboard_Back = 2,
        //k_EButton_Max = 64,

        //space bar, rift A button, rift right controller back stick, vive right touchpad back and press.
        if (isWMR) {
            if (Input.GetAxis("WMR_RightTrackpadVertical") < -.95)
                Debug.Log("wmr righttouchpadVert down");
            if (Input.GetButton("WMR_RightTrackpadPress"))//registers when just touched
                Debug.Log("wmr righttouchpadPress");

        }
        if (!flippedHands)
        {
            if (device != null)
                if (device.GetPress((ulong)4) && !(isRift || isWMR))
                {
                    isKnuckles = true;//no longer valid
                   return true;
                }
            if (
                Input.GetKey(KeyCode.Space)
                || (buttonAPress && isRift)
                //|| (device.GetPress((ulong)4) && !(isRift || isWMR))//knuckles b button and grips, and also vive grips and probably rift grips
                || (Input.GetAxis("HTC_VIU_RightTrackpadVertical") < -.5 && Input.GetButton("Vive_RightTrackpadPress") && !(isRift || isWMR))
                || (isWMR && ((Input.GetAxis("WMR_RightTrackpadVertical") < -.5 && Input.GetButton("WMR_RightTrackpadPress") && Input.GetKey(KeyCode.JoystickButton9)) ))
                || (Input.GetAxis("Oculus Touch Right Palm Squeeze") > .95)//somewhat redundant with device.getpress((ulong)4)
                //since wmr touchpad touch is being registered as a press and touch is never registered I'm switching to fire1
                //which steps on the blade on/off functionality which actually works great
                )
                return true;
        } else {
            if (device != null)
                if (device.GetPress((ulong)4) && !(isRift || isWMR))//knuckles b, grips, doesn't need to be isIndex because knuckles can be used with other headsets
                {
                   //isKnuckles = true;
                   return true;
                }
            if (
                Input.GetKey(KeyCode.Space)
                || (buttonAPress && isRift)
                //|| (device.GetPress((ulong)4) && !(isRift || isWMR))//knuckles b, grips
                || (Input.GetAxis("HTC_VIU_LeftTrackpadVertical") < -.5 && Input.GetButton("Vive_LeftTrackpadPress") && !(isRift || isWMR))
                || (isWMR && ((Input.GetAxis("WMR_LeftTrackpadVertical") < -.5 && Input.GetButton("WMR_LeftTrackpadPress")) && Input.GetKey(KeyCode.JoystickButton8) ))
                || (Input.GetAxis("Oculus Touch Left Palm Squeeze") > .95)//somewhat redundant with device.getpress((ulong)4)
                )
                return true;
        }

        if (HandednessHack.GetPostSwitchSaberPull()) return true;
        
        return false;

    }

    // bool GetControllerTriggerPull() {
    //     if (!flippedHands)
    //     {
    //         if (Input.GetAxis("Oculus Touch Right Trigger Squeeze") > .5)
    //             return true;
    //     } else {
    //         if (Input.GetAxis("Oculus Touch Left Trigger Squeeze") > .5)
    //             return true;
    //     }
    //     return false;
    // }

    /*
    void DesperatelySearchForButtonInputs(){
        if (device != null){
            for (int i = 0; i<100; i++){
                //Debug.Log("test" + i.ToString());
                if (device.GetPressDown((EVRButtonId)i))
                    Debug.Log("device button pressed for id: " + i.ToString());
            }
        }
    }
    */
    float _deltaTime;
    bool controllerOffsetEstablished = false;
    void Update() {
        _deltaTime = Time.deltaTime;
        //SteamVR_RenderModel [] rendermodels = controller.GetComponentInChildren<SteamVR_RenderModel>();
        if (controller != null && !controllerOffsetEstablished)//this is wasting a lot of cpu time, maybe I did it because I have one index controller
        {
            if (isOculusQuest){//hack
                xRotationOffset = -45;
                yPositionOffset = .05f;
                zPositionOffset = .05f;//.2 made the saber go down
                controllerOffsetEstablished = true;
            }

            SteamVR_RenderModel rendermodel = controller.GetComponentInChildren<SteamVR_RenderModel>();
            if (rendermodel != null)
            {
                if (renderModelName != rendermodel.renderModelName)
                {
                    renderModelName = rendermodel.renderModelName;
                    if (renderModelName != null)
                    {
                        controllerOffsetEstablished = true;
                        if (renderModelName.Contains("indexcontroller"))
                        {
                            xRotationOffset = -15;
                            yPositionOffset = .02f;
                        }
                        else if (isOculusQuest)
                        {
                            xRotationOffset = -45;
                            yPositionOffset = 0;//.02f;
                        }
                        else
                        {
                            xRotationOffset = 0;
                            yPositionOffset = 0;
                        }
                    }
                }
            }
        }
        if (Time.timeScale >= .99)//hacky way to make sure you can't do shit while the game is paused
        {
            if (controller != HandednessHack.VirtualSteamVRRightController)
            {
                if (controller != null)
                {
                    isRecentlySwitchedHands = true;
                    triggerTimer = 1;
                    isPreviousTriggerSqueeze = true;
                    isKnuckles = false;//weird case when someone has multiple types of controllers
                }

                HapticsTesting.SetBuzzingRemotely(controllerSide, false);//stop buzz during switchover
                flippedHands = HandednessHack.flippedHands;
                if (flippedHands)
                    controllerSide = "left";
                else
                    controllerSide = "right";
                controller = HandednessHack.VirtualSteamVRRightController;
                _controllerTransform = controller.transform;

                if (UnityEngine.XR.XRDevice.isPresent && useController)
                {
                    DetachJoint(hand);
                    hand = controller;
                }

                CapsuleCheck();
            }

            if ((alwaysGrab || isRecentlySwitchedHands) && !isHoldingSaberRt && UnityEngine.XR.XRDevice.isPresent /*&& Time.timeSinceLevelLoad > 2*/)
            {
                isRecentlySwitchedHands = false;
                StartCoroutine(AttachJoint(hand, weapon));
            }


            if (UnityEngine.XR.XRDevice.isPresent && !xrDeviceConfirmed)
            {
                xrDeviceConfirmed = true;
                //Debug.Log(UnityEngine.XR.XRDevice.model);
                //Debug.Break();
                string vrDeviceModel = UnityEngine.XR.XRDevice.model;

                if (vrDeviceModel.StartsWith("Acer") ||
                    vrDeviceModel.Contains("Windows") ||
                    vrDeviceModel.Contains("Lenovo") ||
                    vrDeviceModel.Contains("HP") ||
                    vrDeviceModel.Contains("Samsung"))
                {
                    isWMR = true;
                    //isRift = true;//used this erroneously to support angled pointer
                }
                else if (vrDeviceModel.StartsWith("Oculus"))//"Oculus Rift CV1")
                {
                    isRift = true;
                    if (vrDeviceModel.Contains("Quest")){
                        isOculusQuest = true;
                    }
                }
                else if (vrDeviceModel.StartsWith("Index"))
                {
                    isIndex = true;
                }
            }

            Vector3 pointerDirection;
            if ((pointerDirectionStyle == pointerDirectionStyles.controllerAngledDown45) && (isRift || isWMR))
                pointerDirection = (_controllerTransform.forward - _controllerTransform.up).normalized;
            else
                pointerDirection = _controllerTransform.forward;

            float myDot = Vector3.Dot(pointerDirection, (_weaponTransform.position - (_controllerTransform.position - (.1f * pointerDirection))).normalized);


            if ((myDot >= 1f - (frustumAngle / 180f) || !isDirectional) || !UnityEngine.XR.XRDevice.isPresent)
                isCurrentSense = true;
            else
                isCurrentSense = false;

            /*
            if (isCurrentSense && !isPreviousSense)
            {//nudge the saber and make a haptic pulse when you sense the saber
                if (fj == null)
                {
                    StartCoroutine(HapticsTesting.GrabBuzz(controllerSide, 2f));
                    StartCoroutine(nudge((_controllerTransform.position - _weaponTransform.position).normalized, 20f, .1f));
                }
            }
            */


            if ((blade.activeInHierarchy && bladeMeshRenderer.enabled) && hand.GetComponent<FixedJoint>())
                HapticsTesting.SetBuzzingRemotely(controllerSide, true);
            else
                HapticsTesting.SetBuzzingRemotely(controllerSide, false);

            trackedObj = controller.GetComponent<SteamVR_TrackedObject>();
            if (trackedObj != null)
            {
                deviceIndex = (int)trackedObj.index;
                if (deviceIndex > -1)
                    device = SteamVR_Controller.Input(deviceIndex);
            }
            trackedObjLf = gameObject.GetComponent<HandednessHack>().GetLeftController().GetComponent<SteamVR_TrackedObject>(); ;
            if (trackedObjLf != null)
            {
                int deviceIndexLf = (int)trackedObjLf.index;
                if (deviceIndexLf > -1)
                    deviceLf = SteamVR_Controller.Input(deviceIndex);//sometimes getting an outofbounds error
            }
            
            isSpace = GetForcePullInput(); //check for force grab

            //check blade on/off switch
            isSaberButtonPush = false;
            bool buttonAPress = false;
            if (device != null)
            {
                if (device.GetPressDown(Valve.VR.EVRButtonId.k_EButton_A))
                    buttonAPress = true;
            }
            if (flippedHands)
            {
                if (Input.GetButton("Fire3") && !isOculusQuest)
                    isSaberButtonPush = true;
            }
            else
            {
                if (Input.GetButton("Fire1") && !isOculusQuest)//quest hack, find a more specific way to handle this
                    isSaberButtonPush = true;
            }

            if (!isSpace)
            {
                buttonTimer += _deltaTime;
                tossingVelocityTimer += _deltaTime;
            }
            if (!isSaberButtonPush)
                triggerTimer += _deltaTime;

            if ((isSaberButtonPush && !isPreviousTriggerSqueeze && triggerTimer < .25) && hand.GetComponent<FixedJoint>() || Input.GetKeyDown(KeyCode.Z))
            {//double-trigger
                bladeOn = !bladeOn;
                if (bladeOn)
                {
                    if ((!blade.activeInHierarchy || !bladeMeshRenderer.enabled) || !bladeAnimator.GetBool("isOn"))
                        bladeControl.MyEnable();
                    saberRen.material.SetColor("_EmissionColor", Color.white * 10f);
                }
                else
                    saberRen.material.SetColor("_EmissionColor", Color.black);
            }


            if (isSaberButtonPush && hand.GetComponent<FixedJoint>() && !isPreviousTriggerSqueeze)
            {//on/off behavior
                if (blade.activeInHierarchy && bladeMeshRenderer.enabled)
                {//if the blade is halfway on or off, toggle it
                    if (bladeAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "bladeOn")
                    {
                        bladeControl.MyDisable();
                    }
                    else
                    {
                        if (!bladeAnimator.GetBool("isOn"))
                        {
                            bladeControl.MyEnable();
                        }
                    }
                }
                else
                {//if the blade isn't on just switch it on
                    if (!bladeAnimator.GetBool("isOn"))
                    {
                        bladeControl.MyEnable();
                        //Debug.Log("RemoteGrab:MyEnable");
                    }
                }
            }
            if (isSpace && !isSpacePrevious && buttonTimer < .25)
            {
                alwaysGrab = !alwaysGrab;
                grabLock = alwaysGrab;
            }

            isSpacePrevious = isSpace;
            isPreviousTriggerSqueeze = isSaberButtonPush;
            if (isSpace)
                buttonTimer = 0;//reset timers at the end so you can process double-clicks first
            if (isSaberButtonPush)
                triggerTimer = 0;
            isPreviousSense = isCurrentSense;

            //was FixedUpdate

            float dist = Vector3.Distance(_weaponTransform.position, rb.position);

            //need to fix this. If isPulling and you lose the frustum, it should still be pulling i.e. stickyGrab
            //what is prePullVelocity and how am I using isPulling?
            if (isSpace && (isCurrentSense || isPulling || alwaysGrab)/*|| isStickyGrab*/)
            {//stickyGrab is just holding down the button, replace this with a joint that only detaches when there is enough velocity
                if (fj == null)
                {//if detached
                    isPulling = true;//starts auto-pulling to grab the saber I think
                    if (dist < .1)//was .1 tried .05 but it would wiggle too much
                    {//saber relatively still
                        StartCoroutine(AttachJoint(hand, weapon));
                    }
                    else
                    {
                        Vector3 weaponToHandDirection = (rb.position - _weaponTransform.position).normalized;
                        float closeupPullDamper = 1;// Mathf.Lerp(.1f, 1, Vector3.Distance(_weaponTransform.position, rb.position) * 10f);
                        if (dist < .2)
                            closeupPullDamper = Mathf.Lerp(.1f, 1, Vector3.Distance(_weaponTransform.position, rb.position) * 5f);

                        //weaponRB.velocity = (prePullVelocity + weaponToHandDirection * grabVelocity) * closeupPullDamper;
                        weaponRB.velocity *= .5f;
                        weaponRB.velocity += weaponToHandDirection * (grabVelocity * closeupPullDamper);
                        if (dist < 3)
                        {
                            float rotationSpeed;
                            if (dist < 2) rotationSpeed = Mathf.Lerp(100f, 10f, dist / 2f);
                            else rotationSpeed = 10f;
                            weaponRB.rotation = Quaternion.Slerp(weaponRB.rotation, rb.rotation, Time.fixedDeltaTime * rotationSpeed);
                        }
                        prePullVelocity *= .95f;
                        StartCoroutine(HapticsTesting.GrabBuzz(controllerSide, .2f));

                    }
                }
            }
            else
            {
                if (fj != null)
                {//if currently holding the saber you can toss it
                 //controller.transform.Find ("Model").gameObject.SetActive (false);//not sure why this was here
                    if (!useController)
                    {
                        DetachJoint(hand);
                    }
                    else
                    {
                        if (!isSpace && (buttonTimer < tossReleaseTime || !grabLock))//within .1 sec of a button release can toss the saber
                        {
                            UpdateNewVelocities(weapon);

                            if (newVelocity.magnitude > tossVelThreshold || newAngularVelocity.magnitude > tossAVelThreshold)
                                if (newVelocity.magnitude > tossVelThreshold)

                                    if (alwaysGrab)
                                        alwaysGrab = false;//disable autograb if you release properly //super advanced behavior
                            tossingVelocityTimer = 0;
                            if (tossingVelocityTimer < tossReleaseTime)
                            {
                                if (useController && !alwaysGrab)
                                { //hacky, prevents double-grab
                                    DetachJoint(hand);
                                    TossNew(weapon,tossAngularMultiplier);
                                }
                            }
                        }
                    }


                }
                isPulling = false;
            }
            if (!isPulling)
                prePullVelocity = weaponRB.velocity;
            if (fj == null)
                isHoldingSaberRt = false;
            else
                isHoldingSaberRt = true;
            previousPos = weaponRB.position;// _weaponTransform.position;
            previousRotation = weaponRB.rotation;//_weaponTransform.rotation;
            SaberTossDataAddToArray(ref tossSamples, new SaberTossData(weaponRB.position, weaponRB.rotation, _deltaTime));
            CheckControllerVisibility();//this works but

        }//TimeScale check

    }
    private void SaberTossDataAddToArray(ref SaberTossData[] dataArray, SaberTossData data)
    {
        for (int i = dataArray.Length-1; i>0; i--)
        {
            dataArray[i] = dataArray[i-1];
        }
        dataArray[0] = data;
    }

    SaberTossData[] tossSamples = new SaberTossData[4];//4 felt good, 10 feels sluggish but why?
    // I think it's because a throw happens in the last 30 - 50 milliseconds 10 frames of data is more than 100ms so I would be capturing data from before there is much movement
    // the opposite was true too, when I wasn't sampling farther back than one frame, right at the end of the throw, I was sometimes capturing the arm's breaking action and missing the throw
    public struct SaberTossData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float deltaTime;
        public SaberTossData(Vector3 pos, Quaternion rot, float dt)
        {
            position = pos;
            rotation = rot;
            deltaTime = dt;
        }
    }

    void SetControllerVisibility(GameObject go, bool visible) {
        foreach (SteamVR_RenderModel model in go.GetComponentsInChildren<SteamVR_RenderModel>())
        {
            foreach (var child in model.GetComponentsInChildren<MeshRenderer>())
                child.enabled = visible;
        }
    }

    public void PutSaberInHandNow() { //accessed by menuToggle
        StartCoroutine(AttachJoint(hand, weapon));
    }

    IEnumerator AttachJoint(GameObject source, GameObject target){
        //changed this to a coroutine because I wanted to adjust the position of the target object before freezing it
        //with a fixed joint. I was doing this in Rigidbody.position and rotation but that doesn't update until late in the frame which
        //I guess happens after the constraint gets applied.
        //I guess I fixed it in the transform.

        //new issue, sometimes after handedness switching it doesn't attach and instead shoots the saber far away
        //but after the saber is comfortably held in either hand it works flawlessly

        Rigidbody targetRB = target.GetComponent<Rigidbody>();
        Transform targetTransform = target.transform;
        Transform sourceTransform = source.transform;

        if (fj != null && fj.connectedBody == targetRB)
        {
            yield return null;//seems pretty sloppy coding //look into this, not sure why this is a coroutine anymore...
        } else {
            bool done = false;
            source.GetComponent<Rigidbody>().isKinematic = true;
            targetTransform.rotation = sourceTransform.rotation;
            targetTransform.Rotate(Vector3.right, xRotationOffset);
            targetTransform.position = sourceTransform.position;
            targetTransform.position += yPositionOffset * sourceTransform.up + zPositionOffset * sourceTransform.forward;
            if (fj == null)
                fj = source.AddComponent<FixedJoint>() as FixedJoint;
            fj.connectedBody = targetRB;

            //there's a problem setting non-infinite breakforce and breakTorque... jointConnection becomes invalid
            //if breaking the joint results in losing the jointconnection, the blade will shut off but still be basically held in your hand which is really weird
            //fj.breakForce = 5000f;//high but not infinite
            //fj.breakTorque = 5000f;//high but not infinite
            if (!target.GetComponent<JointConnection>())
            {
                jc = target.AddComponent<JointConnection>();
                jc.joint = fj;
            }

            if ((bladeOn || isSaberButtonPush) && !bladeAnimator.GetBool("isOn"))
                bladeControl.MyEnable();//is this redundant?

            SetControllerVisibility(controller,false);
            StartCoroutine(HapticsTesting.GrabBuzz(controllerSide, 1f));
            PlayCatchSound(target);
        }
    }

    void PlayCatchSound(GameObject targetObject)
    {
        AudioSource ass = targetObject.GetComponent<AudioSource>();
        Debug.Log("catch audio source " + ass.name);
        if (ass != null)
        {
            int clipNum = 0;
            if (catchClips.Length > 0){
                clipNum = Random.Range(0,catchClips.Length);
                ass.clip = catchClips[clipNum];
            }
            ass.pitch = Random.Range(.8f,1.1f);
            ass.Play();
        }
    }
    bool CheckJoint(GameObject source, GameObject target){ //forces the blade to stay anchored to the hand but this should generate a dramatic effect
        FixedJoint fj = source.GetComponent<FixedJoint>();
        Transform targetTransform = target.transform;
        Transform sourceTransform = source.transform;
        if (fj)
            {
                if (targetTransform.rotation!=sourceTransform.rotation)
                    {
                        targetTransform.rotation=sourceTransform.rotation;
                        return false;
                    }
            } else {
                return false;
            }
        return true;
    }

    void DetachJoint(GameObject source)
    {
        if (fj != null){
            Destroy(fj.connectedBody.gameObject.GetComponent<JointConnection>());
            Destroy(fj);
        }

        if (bladeAnimator.GetBool("isOn") && switchBladeOffWhenDetached)
        {
            bladeControl.MyDisable();
        }
        if (UnityEngine.XR.XRDevice.isPresent && useController)
            source.GetComponent<Rigidbody> ().isKinematic = false;
        SetControllerVisibility(source, true);
    }

    void TossNew(GameObject target)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Transform targetTransform = target.transform;
        
        Vector3 vel = (targetRb.position - previousPos)/ _deltaTime * 1.25f;//cheater
        targetRb.velocity = vel;

        Quaternion itemRotation = targetRb.rotation;
        Quaternion deltaRotation = itemRotation * Quaternion.Inverse(previousRotation);
        previousRotation = itemRotation;
        float angle  = 0.0f;
        Vector3 axis = Vector3.zero;
        deltaRotation.ToAngleAxis(out angle, out axis);
        angle *= Mathf.Deg2Rad;
        targetRb.angularVelocity = axis * angle * (1.0f / _deltaTime);
        
    }

    void TossNew(GameObject target, float angularMultiplier)//multiplying the angular velocity isn't really helping, 
        //I think I'm not detecting a large deltaRotation because I'm not twisting my wrist. 
        //I need to calculate the rotation based on the length of my arm to the elbow
        //and maybe I should sample more points over multiple frames
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Transform targetTransform = target.transform;
        Vector3 currentPos = targetRb.position;

        float earliestDeltaTime = _deltaTime;
        int over50Index = tossSamples.Length - 1;
        for (int i = 0; i < tossSamples.Length - 1; i++)//skip the last one
        {
            earliestDeltaTime += tossSamples[i].deltaTime;
            if (earliestDeltaTime > .050)//was 50 big oops
            {
                over50Index = i;//limit going farther back than 50ms in case framerate is slow
                break;
            }
        }

        SaberTossData earliestSample = tossSamples[over50Index];
        Quaternion earliestRotation = earliestSample.rotation;
        Vector3 earliestPosition = earliestSample.position;


        //Vector3 vel = (currentPos - previousPos) / _deltaTime * 1.25f;//cheater
        Vector3 vel = (currentPos - earliestPosition) / earliestDeltaTime;
        targetRb.velocity = vel * 1.25f;//cheater

        Quaternion itemRotation = targetRb.rotation;
        Quaternion deltaRotation = itemRotation * Quaternion.Inverse(earliestRotation);//previousRotation
        previousRotation = itemRotation;//not really being used anymore
        float angle = 0.0f;
        Vector3 axis = Vector3.zero;
        deltaRotation.ToAngleAxis(out angle, out axis);
        angle *= Mathf.Deg2Rad;
        targetRb.angularVelocity = axis * (angle * (1.0f / _deltaTime) * angularMultiplier);

    }

    void UpdateNewVelocities(GameObject target){
        Transform targetTransform = target.transform;

        newVelocity = (targetTransform.position - previousPos) / _deltaTime;// * 1.25f;//cheater

        Quaternion itemRotation = targetTransform.rotation;
        Quaternion deltaRotation = itemRotation * Quaternion.Inverse(previousRotation);
        //previousRotation = itemRotation;
        float angle  = 0.0f;
        Vector3 axis = Vector3.zero;
        deltaRotation.ToAngleAxis(out angle, out axis);
        angle *= Mathf.Deg2Rad;
        newAngularVelocity = axis * (angle * (1.0f / _deltaTime));

    }

}


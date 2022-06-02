using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class Framerate : MonoBehaviour {
    private TextMesh text;
    private float fps = 0;
    private float smoothFps = 90;
    private enum displayTypes {Framerate, Timer, NumChunks, NumInstances, NumCuts, XRDevice, Buttons, SteamVRButtons}; 
    [SerializeField] private displayTypes displayType;
    [SerializeField] private float fpsInterval = .2f;
    private float timer=0;
    private int lastFrameCount=0;
    private float lastTime=0;
    private Transform _CameraTransform;
    [SerializeField] private bool isFollowCamera = false;
    [SerializeField] private Vector3 followOffset;
    [SerializeField] private float speed = 15f;
    private float time = 0;
    private float _deltaTime=0;
    [SerializeField] Gradient g;
    private int numChunks=-1;
    private int chunksPoolSize=0;
    private string xrDeviceName = "";
    private System.Array keyCodes;
    private string[] joystickAxes;
    [SerializeField] private bool filterOutRedundantJoystick = true;
    MeshRenderer mr;
    [SerializeField] GameObject controllerLf;
    [SerializeField] GameObject controllerRt;
    SteamVR_TrackedObject trackedObjRt; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_Controller.Device deviceRt; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_TrackedObject trackedObjLf; //steamvr 1.2.3, remove for steamvr 2.x
    SteamVR_Controller.Device deviceLf; //steamvr 1.2.3, remove for steamvr 2.x
    private int localMeshCutInstances = -1;

	void Start () {
        text = GetComponent<TextMesh> ();
        lastFrameCount = Time.frameCount;
        lastTime = Time.time;
        _CameraTransform = Camera.main.transform;
        if (UnityEngine.XR.XRDevice.isPresent) {
            xrDeviceName = UnityEngine.XR.XRDevice.model;
            if (xrDeviceName != "" && displayType == displayTypes.XRDevice)
                text.text = xrDeviceName;

            GameObject head = GameObject.Find ("Camera (head)");
            if (head != null) {
                _CameraTransform = head.transform;
            }
        }
        //g = new Gradient();
        chunksPoolSize = BLINDED_AM_ME.MeshCut.GetChunksPoolSize ();
        keyCodes = System.Enum.GetValues(typeof(KeyCode));
        mr = GetComponent<MeshRenderer>();
        Refresh();
    }

    //not to my future self- you spent a long time writing this script
    //you will be wondering why it doesn't display the same information as the stats
    //display in the unity editor. just ignore that. you are recording the time and framecount
    //since the last fps update and you are doing the right math. trust the math.
    //don't necessarily trust the time passed. Might be more accurate in a build.
    void Fps (){
       // if (timer > fpsInterval) {
            fps = (Time.frameCount - lastFrameCount) / (Time.time - lastTime);
            text.text = fps.ToString ("0") + " FPS";
            //timer = 0;
            lastFrameCount = Time.frameCount;
            lastTime = Time.time;
            if (fps > 0)
                smoothFps = fps;

            if (smoothFps >= 0 && smoothFps <= 90)
            text.color = g.Evaluate (smoothFps/90 );
        //}
    }
    void NumChunks(){
        int newChunks = BLINDED_AM_ME.MeshCut.GetNumActiveChunks ();
        if (newChunks != numChunks || timer > fpsInterval ) {
            chunksPoolSize = BLINDED_AM_ME.MeshCut.GetChunksPoolSize ();
            timer = 0;
            text.text = newChunks.ToString ("0") + "/" + chunksPoolSize.ToString() + " Chunks";
            numChunks = newChunks;
        }
        if (numChunks >= 0 && chunksPoolSize > 0)
        text.color = g.Evaluate ((float)numChunks/chunksPoolSize);
    }

    void NumInstances(){
        int newChunks = BLINDED_AM_ME.MeshCut.numMeshCutInstances;
        if (newChunks != localMeshCutInstances || timer > fpsInterval ) {
            chunksPoolSize = BLINDED_AM_ME.MeshCut.GetChunksPoolSize();
            timer = 0;
            text.text = newChunks.ToString ("0") + "/" + chunksPoolSize.ToString() + " Instances";
            numChunks = newChunks;
        }
        text.color = g.Evaluate ((float)numChunks/chunksPoolSize);
    }

    string infoPersistent = "";
    void NumCutsInQueue() {
        int cuts = ToolUser.GetNumCutsInQueue();
        int cuttempt = ToolUser.GetNumCutAttempts();
        if (cuts != null)
        {
            //text.text = cuts.ToString() + " inQ\n" + cuttempt.ToString() + " attempts\n" + BLINDED_AM_ME.MeshCut.GetTriPointsStartIndex() +  " triPoints\n" + ToolUser.GetCutName();
            text.text = cuts.ToString() + " inQ\n" + cuttempt.ToString() + " attempts\n";
            string info = BLINDED_AM_ME.MeshCut.GetCutInfo();
            if (info != "")
                infoPersistent = info;
            text.text += infoPersistent;
        }
        else
            text.text = "null";
    }

    void Buttons ()
    {
        text.text = "";

        if (Input.anyKey)
        {
            foreach(KeyCode kcode in keyCodes)
            {
                if (!kcode.ToString().StartsWith("JoystickButton") || !filterOutRedundantJoystick)
                if (Input.GetKey(kcode))
                {
                    text.text += kcode.ToString();
                    text.text += "\n";
                }
            }
        } 

        for (int i = 3; i < 28; i++){
            string joystickAxisName = "Axis" + i.ToString();
            float axis = Input.GetAxis(joystickAxisName);
            if (Input.GetAxis(joystickAxisName) > 0.5 || Input.GetAxis(joystickAxisName) < -0.5)
                {
                    if (text.text.EndsWith("\n"))
                        text.text += "Joystick";
                    text.text += joystickAxisName+" "+ axis.ToString();
                }
        }
        if (Input.GetAxis("Horizontal") > 0.5 || Input.GetAxis("Horizontal") < -0.5)
            text.text += "Horizontal";
        if (Input.GetAxis("Vertical") > 0.5 || Input.GetAxis("Vertical") < -0.5)
            text.text += "Vertical";

        if (text.text == "")
            text.text = "---";

    }

    string GetSteamConstant(int buttonIndex)
    {
        string steamConst = "";
        switch (buttonIndex){
            case 0:
            steamConst = "k_EButton_System";
            break;
            case 1:
            steamConst ="k_EButton_ApplicationMenu";
            break;


            default:
            steamConst = "";
            break;

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


        }
        return steamConst;
    }

    void SteamVRAxes ()
    {
        if (trackedObjRt == null){
            if (controllerRt != null) trackedObjRt = controllerRt.GetComponent<SteamVR_TrackedObject>();
            if (trackedObjRt != null)
            {
                int deviceIndexRt = (int)trackedObjRt.index;
                if (deviceIndexRt > -1)
                    deviceRt = SteamVR_Controller.Input(deviceIndexRt);
            }
        }
        if (trackedObjLf == null){
            if (controllerLf != null) trackedObjLf = controllerLf.GetComponent<SteamVR_TrackedObject>(); ;
            if (trackedObjLf != null)
            {
                int deviceIndexLf = (int)trackedObjLf.index;
                if (deviceIndexLf > -1)
                    deviceLf = SteamVR_Controller.Input(deviceIndexLf);
            }
        }

        for (int i = 32; i<= 36; i++)//SteamVR_Controller::Device.GetAxis() only sees 5 axes
        {
            if (deviceLf != null){
                //if (deviceLf.GetPress((ulong)i))
                //    text.text += "SteamVRButtonLf(" + i.ToString() + ")\n";
                Vector2 axis = deviceLf.GetAxis((Valve.VR.EVRButtonId)i);
                if (axis.x > 0 || axis.x < 0)
                    text.text += "SteamVRAxisXLf(" + i.ToString() + ") " + axis.x.ToString() + "\n";
                if (axis.y > 0 || axis.y < 0)
                    text.text += "SteamVRAxisYLf(" + i.ToString() + ") " + axis.y.ToString() + "\n";

                if (axis.x > 0.5)
                    text.text += "SteamVRAxisX+Lf(" + i.ToString() + ")\n";
                if (axis.x < -0.5)
                    text.text += "SteamVRAxisX-Lf(" + i.ToString() + ")\n";
                if (axis.y > 0.5)
                    text.text += "SteamVRAxisY+Lf(" + i.ToString() + ")\n";
                if (axis.y < -0.5)
                    text.text += "SteamVRAxisY-Lf(" + i.ToString() + ")\n";
            }
            if (deviceRt != null)
            {
                Vector2 axis = deviceRt.GetAxis((Valve.VR.EVRButtonId)i);
                if (axis.x > 0 || axis.x < 0)
                    text.text += "SteamVRAxisXRt(" + i.ToString() + ") " + axis.x.ToString()+ "\n";
                if (axis.y > 0 || axis.y < 0)
                    text.text += "SteamVRAxisYRt(" + i.ToString() + ") " + axis.y.ToString() + "\n";

                if (axis.x > 0.5)
                    text.text += "SteamVRAxisX+Rt(" + i.ToString() + ")\n";
                if (axis.x < -0.5)
                    text.text += "SteamVRAxisX-Rt(" + i.ToString() + ")\n";
                if (axis.y > 0.5)
                    text.text += "SteamVRAxisY+Rt(" + i.ToString() + ")\n";
                if (axis.y < -0.5)
                    text.text += "SteamVRAxisY-Rt(" + i.ToString() + ")\n";
            }
        }

        if (text.text == "")
            text.text = "+++";

    }

    void SteamVRButtons ()
    {
        if (trackedObjRt == null){
            if (controllerRt != null) trackedObjRt = controllerRt.GetComponent<SteamVR_TrackedObject>();
            if (trackedObjRt != null)
            {
                int deviceIndexRt = (int)trackedObjRt.index;
                if (deviceIndexRt > -1)
                    deviceRt = SteamVR_Controller.Input(deviceIndexRt);
            }
        }
        if (trackedObjLf == null){
            if (controllerLf != null) trackedObjLf = controllerLf.GetComponent<SteamVR_TrackedObject>(); ;
            if (trackedObjLf != null)
            {
                int deviceIndexLf = (int)trackedObjLf.index;
                if (deviceIndexLf > -1)
                    deviceLf = SteamVR_Controller.Input(deviceIndexLf);
            }
        }

        ulong[] steamButtonIDs ={
            SteamVR_Controller.ButtonMask.System,
            SteamVR_Controller.ButtonMask.ApplicationMenu,
            SteamVR_Controller.ButtonMask.Grip,
            SteamVR_Controller.ButtonMask.Axis0,
            SteamVR_Controller.ButtonMask.Axis1,
            SteamVR_Controller.ButtonMask.Axis2,
            SteamVR_Controller.ButtonMask.Axis3,
            SteamVR_Controller.ButtonMask.Axis4,
            SteamVR_Controller.ButtonMask.Touchpad,
            SteamVR_Controller.ButtonMask.Trigger
        };
        for (int i = 0; i<steamButtonIDs.Length; i++)
        {
            if (deviceLf != null)
            {
                if (deviceLf.GetPress (steamButtonIDs[i]))
                    text.text += "\nSteamVRButtonLf(" + steamButtonIDs[i].ToString() + ")\n";
            }

            if (deviceRt != null)
            {
                if (deviceRt.GetPress (steamButtonIDs[i]))
                    text.text += "\nSteamVRButtonRt(" + steamButtonIDs[i].ToString() + ")\n";
            }
        }

        //Debug.Log("type Valve.VR.EVRButtonId.k_EButton_A = " + (string) myTypeStr);
        //if (deviceLf.GetPressDown(Valve.VR.EVRButtonId.k_EButton_A))
        //    Debug.Log("ButtonA");     

        /*if (Input.anyKeyDown)
            Debug.Log("any key");
        foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKey(vKey))
            {
                Debug.Log(vKey.ToString());//JoystickButton9 & 17 Joystick2Button9 & 17

            }
        }*/
        //}
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

        // public class SteamVR_Controller
        // {
        //     public class ButtonMask
        //     {
        // public const ulong System = (1ul << (int)EVRButtonId.k_EButton_System); // reserved
        // public const ulong ApplicationMenu = (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu);
        // public const ulong Grip = (1ul << (int)EVRButtonId.k_EButton_Grip);
        // public const ulong Axis0 = (1ul << (int)EVRButtonId.k_EButton_Axis0);
        // public const ulong Axis1 = (1ul << (int)EVRButtonId.k_EButton_Axis1);
        // public const ulong Axis2 = (1ul << (int)EVRButtonId.k_EButton_Axis2);
        // public const ulong Axis3 = (1ul << (int)EVRButtonId.k_EButton_Axis3);
        // public const ulong Axis4 = (1ul << (int)EVRButtonId.k_EButton_Axis4);
        // public const ulong Touchpad = (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad);
        // public const ulong Trigger = (1ul << (int)EVRButtonId.k_EButton_SteamVR_Trigger);

        /*
                if (Input.anyKey)
                {
                    foreach(KeyCode kcode in keyCodes)
                    {
                        if (!kcode.ToString().StartsWith("JoystickButton") || !filterOutRedundantJoystick)
                        if (Input.GetKey(kcode))
                        {
                            text.text += kcode.ToString();
                            text.text += "\n";
                        }
                    }
                } 

                for (int i = 3; i < 28; i++){
                    string joystickAxisName = "Axis" + i.ToString();
                    if (Input.GetAxis(joystickAxisName) > 0.5 || Input.GetAxis(joystickAxisName) < -0.5)
                        {
                            if (text.text.EndsWith("\n"))
                                text.text += "Joystick";
                            text.text += joystickAxisName+" ";
                        }
                }
                if (Input.GetAxis("Horizontal") > 0.5 || Input.GetAxis("Horizontal") < -0.5)
                    text.text += "Horizontal";
                if (Input.GetAxis("Vertical") > 0.5 || Input.GetAxis("Vertical") < -0.5)
                    text.text += "Vertical";
        */
        if (text.text == "")
            text.text = "+++";

    }
    void Refresh()
    {
        switch (displayType)
        {
            case displayTypes.Timer:
                float min = Mathf.Floor(time / 60);
                float sec = Mathf.Floor(time) % 60;
                text.text = min.ToString("00") + ":" + sec.ToString("00");
                break;
            case displayTypes.Framerate:
                Fps();
                break;
            case displayTypes.NumChunks:
                NumChunks();
                break;
            case displayTypes.NumInstances:
                NumInstances();
                break;
            case displayTypes.NumCuts:
                NumCutsInQueue();
                break;
            case displayTypes.XRDevice:
                break;//do nothing
            case displayTypes.Buttons:
                Buttons();
                //if (text.text != "")
                //    text.text += "\n";
                SteamVRButtons();
                SteamVRAxes();
                break;
            case displayTypes.SteamVRButtons:
                text.text = "";
                SteamVRButtons();
                break;
            default:
                Fps();
                break;
        }
    }
	void Update () {
        time = Time.timeSinceLevelLoad;
        _deltaTime = Time.deltaTime;
        timer += _deltaTime;
        smoothFps = Mathf.Lerp (smoothFps,fps,_deltaTime);

        if (mr.enabled)
        {
            if (timer > fpsInterval)
            {
                timer = 0;

                Refresh();
            }
        }

        if (isFollowCamera){
            Vector3 camPos = _CameraTransform.position;
            Vector3 newPos = camPos + _CameraTransform.forward * followOffset.z + _CameraTransform.right * followOffset.x + _CameraTransform.up * followOffset.y;
            transform.position = Vector3.Lerp(transform.position,newPos,_deltaTime * speed);
            transform.LookAt(camPos + _CameraTransform.right * followOffset.x,_CameraTransform.up);
        }
	}
}

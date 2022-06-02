using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.VR;
using UnityEngine.XR;

using Valve;
using Valve.VR;
public class HapticsTesting : MonoBehaviour {
    public static SteamVR_TrackedObject trackedObjRt;
    public static SteamVR_TrackedObject trackedObjLf;
    [SerializeField] private GameObject rightControllerGO;
    [SerializeField] private GameObject leftControllerGO;
    [SerializeField] float buzzGap = .035f;//sec
    [SerializeField] ushort buzzPulse = 500;//300 hard to feel, 500 maybe a little high or difficult to tell contrast, impossible to tell with WMR
    private float gapTimer = 0;
    [SerializeField] float hitGap = .025f;
    [SerializeField] float hitLength = .1f;

    public static float hitGapTimer = 0;
    private bool isHit = false;

    [SerializeField] bool isRemoteControlled = false;
    public static bool isBuzzingRemotelyLf = false;
    public static bool isBuzzingRemotelyRt = false;
    public static bool isStrikeLeft = false;
    public static bool isStrikeRight = false;
    public static bool isDraggingRight = false;
    public static bool isDraggingLeft = false;

    private float triggerTimer = 0;
    [SerializeField] private float triggerMaxTime = 1f;
    private bool previousTriggerPress = false;
    private bool isTriggerPress = false;
    public static bool isExploding = false;
    private float cooldownTime = 2;
    public static float oscillateTimer = 0;
    public static float sineMult = 1.65f;
    public static float weirdSinFunc = 0;
    public static float sinePower = 2;

    static bool isLeftPulsing = false;//quest
    static bool isRightPulsing = false;//quest

   public static IEnumerator PulseLf(float freq, float amp, float duration)
    {
        if (!isLeftPulsing)
        {
            isLeftPulsing = true;
            OVRInput.SetControllerVibration(freq, amp, OVRInput.Controller.LTouch);
            float timeStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - timeStart < duration)
            {
                yield return null;
            }
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
            isLeftPulsing = false;
        }
    }

    public static IEnumerator PulseRt(float freq, float amp, float duration)
    {
        if (!isRightPulsing)
        {
            isRightPulsing = true;
            OVRInput.SetControllerVibration(freq, amp, OVRInput.Controller.RTouch);
            float timeStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - timeStart < duration)
            {
                yield return null;
            }
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
            isRightPulsing = false;
        }
    }

    static void TriggerHapticPulse(string side, float duration)
    {
        if (side == "left")
        {
            if (SaberGame.isOculusOpenVR)
            {
                SaberEventManager sem = GameObject.Find("Game").GetComponent<SaberEventManager>();
                sem.StartCoroutine(GameObject.Find("Game").GetComponent<SaberEventManager>().PulseLf(1f, 1f, duration / 1000f));
            }
            else
            {
                if (trackedObjLf != null && (int)trackedObjLf.index > 0)
                    SteamVR_Controller.Input((int)trackedObjLf.index).TriggerHapticPulse((ushort)duration);
            }
        }

        else if (side == "right")
        {
            if (SaberGame.isOculusOpenVR)
            {
                SaberEventManager sem = GameObject.Find("Game").GetComponent<SaberEventManager>();
                sem.StartCoroutine(GameObject.Find("Game").GetComponent<SaberEventManager>().PulseRt(1f, 1f, duration / 1000f));            }
            else
            {
                if (trackedObjRt != null && (int)trackedObjRt.index > 0)
                    SteamVR_Controller.Input((int)trackedObjRt.index).TriggerHapticPulse((ushort)duration);
            }
        }
    }
	// Use this for initialization
	void Start () {


	}

    //.035 3999 - nice low freq light saber
    public static void SetBuzzingRemotely(string side, bool value) {
        if (side.ToLower() == "right")
            isBuzzingRemotelyRt = value;
        if (side.ToLower() == "left")
            isBuzzingRemotelyLf = value;
    }

    public static void SetStrike(string side) {
        if (side.ToLower() == "right")
            isStrikeRight = true;
        if (side.ToLower() == "left")
            isStrikeLeft = true;
    }

    public static void SetDrag(string side)
    {
        if (side.ToLower() == "right")
            isDraggingRight = true;
        if (side.ToLower() == "left")
            isDraggingLeft = true;
    }
    // Update is called once per frame
    void Update () {
        if (!SaberGame.isOculusOpenVR){
            trackedObjLf = leftControllerGO.GetComponent<SteamVR_TrackedObject>();
            trackedObjRt = rightControllerGO.GetComponent<SteamVR_TrackedObject>();
        }
        if (!isHit){
            if (isRemoteControlled) {
                if (isBuzzingRemotelyLf) {
                    if (!SaberGame.isOculusOpenVR) LeftControllerBuzz();
                    if (isStrikeLeft) {
                        if (SaberGame.isOculusOpenVR) StartCoroutine("VibrateSuperShortLf");
                        else StartCoroutine ("VibrateLongLf");
                    } else if (isDraggingLeft){
                        if (SaberGame.isOculusOpenVR) StartCoroutine("VibrateSuperShortLf");
                        else StartCoroutine("VibrateShortLf");
                    }
                }
                if (isBuzzingRemotelyRt) {
                    if (!SaberGame.isOculusOpenVR) RightControllerBuzz();
                    if (isStrikeRight) {
                        if (SaberGame.isOculusOpenVR) StartCoroutine("VibrateSuperShortRt");
                        else StartCoroutine ("VibrateLongRt");
                    } else if (isDraggingRight){
                        if (SaberGame.isOculusOpenVR) StartCoroutine("VibrateSuperShortRt");
                        else StartCoroutine ("VibrateShortRt");
                    }
                }
                isStrikeLeft = false;//hacky
                isStrikeRight = false;//hacky
                isDraggingRight = false;//hacky
                isDraggingLeft = false;//hacky

            }
            else {
                if (Input.GetButton ("Fire1")){//A Button
                    //Debug.Log("Fire1");
                    RightControllerBuzz();
                }
                if (Input.GetButtonDown ("Fire2")){
                    //Debug.Log("Fire2");
                }
                if (Input.GetButton ("Fire3")){//Y button
                    LeftControllerBuzz();
                    //Debug.Log("Fire3");
                }
                if (Input.GetButtonDown ("Jump")){
                    //Debug.Log("Jump");
                    StartCoroutine("VibrateLong");
                }
            }
        }
        if (!isRemoteControlled) {//I don't know what this condition is

            if ((Input.GetAxis ("Oculus Touch Left Trigger Squeeze") > .5) || Input.GetKey(KeyCode.P)) {
                isTriggerPress = true;
                triggerTimer += Time.deltaTime;
            } else {
                isTriggerPress = false;
                if (triggerTimer < 0)//cooldown should wind up even when the trigger isn't pressed
                    triggerTimer += Time.deltaTime;
                else
                    triggerTimer = 0;
            }
            if (isTriggerPress && triggerTimer < triggerMaxTime && triggerTimer > 0)
                LeftControllerOscillate(triggerTimer / triggerMaxTime);

            if (triggerTimer > triggerMaxTime) {
                if (!isExploding)
                    StartCoroutine("VibrateLongLf");
                if (triggerTimer > triggerMaxTime + .5) {
                    triggerTimer = -cooldownTime;
                }
            }
        }	
    }
    public static void LeftControllerOscillate(float signal){
        //float oscillateTimer = 0;
        signal = Mathf.Pow(signal,2) * .8f + .2f;
        weirdSinFunc = Mathf.Pow(Mathf.Sin(Time.realtimeSinceStartup*360*sineMult % 360 ) * .5f + .5f,sinePower);
        float sineRemap = weirdSinFunc  *.7f + .3f;
        oscillateTimer += Time.deltaTime;
        float gapTime = Mathf.Pow(weirdSinFunc,4) * .015f;
        //SteamVR_Controller.Input((int)trackedObjLf.index).TriggerHapticPulse ((ushort)(3999 * (sineRemap)* signal ) );
        TriggerHapticPulse("left", 3999f * sineRemap * signal);
    }

    public static void ControllerOscillate(string side, float signal)
    {
        if (trackedObjLf == null || trackedObjRt == null) return;
        int controllerIndex = 0;
        if (side.ToLower() == "right")
            controllerIndex = (int)trackedObjRt.index;
        if (side.ToLower() == "left")
            controllerIndex = (int)trackedObjLf.index;
        //float oscillateTimer = 0;
        signal = Mathf.Pow(signal, 2) * .8f + .2f;
        weirdSinFunc = Mathf.Pow(Mathf.Sin(Time.realtimeSinceStartup * 360 * sineMult % 360) * .5f + .5f, sinePower);
        float sineRemap = weirdSinFunc * .7f + .3f;
        oscillateTimer += Time.deltaTime;
        float gapTime = Mathf.Pow(weirdSinFunc, 4) * .015f;
        //if (oscillateTimer > gapTime){
        //SteamVR_Controller.Input(controllerIndex).TriggerHapticPulse((ushort)(3999 * (sineRemap) * signal));
        TriggerHapticPulse(side, 3999f * sineRemap * signal);
        //oscillateTimer = 0;
        //  }
    }

    IEnumerator VibrateLong(){
        isHit = true;
        float timer = 0;
        while (timer < hitLength){
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap){
                //if (trackedObjLf != null && (int)trackedObjLf.index > 0)
                    TriggerHapticPulse("left", 3999f);//SteamVR_Controller.Input((int)trackedObjLf.index).TriggerHapticPulse (3999);
                //if (trackedObjRt != null && (int)trackedObjRt.index > 0)
                    TriggerHapticPulse("right", 3999f);//SteamVR_Controller.Input((int)trackedObjRt.index).TriggerHapticPulse (3999);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }
    IEnumerator VibrateLongRt(){
        isHit = true;
        float timer = 0;
        while (timer < hitLength){
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap){
                //if (trackedObjRt != null && (int)trackedObjRt.index > 0)
                    TriggerHapticPulse("right", 3999f);//SteamVR_Controller.Input((int)trackedObjRt.index).TriggerHapticPulse (3999);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }
    IEnumerator VibrateLongLf()
    {
        isHit = true;
        float timer = 0;
        while (timer < hitLength)
        {
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap)
            {
                //if (trackedObjLf != null && (int)trackedObjLf.index > 0)
                    TriggerHapticPulse("left", 3999f);//SteamVR_Controller.Input((int)trackedObjLf.index).TriggerHapticPulse(3999);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }

    public static void SimplePulse(string side)
    {
        int controllerIndex = 0;
        if (side.ToLower() == "right")
            controllerIndex = (int)trackedObjRt.index;
        if (side.ToLower() == "left")
            controllerIndex = (int)trackedObjLf.index;
        TriggerHapticPulse(side, 3999f);//SteamVR_Controller.Input(controllerIndex).TriggerHapticPulse(3999);
    }

    public static IEnumerator VibrateExplode(string side)
    {
        isExploding = true;

        // int controllerIndex = 0;
        // if (side.ToLower() == "right")
        //     controllerIndex = (int)trackedObjRt.index;
        // if (side.ToLower() == "left")
        //     controllerIndex = (int)trackedObjLf.index;

        //float startTime = Time.realtimeSinceStartup;
        float timer = 0;
        float explodeGap = .025f;
        float explodeTime = .5f;
        while (timer < explodeTime)
        {
            //timer = Time.realtimeSinceStartup - startTime;
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= explodeGap)
            {
                //if (trackedObjRt != null && controllerIndex > 0)
                TriggerHapticPulse(side.ToLower(), 3999f);//SteamVR_Controller.Input(controllerIndex).TriggerHapticPulse(3999);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isExploding = false;
    }

    public static IEnumerator GrabBuzzRt(float signal){  //very hacky, check Remote Grab and think of a way to improve this
        //isHit = true;
        float timer = 0;
        while (timer < .05){
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= .001){
                //if (trackedObjRt != null && (int)trackedObjRt.index > 0)
                TriggerHapticPulse("right", 2000f * signal);//SteamVR_Controller.Input((int)trackedObjRt.index).TriggerHapticPulse ((ushort)(2000 * signal));
                hitGapTimer = 0;
            }
            yield return null;
        }
        //isHit = false;
    }

    public static IEnumerator GrabBuzzTest(string side, float signal) //rewritten to handle left or right
    {  //very hacky, check Remote Grab and think of a way to improve this
        float timer = 0;
        while (timer < .05)
        {
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= .001)
            {
                TriggerHapticPulse(side, 2000f * signal);
                hitGapTimer = 0;
            }
            yield return null;
        }
    }

    public static IEnumerator GrabBuzz(string side, float signal) //rewritten to handle left or right
    {  //very hacky, check Remote Grab and think of a way to improve this
        float timer = 0;
        while (timer < .05)
        {
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= .001)
            {
                TriggerHapticPulse(side, 2000f * signal);//SteamVR_Controller.Input(controllerIndex).TriggerHapticPulse((ushort)(2000 * signal));
                hitGapTimer = 0;
            }
            yield return null;
        }
    }

    public static IEnumerator PulseRt(float length){
        float timer = 0;
        while (timer < length) 
        {
            TriggerHapticPulse("right", 3999f);////SteamVR_Controller.Input ((int)trackedObjRt.index).TriggerHapticPulse (3999);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator Pulse(string side,float length)
    {
        // int controllerIndex = 0;
        // if (side.ToLower() == "right")
        //     controllerIndex = (int)trackedObjRt.index;
        // if (side.ToLower() == "left")
        //     controllerIndex = (int)trackedObjLf.index;
        float timer = 0;
        while (timer < length)
        {
            TriggerHapticPulse(side, 3999f);//not sure about this//SteamVR_Controller.Input(controllerIndex).TriggerHapticPulse(3999);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator VibrateShortRt(){
        isHit = true;
        float timer = 0;
        while (timer < hitLength * .15){
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap * .1){
                //if (trackedObjRt != null && (int)trackedObjRt.index > 0)
                TriggerHapticPulse("right", 600f);//SteamVR_Controller.Input((int)trackedObjRt.index).TriggerHapticPulse (600);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }

    IEnumerator VibrateSuperShortRt(){
        isHit = true;
        float timer = 0;
        while (timer < hitLength * .15){
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap * .1){
                //if (trackedObjRt != null && (int)trackedObjRt.index > 0)
                TriggerHapticPulse("right", 100f);//SteamVR_Controller.Input((int)trackedObjRt.index).TriggerHapticPulse (600);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }

    IEnumerator VibrateShortLf()
    {
        isHit = true;
        float timer = 0;
        while (timer < hitLength * .15)
        {
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap * .1)
            {
                //if (trackedObjRt != null && (int)trackedObjLf.index > 0)
                TriggerHapticPulse("left", 600f);//SteamVR_Controller.Input((int)trackedObjLf.index).TriggerHapticPulse(600);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }
    IEnumerator VibrateSuperShortLf()
    {
        isHit = true;
        float timer = 0;
        while (timer < hitLength * .15)
        {
            timer += Time.deltaTime;
            hitGapTimer += Time.deltaTime;
            if (hitGapTimer >= hitGap * .1)
            {
                //if (trackedObjRt != null && (int)trackedObjLf.index > 0)
                TriggerHapticPulse("left", 100f);//SteamVR_Controller.Input((int)trackedObjLf.index).TriggerHapticPulse(600);
                hitGapTimer = 0;
            }
            yield return null;
        }
        isHit = false;
    }

    void Vibrate(int index, ushort length ){//vibrate with intervals to create a low frequency buzz
        gapTimer += Time.deltaTime;
        string side = "left";
        if (index == (int)trackedObjRt.index) side = "right";//hack to go across platforms
        if (gapTimer >= buzzGap ){            
            gapTimer = 0;
            TriggerHapticPulse(side, (float)length);//SteamVR_Controller.Input(index).TriggerHapticPulse (length);
        }
    }

    void Vibrate(string side, ushort length ){//vibrate with intervals to create a low frequency buzz
        gapTimer += Time.deltaTime;
        //string side = "left";
        //if (index == (int)trackedObjRt.index) side = "right";//hack to go across platforms
        if (gapTimer >= buzzGap ){            
            gapTimer = 0;
            TriggerHapticPulse(side, (float)length);//SteamVR_Controller.Input(index).TriggerHapticPulse (length);
        }
    }

    void LeftControllerBuzz(){
        // if (trackedObjLf != null && (int)trackedObjLf.index > 0)
        //     Vibrate((int)trackedObjLf.index,buzzPulse);
        Vibrate("left",buzzPulse);
    }

    void RightControllerBuzz(){
        // if (trackedObjRt != null && (int)trackedObjRt.index > 0)
        //     Vibrate((int)trackedObjRt.index,buzzPulse);
        Vibrate("right",buzzPulse);
    }
}

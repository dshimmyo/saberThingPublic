using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
public class SaberEventManager : MonoBehaviour
{
    [SerializeField] private SaberFloor SaberFloorScript;
    [SerializeField] private ToolUser ToolUserScript;
    [SerializeField] private int eventLimit=2;

    [SerializeField] private int eventsCalled = 0;
    [SerializeField] private int eventCount = 0;
    [SerializeField] private int skips=0;
    private int numEventTypes = 3;//saberFloor and Cuts

    private bool previousFrameTextureApply = false;
    [SerializeField] private bool saberFloorDisable = false;

    bool isLeftPulsing = false;
    bool isRightPulsing = false;
    
   public IEnumerator PulseLf(float freq, float amp, float duration)
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

    public IEnumerator PulseRt(float freq, float amp, float duration)
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


    // float hapticsTestTimer = 0;
    // bool hapticsSideBool = false;
    void Update()
    {

        // hapticsTestTimer += Time.deltaTime;

        // if (hapticsTestTimer > 5)
        // {
        //     hapticsTestTimer = 0;
        //     if (hapticsSideBool)
        //     StartCoroutine(HapticsTesting.GrabBuzz("right", .5f));//StartCoroutine(PulseRt(1,1,.5f));
        //     else
        //     StartCoroutine(HapticsTesting.GrabBuzzTest("left", .5f));//StartCoroutine(PulseLf(1,1,.5f));//doesn't work
        //     hapticsSideBool = !hapticsSideBool;
        // }


        
        //possible actions to support:
        // saberFloor emissionsCooling operation
        // saberFloor emissionsTextureApply
        // saberFloor colorTextureApply
        // saberFloor burns need to happen and will be ignored in the event manager
        // ToolUser CutFromQueue() - allocate a certain amount of time to Cuts

        // leverage off of current optimizations, cuts queue and saberFloor action queue

        // prioritize events:
        // first come first served? simply alternate systems so they work cooperatively 
        
        //Note for later: Time.time is the time at the beginning of the frame so you'll know exactly how much time is left at the end of this frame.
        float startTime = Time.realtimeSinceStartup;
        float eventsTimer = 0;
        bool done = false;
        //float timeDeficit = startTime - Time.unscaledTime;//unscaledTime is the time at the beginning of the frame so you know how much time has elapsed
        //Debug.Log("Time Deficit:" + timeDeficit.ToString());
        eventCount = Time.frameCount % numEventTypes;//alternates order
        eventsCalled = 0;
        float timeLimit = .001f;//.00275 1/4 of frame based on 90fps

        bool currentFrameTextureApply = false;
        //bool skippedFrame = false;
        skips = 0;

        while (!done)
        {
        
            if (eventCount % numEventTypes == 0){//cuts and collider bakes from tooluser()
                if (ToolUserScript.GetIsDelayedColliderBake())//prioritize delayed collider baking operations before cutting new pieces
                {
                    Debug.Log("SaberEventManager:ToolUser:DoBake()");
                    ToolUserScript.DoBake();
                    eventsCalled++;
                    skips = 0;
                }
                else if (ToolUser.GetNumCutsInQueue() > 0)
                {
                    Debug.Log("ToolUserScript.CutFromQueue()");
                    ToolUserScript.CutFromQueue();
                    eventsCalled++;
                    skips = 0;
                }
                else
                {
                    skips++;
                }
            } 
            else if (eventCount % numEventTypes == 1)//saberFloor actions, burns and textureApply
            {
                if (SaberFloorScript.IsActionsInQ() && !saberFloorDisable)
                {
                    bool isTextureApply = SaberFloorScript.GetNextActionIsTextureApply();
                    //if (previousFrameTextureApply && isTextureApply)
                    //if (isTextureApply)
                    //{
                        //if (skippedFrame)
                        //{
                            //done = true;//I think this guarantees that there's only one texture update per frame
                        //}
                        //else
                        //{
                        //    skippedFrame = true;
                        //}
                        //skip this event but still increment eventsCalled to move on to the next event
                    //}
                    //else
                    {
                        Debug.Log("SaberFloorScript.DoNextAction()");
                        SaberFloorScript.DoNextAction();
                        if (isTextureApply)
                        {
                            currentFrameTextureApply = true;
                            done = true;//if a textureapply happens skip all other events
                        }
                        eventsCalled++;
                    }
                    skips = 0;
                }
                else
                {
                    skips++;
                }
            }
            else if (eventCount % numEventTypes == 2)//cuttable burns piggybacking through tooluser
            {
                if (ToolUserScript.GetIsCuttableBurns())
                {
                    ToolUserScript.DoCuttableBurn();
                    eventsCalled++;
                    skips = 0;

                } else {
                    skips++;
                }
            }
        
            eventCount++;
            eventsTimer = Time.realtimeSinceStartup - startTime;
            if  (
                    (eventsTimer > timeLimit && eventsCalled > 0)
                    || (skips >= numEventTypes)
                    || (eventsCalled >= eventLimit)//new thing meant to throttle events and speed things up
                ) //.011 is full frame so .0055 is half frame, .00275 is 1/4 frame
            {
                done = true;
            }
        }
        previousFrameTextureApply = currentFrameTextureApply;

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

using Unity.Jobs;//job system
using UnityEngine.Jobs;//job system
using Unity.Collections;//job system
using Unity.Burst;

public class ForceExploder : MonoBehaviour 
{
    float triggerTimer = 0;
    [SerializeField] float triggerMaxTime = .5f;
    bool previousTriggerPress = false;
    bool isTriggerPress = false;
    bool isExploding = false;
    public GameObject controller;
    public AudioSource explodeSound;
    public float cooldownTime = 2;
    [SerializeField] float forceRange = 10;
    private Vector3 forceDir;// = controller.transform.forward;
    private Vector3 forceUp;
    private Vector3 forceRight;
    private Vector3 explodePosition;// = controller.transform.position;
    [SerializeField] float frustumAngle = 45f;//14?
    private float explodeVolume = 1;
    private bool isRift = false;
    private bool isWMR = false;
    [SerializeField] float frustumOriginOffset = 4;//the number of units behind you the frustum starts
    [SerializeField] private bool flippedHands = false;
    string controllerSide = "left";
    [SerializeField] bool useImpulse = false;
    [SerializeField] float impulseMult = .2f;
    private bool vrControllerAlreadySetup = false;
    private bool isOculusQuest = false;
    
    // Use this for initialization
    void Start () 
    {
        explodeVolume = explodeSound.volume;
	}

//make a job system struct that takes an array of all of the cuttable game objects and filters them by their transform. look at their location
    //and decide if they are in the list or not. should be easy. 
    //use find gameobject by tag
    //loop through and convert to a vector3 position array
    //return an array based on the comparisons made
    
    [BurstCompile(CompileSynchronously = false)]

    public struct FilterCuttablesJob : IJobParallelFor {
        [ReadOnly]
        public NativeArray<Vector3> Positions;
        [WriteOnly]
        public NativeArray<int> Sides;
        [ReadOnly]
        public Vector3 NearClipNormal;
        [ReadOnly]
        public Vector3 NearClipPoint;
        [ReadOnly]
        public Vector3 FarClipNormal;
        [ReadOnly]
        public Vector3 FarClipPoint;
        [ReadOnly]
        public Vector3 RightClipNormal;
        [ReadOnly]
        public Vector3 RightClipPoint;
        [ReadOnly]
        public Vector3 LeftClipNormal;
        [ReadOnly]
        public Vector3 LeftClipPoint;
        [ReadOnly]
        public Vector3 TopClipNormal;
        [ReadOnly]
        public Vector3 TopClipPoint;
        [ReadOnly]
        public Vector3 BottomClipNormal;
        [ReadOnly]
        public Vector3 BottomClipPoint;
        public void Execute (int index) 
        {
            //get sign of normal dot (p1 - p2) where p1 is the vert and p2 is any point on the plane
            //if (Vector3.Dot(BladeNormal,(Vertices[index] - PointOnPlane)) >= 0)
            if (Vector3.Dot(NearClipNormal,(Positions[index] - NearClipPoint)) >= 0 &&
                Vector3.Dot(FarClipNormal,(Positions[index] - FarClipPoint)) >= 0 &&
                Vector3.Dot(RightClipNormal,(Positions[index] - RightClipPoint)) >= 0 &&
                Vector3.Dot(LeftClipNormal,(Positions[index] - LeftClipPoint)) >= 0 &&
                Vector3.Dot(TopClipNormal,(Positions[index] - TopClipPoint)) >= 0 &&
                Vector3.Dot(BottomClipNormal,(Positions[index] - BottomClipPoint)) >= 0)
                Sides [index] = 1;
            else 
                Sides [index] = 0;
        }
    }

    IEnumerator ForcePush (Vector3 pos, Vector3 dir, float signal, float timeLimit )
    {
        //parameters forcePosition, forceDirection, forceSignal //based on trigger time
        isExploding = true;
        float timer = 0;
        //Debug.Log("force push coroutine signal : " + signal.ToString());
        explodeSound.volume = Mathf.Max(.2f,signal) * explodeVolume;
        explodeSound.Play ();
        if (UnityEngine.XR.XRDevice.isPresent){//not so spammy anymore
            StartCoroutine(HapticsTesting.VibrateExplode(controllerSide));

            if (!vrControllerAlreadySetup)//if (!(isWMR || isRift) && )
            {
                vrControllerAlreadySetup = true;
                string vrDeviceModel = UnityEngine.XR.XRDevice.model;

                if (vrDeviceModel.StartsWith("Acer") ||
                    vrDeviceModel.Contains("Windows") ||
                    vrDeviceModel.Contains("Lenovo") ||
                    vrDeviceModel.Contains("Samsung"))
                {
                    isWMR = true;
                    isRift = true;
                } else if (vrDeviceModel.StartsWith("Oculus"))
                {
                    isRift = true;
                    if (vrDeviceModel.Contains("Quest"))
                    {
                        isOculusQuest = true;
                    }
                }
            }
        }

        GameObject[] cuttables1 = GameObject.FindGameObjectsWithTag ("Cuttable");
        GameObject[] forceTagged = GameObject.FindGameObjectsWithTag ("ForceObject");

        GameObject[] cuttables = new GameObject[cuttables1.Length + forceTagged.Length];//cuttables1.Concat(forceTagged).ToArray();
        cuttables1.CopyTo(cuttables,0);
        forceTagged.CopyTo(cuttables,cuttables1.Length);

        GameObject[] cuttablesRefined = new GameObject[cuttables.Length];

////job system
        NativeArray<Vector3> posArray = new NativeArray<Vector3>(cuttables.Length, Allocator.TempJob);
        NativeArray<int> sidesArray = new NativeArray<int>(cuttables.Length,Allocator.TempJob);
        for (int i=0; i<cuttables.Length; i++)
            posArray[i]=cuttables[i].GetComponent<Transform>().position;
        Vector3 testClipPos = pos - forceDir * 1f;
        FilterCuttablesJob job = new FilterCuttablesJob()//new stuff
        {
            Positions = posArray,
            Sides = sidesArray,
            NearClipNormal = forceDir,
            NearClipPoint = pos - forceDir*1f,//nearClip is 2u behind the controller
            FarClipNormal = -forceDir,
            FarClipPoint = testClipPos + forceDir * forceRange,      
            RightClipNormal = (-forceRight + forceDir).normalized,
            RightClipPoint = testClipPos + forceRight * .25f,
            LeftClipNormal = (forceRight + forceDir).normalized,
            LeftClipPoint = testClipPos - forceRight * .25f,
            TopClipNormal = (-forceUp + forceDir).normalized,
            TopClipPoint = testClipPos + forceUp * .25f,
            BottomClipNormal = (forceUp + forceDir).normalized,
            BottomClipPoint = testClipPos - forceUp * .25f
        };

        JobHandle sidednessJobHandle = job.Schedule(cuttables.Length, 8);//
        sidednessJobHandle.Complete();
        int[] sides = new int[cuttables.Length];
        sidesArray.CopyTo(sides);
        posArray.Dispose();
        sidesArray.Dispose();
        //vertexArray.CopyFrom(vertices);
        //sidesArray.CopyFrom(sides);
        yield return null;

        ////job system

        //frustum filter
        int refinedCount = 0;
        for (int j = 0; j < cuttables.Length; j++) {
                if(sides[j]==1)
                    cuttablesRefined[refinedCount++] = cuttables[j];
        }

        if (refinedCount < cuttables.Length)
        Debug.Log("forcePush acting on " + refinedCount.ToString() + " objects down from " + cuttables.Length.ToString());
        cuttables = cuttablesRefined;
        float[] forceArray = new float[refinedCount];
        Vector3 explodePositionFlat = new Vector3(explodePosition.x,0f,explodePosition.z);
        float myExplosiveForce = forceRange * Mathf.Max(.2f,signal);
        //if (useImpulse)
         //   myExplosiveForce = forceRange * Mathf.Max(.5f,signal);
        int iterationCount = 0;
        int count = 0;
        while (timer < timeLimit)
        {
            timer += Time.deltaTime;

            Rigidbody rb;
            for (int i = 0; i < refinedCount; i++) 
            {
                if (cuttables[i] != null)
                {
                    //Vector3 targetDir = (cuttables[i].transform.position-(pos - (frustumOriginOffset * forceDir))).normalized;
                    Vector3 origTargetDir = (cuttables[i].transform.position-pos).normalized;
                    float forcePowerMult = 1;

                    float myOriginalDot = Vector3.Dot(forceDir,origTargetDir);
                    
                    if (iterationCount == 0)
                    {
                        float posDotClamp = Mathf.Max(myOriginalDot,0);//this original dot product unadulterated needs to be > 0 for anything to happen

                        if (posDotClamp > 0)
                            forcePowerMult = 1 - Mathf.Pow (1 - myOriginalDot, 2);//inverted pow'd invert for a softer dropoff at the edges
                        else
                            forcePowerMult = 0;
                        
                        if (forcePowerMult > 0)
                            forceArray[i] = 10000f * forcePowerMult * Mathf.Max(.2f,signal);
                        else
                            forceArray[i] = 0;
                    }
                        //if (forcePowerMult > 0){
                        //    rb = cuttables [i].GetComponent<Rigidbody> ();
                        //    if (rb != null) 
                        //    {
                        //        //rb.AddExplosionForce (forceArray[i],explodePositionFlat, myExplosiveForce);//old style that requires continuous force applied over a number of frames
                        //        //force and impulse use the rigidbody's mass
                        //        rb.AddExplosionForce (forceArray[i],explodePositionFlat, myExplosiveForce, .5f/*upward*/,ForceMode.Force);//Impulse (* .25)
                        //   }
                        //}
                    //} else {
                    if (forceArray[i] > 0){
                        rb = cuttables [i].GetComponent<Rigidbody> ();
                        if (rb != null) 
                        {
                            if (forceArray[i]>0){
                                //rb.AddExplosionForce (forceArray[i],explodePositionFlat, myExplosiveForce);
                                if (useImpulse)
                                {
                                    rb.AddExplosionForce (forceArray[i],explodePositionFlat, myExplosiveForce * impulseMult, .25f/*upward*/,ForceMode.Impulse);//Impulse (* .25)
                                    timer = timeLimit;//hacky way to disable the forcePush after a single imp                                    
                                }
                                else
                                {
                                    rb.AddExplosionForce (forceArray[i],explodePositionFlat, myExplosiveForce, .25f/*upward*/,ForceMode.Force);//Impulse (* .25)
                                }
                            }
                        }
                    }
                    //}
                    if (++count > 32){
                        count = 0;
                        yield return null;
                    }

                }
            }
            iterationCount++;
            yield return null;
        }
        while (explodeSound.isPlaying){ 
            yield return null;
        }
        isExploding = false;
    }

    bool GetControllerTriggerPull()
    {
        if (Time.timeScale > .999f)
        {
            if (flippedHands)
            {
                if (Input.GetAxis("Oculus Touch Right Trigger Squeeze") > .5)
                    return true;
            }
            else
            {
                if (Input.GetAxis("Oculus Touch Left Trigger Squeeze") > .5)
                    return true;
            }
        }
        return false;
    }

    void Update () {
        flippedHands = HandednessHack.flippedHands;
        if (flippedHands)
            controllerSide = "right";
        else
            controllerSide = "left";
        if (controller != HandednessHack.VirtualSteamVRLeftController)
            controller = HandednessHack.VirtualSteamVRLeftController;

        if (GetControllerTriggerPull() || Input.GetKey(KeyCode.P)) 
        {
            isTriggerPress = true;
            triggerTimer += Time.deltaTime;
        } else {
            isTriggerPress = false;
            if (triggerTimer < 0)//cooldown should wind down even when the trigger isn't pressed
                triggerTimer += Time.deltaTime;
        }
        if (triggerTimer > 0 && triggerTimer < triggerMaxTime) 
        {
            if (!isExploding)
            {
                if (UnityEngine.XR.XRDevice.isPresent)
                    HapticsTesting.ControllerOscillate(controllerSide,triggerTimer / triggerMaxTime);
            }
        }
        if (triggerTimer > triggerMaxTime || (!isTriggerPress && previousTriggerPress)) 
        {
            if (!isExploding)
            {
                if (UnityEngine.XR.XRDevice.isPresent)
                {
                    if ((isRift || isWMR) && !isOculusQuest){//oculus quest is using the oculus sdk which is different from the other shits
                        forceRight = controller.transform.right;
                        forceDir = (controller.transform.forward - controller.transform.up).normalized;//this looks wrong but it's probably right
                        forceUp = Vector3.Cross(forceRight,forceDir);
                    }
                    else {
                        forceRight = controller.transform.right;
                        forceDir = controller.transform.forward;
                        forceUp = controller.transform.up;
                    }
                    explodePosition = controller.transform.position;
                } 
                else 
                {
                    forceDir = Camera.main.transform.forward;
                    explodePosition = Camera.main.transform.position;
                }
                StartCoroutine(ForcePush(explodePosition,forceDir,(float) triggerTimer/triggerMaxTime,.25f));
            }
        } 

        previousTriggerPress = isTriggerPress;
        if (!isTriggerPress)
            triggerTimer = 0;//needed to leave this alone until after it gets processed
	}
}

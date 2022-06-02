using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this class is attached to the blade which is fine, except occasionally the blade gets disabled.
//need to keep the blade enabled so tha this script can continue running. It just makes more sense I think.
//can switch off the meshrenderer and make a bool to disable cutting when it's shut off.

public class ToolUser : MonoBehaviour {

	public Material capMaterial;
    public GameObject PointA; //top
    public GameObject PointB; //base
    public GameObject PointC; //topfollow
    private Transform _PointATransform;
    private Transform _PointBTransform;
    private Transform _PointCTransform;
    private Vector3 pointAPreviousFrame;
    private Vector3 pointBPreviousFrame;
    public GameObject sparkLoopGO;
    private Transform _sparkLoopTransform;
    public Light sparkLightPrefab;
    private AudioSource sparkLoop;
    private GameObject triggerVictim;
    private Transform triggerVictimTransform;
    private Collider bladeCollider;
    private GameObject hitSound;
    private Dictionary<string, SaberCut> victimCutInfo = new Dictionary<string, SaberCut> ();

    private bool isPreviousHit = false;
    private string previousHitName = "";
    private float sparkRaycastDistance = 1;
    private float sparkTime = 0;
    public float sparkInterval = .1f;
    private GameObject[] whooshes;
    private int whooshIndex = 0;
    private int whooshCount = 3;
    private GameObject[] sparks;
    private int sparkIndex = 0;
    private int sparkCount = 7;
    public float minSize = .1f;
    private float minVolume = 0;
    [SerializeField] private bool isTestCut = false;
    [SerializeField] private bool useOnTriggerStay = false;
    public float cutJumpVelocity = 1.25f;
    [SerializeField] AudioClip[] sparkSounds;
    [SerializeField] private bool isDebug = false;
    //private static List<string> triggerVictimList;
    private static HashSet<string> triggerVictimHashSet;
    //private static List<int> triggerVictimIDList;
    [SerializeField] private float defaultYScale = 1;
    private Transform _transform;
    string controllerSide = "right";//use this to toggle haptics buzz sidedness
    [SerializeField] private float bladeCollisionToTriggerDelay = .25f;
    private bool isDelayedMeshColliderBake = false;
    public static int cuttempt = 0;
    public GameObject bladeLight;
    private Transform _bladelightTransform;
    private bool isTriggerEvent = false;
    private bool isTriggerStayEvent = false;

    private float minRaycastTimer = 0;
    private RaycastHit previousHit;
    private RaycastHit previousGroundHit;
    private bool isPreviousHitGround=false;
    [SerializeField] private int triggerExitsWithoutEnter = 0;
    //[SerializeField] private bool useBoxBlast = false;
    [SerializeField] private bool usePressCToCut = false;
    [SerializeField] private bool disableRaycasts = false;
    [SerializeField] private bool useCollisionBurns = false;
    private int triggerFrame = 0;
    private static List<CuttableBurn> cuttableBurnList = new List<CuttableBurn>();
    static private string currentVictim = "";
    public bool GetIsDelayedColliderBake()
    {
        if (isDelayedMeshColliderBake && delayedCut.piece != null)
            return true;
        return false;
    }

    public bool GetIsCuttableBurns()
    {
        if (cuttableBurnList.Count > 0)
            return true;
        return false;

    }
    public struct DelayedCutData
    {
        public GameObject piece;
        public float origVolume;
        public Plane cutPlane;
        public float mass;
        public float drag;
        public float angularDrag;
        public PhysicMaterial pm;

        public DelayedCutData(
                                GameObject myPiece
                                , float myOrigVolume
                                , Plane myCutPlane
                                , float myMass
                                , float myDrag
                                , float myAngularDrag
                                , PhysicMaterial myPm
                                )
        {
            piece = myPiece;
            origVolume = myOrigVolume;
            cutPlane = myCutPlane;
            mass = myMass;
            drag = myDrag;
            angularDrag = myAngularDrag;
            pm = myPm;
        }
    }
    public DelayedCutData delayedCut;
    //private int num=0;
    // Use this for initialization

    public static int GetNumCutsInQueue() {
        int count = 0;
        if (triggerVictimHashSet != null)
        {
            count = triggerVictimHashSet.Count;
        }
        return count;
    }

    public static int GetNumCutAttempts()
    {
        return cuttempt;
    }

    public static string GetCutName()
    {
        if (triggerVictimHashSet.Count > 0)
            return currentVictim;// triggerVictimList[0];
        else
            return "";
    }

    struct BoxBlastFrame
    {
        public Vector3 position;//origin? 
        public Vector3 rayStart;//_PointBTransform.position
        public Vector3 rayEnd;//_PointATransform.position
        public Vector3 rayDir;//rayEnd - rayStart;
        public Quaternion orientation; //_transform.rotation;
        public Vector3 origin;
        //public Transform objTransform;

        public BoxBlastFrame(
                                Transform myObjTransform,
                                Vector3 myRayStart,
                                Vector3 myRayEnd
                            )
        {
            //input values
            rayStart = myRayStart;
            rayEnd = myRayEnd;
            //objTransform = myObjTransform;

            //computed values recorded at the time it is being sampled
            position = myObjTransform.position;
            rayDir = myRayEnd - myRayStart;
            origin = myObjTransform.gameObject.GetComponent<Renderer>().bounds.center;
            orientation = myObjTransform.rotation;//frame-dependent data
        }
    }

    private BoxBlastFrame[] boxBlastFrameData = new BoxBlastFrame[4];
    private Vector3[] boxBlastCenterPreviousFrame = new Vector3[4];

    void Start ()
    {
        if (bladeLight != null)
            _bladelightTransform = bladeLight.GetComponent<Transform>();
        delayedCut = new DelayedCutData();
        //triggerVictimList = new List<string>();
        triggerVictimHashSet = new HashSet<string>();
        //triggerVictimIDList = new List<int>();
        whooshes = new GameObject[whooshCount];
        sparks = new GameObject[sparkCount];
        _PointATransform = PointA.transform;
        _PointBTransform = PointB.transform;
        _PointCTransform = PointC.transform;
        sparkRaycastDistance = Vector3.Distance (_PointATransform.position, _PointBTransform.position);
        sparkLoop = sparkLoopGO.GetComponent<AudioSource>();
        _sparkLoopTransform = sparkLoopGO.transform;
        for (int i=0; i<whooshCount; i++){
            if (i%3 == 0)
                whooshes[i] = Instantiate (Resources.Load("prefabs/saberSliceAPool") as GameObject,Vector3.zero,Quaternion.identity);
            else if (i%3 == 1)
                whooshes[i] = Instantiate (Resources.Load("prefabs/saberSliceBPool") as GameObject,Vector3.zero,Quaternion.identity);
            else 
                whooshes[i] = Instantiate (Resources.Load("prefabs/saberSliceCPool") as GameObject,Vector3.zero,Quaternion.identity);

            whooshes[i].name = "whoosh" + i.ToString();
        }

        //after break finish pooling spark-ticles
        int sparkSoundsCount = 0;
        for (int j=0; j<sparkCount; j++){
            if (SaberGame.QuestCheck())
            sparks[j] = Instantiate (Resources.Load("prefabs/sparksPoolQuest") as GameObject,Vector3.zero,Quaternion.identity);
            else
            sparks[j] = Instantiate (Resources.Load("prefabs/sparksPool") as GameObject,Vector3.zero,Quaternion.identity);
            if (sparkSounds.Length > 0){
                sparks[j].GetComponent<AudioSource>().clip = sparkSounds[sparkSoundsCount++ % sparkSounds.Length];
            }
            ParticleSystem ps = sparks[j].GetComponent<ParticleSystem>();
            var lights = ps.lights;
            lights.light = sparkLightPrefab;
            sparks[j].SetActive(false);
        }
        minVolume = Mathf.Pow (minSize, 3);
        _transform = transform;
        defaultYScale = _transform.localScale.y;
        for (int i = 0; i < 4; i++)
        {
            boxBlastCenterPreviousFrame[i] = Vector3.zero;
            boxBlastFrameData[i] = new BoxBlastFrame(_transform, _PointBTransform.position, _PointATransform.position);
        }
        bladeCollider = gameObject.GetComponent<Collider>();
    }

    void OnDisable(){
        if (sparkLoop != null)
            sparkLoop.Stop();
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Cuttable"))
            col.gameObject.GetComponent<Cuttable>().SetBladeTriggerCondition(col.contacts[0].point);//change to getContact after upgrading unity version

        CollisionBurn(col);
    }

    void CollisionBurn(Collision col)
    {
        if (useCollisionBurns)
        {
            if (col.gameObject.CompareTag("Cuttable"))
            {
                int i = 0;
                {
                    IBurnable burnme = col.contacts[i].otherCollider.GetComponent<IBurnable>();
                    if (burnme != null)
                    {
                        Vector3 origin = col.contacts[i].point + col.contacts[i].normal;
                        Vector3 dir = -col.contacts[i].normal;
                        RaycastHit hit = new RaycastHit();
                        if (col.contacts[i].otherCollider.Raycast(new Ray(origin, dir), out hit, .5f))//public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance);
                        {
                            CuttableBurn cb = new CuttableBurn(new RaycastHit[] { hit }, burnme);
                            cuttableBurnList.Add(cb);
                            //if (isDebug) Debug.Log("collision burn");
                            //burnme.Burn(hit);
                        }
                    }
                }
            }
        }
    }

    void OnCollisionStay(Collision col)
    {
        if (useCollisionBurns) CollisionBurn(col);
    }

    public void ExternalOnTriggerEnter (Collider other)
    {
        bool isAlreadyEntered = false;
        if (victimCutInfo.ContainsKey (other.gameObject.name))
            isAlreadyEntered = true;
        if (!isAlreadyEntered)
        {
            OnTriggerEnter(other);
            //if (isDebug) Debug.Log("Trail Enter");

                //Renderer rend = triggerVictim.GetComponent<Renderer>();
                //if (rend != null)
                //triggerVictim.GetComponent<Renderer> ().material.SetColor ("_OutlineColor", Color.cyan);
        }
    }

    public float FastSqrtInvAroundOne(float x)
    {
        const float a0 = 15.0f / 8.0f;
        const float a1 = -5.0f / 4.0f;
        const float a2 = 3.0f / 8.0f;

        return a0 + a1 * x + a2 * x * x;
    }

    public Vector3 FastNormalize(ref Vector3 v)
    {
        float len_sq = v.x * v.x + v.y * v.y + v.z * v.z;
        float len_inv = FastSqrtInvAroundOne(len_sq);
        return new Vector3(v.x * len_inv, v.y * len_inv, v.z * len_inv);
    }

    void OnTriggerEnter (Collider other){
        if (triggerFrame != Time.frameCount)
        {
            triggerFrame = Time.frameCount;
            if (isDebug) Debug.Log("Trigger Frame : " + triggerFrame);
        }
        isTriggerEvent = true;
        //try this, fire a ray and get the normal and point and store it.
        if (RemoteGrab.isHoldingSaberRt)
            HapticsTesting.SetStrike(controllerSide);
        
        Vector3 bladeDirection = _PointATransform.position - pointAPreviousFrame;

        //bladeDirection.Normalize();
        bladeDirection = FastNormalize(ref bladeDirection);
        /*RaycastHit hit = new RaycastHit ();
        bool rayResult = false;
        //top - base brings the ray to the origin
        Vector3 rayDirection = new Vector3(_PointATransform.position.x - _PointBTransform.position.x,
                                            _PointATransform.position.y - _PointBTransform.position.y,
                                            _PointATransform.position.z - _PointBTransform.position.z);
        rayDirection.Normalize();
        Ray ray = new Ray (_PointBTransform.position, rayDirection);
        rayResult=other.Raycast (ray, out hit, sparkRaycastDistance * _transform.localScale.y / defaultYScale);//removed the * .9f part because I just want to cut stuff
        */
        /*if (!rayResult)//missed ray
        {
            Vector3 experimentalRayDir = (_PointATransform.position - pointAPreviousFrame);//in the direction of the blade's movement
            Vector3 experimentalRayOrigin = other.transform.position - experimentalRayDir;//towards the object's pivot point //not accurate but likely to hit something
            ray = new Ray(experimentalRayOrigin, experimentalRayDir);
            rayResult = other.Raycast(ray, out hit, 1f);//removed the * .9f part because I just want to cut stuff
        }*/

        if (other.gameObject.CompareTag("Cuttable"))
        {
            if (isDebug) Debug.Log("OnTriggerEnter");

            if (triggerVictim != other.gameObject) 
            {    //need to know why the heck I'm using triggerVictim at all, 
                                                        //should just be passing parameters instead of using a class-level variable
                triggerVictim = other.gameObject;
                triggerVictimTransform = triggerVictim.transform;
            }
            if (triggerVictimTransform==null)
                triggerVictimTransform = other.gameObject.transform;
            Vector3 localSpaceHitPoint = other.gameObject.transform.InverseTransformPoint(_PointATransform.position);
            other.gameObject.GetComponent<Cuttable>().SetBladeTriggerCondition(_PointATransform.position);

            if (victimCutInfo.ContainsKey (other.gameObject.name)) 
            { //added on 3/19/2018 because I hate missing cuts //commenting stuff out 5/10/2018
                //uncommented 08/19/2019 just in case it fixes some weird cuts
               victimCutInfo [triggerVictim.name].Clear();
               victimCutInfo [triggerVictim.name].AddPoint(localSpaceHitPoint);
               victimCutInfo [triggerVictim.name].AddTime(Time.time);
            } 
            else 
            {
                if (!triggerVictimHashSet.Contains (other.gameObject.name)) {//if (!triggerVictimList.Contains (other.gameObject.name)) {
                    victimCutInfo.Add (other.gameObject.name, new SaberCut (localSpaceHitPoint, Time.time));
                    victimCutInfo[other.gameObject.name].AddHitDir(bladeDirection);
                }
            }

            //if (rayResult){  //don't waste a raycast, use the update loop to do this
            //    MakeSparks(hit,true);
            //    sparkTime = 0;
            //}

            //}
        }

    }

    public void OnTriggerStay(Collider other){
       if (useOnTriggerStay){
            isTriggerStayEvent = true;

            if (triggerVictim != other.gameObject) {
                triggerVictim = other.gameObject;
                triggerVictimTransform = other.gameObject.transform;
            }
            if (triggerVictimTransform==null)
                triggerVictimTransform = other.gameObject.transform;

            if (other.gameObject.CompareTag("Cuttable")){
                //triggerVictim = other.gameObject;
                //triggerVictimTransform = other.gameObject.transform;
                //initiate cuts during mid-cut?


                if (victimCutInfo.ContainsKey (other.gameObject.name)) {  // 3/6 edited some shit here because I didn't think it should be there
                    //midCutInfo
                    victimCutInfo [other.gameObject.name].AddMidTop(other.gameObject.transform.InverseTransformPoint(_PointATransform.position));
                    victimCutInfo [other.gameObject.name].AddMidBot(other.gameObject.transform.InverseTransformPoint(_PointBTransform.position));
                } else {
                    Vector3 localSpaceHitPoint = other.gameObject.transform.InverseTransformPoint(_PointATransform.position);
                    victimCutInfo.Add (other.gameObject.name, new SaberCut(localSpaceHitPoint, Time.time));
                    //Debug.Log("Fixing missed triggerEntry");
                    //triggerVictim.GetComponent<Renderer>().material.SetFloat("_Outline",1);
                    //triggerVictim.GetComponent<Renderer> ().material.SetColor ("_OutlineColor", Color.black);
                }            
                //add tip/base points to an array in the victimCutInfo dictionary
                //single entry, single exit, two mid points defining a mid line.
                //mid plane defined by the 2 mid points and a third point which is half-way between entry and exit
                //end up with 3 planes, (entry,mid1,mid2), (exit,mid1,mid2), bisect plane (mid1,mid2,(entry+exit)/2)
                //side algo will be like this determine leftup and leftdn points by checking sidedness based on cut plane and bisect plane
                //leftup cutside 0 bisect 0
                //leftdn cutside 0 bisect 1
                //rightup cutside 1 bisect 0
                //rightdn cutside 1 bisect 1
                //feed the left and right points into the original code to create the two cuts.
                //capping algo will need to cap 4 pieces instead of just 2

                //triggerVictim = other.gameObject;
                //AddToCutQueue (triggerVictim.name);
            }
        }
    }

    public void ExternalOnTriggerExit (Collider other){//this gets called a lot more often than OnTriggerExit
        if (!triggerVictimHashSet.Contains(other.gameObject.name))//if (!triggerVictimList.Contains(other.gameObject.name))
        {
            //Debug.Log("Hacky:");
            if (isDebug)
                Debug.Log("Trail Exit");

            OnTriggerExit(other);
        }
    }
    public void OnTriggerExit(Collider other){
        if (other.gameObject.CompareTag("Cuttable")){
            if (isDebug)
                Debug.Log("OnTriggerExit");

            Vector3 bladeMovementDirection = _PointATransform.position - pointAPreviousFrame;
            float bladeSpeedSqrd = bladeMovementDirection.magnitude / Time.deltaTime;//I am aware that I am using magnitude and the name of the variable is sqrd
            if (bladeSpeedSqrd < 4)//ignore cuts where the blade exits slowly
                return;//hoping this reduces wonky displacement cuts
            //bladeMovementDirection.Normalize();
            bladeMovementDirection = FastNormalize(ref bladeMovementDirection);
            if (victimCutInfo.ContainsKey (other.gameObject.name)) {
                Vector3 cutEntryDir = victimCutInfo[other.gameObject.name].GetHitDir();

                float myDot = Vector3.Dot(bladeMovementDirection,cutEntryDir);
                if (myDot < -.5){
                    //victimCutInfo.Remove(other.gameObject.name);//I think this caused a big bug
                    if (isDebug)
                        Debug.Log("Rejecting a reversed cut");
                    return; //rejected cut based on the blade reversing when it exited
                }
            }

            if (!victimCutInfo.ContainsKey (other.gameObject.name)) {//if the cut entry was not recorded
                if (isDebug)
                    Debug.Log("*******  *******  cut exit with no enter " + other.gameObject.name);
                triggerExitsWithoutEnter++;
/*
                Vector3 midPointA = new Vector3( (_PointATransform.position.x + pointAPreviousFrame.x)/2f, (_PointATransform.position.y + pointAPreviousFrame.y)/2f, (_PointATransform.position.z + pointAPreviousFrame.z)/2f);
                Vector3 midPointB = new Vector3( (_PointBTransform.position.x + pointBPreviousFrame.x)/2f, (_PointBTransform.position.y + pointBPreviousFrame.y)/2f, (_PointBTransform.position.z + pointBPreviousFrame.z)/2f);
                //This does happen - would be a good opportunity to fire a couple rays back to see if it's there and if you find it, submit it
                
                //from OnTriggerEnter
                RaycastHit hit = new RaycastHit ();
                bool rayResult = false;
                Vector3 rayDirection = new Vector3(midPointA.x - midPointB.x,
                                                    midPointA.y - midPointB.y,
                                                    midPointA.z - midPointB.z);
                rayDirection.Normalize();
                Ray ray = new Ray (midPointB, rayDirection);
                rayResult=other.Raycast (ray, out hit, sparkRaycastDistance * _transform.localScale.y / defaultYScale);//removed the * .9f part because I just want to cut stuff
                    //if hit, submit the cut using the midpoint as the entry point and the current point as exit
*/
                //if (rayResult){
                  //just submit the cut, don't bother firing a ray
                    if (triggerVictim != other.gameObject) {
                        triggerVictim = other.gameObject;
                        triggerVictimTransform = triggerVictim.transform;
                    }
                    if (triggerVictimTransform==null)
                        triggerVictimTransform = other.gameObject.transform;

                    //Vector3 localSpaceHitPoint = other.gameObject.transform.InverseTransformPoint(midPointA);
                    Vector3 localSpaceHitPoint = Vector3.zero;//other.gameObject.transform.InverseTransformPoint(midPointA);
/*  //4/3/2019 trying to reduce aggressive cutting behavior now that displacement and burning is a thing - commenting things out
                    if (!triggerVictimList.Contains (other.gameObject.name)) {
                        victimCutInfo.Add (other.gameObject.name, new SaberCut (localSpaceHitPoint, Time.time));
                        AddToCutQueue (other.gameObject.name);
                        if (isDebug)
                            Debug.Log("XXXXXXsubmitted a missed triggerEnter as a cut!");
                    }
*/  
                //}


            }

            if (!triggerVictimHashSet.Contains (other.gameObject.name)) {
                if (isDebug)
                    Debug.Log ("ToolUserTriggerExit");
                if (triggerVictim != other.gameObject) {
                    triggerVictim = other.gameObject;
                    triggerVictimTransform = triggerVictim.transform;
                }
                if (triggerVictimTransform == null)
                    triggerVictimTransform = other.gameObject.transform;
                AddToCutQueue (other.gameObject.name);
            }

            //Debug.Break();
        }
    }

    void PlayWhoosh(Vector3 pos){// using object pooling yeah!
        AudioSource ass = whooshes[whooshIndex % whooshCount].GetComponent<AudioSource>();
        if (!ass.isPlaying){
            ass.transform.position = pos;
            ass.Play();
            whooshIndex++;
        }
    }

    void AddToCutQueue(string cuttableVictimName){
        if (triggerVictimTransform==null)
            triggerVictimTransform = triggerVictim.transform;
        if (victimCutInfo.ContainsKey (cuttableVictimName)) {//if it finds it, it fixes bad point data?
            if (victimCutInfo[cuttableVictimName].pointA == Vector3.zero && victimCutInfo[cuttableVictimName].pointB == Vector3.zero)
            //collects point data from the saber to define the root and tip of the saber blade
                victimCutInfo [cuttableVictimName].AddPointAandB(triggerVictimTransform.InverseTransformPoint(_PointATransform.position), triggerVictimTransform.InverseTransformPoint(_PointBTransform.position));//also calculates exitpoint
            if (!triggerVictimHashSet.Contains(cuttableVictimName))
            {
                triggerVictimHashSet.Add(cuttableVictimName);
                GameObject go = GameObject.Find(cuttableVictimName);
                if (go != null)
                    go.GetComponent<Cuttable>().SetTessellate(false);
            }

        } else {
            GameObject go = GameObject.Find(cuttableVictimName);
            if (go != null)
                    go.GetComponent<Cuttable>().SetTessellate(true);
            if (isDebug)
                Debug.Log("Couldn't add to queue because the key was missing from the list");
        }
    }

    void AddToCutQueue(GameObject cuttableVictimGO){//overload to transition away from the string
        Cuttable cvgCuttable = GetComponent<Cuttable>();
        if (victimCutInfo.ContainsKey (cuttableVictimGO.name)) {//if it finds it, it fixes bad point data?
            if (victimCutInfo[cuttableVictimGO.name].pointA == Vector3.zero && victimCutInfo[cuttableVictimGO.name].pointB == Vector3.zero)
            //collects point data from the saber to define the root and tip of the saber blade
                victimCutInfo [cuttableVictimGO.name].AddPointAandB(cuttableVictimGO.transform.InverseTransformPoint(_PointATransform.position), cuttableVictimGO.transform.InverseTransformPoint(_PointBTransform.position));//also calculates exitpoint
            if (!triggerVictimHashSet.Contains(cuttableVictimGO.name))
            {
                triggerVictimHashSet.Add(cuttableVictimGO.name);
                if (cuttableVictimGO != null)
                    cvgCuttable.SetTessellate(false);
            }
        } else {
            
            cvgCuttable.SetTessellate(true);
            if (isDebug)
                Debug.Log("Couldn't add to queue because the key was missing from the list");
        }
    }

    public void SelfDestruct(GameObject selfDestructer, Vector3 longAxis, Vector3 touchPoint){
        //Debug.Log("SelfDestruct was called");
        string triggerVictimName = selfDestructer.name;
        triggerVictim = selfDestructer;
        triggerVictimTransform = selfDestructer.transform;
        if (!victimCutInfo.ContainsKey (triggerVictimName)) {//if it finds it, it fixes bad point data?
            victimCutInfo.Add (triggerVictim.name, new SaberCut (Random.onUnitSphere, Time.time));//setting the first hit point to the center of the object
        }
        if (touchPoint != Vector3.zero)
            victimCutInfo[triggerVictimName].AddPoint(touchPoint);

        victimCutInfo[triggerVictimName].AddPointAandB(Vector3.ProjectOnPlane(Random.onUnitSphere,longAxis), Vector3.ProjectOnPlane(Random.onUnitSphere,longAxis));//also calculates exitpoint
        if (!triggerVictimHashSet.Contains(triggerVictimName))
            triggerVictimHashSet.Add(triggerVictimName);
       // else
            //selfDestructer.GetComponent<Cuttable>().CancelSelfDestruct();
    }

    public void CutFromQueue(){
        //if (isDelayedMeshColliderBake)
            //return;
        float timeCutting = 0;
        float cuttingStartTime = Time.realtimeSinceStartup;
        float maxCuttingTime = .001f;//was .003 as of 06/02/2019//.0055//allowed to use up half of the frame-drawing time to maintain 90fps so hopefully everything else can be done faster.
        //reduced the number to .003 to leave room for meshcollider cooking
        int numCutsPerFrame = 0;
        if (triggerVictimHashSet.Count > 0) {
            string[] triggerVictimArray = new string[triggerVictimHashSet.Count];
            triggerVictimHashSet.CopyTo(triggerVictimArray);
            for(int i=0; i< triggerVictimHashSet.Count; i++){
                currentVictim = triggerVictimArray [0];
                if (GameObject.Find (currentVictim))
                {
                    Cuttable tessTest = GameObject.Find(currentVictim).GetComponent<Cuttable>();
                    bool skipThisOne = false;
                    if (tessTest != null)
                    {
                        if (tessTest.GetIsTessellatingInProgress())//this is redundant and will never happen unless tessellation becomes asyncronous
                        {
                            if (isDebug)
                                Debug.Log("Tess/Cut Clash!");
                            skipThisOne = true;
                        }
                    }
                    if (!skipThisOne){
                        bool result = CutStuff (currentVictim);
                        if (result) {//if returns false and the cuts do not resume from the middle this will result in a frame-friendly endless loop
                            triggerVictimHashSet.Remove(currentVictim);//triggerVictimList.RemoveAt (0);
                            victimCutInfo.Remove (currentVictim);//theoretically helps with memory leaks
                            numCutsPerFrame++;
                            if (isDelayedMeshColliderBake)
                                break;
                        } else {//cutstuff returns false for lots of reasons
                            if (isDebug)
                                Debug.Log ("CutStuff Rejected:" + currentVictim);
                        }
                    }
                } else {
                    triggerVictimHashSet.Remove(currentVictim);//triggerVictimList.RemoveAt (0);
                    victimCutInfo.Remove (currentVictim);//theoretically helps with memory leaks
                }
                if (Time.realtimeSinceStartup - cuttingStartTime > maxCuttingTime)
                    break;
            }
            if (numCutsPerFrame > 1 && isDebug)
            {
                if (isDebug) Debug.Log("numCutsPerFrame: " + numCutsPerFrame.ToString());
            }
        }
    }

    bool CutStuff(string triggerVictimName){
        GameObject localTriggerVictim = GameObject.Find (triggerVictimName);
        if (localTriggerVictim == null)
        {
            if (isDebug) Debug.Log("localTriggerVictim == null");
            return false;
        }
        triggerVictimTransform = localTriggerVictim.transform;
        Cuttable cc = localTriggerVictim.GetComponent<Cuttable>();
        //if (cc == null)
        //    return false;
        float minDestructVolume = 0;
        minDestructVolume = cc.minDestructVolume;//for self-destruct feature
        cc.SetTessellate(false); //prevent modification while being cut

        Vector3 localPointA = triggerVictimTransform.InverseTransformPoint(_PointATransform.position);
        Vector3 localPointB = triggerVictimTransform.InverseTransformPoint(_PointBTransform.position);

        float mass = 2;
        float drag = 1;
        float angularDrag = 1;
        Renderer rend = localTriggerVictim.GetComponent<Renderer> ();
        Vector3 size = Vector3.one;

        if (rend){
            size = rend.bounds.size;//worldspace bounds
        }
        else{
            cc.SetTessellate(true);
            return false;
        }
        float origVolume = size.x * size.y * size.z;
        Rigidbody rbv = localTriggerVictim.GetComponent<Rigidbody>();//triggerVictim.GetComponent<Rigidbody>();
        if (rbv){
            mass = rbv.mass;
            drag = rbv.drag;
            angularDrag = rbv.angularDrag;
        }
        Vector3 thirdPoint = pointAPreviousFrame;//PointC.transform.position

        bool isValidCut = true;
        SaberCut tempSaberCut;

        if(victimCutInfo.TryGetValue(triggerVictimName, out tempSaberCut)) //if Sabercut info exists
        {
            float tempTime=0;
            localPointA = tempSaberCut.pointA;
            localPointB = tempSaberCut.pointB;
            thirdPoint = tempSaberCut.point;//third point is the cut entry point
        } else {
            thirdPoint = pointAPreviousFrame; //plane will be defined by the current blade direction
        }

        if (isValidCut || isTestCut) {
            PhysicMaterial pm = new PhysicMaterial();
            if (localTriggerVictim.GetComponent<Collider> ()) {
                PhysicMaterial pmV = localTriggerVictim.GetComponent<Collider> ().material;//I think localTriggerVictim gets modified by the Cut() method
                if (pmV != null) {
                    pm = pmV;
                }
            }
            GameObject[] pieces;
            Vector3 victimSize = localTriggerVictim.GetComponent<Renderer>().bounds.size;//mesh bounds are in local space, collider and Renderer bounds are in world space
            if (victimCutInfo.ContainsKey (triggerVictimName))
            {
                cuttempt = ++victimCutInfo[triggerVictimName].cutAttemptCount;
                if (cuttempt < 150)//arbitrary limit for cuts that take multiple calls to meshcut.cut
                {
                    //if (cuttempt > 0)
                    //    Debug.Log("***cut attempt :" + cuttempt.ToString() + " " + triggerVictimName);
                    pieces = BLINDED_AM_ME.MeshCut.Cut (localTriggerVictim, localPointA, localPointB, thirdPoint, capMaterial);
                } 
                else 
                {
                    //Debug.Break();
                    BLINDED_AM_ME.MeshCut.Destroy(localTriggerVictim);
                    victimCutInfo.Remove (triggerVictimName);
                    if (isDebug)
                        Debug.Log("*********TooHeavy Or fucked up***********");
                    cc.SetTessellate(true);
                    return false;
                }
            }
            else //fallback while rewriting the code
            {
                BLINDED_AM_ME.MeshCut.Destroy(localTriggerVictim);
                victimCutInfo.Remove (triggerVictimName);
                if (isDebug)
                    Debug.Log("*********TooHeavy***********");
                cc.SetTessellate(true);
                return false;
            }

            if (pieces[0]==null && pieces[1]==null)//signals a continuation
            {
                if (isDebug)
                    Debug.Log("null cut");
                cc.SetTessellate(true);
                return false;//allows it to move on instead of freezing
            }
            if (pieces[1].name == "empty cut")
            {
                cc.SetTessellate(true);
                return true;
            } 
            else
            {
                Plane cutPlane = new Plane (localPointA, localPointB, thirdPoint);
                Mesh mesh = pieces [1].GetComponent<MeshFilter> ().sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Mesh mesh1 = pieces [0].GetComponent<MeshFilter> ().sharedMesh;
                Vector3[] vertices1 = mesh1.vertices;

                if (vertices.Length > 0 && vertices1.Length > 0) {
                    PlayWhoosh(localTriggerVictim.transform.position);
                }
                    
                if (vertices.Length < 1) {
                    BLINDED_AM_ME.MeshCut.Destroy (pieces [1]);
                    if (isDebug)
                        Debug.Log ("deleting empty mesh");
                } else {
                    ProcessCut(pieces[1], origVolume, cutPlane, mass, drag, angularDrag, pm);
                    pieces[1].gameObject.GetComponent<Cuttable>().minDestructVolume = minDestructVolume;//copy mindestructvolume
                }

                if (vertices1.Length < 1) {
                    BLINDED_AM_ME.MeshCut.Destroy (pieces [0]);
                    if (isDebug)
                        Debug.Log ("deleting empty mesh");
                } else
                {
                    if (pieces[1] != null){//don't need to delay baking if the first piece didn't make it
                        isDelayedMeshColliderBake = true;
                        if (pieces[0].GetComponent<Collider>())
                            Destroy(pieces[0].GetComponent<Collider>());
                        delayedCut = new DelayedCutData(pieces[0], origVolume, cutPlane, mass, drag, angularDrag, pm);
                    }
                    else{
                        pieces[0].gameObject.GetComponent<Cuttable>().minDestructVolume = minDestructVolume;//copy mindestructvolume in case piece[0] didn't have it?
                        ProcessCut(pieces[0], origVolume, cutPlane, mass, drag, angularDrag, pm);
                    }
                }
            }
        } else {
            //rbv.velocity += tempSaberCut.normal * .2f;
            rbv.velocity += (_PointATransform.position - pointAPreviousFrame) / Time.fixedDeltaTime * .2f;
        }
        return true;//placeholder just to make things work while I build a frame-friendly cutting system
    }

    void ProcessCut(
        GameObject piece
        , float origVolume
        , Plane cutPlane
        , float mass
        , float drag
        , float angularDrag
        , PhysicMaterial pm)
    {
        if (piece == null)
            return;
        Vector3 newSize = piece.GetComponent<Renderer>().bounds.size;//mesh bounds are in local space, collider and Renderer bounds are in world space
        float newVolume = newSize.x * newSize.y * newSize.z;
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = piece.AddComponent<Rigidbody>();
        }
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        if (!rb.isKinematic)
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        DetachLimb(rb);//rb.isKinematic = false; //might need to recurse through all of the attached parts
        if (piece.GetComponent<Collider>())
        {
            Destroy(piece.GetComponent<Collider>());
        }

        MeshCollider mc = piece.AddComponent<MeshCollider>();
        //mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;//comment out for faster cooking
        mc.cookingOptions = MeshColliderCookingOptions.None;//comment out for faster cooking

        mc.inflateMesh = true;//true; //
        Vector3 mcbs = newSize;//mc.bounds.size;
        float mind = Mathf.Min(mcbs.x, Mathf.Min(mcbs.y, mcbs.z));
        if (mind < minSize * 8)
            mc.skinWidth = .01f;//.005f;
        else
            mc.skinWidth = .005f;//.002f;
        //.002 is optimal but doesn't look great .01 is classic  
        mc.convex = true;
        mc.material = pm;

        Mesh mainMesh = piece.GetComponent<MeshFilter>().sharedMesh;
        if (mainMesh == null)
            return;
        Vector3[] verts = mainMesh.vertices;
        Vector3 size = piece.GetComponent<Renderer>().bounds.size;
        Transform pieceTransform = piece.transform;
            Mesh newMesh = new Mesh();
            IcoSphere.Create(newMesh);//new ico sphere at the origin
            Vector3[] icoVerts = newMesh.vertices;
            float maxSize = Mathf.Max(Mathf.Abs(size.x),Mathf.Max(Mathf.Abs(size.y),Mathf.Abs(size.z)));
            for (int i = 0; i < icoVerts.Length; i++) {
                //scale up the sphere before "shrinkwrapping" it
                icoVerts[i].x *= maxSize * 5f;
                icoVerts[i].y *= maxSize * 5f;
                icoVerts[i].z *= maxSize * 5f;

                //snap points to the nearest vert
                icoVerts[i] = Physics.ClosestPoint(icoVerts[i], mc, Vector3.zero, Quaternion.identity);//works on everything except the floppy rigged thing. the colliders looked offset and the wrong size
                //////assume that the shrinkwrap happens in a worldspace scale
                //////then divide points by worldspace scale
                icoVerts[i].x *= 1/piece.transform.lossyScale.x;
                icoVerts[i].y *= 1/piece.transform.lossyScale.y;
                icoVerts[i].z *= 1/piece.transform.lossyScale.z;

            }
            newMesh.vertices = icoVerts;

            
            //remove degenerate tris and build a list of valid vertices
            int[] tris = newMesh.triangles;//new int[newMesh.triangles.Length * 3];
            List<int>newTris = new List<int>();
            List<Vector3>goodVerts = new List<Vector3>();//rebuild vertsArray
            Dictionary<int, int> goodVertsDictionary = new Dictionary<int,int>();//create a mapping from old vert(key) to new vert(value)
            int goodVertCount = 0;
            int temp = 0;
            for (int j = 0; j < tris.Length; j+=3) {
                //scale up the sphere before shrinkwrapping it
                Vector3 a=newMesh.vertices[tris[j]];
                Vector3 b=newMesh.vertices[tris[j+1]];
                Vector3 c=newMesh.vertices[tris[j+2]];

                if (!(a==b || a==c || b==c)){//if not a degenerate tri
                    newTris.Add(tris[j]);//add vert to tris
                    if (!goodVerts.Contains(newMesh.vertices[tris[j]])){//add to good verts array if it isn't already there
                        goodVerts.Add(newMesh.vertices[tris[j]]);
                        goodVertCount++;
                    }
                    if (!goodVertsDictionary.TryGetValue(tris[j],out temp))
                        goodVertsDictionary.Add(tris[j],goodVerts.IndexOf(newMesh.vertices[tris[j]]));

                    newTris.Add(tris[j+1]);
                    if (!goodVerts.Contains(newMesh.vertices[tris[j+1]])){
                        goodVerts.Add(newMesh.vertices[tris[j+1]]);
                        goodVertCount++;
                    }
                    if (!goodVertsDictionary.TryGetValue(tris[j+1],out temp))
                        goodVertsDictionary.Add(tris[j+1],goodVerts.IndexOf(newMesh.vertices[tris[j+1]]));

                    newTris.Add(tris[j+2]);
                    if (!goodVerts.Contains(newMesh.vertices[tris[j+2]])){
                        goodVerts.Add(newMesh.vertices[tris[j+2]]);
                        goodVertCount++;
                    }
                    if (!goodVertsDictionary.TryGetValue(tris[j+2],out temp))
                        goodVertsDictionary.Add(tris[j+2],goodVerts.IndexOf(newMesh.vertices[tris[j+2]]));
                }
            }

            //remap tris to new verts array
            for (int k=0; k<newTris.Count; k++){
                newTris[k] = goodVertsDictionary[newTris[k]];
            }
            newMesh.triangles = newTris.ToArray();//update tris before updating verts or else you get an out of bounds error
            newMesh.vertices = goodVerts.ToArray();
            


            //think of a way to detect a degenerate condition before trying to override the mesh
            if (newMesh.vertices.Length > 8)
                mc.sharedMesh = newMesh;//occasionally get QuickHullConvexHullLib::findSimplex: Simplex input points appers to be coplanar.
            else
                if (isDebug) Debug.Log("***Avoided degenerate mesh collider condition***");
            //scale the mesh up larger than the base mesh then shrinkwrap it
        //}

        //}
        rb.centerOfMass = piece.GetComponent<MeshFilter>().sharedMesh.bounds.center;//pieces [0].transform.InverseTransformPoint (mc.bounds.center);//converting world to local
        rb.mass *= newVolume / origVolume; //mass adjusts to new size
        Vector3 pieceCenter = piece.GetComponent<Renderer>().bounds.center;
        //rb.velocity += (pieceCenter - cutPlane.ClosestPointOnPlane(pieceCenter)).normalized * cutJumpVelocity;
        int side = -1;
        if (cutPlane.GetSide(pieceCenter))
            side = 1;

        //rb.velocity += cutPlane.normal * side * cutJumpVelocity;
        //is it faster if I compute each component separately?
        Vector3 newVelocity = rb.velocity;
        newVelocity.x += cutPlane.normal.x * side * cutJumpVelocity;
        newVelocity.y += cutPlane.normal.y * side * cutJumpVelocity;
        newVelocity.z += cutPlane.normal.z * side * cutJumpVelocity;
        rb.velocity = newVelocity;
        //rb.velocity.x += cutPlane.normal.x * side * cutJumpVelocity;
        //rb.velocity.y += cutPlane.normal.y * side * cutJumpVelocity;
        //rb.velocity.z += cutPlane.normal.z * side * cutJumpVelocity;

        if (newVolume < minVolume)
        {
            if (!piece.gameObject.GetComponent<KillSpark>())
            {
                piece.gameObject.AddComponent<KillSpark>();
            }
        }
        piece.GetComponent<Cuttable>().SetTessellate(true);
    }

    void DetachLimb(Rigidbody rb){
        rb.transform.parent = null;
        rb.isKinematic = false;
        Joint[] jay = rb.gameObject.GetComponents<Joint>();
        for (int i=0; i<jay.Length; i++){
            Rigidbody cb = jay[i].connectedBody;
            if (cb != null)
                DetachLimb(cb);//jrb.isKinematic = false;
        }
    }

    void FixedUpdate()
    {
        isTriggerEvent = false;//an order-of-events thing
        isTriggerStayEvent = false;//an order-of-events thing
        //commented out triggerVictimStuff because it's too redundant and is already being checked in OnTriggerXXX and OnCollisionXXX
        if (triggerVictim != null)//possibly necessary to make sure cuts get registered correctly when multiples per frame
            triggerVictimTransform = triggerVictim.transform;
    }

    void BladeLightPosition(Vector3 bottomPos, Vector3 topPos){

        //pass in raycast hit pos for top pos when hit something
        Vector3 currentPos = _bladelightTransform.localPosition;
        if (bladeLight != null){
            float yPos = (bottomPos.y + topPos.y) / 2;
            Vector3 newLocalPos = new Vector3(0, yPos, 0);
            float lerpMult = 5f;
            if (yPos - bottomPos.y < .6){
                newLocalPos = new Vector3(0, bottomPos.y, 0);
            }
            if (currentPos.y < newLocalPos.y)
                lerpMult = .25f;
            _bladelightTransform.localPosition = Vector3.Lerp(currentPos,newLocalPos,Time.deltaTime * lerpMult) ;
        }
    }

    void MakeSparks(RaycastHit hit,bool isPlaySound){
        sparks[sparkIndex % sparkCount].transform.position = hit.point + (hit.normal * .05f);
        sparks[sparkIndex % sparkCount].SetActive(false);
        sparks[sparkIndex % sparkCount].SetActive(true);
        if (isPlaySound)
            sparks [sparkIndex % sparkCount].GetComponent<AudioSource> ().Play ();
        sparkTime = 0;
        sparkIndex++;
    }

    /*
    void RayBlast()
    {
        float bladeTopDisplacement = Mathf.Abs(Vector3.Distance(_PointATransform.position, pointAPreviousFrame));
        int numRays = Mathf.CeilToInt(bladeTopDisplacement / .01f);//was .025
        if (numRays > 100)
            numRays = 100;
        Vector3 topIncrement = (_PointATransform.position - pointAPreviousFrame) / (float)numRays;
        Vector3 botIncrement = (_PointBTransform.position - pointBPreviousFrame) / (float)numRays;
        Vector3 bladeDirection = _PointATransform.position - pointAPreviousFrame;
        //bladeDirection.Normalize();
        bladeDirection = FastNormalize(ref bladeDirection);

        Dictionary<string, int> triggerEntries = new Dictionary<string, int>();//name and frame number
        if (bladeTopDisplacement > .1) //think of it as 1m per cinematic frame or .011m / frame at 90fps or .275m/.011sec
        {
            Vector3 rayStart = pointBPreviousFrame;
            Vector3 rayEnd = pointAPreviousFrame;

            //fire a ray for every .05m of movement or .0025 of sqrDistance
            //fire rays until one hits and then submit a cut to the queue
            for (int i = 0; i <= numRays; i++)//ray loop
            {//one extra ray just to get  one last exit possibility
                rayStart += botIncrement;
                rayEnd += topIncrement;
                Vector3 rayDir = rayEnd - rayStart;
                rayDir = FastNormalize(ref rayDir);
                RaycastHit[] hits = null;// = new RaycastHit[] ();
                RaycastHit hit = new RaycastHit();
                int layerMask = 1 << 15;// 1 << 12;//12 is the lightsaber layer //15 is cuttable
                                        //layerMask = ~layerMask;//hit everything but layer 12
                bool isHit = false;
                hits = Physics.RaycastAll(rayStart, rayDir, sparkRaycastDistance * _transform.localScale.y / defaultYScale, layerMask);

                Debug.DrawRay(rayStart, rayDir, Color.green);

                isHit = hits.Length > 0;

                if (isHit)
                {
                    //Debug.Log("multicast loop");

                    for (int j = 0; j < hits.Length; j++)
                    { //wait until this ray's hits are exhausted before checking to see if anything is exited.
                        if (hits[j].transform.tag == "Cuttable")
                        {
                            int ival;
                            if (triggerEntries.TryGetValue(hits[j].transform.name, out ival))
                            {
                                //if (ival <= i)//was hit in a previous raycast
                                triggerEntries[hits[j].transform.name] = i;//change it to the current ray so it is looked at like a triggerstay
                            }
                            else //this is a triggerEntry and needs to be added to the dictionary
                            {
                                triggerEntries.Add(hits[j].transform.name, i);//i indicates the ray number
                            }
                        }
                    }//for j

                    if (i > 0)//check for exits after each ray is shot
                    {
                        foreach (string key in triggerEntries.Keys)
                        {
                            if (triggerEntries[key] == i - 1)//submit anything from the previous ray number
                            {
                                if (!triggerVictimHashSet.Contains(key))//if (!triggerVictimList.Contains(key))//not in queue should be entered
                                {
                                    triggerVictim = GameObject.Find(key);// hits[k].transform.gameObject;
                                    triggerVictimTransform = triggerVictim.transform;
                                    Vector3 localSpaceHitPoint = triggerVictim.transform.InverseTransformPoint(hits[0].point);//can be any of the ray hits so 0 is the easiest to get

                                    if (!victimCutInfo.ContainsKey(key))
                                    {
                                        victimCutInfo.Add(key, new SaberCut(localSpaceHitPoint, Time.time));
                                    }
                                    AddToCutQueue(key);
                                    if (isDebug)
                                        Debug.Log("YYYYYYYYY submitted a sub-frame cut! " + key);
                                }
                            }
                        }
                    }

                }//isHit

            }//rays loop
        }
    }
*/
    
    /*
    void BoxBlast()
    {
        float bladeTopDisplacement = Mathf.Abs(Vector3.Distance(_PointATransform.position, pointAPreviousFrame));
        Vector3[] positionsArray = new Vector3[5];
        positionsArray[0] = _transform.position;
        for (int i = 1; i < 5; i++)
            positionsArray[i] = boxBlastCenterPreviousFrame[i - 1];

        int index = 0;//currentFrame is 0, previousframe is 1
        if (bladeTopDisplacement > .1) //think of it as 1m per cinematic frame or .011m / frame at 90fps or .275m/.011sec
        {
            Vector3 direction = positionsArray[1] - positionsArray[0]; //(boxBlastCenterPreviousFrame - _transform.position);//
            float maxDistance = direction.magnitude;
            Vector3 rayStart = _PointBTransform.position;
            Vector3 rayEnd = _PointATransform.position;
            Vector3 rayDir = rayEnd - rayStart;
            float rayLength = rayDir.magnitude;
            rayDir = FastNormalize(ref rayDir);
            Debug.DrawRay(rayStart, rayDir, Color.yellow);

            Vector3 halfExtents = new Vector3(.015f, rayLength * .5f, .015f);//new Vector3(.015f, .666f, .015f);//new Vector3(.015f,.015f,.666f);//swap z and y
            int layerMask = 1 << 15;// 1 << 12;//12 is the lightsaber layer //15 is cuttable
            Vector3 origin = GetComponent<Renderer>().bounds.center;//(_PointATransform.position + _PointBTransform.position / 2f);//don't know why averaging top/bot positions doesn't work // positionsArray[0];//(_PointATransform.position + _PointBTransform.position / 2);
            //Vector3 orientationEuler = _transform.rotation.eulerAngles;
            Quaternion orientation = _transform.rotation;//Quaternion.Euler(orientationEuler);//
            RaycastHit[] hits = Physics.BoxCastAll(origin, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            if (isDebug)
                ExtDebug.DrawBoxCastBox(origin, halfExtents, orientation, direction, maxDistance, Color.green);
            //fire a ray at the current frame to determine the exit point for every cut

            RaycastHit[] lastFrameRaycastHits = Physics.RaycastAll(rayStart, rayDir, sparkRaycastDistance * _transform.localScale.y / defaultYScale);
            int cutsSubmitted = 0;
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.tag == "Cuttable")//if this object is not in the lastFrameRaycastHits, it should be submitted as a cut
                    {
                        bool isExit = true; //assume good exit condition
                        for (int j = 0; j < lastFrameRaycastHits.Length; j++)
                        {
                            if (hits[i].transform.gameObject == lastFrameRaycastHits[j].transform.gameObject)
                            {
                                isExit = false;
                                break;//break out of this loop
                            }
                        }

                        if (isExit)
                        {
                            if (!triggerVictimHashSet.Contains(hits[i].transform.name))//not in queue should be entered
                            {
                                triggerVictim = hits[i].transform.gameObject;
                                triggerVictimTransform = triggerVictim.transform;
                                Vector3 localSpaceHitPoint = hits[i].transform.InverseTransformPoint(_PointATransform.position);

                                if (!victimCutInfo.ContainsKey(hits[i].transform.name))
                                {
                                    victimCutInfo.Add(hits[i].transform.name, new SaberCut(localSpaceHitPoint, Time.time));
                                }
                                AddToCutQueue(hits[i].transform.name);
                                if (isDebug)
                                    Debug.Log("BBBBBBBBBB submitted a sub-frame cut! " + hits[i].transform.name);
                                cutsSubmitted++;
                            }
                        }
                    }

                }

            }
            //if (cutsSubmitted>0)
            //    Debug.Break();

        }
    }
*/
    public void DoBake()
    {
        //float bakeStartTime = Time.realtimeSinceStartup;
        ProcessCut(delayedCut.piece, delayedCut.origVolume, delayedCut.cutPlane, delayedCut.mass, delayedCut.drag, delayedCut.angularDrag, delayedCut.pm);
        //bakeTime = Time.realtimeSinceStartup - bakeStartTime;
        delayedCut.piece = null;
        isDelayedMeshColliderBake = false;
    }
/*
    void BoxBlast(BoxBlastFrame bb0, BoxBlastFrame bb1)
    {
        //pointATransform.position is bb0 rayEnd
        //pointAPreviousFrame.position is bb1 rayEnd
        //float bladeTopDisplacement = Mathf.Abs(Vector3.Distance(_PointATransform.position, pointAPreviousFrame));
        float bladeTopDisplacement = Mathf.Abs(Vector3.Distance(bb0.rayEnd, bb1.rayEnd));

        int index = 0;//currentFrame is 0, previousframe is 1
        if (bladeTopDisplacement > .05) //think of it as 1m per cinematic frame or .011m / frame at 90fps or .275m/.011sec
        {
            Vector3 direction = bb1.position - bb0.position; //(boxBlastCenterPreviousFrame - _transform.position);//
            float maxDistance = direction.magnitude;
            Vector3 rayStart = bb0.rayStart;// _PointBTransform.position;
            Vector3 rayEnd = bb0.rayEnd;// _PointATransform.position;
            Vector3 rayDir = rayEnd - rayStart;
            float rayLength = rayDir.magnitude;
            rayDir = FastNormalize(ref rayDir);
            Debug.DrawRay(rayStart, rayDir, Color.yellow);

            Vector3 halfExtents = new Vector3(.015f, rayLength * .5f, .015f);//new Vector3(.015f, .666f, .015f);//new Vector3(.015f,.015f,.666f);//swap z and y
            int layerMask = 1 << 15;// 1 << 12;//12 is the lightsaber layer //15 is cuttable
            Vector3 origin = bb0.origin;// GetComponent<Renderer>().bounds.center;//(_PointATransform.position + _PointBTransform.position / 2f);//don't know why averaging top/bot positions doesn't work // positionsArray[0];//(_PointATransform.position + _PointBTransform.position / 2);
            Quaternion orientation = bb0.orientation;// _transform.rotation;//Quaternion.Euler(orientationEuler);//
            RaycastHit[] hits = Physics.BoxCastAll(origin, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            if (isDebug)
                ExtDebug.DrawBoxCastBox(origin, halfExtents, orientation, direction, maxDistance, Color.green);
            //fire a ray at the current frame to determine the exit point for every cut

            RaycastHit[] lastFrameRaycastHits = Physics.RaycastAll(rayStart, rayDir, sparkRaycastDistance * _transform.localScale.y / defaultYScale);
            int cutsSubmitted = 0;
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.tag == "Cuttable")//if this object is not in the lastFrameRaycastHits, it should be submitted as a cut
                    {
                        bool isExit = true; //assume good exit condition
                        for (int j = 0; j < lastFrameRaycastHits.Length; j++)
                        {
                            if (hits[i].transform.gameObject == lastFrameRaycastHits[j].transform.gameObject)
                            {
                                isExit = false;
                                break;//break out of this loop
                            }
                        }

                        if (isExit)
                        {
                            if (!triggerVictimHashSet.Contains(hits[i].transform.name))//not in queue should be entered
                            {
                                triggerVictim = hits[i].transform.gameObject;
                                triggerVictimTransform = triggerVictim.transform;
                                Vector3 localSpaceHitPoint = hits[i].transform.InverseTransformPoint(_PointATransform.position);

                                if (!victimCutInfo.ContainsKey(hits[i].transform.name))
                                {
                                    victimCutInfo.Add(hits[i].transform.name, new SaberCut(localSpaceHitPoint, Time.time));
                                }
                                AddToCutQueue(hits[i].transform.name);
                                if (isDebug)
                                    Debug.Log("BBBBBBBBBB submitted a sub-frame cut! " + hits[i].transform.name);
                                cutsSubmitted++;
                            }
                        }
                    }

                }

            }
            //if (cutsSubmitted>0)
            //    Debug.Break();

        }
    }*/

    public struct CuttableBurn {//make a struct
        public RaycastHit[] hits;
        public RaycastHit previousHit;
        public IBurnable burnable;
        public bool usePreviousHit;

        public CuttableBurn(
            RaycastHit[] newHits,
            IBurnable newBurnable
            )
        {
            hits = newHits;
            burnable = newBurnable;
            previousHit = new RaycastHit();
            usePreviousHit = false;
        }
        public CuttableBurn(
            RaycastHit[] newHits,
            RaycastHit newPreviousHit,
            IBurnable newBurnable
            )
        {
            hits = newHits;
            burnable = newBurnable;
            previousHit = newPreviousHit;
            usePreviousHit = true;
        }        
    }

    public void DoCuttableBurn()
    {
        CuttableBurn cb = cuttableBurnList[0];
        IBurnable burnable = cb.burnable;
        //if (cb != null)
        {
            if (cb.usePreviousHit)
            {
                cb.burnable.Burn(cb.hits[0],cb.previousHit);//assuming only one front hit to a back hit
                cuttableBurnList.RemoveAt(0);
            }
            else
            {
                List<int> combinedBurnIndices = new List<int>();//combine all non-previousHit burns
                List<RaycastHit> combinedHits = new List<RaycastHit>();

                for (int i = cuttableBurnList.Count-1; i >= 0; i--)
                {
                    if (cuttableBurnList[i].burnable == burnable && !cuttableBurnList[i].usePreviousHit)
                    {
                        for (int j = 0; j < cuttableBurnList[i].hits.Length; j++)
                            combinedHits.Add(cuttableBurnList[i].hits[j]);
                        cuttableBurnList.RemoveAt(i);
                    }
                }
                cb.burnable.Burn(combinedHits.ToArray());

                //cb.burnable.Burn(cb.hits);//old version
                //cuttableBurnList.RemoveAt(0);

            }
        }
    }

    void Update(){ //tooluser seems to take up a lot of time (13ms) per frame on my home machine, probably only when cutting, probably because of meshcut, but worth noting

        if (HandednessHack.flippedHands)
            controllerSide = "left";
        else
            controllerSide = "right";

        float bladeLengthSquared = (_PointATransform.position - _PointBTransform.position).sqrMagnitude;

        minRaycastTimer += Time.deltaTime;
        if (bladeLengthSquared > .01)
        {
            if (isTriggerEvent || minRaycastTimer > .015 && !disableRaycasts)
            {
                RaycastHit hit = new RaycastHit();
                minRaycastTimer = 0;
                RaycastHit[] hits;// = new RaycastHit[] ();
                int layerMask = 1 << 12;//12 is the lightsaber layer
                bool isGroundHit = false;
                // This would cast rays only against colliders in layer 8.
                // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
                layerMask = ~layerMask;//hit everything but layer 12
                                       //bool isHit = Physics.Raycast (_PointBTransform.position, (_PointATransform.position - _PointBTransform.position).normalized, out hit, sparkRaycastDistance * _transform.localScale.y / defaultYScale, layerMask);
                Vector3 burnRayDirection = (_PointATransform.position - _PointBTransform.position).normalized;
                hits = Physics.RaycastAll (_PointBTransform.position, burnRayDirection,  sparkRaycastDistance * _transform.localScale.y / defaultYScale, layerMask);
                bool isOneSpark = false;
                Vector3 bladeMovementDirection = _PointATransform.position - pointAPreviousFrame;
                float bladeSpeedSqrd = bladeMovementDirection.sqrMagnitude / Time.deltaTime;

                if (hits.Length > 0)
                {
                    hit = hits[0];
                    for (int k = 0; k < hits.Length; k++)
                    {
                        BladeLightPosition(_PointBTransform.localPosition, _bladelightTransform.InverseTransformPoint(hits[k].point));
                        IBurnable burnme = hits[k].collider.GetComponent<IBurnable>();
                        if (burnme != null)
                        {
                            hit = hits[k];
                            //Debug.Log("hit: " + hit.collider.name);
                            if (hit.collider.name == "groundplane")//deal with double-hit on ground first, then treat others
                            {
                                if (isPreviousHitGround)
                                {
                                    burnme.Burn(hits[k], previousGroundHit);
                                }
                                else
                                {
                                    burnme.Burn(hits[k]);
                                }
                                isGroundHit = true;
                                previousGroundHit = hit;
                            }
                            else if (bladeSpeedSqrd < 4)//ignore cuts where the blade exits slowly //it must be a cuttable object
                            {
                                RaycastHit backHit = new RaycastHit();
                                Vector3 backRayDirection = (_PointBTransform.position - _PointATransform.position).normalized;
                                float backDistance = sparkRaycastDistance - hit.distance;//sparkRaycastDistance;// * _transform.localScale.y / defaultYScale ;
                                bool isBackBurn = hit.collider.Raycast(new Ray(_PointATransform.position, -burnRayDirection), out backHit, backDistance);
                                {
                                    if (isBackBurn)
                                    {
                                        if (isDebug) Debug.Log("backburn bro");
                                        CuttableBurn cb = new CuttableBurn(new RaycastHit[] { hit, backHit }, burnme);
                                        cuttableBurnList.Add(cb);
                                        //burnme.Burn(new RaycastHit[] { hit, backHit });
                                    }
                                    else
                                    {
                                        CuttableBurn cb = new CuttableBurn(new RaycastHit[] { hit }, burnme);
                                        cuttableBurnList.Add(cb);
                                        //burnme.Burn(hit);
                                    }
                                }


                            }

                            if (!bladeCollider.isTrigger && hit.collider.CompareTag("Cuttable")/*hit.collider.GetComponent<Cuttable>()*/)
                                bladeCollider.isTrigger = true;
                        }

                        _sparkLoopTransform.position = hits[k].point;

                        if (!sparkLoop.isPlaying)
                        {
                            sparkLoop.Play();
                        }
                        if (RemoteGrab.isHoldingSaberRt)
                            HapticsTesting.SetDrag(controllerSide);

                        if ( hits[k].transform.CompareTag("Cuttable") && !isOneSpark && sparkTime > sparkInterval)
                        {
                            if ( Vector3.Distance (_PointATransform.position, _PointCTransform.position) > .25){
                                MakeSparks(hits[k], true);
                                isOneSpark = true;
                            }
                        }
                        //sparkTime = 0; //this happens in the makesparks method
                    }//hits loop
                } else {
                    if (sparkLoop.isPlaying)
                        sparkLoop.Stop();
                    if (_bladelightTransform.localPosition.y != 0)
                        BladeLightPosition(_PointBTransform.localPosition, _PointATransform.localPosition);
                }

                if (hits.Length > 0)
                {
                    previousHit = hit;
                    previousHitName = hit.collider.name;
                    isPreviousHit = true;
                    isPreviousHitGround = isGroundHit;
                }
                else
                {
                    isPreviousHit = false;
                    isPreviousHitGround = false;

                }

                if (sparkTime > sparkInterval ) {//quiet sparks when consecutive frames are sparking
                    //if (Vector3.Distance (_PointATransform.position, _PointCTransform.position) > .5) {
                    //if ((_PointATransform.position - _PointCTransform.position).sqrMagnitude > .01) {//hopefully faster than distance
                        if (isPreviousHit) {

                            //isPreviousHit = false;//hacky
                            if (hit.collider.name != "bladeTrail" /*&& hit.collider.name != "groundplane"*/){
                                MakeSparks(hit,false);
                            //Debug.Log ("spark quiet " + hit.collider.name);
                            }
                        }
                    //}
                } else {
                    sparkTime += Time.deltaTime;
                }
            }//if isTriggerEvent
        } 
        else 
        {//if blade is very short
            if (sparkLoop.isPlaying)
                sparkLoop.Stop();
            if (_bladelightTransform.localPosition.y != 0)
                BladeLightPosition(_PointBTransform.localPosition, _PointATransform.localPosition);
        }

        if (usePressCToCut && Input.GetKeyDown(KeyCode.C))//weird test using frozen trail geometry
        {
            if (triggerVictim != null)
            {

                triggerVictimTransform = triggerVictim.transform;
                Vector3 localSpaceHitPoint = triggerVictimTransform.InverseTransformPoint(_PointCTransform.position);
                Vector3 bladeDirection = _PointCTransform.position - _PointATransform.position ;//pointAPreviousFrame;
                bladeDirection = FastNormalize(ref bladeDirection);

                if (!victimCutInfo.ContainsKey(triggerVictim.name)){
                    victimCutInfo.Add (triggerVictim.name, new SaberCut (localSpaceHitPoint, Time.time));
                }
                victimCutInfo [triggerVictim.name].Clear();
                victimCutInfo [triggerVictim.name].AddHitDir(bladeDirection);
                victimCutInfo [triggerVictim.name].AddPoint(localSpaceHitPoint);
                victimCutInfo [triggerVictim.name].AddTime(Time.time);

                if (!triggerVictimHashSet.Contains (triggerVictim.name)) 
                {
                    if (isDebug)
                        Debug.Log ("cut testing!");
                    AddToCutQueue (triggerVictim.name);
                }
            }
        }

        // if (useBoxBlast)
        // {
        //     if (!(isTriggerEvent || isTriggerStayEvent) && bladeCollider.isTrigger && (bladeLengthSquared > .1))//when I comment this out I miss a lot more cuts for some reason
        //     { //fire some rays if there is significant distance between pointA and pointAprevious
        //       //RayBlast();//fires a shitload of rays between blade positions to find missed objects
        //       //BoxBlast();//fires a box-shaped raycast
        //         boxBlastFrameData[3] = boxBlastFrameData[2];
        //         boxBlastFrameData[2] = boxBlastFrameData[1];
        //         boxBlastFrameData[1] = boxBlastFrameData[0];
        //         boxBlastFrameData[0] = new BoxBlastFrame(_transform, _PointBTransform.position, _PointATransform.position);
        //         BoxBlast(boxBlastFrameData[0], boxBlastFrameData[1]);
        //         BoxBlast(boxBlastFrameData[1], boxBlastFrameData[2]);
        //         //BoxBlast(boxBlastFrameData[2], boxBlastFrameData[3]);
        //
        //     }//if !triggerEvents
        // }

        pointAPreviousFrame = _PointATransform.position;
        pointBPreviousFrame = _PointBTransform.position;//going to use this to fire some extra raycasts for smoother cutting
        // boxBlastCenterPreviousFrame[3] = boxBlastCenterPreviousFrame[2];
        // boxBlastCenterPreviousFrame[2] = boxBlastCenterPreviousFrame[1];
        // boxBlastCenterPreviousFrame[1] = boxBlastCenterPreviousFrame[0];
        // boxBlastCenterPreviousFrame[0] = _transform.position;

    }


}

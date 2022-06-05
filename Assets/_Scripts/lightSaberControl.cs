using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lightSaberControl : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clipHead;
    [SerializeField] private AudioClip clipMid;
    [SerializeField] private AudioClip clipTail;
    private bool loopStarted = false;
    private MeshRenderer myMeshRenderer;
    private float saberDistance;
    [SerializeField] private GameObject saberTop;
    [SerializeField] private GameObject saberBot;
    [SerializeField] private GameObject saberTopFollow;
    private Vector3 previousSaberTopPosition;
    private Collider bladeCollider;
    private int triggerCount = 0;
    [SerializeField] private bool isTestCut = false;
    private bool isBladeOn = false;
    [SerializeField] private float saberMinDistance = 1f;//conceived as per frame but should be defined as meters/(1/90 sec)
    private float throwingMinDistance = .1f;
    private Transform _saberTopTransform;
    private Transform _saberTopFollowTransform;
    private Transform _saberBotTransform;
    private float saberMinVelocity = 0;
    private float throwingSaberMinVelocity = 0;
    private bool isTriggerStay = false;
    private bool isCollisionStay = false;
    private float collisionStayTimer = 0;//if colliding for say .5 second, switch to a trigger
    private Transform _transform;
    private float saberRestTimer = 0;
    private Vector3 previousSaberBotPosition;
    private float cuttableRaycastIntervalTimer = 0;
    [SerializeField] private float cuttableRaycastInterval = 0.02f;//for stabbing and floor intersections
    private float bladeLength = 1;
    [SerializeField] private bool torqueTest = false;
    [SerializeField] private float torqueBack = 1f;
    public bool isRepelling = false;
    [SerializeField] float saberPushSqrThreshold = .0005f;//.0001f;
    //    private bool isTriggerPushed = false;
    //    private bool isPreviousTriggerPushed = false;
    //    private bool isSwitchedOn = false;
    private Animator _anim;
    // Use this for initialization
    void Start () {
        _transform = transform;
        myMeshRenderer = GetComponent<MeshRenderer>();
        //audioSource = GetComponent<AudioSource>();
        //saberTop = gameObject.GetComponent<DrawBlur>().top;
        //saberTopFollow = gameObject.GetComponent<DrawBlur>().topFollow;
        //saberBot = gameObject.GetComponent<DrawBlur>().bottom;
        //gameObject.GetComponent<Rigidbody>().ResetCenterOfMass();
        bladeCollider = GetComponent<Collider>();
        _saberTopTransform = saberTop.transform;
        _saberBotTransform = saberBot.transform;
        _saberTopFollowTransform = saberTopFollow.transform;
        previousSaberTopPosition = _saberTopTransform.position;
        previousSaberBotPosition = _saberBotTransform.position;
        throwingMinDistance = saberMinDistance * 0.1f;
        saberMinVelocity = saberMinDistance * 90f;
        throwingSaberMinVelocity = throwingMinDistance * 90f;
        myMeshRenderer.enabled = false;
        bladeCollider.isTrigger = false;
        bladeLength = (_saberTopFollowTransform.position - _saberBotTransform.position).magnitude;
        //MyDisable();
        _anim = GetComponent<Animator>();
        MyStartOff();
    }

    void OnTriggerEnter(){
        triggerCount++;
    }
    void OnTriggerStay(){
       // isTriggerStay = true;//track this and don't switch back to collider if you are still intersecting with a col
       //too many false positives
    }
    void OnTriggerExit(){
        triggerCount--;
    }
    void OnCollisionStay(){
        isCollisionStay=true;
    }
    void OnCollisionEnter(){
        isCollisionStay=true;
    }
    void OnEnable(){
            //Animator anim = GetComponent<Animator>();

            loopStarted = false;
            if (myMeshRenderer != null)
                myMeshRenderer.enabled = true;
            if (audioSource != null){
                //audioSource.pitch = 1;
                //audioSource.volume = 1;
                audioSource.clip = clipHead;
                audioSource.loop = false;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            if (_anim)
            _anim.SetBool("isOn",true);

    }
        
    public void MyEnable(){
        audioSource.loop = false;
        audioSource.Stop();
        StartCoroutine(SwitchOn());
    }

    IEnumerator SwitchOn(){//this is new
        bool done = false;
        //Animator anim = GetComponent<Animator>();
        _anim.SetBool("isOn",true);

        //GetComponent<Animator>().SetBool("isIdle",false);
        myMeshRenderer.enabled = true;

        loopStarted = false;
        if (myMeshRenderer )
            myMeshRenderer.enabled = true;
        if (audioSource ){
            audioSource.clip = clipHead;
            audioSource.loop = false;
            loopStarted = false;
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        //GetComponent<MeshRenderer>().enabled = false;
        //GetComponent<DrawBlur>().blur.SetActive(false);
        while (audioSource.isPlaying && _anim.GetBool("isOn")){
            yield return null;
        }
        //gameObject.SetActive(false);
    }

    IEnumerator SwitchOff(){
        bool done = false;
        //Animator anim = GetComponent<Animator>();
        if (_anim.GetBool("isOn")){
                _anim.SetBool("isOn",false);
            }

        audioSource.clip = clipTail;
        audioSource.Play();

        while (audioSource.isPlaying && !audioSource.loop && gameObject.activeInHierarchy){
            yield return null;
        }
        if (!_anim.GetBool("isOn"))
            myMeshRenderer.enabled = false;
        //else
            //Debug.Log("clashyclash");
        loopStarted = false;
    }

    public void MyDisable(){
        audioSource.loop = false;
        audioSource.Stop();
        StartCoroutine(SwitchOff());
    }

    public void MyStartOff(){
        bladeCollider.isTrigger = false;

        audioSource.Stop();
        //Animator anim = GetComponent<Animator>();
        if (_anim.GetBool("isOn")){
                _anim.SetBool("isOn",false);
            }
        if (!_anim.GetBool("isOn"))
            myMeshRenderer.enabled = false;
        loopStarted = false;       
    }
    void FixedUpdate()
    {
    isTriggerStay = false;
    isCollisionStay = false;
    }
    
    void Update()
    {
        bool isHitCheckTime = false;
        float _deltaTime = Time.deltaTime;
        cuttableRaycastIntervalTimer += _deltaTime;
        bool saberPush = false; //pushing saber out of hand
        if (cuttableRaycastIntervalTimer > cuttableRaycastInterval)
        {
            isHitCheckTime = true;
            cuttableRaycastIntervalTimer = 0;
        }
            // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 15;//Raycast for cuttables
        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        //layerMask = ~layerMask;
        RaycastHit hit = new RaycastHit();//don't spam Update!
        bool isHit = false;
        if (isHitCheckTime)
        {
            //to detect stabbing motion, cast from near the tip
            //isHit = Physics.Raycast(saberTop.transform.position - saberTop.transform.forward * .25f, saberTop.transform.forward, out hit, 0.35f, layerMask);
            Vector3 rayStart = _saberBotTransform.position;
            Vector3 rayDir = _saberTopTransform.position - _saberBotTransform.position;
            isHit = Physics.Raycast(rayStart, rayDir, out hit, bladeLength + .1f, layerMask);
        }
        //Animator anim = GetComponent<Animator>();

        if (gameObject.activeInHierarchy && myMeshRenderer.enabled && _anim.GetBool("isOn")) {

            saberDistance = (_saberTopTransform.position - previousSaberTopPosition).sqrMagnitude;

        }
        float smd = saberMinDistance;
        float smv = saberMinVelocity;
        JointConnection jc = _transform.parent.gameObject.GetComponent<JointConnection>();
        isRepelling = false;
        if (!jc)
        {
            if (gameObject.layer != LayerMask.NameToLayer("Lightsaber"))
            {
                Debug.Log("detached from hand");
                gameObject.layer = LayerMask.NameToLayer("Lightsaber");
            }

            smd = throwingMinDistance;
            smv = throwingSaberMinVelocity;
            float saberFakeSpeed = (_saberBotTransform.position - previousSaberBotPosition).sqrMagnitude / _deltaTime;

            if (_anim.GetBool("isOn"))
            {
                if (saberFakeSpeed < .02)//saber on but barely moving
                {
                    saberRestTimer += _deltaTime;
                    if (saberRestTimer > .5)
                    {
                        Debug.Log("switching off");
                        MyDisable();
                    }
                }
                else
                {
                    saberRestTimer = 0;
                    //saber is on but not resting

                }
                    //the saber is dropped but still on
                    //don't spam Update with raycasts, this is just a test, run a raycast against the floor to make sure it isn't stuck
                    //then use some kind of force on the saber to dislodge it
            }

            if (isHitCheckTime && myMeshRenderer.enabled && saberFakeSpeed < .25)
            {
                //Debug.Log("checking floor stuck");
                int layerMaskFloor = 1 << 11;//Raycast for cuttables
                RaycastHit hitFloor = new RaycastHit();//don't spam Update!
                Vector3 rayStart = _saberBotTransform.position - _saberBotTransform.up * .1f;
                Vector3 rayDir = _saberTopTransform.position - _saberBotTransform.position;
                bool isHitFloor = Physics.Raycast(rayStart, rayDir.normalized, out hitFloor, rayDir.magnitude, layerMaskFloor);
                if (isHitFloor)
                {
                    Debug.DrawRay(rayStart, rayDir.normalized * .3f, Color.green);
                    Debug.Log("saberStuckInFloor");
                    Rigidbody parentrb = transform.GetComponentInParent<Rigidbody>();
                    if (parentrb != null)
                    {
                        Debug.Log("unsticking");
                        isRepelling = true;
                        //Debug.Break();
                        //parentrb.AddForceAtPosition(Random.onUnitSphere * 200f, hitFloor.point);
                        //parentrb.angularVelocity = Random.onUnitSphere * 20f;
                        if (torqueTest){
                            Vector3 torqueAxis = -Vector3.Cross( hitFloor.normal, rayDir );
                            float dotProd = Mathf.Lerp(1,20,-Vector3.Dot(hitFloor.normal, rayDir.normalized));

                            parentrb.AddTorque(torqueAxis * torqueBack * dotProd);
                        }
                        //parentrb.velocity += hitFloor.normal * .25f;//new Vector3(0, .5f, 0);
                    }
                }
            }

        }
        else //saber still attached
        {
            gameObject.layer = LayerMask.NameToLayer("LightsaberInHand");
            if (jc.joint != null)
            {  //trying to identify a situation where the saber is in your hand but being pushed out by the blade collider
                if ((jc.joint.gameObject.transform.position - _transform.parent.transform.position).sqrMagnitude > saberPushSqrThreshold)//.0001
                {
                    //Debug.Log("saber is being pushed hard!");
                    saberPush = true;
                }
            }
            else
            {
                Debug.Log("saber joint disconnected!");

                Destroy(jc);//hacky but might fix some things

            }
        }
        if (saberPush)
            bladeCollider.isTrigger = true;
        else if (isHitCheckTime)//fires a ray once in a while to see if it's intersecting with something and switches it to trigger
        {//putting these things in this block is less spammy but maybe getting sloppy. revisit this logic soon!
            if (isHit && myMeshRenderer.enabled)
            {//stabbing motion
                bladeCollider.isTrigger = true;
            }
            else if (saberDistance / _deltaTime < smv)
            {
                if (!isTestCut && !isTriggerStay)
                {//this !isTriggerStay thing is an old issue that needed fixin!
                    bladeCollider.isTrigger = false;
                }
            }
            else
            {//if saber moving fast trigger is true
                if (myMeshRenderer.enabled)//only if the blade is visible should the cutting ability be enabled
                {
                    bladeCollider.isTrigger = true;
                }
            }

            if (collisionStayTimer > .5)//if the blade is being pressed into an immovable object
            {
                if (myMeshRenderer.enabled)
                    bladeCollider.isTrigger = true;
            }

            if (isTestCut && !bladeCollider.isTrigger)
            { //redundant
                bladeCollider.isTrigger = true;
            }
        }

        if (isCollisionStay){
            collisionStayTimer += _deltaTime;
        } else {
            collisionStayTimer = 0;
        }

        previousSaberTopPosition = _saberTopTransform.position;
        previousSaberBotPosition = _saberBotTransform.position;
        //if (cuttableRaycastIntervalTimer > cuttableRaycastInterval)
        //    cuttableRaycastIntervalTimer = 0;//using it multiple times so I'll reset it at the end

    }

}

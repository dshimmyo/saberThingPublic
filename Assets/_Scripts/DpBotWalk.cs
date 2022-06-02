using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DpBotWalk : MonoBehaviour
{
    private GameObject target;
    private Animator anim;
    public float walkSpeed = .025f;
    private float walkSpeedMult = 1;
    private Rigidbody rb;
    private Collider col;
    private bool isSetup = false;
    private float floorLevel = 0;
    [SerializeField] float floorAdjustSpeed = 3f;

    int floorLayerMask = 0;
    [SerializeField] int floorLayer = 11;
    Transform _transform;
    [SerializeField] float scanAheadDistance = .35f;
    private float floorCheckTimer = 0;
    private float floorCheckInterval = .5f;
    [SerializeField] bool useRootMotion = true;
    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("MainCamera");
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        if (anim != null){
            if (!useRootMotion)
        	   anim.applyRootMotion = false;//root motion will break the joints causing the bot to fall apart
        	isSetup = true;
        }
        floorLayerMask = 1 << floorLayer;//floor layer is currently set to 11
        _transform = transform;
    }
    private float GetFloorLevel()
    {
    	RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(_transform.position + Vector3.up + (_transform.forward * scanAheadDistance), Vector3.down, out hit, 1.25f, floorLayerMask))
        {
            floorCheckTimer = 0;
        	return hit.point.y;
        }
        else{
        	Debug.Log("couldn't get floor level");//might mean there's a dropoff and you should stop!!
    		return floorLevel;
        }
    }
  //This script should make the bot walk as long as it hasn't been cut which is to say, as long as the animator is enabled.
  //the animator gets disabled when the bot gets cut, and presumably when it goes to a ragdoll state, which might 
    //happen when force push gets applied to the core of dpbot
    void UpdatePosition()
    {
        floorCheckTimer += Time.deltaTime;
        if (!anim.GetBool("isWalking"))
        {
            anim.SetFloat("characterSpeed", walkSpeed);
            anim.SetBool("isWalking",true);
        }
        else
        {
            if (Time.timeScale > 0)
            {
                if (!useRootMotion)
                    _transform.position += _transform.forward * walkSpeed * Time.timeScale; 
                if (floorCheckTimer > floorCheckInterval)
                {
                    floorLevel = GetFloorLevel();//the game slows down drastically if you don't limit these raycasts
                }
                if (_transform.position.y != floorLevel)
                    _transform.position = Vector3.Lerp(_transform.position, new Vector3(_transform.position.x, floorLevel,_transform.position.z), Time.deltaTime * floorAdjustSpeed);

            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (BLINDED_AM_ME.MeshCut.isDoneInitializing && anim.enabled && isSetup)
        {
            UpdatePosition();
        	//check if it is blocked by something so it doesn't try to walk through it

        }
        //raycast downward to make sure you find the floorlevel 
    }
}

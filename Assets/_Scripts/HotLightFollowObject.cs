using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotLightFollowObject : MonoBehaviour {
    private GameObject target;
    private int submeshNum;
    private Material material;
    private Light myLight;
    private Transform _transform;
    private float hotStart = 0;
    private float hotInterval = 0;
    private float hotRatio = 0;
    private float hotTimeElapsed = 0;
    private float range = 1;
    private enum FollowStyle { none, followSubmeshMaterial, followObjectHeatSignal };
    private FollowStyle myFollowStyle = FollowStyle.none;
    private Vector3 lightPos = Vector3.zero;
    private Cuttable targCuttable;
    private float timer;
    private float updateInterval = 1f / 24f;
    public static int deltaTimeCacheUpdateFrame = 0;
    public static float _deltaTime = 0;
    private Vector3 size;
    private bool isInitialized = false;
    private Transform _mainCameraTransform;

    void Init(GameObject targ) {
        target = targ;
        size = target.GetComponent<Renderer>().bounds.size;
        range = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        isInitialized = true;
    }
    public void FollowObject(GameObject targ, int subm, Material mat){
        if (myFollowStyle != FollowStyle.followSubmeshMaterial)
            myFollowStyle = FollowStyle.followSubmeshMaterial;

        if (!isInitialized)
            Init(targ);
        //Vector3 size = targ.GetComponent<Renderer> ().bounds.size;
        //range = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        if (range > .025) {
            target = targ;
            submeshNum = subm;
            material = mat;
            hotStart = material.GetFloat ("_HeatStart");
            hotInterval = material.GetFloat ("_HeatInterval");
            hotTimeElapsed = Time.realtimeSinceStartup - hotStart;
            hotRatio = Mathf.Max (1 - (hotTimeElapsed / hotInterval), 0);
            myLight.range = range;// * 1.25f;
        }
    }

    public void FollowObject(GameObject targ)
    {
        if (myFollowStyle != FollowStyle.followObjectHeatSignal)
            myFollowStyle = FollowStyle.followObjectHeatSignal;
        if (!isInitialized)
            Init(targ);
        //Vector3 size = targ.GetComponent<Renderer>().bounds.size;
        //range = (size.x + size.y + size.z) / 3;
        targCuttable = targ.GetComponent<Cuttable>();
        
        if (range > .025)
        {
            target = targ;
            hotInterval = Cuttable.hotMetalHeatInterval;
            hotRatio = targCuttable.hotLightSignal;//Mathf.Max(1 - (hotTimeElapsed / hotInterval), 0);
            myLight.range = range * 1.25f;
            lightPos = targCuttable.hotLightPos + ((targCuttable.GetComponent<MeshFilter>().mesh.bounds.center - targCuttable.hotLightPos).normalized * .05f);
        }
    }

    // Use this for initialization
    void Start () {
        myLight = GetComponent<Light> ();
        _transform = gameObject.transform;
        _mainCameraTransform = Camera.main.transform;

	}
    void Awake()
    {
        isInitialized = false;
        if (target != null)
        {
            size = target.GetComponent<Renderer>().bounds.size;
            range = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        }
    }
    void UpdateFromHeatSignal()
    {
        hotRatio = 0;//maybe this will catch unusual problems
        if (target != null && target.activeInHierarchy)
        {
            if (!myLight.enabled)
                myLight.enabled = true;
            if (target != null)//redundant but there was an error
            {
                _transform.localPosition = target.GetComponent<Renderer>().bounds.center;
                if (targCuttable == null)
                    targCuttable = target.GetComponent<Cuttable>();
                if (targCuttable != null)//fixes a bug caused by another bug
                {
                    hotRatio = targCuttable.hotLightSignal;
                }
                else
                {
                    hotRatio = 0;
                    myLight.enabled = false;
                    target = null;
                    //Debug.Log("This is a buggy asset that is missing a cuttable class");
                }
                if (hotRatio > 0)
                {
                    myLight.enabled = true;
                    myLight.intensity = hotRatio * 2f;//(1 - Mathf.Pow(1 - hotRatio, 3)) * 2f;//vertex lights are cheaper but need a little more intensity
                    myLight.color = Color.Lerp(Color.red, Color.yellow, hotRatio/*light.intensity*/);
                }
                else
                {
                    myLight.enabled = false;
                    _transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                myLight.enabled = false;
                _transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            myLight.enabled = false;
            _transform.localPosition = Vector3.zero;
        }

    }

    void UpdateFromSubmeshMaterial()
    {
        if (target != null && target.activeInHierarchy && material != null)
        {
            myLight.enabled = true;
            _transform.localPosition = target.GetComponent<Renderer>().bounds.center;//target.transform.position;
            hotTimeElapsed = Time.realtimeSinceStartup - hotStart;
            hotRatio = Mathf.Max(1 - (hotTimeElapsed / hotInterval), 0);
            //light.range = Mathf.Lerp(range * .5f,range ,hotRatio);
            if (hotRatio > 0)
            {
                myLight.enabled = true;
                myLight.intensity = hotRatio * 2f;//1 - Mathf.Pow(1 - hotRatio, 3);
                myLight.color = Color.Lerp(Color.red, Color.yellow, hotRatio/*light.intensity*/);
            }
            else
            {
                myLight.enabled = false;
                _transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            myLight.enabled = false;
            _transform.localPosition = Vector3.zero;

        }
    }


    // Update is called once per frame
    void Update()
    {
        int frameCount = Time.frameCount;
        if (deltaTimeCacheUpdateFrame != frameCount) {//just because it's static
            deltaTimeCacheUpdateFrame = frameCount;
            _deltaTime = Time.deltaTime;
        }
        if (_mainCameraTransform == null)//happens a lot I guess
        {
            _mainCameraTransform = Camera.main.transform;
        }

        //I'm not sure if I just made this script slower by adding this heatCheckTimeMultiplier
        float heatCheckTimeMultiplier=1;
        Vector3 directionFromCamera = _transform.position - _mainCameraTransform.position;
        directionFromCamera = FastNormalize(ref directionFromCamera);
        float facingRatio = Mathf.Max(Vector3.Dot(directionFromCamera, _mainCameraTransform.forward),0);//0 to 1 signal
        heatCheckTimeMultiplier = ((1-facingRatio)*5 + 1);//slows down heat check when not in view of the main camera

        if (target != null)
        {
            timer += _deltaTime;
            if (timer > updateInterval * heatCheckTimeMultiplier)
            {
                if (myFollowStyle == FollowStyle.followSubmeshMaterial)
                    UpdateFromSubmeshMaterial();
                else if (myFollowStyle == FollowStyle.followObjectHeatSignal)
                    UpdateFromHeatSignal();
            }
        } else {
            if (myLight.enabled)
                myLight.enabled = false;
        }
	}

    private float FastSqrtInvAroundOne(float x)
    {
        const float a0 = 15.0f / 8.0f;
        const float a1 = -5.0f / 4.0f;
        const float a2 = 3.0f / 8.0f;

        return a0 + a1 * x + a2 * x * x;
    }

    Vector3 FastNormalize(ref Vector3 v)
    {
      float len_sq = v.x * v.x + v.y * v.y + v.z * v.z;
      float len_inv = FastSqrtInvAroundOne(len_sq);
      return new Vector3(v.x* len_inv, v.y* len_inv, v.z* len_inv);
    }
}

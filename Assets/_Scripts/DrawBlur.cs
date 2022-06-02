using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

using Unity.Jobs;//job system
using UnityEngine.Jobs;//job system
using Unity.Collections;//job system
using Unity.Burst;
using UnityEditor;

public class DrawBlur : MonoBehaviour {

    [SerializeField]
    private GameObject top;//gameObject at the tip of the lightsaber
    [SerializeField]
    private GameObject topFollow;//gameObject that follows the lightsaber tip delayed .16 seconds
    [SerializeField]
    private GameObject bottom;//gameObject at the base of the lightsaber
    [SerializeField]
    private GameObject bottomFollow;//gameObject that follows the lightsaber tip delayed .16 seconds
    private Vector3[] topSample;
    private Vector3[] botSample;

    [SerializeField]
    private Material material;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] tri;
    private Vector3[] normals;
    private Vector2[] uv;
    private GameObject blur;
    private int numPositions=0;
    private int numPositionsNew = 0;
    private int numVertices=0;
    public bool freeze = false;
    [SerializeField] private bool freezeOnButtonPush = false;
    public int maxPoints = 8; //increased from 4 to 8 3/26/2019 because it will look a lot better and I think it takes as much time.
    private Transform _transform;
    private GameObject blade;
    private ToolUser tu;
    [SerializeField] bool useMeshCollider = false;  //this definitely seems to help detecting cuts
    private float bladeLengthSqrd = 1;
    private bool useJobSystem = false; //much faster without the job system!!!
    //job system overhead is too high to justify running such small loops through the job system!
    private Transform _topTransform ;
    private Transform _bottomTransform;
    private Transform _topFollowTransform;
    private Transform _bottomFollowTransform;
    [SerializeField] private bool writeOutFrozenMesh = false;

    private MeshRenderer mr;
//FollowObject code
    //already using top, topFollow, bottom, and bottomFollow
    public float followDelay = .16f;//.16 is the current value assigned in the inspector
    [SerializeField] private Vector3[] topPositions;//need to be public so drawblur can access them
    [SerializeField] private Vector3[] botPositions;//need to be public so drawblur can access them
    private bool isDegenerate = false;

    //
    // The purpose of this script is to record the positions of the tip and base of the blade at every fixedUpdate
    // and then to draw a mesh in between the two points.
    // at the moment I'm cheating and getting the positions from another script. that's sloppy, gotta fix that.
    //

    void Start () {
//followObject
        numPositions = Mathf.CeilToInt(followDelay / Time.fixedDeltaTime);//should be stable, even when paused
        topPositions = new Vector3[numPositions];
        botPositions = new Vector3[numPositions];
        _topTransform = top.transform;
        _bottomTransform = bottom.transform;
        _topFollowTransform = topFollow.transform;
        _bottomFollowTransform = bottomFollow.transform;
        for (int i=0; i< numPositions; i++ ){
            topPositions[i] = _topTransform.position;
            botPositions[i] = _bottomTransform.position;
        }
//followObject

        bladeLengthSqrd = (_topTransform.position - _bottomTransform.position).magnitude;
        bladeLengthSqrd *= bladeLengthSqrd;
        //numPositions = topFollow.GetComponent<FollowObject>().numPositions; //number of points along the length of the mesh
        //this will be set by the follow code
        if (numPositions > maxPoints)
            numPositionsNew = maxPoints;
        else
            numPositionsNew = numPositions;
        topSample = topPositions;
        botSample = botPositions;

        if (!UnityEngine.XR.XRDevice.isPresent)
            gameObject.GetComponent<Renderer>().material.SetFloat("_Outline",0f);
        else
            gameObject.GetComponent<Renderer>().material.SetFloat("_Outline",1f);

        blur = new GameObject("bladeTrail");
        _transform = blur.transform;

        //with only one follow point there is only one quad so 4 points
        //if there are 4 follow points, maybe there will be 4 quads with the 5th point the current point
        //5 points is
        //top.transform.position
        //topFollow.positions[0]
        //topFollow.positions[1]
        //topFollow.positions[2]
        //topFollow.positions[3]
        //so 10 verts if we include bottom
        //4 follow points yields (numPositions+1) * 2

		//vertices = new Vector3[4];//quad
        numVertices = (numPositionsNew+1) * 2;//4
        vertices = new Vector3[numVertices];

        MeshFilter myMeshFilter = blur.AddComponent<MeshFilter>();
        mesh = new Mesh();
        myMeshFilter.mesh = mesh;
        mr = blur.AddComponent<MeshRenderer>();
        //mr.lightProbeUsage = LightProbeUsage.Off;
        blur.GetComponent<Renderer>().material = material;
        blur.GetComponent<Renderer>().material.SetFloat("_Outline",0f);
        blur.GetComponent<Renderer>().material.SetFloat("_Displacement",0f);

        //number of quads is numPositions + 1
        //number of verts is (numPositions+1) * 2
        //number of tris is 6 per quads , (numPositions + 1) * 6
        // vertices[0] = top.transform.position;
        // vertices[1] = topFollow.transform.position;
        // vertices[2] = bottomFollow.transform.position;
        // vertices[3] = bottom.transform.position;

        // mesh.vertices = vertices;
        mesh.MarkDynamic();
        RefreshVerts();

        tri = new int[(numPositionsNew) * 6];//6

        for (int i=0; i<numPositionsNew; i++)
        { //this is that fucking difficult algorithm I couldn't figure out
            tri[i*6] = i*2;//0
            tri[i*6+1] = i*2+1;//2;////1;//1
            tri[i*6+2] = i*2+3;//2;//2
            
            tri[i*6+3] = i*2+3;//2;//2
            tri[i*6+4] = i*2+2;//1;//3;//3
            tri[i*6+5] = tri[i*6];//0
            //0 2 4 6
            //1 3 5 7
            //013 320 235 542 457 764   679 986
            //i=0 013 320
            //i=1 235 542
            //i=2 457 764
        }

        mesh.triangles = tri;

        normals = new Vector3[numVertices];//4
        
        // normals[0] = -Vector3.forward;
        // normals[1] = -Vector3.forward;
        // normals[2] = -Vector3.forward;
        // normals[3] = -Vector3.forward;

        for(int i=0; i<numVertices; i++){
            normals[i] = -Vector3.forward;
        }
        mesh.normals = normals;
        mesh.RecalculateNormals();


        uv = new Vector2[numVertices];//4
        //0 2 4 6  -->v=0
        //1 3 5 7  -->v=1
        // uv[0] = new Vector2(0, 0);
        // uv[1] = new Vector2(0, 1);
        // uv[2] = new Vector2(1, 0);
        // uv[3] = new Vector2(1, 1);

        for (int i=0; i<numVertices; i+=2){
            uv[i]   = new Vector2((float)i/(numPositionsNew*2),1);
            uv[i+1] = new Vector2((float)i/(numPositionsNew*2),0);
        }
        
        mesh.uv = uv;
        if (useMeshCollider) {
            MeshCollider mc = blur.AddComponent<MeshCollider> ();
            mc.convex = true;
            mc.isTrigger = true;
            mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;

            mc.inflateMesh = true;
            mc.skinWidth = .03f;//tried it at 1 and it still throws errors on a degenerate mesh
            mc.enabled = false;

        }
        blur.layer = 12;
        blade = gameObject;//gameObject.transform.parent.gameObject;
        tu = blade.GetComponent<ToolUser> ();
	}

//followObject
    void AddPosition(Vector3 topPos, Vector3 botPos){//add the position to the zero element
        for (int i=numPositions-1; i > 0; i--){
            topPositions[i]=topPositions[i-1];
            botPositions[i]=botPositions[i-1];
        }
        botPositions[0]=botPos;
        topPositions[0]=topPos;
    }
    Vector3 GetTopPosition(){ // get last position (fully delyed position)
        return topPositions[numPositions-1];
    }
    Vector3 GetBottomPosition(){ // get last position (fully delyed position)
        return botPositions[numPositions-1];
    }

//followObject

/*

	void OnTriggerEnter (Collider other){ //this doesn't really make sense at all
        if (other.gameObject.tag == "Cuttable"){
            //Debug.Log("HackyTriggerEnter");
            if (tu == null)
                tu = blade.GetComponent<ToolUser> ();

            if (tu != null)
                tu.ExternalOnTriggerEnter(other);
        }
    }
     void OnTriggerStay (Collider other){
         //Debug.Log("HackyTriggerStay");
         if (tu != null)
            if (other != null)
                tu.ExternalOnTriggerEnter(other);
     }
    void OnTriggerExit (Collider other){
        if (other.gameObject.tag == "Cuttable"){
            //Debug.Log("HackyTriggerExit");
            if (tu == null)
                tu = blade.GetComponent<ToolUser> ();
            else
                tu.ExternalOnTriggerExit(other);
        }
    }
*/
    
    //jobSystem - create a struct
    [BurstCompile(FloatPrecision.Low,FloatMode.Fast,CompileSynchronously = false)]
    public struct RefreshVertsJob : IJobParallelFor
    {
        //[ReadOnly]
        //[NativeDisableParallelForRestriction]
        //public NativeArray<Vector3> Vertices;
        [WriteOnly]
        [NativeDisableParallelForRestriction]

        public NativeArray<Vector3> NewVertices;
        [ReadOnly]
        public NativeArray<Vector3> TopSample;
        [ReadOnly]
        public NativeArray<Vector3> BotSample;
        //[ReadOnly]
        //public int IsDegenerate;
        [ReadOnly]
        public float Interval;
        //public int NumPos;
        //skip first 2 verts which get handled externally
        public void Execute(int i)
        {
            NewVertices[(i * 2) + 2] = TopSample[(int)(i * Interval)];
            NewVertices[(i * 2) + 3] = BotSample[(int)(i * Interval)];

        }
    }

    private void ExecuteVertRefreshJob(float interval,int numPosNew, Vector3[] newVerts)
    {
        //NativeArray<Vector3> nativeVerts = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob);
        NativeArray<Vector3> nativeTopSample = new NativeArray<Vector3>(topSample.Length, Allocator.TempJob);
        NativeArray<Vector3> nativeBotSample = new NativeArray<Vector3>(botSample.Length, Allocator.TempJob);
        NativeArray<Vector3> nativeNewVerts = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob);//copy to a new array so you can run in parallel
        //nativeVerts.CopyFrom(vertices);
        nativeTopSample.CopyFrom(topSample);
        nativeBotSample.CopyFrom(botSample);
        nativeNewVerts.CopyFrom(vertices);
        //nativeIsDegenerate[0] = isDegenerate;
        //nativeInterval[0] = interval;
        //isHotNativeArray.CopyFrom(isHotArray);
        RefreshVertsJob job = new RefreshVertsJob()
        {
            //Vertices = nativeVerts, //parameters for job
            NewVertices = nativeNewVerts,
            TopSample = nativeTopSample,
            BotSample = nativeBotSample,
            //IsDegenerate = isDegenerate,
            Interval = interval
            //NumPos = numPosNew

        };
        JobHandle jobHandle = job.Schedule(numPosNew,32);//was 5, I dunno //this might be a mistake trying to make this use threads, check the code that tracks the stored positions
        //numPosNew, 128//8 batch size
        jobHandle.Complete();

        nativeNewVerts.CopyTo(newVerts);
        nativeNewVerts.Dispose();
        //nativeVerts.Dispose();
        nativeTopSample.Dispose();
        nativeBotSample.Dispose();
        //nativeIsDegenerate.Dispose();
        //nativeInterval.Dispose();
    }

    void RefreshVerts(){
        if (Mathf.Abs(Vector3.Distance(_topTransform.position, _bottomTransform.position)) < .01)
            isDegenerate = true;

        vertices[0] = _topTransform.position;
        vertices[1] = _bottomTransform.position;//topSample[0];

        float interval = numPositions / numPositionsNew;
        Vector3[] newVerts = new Vector3[vertices.Length];//vertices;

        if (useJobSystem)
        {

            ExecuteVertRefreshJob(interval, numPositionsNew, newVerts);
            mesh.vertices = newVerts;//vertices;       

        }
        else
        {
            for (int i = 0; i < numPositionsNew; i++)
            {
                vertices[(i * 2) + 2] = topSample[(int)(i * interval)];
                vertices[(i * 2) + 3] = botSample[(int)(i * interval)];
                if (isDegenerate)
                {
                    vertices[(i * 2) + 2].x += .0001f * i;//was degenerateOffset
                    vertices[(i * 2) + 3].y += .0001f * i;//was degenerateOffset
                }
            }            
            mesh.vertices = vertices;       

        }

        mesh.RecalculateBounds ();//maybe this is unnecessary, try to make one big bounds region

        if (useMeshCollider){
            MeshCollider mc = blur.GetComponent<MeshCollider> ();
            //if (Vector3.Distance(vertices[0],vertices[2]) > .00001){
            if (!isDegenerate){
               if (mc != null){
                    mc.sharedMesh = null;
                    mc.sharedMesh = mesh;//this is where the meshcollider would get rebuilt
                    mc.enabled = true;
                    //mc.cookingOptions = MeshColliderCookingOptions.None;//turns everything on
                }
            } else {
                if (mc != null){
                    mc.sharedMesh = null;
                }
            }
        }
    }

    void OnDisable(){
        //Transform _topTransform = top.transform;
        vertices[0] = _topTransform.position;//makes it basically invisible so that it doesn't appear when the blade gets switched on elsewhere
        vertices[1] = _topTransform.position;
        vertices[2] = _topTransform.position;
        vertices[3] = _topTransform.position;

        mesh.vertices = vertices;       
        mesh.RecalculateBounds ();
        if (blur != null)
            blur.SetActive(false);
    }
    void OnEnable(){
        if (blur != null)
            blur.SetActive(true);
    }
    // Update is called once per frame
    private bool previousFreeze = false;
	void Update () {
        if (freezeOnButtonPush && !freeze)
        {
            if (Input.GetButton("Fire1"))
                freeze = true;
        }
        isDegenerate = false; //resets before next frame - gives a chance for other events to determine degeneracy

        if (!freeze){
            //if (vertices[0])
            RefreshVerts();//updates the mesh
        }
        #if UNITY_EDITOR

        if (freeze && !previousFreeze && writeOutFrozenMesh)
        {
            Mesh meshToSave = Object.Instantiate(mesh) as Mesh;
            meshToSave.Optimize();
            AssetDatabase.CreateAsset(meshToSave, "Assets/frozenTrailMesh.mesh");
            AssetDatabase.SaveAssets();
        }
        #endif

        if (mr != null)
            if (isDegenerate)
                mr.enabled = false;
            else mr.enabled = true;

        previousFreeze = freeze;
	}
    void FixedUpdate () {

        _topFollowTransform.position = GetTopPosition();
        _bottomFollowTransform.position = GetBottomPosition();
        AddPosition(_topTransform.position, _bottomTransform.position);

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Valve.VR;
using Unity.Jobs;//job system
using UnityEngine.Jobs;//job system
using Unity.Collections;//job system
using Unity.Burst;
using UnityEngine.XR;
public abstract class Cuttable : MonoBehaviour, IBurnable
{
    public Material cutMat;
    public Vector3 cutDirection;
    public Vector3 cutPosition;
    public int submeshIndex;//set by meshCut after a cut
    private Material[] mats;
    private float boundsVolume = 1;
    private float minBound = 0;
    [SerializeField] private bool isHots;
    private float timer = 0;
    private float previousCheckTime = 0;
    private float hotCheckInterval = 5;
    protected float killTime = 5;
    static private GameObject hotMetalLight;
    static private GameObject[] hotMetalLights;
    static private int numHotLights = 50;//what if you just cycle through the lights first-come-first-served? it would make things wicked fast
    static private int hotLightIndex = 0;
    private float minCollisionVelocity = .75f;//.025f;
    private float pitch = 1;
    static public float hotMetalHeatInterval = 15f;//was 15 but changed because of staggered cooling over 5 frames
    [SerializeField] private bool useUV2HeatSignal = true;
    public float hotLightSignal = 1;
    public Vector3 hotLightPos = Vector3.zero;
    static public float hotMetalCoolingInterval = .1f;//cooling every .1 seconds
    private float deltaHeatReductionTime = 0;//time elapsed between cooling
    public float minDestructVolume = 0;
    private GameObject cutter;
    private float bladeInsertTime = .75f;//less than 1 second, not sure why some objects take longer to destruct
    private float bladeInsertTimer = 0;
    private bool bladeInserted = false;
    private Vector3 longAxis = new Vector3(1,0,0);
    private Vector3 lastHotTouchPoint = Vector3.zero;
    public bool isTestUV2 = false;//when enabled adds heat to the uv2 signal
    private bool isInitialized = false;
    public static bool useJobSystem = true;//actually much faster with the job system :) from like .5ms to 2,3,4...
    private bool isSplitVertices = false;//if the verts are split displacement will be an issue
    //private float burnMult = 1;
    //private int recursionDepth = 0;
    private static bool useDisplacement = true;//disp definitely responsible for shearing of polygons
    private static int dontDisplace = 1;//1;//new system which adds the displaceSignal but doesn't modify the mesh
    private bool isTessellate = true;//this is set by meshcut or some shit this is hacky
    private bool isTessellatingInProgress = false;
    private bool isAdaptiveTessellate = false;//true;//this is the switch
    private float invertedScale = 1;
    private Transform _transform;
    private int debugCubeNum = 0;
    private bool isRecursive = true; //disable for debugging purposes
    private bool isDebug = false;
    private bool isUV2Reset = false;
    public float angle = 100;//60
    //[SerializeField] bool triangulateFromStart = false;//stupid hacky test for troublesome objects
    private float displacementLimit = 0.025f;
    private float maxTessellationTime = .0005f;//.005f;//.0005f;
    [SerializeField] private bool isNoMesh = false;
    private float tessellationMinLengthRatio = 1.5f;//1.25f;//if the length / Range > tessellationMinLengthRatio it will divide it //can't decide if 1.5 is making things cut badly 1.5 is tight 1.75 maybe made more holey cuts
    private Transform _mainCameraTransform;
    private List<int> heatVertMap = new List<int>();
    private float burnRange = .07f;//.07f;//was .005 which represented distance squared, switched back to regular distance
    private float minHeatForDisplacement = .1f;
    [SerializeField] bool smoothNormals = false;
    private Mesh localMesh;//can't use sharedmesh in case there are instances of this asset
    private float burnInterval = .030f;//30ms?
    private float previousBurnTime = 0;
    private float heatBurnMultiplier = 3f;
    private float _deltaTime = 0;
    private bool toggleTesselateWithLists = true;//after 2 days of testing, lists are far superior than arrays, even if we resize arrays with a buffer
    private bool skipUpdate = false;//maybe burns can suggest skipping an update?
    //note to self: make tessellation/displacement run async so it doesn't mess up the frame rate
    private MeshRenderer mr;
    private MeshFilter mf;
    private Renderer ren;
    private AudioManager _audioManager;
    public bool GetSmoothNormals() 
    {
        return smoothNormals;
    }
    public void SetSmoothNormals(bool value) 
    {
        smoothNormals = value;
    }
    public float GetSmoothNormalsAngle ()
    {
        return angle;
    }
    public bool GetIsTessellatingInProgress()
    {
        return isTessellatingInProgress;
    }

    public void SetTessellate(bool mybool)//this gets set to false by tooluser to prevent cuts and displacement from clashing
    {
        isTessellate = mybool;
    }

    public bool GetTessellate()
    {
        return isTessellate;
    }

    private int GetClosestPointIndex(Vector3 point)
    {
        point = _transform.InverseTransformPoint(point);
        //Mesh mesh = localMesh;// mf.sharedMesh;
        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestVertex = Vector3.zero;
        int index = 0;
        Vector3[] _meshVertices = localMesh.vertices;
        // scan all vertices to find nearest
        for (int i = 0; i < _meshVertices.Length; i++)
        {
            Vector3 vertex = _meshVertices[i];
            Vector3 diff = point - vertex;
            float distSqr = diff.sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestVertex = vertex;
                index = i;
            }
        }
        return index;
    }

    [BurstCompile(FloatPrecision.Low,FloatMode.Fast,CompileSynchronously = false)]
    public struct PointsInRangeJob : IJobParallelFor
    {
        public NativeArray<float> GoodArray;//readWrite so you can combine results
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        //public Vector3 Point;
        public NativeArray<Vector3> Points;
        [ReadOnly]
        public float Range;
        [ReadOnly]
        public NativeArray<Vector3> Vertices;

        public void Execute(int index)
        {
            int numPoints = Points.Length;
            Vector3 vertex = Vertices[index];

            float goodValue = 0;//loop through points, find best goodValue
            for (int i = 0; i<numPoints; i++){
                Vector3 diff = Points[i] - vertex;//loop through points and find the 
                float dist = diff.magnitude;
                if (dist < Range)
                {
                    float value = Mathf.Max(Mathf.Min(Mathf.Lerp(1, 0, dist / Range), 1f), 0f);//the higher the better
                    if (value > goodValue)
                    {
                        goodValue = value;
                        //GoodArray[index] = value;
                    }
                }
                else if (goodValue <= 0)
                {
                    if (goodValue<=0)//if the current stored result is unassigned or is a stored distance
                    {
                        if (dist < -goodValue) //record the distance to the closest point
                            goodValue = -dist;//-distSqr
                    }
                }
            }
            GoodArray[index] = goodValue;
        }
    }

    /*private int[] GetPointsInRange(Vector3 point, float range)
    {
        point = _transform.InverseTransformPoint(point);
        Mesh _mesh = mf.mesh;
        Vector3[] _meshVertices = _mesh.vertices;
        range /= _transform.localScale.x;
        int[] returnArray = new int[0];
        List<int> indices = new List<int>();
        float[] isGoodArray = new float[_meshVertices.Length];
        if (useJobSystem)
        {
            NativeArray<float> nativeGoodArray = new NativeArray<float>(_mesh.vertexCount, Allocator.TempJob);
            NativeArray<Vector3> nativeMeshVertices = new NativeArray<Vector3>(_mesh.vertexCount, Allocator.TempJob);
            nativeMeshVertices.CopyFrom(_mesh.vertices);
            PointsInRangeJob job = new PointsInRangeJob()
            {
                GoodArray = nativeGoodArray,//write only
                Point = point,//read only
                Range = range, //read only
                Vertices = nativeMeshVertices//read only
            };
            JobHandle jobHandle = job.Schedule(_mesh.vertexCount, 8);
            jobHandle.Complete();
            nativeGoodArray.CopyTo(isGoodArray);
            nativeGoodArray.Dispose();
            nativeMeshVertices.Dispose();
            for (int i = 0; i<_mesh.vertexCount; i++)
                if (isGoodArray[i] > 0.0000001)
                    indices.Add(i);
            returnArray = indices.ToArray();
            //make a job that takes an array the size of all of the vertices
            //return 1 if good 0 if bad, the indices are what they are
        }
        else
        {
            for (int i = 0; i < _mesh.vertexCount; i++)
            {
                Vector3 vertex = _meshVertices[i];
                Vector3 diff = point - vertex;
                float distSqr = diff.sqrMagnitude;
                if (distSqr < range)
                {
                    indices.Add(i);
                }
            }
            returnArray = indices.ToArray();
        }
        return returnArray;// indices.ToArray();
    }*/

    private bool GetGoodArray(Vector3[] points, float range, ref float[] isGoodArray, out bool displaceable)
    {
        for (int i=0; i<points.Length; i++)
        {
            points[i] = _transform.InverseTransformPoint(points[i]);//convert hit points to localSpace
        }
        NativeArray<Vector3> nativePoints = new NativeArray<Vector3>(points.Length, Allocator.TempJob);
        nativePoints.CopyFrom(points);
        //points[0] = _transform.InverseTransformPoint(point);
        //Mesh _mesh = localMesh;// mf.sharedMesh;
        range *= invertedScale;//if the asset was scaled down this range should be scaled up because local space will be the original cube
        float[] returnArray = new float[0];
        List<int> indices = new List<int>();
        //isGoodArray = new float[_mesh.vertexCount];
        NativeArray<float> nativeGoodArray = new NativeArray<float>(localMesh.vertexCount, Allocator.TempJob);
        nativeGoodArray.CopyFrom(isGoodArray);
        NativeArray<Vector3> nativeMeshVertices = new NativeArray<Vector3>(localMesh.vertexCount, Allocator.TempJob);
        nativeMeshVertices.CopyFrom(localMesh.vertices);
        PointsInRangeJob job = new PointsInRangeJob()
        {
            GoodArray = nativeGoodArray,//readwrite
            Points = nativePoints,//read only
            Range = range, //read only
            Vertices = nativeMeshVertices//read only
        };
        JobHandle jobHandle = job.Schedule(localMesh.vertexCount, 8);
        jobHandle.Complete();
        nativeGoodArray.CopyTo(isGoodArray);
        nativeGoodArray.Dispose();
        nativeMeshVertices.Dispose();
        nativePoints.Dispose();
        returnArray = isGoodArray;// indices.ToArray();

        //isGoodArray contains the distance values within range
        //displaceable simply tells the calling function whether it should run tessellation/displacement
        displaceable = false;
        Vector2[] _mesh_uv2 = localMesh.uv2;
        bool returnValue = false;
        for (int i=0; i<returnArray.Length; i++){
            if (returnArray[i] > 0){
                returnValue = true;
                if (_mesh_uv2[i].y <= .026) {
                    displaceable = true;
                    break;
                }
            }
        }
        return returnValue;

    }

    public void Burn(RaycastHit hit)
    {//IBurnable interface
        //skipUpdate = true;
        //disabled single non-consecutive burns with displacement because it could be moving fast //re-enabled displacement
        if (toggleTesselateWithLists)
        {
            BurnNice(new RaycastHit[] { hit },true);//StartCoroutine(BurnNice(hit.point));
        }
        else
        {
            BurnNiceWithArrays(new RaycastHit[] { hit },true);
        }
    }

    public void Burn(RaycastHit[] hits)
    {
        //skipUpdate = true;

        if (toggleTesselateWithLists)
        {
            BurnNice(hits,true);//StartCoroutine(BurnNice(hit.point));
        }
        else //use arrays the old way
        {
            BurnNiceWithArrays(hits,true);
        }
    }

    public void Burn(RaycastHit hit, RaycastHit previousHit)//overloaded version for burning a line
    {//IBurnable interface
        //skipUpdate = true;

        if ((hit.point - previousHit.point).sqrMagnitude < .01)
        {//short distance
            if (toggleTesselateWithLists)
            {
                BurnNice(new RaycastHit[] { hit },true);//StartCoroutine(BurnNice(hit.point));
            }
            else
            {
                BurnNiceWithArrays(new RaycastHit[] { hit },true);
            }
        }
        else
        {
        
            float distanceBetweenHits = Vector2.Distance(hit.point, previousHit.point);//trick them into overlapping
            int numSpots = 2;
            if (distanceBetweenHits > .1)
                numSpots = Mathf.CeilToInt(distanceBetweenHits / .1f);
            List<RaycastHit> hitsList = new List<RaycastHit>();
            for (int i = 0; i < numSpots; i++)
            {
                Vector3 newPoint = Vector3.Lerp(hit.point, previousHit.point, (float)(i / numSpots));
                Vector3 newNormal = Vector3.Lerp(hit.normal, previousHit.normal, (float)(i / numSpots));

                RaycastHit newHit = new RaycastHit();
                newHit.point = newPoint;
                newHit.normal = newNormal;

                hitsList.Add(newHit);
            }
            if (toggleTesselateWithLists)
            {
                BurnNice(hitsList.ToArray(),true);
            }
            else
            {
                BurnNiceWithArrays(hitsList.ToArray(),true);
            }
        }
    }

    [BurstCompile(FloatPrecision.Low,FloatMode.Fast,CompileSynchronously = false)]
    public struct AddHeatToUV2Job : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> MeshUV2Array;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> GoodArray;

        [ReadOnly]
        public NativeArray<int> HeatVertMap;
        [ReadOnly]
        public int DontDisplace;
        public float MinHeatForDisplacement;
        public float HeatMultiplier;
        private float signalMin;

        public void Execute(int index)
        {
            int newIndex = HeatVertMap[index];
            signalMin = .05f;////.25f;

            //if (GoodArray[newIndex] > .0000001)
            {
                float newUV2X = MeshUV2Array[newIndex].x;
                float newUV2Y = MeshUV2Array[newIndex].y;
                if (newUV2X < 1)
                    newUV2X = Mathf.Min((newUV2X + .2f) * HeatMultiplier,1);

                if (DontDisplace==1)//if don't displace then add displacement signal to the uv
                {
                    //float dispValue = MeshUV2Array[newIndex].y;
                    if (newUV2Y < 1 && newUV2X > MinHeatForDisplacement)
                    {
                        float signal = Mathf.Lerp(0.5f,1f,HeatMultiplier * (GoodArray[newIndex] - signalMin) / (1-signalMin));//1;// 
                        newUV2Y = Mathf.Min(newUV2Y + .1f * signal, .99f);
                    }
                    //Vector2 newHeatVec = new Vector2(MeshUV2Array[newIndex].x, dispValue);
                    //MeshUV2Array[newIndex] = newHeatVec;//slightly faster way to encode this data?
                }

                MeshUV2Array[newIndex] = new Vector2(newUV2X,newUV2Y);

                //if (MeshUV2Array[newIndex].x < 1)//.8 looks better
                    //MeshUV2Array[newIndex] = new Vector2(Mathf.Min(MeshUV2Array[newIndex].x + .2f,1), MeshUV2Array[newIndex].y);
            }
            
        }
    }

    [BurstCompile(FloatPrecision.Low,FloatMode.Fast,CompileSynchronously = false)]
    public struct HeatDisplacementJob : IJobParallelFor
    {//no heat gets added in this job, just displacement!
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> MeshUV2Array;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> GoodArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> Vertices;
        //[ReadOnly]
        //public NativeArray<Vector3> Normals;
        [ReadOnly]
        public Vector3 AltNormal;
        [ReadOnly]
        public float DispScale;
        //[ReadOnly]
        //public float DispLimit;
        public int DontDisplace;
        private float signalMin;
        [ReadOnly]
        public NativeArray<int> HeatVertMap;

        public void Execute(int index)
        {
            int newIndex = HeatVertMap[index];

            signalMin = .05f;////.25f;

            if (GoodArray[newIndex] > signalMin)//.07//.0000001 //narrow part of the "good" range makes a more pronounced displacement effect
            {
                float signal = Mathf.Lerp(0.5f,1f,(GoodArray[newIndex] - signalMin) / (1-signalMin));//1;// 

                //MeshUV2Array[index] += new Vector2(0,.01f * signal);//store displacement in uv2.y
                float dispValue = MeshUV2Array[newIndex].y;
                if (MeshUV2Array[newIndex].y < 1)
                {
                    dispValue = Mathf.Min(MeshUV2Array[newIndex].y + .1f * signal, .999f);
                }
                Vector2 newHeatVec = new Vector2(MeshUV2Array[newIndex].x, dispValue);
                MeshUV2Array[newIndex] = newHeatVec;//slightly faster way to encode this data?

                if (DontDisplace != 1){
                    if (MeshUV2Array[newIndex].x > .9 && MeshUV2Array[newIndex].y < 1 /*DispLimit*/){//uv2.y should not get close to 1 unless set deliberately i.e. cut faces
                        //Vertices[index] -= AltNormal* .01f * signal * DispScale;//tracking displacement using uv2.y
                        float dispMult = .01f * signal * DispScale;// * DispScale;//should the displacement signal be scaled, no it should be applied in worldspace in the shader
                        // Vector3 displacedVector = new Vector3(  Vertices[index].x - Normals[index].x * dispMult,
                        //                                         Vertices[index].y - Normals[index].y * dispMult, 
                        //                                         Vertices[index].z - Normals[index].z * dispMult);
                        Vector3 displacedVector = new Vector3(  Vertices[newIndex].x - AltNormal.x * dispMult,
                                                                Vertices[newIndex].y - AltNormal.y * dispMult, 
                                                                Vertices[newIndex].z - AltNormal.z * dispMult);
                        //Vertices[index] -= Normals[index] * dispMult;//tracking displacement using uv2.y
                        Vertices[newIndex] = displacedVector;

                        //altnormal works great on the sphere and other smooth surfaces, but is bad on things like the cube where it breaks apart at the corners
                        //altnormal was a cool idea for optimization but the average vertex normal makes more sense
                        //but then how can I deal with inside-out geometry?
                    }
                }
            }
            
        }
    }

        static bool MagnitudeLessThan(float a, float b)
        {
            if (a < 0)
                a = -a;
            if (a < b)
                return true;
            else
                return false;

        }
    static bool Vector3Compare(Vector3 vecA, Vector3 vecB)
    {
        if (MagnitudeLessThan((vecA.x - vecB.x), 0.0001f))
            if (MagnitudeLessThan((vecA.y - vecB.y), .0001f))
                if (MagnitudeLessThan((vecA.z - vecB.z), .0001f))
                    return true;
                else
                    return false;
            else
                return false;
        else
            return false;
    }
    static Vector3 Vector3Add(Vector3 vecA, Vector3 vecB)
    {
        vecA.x += vecB.x;
        vecA.y += vecB.y;
        vecA.z += vecB.z;
        return vecA;
    }
    static void Vector3AddEquals(ref Vector3 vecA, Vector3 vecB)
    {
        vecA.x += vecB.x;
        vecA.y += vecB.y;
        vecA.z += vecB.z;
    }

    private struct VertexKey
    {
        private readonly long _x;
        private readonly long _y;
        private readonly long _z;
 
        // Change this if you require a different precision.
        private const int Tolerance = 100000;
 
        // Magic FNV values. Do not change these.
        private const long FNV32Init = 0x811c9dc5;
        private const long FNV32Prime = 0x01000193;
 
        public VertexKey(Vector3 position) {
            //_x = (long)(Mathf.Round(position.x * Tolerance));
            //_y = (long)(Mathf.Round(position.y * Tolerance));
            //_z = (long)(Mathf.Round(position.z * Tolerance));
            _x = (long)(position.x * Tolerance);//maybe round isn't necessary?
            _y = (long)(position.y * Tolerance);
            _z = (long)(position.z * Tolerance);
        }
 
        public override bool Equals(object obj) {
            var key = (VertexKey)obj;
            return _x == key._x && _y == key._y && _z == key._z;
        }
 
        public override int GetHashCode() {
            long rv = FNV32Init;
            rv ^= _x;
            rv *= FNV32Prime;
            rv ^= _y;
            rv *= FNV32Prime;
            rv ^= _z;
            rv *= FNV32Prime;
 
            return rv.GetHashCode();
        }
    }
 
    private struct VertexEntry {
        public int MeshIndex;
        public int TriangleIndex;
        public int VertexIndex;
 
        public VertexEntry(int meshIndex, int triIndex, int vertIndex) {
            MeshIndex = meshIndex;
            TriangleIndex = triIndex;
            VertexIndex = vertIndex;
        }
    }

    void BurnNiceWithArrays(RaycastHit[] hits, bool isDisplaceBurn)
    {

        float currentTime = Time.time;
        if (currentTime - previousBurnTime < burnInterval) { return; }
        skipUpdate = true;
        previousBurnTime = currentTime;

        //IBurnable interface
        //make a burn mark at the hit location;
        RaycastHit hit = hits[0];
        Mesh mesh = localMesh;// mf.sharedMesh;
        Vector2[] mesh_uv2 = mesh.uv2;//heat
        Vector3[] mesh_vertices = mesh.vertices; //disp
        Vector3[] mesh_normals = mesh.normals;//disp
        Vector2[] mesh_uv = mesh.uv;//tessellation
        Vector4[] mesh_tangents = mesh.tangents;//tessellation

        //can put the tessellation code before GetGoodArray... by first figuring out which triangles are good candidates for tessellation
        //tessellation will help both the heat shader and displacement but work independently of both...
        float[] goodArray = new float[mesh_vertices.Length];// = GetGoodArray(hitPoint, .02f);//finds points in a mesh that are within range of the hitPoint
        bool isDisplaceable=false;
        //Step 1: look for points near burn
        bool goodPointsFound = false;// GetGoodArray(hitPoint, .005f, ref goodArray, out isDisplaceable);

        Vector3[] hitPoints = new Vector3[hits.Length];
        bool foundUndisplacedPoints = false;
        for (int j=0; j<hits.Length; j++)
        {
            hitPoints[j] = hits[j].point;
            if (hits[j].textureCoord2.y < 0.01) {
                foundUndisplacedPoints = true;
                if (isDebug) Debug.Log("undisplaced points found");
            }
        }
        goodPointsFound = GetGoodArray(hitPoints, burnRange, ref goodArray, out isDisplaceable);

        //Step 1.2: if no points found Intersect Tri - wonky experiment
        //if it's pointy, split it down the long axis, otherwise project a point from the hit

        Dictionary<VertexKey, VertexEntry> tessDictionary = new Dictionary<VertexKey, VertexEntry>() ;//for each vertexKey make a list of normals
        float tessellationStartTime = Time.realtimeSinceStartup;
        bool tessellationTimeOut = false;
        if (!goodPointsFound)
        {
            if (isDebug)
                Debug.Log("no points found in range of the burn. subMeshCount: " + mesh.subMeshCount.ToString());

            if (isTessellate && isAdaptiveTessellate && BLINDED_AM_ME.MeshCut.isDoneInitializing && isDisplaceBurn)
            {
                Vector3 point = _transform.InverseTransformPoint(hit.point);

                //get the nearest point that is facing in the same direction as the hit point-ish
                if (isDebug) Debug.Log("Getting the nearest out-of-range point");
                //int minIndex = -1;
                float minValue = 10000;//start with a hypothetical very large number
                for (int i=0; i<mesh_vertices.Length; i++){
                    if (goodArray[i] < 0){//real distance is hidden in negative range
                        if (-goodArray[i]<minValue && Vector3.Dot(mesh_normals[i],hit.normal) > 0.5){
                            minValue = -goodArray[i];
                            //minIndex = i;
                        }
                    }
                }
                int[] minIndices = new int[20];//no more than 10
                int indexCount = 0;
                for (int i=0; i<mesh_vertices.Length; i++){
                    if (goodArray[i] < 0){//real sqrdistance is hidden in negative range
                        if (-goodArray[i] == minValue){
                            if (indexCount < 20)
                                minIndices[indexCount++]=i;
                            else
                            break;
                        }
                    }
                }
                //Debug.Log("nearest points found: " + indexCount.ToString());
                goodPointsFound = true;

                for (int j=0; j<indexCount; j++){
                    int minIndex = minIndices[j];
                    goodArray[minIndex]=.01f;
                    goodPointsFound = true;
                    List<int> trianglesAlreadyTessellated = new List<int>();
                    bool triFound = false;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {//Loop through submeshes
                        int[] mesh_triangles = mesh.GetTriangles(sub);// mesh.GetTriangles(sub);//mesh.triangles;

                        int numTriangles = mesh_triangles.Length;
                        for (int i=0; i< mesh_triangles.Length; i+=3)
                        {//loop through triangles to find the ones that include the point
                            int triIndex = i/3;
                            int iA = mesh_triangles[i];
                            int iB = mesh_triangles[i + 1];
                            int iC = mesh_triangles[i + 2];
                            if (iA == minIndex || iB == minIndex || iC == minIndex){//if you've found an adjacent triangle
                                //check to see if the hitPoint is inside it
                                Vector3 pointA = mesh_vertices[iA];
                                Vector3 pointB = mesh_vertices[iB];
                                Vector3 pointC = mesh_vertices[iC];
                                Plane triPlane = new Plane(pointA, pointB, pointC);
                                float distanceToPoint = triPlane.GetDistanceToPoint(point);//local space point
                                if (distanceToPoint < .001 && distanceToPoint > -.001){
                                    Vector3 newPoint = point + (-triPlane.normal * distanceToPoint);//projecting point onto the tri's plane
                                    if (IsInside(pointA,pointB,pointC,newPoint))
                                    {
                                        int[] triangle = new int[] {iA, iB, iC};
                                        float range = .05f;//.075f;//.05//.03f;//.01
                                        int numVerts = mesh_vertices.Length;
                                        if (!trianglesAlreadyTessellated.Contains(triIndex))
                                        {
                                            SplitTriangleAtPoint(
                                                    triangle, //int[3]
                                                    triIndex, //int
                                                    sub, //int
                                                    range, //float
                                                    ref mesh_vertices,
                                                    ref mesh_normals,
                                                    ref mesh_uv,
                                                    ref mesh_uv2,
                                                    ref mesh_tangents,
                                                    ref mesh_triangles,
                                                    ref tessDictionary, //new verts
                                                    ref goodArray, //float[]
                                                    0,//recursion depth
                                                    newPoint,
                                                    ref numVerts,
                                                    ref numTriangles
                                                    );
                                            trianglesAlreadyTessellated.Add(triIndex);
                                            break;
                                        }else{
                                            if (isDebug)
                                                Debug.Log("Triangle already tessellated");
                                        }
                                    }
                                }//distance to point
                                else
                                {
                                    if (isDebug)
                                        Debug.Log("tessellation skipped because the triangle seems too far from the hit point");
                                }
                            }
                            if (triFound)
                                break;
                        }//loop through tris
                        mesh.vertices = mesh_vertices;
                        mesh.normals = mesh_normals;
                        mesh.uv = mesh_uv;
                        mesh.uv2 = mesh_uv2;
                        mesh.tangents = mesh_tangents;
                        mesh.SetTriangles(mesh_triangles,sub);//mesh.triangles = mesh_triangles;
                    }//loop through submeshes
                }
            }//isTessellate
        }

        //Step 1.3: find the nearest point using data gathered and hidden in the negative range of goodArray

        //Step 1.4: if any points were found tessellate the fuckers by 
        //looping through the triangles, 
        //checking if any points are good
        //rearrange points so that the best point is first,
        //keep track so you don't re-tessellate a tessellated tri
        //
        //actual tessellation should happen here, based on the current list of goodPoints

        //if (!tessellationTimeOut)
        //    if (Time.realtimeSinceStartup - tessellationStartTime > maxTessellationTime)//.0025 =>2.5 milliseconds
        //        tessellationTimeOut = true;
        if (!tessellationTimeOut)
        if (goodPointsFound && isTessellate && isAdaptiveTessellate && BLINDED_AM_ME.MeshCut.isDoneInitializing && isDisplaceBurn && foundUndisplacedPoints )
        {
            List<int> goodForTessIndexList = new List<int>();//make a list of the good points so you don't concern yourself with the others
            for (int j = 0; j < mesh_vertices.Length; j++)
            {
                if (goodArray[j] > 0)
                    goodForTessIndexList.Add(j);
            }

            int currentGoodArrayLength = goodArray.Length;
            //for (int goodIndex=0; goodIndex<currentGoodArrayLength; goodIndex++)
            int goodIndex = 0;
            for (int k = 0; k<goodForTessIndexList.Count; k++)
            {//loop through goodPoints - this is an array the size of mesh_vertices that contains floats determining the closeness within range to the hit point
                goodIndex = goodForTessIndexList[k];
                if (goodArray[goodIndex] > 0)//for every selected point find adjacent triangles
                {
                    //for very new good vert found why re-cache all of the triangles in every sub?
                    List<int> trianglesAlreadyTessellated = new List<int>();
                    List<int[]> subTriangles = new List<int[]>();
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        subTriangles.Add(mesh.GetTriangles(sub));//mesh.GetTriangles(sub)

                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {//Loop through submeshes
                        int[] mesh_triangles = subTriangles[sub];// mesh.GetTriangles(sub);//mesh.triangles;

                        int numTriangles = mesh_triangles.Length;//arrays optimization attempt

                        for (int i=0; i< mesh_triangles.Length; i+=3)
                        {//loop through triangles to find the ones that include the point
                            int triIndex = i/3;
                            int iA = mesh_triangles[i];
                            int iB = mesh_triangles[i + 1];
                            int iC = mesh_triangles[i + 2];
                            if (iA == goodIndex || iB == goodIndex || iC == goodIndex){
                                //reorder points to follow a consistent algorithm
                                if (iB == goodIndex)
                                {//remap bca to abc
                                    iB = iC;
                                    iC = iA;
                                    iA = goodIndex;
                                } 
                                else if (iC == goodIndex)
                                {//remap cab to abc 
                                    iC = iB;
                                    iB = iA;
                                    iA = goodIndex;
                                }
                                int[] triangle = new int[] {iA, iB, iC};
                                //getGoodArray uses .01 as a sqrMagnitude range which I think means .1m is the distance
                                //if 10cm is the heat range, I want my displacement range to be 5cm try 3cm as a tessellation range
                                //3cm or .03 or .0009 if it's sqr
                                float range = burnRange * invertedScale;//remove .66f// .05f; //range used to be .05 where burnRange was .075, so multiply by .66
                                if (!trianglesAlreadyTessellated.Contains(triIndex) && !tessellationTimeOut)//maybe add a time check here that stops tessellating if a frame is taking too long.
                                {
                                    int numVerts = mesh_vertices.Length;//arrays optimization attempt by tracking these array lengths so that internally we can make an oversized array and not worry about resizing during every tessellation

                                    Tessellate( //calling before updating mesh_normals? or is it in the midst of a cut? don't think so because I'm setting isTessellate to false... or I should be...
                                                triangle, //int[3]
                                                triIndex, //int
                                                sub, //int
                                                range, //float
                                                ref mesh_vertices,
                                                ref mesh_normals,
                                                ref mesh_uv,
                                                ref mesh_uv2,
                                                ref mesh_tangents,
                                                ref mesh_triangles,
                                                ref tessDictionary, //new verts
                                                ref goodArray, //float[]
                                                0,//recursion depth
                                                ref numVerts,
                                                ref numTriangles
                                                );
                                    trianglesAlreadyTessellated.Add(triIndex);
                                }
                                    //for convenience reorder the tri points a,b,c where a is the "good" point
                                    //3 possible outcomes
                                        //split both edges ab and ac (2 new verts d & e) - new tris ade dbe bce
                                        //split first edge ab (new vert d) - new tris adc and dbc
                                        //split second edge ac (new vert d) - new tris abd bcd
                                    //calculate new vert position and check to see if it is in the dict else add to mesh_vertices, and add to dict
                            }
                        }//loop through tris
                        mesh.vertices = mesh_vertices;
                        mesh.normals = mesh_normals;
                        mesh.uv = mesh_uv;
                        mesh.uv2 = mesh_uv2;
                        mesh.tangents = mesh_tangents;
                        mesh.SetTriangles(mesh_triangles,sub);//mesh.triangles = mesh_triangles;
                        //mesh.Optimize();//made things get really wonky
                    }//loop through submeshes
                }//is a good point
            }//loop through good points (goodArray)
        //goodPointsFound = GetGoodArray(hitPoint, .01f, out goodArray);//refresh them at this point because wtf?//not necessary because tessellate handles it?
        }

        if (goodPointsFound)//need to check again after tessellation
        {
            //Debug.Log("RunningAddHeatToUV2Job");
            heatVertMap.Clear();// refresh after tessellation
            for (int g = 0; g < mesh_vertices.Length; g++)
            {
                if (goodArray[g] > 0)
                    heatVertMap.Add(g);
            }
            NativeArray<int> nativeHeatVertMapArray = new NativeArray<int>(heatVertMap.Count, Allocator.TempJob);
            nativeHeatVertMapArray.CopyFrom(heatVertMap.ToArray());

            NativeArray<float> nativeGoodArray = new NativeArray<float>(mesh_vertices.Length, Allocator.TempJob);
            if (goodArray.Length != mesh_vertices.Length)
                System.Array.Resize(ref goodArray, mesh_vertices.Length);

            nativeGoodArray.CopyFrom(goodArray);
            NativeArray<Vector2> nativeMeshUV2Array = new NativeArray<Vector2>(mesh_vertices.Length, Allocator.TempJob);
            nativeMeshUV2Array.CopyFrom(mesh_uv2);//read/write
            //adding displacement code
            NativeArray<Vector3> nativeMeshVertices = new NativeArray<Vector3>(mesh_vertices.Length, Allocator.TempJob);
            nativeMeshVertices.CopyFrom(mesh_vertices);
            //NativeArray<Vector3> nativeMeshNormals = new NativeArray<Vector3>(mesh_vertices.Length, Allocator.TempJob);
            //nativeMeshNormals.CopyFrom(mesh_normals);
            //make a job that adds heat to the uv in the array
            AddHeatToUV2Job job = new AddHeatToUV2Job()
            {
                MeshUV2Array = nativeMeshUV2Array,
                GoodArray = nativeGoodArray,//read only
                HeatVertMap = nativeHeatVertMapArray,
                DontDisplace = dontDisplace,//if dontDisplace==1 add the displace signal to the uv2 here
                MinHeatForDisplacement = minHeatForDisplacement,
                HeatMultiplier = heatBurnMultiplier
                //Displace = displaceInt
            };
            //JobHandle jobHandle = job.Schedule(mesh.vertexCount, 4);
            JobHandle jobHandle = job.Schedule(heatVertMap.Count, 4);
            jobHandle.Complete();

            //before running the displacement job you need to create a displacement normals array which tells each point which direction to displace
            //points on the border of a cut should not get displaced so can get assigned 0,0,0 as the normal or create another array of bool or int
            //to mask it out
            //in order to do both you need to create a hash of lists of points that share the same location
            //loop through them to get the normals, very similar to smoothing the normals

            //phase 1: create a hash of all of the points...
            bool needsDisplacement = false;
            Vector3 burnNormal = _transform.InverseTransformDirection(hit.normal);
            if (useDisplacement && isDisplaceBurn && isDisplaceable && isTessellate && dontDisplace!=1)//commented out isDisplaceable to burn, but this was a mistake
            {
                //Debug.Log("RunningDispJob");
                List<VertexEntry> entry;
                VertexKey key;
                Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>() ;//for each vertexKey make a list of normals
                // dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);
                //Vector3[] displacementNormals = mesh_normals;
                Vector3[] _meshVertices = mesh.vertices;
                for (int i=0; i<_meshVertices.Length; i++)
                {//add each vert, ignore triIndex
                    if (goodArray[i]>0)//if it's a good vert, add it to the hash
                        {
                            if (mesh_uv2[i].y < 1 /*= displacementLimit*/)
                            {
                                needsDisplacement = true;
                            }
                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry); //key and the index point
                            }
                            entry.Add(new VertexEntry(0, 0, i));//0 submesh and triIndex because we don't care at the moment
                        }
                }

                HeatDisplacementJob jobDisp = new HeatDisplacementJob()
                {
                    MeshUV2Array = nativeMeshUV2Array,
                    GoodArray = nativeGoodArray,//read only
                    Vertices = nativeMeshVertices,
                    //Normals = nativeMeshNormals,
                    AltNormal = burnNormal,// _transform.InverseTransformDirection(hit.normal),
                    DispScale = invertedScale,
                    //DispLimit = displacementLimit,
                    DontDisplace = dontDisplace,
                    HeatVertMap = nativeHeatVertMapArray
                };
                //JobHandle jobDispHandle = jobDisp.Schedule(mesh.vertexCount, 4);
                JobHandle jobDispHandle = jobDisp.Schedule(heatVertMap.Count, 4);
                jobDispHandle.Complete();

                if (isDisplaceable)
                    nativeMeshVertices.CopyTo(mesh_vertices);//got displaced
                
                //nativeMeshNormals.CopyTo(mesh_normals);//these are ReadOnly, don't need to copy


                //after displacement, loop through just the tris that include a point from the goodArray
                //then recalculate the normals based on the face
                //all of the new points should get cross(b-a,c-a).normalized
                //List<float>GoodList = new List(goodArray);
                //repurpose this code to only look at the verts that share the same space and then average the normals and/or identify non-displaceable points
                if (isDisplaceable)
                {
                    mesh.vertices = mesh_vertices;
                    mesh.normals = mesh_normals;
                }
                mesh.uv2 = mesh_uv2;
                //}//needs displacement

            }
            nativeMeshUV2Array.CopyTo(mesh_uv2);//got heated up in a previous block so it gets copied outside the block
            nativeHeatVertMapArray.Dispose();

            nativeGoodArray.Dispose();
            nativeMeshUV2Array.Dispose();
            nativeMeshVertices.Dispose();
            //nativeMeshNormals.Dispose();

            if (useDisplacement && isDisplaceBurn && isDisplaceable && isTessellate && needsDisplacement) //smooth normals - seems expensive
            {
                float angle = 90;
                float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

                Vector3[] _meshVertices = mesh.vertices;
                Vector3[][] triNormals = new Vector3[mesh.subMeshCount][];
                Vector3[] newNormals = mesh.normals;
                Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>() ;//for each vertexKey make a list of normals
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++) 
                {
                    int[] triangles = mesh.GetTriangles(subMeshIndex);
                    triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

                    for (int i=0; i<triangles.Length; i+=3)
                    {//accumulate face Normals
                        int i1 = triangles[i];
                        int i2 = triangles[i + 1];
                        int i3 = triangles[i + 2];
                        //Vector3 displaceVec = burnNormal * -1 * mesh_uv2[i].y;

                        //if (goodArray[i1]>0 || goodArray[i2]>0 || goodArray[i3]>0)
                        {
                        // Calculate the normal of the triangle
                            Vector3 p1;
                            Vector3 p2;
                            if (dontDisplace==1 && mesh_uv2[i1].y > 0 && mesh_uv2[i1].y < 1
                                                && mesh_uv2[i2].y > 0 && mesh_uv2[i2].y < 1
                                                && mesh_uv2[i3].y > 0 && mesh_uv2[i3].y < 1
                                )
                            {//if dontDisplace the new face normal will get calculated based on the displaced vertex positions
                                p1 = (_meshVertices[i2] + burnNormal * -1 * mesh_uv2[i2].y * .01f * invertedScale) - (_meshVertices[i1] + burnNormal * -1 * mesh_uv2[i1].y * .01f * invertedScale);
                                p2 = (_meshVertices[i3] + burnNormal * -1 * mesh_uv2[i3].y * .01f * invertedScale) - (_meshVertices[i1] + burnNormal * -1 * mesh_uv2[i1].y * .01f * invertedScale);
                            }
                            else
                            {
                                p1 = _meshVertices[i2] - _meshVertices[i1];
                                p2 = _meshVertices[i3] - _meshVertices[i1];
                            }
                            Vector3 faceNormal = Vector3.Cross(p1, p2).normalized;//pretty sure this needs to be normalized, and that fastNormalize is wonky
                            //faceNormal = FastNormalize(ref faceNormal);//I think this caused a weird flashy artifact on already-cooled displaced geo
                            int triIndex = i / 3;
                            triNormals[subMeshIndex][triIndex] = faceNormal;//index was out of bounds of the array
                            newNormals[i1] = faceNormal;
                            newNormals[i2] = faceNormal;
                            newNormals[i3] = faceNormal;

                            List<VertexEntry> entry;
                            VertexKey key;

                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i1]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry);
                            }
                            entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i2]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry);
                            }
                            entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i3]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry);
                            }
                            entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                        }
                    }
                }//submesh

                foreach (var vertList in dictionary.Values) 
                {
                    for (var i = 0; i < vertList.Count; ++i) 
                    {
         
                        var sum = new Vector3();
                        var lhsEntry = vertList[i];
                        int normalsCount = 0;

                        if (goodArray[lhsEntry.VertexIndex]>0)//only reacalculating in the range of the burn, optimization 8/30/2019
                        {
                            for (var j = 0; j < vertList.Count; ++j) 
                            {
                                var rhsEntry = vertList[j];
             
                                if (lhsEntry.VertexIndex == rhsEntry.VertexIndex) 
                                {
                                    Vector3AddEquals(ref sum,triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                    normalsCount++;
                                    //sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                                } 
                                else 
                                {
                                    // The dot product is the cosine of the angle between the two triangles.
                                    // A larger cosine means a smaller angle.
                                    var dot = Vector3.Dot(
                                    triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                                    triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                    if (dot >= cosineThreshold) 
                                    {
                                        Vector3AddEquals(ref sum,triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                        normalsCount++;
                                        //sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                                    }
                                }
                            }
                            if (mesh_uv2[lhsEntry.VertexIndex].y < 1 && mesh_uv2[lhsEntry.VertexIndex].y > 0){//dks uncommented 8/30/2019
                                //sum /= normalsCount;//trying to get fastNormalize to work - not helping
                                //sum = FastNormalize(ref sum);//doesn't displace, fast normalize must not work well with over inflated vectors
                                mesh_normals[lhsEntry.VertexIndex] = sum.normalized;//newNormals[lhsEntry.VertexIndex];//
                                //mesh_normals[lhsEntry.VertexIndex] = new Vector3(sum.x/normalsCount, sum.y/normalsCount, sum.z/normalsCount);//doesn't displace when I use this line
                            }
                        }
                    }
                }

            }//if useDisplacement - responsible for flashy bloom

        }//if getgoodarray
        else
        {
            if (isDebug) Debug.Log("no good points found in range so no uvs or nothing was done");
        }
        mesh.vertices = mesh_vertices;
        mesh.normals = mesh_normals;
        mesh.uv2 = mesh_uv2;
        isHots = true;
        mesh.RecalculateBounds();
        //mesh.Optimize();//in the future try to only execute this if there was tessellation
    
    }

    void BurnNice(RaycastHit[] hits, bool isDisplaceBurn)
    {
        float currentTime = Time.time;
        if (currentTime - previousBurnTime < burnInterval) { return; }
        skipUpdate = true;
        previousBurnTime = currentTime;
        //IBurnable interface
        //make a burn mark at the hit location;
        RaycastHit hit = hits[0];
        Mesh mesh = localMesh;// mf.sharedMesh;
        List<Vector3> mesh_vertices = new List<Vector3>();//mesh.vertices; //disp
        mesh_vertices.AddRange(mesh.vertices);
        List<Vector3> mesh_normals = new List<Vector3>();//mesh.normals;//disp
        mesh_normals.AddRange(mesh.normals);
        List<Vector2> mesh_uv = new List<Vector2>();//mesh.uv;//tessellation
        mesh_uv.AddRange(mesh.uv);
        List<Vector2> mesh_uv2 = new List<Vector2>();//mesh.uv2;//heat
        mesh_uv2.AddRange(mesh.uv2);
        List<Vector4> mesh_tangents = new List<Vector4>();//mesh.tangents;//tessellation
        mesh_tangents.AddRange(mesh.tangents);
        float[] goodArray = new float[mesh.vertices.Length];//finds points in a mesh that are within range of the hitPoint
        bool goodPointsFound = false;// GetGoodArray(hitPoint, .005f, ref goodArray, out isDisplaceable);
        Vector3[] hitPoints = new Vector3[hits.Length];
        for (int j=0; j<hits.Length; j++)
        {
            hitPoints[j] = hits[j].point;
        }
        bool isDisplaceable=false;
        List<float> goodList = new List<float>();
        goodPointsFound = GetGoodArray(hitPoints, burnRange, ref goodArray, out isDisplaceable);
        goodList.AddRange(goodArray);
        //Step 1: look for points near burn

        for (int j=0; j<hits.Length; j++)
        {
            hitPoints[j] = hits[j].point;
        }

        bool foundUndisplacedPoints = false;

        for (int j = 0; j < goodList.Count; j++)
        {
            if (goodArray[j] > 0 && mesh_uv2[j].y == 0)
            {
                foundUndisplacedPoints = true;
                if (isDebug) Debug.Log("undisplaced points found" + mesh_uv2[j].y.ToString());
                break;
            }
        }

        if (!foundUndisplacedPoints && isDebug) Debug.Log("no undisplaced points found!");

        /*
                //visualizing points found
                if (goodPointsFound){
                    for (int i=0; i<goodArray.Length; i++){
                        if (goodArray[i]>0){
                            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cube.transform.position = transform.TransformPoint(mesh_vertices[i]);
                            cube.transform.localScale = Vector3.one * .01f;
                            cube.transform.rotation = Quaternion.LookRotation(mesh_normals[i]);
                            cube.transform.parent = _transform;
                        }
                    }
                }
        */

        //Step 1.2: if no points found Intersect Tri - wonky experiment
        //if it's pointy, split it down the long axis, otherwise project a point from the hit

        Dictionary<VertexKey, VertexEntry> tessDictionary = new Dictionary<VertexKey, VertexEntry>() ;//for each vertexKey make a list of normals
        float tessellationStartTime = Time.realtimeSinceStartup;
        bool tessellationTimeOut = false;
        if (!goodPointsFound)
        {//find the nearest point and 
            if (isDebug)
                Debug.Log("no points found in range of the burn. subMeshCount: " + mesh.subMeshCount.ToString());

            if (isTessellate && isAdaptiveTessellate && BLINDED_AM_ME.MeshCut.isDoneInitializing && isDisplaceBurn)
            {
                Vector3 point = _transform.InverseTransformPoint(hit.point);

                //get the nearest point that is facing in the same direction as the hit point-ish
                if (isDebug) Debug.Log("Getting the nearest out-of-range point");
                //int minIndex = -1;
                float minValue = 10000;//start with a hypothetical very large number
                for (int i=0; i<mesh_vertices.Count; i++){
                    if (goodList[i] < 0){//real distance is hidden in negative range
                        if (-goodList[i]<minValue && Vector3.Dot(mesh_normals[i],hit.normal) > -.25){//.5
                            minValue = -goodList[i];
                            //minIndex = i;
                        }
                    }
                }
                int[] minIndices = new int[20];//no more than 10
                int indexCount = 0;
                for (int i=0; i<mesh_vertices.Count; i++){
                    if (goodList[i] < 0){//real sqrdistance is hidden in negative range
                        if (-goodList[i] == minValue){
                            if (indexCount < 20)
                                minIndices[indexCount++]=i;
                            else
                            break;
                        }
                    }
                }
                //Debug.Log("nearest points found: " + indexCount.ToString());
                goodPointsFound = true;
                for (int j=0; j<indexCount; j++){
                    int minIndex = minIndices[j];
                    goodList[minIndex]=.01f;
                    goodPointsFound = true;
                    List<int> trianglesAlreadyTessellated = new List<int>();
                    bool triFound = false;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {//Loop through submeshes
                        List<int> mesh_triangles = new List<int>();
                        mesh_triangles.AddRange(mesh.GetTriangles(sub));// mesh.GetTriangles(sub);//mesh.triangles;
                        for (int i=0; i< mesh_triangles.Count; i+=3)
                        {//loop through triangles to find the ones that include the point
                            int triIndex = i/3;
                            int iA = mesh_triangles[i];
                            int iB = mesh_triangles[i + 1];
                            int iC = mesh_triangles[i + 2];
                            if (iA == minIndex || iB == minIndex || iC == minIndex){//if you've found an adjacent triangle
                                //check to see if the hitPoint is inside it
                                Vector3 pointA = mesh_vertices[iA];
                                Vector3 pointB = mesh_vertices[iB];
                                Vector3 pointC = mesh_vertices[iC];
                                Plane triPlane = new Plane(pointA, pointB, pointC);
                                float distanceToPoint = triPlane.GetDistanceToPoint(point);//local space point
                                if (distanceToPoint < .001 && distanceToPoint > -.001){
                                    Vector3 newPoint = point + (-triPlane.normal * distanceToPoint);//projecting point onto the tri's plane
                                    if (IsInside(pointA,pointB,pointC,newPoint))
                                    {
                                        int[] triangle = new int[] {iA, iB, iC};
                                        float range = .05f;//.075f;//.05//.03f;//.01
                                        if (!trianglesAlreadyTessellated.Contains(triIndex))
                                        {
                                            SplitTriangleAtPoint2(
                                                    triangle, //int[3]
                                                    triIndex, //int
                                                    sub, //int
                                                    range, //float
                                                    ref mesh_vertices,
                                                    ref mesh_normals,
                                                    ref mesh_uv,
                                                    ref mesh_uv2,
                                                    ref mesh_tangents,
                                                    ref mesh_triangles,
                                                    ref tessDictionary, //new verts
                                                    ref goodList, //float[]
                                                    0,//recursion depth
                                                    newPoint
                                                    );
                                            trianglesAlreadyTessellated.Add(triIndex);
                                            break;
                                        }else{
                                            if (isDebug)
                                                Debug.Log("Triangle already tessellated");
                                        }
                                    }
                                }//distance to point
                                else
                                {
                                    if (isDebug)
                                        Debug.Log("tessellation skipped because the triangle seems too far from the hit point");
                                }
                            }
                            if (triFound)
                                break;
                        }//loop through tris
                        mesh.vertices = mesh_vertices.ToArray();
                        mesh.normals = mesh_normals.ToArray();
                        mesh.uv = mesh_uv.ToArray();
                        mesh.uv2 = mesh_uv2.ToArray();
                        mesh.tangents = mesh_tangents.ToArray();
                        mesh.SetTriangles(mesh_triangles.ToArray(),sub);//mesh.triangles = mesh_triangles;
                    }//loop through submeshes
                }
            }//isTessellate


            for (int j = 0; j < goodList.Count; j++)//check again to see if points need to be displaced
            {
                if (goodArray[j] > 0 && mesh_uv2[j].y == 0)
                {
                    foundUndisplacedPoints = true;
                    if (isDebug) Debug.Log("undisplaced points found" + mesh_uv2[j].y.ToString());
                    break;
                }
            }
        } 
        else 
        {
            if (isDebug) Debug.Log("good points found on the first try!");
        }

        //Step 1.3: find the nearest point using data gathered and hidden in the negative range of goodArray

        //Step 1.4: if any points were found tessellate the fuckers by 
        //looping through the triangles, 
        //checking if any points are good
        //rearrange points so that the best point is first,
        //keep track so you don't re-tessellate a tessellated tri
        //
        //actual tessellation should happen here, based on the current list of goodPoints

        //if (!tessellationTimeOut)
        //    if (Time.realtimeSinceStartup - tessellationStartTime > maxTessellationTime)//.0025 =>2.5 milliseconds
        //        tessellationTimeOut = true;
        if (!tessellationTimeOut)
        if (goodPointsFound && isTessellate && isAdaptiveTessellate && BLINDED_AM_ME.MeshCut.isDoneInitializing && isDisplaceBurn && foundUndisplacedPoints)
        {
            List<int> goodForTessIndexList = new List<int>();//make a list of the good points so you don't concern yourself with the others
            for (int j = 0; j < mesh_vertices.Count; j++)
            {
                if (goodList[j] > 0)
                    goodForTessIndexList.Add(j);
            }

            int currentGoodArrayLength = goodList.Count;
            //for (int goodIndex=0; goodIndex<currentGoodArrayLength; goodIndex++)
            if (isDebug) Debug.Log("going into the tessellation loop");
            int goodIndex = 0;
            for (int k = 0; k<goodForTessIndexList.Count; k++)
            {//loop through goodPoints - this is an array the size of mesh_vertices that contains floats determining the closeness within range to the hit point
                goodIndex = goodForTessIndexList[k];
                if (goodList[goodIndex] > 0)//for every selected point find adjacent triangles
                {
                    //for very new good vert found why re-cache all of the triangles in every sub?
                    List<int> trianglesAlreadyTessellated = new List<int>();
                    List<int[]> subTriangles = new List<int[]>();
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        subTriangles.Add(mesh.GetTriangles(sub));//mesh.GetTriangles(sub)

                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {//Loop through submeshes
                        List<int> mesh_triangles = new List<int>();//
                        mesh_triangles.AddRange(subTriangles[sub]);// mesh.GetTriangles(sub);//mesh.triangles;
                        /*for (int j=0; j<mesh_triangles.Length; j+=3)
                        {
                            if (goodArray[mesh_triangles[j]]>0){
                                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                sphere.transform.position = transform.TransformPoint(mesh_vertices[mesh_triangles[j]]);
                                sphere.transform.localScale = Vector3.one * .02f;
                                sphere.transform.parent = _transform;
                                sphere.name = ("sphere"+mesh_triangles[j].ToString()+"tri"+(j/3).ToString()+"sub"+sub.ToString());

                                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                sphere.transform.position = transform.TransformPoint(mesh_vertices[mesh_triangles[j+1]]);
                                sphere.transform.localScale = Vector3.one * .02f;
                                sphere.transform.parent = _transform;
                                sphere.name = ("sphere"+mesh_triangles[j+1].ToString()+"tri"+(j/3).ToString()+"sub"+sub.ToString());

                                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                sphere.transform.position = transform.TransformPoint(mesh_vertices[mesh_triangles[j+2]]);
                                sphere.transform.localScale = Vector3.one * .02f;
                                sphere.transform.parent = _transform;
                                sphere.name = ("sphere"+mesh_triangles[j+2].ToString()+"tri"+(j/3).ToString()+"sub"+sub.ToString());
                            }
                        }
                        GameObject nullObject = new GameObject();
                        nullObject.transform.parent = _transform;
                        nullObject.name = "-----------";
                        */
                        for (int i=0; i< mesh_triangles.Count; i+=3)
                        {//loop through triangles to find the ones that include the point
                            int triIndex = i/3;
                            int iA = mesh_triangles[i];
                            int iB = mesh_triangles[i + 1];
                            int iC = mesh_triangles[i + 2];
                            if (iA == goodIndex || iB == goodIndex || iC == goodIndex){
                                //reorder points to follow a consistent algorithm
                                if (iB == goodIndex)
                                {//remap bca to abc
                                    iB = iC;
                                    iC = iA;
                                    iA = goodIndex;
                                } 
                                else if (iC == goodIndex)
                                {//remap cab to abc 
                                    iC = iB;
                                    iB = iA;
                                    iA = goodIndex;
                                }
                                int[] triangle = new int[] {iA, iB, iC};
                                //getGoodArray uses .01 as a sqrMagnitude range which I think means .1m is the distance
                                //if 10cm is the heat range, I want my displacement range to be 5cm try 3cm as a tessellation range
                                //3cm or .03 or .0009 if it's sqr
                                float range = burnRange * invertedScale;//remove .66f// .05f; //range used to be .05 where burnRange was .075, so multiply by .66
                                if (!trianglesAlreadyTessellated.Contains(triIndex) && !tessellationTimeOut)//maybe add a time check here that stops tessellating if a frame is taking too long.
                                {
                                    Tessellate2( //calling before updating mesh_normals? or is it in the midst of a cut? don't think so because I'm setting isTessellate to false... or I should be...
                                                triangle, //int[3]
                                                triIndex, //int
                                                sub, //int
                                                range, //float
                                                ref mesh_vertices,
                                                ref mesh_normals,
                                                ref mesh_uv,
                                                ref mesh_uv2,
                                                ref mesh_tangents,
                                                ref mesh_triangles,
                                                ref tessDictionary, //new verts
                                                ref goodList, //float[]
                                                0//recursion depth
                                                );
                                    trianglesAlreadyTessellated.Add(triIndex);
                                }
                                    //for convenience reorder the tri points a,b,c where a is the "good" point
                                    //3 possible outcomes
                                        //split both edges ab and ac (2 new verts d & e) - new tris ade dbe bce
                                        //split first edge ab (new vert d) - new tris adc and dbc
                                        //split second edge ac (new vert d) - new tris abd bcd
                                    //calculate new vert position and check to see if it is in the dict else add to mesh_vertices, and add to dict
                            }
                        }//loop through tris
                        mesh.vertices = mesh_vertices.ToArray();
                        mesh.normals = mesh_normals.ToArray();
                        mesh.uv = mesh_uv.ToArray();
                        mesh.uv2 = mesh_uv2.ToArray();
                        mesh.tangents = mesh_tangents.ToArray();
                        mesh.SetTriangles(mesh_triangles.ToArray(),sub);//mesh.triangles = mesh_triangles;
                        //mesh.Optimize();//made things get really wonky
                    }//loop through submeshes
                }//is a good point
            }//loop through good points (goodArray)
        //goodPointsFound = GetGoodArray(hitPoint, .01f, out goodArray);//refresh them at this point because wtf?//not necessary because tessellate handles it?
        }

        if (goodPointsFound)//need to check again after tessellation
        {
            //Debug.Log("RunningAddHeatToUV2Job");
            heatVertMap.Clear();// refresh after tessellation
            for (int g = 0; g < mesh_vertices.Count; g++)
            {
                if (goodList[g] > 0)
                    heatVertMap.Add(g);
            }
            NativeArray<int> nativeHeatVertMapArray = new NativeArray<int>(heatVertMap.Count, Allocator.TempJob);
            nativeHeatVertMapArray.CopyFrom(heatVertMap.ToArray());

            NativeArray<float> nativeGoodArray = new NativeArray<float>(goodList.Count, Allocator.TempJob);
            //if (goodArray.Length != mesh_vertices.Length)
            //{//why was I resizing this array?
            //    Debug.Log("**********************************************************************");
            //    goodArray = new float[mesh_vertices.Length];//System.Array.Resize(ref goodArray, mesh_vertices.Length);
            //}

            nativeGoodArray.CopyFrom(goodList.ToArray());
            NativeArray<Vector2> nativeMeshUV2Array = new NativeArray<Vector2>(mesh_uv2.Count, Allocator.TempJob);
            nativeMeshUV2Array.CopyFrom(mesh_uv2.ToArray());//read/write
            //adding displacement code
            NativeArray<Vector3> nativeMeshVertices = new NativeArray<Vector3>(mesh_vertices.Count, Allocator.TempJob);
            nativeMeshVertices.CopyFrom(mesh_vertices.ToArray());
            //NativeArray<Vector3> nativeMeshNormals = new NativeArray<Vector3>(mesh_vertices.Length, Allocator.TempJob);
            //nativeMeshNormals.CopyFrom(mesh_normals);
            //make a job that adds heat to the uv in the array
            AddHeatToUV2Job job = new AddHeatToUV2Job()
            {
                MeshUV2Array = nativeMeshUV2Array,
                GoodArray = nativeGoodArray,//read only
                HeatVertMap = nativeHeatVertMapArray,
                DontDisplace = dontDisplace,//if dontDisplace==1 add the displace signal to the uv2 here
                MinHeatForDisplacement = minHeatForDisplacement,
                HeatMultiplier = heatBurnMultiplier
                //Displace = displaceInt
            };
            //JobHandle jobHandle = job.Schedule(mesh.vertexCount, 4);
            JobHandle jobHandle = job.Schedule(heatVertMap.Count, 4);
            jobHandle.Complete();

            //before running the displacement job you need to create a displacement normals array which tells each point which direction to displace
            //points on the border of a cut should not get displaced so can get assigned 0,0,0 as the normal or create another array of bool or int
            //to mask it out
            //in order to do both you need to create a hash of lists of points that share the same location
            //loop through them to get the normals, very similar to smoothing the normals

            //phase 1: create a hash of all of the points...
            bool needsDisplacement = false;
            Vector3 burnNormal = _transform.InverseTransformDirection(hit.normal);
            if (useDisplacement && isDisplaceBurn && isDisplaceable && isTessellate && dontDisplace!=1)//commented out isDisplaceable to burn, but this was a mistake
            {
                //Debug.Log("RunningDispJob");
                List<VertexEntry> entry;
                VertexKey key;
                Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>() ;//for each vertexKey make a list of normals
                // dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);
                //Vector3[] displacementNormals = mesh_normals;
                Vector3[] _meshVertices = mesh.vertices;
                for (int i=0; i<_meshVertices.Length; i++)
                {//add each vert, ignore triIndex
                    if (goodList[i]>0)//if it's a good vert, add it to the hash
                        {
                            if (mesh_uv2[i].y < 1 /*= displacementLimit*/)
                            {
                                needsDisplacement = true;
                            }
                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry); //key and the index point
                            }
                            entry.Add(new VertexEntry(0, 0, i));//0 submesh and triIndex because we don't care at the moment
                        }
                }

                //if (needsDisplacement){
                //probably don't need the displacement normals anymore since we like altnormal
                /*
                    foreach (var vertList in dictionary.Values) 
                    {

                        //loop through every list of identical vertices and come up with a common normal
                        Vector3 commonNormal = new Vector3();
                        bool foundCut = false;
                        int normalsCount = 0;
                        for (var i = 0; i < vertList.Count; i++)
                        {//add up all of the normals in the list
                            if (mesh_uv2[vertList[i].VertexIndex].y == 1)
                            { //if the vert is part of a cut it shouldn't displace
                            //trying mesh.uv2 so we are looking at the state before the current burn
                                foundCut = true;
                                break;
                            }
                            //commonNormal += mesh_normals[vertList[i].VertexIndex];
                            Vector3AddEquals(ref commonNormal,mesh_normals[vertList[i].VertexIndex]);
                            normalsCount++;
                        }
                        //commonNormal.Normalize();//dks 8/19/2019 
                        commonNormal /= (float)normalsCount;//average should be faster but less accurate than normalize
                        //commonNormal = FastNormalize(ref commonNormal);//something is making displacements look terrible, maybe this line
                        for (var j = 0; j < vertList.Count; j++)
                        {//comparing each point in the list to the rest of the list
                            if (foundCut)
                                displacementNormals[vertList[j].VertexIndex] = Vector3.zero;
                            else
                                displacementNormals[vertList[j].VertexIndex] = commonNormal;
                        }

                    }
                    nativeMeshNormals.CopyFrom(displacementNormals);//displacement normals are a sort of average of all of the common verts
                    */

                    //nativeMeshNormals.CopyFrom(mesh_normals);//trying mesh normals to see if I can reduce odd behavior
                    


                    HeatDisplacementJob jobDisp = new HeatDisplacementJob()
                    {
                        MeshUV2Array = nativeMeshUV2Array,
                        GoodArray = nativeGoodArray,//read only
                        Vertices = nativeMeshVertices,
                        //Normals = nativeMeshNormals,
                        AltNormal = burnNormal,// _transform.InverseTransformDirection(hit.normal),
                        DispScale = invertedScale,
                        //DispLimit = displacementLimit,
                        DontDisplace = dontDisplace,
                        HeatVertMap = nativeHeatVertMapArray
                    };
                    //JobHandle jobDispHandle = jobDisp.Schedule(mesh.vertexCount, 4);
                    JobHandle jobDispHandle = jobDisp.Schedule(heatVertMap.Count, 4);
                    jobDispHandle.Complete();

                    if (isDisplaceable)
                    {
                        //Vector3[] dispVerts = new Vector3[mesh_vertices.Count];
                        //nativeMeshVertices.CopyTo(dispVerts);//got displaced
                        mesh_vertices.Clear();
                        mesh_vertices.AddRange(nativeMeshVertices.ToArray()/*dispVerts*/);
                        //mesh_vertices.AddRange(dispVerts);
                        //nativeMeshVertices.CopyTo(mesh_vertices);//got displaced
                    }
                    
                    //nativeMeshNormals.CopyTo(mesh_normals);//these are ReadOnly, don't need to copy


                //after displacement, loop through just the tris that include a point from the goodArray
                //then recalculate the normals based on the face
                //all of the new points should get cross(b-a,c-a).normalized
                //List<float>GoodList = new List(goodArray);
                //repurpose this code to only look at the verts that share the same space and then average the normals and/or identify non-displaceable points
                if (isDisplaceable)
                {
                    mesh.vertices = mesh_vertices.ToArray();
                    mesh.normals = mesh_normals.ToArray();
                }
                mesh.uv2 = mesh_uv2.ToArray();
                //}//needs displacement

            }

            //nativeMeshUV2Array.CopyTo(mesh_uv2);//got heated up in a previous block so it gets copied outside the block
            mesh_uv2.Clear();
            //Vector2[] dispUV2 = new Vector2[mesh_vertices.Count]; 
            //nativeMeshUV2Array.CopyTo(dispUV2);
            mesh_uv2.AddRange(nativeMeshUV2Array.ToArray());
            //mesh_uv2.AddRange(nativeMeshUV2Array.ToArray());
            nativeMeshUV2Array.Dispose();
            
            nativeHeatVertMapArray.Dispose();

            nativeGoodArray.Dispose();
            nativeMeshVertices.Dispose();
            //nativeMeshNormals.Dispose();

            if (useDisplacement && isDisplaceBurn && isDisplaceable && isTessellate && needsDisplacement) //smooth normals - seems expensive
            {
                float angle = 90;
                float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

                Vector3[] _meshVertices = mesh.vertices;
                Vector3[][] triNormals = new Vector3[mesh.subMeshCount][];
                Vector3[] newNormals = mesh.normals;
                Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>() ;//for each vertexKey make a list of normals
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++) 
                {
                    int[] triangles = mesh.GetTriangles(subMeshIndex);
                    triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

                    for (int i=0; i<triangles.Length; i+=3)
                    {//accumulate face Normals
                        int i1 = triangles[i];
                        int i2 = triangles[i + 1];
                        int i3 = triangles[i + 2];
                        //Vector3 displaceVec = burnNormal * -1 * mesh_uv2[i].y;

                        //if (goodArray[i1]>0 || goodArray[i2]>0 || goodArray[i3]>0)
                        {
                        // Calculate the normal of the triangle
                            Vector3 p1;
                            Vector3 p2;
                            if (dontDisplace==1 && mesh_uv2[i1].y > 0 && mesh_uv2[i1].y < 1
                                                && mesh_uv2[i2].y > 0 && mesh_uv2[i2].y < 1
                                                && mesh_uv2[i3].y > 0 && mesh_uv2[i3].y < 1
                                )
                            {//if dontDisplace the new face normal will get calculated based on the displaced vertex positions
                                p1 = (_meshVertices[i2] + burnNormal * -1 * mesh_uv2[i2].y * .01f * invertedScale) - (_meshVertices[i1] + burnNormal * -1 * mesh_uv2[i1].y * .01f * invertedScale);
                                p2 = (_meshVertices[i3] + burnNormal * -1 * mesh_uv2[i3].y * .01f * invertedScale) - (_meshVertices[i1] + burnNormal * -1 * mesh_uv2[i1].y * .01f * invertedScale);
                            }
                            else
                            {
                                p1 = _meshVertices[i2] - _meshVertices[i1];
                                p2 = _meshVertices[i3] - _meshVertices[i1];
                            }
                            Vector3 faceNormal = Vector3.Cross(p1, p2).normalized;//pretty sure this needs to be normalized, and that fastNormalize is wonky
                            //faceNormal = FastNormalize(ref faceNormal);//I think this caused a weird flashy artifact on already-cooled displaced geo
                            int triIndex = i / 3;
                            triNormals[subMeshIndex][triIndex] = faceNormal;//index was out of bounds of the array
                            newNormals[i1] = faceNormal;
                            newNormals[i2] = faceNormal;
                            newNormals[i3] = faceNormal;

                            List<VertexEntry> entry;
                            VertexKey key;

                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i1]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry);
                            }
                            entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i2]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry);
                            }
                            entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                            if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i3]), out entry)) 
                            {
                                entry = new List<VertexEntry>();
                                dictionary.Add(key, entry);
                            }
                            entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                        }
                    }
                }//submesh

                foreach (var vertList in dictionary.Values) 
                {
                    for (var i = 0; i < vertList.Count; ++i) 
                    {
         
                        var sum = new Vector3();
                        var lhsEntry = vertList[i];
                        int normalsCount = 0;

                        if (goodList[lhsEntry.VertexIndex]>0)//only reacalculating in the range of the burn, optimization 8/30/2019
                        {
                            for (var j = 0; j < vertList.Count; ++j) 
                            {
                                var rhsEntry = vertList[j];
             
                                if (lhsEntry.VertexIndex == rhsEntry.VertexIndex) 
                                {
                                    Vector3AddEquals(ref sum,triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                    normalsCount++;
                                    //sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                                } 
                                else 
                                {
                                    // The dot product is the cosine of the angle between the two triangles.
                                    // A larger cosine means a smaller angle.
                                    var dot = Vector3.Dot(
                                    triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                                    triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                    if (dot >= cosineThreshold) 
                                    {
                                        Vector3AddEquals(ref sum,triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                        normalsCount++;
                                        //sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                                    }
                                }
                            }
                            if (mesh_uv2[lhsEntry.VertexIndex].y < 1 && mesh_uv2[lhsEntry.VertexIndex].y > 0){//dks uncommented 8/30/2019
                                //sum /= normalsCount;//trying to get fastNormalize to work - not helping
                                //sum = FastNormalize(ref sum);//doesn't displace, fast normalize must not work well with over inflated vectors
                                mesh_normals[lhsEntry.VertexIndex] = sum.normalized;//newNormals[lhsEntry.VertexIndex];//
                                //mesh_normals[lhsEntry.VertexIndex] = new Vector3(sum.x/normalsCount, sum.y/normalsCount, sum.z/normalsCount);//doesn't displace when I use this line
                            }
                        }
                    }
                }

            }//if useDisplacement - responsible for flashy bloom

        }//if getgoodarray
        mesh.vertices = mesh_vertices.ToArray();
        mesh.normals = mesh_normals.ToArray();
        mesh.uv2 = mesh_uv2.ToArray();
        isHots = true;
        mesh.RecalculateBounds();
        //mesh.Optimize();//in the future try to only execute this if there was tessellation
    
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


/*     //Union used by InvSqrt
     //[StructLayout(LayoutKind.Explicit)]
     struct FloatIntUnion
     {
         [FieldOffset(0)]
         public float x;
 
         [FieldOffset(0)]
         public int i;
     }
 
     FloatIntUnion union = new FloatIntUnion();
 
     //Fast inverse Sqrt
     float InvSqrt(float x)
     {
         union.x = x;
         union.i = 0x5f3759df - (union.i >> 1);
         x = union.x;
         x = x * (1.5f - 0.5f * x * x * x);
         return x;
     }
 
 
     //Normalize vector using fast inverse Sqrt
     Vector3 FastNormalized(Vector3 src)
     {
         float inversedMagnitude = InvSqrt(src.sqrMagnitude);
         return src * inversedMagnitude;
     }
     */



private float AreaOfTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        return Vector3.Cross(B-A,C-A).magnitude / 2f;
    }

    private bool IsInside(Vector3 A,Vector3 B,Vector3 C,Vector3 P)
    {
        //if area of ABC == PAB + PBC + PAC then P is inside ABC
        float ABC = AreaOfTriangle(A,B,C);
        float PAB = AreaOfTriangle(P,A,B);
        float PBC = AreaOfTriangle(P,B,C);
        float PAC = AreaOfTriangle(P,A,C);
        if (ABC - (PAB + PBC + PAC) < Mathf.Epsilon)
            return true;
        else
            return false;
    }

    private void SplitTriangleAtPoint(//tessellate towards a target point
        int[] triangle, 
        int triIndex, 
        int sub, 
        float range, 
        ref Vector3[] mesh_vertices,
        ref Vector3[] mesh_normals,
        ref Vector2[] mesh_uv,
        ref Vector2[] mesh_uv2,
        ref Vector4[] mesh_tangents,
        ref int[] mesh_triangles,
        ref Dictionary <VertexKey, VertexEntry> tessDictionary,
        ref float[] goodArray,
        int recursionDepth,
        Vector3 targetPosition, //determine the closest point, reorder points then TessellateToPoint() until in range
        ref int numVerts,
        ref int numTriangles
        )
    {
        int iA = triangle[0];
        int iB = triangle[1];
        int iC = triangle[2];
        int iD = mesh_vertices.Length;
        float sqrRange = range * range * invertedScale * invertedScale;
        Vector3 pointA = mesh_vertices[iA];
        Vector3 pointB = mesh_vertices[iB];
        Vector3 pointC = mesh_vertices[iC];
        float distA = (pointA - targetPosition).sqrMagnitude;
        float distB = (pointB - targetPosition).sqrMagnitude;
        float distC = (pointC - targetPosition).sqrMagnitude;

        float distSum = distA + distB + distC;
        float percA = distA / distSum;
        float percB = distB / distSum;
        float percC = distC / distSum;
        float weightA = 1f/distA;
        float weightB = 1f/distB;
        float weightC = 1f/distC;
        Vector2 uvWeightedAvg = (mesh_uv[iA]*weightA+mesh_uv[iB]*weightB+mesh_uv[iC]*weightC)/(weightA+weightB+weightC);
        Vector2 uv2WeightedAvg = (mesh_uv2[iA]*weightA+mesh_uv2[iB]*weightB+mesh_uv2[iC]*weightC)/(weightA+weightB+weightC);
        Vector3 pointWeightedAvg = (mesh_vertices[iA]*weightA+mesh_vertices[iB]*weightB+mesh_vertices[iC]*weightC)/(weightA+weightB+weightC);
        Vector3 newVertex = pointWeightedAvg;//Vector3.Project(point,localHitNormal);//local space
        Vector3 newNormal = (mesh_normals[iA]*weightA+mesh_normals[iB]*weightB+mesh_normals[iC]*weightC)/(weightA+weightB+weightC);
        Vector2 newUV = uvWeightedAvg;// mesh_uv[iA]*percA + mesh_uv[iB]*percB + mesh_uv[iC]*percC; // hit.textureCoord;//these don't work
        Vector2 newUV2 = uv2WeightedAvg;//mesh_uv2[iA]*percA + mesh_uv2[iB]*percB + mesh_uv2[iC]*percC; //hit.textureCoord2;//these don't work
        Vector3 newTangents = mesh_tangents[iA];
        int triLastIndex = mesh_triangles.Length;
        //add one new vertex and 2 new triangles
        System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+1);
        System.Array.Resize(ref mesh_normals, mesh_normals.Length+1);
        System.Array.Resize(ref mesh_uv, mesh_uv.Length+1);
        System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+1);
        System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+1);
        System.Array.Resize(ref mesh_triangles, mesh_triangles.Length+6);
        System.Array.Resize(ref goodArray, goodArray.Length+1);

        mesh_vertices[mesh_vertices.Length-1] = newVertex;
        mesh_normals[mesh_vertices.Length-1] = newNormal;
        mesh_uv[mesh_vertices.Length-1] = newUV;
        mesh_uv2[mesh_vertices.Length-1] = newUV2;
        mesh_tangents[mesh_vertices.Length-1] = newTangents;
        goodArray[mesh_vertices.Length-1] = 1f;
        mesh_triangles[triIndex*3]=iD;//abd
        mesh_triangles[triIndex*3+1]=iA;//abd
        mesh_triangles[triIndex*3+2]=iB;//abd

        mesh_triangles[triLastIndex]=iD;//bcd
        mesh_triangles[triLastIndex+1]=iB;//bcd
        mesh_triangles[triLastIndex+2]=iC;//bcd

        mesh_triangles[triLastIndex+3]=iD;//cad
        mesh_triangles[triLastIndex+4]=iC;//cad
        mesh_triangles[triLastIndex+5]=iA;//cad

        int[] triangle1 = new int[] {iD, iA, iB};
        int triIndex1 = triIndex;
        int[] triangle2 = new int[] {iD, iB, iC};
        int triIndex2 = triLastIndex/3;
        int[] triangle3 = new int[] {iD, iC, iA};
        int triIndex3 = triLastIndex/3 + 1;  

        Tessellate(
            triangle1, //int[3]
            triIndex1, //int
            sub, //int
            range, //float
            ref mesh_vertices,
            ref mesh_normals,
            ref mesh_uv,
            ref mesh_uv2,
            ref mesh_tangents,
            ref mesh_triangles,
            ref tessDictionary, //new verts
            ref goodArray, //float[]
            0, //-1 tells Tessellate() not to recurse because recursion needs to be controlled here
            ref numVerts,
            ref numTriangles
        );

        Tessellate(
            triangle2, //int[3]
            triIndex2, //int
            sub, //int
            range, //float
            ref mesh_vertices,
            ref mesh_normals,
            ref mesh_uv,
            ref mesh_uv2,
            ref mesh_tangents,
            ref mesh_triangles,
            ref tessDictionary, //new verts
            ref goodArray, //float[]
            0, //-1 tells Tessellate() not to recurse because recursion needs to be controlled here
            ref numVerts,
            ref numTriangles
        );

        Tessellate(
            triangle3, //int[3]
            triIndex3, //int
            sub, //int
            range, //float
            ref mesh_vertices,
            ref mesh_normals,
            ref mesh_uv,
            ref mesh_uv2,
            ref mesh_tangents,
            ref mesh_triangles,
            ref tessDictionary, //new verts
            ref goodArray, //float[]
            0, //-1 tells Tessellate() not to recurse because recursion needs to be controlled here
            ref numVerts,
            ref numTriangles
        );

    }

    private void SplitTriangleAtPoint2(//tessellate towards a target point
        int[] triangle, 
        int triIndex, 
        int sub, 
        float range, 
        ref List<Vector3> mesh_vertices,
        ref List<Vector3> mesh_normals,
        ref List<Vector2> mesh_uv,
        ref List<Vector2> mesh_uv2,
        ref List<Vector4> mesh_tangents,
        ref List<int> mesh_triangles,
        ref Dictionary <VertexKey, VertexEntry> tessDictionary,
        ref List<float> goodArray,
        int recursionDepth,
        Vector3 targetPosition //determine the closest point, reorder points then TessellateToPoint() until in range
        )
    {
        int iA = triangle[0];
        int iB = triangle[1];
        int iC = triangle[2];
        int iD = mesh_vertices.Count;
        float sqrRange = range * range * invertedScale * invertedScale;
        Vector3 pointA = mesh_vertices[iA];
        Vector3 pointB = mesh_vertices[iB];
        Vector3 pointC = mesh_vertices[iC];
        float distA = (pointA - targetPosition).sqrMagnitude;
        float distB = (pointB - targetPosition).sqrMagnitude;
        float distC = (pointC - targetPosition).sqrMagnitude;

        float distSum = distA + distB + distC;
        float percA = distA / distSum;
        float percB = distB / distSum;
        float percC = distC / distSum;
        float weightA = 1f/distA;
        float weightB = 1f/distB;
        float weightC = 1f/distC;
        Vector2 uvWeightedAvg = (mesh_uv[iA]*weightA+mesh_uv[iB]*weightB+mesh_uv[iC]*weightC)/(weightA+weightB+weightC);
        Vector2 uv2WeightedAvg = (mesh_uv2[iA]*weightA+mesh_uv2[iB]*weightB+mesh_uv2[iC]*weightC)/(weightA+weightB+weightC);
        Vector3 pointWeightedAvg = (mesh_vertices[iA]*weightA+mesh_vertices[iB]*weightB+mesh_vertices[iC]*weightC)/(weightA+weightB+weightC);
        Vector3 newVertex = pointWeightedAvg;//Vector3.Project(point,localHitNormal);//local space
        Vector3 newNormal = (mesh_normals[iA]*weightA+mesh_normals[iB]*weightB+mesh_normals[iC]*weightC)/(weightA+weightB+weightC);
        Vector2 newUV = uvWeightedAvg;// mesh_uv[iA]*percA + mesh_uv[iB]*percB + mesh_uv[iC]*percC; // hit.textureCoord;//these don't work
        Vector2 newUV2 = uv2WeightedAvg;//mesh_uv2[iA]*percA + mesh_uv2[iB]*percB + mesh_uv2[iC]*percC; //hit.textureCoord2;//these don't work
        Vector3 newTangents = mesh_tangents[iA];
        int triLastIndex = mesh_triangles.Count;
        //add one new vertex and 2 new triangles
        mesh_vertices.Add( new Vector3());//System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+1);
        mesh_normals.Add( new Vector3());//System.Array.Resize(ref mesh_normals, mesh_normals.Length+1);
        mesh_uv.Add( new Vector2());//System.Array.Resize(ref mesh_uv, mesh_uv.Length+1);
        mesh_uv2.Add( new Vector2());//System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+1);
        mesh_tangents.Add( new Vector4());//System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+1);
        mesh_triangles.AddRange( new int[6]);//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length+6);
        goodArray.Add( new float());//System.Array.Resize(ref goodArray, goodArray.Length+1);

        mesh_vertices[mesh_vertices.Count-1] = newVertex;
        mesh_normals[mesh_vertices.Count-1] = newNormal;
        mesh_uv[mesh_vertices.Count-1] = newUV;
        mesh_uv2[mesh_vertices.Count-1] = newUV2;
        mesh_tangents[mesh_vertices.Count-1] = newTangents;
        goodArray[mesh_vertices.Count-1] = 1f;
        mesh_triangles[triIndex*3]=iD;//abd
        mesh_triangles[triIndex*3+1]=iA;//abd
        mesh_triangles[triIndex*3+2]=iB;//abd

        mesh_triangles[triLastIndex]=iD;//bcd
        mesh_triangles[triLastIndex+1]=iB;//bcd
        mesh_triangles[triLastIndex+2]=iC;//bcd

        mesh_triangles[triLastIndex+3]=iD;//cad
        mesh_triangles[triLastIndex+4]=iC;//cad
        mesh_triangles[triLastIndex+5]=iA;//cad

        int[] triangle1 = new int[] {iD, iA, iB};
        int triIndex1 = triIndex;
        int[] triangle2 = new int[] {iD, iB, iC};
        int triIndex2 = triLastIndex/3;
        int[] triangle3 = new int[] {iD, iC, iA};
        int triIndex3 = triLastIndex/3 + 1;  

        Tessellate2(
            triangle1, //int[3]
            triIndex1, //int
            sub, //int
            range, //float
            ref mesh_vertices,
            ref mesh_normals,
            ref mesh_uv,
            ref mesh_uv2,
            ref mesh_tangents,
            ref mesh_triangles,
            ref tessDictionary, //new verts
            ref goodArray, //float[]
            0 //-1 tells Tessellate() not to recurse because recursion needs to be controlled here
        );

        Tessellate2(
            triangle2, //int[3]
            triIndex2, //int
            sub, //int
            range, //float
            ref mesh_vertices,
            ref mesh_normals,
            ref mesh_uv,
            ref mesh_uv2,
            ref mesh_tangents,
            ref mesh_triangles,
            ref tessDictionary, //new verts
            ref goodArray, //float[]
            0 //-1 tells Tessellate() not to recurse because recursion needs to be controlled here
        );

        Tessellate2(
            triangle3, //int[3]
            triIndex3, //int
            sub, //int
            range, //float
            ref mesh_vertices,
            ref mesh_normals,
            ref mesh_uv,
            ref mesh_uv2,
            ref mesh_tangents,
            ref mesh_triangles,
            ref tessDictionary, //new verts
            ref goodArray, //float[]
            0 //-1 tells Tessellate() not to recurse because recursion needs to be controlled here
        );

    }


    private void Tessellate(
        int[] triangle, 
        int triIndex, 
        int sub, 
        float range, 
        ref Vector3[] mesh_vertices,
        ref Vector3[] mesh_normals,
        ref Vector2[] mesh_uv,
        ref Vector2[] mesh_uv2,
        ref Vector4[] mesh_tangents,
        ref int[] mesh_triangles,
        ref Dictionary <VertexKey, VertexEntry> tessDictionary,
        ref float[] goodArray,
        int recursionDepth,
        ref int numVerts,
        ref int numTriangles
        )
    {
        int bufferSize = 256;

        if (recursionDepth == 0 && numVerts == mesh_vertices.Length)
        {
            isTessellatingInProgress = true;
            System.Array.Resize(ref mesh_vertices, numVerts + bufferSize);
            System.Array.Resize(ref mesh_normals, numVerts + bufferSize);
            System.Array.Resize(ref mesh_uv, numVerts + bufferSize);
            System.Array.Resize(ref mesh_uv2, numVerts + bufferSize);
            System.Array.Resize(ref mesh_tangents, numVerts + bufferSize);
            System.Array.Resize(ref goodArray, numVerts + bufferSize);
            System.Array.Resize(ref mesh_triangles, numTriangles + bufferSize * 6);
        }
        float sqrRange = range * range * invertedScale * invertedScale;
        //oops , for a long while I've been squaring "range" so sqrRange was to the 4th power!
        //calculate the distances for ab and ac
        int iA = triangle[0];
        int iB = triangle[1];
        int iC = triangle[2];
        Vector3 pointA = mesh_vertices[iA];
        Vector3 pointB = mesh_vertices[iB];
        Vector3 pointC = mesh_vertices[iC];
        Vector3 normalA = mesh_normals[iA];
        Vector3 normalB = mesh_normals[iB];
        Vector3 normalC = mesh_normals[iC];
        Vector3 uvA = mesh_uv[iA];
        Vector3 uvB = mesh_uv[iB];
        Vector3 uvC = mesh_uv[iC];
        Vector3 uv2A = mesh_uv2[iA];
        Vector3 uv2B = mesh_uv2[iB];
        Vector3 uv2C = mesh_uv2[iC];
        Vector4 tangentsA = mesh_tangents[iA];
        Vector4 tangentsB = mesh_tangents[iB];
        Vector4 tangentsC = mesh_tangents[iC];

        if (uv2A.y >= displacementLimit && uv2B.y >= displacementLimit && uv2C.y >= displacementLimit)//putting a limit on tessellation of displaced points to avoid extra shearing - doesn't really help
        {    
            if (isDebug) Debug.Log("displacement limit prevented further tessellation");
            if (recursionDepth == 0){
                isTessellatingInProgress = false;
                System.Array.Resize(ref mesh_vertices, numVerts);
                System.Array.Resize(ref mesh_normals, numVerts);
                System.Array.Resize(ref mesh_uv, numVerts);
                System.Array.Resize(ref mesh_uv2, numVerts);
                System.Array.Resize(ref mesh_tangents, numVerts);
                System.Array.Resize(ref goodArray, numVerts);
                System.Array.Resize(ref mesh_triangles, numTriangles);
            }
            return;//if none of the line segments are long enough to cut
        }
        //Debug.Log("Tessellate " + recursionDepth.ToString());
        float sqrdistAB = (pointA - pointB).sqrMagnitude;
        float sqrdistAC = (pointA - pointC).sqrMagnitude;
        float sqrdistBC = (pointB - pointC).sqrMagnitude;
        int iD = numVerts;//mesh_vertices.Length;
        int iE = numVerts + 1;//mesh_vertices.Length+1;

        VertexEntry entry;
        VertexKey key;
        bool cutAB = false;
        bool cutAC = false;

        if (sqrdistAB * invertedScale > sqrRange && sqrdistAB * invertedScale / sqrRange > tessellationMinLengthRatio)//1.25
            cutAB = true;
        if (sqrdistAC * invertedScale > sqrRange && sqrdistAC * invertedScale / sqrRange > tessellationMinLengthRatio)//1.25
            cutAC = true;

        if (cutAB && cutAC) //this scenario alone seems to be resulting in invalid tris indexed to high index verts
        {//this block works I think, but some faces are reversed
            int lengthAdd = 0;
            //bisect both edges, replace triangle, add 2 triangles
            //send 3 new tris to the Tessellate function recursively
            Vector3 pointD = new Vector3((pointA.x + pointB.x)/2f,(pointA.y + pointB.y)/2f,(pointA.z + pointB.z)/2f);//bisect ab
            bool pointDExists = tessDictionary.TryGetValue(key = new VertexKey(pointD), out entry);

            if (!pointDExists)
            {                
                iD = numVerts /*mesh_vertices.Length*/ + lengthAdd++;//assuming adding a vert at the end of the list
                entry = new VertexEntry(sub, 0, iD);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalD;
            Vector2 uvD;
            Vector2 uv2D;
            Vector3 tangentsD;
            float goodValueD;

            if (pointDExists)
            {
                iD = entry.VertexIndex;
                if (iD >= numVerts/*mesh_normals.Length*/)//added just in case
                {
                    if (recursionDepth == 0){
                        isTessellatingInProgress = false;
                        System.Array.Resize(ref mesh_vertices, numVerts);
                        System.Array.Resize(ref mesh_normals, numVerts);
                        System.Array.Resize(ref mesh_uv, numVerts);
                        System.Array.Resize(ref mesh_uv2, numVerts);
                        System.Array.Resize(ref mesh_tangents, numVerts);
                        System.Array.Resize(ref goodArray, numVerts);
                        System.Array.Resize(ref mesh_triangles, numTriangles);
                    }
                    if (isDebug) Debug.Log("out of bounds iD:" + iD.ToString() + " >= mesh_normals.Length:" + mesh_normals.Length.ToString() + " verts; " + mesh_vertices.Length.ToString());
                    return;//if none of the line segments are long enough to cut
                }
                normalD = mesh_normals[iD];
                uvD = mesh_uv[iD];
                uv2D = mesh_uv2[iD];
                tangentsD = mesh_tangents[iD];
                goodValueD = goodArray[iD];
            }
            else
            {
                normalD = new Vector3((normalA.x + normalB.x)/2f,(normalA.y + normalB.y)/2f,(normalA.z + normalB.z)/2f);//bisect ab;
                uvD = new Vector2((uvA.x + uvB.x)/2f,(uvA.y + uvB.y)/2f);//bisect ab;
                uv2D = new Vector2((uv2A.x + uv2B.x)/2f,(uv2A.y + uv2B.y)/2f);//bisect ab;
                tangentsD = (tangentsA + tangentsB)/2f;//bisect ab;
                goodValueD = (goodArray[iA]+goodArray[iB])/2f;
            }

            Vector3 pointE = new Vector3((pointA.x + pointC.x)/2f,(pointA.y + pointC.y)/2f,(pointA.z + pointC.z)/2f);//bisect ac
            bool pointEExists = tessDictionary.TryGetValue(key = new VertexKey(pointE), out entry);
            if (!pointEExists)
            {
                iE = numVerts/*mesh_vertices.Length*/ + lengthAdd++;//assuming adding a vert at the end of the list
                entry = new VertexEntry(sub, 0, iE);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalE;
            Vector2 uvE;
            Vector2 uv2E;
            Vector3 tangentsE;
            float goodValueE;

            if (pointEExists)
            {
                iE = entry.VertexIndex;
                if (iE >= numVerts /*mesh_normals.Length*/)
                {
                    if (recursionDepth == 0){
                        isTessellatingInProgress = false;
                        System.Array.Resize(ref mesh_vertices, numVerts);
                        System.Array.Resize(ref mesh_normals, numVerts);
                        System.Array.Resize(ref mesh_uv, numVerts);
                        System.Array.Resize(ref mesh_uv2, numVerts);
                        System.Array.Resize(ref mesh_tangents, numVerts);
                        System.Array.Resize(ref goodArray, numVerts);
                        System.Array.Resize(ref mesh_triangles, numTriangles);
                    }
                    if (isDebug) Debug.Log("out of bounds iE:" + iE.ToString() + " >= mesh_normals.Length:" + mesh_normals.Length.ToString() + " verts; " + mesh_vertices.Length.ToString());
                    return;//if none of the line segments are long enough to cut
                }
                normalE = mesh_normals[iE];//out of bounds 4/11/2019, must be calling tessellate without making sure mesh_normals is updated
                uvE = mesh_uv[iE];
                uv2E = mesh_uv2[iE];
                tangentsE = mesh_tangents[iE];
                goodValueE = goodArray[iE];
            }
            else
            {
                normalE = new Vector3((normalA.x + normalC.x)/2f,(normalA.y + normalC.y)/2f,(normalA.z + normalC.z)/2f);//bisect ac;
                uvE = new Vector2((uvA.x + uvC.x)/2f,(uvA.y + uvC.y)/2f);//bisect ac;
                uv2E = new Vector2((uv2A.x + uv2C.x)/2f,(uv2A.y + uv2C.y)/2f);//bisect ac;
                tangentsE = (tangentsA + tangentsC)/2f;//bisect ac;
                goodValueE = (goodArray[iA] + goodArray[iC])/2f;
            }

            int triLastIndex = numTriangles;//mesh_triangles.Length;

            if (lengthAdd > 0){
                numVerts += lengthAdd;
                // System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+lengthAdd);
                // System.Array.Resize(ref mesh_normals, mesh_normals.Length+lengthAdd);
                // System.Array.Resize(ref mesh_uv, mesh_uv.Length+lengthAdd);
                // System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+lengthAdd);
                // System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+lengthAdd);
                // System.Array.Resize(ref goodArray, goodArray.Length+lengthAdd);
                if (numVerts > mesh_vertices.Length)//if we push past the buffersize, add more
                {
                    System.Array.Resize(ref mesh_vertices, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_normals, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_uv, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_uv2, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_tangents, numVerts + bufferSize);
                    System.Array.Resize(ref goodArray, numVerts + bufferSize);
                }
            }
            numTriangles += 6;//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length + 6);
            if (numTriangles > mesh_triangles.Length) System.Array.Resize(ref mesh_triangles, numTriangles + bufferSize * 6);

            if (!pointDExists)
            {
                mesh_vertices[iD] = pointD;
                mesh_normals[iD] = normalD;
                mesh_uv[iD] = uvD;
                mesh_uv2[iD] = uv2D;
                mesh_tangents[iD] = tangentsD;
                goodArray[iD] = goodValueD;
            }

            if (!pointEExists)
            {
                mesh_vertices[iE] = pointE;
                mesh_normals[iE] = normalE;
                mesh_uv[iE] = uvE;
                mesh_uv2[iE] = uv2E;
                mesh_tangents[iE] = tangentsE;
                goodArray[iE] = goodValueE;
            }
            //ade dbe bce
            mesh_triangles[triIndex*3]=iA;//ade //with recursion I'm getting out of range exceptions
            mesh_triangles[triIndex*3+1]=iD;//ade
            mesh_triangles[triIndex*3+2]=iE;//ade
            mesh_triangles[triLastIndex]=iD;//dbe dce
            mesh_triangles[triLastIndex+1]=iC;//dbe dce
            mesh_triangles[triLastIndex+2]=iE;//dbe dce
            mesh_triangles[triLastIndex+3]=iD;//bce dbc
            mesh_triangles[triLastIndex+4]=iB;//bce dbc
            mesh_triangles[triLastIndex+5]=iC;//bce dbc

            int[] triangle1 = new int[] {iA, iD, iE};
            int triIndex1 = triIndex;
            if (isRecursive && recursionDepth > 0){
                Tessellate(
                    triangle1, //int[3]
                    triIndex1, //int
                    sub, //int
                    range, //float
                    ref mesh_vertices,
                    ref mesh_normals,
                    ref mesh_uv,
                    ref mesh_uv2,
                    ref mesh_tangents,
                    ref mesh_triangles,
                    ref tessDictionary, //new verts
                    ref goodArray, //float[]
                    recursionDepth+1, //if it's 0 you can finalize the data before returning
                    ref numVerts,//track these separately so you can resize each array once or twice only
                    ref numTriangles//track these separately so you can resize each array once or twice only
                );
            }
            if (recursionDepth == 0){
                isTessellatingInProgress = false;
                System.Array.Resize(ref mesh_vertices, numVerts);
                System.Array.Resize(ref mesh_normals, numVerts);
                System.Array.Resize(ref mesh_uv, numVerts);
                System.Array.Resize(ref mesh_uv2, numVerts);
                System.Array.Resize(ref mesh_tangents, numVerts);
                System.Array.Resize(ref goodArray, numVerts);
                System.Array.Resize(ref mesh_triangles, numTriangles);
            }
            return;//if none of the line segments are long enough to cut
        }
        else if (cutAB)
        {
            int lengthAdd = 0;
            //bisect AB, add to dict, replace triangle, add a new triangle
            Vector3 pointD = new Vector3((pointA.x + pointB.x)/2f,(pointA.y + pointB.y)/2f,(pointA.z + pointB.z)/2f);//bisect ab
            bool pointDExists = tessDictionary.TryGetValue(key = new VertexKey(pointD), out entry);
            if (!pointDExists)
            {
                iD = numVerts/*mesh_vertices.Length*/ + lengthAdd++;
                entry = new VertexEntry(sub, 0, iD);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalD;
            Vector2 uvD;
            Vector2 uv2D;
            Vector3 tangentsD;
            float goodValueD;

            if (pointDExists)
            {
                iD = entry.VertexIndex;
                if (iD >= numVerts/*mesh_normals.Length*/)//this is happening in the build and is probably resulting in holes in the mesh, fix it or learn to abort gracefully!
                {
                    if (recursionDepth == 0){
                        isTessellatingInProgress = false;
                        System.Array.Resize(ref mesh_vertices, numVerts);
                        System.Array.Resize(ref mesh_normals, numVerts);
                        System.Array.Resize(ref mesh_uv, numVerts);
                        System.Array.Resize(ref mesh_uv2, numVerts);
                        System.Array.Resize(ref mesh_tangents, numVerts);
                        System.Array.Resize(ref goodArray, numVerts);
                        System.Array.Resize(ref mesh_triangles, numTriangles);
                    }
                    if (isDebug) Debug.Log("out of bounds iD:" + iD.ToString() + " >= mesh_normals.Length:" + mesh_normals.Length.ToString() + " verts; " + mesh_vertices.Length.ToString());
                    return;//if none of the line segments are long enough to cut
                }
                normalD = mesh_normals[iD];//out of bounds
                uvD = mesh_uv[iD];
                uv2D = mesh_uv2[iD];
                tangentsD = mesh_tangents[iD];
                goodValueD = goodArray[iD];
            }
            else 
            {
                normalD = new Vector3((normalA.x + normalB.x)/2f,(normalA.y + normalB.y)/2f,(normalA.z + normalB.z)/2f);//bisect ab;
                uvD = new Vector2((uvA.x + uvB.x)/2f,(uvA.y + uvB.y)/2f);//bisect ab;
                uv2D = new Vector2((uv2A.x + uv2B.x)/2f,(uv2A.y + uv2B.y)/2f);//bisect ab;
                tangentsD = (tangentsA + tangentsB)/2f;//bisect ab;
                goodValueD = (goodArray[iA]+goodArray[iB])/2f;
            }

            int triLastIndex = numTriangles;//mesh_triangles.Length;

            if (lengthAdd > 0){
                numVerts += lengthAdd;
                // System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+lengthAdd);
                // System.Array.Resize(ref mesh_normals, mesh_normals.Length+lengthAdd);
                // System.Array.Resize(ref mesh_uv, mesh_uv.Length+lengthAdd);
                // System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+lengthAdd);
                // System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+lengthAdd);
                // System.Array.Resize(ref goodArray, goodArray.Length+lengthAdd);
                if (numVerts > mesh_vertices.Length)//if we push past the buffersize, add more
                {
                    System.Array.Resize(ref mesh_vertices, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_normals, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_uv, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_uv2, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_tangents, numVerts + bufferSize);
                    System.Array.Resize(ref goodArray, numVerts + bufferSize);
                }
            }
            numTriangles += 3;//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length + 6);
            if (numTriangles > mesh_triangles.Length) System.Array.Resize(ref mesh_triangles, numTriangles + bufferSize * 6);

            if (!pointDExists)
            {
                mesh_vertices[iD] = pointD;
                //Debug.Log("adding new vert indexD: " + iD.ToString());
                mesh_normals[iD] = normalD;
                mesh_uv[iD] = uvD;
                mesh_uv2[iD] = uv2D;
                mesh_tangents[iD] = tangentsD;
                goodArray[iD] = goodValueD;
            }

            mesh_triangles[triIndex*3]=iA;//adc
            mesh_triangles[triIndex*3+1]=iD;//adc
            mesh_triangles[triIndex*3+2]=iC;//adc

            mesh_triangles[triLastIndex]=iD;//dbc
            mesh_triangles[triLastIndex+1]=iB;//dbc
            mesh_triangles[triLastIndex+2]=iC;//dbc
            int[] triangle1 = new int[] {iA, iD, iC};
            int triIndex1 = triIndex;
            if (isDebug) Debug.Log("cutAB recursion depth " + recursionDepth.ToString());
            if (isRecursive && recursionDepth > 0){
                Tessellate(
                    triangle1, //int[3]
                    triIndex1, //int
                    sub, //int
                    range, //float
                    ref mesh_vertices,
                    ref mesh_normals,
                    ref mesh_uv,
                    ref mesh_uv2,
                    ref mesh_tangents,
                    ref mesh_triangles,
                    ref tessDictionary, //new verts
                    ref goodArray, //float[]
                    recursionDepth+1,
                    ref numVerts,
                    ref numTriangles
                );
            }
            if (recursionDepth == 0){
                isTessellatingInProgress = false;
                System.Array.Resize(ref mesh_vertices, numVerts);
                System.Array.Resize(ref mesh_normals, numVerts);
                System.Array.Resize(ref mesh_uv, numVerts);
                System.Array.Resize(ref mesh_uv2, numVerts);
                System.Array.Resize(ref mesh_tangents, numVerts);
                System.Array.Resize(ref goodArray, numVerts);
                System.Array.Resize(ref mesh_triangles, numTriangles);
            }
            return;//if none of the line segments are long enough to cut
        }
        else if (cutAC)
        {
            int lengthAdd = 0;
            //bisect AC add triangles adc abd
            Vector3 pointD = new Vector3((pointA.x + pointC.x)/2f,(pointA.y + pointC.y)/2f,(pointA.z + pointC.z)/2f);//bisect ac
            bool pointDExists = tessDictionary.TryGetValue(key = new VertexKey(pointD), out entry);
            if (!pointDExists)
            {
                iD = numVerts/*mesh_vertices.Length*/ + lengthAdd++;
                entry = new VertexEntry(sub, 0, iD);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalD;
            Vector2 uvD;
            Vector2 uv2D;
            Vector3 tangentsD;
            float goodValueD;
            if (pointDExists)
            {
                iD = entry.VertexIndex;
                if (iD >= numVerts/*mesh_normals.Length*/)
                {
                    if (recursionDepth == 0){
                        isTessellatingInProgress = false;
                        System.Array.Resize(ref mesh_vertices, numVerts);
                        System.Array.Resize(ref mesh_normals, numVerts);
                        System.Array.Resize(ref mesh_uv, numVerts);
                        System.Array.Resize(ref mesh_uv2, numVerts);
                        System.Array.Resize(ref mesh_tangents, numVerts);
                        System.Array.Resize(ref goodArray, numVerts);
                        System.Array.Resize(ref mesh_triangles, numTriangles);
                    }
                    if (isDebug) Debug.Log("out of bounds iD:" + iD.ToString() + " >= mesh_normals.Length:" + mesh_normals.Length.ToString() + " verts; " + mesh_vertices.Length.ToString());

                    return;//if none of the line segments are long enough to cut
                }
                normalD = mesh_normals[iD];//out of bounds
                uvD = mesh_uv[iD];
                uv2D = mesh_uv2[iD];
                tangentsD = mesh_tangents[iD];
                goodValueD = goodArray[iD];
            }
            else 
            {
                normalD = new Vector3((normalA.x + normalC.x)/2f,(normalA.y + normalC.y)/2f,(normalA.z + normalC.z)/2f);//bisect ab;
                uvD = new Vector2((uvA.x + uvC.x)/2f,(uvA.y + uvC.y)/2f);//bisect ab;
                uv2D = new Vector2((uv2A.x + uv2C.x)/2f,(uv2A.y + uv2C.y)/2f);//bisect ab;
                tangentsD = (tangentsA + tangentsC)/2f;//bisect ab;
                goodValueD = (goodArray[iA]+goodArray[iC])/2f;
            }

            int triLastIndex = numTriangles;//mesh_triangles.Length;

            if (lengthAdd > 0){
                numVerts += lengthAdd;
                // System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+lengthAdd);
                // System.Array.Resize(ref mesh_normals, mesh_normals.Length+lengthAdd);
                // System.Array.Resize(ref mesh_uv, mesh_uv.Length+lengthAdd);
                // System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+lengthAdd);
                // System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+lengthAdd);
                // System.Array.Resize(ref goodArray, goodArray.Length+lengthAdd);
                if (numVerts > mesh_vertices.Length)//if we push past the buffersize, add more
                {
                    System.Array.Resize(ref mesh_vertices, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_normals, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_uv, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_uv2, numVerts + bufferSize);
                    System.Array.Resize(ref mesh_tangents, numVerts + bufferSize);
                    System.Array.Resize(ref goodArray, numVerts + bufferSize);
                }
            }
            numTriangles += 3;//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length + 6);
            if (numTriangles > mesh_triangles.Length) System.Array.Resize(ref mesh_triangles, numTriangles + bufferSize * 6);

            if (!pointDExists)
            {
                mesh_vertices[iD] = pointD;
                //Debug.Log("adding new vert indexD: " + iD.ToString());//664
                //Debug.Log("mesh_normals.Length: " + mesh_normals.Length.ToString());//665
                mesh_normals[iD] = normalD;
                mesh_uv[iD] = uvD;
                mesh_uv2[iD] = uv2D;
                mesh_tangents[iD] = tangentsD;
                goodArray[iD] = goodValueD;
            }

            mesh_triangles[triIndex*3]=iA;//abd
            mesh_triangles[triIndex*3+1]=iB;//abd
            mesh_triangles[triIndex*3+2]=iD;//abd

            mesh_triangles[triLastIndex]=iB;//bcd
            mesh_triangles[triLastIndex+1]=iC;//bcd
            mesh_triangles[triLastIndex+2]=iD;//bcd

            int[] triangle1 = new int[] {iA, iB, iD};
            int triIndex1 = triIndex;
            if (isDebug) Debug.Log("cutAC recursion depth " + recursionDepth.ToString());
            if (isRecursive && recursionDepth > 0)
            {
                Tessellate(
                    triangle1, //int[3]
                    triIndex1, //int
                    sub, //int
                    range, //float
                    ref mesh_vertices,
                    ref mesh_normals,
                    ref mesh_uv,
                    ref mesh_uv2,
                    ref mesh_tangents,
                    ref mesh_triangles,
                    ref tessDictionary, //new verts
                    ref goodArray, //float[]
                    recursionDepth+1,
                    ref numVerts,
                    ref numTriangles
                    );
            }
            if (recursionDepth == 0){
                isTessellatingInProgress = false;
                System.Array.Resize(ref mesh_vertices, numVerts);
                System.Array.Resize(ref mesh_normals, numVerts);
                System.Array.Resize(ref mesh_uv, numVerts);
                System.Array.Resize(ref mesh_uv2, numVerts);
                System.Array.Resize(ref mesh_tangents, numVerts);
                System.Array.Resize(ref goodArray, numVerts);
                System.Array.Resize(ref mesh_triangles, numTriangles);
            }
            return;//if none of the line segments are long enough to cut
        }
        if (recursionDepth == 0){
            isTessellatingInProgress = false;
            System.Array.Resize(ref mesh_vertices, numVerts);
            System.Array.Resize(ref mesh_normals, numVerts);
            System.Array.Resize(ref mesh_uv, numVerts);
            System.Array.Resize(ref mesh_uv2, numVerts);
            System.Array.Resize(ref mesh_tangents, numVerts);
            System.Array.Resize(ref goodArray, numVerts);
            System.Array.Resize(ref mesh_triangles, numTriangles);
        }
        return;//if none of the line segments are long enough to cut
    }

    private void Tessellate2(//uses lists instead of arrays
        int[] triangle, 
        int triIndex, 
        int sub, 
        float range, 
        ref List<Vector3> mesh_vertices,
        ref List<Vector3> mesh_normals,
        ref List<Vector2> mesh_uv,
        ref List<Vector2> mesh_uv2,
        ref List<Vector4> mesh_tangents,
        ref List<int> mesh_triangles,
        ref Dictionary <VertexKey, VertexEntry> tessDictionary,
        ref List<float> goodArray,
        int recursionDepth
        )
    {
        if (recursionDepth == 0)
        {
            isTessellatingInProgress = true;
        }
        float sqrRange = range * range * invertedScale * invertedScale;
        //oops , for a long while I've been squaring "range" so sqrRange was to the 4th power!
        //calculate the distances for ab and ac
        int iA = triangle[0];
        int iB = triangle[1];
        int iC = triangle[2];
        Vector3 pointA = mesh_vertices[iA];
        Vector3 pointB = mesh_vertices[iB];
        Vector3 pointC = mesh_vertices[iC];
        Vector3 normalA = mesh_normals[iA];
        Vector3 normalB = mesh_normals[iB];
        Vector3 normalC = mesh_normals[iC];
        Vector3 uvA = mesh_uv[iA];
        Vector3 uvB = mesh_uv[iB];
        Vector3 uvC = mesh_uv[iC];
        Vector3 uv2A = mesh_uv2[iA];
        Vector3 uv2B = mesh_uv2[iB];
        Vector3 uv2C = mesh_uv2[iC];
        Vector4 tangentsA = mesh_tangents[iA];
        Vector4 tangentsB = mesh_tangents[iB];
        Vector4 tangentsC = mesh_tangents[iC];

        if (uv2A.y >= displacementLimit && uv2B.y >= displacementLimit && uv2C.y >= displacementLimit)//putting a limit on tessellation of displaced points to avoid extra shearing - doesn't really help
        {    
            if (isDebug) Debug.Log("displacement limit prevented further tessellation");
            if (recursionDepth == 0)
                isTessellatingInProgress = false;
            return;
        }
        //Debug.Log("Tessellate " + recursionDepth.ToString());
        float sqrdistAB = (pointA - pointB).sqrMagnitude;
        float sqrdistAC = (pointA - pointC).sqrMagnitude;
        float sqrdistBC = (pointB - pointC).sqrMagnitude;
        int iD = mesh_vertices.Count;
        int iE = mesh_vertices.Count+1;

        VertexEntry entry;
        VertexKey key;
        bool cutAB = false;
        bool cutAC = false;

        if (sqrdistAB * invertedScale > sqrRange && sqrdistAB * invertedScale / sqrRange > tessellationMinLengthRatio)//1.25
            cutAB = true;
        if (sqrdistAC * invertedScale > sqrRange && sqrdistAC * invertedScale / sqrRange > tessellationMinLengthRatio)//1.25
            cutAC = true;

        if (cutAB && cutAC) //this scenario alone seems to be resulting in invalid tris indexed to high index verts
        {//this block works I think, but some faces are reversed
            int lengthAdd = 0;
            //bisect both edges, replace triangle, add 2 triangles
            //send 3 new tris to the Tessellate function recursively
            Vector3 pointD = new Vector3((pointA.x + pointB.x)/2f,(pointA.y + pointB.y)/2f,(pointA.z + pointB.z)/2f);//bisect ab
            bool pointDExists = tessDictionary.TryGetValue(key = new VertexKey(pointD), out entry);

            if (!pointDExists)
            {                
                iD = mesh_vertices.Count + lengthAdd++;//assuming adding a vert at the end of the list
                entry = new VertexEntry(sub, 0, iD);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalD;
            Vector2 uvD;
            Vector2 uv2D;
            Vector3 tangentsD;
            float goodValueD;

            if (pointDExists)
            {
                iD = entry.VertexIndex;
                if (iD >= mesh_normals.Count)//added just in case
                {
                    if (isDebug) Debug.Log("out of bounds iD:" + iD.ToString() + " >= mesh_normals.Count:" + mesh_normals.Count.ToString() + " verts; " + mesh_vertices.Count.ToString());
                    if (recursionDepth == 0)
                        isTessellatingInProgress = false;
                    return;//abort
                }
                normalD = mesh_normals[iD];
                uvD = mesh_uv[iD];
                uv2D = mesh_uv2[iD];
                tangentsD = mesh_tangents[iD];
                goodValueD = goodArray[iD];
            }
            else
            {
                normalD = new Vector3((normalA.x + normalB.x)/2f,(normalA.y + normalB.y)/2f,(normalA.z + normalB.z)/2f);//bisect ab;
                uvD = new Vector2((uvA.x + uvB.x)/2f,(uvA.y + uvB.y)/2f);//bisect ab;
                uv2D = new Vector2((uv2A.x + uv2B.x)/2f,(uv2A.y + uv2B.y)/2f);//bisect ab;
                tangentsD = (tangentsA + tangentsB)/2f;//bisect ab;
                goodValueD = (goodArray[iA]+goodArray[iB])/2f;
            }

            Vector3 pointE = new Vector3((pointA.x + pointC.x)/2f,(pointA.y + pointC.y)/2f,(pointA.z + pointC.z)/2f);//bisect ac
            bool pointEExists = tessDictionary.TryGetValue(key = new VertexKey(pointE), out entry);
            if (!pointEExists)
            {
                iE = mesh_vertices.Count + lengthAdd++;//assuming adding a vert at the end of the list
                entry = new VertexEntry(sub, 0, iE);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalE;
            Vector2 uvE;
            Vector2 uv2E;
            Vector3 tangentsE;
            float goodValueE;
            //if (mesh_vertices.Count != mesh_normals.Count)
            //    Debug.Log("out of sync verts:" + mesh_vertices.Count.ToString() + " normals: " + mesh_normals.Count.ToString());

            if (pointEExists)
            {
                iE = entry.VertexIndex;
                if (iE >= mesh_normals.Count)
                {
                    if (isDebug) Debug.Log("out of bounds iE:" + iE.ToString() + " >= mesh_normals.Count:" + mesh_normals.Count.ToString() + " verts; " + mesh_vertices.Count.ToString());
                    if (recursionDepth == 0)
                        isTessellatingInProgress = false;
                    return;//abort
                }
                normalE = mesh_normals[iE];//out of bounds 4/11/2019, must be calling tessellate without making sure mesh_normals is updated
                uvE = mesh_uv[iE];
                uv2E = mesh_uv2[iE];
                tangentsE = mesh_tangents[iE];
                goodValueE = goodArray[iE];
            }
            else
            {
                normalE = new Vector3((normalA.x + normalC.x)/2f,(normalA.y + normalC.y)/2f,(normalA.z + normalC.z)/2f);//bisect ac;
                uvE = new Vector2((uvA.x + uvC.x)/2f,(uvA.y + uvC.y)/2f);//bisect ac;
                uv2E = new Vector2((uv2A.x + uv2C.x)/2f,(uv2A.y + uv2C.y)/2f);//bisect ac;
                tangentsE = (tangentsA + tangentsC)/2f;//bisect ac;
                goodValueE = (goodArray[iA] + goodArray[iC])/2f;
            }

            int triLastIndex = mesh_triangles.Count;

            if (lengthAdd > 0){
                mesh_vertices.AddRange( new Vector3[lengthAdd]);//System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+lengthAdd);
                mesh_normals.AddRange(new Vector3[lengthAdd]);//System.Array.Resize(ref mesh_normals, mesh_normals.Length+lengthAdd);
                mesh_uv.AddRange(new Vector2[lengthAdd]);//System.Array.Resize(ref mesh_uv, mesh_uv.Length+lengthAdd);
                mesh_uv2.AddRange(new Vector2[lengthAdd]);//System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+lengthAdd);
                mesh_tangents.AddRange(new Vector4[lengthAdd]);// System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+lengthAdd);
                goodArray.AddRange(new float[lengthAdd]);//System.Array.Resize(ref goodArray, goodArray.Length+lengthAdd);
            }
            mesh_triangles.AddRange(new int[6]);//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length + 6);

            if (!pointDExists)
            {
                mesh_vertices[iD] = pointD;
                mesh_normals[iD] = normalD;
                mesh_uv[iD] = uvD;
                mesh_uv2[iD] = uv2D;
                mesh_tangents[iD] = tangentsD;
                goodArray[iD] = goodValueD;
            }

            if (!pointEExists)
            {
                mesh_vertices[iE] = pointE;
                mesh_normals[iE] = normalE;
                mesh_uv[iE] = uvE;
                mesh_uv2[iE] = uv2E;
                mesh_tangents[iE] = tangentsE;
                goodArray[iE] = goodValueE;
            }
            //ade dbe bce
            mesh_triangles[triIndex*3]=iA;//ade //with recursion I'm getting out of range exceptions
            mesh_triangles[triIndex*3+1]=iD;//ade
            mesh_triangles[triIndex*3+2]=iE;//ade
            mesh_triangles[triLastIndex]=iD;//dbe dce
            mesh_triangles[triLastIndex+1]=iC;//dbe dce
            mesh_triangles[triLastIndex+2]=iE;//dbe dce
            mesh_triangles[triLastIndex+3]=iD;//bce dbc
            mesh_triangles[triLastIndex+4]=iB;//bce dbc
            mesh_triangles[triLastIndex+5]=iC;//bce dbc

            int[] triangle1 = new int[] {iA, iD, iE};
            int triIndex1 = triIndex;

            if (isRecursive && recursionDepth > 0){
                Tessellate2(
                    triangle1, //int[3]
                    triIndex1, //int
                    sub, //int
                    range, //float
                    ref mesh_vertices,
                    ref mesh_normals,
                    ref mesh_uv,
                    ref mesh_uv2,
                    ref mesh_tangents,
                    ref mesh_triangles,
                    ref tessDictionary, //new verts
                    ref goodArray, //float[]
                    recursionDepth+1 //if it's 0 you can finalize the data before returning
                );
            }
            if (recursionDepth == 0)
                isTessellatingInProgress = false;
            return;
        }
        else if (cutAB)
        {
            int lengthAdd = 0;
            //bisect AB, add to dict, replace triangle, add a new triangle
            Vector3 pointD = new Vector3((pointA.x + pointB.x)/2f,(pointA.y + pointB.y)/2f,(pointA.z + pointB.z)/2f);//bisect ab
            bool pointDExists = tessDictionary.TryGetValue(key = new VertexKey(pointD), out entry);
            if (!pointDExists)
            {
                iD = mesh_vertices.Count + lengthAdd++;
                entry = new VertexEntry(sub, 0, iD);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalD;
            Vector2 uvD;
            Vector2 uv2D;
            Vector3 tangentsD;
            float goodValueD;

            if (pointDExists)
            {
                iD = entry.VertexIndex;
                if (iD >= mesh_normals.Count)//this is happening in the build and is probably resulting in holes in the mesh, fix it or learn to abort gracefully!
                {
                    if (isDebug) Debug.Log("out of bounds iD:" + iD.ToString() + " >= mesh_normals.Count:" + mesh_normals.Count.ToString() + " verts; " + mesh_vertices.Count.ToString());
                    if (recursionDepth == 0)
                        isTessellatingInProgress = false;
                    return;//abort
                }
                normalD = mesh_normals[iD];//out of bounds
                uvD = mesh_uv[iD];
                uv2D = mesh_uv2[iD];
                tangentsD = mesh_tangents[iD];
                goodValueD = goodArray[iD];
            }
            else 
            {
                normalD = new Vector3((normalA.x + normalB.x)/2f,(normalA.y + normalB.y)/2f,(normalA.z + normalB.z)/2f);//bisect ab;
                uvD = new Vector2((uvA.x + uvB.x)/2f,(uvA.y + uvB.y)/2f);//bisect ab;
                uv2D = new Vector2((uv2A.x + uv2B.x)/2f,(uv2A.y + uv2B.y)/2f);//bisect ab;
                tangentsD = (tangentsA + tangentsB)/2f;//bisect ab;
                goodValueD = (goodArray[iA]+goodArray[iB])/2f;
            }

            int triLastIndex = mesh_triangles.Count;

            if (lengthAdd > 0){
                mesh_vertices.AddRange( new Vector3[lengthAdd]);//System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+lengthAdd);
                mesh_normals.AddRange(new Vector3[lengthAdd]);//System.Array.Resize(ref mesh_normals, mesh_normals.Length+lengthAdd);
                mesh_uv.AddRange(new Vector2[lengthAdd]);//System.Array.Resize(ref mesh_uv, mesh_uv.Length+lengthAdd);
                mesh_uv2.AddRange(new Vector2[lengthAdd]);//System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+lengthAdd);
                mesh_tangents.AddRange(new Vector4[lengthAdd]);// System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+lengthAdd);
                goodArray.AddRange(new float[lengthAdd]);//System.Array.Resize(ref goodArray, goodArray.Length+lengthAdd);
            }
            mesh_triangles.AddRange(new int[3]);//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length + 3);

            if (!pointDExists)
            {
                mesh_vertices[iD] = pointD;
                //Debug.Log("adding new vert indexD: " + iD.ToString());
                mesh_normals[iD] = normalD;
                mesh_uv[iD] = uvD;
                mesh_uv2[iD] = uv2D;
                mesh_tangents[iD] = tangentsD;
                goodArray[iD] = goodValueD;
            }

            mesh_triangles[triIndex*3]=iA;//adc
            mesh_triangles[triIndex*3+1]=iD;//adc
            mesh_triangles[triIndex*3+2]=iC;//adc

            mesh_triangles[triLastIndex]=iD;//dbc
            mesh_triangles[triLastIndex+1]=iB;//dbc
            mesh_triangles[triLastIndex+2]=iC;//dbc
            int[] triangle1 = new int[] {iA, iD, iC};
            int triIndex1 = triIndex;
            if (isDebug) Debug.Log("cutAB recursion depth " + recursionDepth.ToString());
            if (isRecursive && recursionDepth > 0){
                Tessellate2(
                    triangle1, //int[3]
                    triIndex1, //int
                    sub, //int
                    range, //float
                    ref mesh_vertices,
                    ref mesh_normals,
                    ref mesh_uv,
                    ref mesh_uv2,
                    ref mesh_tangents,
                    ref mesh_triangles,
                    ref tessDictionary, //new verts
                    ref goodArray, //float[]
                    recursionDepth+1
                );
            }
            if (recursionDepth == 0)
                isTessellatingInProgress = false;
            return;
        }
        else if (cutAC)
        {
            int lengthAdd = 0;
            //bisect AC add triangles adc abd
            Vector3 pointD = new Vector3((pointA.x + pointC.x)/2f,(pointA.y + pointC.y)/2f,(pointA.z + pointC.z)/2f);//bisect ac
            bool pointDExists = tessDictionary.TryGetValue(key = new VertexKey(pointD), out entry);
            if (!pointDExists)
            {
                iD = mesh_vertices.Count + lengthAdd++;
                entry = new VertexEntry(sub, 0, iD);//sub, triIndex,vertIndex
                tessDictionary.Add(key, entry); //key and the index point
            }
            Vector3 normalD;
            Vector2 uvD;
            Vector2 uv2D;
            Vector3 tangentsD;
            float goodValueD;
            if (pointDExists)
            {
                iD = entry.VertexIndex;
                if (iD >= mesh_normals.Count)
                {
                    if (isDebug) Debug.Log("out of bounds iD:" + iD.ToString() + " >= mesh_normals.Count:" + mesh_normals.Count.ToString() + " verts; " + mesh_vertices.Count.ToString());
                    if (recursionDepth == 0)
                        isTessellatingInProgress = false;
                    return;//abort
                }
                normalD = mesh_normals[iD];//out of bounds
                uvD = mesh_uv[iD];
                uv2D = mesh_uv2[iD];
                tangentsD = mesh_tangents[iD];
                goodValueD = goodArray[iD];
            }
            else 
            {
                normalD = new Vector3((normalA.x + normalC.x)/2f,(normalA.y + normalC.y)/2f,(normalA.z + normalC.z)/2f);//bisect ab;
                uvD = new Vector2((uvA.x + uvC.x)/2f,(uvA.y + uvC.y)/2f);//bisect ab;
                uv2D = new Vector2((uv2A.x + uv2C.x)/2f,(uv2A.y + uv2C.y)/2f);//bisect ab;
                tangentsD = (tangentsA + tangentsC)/2f;//bisect ab;
                goodValueD = (goodArray[iA]+goodArray[iC])/2f;
            }

            int triLastIndex = mesh_triangles.Count;

            if (lengthAdd > 0){
                mesh_vertices.AddRange( new Vector3[lengthAdd]);//System.Array.Resize(ref mesh_vertices, mesh_vertices.Length+lengthAdd);
                mesh_normals.AddRange(new Vector3[lengthAdd]);//System.Array.Resize(ref mesh_normals, mesh_normals.Length+lengthAdd);
                mesh_uv.AddRange(new Vector2[lengthAdd]);//System.Array.Resize(ref mesh_uv, mesh_uv.Length+lengthAdd);
                mesh_uv2.AddRange(new Vector2[lengthAdd]);//System.Array.Resize(ref mesh_uv2, mesh_uv2.Length+lengthAdd);
                mesh_tangents.AddRange(new Vector4[lengthAdd]);// System.Array.Resize(ref mesh_tangents, mesh_tangents.Length+lengthAdd);
                goodArray.AddRange(new float[lengthAdd]);//System.Array.Resize(ref goodArray, goodArray.Length+lengthAdd);
            }
            mesh_triangles.AddRange(new int[3]);//System.Array.Resize(ref mesh_triangles, mesh_triangles.Length + 3);

            if (!pointDExists)
            {
                mesh_vertices[iD] = pointD;
                //Debug.Log("adding new vert indexD: " + iD.ToString());//664
                //Debug.Log("mesh_normals.Length: " + mesh_normals.Length.ToString());//665
                mesh_normals[iD] = normalD;
                mesh_uv[iD] = uvD;
                mesh_uv2[iD] = uv2D;
                mesh_tangents[iD] = tangentsD;
                goodArray[iD] = goodValueD;
            }

            mesh_triangles[triIndex*3]=iA;//abd
            mesh_triangles[triIndex*3+1]=iB;//abd
            mesh_triangles[triIndex*3+2]=iD;//abd

            mesh_triangles[triLastIndex]=iB;//bcd
            mesh_triangles[triLastIndex+1]=iC;//bcd
            mesh_triangles[triLastIndex+2]=iD;//bcd

            int[] triangle1 = new int[] {iA, iB, iD};
            int triIndex1 = triIndex;
            if (isDebug) Debug.Log("cutAC recursion depth " + recursionDepth.ToString());
            if (isRecursive && recursionDepth > 0)
            {
                Tessellate2(
                    triangle1, //int[3]
                    triIndex1, //int
                    sub, //int
                    range, //float
                    ref mesh_vertices,
                    ref mesh_normals,
                    ref mesh_uv,
                    ref mesh_uv2,
                    ref mesh_tangents,
                    ref mesh_triangles,
                    ref tessDictionary, //new verts
                    ref goodArray, //float[]
                    recursionDepth+1
                    );
            }
            if (recursionDepth == 0)
                isTessellatingInProgress = false;
            return;
        }
        if (recursionDepth == 0)
            isTessellatingInProgress = false;
        return;//if none of the line segments are long enough to cut
    }

    public void SetHotTouchPoint(Vector3 touchPoint) {
        lastHotTouchPoint = _transform.InverseTransformPoint(touchPoint);
    }

    void Start (){//this runs when meshcuts get re-enabled
        ren = GetComponent<Renderer>();
        _audioManager = GameObject.Find("Game").GetComponent<AudioManager>();
        _transform = transform;
        _mainCameraTransform = Camera.main.transform;
        invertedScale = 1/((_transform.lossyScale.x + _transform.lossyScale.y + _transform.lossyScale.z)/3);//trying lossyScale instead of localScale
        mf = GetComponent<MeshFilter>();
        localMesh = mf.mesh;
        mr = GetComponent<MeshRenderer>();
        //Init hotLights in case they haven't been created yet
        if (UnityEngine.XR.XRDevice.model.Contains("Quest"))
        { 
            numHotLights = 0;
        }
        if (hotMetalLights == null)
        {
            hotMetalLights = new GameObject[numHotLights];
            for (int i = 0; i < numHotLights; i++) 
            {
                hotMetalLights[i] = Instantiate (Resources.Load("prefabs/hotMetalLight") as GameObject,Vector3.zero,Quaternion.identity);
            }
        }
        deltaHeatReductionTime = 0;
        cutter = GameObject.Find("blade");
        gameObject.layer = 15;

        //initialize uv2 only if it is missing

        isHots = true;
        bool isSame = true;
        //mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null)
            isNoMesh = true;

        if (!isNoMesh)
        {
            Mesh mesh = localMesh;// mf.sharedMesh;
            Vector2[] _mesh_uv = mesh.uv;
            Vector2[] _mesh_uv2 = mesh.uv2;
            if (_mesh_uv2.Length != _mesh_uv.Length)//initialize
            {
                _mesh_uv2 = new Vector2[_mesh_uv.Length];
                for (int i = 0; i < _mesh_uv.Length; i++)
                    _mesh_uv2[i] = Vector2.zero;//Vector2.zero;
                mesh.uv2 = _mesh_uv2;//zeroing out all of the uv2s
            }
            else
            {
                for (int j = 0; j < _mesh_uv.Length; j++)
                {
                    if (_mesh_uv[j] != _mesh_uv2[j])
                    {
                        isSame = false;
                        break;
                    }
                }
            }
            if (smoothNormals)
                SmoothNormals();
            if (!isInitialized && !isTestUV2)
                StartCoroutine(InitUVsWhenDoneInitializing());
        }
    
    }

    private IEnumerator InitUVsWhenDoneInitializing()
    {

        while (!BLINDED_AM_ME.MeshCut.isDoneInitializing){
            yield return null;
        }
        //Debug.Log("InitializingUV2 on " + gameObject.name);
        //Mesh mesh = localMesh;// mf.sharedMesh;
        Vector2[] _mesh_uv2 = localMesh.uv2;
        if (localMesh.uv2.Length == localMesh.vertexCount)
        {
            for (int i = 0; i < localMesh.vertexCount; i++)
                _mesh_uv2[i] = Vector2.zero;//Vector2.zero;
            localMesh.uv2 = _mesh_uv2;
        }

    }
    private IEnumerator InitUVsIfSameWhenDoneInitializing(){
        while (!BLINDED_AM_ME.MeshCut.isDoneInitializing){
            yield return null;
        }
        if (mf /*!= null*/)
            InitUVsIfSame();
    }
    private void InitUVsIfSame()
    {
        bool isSame = true;

        //Mesh mesh = localMesh;// mf.sharedMesh;
        Vector2[] _mesh_uv = localMesh.uv;
        Vector2[] _mesh_uv2 = localMesh.uv2;
        if (_mesh_uv2.Length != _mesh_uv.Length)//initialize
        {
            _mesh_uv2 = new Vector2[_mesh_uv.Length];
            for (int i = 0; i < _mesh_uv.Length; i++)
                _mesh_uv2[i] = Vector2.zero;//Vector2.zero;
            localMesh.uv2 = _mesh_uv2;
        }
        else
        {
            for (int j = 0; j < _mesh_uv.Length; j++)
            {
                if (_mesh_uv[j] != _mesh_uv2[j])
                {
                    isSame = false;
                    break;
                }
            }
        }
        if (isSame){
            for (int i = 0; i < localMesh.vertexCount; i++)
                _mesh_uv2[i] = Vector2.zero;//Vector2.zero;
            localMesh.uv2 = _mesh_uv2;
        }

    }

    public void InitUV2() {
        //Mesh mesh = localMesh;
        Vector2[] _mesh_uv = localMesh.uv;
        Vector2[] _mesh_uv2 = localMesh.uv2;
        if (_mesh_uv2.Length != _mesh_uv.Length)
        {
            _mesh_uv2 = new Vector2[_mesh_uv.Length];
        }
        for (int i = 0; i < _mesh_uv.Length; i++)
            _mesh_uv2[i] = Vector2.zero;//Vector2.zero;
        localMesh.uv2 = _mesh_uv2;
    }

    public void InitUV22()
    {
        //Mesh mesh = localMesh;
        Vector2[] _mesh_uv = localMesh.uv;
        Vector2[] _mesh_uv2 = localMesh.uv2;
        if (_mesh_uv2.Length != _mesh_uv.Length)
        {
            _mesh_uv2 = new Vector2[_mesh_uv.Length];
        }
        for (int i = 0; i < _mesh_uv.Length; i++)
            _mesh_uv2[i] = Vector2.zero;//Vector2.zero;
        localMesh.uv2 = _mesh_uv2;
    }
      //commenting out 3/30/2019 because it's so redundant wtf?
    void Awake () { //gets called because meshcut adds this component
        //This function is always called before any Start functions and also just after a prefab is instantiated. 
        //(If a GameObject is inactive during start up Awake is not called until it is made active.)
        //OnEnable();//makes sure unspawned cuttables get the right setup
        //InitUVsIfSame();
        if (!isInitialized)
            if (mf != null)
                StartCoroutine(InitUVsIfSameWhenDoneInitializing());

    }

    void OnEnable() //instead of Awake, might be better
    {
        ren = GetComponent<Renderer>();
        _audioManager = GameObject.Find("Game").GetComponent<AudioManager>();

        //CutSetup();
        //submeshIndex = -1;
        //isHots = false;
        _transform = transform;
        lastHotTouchPoint = Random.onUnitSphere;
        mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            if (!rb.isKinematic)
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (mf != null)
        {
            SetBoundsVolume();
            if (useUV2HeatSignal)
            {
                 if (isTestUV2){
                    //Mesh mesh = localMesh;// mf.sharedMesh;
                    Vector2[] _mesh_uv2 = localMesh.uv2;
                    _mesh_uv2 = new Vector2[_mesh_uv2.Length];
                    for (int i = 0; i < _mesh_uv2.Length; i++)
                        _mesh_uv2[i] = Vector2.one * 1;//Vector2.zero;
                    localMesh.uv2 = _mesh_uv2;
                }
               deltaHeatReductionTime = 0;
                if (isHots)
                    HeatSignalCheck();//sort of initializes it?
            }
            bladeInsertTimer = 0;
            if (!BLINDED_AM_ME.MeshCut.isDoneInitializing)
                StartCoroutine(SelfDestructWhenReady());
            else
                SelfDestruct();
        }
    }

    public void SetBoundsVolume()
    {
        Vector3 size = ren.bounds.size;
        boundsVolume = size.x * size.y * size.z;
        pitch = Mathf.Lerp(3f, .3f, 1 - Mathf.Pow(1 - boundsVolume, 2) / 2f);

        localMesh = mf.mesh;//refresh after a cut
        //Mesh _mesh = localMesh;// mf.sharedMesh;
        Vector3[] _mesh_vertices = localMesh.vertices;
        float longestSegmentLength = 0;// maxBound;
        for (int i = 0; i < 8; i++)
        {
            Vector3 tempAxis = Vector3.right;
            if (i==1) tempAxis = Vector3.up;
            else if (i==2) tempAxis = Vector3.forward;
            else if (i > 2) 
            {
                int rand = Random.Range(0, _mesh_vertices.Length);//min inclusive max exclusive
                int rand1 = Random.Range(0, _mesh_vertices.Length);
                if (rand < _mesh_vertices.Length && rand1 < _mesh_vertices.Length)
                    tempAxis = _mesh_vertices[rand] - _mesh_vertices[rand1];//still getting an out of bounds error
                else
                    if (isDebug) Debug.Log("oooooooooooooooooooooooooooooOut Of Range Exception Avoided ;)");
            }
            //i got an out of range error so I'm subtracting 1 to play it safe even though random.range with ints is supposed to be exclusive
            //still gets out of range exceptions... think this through...04/12/2019
            float temp = tempAxis.sqrMagnitude;

            if (temp > longestSegmentLength)
            {
                longestSegmentLength = temp;
                //tempAxis = FastNormalize(ref tempAxis);
                longAxis = tempAxis.normalized;
            }
        }

    }

    public float GetBoundsVolume(){
        Vector3 size = ren.bounds.size;//just hacking around because I don't know why the column takes so long to break up
        boundsVolume = size.x * size.y * size.z;
        return boundsVolume;
    }

    void OnCollisionEnter(Collision collision){

        if (collision.relativeVelocity.magnitude > minCollisionVelocity)
        {
            float volume = Mathf.Lerp(.005f,1,Mathf.Pow((collision.relativeVelocity.magnitude - minCollisionVelocity)/10f,.5f));
            _audioManager.PlayMetalClip(gameObject,collision.contacts[0].point,volume,pitch);//udioSource.Play();
        } 
        else 
        {
            JointConnection jc = collision.gameObject.GetComponent<JointConnection> ();
            if (jc != null){
                if (jc.joint != null)
                    if (jc.joint.gameObject.GetComponent<SteamVR_TrackedObject>() != null)
                        _audioManager.PlayMetalClip(gameObject,collision.contacts[0].point,.5f,pitch );//udioSource.Play();
            }
        }
    }

    public float GetKillTime() {
        return killTime;
    }

    public void CutSetup() {//gets run by MeshCut

        //leverage off of what this script is already doing to spawn hotMetalLights
        //spawn a light and tell it the duration and hopefully the light will know what intensity/color to use and for how long or it will ping the material over and over
        //it will have to ping the material/gameObject/submesh from time to time just to know if it disappeared, and to follow its transforms
        //the lights will link to the gameObject / submesh / material
        //manage lights in the cuttable class using a static array
        _transform = transform;//refresh transform
        mats = mr.sharedMaterials;
        isHots = true;
        localMesh = mf.mesh;//refresh the mesh after a cut

        if (submeshIndex > localMesh.subMeshCount)//trying to catch an out of bounds error
            submeshIndex = localMesh.subMeshCount - 1;//or set to -1?
        if (submeshIndex > -1 && submeshIndex < localMesh.subMeshCount) {//out of bounds exception subMeshIndex
            Vector3[] _meshVertices = localMesh.vertices;
            int[] indices = localMesh.GetTriangles(submeshIndex);
            if (indices != null && indices.Length > 0)
            {   
                if (_meshVertices.Length >= indices[0])
                    hotLightPos = _meshVertices[indices[0]];
                if (mats[submeshIndex].shader.name == "Shimmy/saberCut")
                {
                    hotLightSignal = 1;
                    hotMetalLights[hotLightIndex++ % numHotLights].GetComponent<HotLightFollowObject>().FollowObject(gameObject);
                }
            }
        }
        SetBoundsVolume ();
        deltaHeatReductionTime = 0;
        HeatSignalCheck();//sort of initializes it?
        bladeInsertTimer = 0;
        //if (_transform.localScale != _transform.lossyScale)
        //    Debug.Log("localScale = " + _transform.localScale.ToString() + " lossyScale = " + _transform.lossyScale.ToString());
        invertedScale = 1/((_transform.lossyScale.x + _transform.lossyScale.y + _transform.lossyScale.z)/3);//lossyScale seems more accurate and traverses the hierarchy
        isInitialized = true;
    }

    void CheckCollapsibleHotMaterials()
    {
        float _timeSinceLevelLoad = Time.timeSinceLevelLoad;
        mats = gameObject.GetComponent<MeshRenderer> ().materials;
        isHots = false;
        if (mr != null){
            for (int i = 0; i < mats.Length; i++) {
                if (mats [i].shader.name == "Shimmy/saberCut" && mats [i] != cutMat) { //if not the default non-instance material
                    if (mats [i].GetFloat ("_HeatStart") < _timeSinceLevelLoad - mats [i].GetFloat ("_HeatInterval")) {
                        mats[i] = cutMat;
                        mr.materials = mats;
                    } else {
                        if (mr.sharedMaterials[i] != null){
                            mats [i] = mr.sharedMaterials [i];
                            mr.materials = mats;
                            isHots = true;
                        }
                    }
                }//not cutMat
            }//for
        }
    }

    void HeatSignalCheck()
    {
        //Mesh _mesh = localMesh;// mf.sharedMesh;
        //if (_mesh == null)
        //    return;//hacky solution to an error
        isHots = false;

        float heatReduction = deltaHeatReductionTime/hotMetalHeatInterval;//1 - (hotMetalHeatInterval - deltaHeatReductionTime) /hotMetalHeatInterval;
        //heat should be 
        Vector2[] _mesh_uv2 = localMesh.uv2;

        if (_mesh_uv2 != null)
            deltaHeatReductionTime = 0;
            if (_mesh_uv2.Length > 0)
            {
                int[] indices;
                isHots = false;

                for (int sub=0; sub < localMesh.subMeshCount; sub++)
                {
                    indices = localMesh.GetTriangles(sub);// GetCachedTriangles(ref _mesh, sub);// 
                    if (indices.Length > 0){
                        for(int i=0; i<indices.Length; i++)
                        {
                            if (_mesh_uv2[indices[i]].x > 0)
                            {
                                _mesh_uv2[indices[i]].x -= heatReduction;//new Vector2(heatRatio,heatRatio);
                                if (!isHots)
                                    isHots = true;
                            }
                        }
                    }
                }
                //try to loop by submesh if possible then only modify submesh that has uv.x values above 0
                //calculate the new value for the first vertex of a submesh and then use that same value across all uv coordinates.
            }
        //mesh.uv2 = mesh_uv2;//replace all uvs in one shot, hopefully this saves time
        //gameObject.GetComponent<MeshFilter> ().mesh.uv2 = _mesh_uv2;//maybe this is the way to do it
        localMesh.uv2 = _mesh_uv2;
        if (isHots)
            hotLightSignal -= heatReduction;
    }


    void SmoothNormals()
    {
        //Mesh mesh = localMesh;// mf.sharedMesh;
        Vector3[] mesh_normals = localMesh.normals;
        //Vector2[] mesh_uv2 = mesh.uv2;

        //float angle = 90;
        float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

        Vector3[] _meshVertices = localMesh.vertices;
        Vector3[][] triNormals = new Vector3[localMesh.subMeshCount][];
        Vector3[] newNormals = localMesh.normals;
        Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>() ;//for each vertexKey make a list of normals
        for (int subMeshIndex = 0; subMeshIndex < localMesh.subMeshCount; subMeshIndex++) 
        {
            int[] triangles = localMesh.GetTriangles(subMeshIndex);
            triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

            for (int i=0; i<triangles.Length; i+=3)
            {//accumulate face Normals
                int i1 = triangles[i];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];
                //Vector3 displaceVec = burnNormal * -1 * mesh_uv2[i].y;

                //if (goodArray[i1]>0 || goodArray[i2]>0 || goodArray[i3]>0)
                {
                // Calculate the normal of the triangle
                    Vector3 p1;
                    Vector3 p2;
                    // if (dontDisplace==1 && mesh_uv2[i1].y > 0 && mesh_uv2[i1].y < 1
                    //                     && mesh_uv2[i2].y > 0 && mesh_uv2[i2].y < 1
                    //                     && mesh_uv2[i3].y > 0 && mesh_uv2[i3].y < 1
                    //     )
                    // {//if dontDisplace the new face normal will get calculated based on the displaced vertex positions
                    //     p1 = (_meshVertices[i2] + burnNormal * -1 * mesh_uv2[i2].y * .01f * invertedScale) - (_meshVertices[i1] + burnNormal * -1 * mesh_uv2[i1].y * .01f * invertedScale);
                    //     p2 = (_meshVertices[i3] + burnNormal * -1 * mesh_uv2[i3].y * .01f * invertedScale) - (_meshVertices[i1] + burnNormal * -1 * mesh_uv2[i1].y * .01f * invertedScale);
                    // }
                    // else
                    // {
                        p1 = _meshVertices[i2] - _meshVertices[i1];
                        p2 = _meshVertices[i3] - _meshVertices[i1];
                    // }
                    Vector3 faceNormal = Vector3.Cross(p1, p2).normalized;//pretty sure this needs to be normalized, and that fastNormalize is wonky
                    //faceNormal = FastNormalize(ref faceNormal);//I think this caused a weird flashy artifact on already-cooled displaced geo
                    int triIndex = i / 3;
                    triNormals[subMeshIndex][triIndex] = faceNormal;//index was out of bounds of the array
                    newNormals[i1] = faceNormal;
                    newNormals[i2] = faceNormal;
                    newNormals[i3] = faceNormal;

                    List<VertexEntry> entry;
                    VertexKey key;

                    if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i1]), out entry)) 
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                    if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i2]), out entry)) 
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                    if (!dictionary.TryGetValue(key = new VertexKey(_meshVertices[i3]), out entry)) 
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                }
            }
        }//submesh

        foreach (var vertList in dictionary.Values) 
        {
            for (var i = 0; i < vertList.Count; ++i) 
            {

                var sum = new Vector3();
                var lhsEntry = vertList[i];
                int normalsCount = 0;

                //if (goodArray[lhsEntry.VertexIndex]>0)//only reacalculating in the range of the burn, optimization 8/30/2019
                {
                    for (var j = 0; j < vertList.Count; ++j) 
                    {
                        var rhsEntry = vertList[j];
     
                        if (lhsEntry.VertexIndex == rhsEntry.VertexIndex) 
                        {
                            Vector3AddEquals(ref sum,triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                            normalsCount++;
                            //sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                        } 
                        else 
                        {
                            // The dot product is the cosine of the angle between the two triangles.
                            // A larger cosine means a smaller angle.
                            var dot = Vector3.Dot(
                            triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                            triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                            if (dot >= cosineThreshold) 
                            {
                                Vector3AddEquals(ref sum,triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                normalsCount++;
                                //sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            }
                        }
                    }
                    //if (mesh_uv2[lhsEntry.VertexIndex].y < 1 && mesh_uv2[lhsEntry.VertexIndex].y > 0){//dks uncommented 8/30/2019
                        //sum /= normalsCount;//trying to get fastNormalize to work - not helping
                        //sum = FastNormalize(ref sum);//doesn't displace, fast normalize must not work well with over inflated vectors
                        mesh_normals[lhsEntry.VertexIndex] = sum.normalized;//newNormals[lhsEntry.VertexIndex];//
                        //mesh_normals[lhsEntry.VertexIndex] = new Vector3(sum.x/normalsCount, sum.y/normalsCount, sum.z/normalsCount);//doesn't displace when I use this line
                    //}
                }
            }
        }
        localMesh.normals = mesh_normals;
    }

    //make an interface or abstract class to serve 2 purposes
    // 1- indicate that the object is cuttable and can be interacted with by the meshcut class
    // 2- give some kind of hook to setup the cuttable material, preferably in the Start() event
    // So CuttableMetal should still be able to be found as a "Cuttable" and 
    // Update is called once per frame
    void Update () {
        _deltaTime = Time.deltaTime;
        //if (!skipUpdate)
        {
            float heatCheckTimeMultiplier = 1;
            if (_mainCameraTransform == null)//happens a lot I guess
            {
                _mainCameraTransform = Camera.main.transform;
                //Debug.Log("Fixing missing maincamera transform");
            }
            if (useUV2HeatSignal)
            {
                Vector3 directionFromCamera = _transform.position - _mainCameraTransform.position;
                directionFromCamera = FastNormalize(ref directionFromCamera);
                //float facingRatio = Mathf.Max(Vector3.Dot(directionFromCamera, _mainCameraTransform.forward),0);//0 to 1 signal
                //heatCheckTimeMultiplier = ((1-facingRatio)*10 + 1);//slows down heat check when not in view of the main camera
                float facingRatio = Vector3.Dot(directionFromCamera, _mainCameraTransform.forward) + 1;//0 to 2
                heatCheckTimeMultiplier = ((2 - facingRatio) * 10 + 1);//slows down heat check when not in view of the main camera
            }

            if (mf /*!= null*/) //meshfilter
            {
                if (bladeInsertTimer > bladeInsertTime)
                {
                    bladeInsertTimer = 0;
                    //not sure if setting a limit is necessary, trying to use the same limit as ToolUser
                    minDestructVolume = boundsVolume * .25f; // minBound * .6f;//changed boundVolume to minBound
                }
                if (useUV2HeatSignal  && !isTestUV2)
                {
                    if (isHots)
                        if (deltaHeatReductionTime >= hotMetalCoolingInterval * heatCheckTimeMultiplier)
                            HeatSignalCheck();
                }
                if (isHots)
                {
                    if (!useUV2HeatSignal)
                    {
                        timer += Time.realtimeSinceStartup - previousCheckTime;//
                        if (timer > hotCheckInterval)
                        {
                            timer = 0;
                            CheckCollapsibleHotMaterials();
                            previousCheckTime = Time.realtimeSinceStartup;
                        }
                    }
                    deltaHeatReductionTime += _deltaTime;//Time.deltaTime;
                }

                if (!cutter /*== null*/)
                    cutter = GameObject.Find("blade");

                if (bladeInserted)
                {
                    bladeInsertTimer += _deltaTime;//Time.deltaTime;
                    bladeInserted = false;
                    if (!BLINDED_AM_ME.MeshCut.isDoneInitializing)
                        StartCoroutine(SelfDestructWhenReady());
                    else
                        SelfDestruct();
                }
                else
                {
                    bladeInsertTimer = 0;
                }
            }
        }
        skipUpdate = false;
    }

    void LateUpdate(){

    }

    private IEnumerator SelfDestructWhenReady() {
        while (!BLINDED_AM_ME.MeshCut.isDoneInitializing) {
            yield return null;
        }
        SelfDestruct();
    }

    private void SelfDestruct()
    {
        if (minDestructVolume > .001)
        {
            if (boundsVolume > minDestructVolume)
            {//changed boundVolume to minBound which is only one dimension
                //isSelfDestructing = true;

                //Debug.Log("attempting self-destruct on " + gameObject.name);
                if (cutter == null)
                    cutter = BLINDED_AM_ME.MeshCut.GetBladeGameObject(); //GameObject.Find("blade");
                cutter.GetComponent<ToolUser>().SelfDestruct(gameObject, longAxis, lastHotTouchPoint);
            }
            else
            {
                minDestructVolume = 0;//disable after successful destruct
            }
        }
    }

    public void SetBladeTriggerCondition(){ //let external forces act on the object
        //bladeInserted = true;
    }

    public void SetBladeTriggerCondition(Vector3 touch)
    { //let external forces act on the object
        //SetHotTouchPoint(touch);
        //bladeInserted = true;
    }

}

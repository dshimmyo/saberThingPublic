
//    MIT License
//    
//    Copyright (c) 2017 Dustin Whirle
//    
//    My Youtube stuff: https://www.youtube.com/playlist?list=PL-sp8pM7xzbVls1NovXqwgfBQiwhTA_Ya
//    
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//    
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//    
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Unity.Jobs;//job system
using UnityEngine.Jobs;//job system
using Unity.Collections;//job system
using Unity.Burst;

using System.Text;

namespace BLINDED_AM_ME{

	public class MeshCut{

        private static Mesh_Maker _leftSide = new Mesh_Maker();
		private static Mesh_Maker _rightSide = new Mesh_Maker();

        //private static Mesh_Maker_Old _leftSide = new Mesh_Maker_Old();
        //private static Mesh_Maker_Old _rightSide = new Mesh_Maker_Old();

        private static Plane _blade;
        private static GameObject _bladeObject;
		private static Mesh  _victim_mesh;
        private static Vector3[] _victim_mesh_vertices;
        private static Vector3[] _victim_mesh_normals;
        private static Vector2[] _victim_mesh_uv;
        private static Vector2[] _victim_mesh_uv2;
        private static Vector4[] _victim_mesh_tangents;

		// capping stuff
		private static List<Vector3> _new_vertices = new List<Vector3>();

		private static int _capMatSub = 1;
		private static int cutCount = 0;

		private static GameObject currentVictim=null; //for resuming an interrupted cut
		private static int subStartIndex = 0;
		private static int triPointsStartIndex =0;
		//private static int totalLeft=0;//stored value
		//private static int totalRight=0;//stored value
        private static bool isCutFinished = false;
        private static float asyncMultiplier = .25f;//06/02/2019 changed .5 to .25//was .8 for a long time, trying .6 then .5, .25 was okay, not sure why it seemed to cause more hiccups
        private static float asyncTime = .005f;
        private static Dictionary<string,SaberCutPoint> cutPoints = new Dictionary<string,SaberCutPoint>();
        private static GameObject emptyCutGO = new GameObject ("empty cut"); //preallocating, only works if you never destroy it
        private static GameObject[] nullCuts = new GameObject[]{ null, null };
        private static Transform _victimTransform;
        private static Transform _rightSideObjTransform;
        public  static bool isDebug = false;
        //private static float timeSinceLoad=0;
        private static Material hotSabercutMat;
        private static Material hotSabercutMatAlt;
        private static List<int> collapsibleSubmeshes = new List<int>();
        private static float stillHotTime = 10f;//6 seconds?
        private static GameObject[] cutReturn = new GameObject[2];//not exactly sure why this works, maybe the array doesn't matter, it is just a container for the return elements
        private static GameObject[] emptyCutsReturn = new GameObject[] { new GameObject("empty cut"), new GameObject("empty cut") };
        private static GameObject[] chunks;// = new GameObject[1]; //preallocating 200 cut chunks
        private static GameObject[] chunksSwap;// for quicker sorting

        private static int chunksPoolSize = 1;//64;//trying powers of 2
        private static GameObject rightSideObj;
        private static GameObject leftSideObj;
        private static Material[] matsRefresh = new Material[0];
        private static Cuttable cuttable;
       	private static long numRenamed = 0;
       	private static bool[] cutSides = null;
        private static int [] cutSidesInt = null;
        private static bool _useJobSystem = true;//false;
        private static bool useNewUV2Metal = true;
        private static int chunksBufferSize = 10;
        private static int chunkRenameNum = 0;
        public static int numMeshCutInstances = 0;
        private static StringBuilder sb = new StringBuilder(10,20);//using stringbuilder to speed up my cut point caching algo
        private static StringBuilder sbLeftSideUniqueName = new StringBuilder(16);//using stringbuilder to speed up my cut point caching algo
        private static float _fixedDeltaTime = Time.fixedDeltaTime;
        private static int CHAR_0 = (int)'0';//const being used for stringbuilder
        //private static int sidednessLoopIndex = 0;
        public static bool isDoneInitializing = false;
        private static int numActiveChunksPreInit = 0;
        //public static JobHandle sidednessJobHandle;
        public static string status = "initializing";
        public static string currentVictimName = "";
        public enum CappingStyles { Orig, OrigTweak, SkipColinear, SkipColinearTweak };
        private static CappingStyles cappingStyle = CappingStyles.SkipColinear;
        private static bool isTestBisectedPoints = true;
        private static float bisectTolerance = 0.0000001f;
        public static string saberCutShaderName = "Shader Graphs/saberCutPBRSG";//"Shimmyo/saberCutLWRP";//"Shimmyo/saberCut";//should be saberCutLWRP

        public static GameObject GetBladeGameObject()
        {
            if (!_bladeObject)
            {
                _bladeObject = GameObject.Find("blade");
            }

            return _bladeObject;
        }

        public static void SetCappingStyle(CappingStyles cs)
        {
            cappingStyle = cs;
        }

        public static CappingStyles GetCappingStyle()
        {
            return cappingStyle;
        }

        public MeshCut (int tempChunkPoolSize, int tempBufferSize, bool tempIsDebug){//use this constructor to create the whole framework for cutting, and maybe it will help keep this thing active
            isDebug = tempIsDebug;
            if (tempChunkPoolSize < tempBufferSize)
                tempChunkPoolSize += tempBufferSize;
            chunksBufferSize = tempBufferSize;
            chunksPoolSize = tempChunkPoolSize;
            
            chunks = new GameObject[chunksPoolSize];
            chunksSwap = new GameObject[chunksPoolSize];
            //MonoBehaviour.StartCoroutine(MeshCutInitialize());

            /*
            bool result;
            for(int i=0; i<chunksPoolSize; i++){
                chunks[i] = new GameObject("right side prealloc" + i.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
                result =  chunks[i].AddComponent<Rigidbody> ();
                chunks[i].SetActive(false);
                result = chunks[i].AddComponent<MeshCutChunk>() as MeshCutChunk;
            }*/
        }

        public IEnumerator MeshCutInitialize()
        {
            if (!isDoneInitializing)//game restart
            {
                numActiveChunksPreInit = 0;
                for (int i = 0; i < chunksPoolSize; i++)
                {
                    MonoBehaviour.Destroy(chunks[i]);
                }
            }

            bool result;
            for (int i = 0; i < chunksPoolSize; i++)
            {
                chunks[i] = new GameObject("right side prealloc" + i.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
                result = chunks[i].AddComponent<Rigidbody>();
                chunks[i].SetActive(false);
                result = chunks[i].AddComponent<MeshCutChunk>() as MeshCutChunk;
                numActiveChunksPreInit++;
                yield return null;
            }
            isDoneInitializing = true;

        }

        public static GameObject MeshCutRepairChunk(int chunkIndex)
        {
            Debug.Log("repairing missing MeshCut chunk");
            bool result;

            chunks[chunkIndex] = new GameObject("right side prealloc" + chunkIndex.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            result = chunks[chunkIndex].AddComponent<Rigidbody>();
            chunks[chunkIndex].SetActive(false);
            result = chunks[chunkIndex].AddComponent<MeshCutChunk>() as MeshCutChunk;
            return chunks[chunkIndex];
        }

        public static void UpdateNumMeshCutInstances(MeshCutInstance mci)
        {
            numMeshCutInstances = mci.GetNumMeshCutInstances();
        }

        //The managed class type `UnityEngine.Plane*` is not supported. 
        //Loading from a static field `UnityEngine.Plane BLINDED_AM_ME.MeshCut::_blade` is not supported by burst
        /*public struct GetSidednessJob : IJobParallelFor {
            [ReadOnly]
            public NativeArray<Vector3> Vertices;
            [WriteOnly]
            public NativeArray<int> Sides;
            
            public void Execute (int index) 
            {
                if (_blade.GetSide (Vertices [index]))
                    Sides [index] = 1;
                else 
                    Sides [index] = 0;
            }
        }*/

        [BurstCompile(FloatPrecision.High,FloatMode.Default,CompileSynchronously = false)]//FloatPrecision.Low, FloatMode. Default,Strict,Deterministic,Fast
        public struct NewGetSidednessJob : IJobParallelFor {
            [ReadOnly]
            public NativeArray<Vector3> Vertices;
            [WriteOnly]
            public NativeArray<int> Sides;
            [ReadOnly]
            //public NativeArray<Vector3> PlaneInfo;//normal, point
            public Vector3 BladeNormal;
            public Vector3 PointOnPlane;
            public Vector3 PointOnPlaneB;
            public float BisectTolerance;
            public void Execute (int index) 
            {
                //get sign of normal dot (p1 - p2) where p1 is the vert and p2 is any point on the plane
                //if (Vector3.Dot(PlaneInfo[0],(Vertices[index] - PlaneInfo[1])) >= 0)

                Vector3 myPointOnPlane = PointOnPlane;
                if ((Vertices[index] - myPointOnPlane).sqrMagnitude < .00001 ) //just in case the points are too close
                {
                    myPointOnPlane = PointOnPlaneB;
                }
                float dot = Vector3.Dot(BladeNormal,(Vertices[index] - myPointOnPlane) );//the second ray doesn't need to be normalized because the projection should still approach zero at any magnitude, unless the second ray is very small
                if (dot > 0)//theoretically >= is more expensive than >
               
                //if (_blade.GetSide (Vertices [index]))
                    Sides [index] = 1;
                else if (dot < BisectTolerance && dot > -BisectTolerance)
                    Sides [index] = -1;//this point should be sent to both sides
                else
                    Sides [index] = 0;

                
            }
        }

        private static void ExecuteGetSidenessJobs(Vector3[] vertices, int[] sides, Vector3 pointOnPlane, Vector3 pointOnPlaneB)
            {

                NativeArray<Vector3> vertexArray = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob);
                NativeArray<int> sidesArray = new NativeArray<int>(sides.Length,Allocator.TempJob);
                //NativeArray<Vector3> planeData = new NativeArray<Vector3>(2,Allocator.TempJob);

                vertexArray.CopyFrom(vertices);
                sidesArray.CopyFrom(sides);
                //planeData[0] = _blade.normal;//new stuff plane normal
                //planeData[1] = pointOnPlane; //new stuff plane point

                NewGetSidednessJob job = new NewGetSidednessJob()//new stuff
                {
                    Vertices = vertexArray,
                    Sides = sidesArray,
                    //PlaneInfo = planeData
                    BladeNormal = _blade.normal,
                    PointOnPlane = pointOnPlane,
                    PointOnPlaneB = pointOnPlaneB,
                    BisectTolerance = bisectTolerance

                };
                JobHandle sidednessJobHandle = job.Schedule(vertices.Length, 8);//

                sidednessJobHandle.Complete();
                //vertexArray.CopyTo(vertices);
                sidesArray.CopyTo(sides);
                vertexArray.Dispose();
                sidesArray.Dispose();
                //planeData.Dispose();
            }

        public static int GetChunksPoolSize(){
            return chunksPoolSize;
        }

        public static int GetTriPointsStartIndex()
        {
            return triPointsStartIndex;
        }

        public static string GetCutInfo() {
            string info = "";

            info = currentVictimName;
            info += "\n" + triPointsStartIndex + " triPointsIndex";
            info += "\n" + cutPoints.Count + " cutPoints\n";
            info += status;
            
            return info;
        }

        public static int GetNumActiveChunks(){
            if (!isDoneInitializing)
            {
                int len = 0;
                if (chunks != null)
                {
                    len = chunks.Length;
                }
                return -(len - numActiveChunksPreInit);
            }

            int count = 0;
            for (int i = 0; i < chunks.Length; i++) {
                if (chunks[i] != null)
                {
                    if (chunks[i].activeInHierarchy)
                        count++;
                }
                else
                {
                    if (isDebug)
                        Debug.Log("replacing missing chunk!");
                    chunks[i] = new GameObject("right side prealloc" + i.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
                    bool result = chunks[i].AddComponent<Rigidbody>();
                    chunks[i].SetActive(false);
                    result = chunks[i].AddComponent<MeshCutChunk>() as MeshCutChunk;
                }
            }
            return count;
        }

        public static string SBIntsToString(int[] myInt, int myPadding)
        {
            sb.Length = 0;
            //Debug.Log("SBIntsToString :" + myInt[0].ToString() + " + " + myInt[1].ToString());
            for (int k=0;k<myInt.Length;k++)
            {
                if (myInt[k]==0)
                {
                    for (int l=0;l<myPadding;l++)
                        sb.Append ((char)(CHAR_0));
                }
                else
                {
                    int log = (int)System.Math.Floor(System.Math.Log10(myInt[k]));
                    for (int j=log+1; j<myPadding; j++)
                    {
                        sb.Append ((char)(CHAR_0));
                    }
                    for (int i = log; i >= 0; i--)
                    {
                        int pow = (int)System.Math.Pow(10,i);
                        int digit = (myInt[k] / pow) % 10;
                        sb.Append ((char)(digit + CHAR_0));
                    }
                }
            }
            string returnString = sb.ToString();
            //Debug.Log("SBIntsToString :" + returnString);
            return returnString;//try GatValue as string;
        }

        public static string SBLeftSideObjName(int myInt)
        { //something is wrong with this. it's too slow. saw 27 calls to append which took up20ms
            int myPadding = 7;
            if (sbLeftSideUniqueName.Length > 10)
            {
                sbLeftSideUniqueName.Length = (int)10;
            }
            else 
            {
                sbLeftSideUniqueName.Length = 0;
                sbLeftSideUniqueName.Append("MeshCutObj");
            }
            bool isRandom = false;
            //int recursion = 0;
            if (myInt < 1)
            {
                if (isDebug)
                    Debug.Log("*** SBLeftSideObjName() random");
                sbLeftSideUniqueName.Append("Random");
                myInt = chunkRenameNum++ % 1000000;//Mathf.FloorToInt(Random.RandomRange(100000, 999999));
                isRandom = true;
            }
            int log = (int)System.Math.Floor(System.Math.Log10(myInt));

            if (myInt == 0)
            {
                for (int l=0;l<myPadding;l++)
                    sbLeftSideUniqueName.Append ((char)(CHAR_0));
            }
            else
            {
                for (int j = log + 1; j < myPadding; j++)
                {
                    sbLeftSideUniqueName.Append((char)(CHAR_0));//ArgumentOutOfRangeException: capacity was less than the current size.
                }
            }
            for (int i = log; i >= 0; i--)
            {
                int pow = (int)System.Math.Pow(10, i);
                int digit = (myInt / pow) % 10;
                sbLeftSideUniqueName.Append((char)(digit + CHAR_0));
            }
            string returnString = sbLeftSideUniqueName.ToString();
            if (isRandom)
            {
                bool clashCheck = GameObject.Find(returnString);
                //Debug.Log("*** SBLeftSideObjName(): " + returnString);
                //Debug.Log("*** Length: " + sbLeftSideUniqueName.Length.ToString());
                //Debug.Log("*** Capacity: " + sbLeftSideUniqueName.Capacity.ToString());

                while (clashCheck)
                {
                    returnString = returnString + "X";
                    clashCheck = GameObject.Find(returnString);
                    //Debug.Log("clash check:" + returnString);
                }
            }
            //if (GameObject.Find(returnString))
            //{
            //    Debug.Log("*** SBLeftSideObjName() recursion" + recursionDepth.ToString());
                //Debug.Break();
            //    return SBLeftSideObjName(0, recursionDepth + 1);//since there was a clash this will trigger a random number
            //}
            //else
            //{
                return returnString;//try GatValue as string;
            //}
        }


        //***************************DKS UPDATES HERE***************************//
        public static GameObject[] Cut(GameObject victim, Vector3 A, Vector3 B, Vector3 C, Material capMaterial) { //overloaded for 3 point plane
            if (!isDoneInitializing)
                return nullCuts;
            //i.e. create a plane in local space to the victim
            if (isDebug)
                Debug.Log("Cutting:" + victim.name);
            float cutStartTime = Time.realtimeSinceStartup;//make sure the cut never goes beyond some division of Time.fixedDeltaTime
            asyncTime = _fixedDeltaTime * asyncMultiplier;
            if (victim == null)
                return emptyCutsReturn;//just in case
            if (currentVictim != victim || currentVictim == null) { //a new cut is about to happen so initialize what needs to be initialized
                if (isDebug) status = "starting new cut ";
                victim.GetComponent<MeshFilter>().mesh.MarkDynamic();
                currentVictim = victim;
                currentVictimName = currentVictim.name;

                subStartIndex = 0;
                triPointsStartIndex = 0;
                // _blade = new Plane(victim.transform.InverseTransformPoint(A),
                // victim.transform.InverseTransformPoint(B),
                // victim.transform.InverseTransformPoint(C)); //negative normal must make the left side and right side match up "correctly"
                _blade = new Plane(A, B, C);

                //changed -normalDirection to normalDirection because it's a waste of computing time
                // get the victims mesh
                MeshFilter vmf = victim.GetComponent<MeshFilter>();

                if (vmf == null) {//I don't think this is working. it was meant to deal with the derrick character. work in progress
                    currentVictim = null;
                    if (isDebug)
                        Debug.Log("No Mesh to Cut!");
                    //return new GameObject[]{ victim, new GameObject ("empty cut") };
                    //emptyCutsReturn[0] = emptyCutGO;//was victim
                    //emptyCutsReturn[1] = emptyCutGO;
                    return emptyCutsReturn;
                    //return new GameObject[]{ victim, emptyCutGO };
                }
                cutReturn[0] = null;
                cutReturn[1] = null;
                _victim_mesh = vmf.mesh;
                _victim_mesh_vertices = _victim_mesh.vertices;
                _victim_mesh_normals = _victim_mesh.normals;
                _victim_mesh_uv = _victim_mesh.uv;
                _victim_mesh_uv2 = _victim_mesh.uv2;
                if (_victim_mesh_uv.Length < _victim_mesh_vertices.Length) {
                    _victim_mesh_uv = new Vector2[_victim_mesh_vertices.Length];
                    for (int i = 0; i < _victim_mesh_vertices.Length; i++)
                        _victim_mesh_uv[i] = Vector2.zero;
                }
                if (_victim_mesh_uv2.Length < _victim_mesh_uv.Length)
                {
                    _victim_mesh_uv2 = new Vector2[_victim_mesh_uv.Length];
                    for (int i = 0; i < _victim_mesh_uv.Length; i++)
                        _victim_mesh_uv2[i] = Vector2.zero;
                }
                _victim_mesh_tangents = _victim_mesh.tangents;
                if (_victim_mesh_tangents.Length < _victim_mesh_uv.Length)
                {
                    _victim_mesh_tangents = new Vector4[_victim_mesh_uv.Length];
                    for (int i = 0; i < _victim_mesh_uv.Length; i++)
                        _victim_mesh_tangents[i] = Vector2.zero;
                }
                rightSideObj = null;
                leftSideObj = null;
                // reset values
                _new_vertices.Clear();

                    _leftSide = new Mesh_Maker(_victim_mesh_vertices.Length);//this should be preallocated maybe and simply use a clear method
                    _rightSide = new Mesh_Maker(_victim_mesh_vertices.Length);
                    //_leftSide.ReinitArrays(_victim_mesh_vertices.Length);//changed from resizearrays
                    //_rightSide.ReinitArrays(_victim_mesh_vertices.Length);//changed from resizearrays

                    //_leftSide = new Mesh_Maker_Old();//this should be preallocated maybe and simply use a clear method
                    //_rightSide = new Mesh_Maker_Old();


                //totalLeft=0;//stored value
                //totalRight=0;//stored value
                isCutFinished = false;
                cutPoints.Clear();
                _victimTransform = victim.transform;
                _capMatSub = 1;
                cuttable = victim.GetComponent<Cuttable>();
                if (cuttable.GetTessellate())
                    {
                        if (isDebug) Debug.Log("Cuttable Victim found to be Tessellatable! Bad!");
                        cuttable.SetTessellate(false);
                        Debug.Break();
                    }
                if (cutSides == null || _victim_mesh.vertexCount > cutSides.Length)
                    cutSides = new bool[_victim_mesh.vertexCount];

                if (cutSidesInt == null || _victim_mesh.vertexCount > cutSidesInt.Length)
                    cutSidesInt = new int[_victim_mesh.vertexCount];

                if (_useJobSystem)
                {
                    //sidednessLoopIndex = 0;
                    //int[] cutSidesInt = new int[cutSides.Length];
                    ExecuteGetSidenessJobs(_victim_mesh_vertices, cutSidesInt, A,B);//_blade.ClosestPointOnPlane(A)//A B & C already one the plain genius
                    for (int i = 0; i < cutSidesInt.Length; i++)
                    {
                        cutSides[i] = (cutSidesInt[i] == 1) ? true : false;
                        //if (cutSidesInt[i] == -1)
                        //{
                        //    Debug.Log("A point was bisected by the blade!");
                        //}
                    }
                } else {
                    //sidednessLoopIndex = -1;//tells the script not to pay attention to the sidedness loop thingie
                    for (int i = 0; i < _victim_mesh.vertexCount; i++) {//caching this prevents the double look-up later and may have other advantages
                        cutSides[i] = _blade.GetSide(_victim_mesh_vertices[i]);
                    }
                }
            }

            /*
            if (Time.realtimeSinceStartup - cutStartTime > asyncTime)
            {
                if (isDebug)
                    Debug.Log("exiting Meshcut right after Init...");
                // if (sidednessJobHandle.IsCompleted)
                //     return nullCuts;
                // else
                //     Debug.Log("jobHandle wasn't completed");
            }
            */
            if (isDebug) status = "sidedness";

            
            if (cuttable != null)
                if (cuttable.cutMat != null)
                    capMaterial = cuttable.cutMat;

            if (!isCutFinished) {
                if (isDebug) status = "cutting...";

                if (_victim_mesh.triangles.Length < 4 || currentVictim == null) {
                    currentVictim = null;
                    if (isDebug)
                        Debug.Log("No Mesh to Cut!");
                    //cutReturn[0] = emptyCutGO;
                    //cutReturn[1] = emptyCutGO;
                    return emptyCutsReturn;
                }
                bool[] sides = new bool[3];
                int[] indices;
                int p1, p2, p3;
                if (isDebug) status = "cutting1...";

                // separate points left and right for each submesh
                for (int sub = subStartIndex; sub < _victim_mesh.subMeshCount; sub++) {
                    if (isDebug) status = "cuttingsub " + sub.ToString();

                    indices = _victim_mesh.GetTriangles(sub);
                    if (sub > subStartIndex)
                        triPointsStartIndex = 0;

                    for (int i = triPointsStartIndex; i < indices.Length; i += 3) {
                        if (isDebug) status = "cuttingsub " + sub.ToString() + " tri " + i.ToString();

                        if (Time.realtimeSinceStartup - cutStartTime > asyncTime && i > triPointsStartIndex+3) {
                            subStartIndex = sub;
                            triPointsStartIndex = i;
                            if (isDebug)
                                Debug.Log("exited Cut()");
                            return nullCuts;//new GameObject[]{ null, null };
                        }

                        p1 = indices[i];
                        p2 = indices[i + 1];
                        p3 = indices[i + 2];
                        if (p1 >= cutSides.Length || p2 >= cutSides.Length || p3 >= cutSides.Length)
                        {
                            currentVictim = null;//fixed a long-standing bug
                            if (isDebug) Debug.Log("xxxxxxxxxxxxOut Of Bounds Exception Avoided ");
                            return emptyCutsReturn;//nullCuts;//empty is bad, null is continue 
                        }
                        if (isTestBisectedPoints)
                        {
                            if (cutSidesInt[p1] != 0)
                            sides[0] = true;
                            else
                            sides[0] = false;

                            if (cutSidesInt[p2] != 0)
                            sides[1] = true;
                            else
                            sides[1] = false;

                            if (cutSidesInt[p3] != 0)
                            sides[2] = true;
                            else
                            sides[2] = false;
                        } 
                        else
                        {
                            sides[0] = cutSides[p1];//
                            sides[1] = cutSides[p2];//
                            sides[2] = cutSides[p3];//
                        }

                        // whole triangle
                        if (p1 >= _victim_mesh_vertices.Length || p2 >= _victim_mesh_vertices.Length || p3 >= _victim_mesh_vertices.Length)
                        {
                            if (isDebug) Debug.Log("xxxxxxxxxxxxOut Of Bounds Exception Avoided2 ");
                            currentVictim = null;//fixed a long-standing bug
                            return emptyCutsReturn;//nullCuts;  //empty is bad, null is continue                          
                        }
                        if (sides[0] == sides[1] && sides[0] == sides[2]) {//all points of the triangle are on the same side of the blade
                            //float addTriStartTime = Time.realtimeSinceStartup;
                            Vector3[] newVert = new Vector3[] { _victim_mesh_vertices[p1], _victim_mesh_vertices[p2], _victim_mesh_vertices[p3] };
                            Vector3[] newNormals = new Vector3[] { _victim_mesh_normals[p1], _victim_mesh_normals[p2], _victim_mesh_normals[p3] };
                            Vector2[] newUvs = new Vector2[] { _victim_mesh_uv[p1], _victim_mesh_uv[p2], _victim_mesh_uv[p3] };
                            Vector2[] newUv2 = new Vector2[] { _victim_mesh_uv2[p1], _victim_mesh_uv2[p2], _victim_mesh_uv2[p3] };
                            Vector4[] newTangents = new Vector4[] { Vector3.zero,Vector3.zero,Vector3.zero };
                            if (_victim_mesh_tangents.Length == _victim_mesh_vertices.Length)
                                newTangents = new Vector4[] { _victim_mesh_tangents[p1], _victim_mesh_tangents[p2], _victim_mesh_tangents[p3] };
                            if (sides[0]) { // left side //1 is the left side (arbitrary)
                                _leftSide.AddTriangle(
                                    newVert,
                                    newNormals,
                                    newUvs,
                                    newUv2,
                                    newTangents,
                                    sub);
                            } else {
                                _rightSide.AddTriangle(
                                    newVert,
                                    newNormals,
                                    newUvs,
                                    newUv2,
                                    newTangents,
                                    sub);
                            }

                        } else { // cut the triangle
                            int newP1 = p1;
                            int newP2 = p2;
                            int newP3 = p3;
                            //rearrange to place the solo point in the first position while retaining the points order
                            if (sides[0] == sides[1]) { //p1 and p2 same side
                                newP1 = p3;//single
                                newP2 = p1;
                                newP3 = p2;
                            }
                            if (sides[2] == sides[0]) { //p3 and p1 same side
                                newP1 = p2;
                                newP2 = p3;
                                newP3 = p1;
                            }

                            int numVerts = _victim_mesh_vertices.Length;
                            if (newP1 < numVerts && newP2 < numVerts && newP3 < numVerts)//trying to figure out this index out of bounds of array error
                                Cut_this_Face(
                                    new Vector3[] { _victim_mesh_vertices[newP1], _victim_mesh_vertices[newP2], _victim_mesh_vertices[newP3] },
                                    new Vector3[] { _victim_mesh_normals[newP1], _victim_mesh_normals[newP2], _victim_mesh_normals[newP3] },
                                    new Vector2[] { _victim_mesh_uv[newP1], _victim_mesh_uv[newP2], _victim_mesh_uv[newP3] },
                                    new Vector2[] { _victim_mesh_uv2[newP1], _victim_mesh_uv2[newP2], _victim_mesh_uv2[newP3] },
                                    new Vector4[] { _victim_mesh_tangents[newP1], _victim_mesh_tangents[newP2], _victim_mesh_tangents[newP3] },
                                    sub, newP1, newP2, newP3);

                            //Debug.Log("triCut time: " + triCutTime.ToString()); //17-74ms
                        }
                    }
                }

                if (_leftSide.GetMesh().triangles.Length < 4 || _rightSide.GetMesh().triangles.Length < 4) {//keep it inside the isCutFinished block
                    currentVictim = null;
                    if (isDebug)
                        Debug.Log("Empty Cut");
                    cutReturn[0] = victim;
                    cutReturn[1] = emptyCutGO;
                    return cutReturn;
                }
                isCutFinished = true;
            }//if !isCutFinished
            if (isDebug) status = "capping...";

            // The capping Material will be at the end
            float currentTimeSinceLoad = Time.timeSinceLevelLoad;

            Material[] mats = victim.GetComponent<MeshRenderer>().sharedMaterials;
            int cuttableSub = -1;
            if (useNewUV2Metal) {
                if (mats[mats.Length - 1].name != capMaterial.name) {
                    Material[] newMats = new Material[mats.Length + 1];
                    mats.CopyTo(newMats, 0);
                    newMats[mats.Length] = capMaterial;
                    mats = newMats;
                }
                _capMatSub = mats.Length - 1; // for later use
                cuttableSub = _capMatSub;
            } else {
                Material[] newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);//array copy, not sure if this creates instances or not
                                        //analyze submeshes to determine if any can be collapsed into the first
                collapsibleSubmeshes.Clear();
                int oldMatSub = -1;
                for (int i = 0; i < mats.Length; i++) {
                    string[] splitString = mats[i].name.Split(new string[] { "Clone" }, System.StringSplitOptions.None);

                    if (mats[i].shader.name == saberCutShaderName) {//collapses all cold sabercuts
                                                                   //if (splitString[0]=="saberCutMtl"){
                        float currentHeatStart = mats[i].GetFloat("_HeatStart");
                        if (currentHeatStart < currentTimeSinceLoad - mats[i].GetFloat("_HeatInterval")) {
                            collapsibleSubmeshes.Add(i);
                        }
                        if ((currentTimeSinceLoad - currentHeatStart < stillHotTime) && (oldMatSub == -1))
                            oldMatSub = i;
                    }
                }
                if (oldMatSub != -1 && oldMatSub != 0) {
                    newMats = victim.GetComponent<MeshRenderer>().sharedMaterials;
                    _capMatSub = oldMatSub;//this is the most recently heated up saberCut material
                    if (hotSabercutMat == null || (currentTimeSinceLoad - hotSabercutMat.GetFloat("_HeatStart") >= stillHotTime)) {
                        hotSabercutMat = newMats[oldMatSub];//refresh the hot material if it's not still hot
                    }
                    else
                    {
                        string[] splitStringHot = hotSabercutMat.name.Split(new string[] { "Clone" }, System.StringSplitOptions.None);
                        string[] splitStringOld = newMats[oldMatSub].name.Split(new string[] { "Clone" }, System.StringSplitOptions.None);
                        if (newMats[oldMatSub].GetFloat("_HeatStart") > hotSabercutMat.GetFloat("_HeatStart"))
                            if (splitStringHot[0] == splitStringOld[0])
                                hotSabercutMat = newMats[oldMatSub];//if the oldest hot metal is hotter than the shared one, replace it. this shouldn't happen but it is happening and needs to be fixed.
                            else {
                                if (splitStringHot[0] == splitStringOld[0])
                                    newMats[oldMatSub] = hotSabercutMat;//replaces the oldest hotmetal with a shared sabercut mat
                            }
                    }
                } else {//adding a new cut material
                    if (capMaterial.shader.name == saberCutShaderName) {//if it's a saberCut the heat needs to be started up
                        string[] splitString = capMaterial.name.Split(new string[] { "Clone" }, System.StringSplitOptions.None);
                        newMats[mats.Length] = MonoBehaviour.Instantiate(capMaterial);//instantiate a new copy of the hot sabercut material
                        string matRoot = splitString[0] + ":";//give it a nice name
                        newMats[mats.Length].name = matRoot + victim.name;
                        //newMats[mats.Length].SetFloat("_HeatStart", currentTimeSinceLoad);//redundant with cuttable class
                        cuttableSub = mats.Length;
                    } else {//non-saberCut materials are simply assigned
                        newMats[mats.Length] = capMaterial;
                    }
                    _capMatSub = newMats.Length - 1; // for later use
                }
                mats = newMats;
            }
            // cap the opennings
            //CappingOld();//the original capping function was just fine, shouldn't have fucked with it
            //turns out the old capping still produced way too many holes no matter how elegant the old code looks. I've reinstated my
            //overly complicated Capping() algo and I'll continue to study and revise it
            if (cappingStyle == CappingStyles.Orig) //SkipColinear
                CappingOrig();
            else if (cappingStyle == CappingStyles.OrigTweak)
                CappingOrigTweak();
            else if (cappingStyle == CappingStyles.SkipColinearTweak)
                CappingColinearTweak();
            else 
                Capping();
            //CappingOld();//tested old capping to see if there were still going to be holes and there are
            //capping seems slightly faster and generates the same amount of garbage as the rewritten capping2
            //Debug.Log("capTime time: " + capTime.ToString()); //17-74ms
            if (collapsibleSubmeshes.Count > 1) {
                if (isDebug)
                    Debug.Log("Found " + collapsibleSubmeshes.Count.ToString() + " collapsible submeshes");
                for (int i = 0; i < collapsibleSubmeshes.Count; i++)//first in the list is the good one
                    mats[collapsibleSubmeshes[i]] = capMaterial;
            }
            // Left Mesh
            Mesh left_HalfMesh = _leftSide.GetMesh();
            //left_HalfMesh.Optimize();//this takes 7.49ms for 2 calls on my macbook air
            left_HalfMesh.name = "Split Mesh Left" + cutCount.ToString();
            //left_HalfMesh.RecalculateNormals();//is this worth trying? //they aren't smooth

            // Right Mesh
            Mesh right_HalfMesh = _rightSide.GetMesh();
            //right_HalfMesh.Optimize();
            right_HalfMesh.name = "Split Mesh Right" + cutCount.ToString();
            //right_HalfMesh.RecalculateNormals();//is this worth trying?

            if (isDebug) status = "joints...";

            //swap meshes if the joint is not of the left side
            JointConnection jc = victim.GetComponent<JointConnection>();
            bool leftRightSwap = false;
            if (jc != null) {
                if (jc.joint != null) {
                    if (jc.joint.connectedBody != null) {
                        Vector3 jointLocalCenter = jc.jointPosition;// _victimTransform.InverseTransformPoint (jc.joint.gameObject.transform.position);
                        if (!_blade.GetSide(jointLocalCenter)) {//this isn't always working - it's connected but jointConnection is missing
                            Mesh tempMesh = left_HalfMesh;
                            left_HalfMesh = right_HalfMesh;
                            right_HalfMesh = tempMesh;
                            leftRightSwap = true;
                        }
                    }// else {
                    //    MonoBehaviour.Destroy (jc.joint);//deleting the joint might save a lot of problems
                    //}
                }
                //jc.joint.connectedBody = victim.GetComponent<Rigidbody>();
            }
            // assign the game objects

            victim.GetComponent<MeshFilter>().mesh = left_HalfMesh;

            Joint[] jo = victim.GetComponents<Joint>();
            bool[] joIsLeft = new bool[jo.Length];//joint should be on the right if the parent joint is left
            bool someJointsNeedToMoveRight = false;

            //kill empty joints on victim before instancing or copying joints
            for (int i = 0; i < jo.Length; i++) {
                if (jo[i].connectedBody == null)
                    MonoBehaviour.Destroy(jo[i]);
            }
            jo = victim.GetComponents<Joint>();//needs a refresh

            if (jo.Length > 0) { //found joints - figure out which ones need to move
                for (int i = 0; i < jo.Length; i++) {
                    if (jo[i] != null)
                        joIsLeft[i] = _blade.GetSide(jo[i].anchor);
                    if (leftRightSwap)
                        joIsLeft[i] = !joIsLeft[i];
                    if (!joIsLeft[i])
                        someJointsNeedToMoveRight = true;
                }
            }

            if (someJointsNeedToMoveRight) {//move the joint from the leftPiece to the rightPiece
                rightSideObj = MonoBehaviour.Instantiate(victim, _victimTransform.position, _victimTransform.rotation, null) as GameObject;//this is a cheat until I can copy the joint from left to right
                MeshCutInstance mci = rightSideObj.GetComponent<MeshCutInstance>();
                if (mci == null)
                    mci = rightSideObj.AddComponent<MeshCutInstance>();
                //mci.MeshCutInstanceAdd();
                //numMeshCutInstances = mci.GetNumMeshCutInstances()+1;
                Joint[] rightJo = rightSideObj.GetComponents<Joint>();
                for (int j = 0; j < jo.Length; j++) {
                    if (!joIsLeft[j]) {//move joint to the right
                        MonoBehaviour.Destroy(jo[j]);//kills the left joint
                        if (rightJo[j].connectedBody == null)//kill it if not connected to anything otherwise it's already there ("copied") //for the love of GOD don't delete these 2 lines!
                            MonoBehaviour.Destroy(rightJo[j]);//kills right joint  //fixes the hanging dangly shit connected to the base fecking redundant double-joint connection
                    } else {//leave left, kill right
                        MonoBehaviour.Destroy(rightJo[j]);//kills right joint
                    }
                }
            } else {
                rightSideObj = GetChunk();
                if (rightSideObj == null)
                {
                    if (isDebug)
                        Debug.LogWarning("***sacrificed cut because couldn't get a chunk!");
                    return nullCuts;//buy some time to get your buffer back
                }

            }
            leftSideObj = victim;
            if (leftSideObj.GetComponent<MeshCutChunk>() == null)
            {
                leftSideObj.name = SBLeftSideObjName(leftSideObj.GetInstanceID());// "MeshCutObj" + leftSideObj.GetInstanceID().ToString(); //leftSideObj.name + "Cut";//hacky way to make sure that instanced rigged chains don't get confused with each other since cutting gets handled by object name
                MeshCutInstance mci = leftSideObj.GetComponent<MeshCutInstance>();
                if (mci == null)
                    mci = leftSideObj.AddComponent<MeshCutInstance>();
                //mci.MeshCutInstanceAdd();
               // numMeshCutInstances = mci.GetNumMeshCutInstances()+1;
            }

			_rightSideObjTransform = rightSideObj.transform;//unnecessary caching of transforms
			_rightSideObjTransform.position = _victimTransform.position;//can't use local on rigged assets?
			_rightSideObjTransform.rotation = _victimTransform.rotation;
			rightSideObj.GetComponent<MeshFilter>().mesh = right_HalfMesh;
            rightSideObj.GetComponent<MeshFilter>().mesh.MarkDynamic();//supposed to make it perform better when updating a mesh frequently
			if(_victimTransform.parent != null){
				_rightSideObjTransform.parent = _victimTransform.parent;
				//RenameChildren(_victimTransform.parent);//this is why they aren't cutting properly
			}

			_rightSideObjTransform.localScale = _victimTransform.localScale;

            if (isDebug) status = "cuttableclass...";

            // assign mats
            leftSideObj.GetComponent<MeshRenderer>().materials = mats;
			rightSideObj.GetComponent<MeshRenderer>().materials = mats;
            Cuttable leftcc = leftSideObj.GetComponent<Cuttable>();
            Cuttable rightcc = rightSideObj.GetComponent<Cuttable>();
            string cuttableTypeString="";
            System.Type victimCuttableType = cuttable.GetType();

            if (cuttable != null)
            {//using new cuttable abstract class to assign unique cappingMtls, should replace Cuttable tag
                cuttableTypeString = victimCuttableType.ToString();
                if (isDebug)
                    Debug.Log("Victim Cuttable Class: " + cuttableTypeString);
            }
            bool probablyRecycled = false;
            if (rightcc != null)
            {
                System.Type rightccType = rightcc.GetType();
                if (rightccType != victimCuttableType){
                    MonoBehaviour.Destroy(rightcc);//delayed destroy but it's okay because we're adding a different component
                    probablyRecycled = true;
                }
            }
            if (rightcc == null || probablyRecycled)//after a reset this should be null every time, hacky sacrifice fix?
            {
                switch (cuttableTypeString)
                {
                    case "CuttableMetal":
                        //rightcc = rightSideObj.AddComponent(typeof(CuttableMetal)) as CuttableMetal;//this works but isn't consistent with the rest
                        rightcc = rightSideObj.AddComponent<CuttableMetal>() as CuttableMetal;
                        break;
                    case "CuttableChunk":
                        rightcc = rightSideObj.AddComponent<CuttableChunk>() as CuttableChunk;
                        break;
                    case "CuttableMetalRig":
                        rightcc = rightSideObj.AddComponent<CuttableMetalRig>() as CuttableMetalRig;
                        break;
                    default:
                        rightcc = rightSideObj.AddComponent<CuttableChunk>() as CuttableChunk;
                        break;
                }
            }

            if (leftcc == null)
            {
                switch (cuttableTypeString)
                {// not exactly sure if this is necessary
                    case "CuttableMetal":
                        if (leftcc == null)
                        leftcc = leftSideObj.AddComponent<CuttableMetal>();
                        break;
                    case "CuttableChunk":
                        if (leftcc == null)
                        leftcc = leftSideObj.AddComponent<CuttableChunk>();
                        break;
                    case "CuttableMetalRig":
                        if (leftcc == null)
                        leftcc = leftSideObj.AddComponent<CuttableMetalRig>();
                        break;
                    default:
                        if (leftcc == null)
                        leftcc = leftSideObj.AddComponent<CuttableChunk>();
                        break;
                }
            }
            rightcc.cutMat = capMaterial;
            leftcc.cutMat = capMaterial;
            rightcc.submeshIndex = cuttableSub;
            leftcc.submeshIndex = cuttableSub;
            rightcc.CutSetup();
            leftcc.CutSetup();
            
			leftSideObj.tag = "Cuttable";
			rightSideObj.tag = "Cuttable";
			cutCount++;//dks
			currentVictim = null;//tells meshcut that it's ready for a new victim
			//return new GameObject[]{ leftSideObj, rightSideObj };
            cutReturn[0]=leftSideObj;
            cutReturn[1]=rightSideObj;
            if (isDebug) status = "done.";

            return cutReturn;
		}//Cut 3-point plane



		/// <summary>
		///  I have no idea how I made this work
		/// </summary>
		private static void Cut_this_Face( // everything uses _blade which is defined at the class level
			Vector3[] vertices, //can assume each of these get called with 3 elements forming a tri
			Vector3[] normals,
			Vector2[] uvs,
            Vector2[] uv2,
			Vector4[] tangents,
			int       submesh,
			int p1,int p2,int p3){

			bool[] sides = new bool[3];
			sides[0] = cutSides[p1];// true = left
			sides[1] = cutSides[p2];
			sides[2] = cutSides[p3];

			Vector3[] leftPoints = new Vector3[2];
			Vector3[] leftNormals = new Vector3[2];
			Vector2[] leftUvs = new Vector2[2];
            Vector2[] leftUv2 = new Vector2[2];
			Vector4[] leftTangents = new Vector4[2];
			Vector3[] rightPoints = new Vector3[2];
			Vector3[] rightNormals = new Vector3[2];
			Vector2[] rightUvs = new Vector2[2];
            Vector2[] rightUv2 = new Vector2[2];
			Vector4[] rightTangents = new Vector4[2];

			bool didset_left = false;
			bool didset_right = false;
			bool twoLeftPoints = false; //if this remains false it can be assumed there are two right points
			bool leftFirst = sides[0];//dks addition because the order matters
			//bool leftLast = sides[2];//do I need this? dks //currently unused
            //maybe doesn't matter which is first or last as long as the order is retained
			int pL0=0;
			int pL1=0;
			int pR0=0;
			int pR1=0;
			int[] vertIndex = {p1,p2,p3};
			for(int i=0; i<3; i++){ //sort the points into left and right sides, 2 on each side even though it's a tri

				if(sides[i]){
					if(!didset_left){//if no point has been found on the left side
						didset_left = true;

						leftPoints[0]   = vertices[i];
						leftPoints[1]   = leftPoints[0];//inits the second point in case this side only has one point
						leftUvs[0]     = uvs[i];
						leftUvs[1]     = leftUvs[0];//inits the second point
                        leftUv2[0]     = uv2[i];
                        leftUv2[1]     = leftUv2[0];//inits the second point
						leftNormals[0] = normals[i];
						leftNormals[1] = leftNormals[0];//inits the second point
						leftTangents[0] = tangents[i];
						leftTangents[1] = leftTangents[0];//inits the second point
						pL0=vertIndex[i];
						pL1=pL0;
					}else{
						leftPoints[1]   = vertices[i]; //if this side had 2 points the second point gets assigned
						leftUvs[1]      = uvs[i];
                        leftUv2[1]      = uv2[i];
						leftNormals[1]  = normals[i];
						leftTangents[1] = tangents[i];
						twoLeftPoints = true;
						pL1=vertIndex[i];
					}
				}else{
					if(!didset_right){
						didset_right = true;

						rightPoints[0]   = vertices[i];
						rightPoints[1]   = rightPoints[0];
						rightUvs[0]     = uvs[i];
						rightUvs[1]     = rightUvs[0];
                        rightUv2[0]     = uv2[i];
                        rightUv2[1]     = rightUv2[0];
						rightNormals[0] = normals[i];
						rightNormals[1] = rightNormals[0];
						rightTangents[0] = tangents[i];
						rightTangents[1] = rightTangents[0];
						pR0=vertIndex[i];
						pR1=pR0;
					}else{
						rightPoints[1]  = vertices[i];
						rightUvs[1]     = uvs[i];
                        rightUv2[1]     = uv2[i];
						rightNormals[1] = normals[i];
						rightTangents[1] = tangents[i];
						pR1=vertIndex[i];
					}
				}
			}

			float normalizedDistance = 0.0f;
			float distance = 0;

			Vector3 newVertex1=Vector3.zero;
			Vector2 newUv_1=Vector2.zero;
            Vector2 newUv2_1=Vector2.zero;
			Vector3 newNormal1=Vector3.zero;
			Vector4 newTangent1=Vector4.zero;
			Vector3 newVertex2=Vector3.zero;
			Vector2 newUv_2=Vector2.zero;
            Vector2 newUv2_2=Vector2.zero;
			Vector3 newNormal2=Vector3.zero;
			Vector4 newTangent2=Vector4.zero;

			Vector3	rtMinusLf0 = rightPoints[0] - leftPoints[0]; //new points are in order already
			Vector3	rtMinusLf1 = rightPoints[1] - leftPoints[1];
			Ray ray;
			//string cutPointL0R0Key = pL0.ToString("0000")+pR0.ToString("0000");
			//string cutPointL1R1Key = pL1.ToString("0000")+pR1.ToString("0000");
			//string cutPointR0L0Key = pR0.ToString("0000")+pL0.ToString("0000");
			//string cutPointR1L1Key = pR1.ToString("0000")+pL1.ToString("0000");

            string cutPointL0R0Key = SBIntsToString(new int[]{pL0,pR0},5);//pL0.ToString("0000")+pR0.ToString("0000");
            string cutPointL1R1Key = SBIntsToString(new int[]{pL1,pR1},5);//pL1.ToString("0000")+pR1.ToString("0000");
            string cutPointR0L0Key = SBIntsToString(new int[]{pR0,pL0},5);//pR0.ToString("0000")+pL0.ToString("0000");
            string cutPointR1L1Key = SBIntsToString(new int[]{pR1,pL1},5);//pR1.ToString("0000")+pL1.ToString("0000");
            
			if (cutPoints.ContainsKey(cutPointL0R0Key)){//storing cuts to save time on redundant edge cuts
				newVertex1 = cutPoints[cutPointL0R0Key].vertex;
				newUv_1 = cutPoints[cutPointL0R0Key].uv;
                newUv2_1 = cutPoints[cutPointL0R0Key].uv2;
				newNormal1 = cutPoints[cutPointL0R0Key].normal;
				newTangent1 = cutPoints[cutPointL0R0Key].tangent;
			} else {
                float rtMinusLf0Magnitude = rtMinusLf0.magnitude;
                rtMinusLf0 = FastNormalize(ref rtMinusLf0);//Think this caused artifacts
                //rtMinusLf0.Normalize();
                ray = new Ray(leftPoints[0], rtMinusLf0);//rtMinusLf0.normalized);
                //find the ray/plane intersect and calculate the distance from leftPoint0 to rightPoint0
                _blade.Raycast(ray, out distance);

				normalizedDistance =  distance/rtMinusLf0Magnitude;//a ratio of the length of that edge
				newVertex1 = ray.GetPoint(distance); //dks - i did this to try to optimize maybe
				//Vector3 newVertex1 = Vector3.Lerp(leftPoints[0], rightPoints[0], normalizedDistance);
				newUv_1     = Vector2.Lerp(leftUvs[0], rightUvs[0], normalizedDistance);
                newUv2_1    = Vector2.Min(Vector2.Lerp(leftUv2[0], rightUv2[0], normalizedDistance) + new Vector2(.5f,0), Vector2.one);
				newNormal1 = Vector3.Lerp(leftNormals[0] , rightNormals[0], normalizedDistance);
				newTangent1 = Vector3.Lerp(leftTangents[0], rightTangents[0], normalizedDistance);		

				//if (!cutPoints.ContainsKey(pL0.ToString("0000")+pR0.ToString("0000"))){//need to make a new class for a struct that has verts, uv,normal,tangent
					cutPoints.Add(cutPointL0R0Key,new SaberCutPoint(newVertex1,newNormal1,newUv_1,newUv2_1,newTangent1));
					cutPoints.Add(cutPointR0L0Key,new SaberCutPoint(newVertex1,newNormal1,newUv_1,newUv2_1,newTangent1));
					//Debug.Log("Added Dictionary entry:"+pL0.ToString("0000")+pR0.ToString("0000")+" "+cutPoints[pL0.ToString("0000")+pR0.ToString("0000")].vertex.ToString());
				//}
			}

			if (cutPoints.ContainsKey(cutPointL1R1Key)){//need to make a new class for a struct that has verts, uv,normal,tangent
				newVertex2 =cutPoints[cutPointL1R1Key].vertex;
				newUv_2 = cutPoints[cutPointL1R1Key].uv;
                newUv2_2 = cutPoints[cutPointL1R1Key].uv2;
				newNormal2 = cutPoints[cutPointL1R1Key].normal;
				newTangent2 = cutPoints[cutPointL1R1Key].tangent;
			} else {
                float rtMinusLf1Magnitude = rtMinusLf1.magnitude;
                rtMinusLf1 = FastNormalize(ref rtMinusLf1);//think this caused artifacts
                //rtMinusLf1.Normalize();
                ray = new Ray(leftPoints[1], rtMinusLf1);
				_blade.Raycast(ray, out distance);

				normalizedDistance =  distance/rtMinusLf1Magnitude;
				newVertex2 = ray.GetPoint(distance);//maybe optimized but maybe messes up capping
				//Vector3 newVertex2 = Vector3.Lerp(leftPoints[1], rightPoints[1], normalizedDistance);
				newUv_2     = Vector2.Lerp(leftUvs[1], rightUvs[1], normalizedDistance);
                newUv2_2    = Vector2.Min(Vector2.Lerp(leftUv2[1], rightUv2[1], normalizedDistance) + new Vector2(.5f,0),Vector2.one);
				newNormal2 = Vector3.Lerp(leftNormals[1] , rightNormals[1], normalizedDistance);
				newTangent2 = Vector3.Lerp(leftTangents[1], rightTangents[1], normalizedDistance);		
				cutPoints.Add(cutPointL1R1Key,new SaberCutPoint(newVertex2,newNormal2,newUv_2,newUv2_2,newTangent2));
				cutPoints.Add(cutPointR1L1Key,new SaberCutPoint(newVertex2,newNormal2,newUv_2,newUv2_2,newTangent2));
			}
            if (leftFirst){
                _new_vertices.Add(newVertex2);//add the cut points in pairs so that capping can handle each pair as a side of a tri
                _new_vertices.Add(newVertex1);//order might need to be flipped so that the left side cap will always have correct normals
            } else {
                _new_vertices.Add(newVertex1);//add the cut points in pairs so that capping can handle each pair as a side of a tri
                _new_vertices.Add(newVertex2);//order might need to be flipped so that the left side cap will always have correct normals
            }

			Vector3[] final_verts; 
			Vector3[] final_norms;
			Vector2[] final_uvs;
            Vector2[] final_uv2;
			Vector4[] final_tangents;

			// first triangle - is optimized but the 2nd and third need to be flipped

			//assuming that the new verts are already in the order they need to be in this should be easy
			//but this assumes that the single point is the first point in the list
			//followed by the doubles in the correct order
			if (!twoLeftPoints){ //left single  - think this is okay
				final_verts = new Vector3[]{leftPoints[0], newVertex1, newVertex2}; //l0n1n2
				final_norms = new Vector3[]{leftNormals[0], newNormal1, newNormal2};
				final_uvs   = new Vector2[]{leftUvs[0], newUv_1, newUv_2};
                final_uv2   = new Vector2[]{leftUv2[0], newUv2_1, newUv2_2};
				final_tangents = new Vector4[]{ leftTangents[0], newTangent1, newTangent2};
			} else {  //right single
				final_verts = new Vector3[]{rightPoints[0], newVertex1, newVertex2};//r0n1n2
				final_norms = new Vector3[]{rightNormals[0], newNormal1, newNormal2};
				final_uvs   = new Vector2[]{rightUvs[0], newUv_1, newUv_2};
                final_uv2   = new Vector2[]{rightUv2[0], newUv2_1, newUv2_2};
				final_tangents = new Vector4[]{ rightTangents[0], newTangent1, newTangent2};
			}
			//saved the following 3 lines just in case I'll need this later on but as far as I'm concerned flipFace is trash -dks
			// 	if (Vector3.Dot(Vector3.Cross(final_verts[1] - final_verts[0], final_verts[2] - final_verts[0]), final_norms[0]) < 0)
			// 		FlipFace(final_verts, final_norms, final_uvs, final_tangents);
			// }

			if (!twoLeftPoints)
				_leftSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents, submesh);
			else
				_rightSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents, submesh);

			// second triangle - starts on p2

			if (!twoLeftPoints){ //left single first
					final_verts = new Vector3[]{newVertex1, rightPoints[0], rightPoints[1] };//r0r1n1 or r0n2n1 or (n1r0r1)
					final_norms = new Vector3[]{newNormal1, rightNormals[0], rightNormals[1]};
					final_uvs   = new Vector2[]{ newUv_1, rightUvs[0], rightUvs[1]};
                    final_uv2   = new Vector2[]{ newUv2_1, rightUv2[0], rightUv2[1]};
					final_tangents = new Vector4[]{ newTangent1, rightTangents[0], rightTangents[1]};
			} else { //right single first
					final_verts = new Vector3[]{newVertex1, leftPoints[0], leftPoints[1]};//l0l1n1 or l0n2n1 or (n1l0l1)
					final_norms = new Vector3[]{newNormal1, leftNormals[0], leftNormals[1]};
					final_uvs   = new Vector2[]{newUv_1, leftUvs[0], leftUvs[1]};
                    final_uv2   = new Vector2[]{newUv2_1, leftUv2[0], leftUv2[1]};
					final_tangents = new Vector4[]{newTangent1, leftTangents[0], leftTangents[1]};
			}
				
			if (!twoLeftPoints)
				_rightSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents, submesh);
			else
				_leftSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents, submesh);

			// third triangle
			if (!twoLeftPoints){ //left single
				final_verts = new Vector3[]{rightPoints[1], newVertex2, newVertex1};//was r1n2n1
				final_norms = new Vector3[]{rightNormals[1], newNormal2, newNormal1};
				final_uvs   = new Vector2[]{rightUvs[1], newUv_2, newUv_1};
                final_uv2   = new Vector2[]{rightUv2[1], newUv2_2, newUv2_1};
				final_tangents = new Vector4[]{ rightTangents[1], newTangent2, newTangent1};
			} else {
				final_verts = new Vector3[]{leftPoints[1], newVertex2, newVertex1};//was l1n2n1
				final_norms = new Vector3[]{leftNormals[1], newNormal2, newNormal1};
				final_uvs   = new Vector2[]{leftUvs[1], newUv_2, newUv_1};
                final_uv2   = new Vector2[]{leftUv2[1], newUv2_2, newUv2_1};
				final_tangents = new Vector4[]{ leftTangents[1], newTangent2, newTangent1};
			}

			if (!twoLeftPoints)
				_rightSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents, submesh);
			else
				_leftSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents, submesh);

		}

		private static void FlipFace(
			Vector3[] verts,
			Vector3[] norms,
			Vector2[] uvs, 
			Vector4[] tangents)
		{

			Vector3 temp = verts[2];
			verts[2] = verts[0];
			verts[0] = temp;

			temp = norms[2];
			norms[2] = norms[0];
			norms[0] = temp;

			Vector2 temp2 = uvs[2];
			uvs[2] = uvs[0];
			uvs[0] = temp2;

			Vector4 temp3 = tangents[2];
			tangents[2] = tangents[0];
			tangents[0] = temp3;

		}

        private static void FlipFace(
            Vector3[] verts,
            Vector3[] norms,
            Vector2[] uvs, 
            Vector2[] uv2,
            Vector4[] tangents)
        {

            Vector3 temp = verts[2];
            verts[2] = verts[0];
            verts[0] = temp;

            temp = norms[2];
            norms[2] = norms[0];
            norms[0] = temp;

            Vector2 temp2 = uvs[2];
            uvs[2] = uvs[0];
            uvs[0] = temp2;

            Vector2 temp2a = uv2[2];
            uv2[2] = uv2[0];
            uv2[0] = temp2a;

            Vector4 temp3 = tangents[2];
            tangents[2] = tangents[0];
            tangents[0] = temp3;

        }


		//big performance hit when using a List, I can probably write a much faster contains() function for an array of verts
        //I also probably need a good way to compare points
        //perhaps due to rewriting VectorListContains, the list method is just as fast as the array method
        static bool VectorListContains(List<Vector3> vecList, Vector3 vec)
        {
            for (int i = 0; i < vecList.Count; i++){
                Vector3 testVec = vecList[i];
                //if (Vector3Compare(vec,testVec))
                if (vec == testVec)//vector3 equality is faster than vector3compare now?
                    return true;
            }
            return false;
        }

        private static List<Vector3> capVertTracker = new List<Vector3>();
        private static HashSet<Vector3> capVertTrackerHashSet = new HashSet<Vector3>();

        private static List<Vector3> capVertpolygon = new List<Vector3>();

		static void Capping(){
			capVertTracker.Clear();//tracks every vert in the cap vertices list 
			//loops through new verts, if it encounters a vert that hasn't been tracked 
            //it adds the current pair of points to the current capVertPol and capVertTracker
			//loops through the entire list of new vertices in pairs and finds the one adjacent to the previous capVert

            //loop through _new_vertices

            //should have recorded line segments during the cutting process, then this would be easier to do
			for(int i=0; i<_new_vertices.Count-1; i++)//for every vert not already capped find all of the connected verts and cap it
            {
                //check to see if capVerTracker contains the new vert
                //vectorlistcontains seems faster for some reason
                if(!VectorListContains(capVertTracker,_new_vertices[i]))//if(!capVertTracker.Contains(_new_vertices[i]))
				{
                    int indexA = i;
                    int indexB = i + 1;
                    if (indexB == _new_vertices.Count)
                    {
                        indexB = 0;//hoping this helps to close the loop if it wasn't already closing properly already
                    }
					capVertpolygon.Clear();//clear the polygon tracker so we can build it up?
					capVertpolygon.Add(_new_vertices[indexA]);//add new vert and next vert to both lists
					capVertpolygon.Add(_new_vertices[indexB]);
					capVertTracker.Add(_new_vertices[indexA]);
					capVertTracker.Add(_new_vertices[indexB]);//not sure if this will be redundant
					bool isDone = false;
					while(!isDone)//loop through all of the remaining new vertices searching for the adjacent vert forming a segment
                    {
						isDone = true;
						for(int k=0; k<_new_vertices.Count-1; k+=2){ // go through the pairs, searching for the adjacent vert and add to the poly list 
							//dks- changed k=0 to k=i-(i%2) to avoid some wasted time --dks 6/14/2018 don't understand the weird loop logic

                            if (_new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !VectorListContains(capVertTracker,_new_vertices[k+1])){ //vector3 equality is faster than vector3compare, maybe because of an update?
                                                                                                                                                       //if(Vector3Compare(_new_vertices[k],capVertpolygon[capVertpolygon.Count-1]) && !VectorListContains(capVertTracker,_new_vertices[k+1])){ 

                                //if the current point is the last one in the cap and the next point is currently untracked...
                                //check colinear
                                Vector3 capVertDiffA = (capVertpolygon[capVertpolygon.Count - 1] - capVertpolygon[capVertpolygon.Count - 2]);
                                capVertDiffA.Normalize();//maybe I need accuracy in this case
                                Vector3 capVertDiffB = _new_vertices[k + 1] - capVertpolygon[capVertpolygon.Count - 1];
                                float sqrLenB = capVertDiffB.sqrMagnitude;
                                capVertDiffB.Normalize();
                                float dot = Vector3.Dot (capVertDiffA,capVertDiffB);
                                if ( dot > .99 && k > 1 && k < capVertpolygon.Count)//if the previous cap segment is colinear with the new segment,
                                {   //move the previous poly point to the new point
									capVertpolygon[capVertpolygon.Count-1] = _new_vertices[k+1];
                                    capVertTracker.Add(_new_vertices[k+1]);//tell the tracker not to worry about this one
								} else { //else grab that new poly point, add to the polygon and add to the tracked list as well
									capVertpolygon.Add(_new_vertices[k+1]);
									capVertTracker.Add(_new_vertices[k+1]);
								}
								isDone = false;//if we've added a new point we aren't done
							//} else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k])){// if so add the other
                            } else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !VectorListContains(capVertTracker,_new_vertices[k])){// if so add the other
                                Vector3 capVertDiffA = capVertpolygon[capVertpolygon.Count - 1] - capVertpolygon[capVertpolygon.Count - 2] ;
                                capVertDiffA.Normalize();
                                Vector3 capVertDiffB = _new_vertices[k] - capVertpolygon[capVertpolygon.Count - 1];
                                float sqrLenB = capVertDiffB.sqrMagnitude;
                                capVertDiffB.Normalize();

                                float dot = Vector3.Dot (capVertDiffA, capVertDiffB);
                                if (dot > .99 && k > 1 && k < capVertpolygon.Count){//colinear edges move the previous poly point to the new point
											capVertpolygon[capVertpolygon.Count-1] = _new_vertices[k];
                                            capVertTracker.Add(_new_vertices[k]);//tell the tracker not to worry about this one
										} else {
											capVertpolygon.Add(_new_vertices[k]);
											capVertTracker.Add(_new_vertices[k]);
										}
								isDone = false;//if we've added a new point we aren't done
							}
						}
					}//while loop looking for neighboring vertices that are not already tracked 
                    //after gathering a clockwise list of vertices, should be easy to fill the cap?
                    //can I intercept this list and build triangles using qualifying adjacent edges?
					bool result = FillCap(capVertpolygon);
					i+=2;//dks
				}//if contains new
			}//for i //why is there an outer and inner loop?
            /*if (capVertTracker.Count < _new_vertices.Count/2){
                Debug.Log("******************Not all verts were added to a cap:" + capVertTracker.Count.ToString()+" != "+(_new_vertices.Count/2).ToString());
                capVertpolygon.Clear();
                //super hacky capping of the leftovers
                for (int l=0; l<_new_vertices.Count; l+=2)
                {
                    if (!capVertpolygon.Contains(_new_vertices[l]))
                        capVertpolygon.Add(_new_vertices[l]);
                }
                FillCap(capVertpolygon);
            }*/
            //4/10/2019 analysis. This is some hacky shit. I've stored the newvertices in pairs so there are double the points 
            //and that's also why the capping script is so weird with the two by two logic
            //try to reorganize the data and rewrite the capping algorithm in a sensible fashion

		}

        static void CappingColinearTweak(){
            fillCapCenterBak = Vector3.zero;//trying to handle scenarios where only a single line segment is sent to fillCap, then this will provide a relatively stable center point
            previousCenterPoint = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _new_vertices.Count; i+=2)
                {
                    fillCapCenterBak.x += _new_vertices[i].x;
                    fillCapCenterBak.y += _new_vertices[i].y;
                    fillCapCenterBak.z += _new_vertices[i].z;
                    count++;
                }
            fillCapCenterBak.x = fillCapCenterBak.x/count;
            fillCapCenterBak.y = fillCapCenterBak.y/count;
            fillCapCenterBak.z = fillCapCenterBak.z/count;
            fillCapCenterBak = _blade.ClosestPointOnPlane(fillCapCenterBak);

            capVertTrackerHashSet.Clear();//tracks every vert in the cap vertices list 
            //loops through new verts, if it encounters a vert that hasn't been tracked 
            //it adds the current pair of points to the current capVertPol and capVertTracker
            //loops through the entire list of new vertices in pairs and finds the one adjacent to the previous capVert

            //loop through _new_vertices

            //should have recorded line segments during the cutting process, then this would be easier to do
            for(int i=0; i<_new_vertices.Count-1; i+=2)//can skip every other index because we are tracking in pairs
            {
                //check to see if capVerTracker contains the new vert
                //vectorlistcontains seems faster for some reason
                if(!capVertTrackerHashSet.Contains(_new_vertices[i]))//if(!capVertTracker.Contains(_new_vertices[i]))
                {
                    int indexA = i;
                    int indexB = i + 1;
                    if (indexB == _new_vertices.Count)
                    {
                        indexB = 0;//hoping this helps to close the loop if it wasn't already closing properly already
                    }
                    capVertpolygon.Clear();//find all connected segments and send poly to fillcap, do this until no more segments are left
                    capVertpolygon.Add(_new_vertices[indexA]);//add new vert and next vert to both lists
                    capVertpolygon.Add(_new_vertices[indexB]);
                    capVertTrackerHashSet.Add(_new_vertices[indexA]);
                    capVertTrackerHashSet.Add(_new_vertices[indexB]);//not sure if this will be redundant
                    bool isDone = false;
                    while(!isDone)//loop through all of the remaining new vertices searching for the adjacent vert forming a segment
                    {
                        isDone = true;
                        for(int k=0; k<_new_vertices.Count-1; k+=2){ // go through the pairs, searching for the adjacent vert and add to the poly list 
                            //dks- changed k=0 to k=i-(i%2) to avoid some wasted time --dks 6/14/2018 don't understand the weird loop logic

                            if (_new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !capVertTrackerHashSet.Contains(_new_vertices[k+1])){ //vector3 equality is faster than vector3compare, maybe because of an update?
                            //if(Vector3Compare(_new_vertices[k],capVertpolygon[capVertpolygon.Count-1]) && !VectorListContains(capVertTracker,_new_vertices[k+1])){ 
                                //if the current point is the last one in the cap and the next point is currently untracked...
                                //check colinear
                                Vector3 capVertDiffA = (capVertpolygon[capVertpolygon.Count - 1] - capVertpolygon[capVertpolygon.Count - 2]);
                                capVertDiffA.Normalize();//maybe I need accuracy in this case
                                Vector3 capVertDiffB = _new_vertices[k + 1] - capVertpolygon[capVertpolygon.Count - 1];
                                float sqrLenB = capVertDiffB.sqrMagnitude;
                                capVertDiffB.Normalize();
                                float dot = Vector3.Dot (capVertDiffA,capVertDiffB);
                                if ( dot > .99 && k > 1 && k < capVertpolygon.Count)//if the previous cap segment is colinear with the new segment,
                                {   //move the previous poly point to the new point
                                    capVertpolygon[capVertpolygon.Count-1] = _new_vertices[k+1];
                                    capVertTrackerHashSet.Add(_new_vertices[k+1]);//tell the tracker not to worry about this one
                                } else { //else grab that new poly point, add to the polygon and add to the tracked list as well
                                    capVertpolygon.Add(_new_vertices[k+1]);
                                    capVertTrackerHashSet.Add(_new_vertices[k+1]);
                                }
                                isDone = false;//if we've added a new point we aren't done
                            //} else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k])){// if so add the other
                            } else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTrackerHashSet.Contains(_new_vertices[k])){// if so add the other
                                Vector3 capVertDiffA = capVertpolygon[capVertpolygon.Count - 1] - capVertpolygon[capVertpolygon.Count - 2] ;
                                capVertDiffA.Normalize();
                                Vector3 capVertDiffB = _new_vertices[k] - capVertpolygon[capVertpolygon.Count - 1];
                                float sqrLenB = capVertDiffB.sqrMagnitude;
                                capVertDiffB.Normalize();

                                float dot = Vector3.Dot (capVertDiffA, capVertDiffB);
                                if (dot > .99 && k > 1 && k < capVertpolygon.Count){//colinear edges move the previous poly point to the new point
                                            capVertpolygon[capVertpolygon.Count-1] = _new_vertices[k];
                                            capVertTrackerHashSet.Add(_new_vertices[k]);//tell the tracker not to worry about this one
                                        } else {
                                            capVertpolygon.Add(_new_vertices[k]);
                                            capVertTrackerHashSet.Add(_new_vertices[k]);
                                        }
                                isDone = false;//if we've added a new point we aren't done
                            }
                        }
                    }//while loop looking for neighboring vertices that are not already tracked 
                    //after gathering a clockwise list of vertices, should be easy to fill the cap?
                    //can I intercept this list and build triangles using qualifying adjacent edges?
                    bool result = FillCap(capVertpolygon);
                    i+=2;//also can jump ahead in pairs because only interested in the segments that came out of Cut()
                }//if contains new
            }//for i //why is there an outer and inner loop?

        }

        private static Vector3 fillCapCenterBak = Vector3.zero;
        static void CappingOrigTweak(){ //updated from a git repo and then tweakd a tiny bit by dks
            fillCapCenterBak = Vector3.zero;//trying to handle scenarios where only a single line segment is sent to fillCap, then this will provide a relatively stable center point
            previousCenterPoint = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _new_vertices.Count; i+=2)
                {
                    fillCapCenterBak.x += _new_vertices[i].x;
                    fillCapCenterBak.y += _new_vertices[i].y;
                    fillCapCenterBak.z += _new_vertices[i].z;
                    count++;
                }
                fillCapCenterBak.x = fillCapCenterBak.x/count;
                fillCapCenterBak.y = fillCapCenterBak.y/count;
                fillCapCenterBak.z = fillCapCenterBak.z/count;
                fillCapCenterBak = _blade.ClosestPointOnPlane(fillCapCenterBak);

            capVertTrackerHashSet.Clear();

            for(int i=0; i<_new_vertices.Count-1; i+=2)//dks added -1 and changed i++ to i+=2
                //loop through every consecutive pair of points representing a cut side of a triangle
                if(!capVertTrackerHashSet.Contains(_new_vertices[i]))  //if the pair in question has not been tracked, start a new polygon
                {
                    capVertpolygon.Clear();//starting a new polygon fresh
                    capVertpolygon.Add(_new_vertices[i]);
                    capVertpolygon.Add(_new_vertices[i+1]);

                    capVertTrackerHashSet.Add(_new_vertices[i]);
                    capVertTrackerHashSet.Add(_new_vertices[i+1]);

                    //loop through all of the following consecutive line segments
                    bool isDone = false;
                    while(!isDone){//as soon as it can't find any more attached line segments in the remaining list, 
                                    //it fills the polygon and starts with the next untracked line segment
                        isDone = true;

                        for(int k=i; k<_new_vertices.Count; k+=2){ // go through the pairs, dks changed k=0 to k=i

                            if(_new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !capVertTrackerHashSet.Contains(_new_vertices[k+1])){ // if so add the other
                                isDone = false;
                                capVertpolygon.Add(_new_vertices[k+1]);
                                capVertTrackerHashSet.Add(_new_vertices[k+1]);

                            }/*else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTrackerHashSet.Contains(_new_vertices[k])){// if so add the other
                                //dks thinks this is only because he thinks the line segment was reversed
                                isDone = false;
                                capVertpolygon.Add(_new_vertices[k]);
                                capVertTrackerHashSet.Add(_new_vertices[k]);
                            }*/ //commented out because dks thinks he deliberately stored all of the line segments in the correct order
                        }
                    }

                    bool result = FillCap(capVertpolygon);

                }
            
        }

        static void CappingOrig(){ //updated from a git repo

            capVertTracker.Clear();

            for(int i=0; i<_new_vertices.Count-1; i++)//dks changed to -1 to avoid errors
                if(!capVertTracker.Contains(_new_vertices[i]))
                {
                    capVertpolygon.Clear();
                    capVertpolygon.Add(_new_vertices[i]);
                    capVertpolygon.Add(_new_vertices[i+1]);

                    capVertTracker.Add(_new_vertices[i]);
                    capVertTracker.Add(_new_vertices[i+1]);


                    bool isDone = false;
                    while(!isDone){
                        isDone = true;

                        for(int k=0; k<_new_vertices.Count; k+=2){ // go through the pairs

                            if(_new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k+1])){ // if so add the other

                                isDone = false;
                                capVertpolygon.Add(_new_vertices[k+1]);
                                capVertTracker.Add(_new_vertices[k+1]);

                            }else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k])){// if so add the other

                                isDone = false;
                                capVertpolygon.Add(_new_vertices[k]);
                                capVertTracker.Add(_new_vertices[k]);
                            }
                        }
                    }

                    bool result = FillCap(capVertpolygon);

                }
            
        }

        static void CappingOld(){

            capVertTracker.Clear();

            for(int i=0; i<_new_vertices.Count-1; i++)//dks mod , -1 needed for out of range exception
                if(!capVertTracker.Contains(_new_vertices[i]))
                {
                    capVertpolygon.Clear();
                    capVertpolygon.Add(_new_vertices[i]);
                    capVertpolygon.Add(_new_vertices[i+1]);

                    capVertTracker.Add(_new_vertices[i]);
                    capVertTracker.Add(_new_vertices[i+1]);


                    bool isDone = false;
                    while(!isDone){
                        isDone = true;

                        for(int k=0; k<_new_vertices.Count; k+=2){ // go through the pairs

                            if(_new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k+1])){ // if so add the other

                                isDone = false;
                                capVertpolygon.Add(_new_vertices[k+1]);
                                capVertTracker.Add(_new_vertices[k+1]);

                            }else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k])){// if so add the other

                                isDone = false;
                                capVertpolygon.Add(_new_vertices[k]);
                                capVertTracker.Add(_new_vertices[k]);
                            }
                        }
                    }

                    bool result = FillCap(capVertpolygon);

                }
            if (capVertTracker.Count < _new_vertices.Count/2){//dks mod
                if (isDebug) Debug.Log("******************Not all verts were added to a cap:" + capVertTracker.Count.ToString()+" != "+(_new_vertices.Count/2).ToString());
                //capVertpolygon.Clear();//super hacky capping of the leftovers
                //for (int l=0; l<_new_vertices.Count; l+=2)
                //{
                //    if (!capVertpolygon.Contains(_new_vertices[l]))
                //        capVertpolygon.Add(_new_vertices[l]);
                //}
                //FillCap(capVertpolygon);
            }
        }
        static bool VectorArrayContains(Vector3[] vecArray, Vector3 vec)
        {
            for (int i = 0; i < vecArray.Length; i++){
                Vector3 testVec = vecArray[i];
                /*if (vec.x == testVec.x)
                    if (vec.y == testVec.y)
                        if (vec.z == testVec.z)
                            return true;
                */
                if (Vector3Compare(vec,testVec))
                    return true;
            }
            return false;
        }
        static bool MagnitudeLessThan(float a, float b)
        {
            if (a < 0)
                a = -a;
            //if (b < 0)
            //    b = -b;
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

        static bool V3Equal(Vector3 a, Vector3 b){
            return (Vector3.SqrMagnitude(a - b) < 0.00000001f);
        }
        
        static void Capping2(){//rewrite Capping to use an array instead of a list
            Vector3[] capVertTrackerArray = new Vector3[_new_vertices.Count];
            Vector3[] capVertpolygonArray = new Vector3[_new_vertices.Count];
            int capVertTrackerCount = 0;
            int capVertPolygonCount = 0;
            //capVertTracker.Clear();//tracks every vert in the cap vertices list 
            //loops through new verts, if it encounters a vert that hasn't been tracked 
            //it adds the current pair of points to the current capVertPol and capVertTracker
            //loops through the entire list of new vertices in pairs and finds the one adjacent to the previous capVert
            for(int i=0; i<_new_vertices.Count-1; i++)//for every vert not already capped find all of the connected verts and cap it
            {
                if(!VectorArrayContains(capVertTrackerArray,_new_vertices[i]))
                {
                    //capVertpolygon.Clear();
                    capVertPolygonCount = 0;
                    capVertpolygonArray[capVertPolygonCount++] = _new_vertices[i];//capVertpolygon.Add(_new_vertices[i]);
                    capVertpolygonArray[capVertPolygonCount++] = _new_vertices[i+1];//capVertpolygon.Add(_new_vertices[i+1]);
                    capVertTrackerArray[capVertTrackerCount++] = _new_vertices[i];//capVertTracker.Add(_new_vertices[i]);
                    capVertTrackerArray[capVertTrackerCount++] = _new_vertices[i+1];//capVertTracker.Add(_new_vertices[i+1]);
                    if (capVertTrackerCount >= capVertTrackerArray.Length)
                        if (isDebug)
                            Debug.Log("*****bad: capVertTrackerCount:" + capVertTrackerCount.ToString()+"\n"+ "capVertTrackerArray.Length:" + capVertTrackerArray.Length.ToString());
                    bool isDone = false;
                    while(!isDone){
                        isDone = true;
                        for(int k=i - (i%2); k<_new_vertices.Count-1; k+=2){ // go through the pairs, searching for the adjacent vert and add to the poly list 
                            //dks- changed k=0 to k=i-(i%2) to avoid a lot of wasted time --dks 6/14/2018 don't understand the weird loop logic
                            //since cuts produce 2 new verts that share an edge this might actually result in good tris and might be an actual fan
                            //if(_new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k+1])){ 
                            //if(_new_vertices[k] == capVertpolygonArray[capVertPolygonCount-1] && !VectorArrayContains(capVertTrackerArray,_new_vertices[k+1])){ 
                            if(Vector3Compare(_new_vertices[k],capVertpolygonArray[capVertPolygonCount-1]) && !VectorArrayContains(capVertTrackerArray,_new_vertices[k+1])){ 

                                //if the current point is the last one in the cap and the next point is currently untracked...
                                float dot = Vector3.Dot ((
                                    //capVertpolygonArray [capVertPolygonCount - 1] - capVertpolygonArray [capVertPolygonCount - 2]
                                    new Vector3(
                                        capVertpolygonArray [capVertPolygonCount - 1].x - capVertpolygonArray [capVertPolygonCount - 2].x,
                                        capVertpolygonArray [capVertPolygonCount - 1].y - capVertpolygonArray [capVertPolygonCount - 2].y,
                                        capVertpolygonArray [capVertPolygonCount - 1].z - capVertpolygonArray [capVertPolygonCount - 2].z
                                        )
                                    ).normalized,(
                                    //_new_vertices [k + 1] - capVertpolygonArray [capVertPolygonCount - 1]
                                    new Vector3(
                                        _new_vertices [k + 1].x - capVertpolygonArray [capVertPolygonCount - 1].x,
                                        _new_vertices [k + 1].y - capVertpolygonArray [capVertPolygonCount - 1].y,
                                        _new_vertices [k + 1].z - capVertpolygonArray [capVertPolygonCount - 1].z
                                        )
                                    ).normalized
                                    );
                                if ( dot == 1)//if the previous cap segment is colinear with the new segment,
                                {   //move the previous poly point to the new point
                                    capVertpolygonArray[capVertPolygonCount-1] = _new_vertices[k+1];
                                    capVertTrackerArray[capVertTrackerCount++] = _new_vertices[k+1];//capVertTracker.Add(_new_vertices[k+1]);
                                } else { //else grab that new poly point, add to the polygon and add to the tracked list as well
                                    capVertpolygonArray[capVertPolygonCount++] = _new_vertices[k+1];//capVertpolygon.Add(_new_vertices[k+1]);
                                    capVertTrackerArray[capVertTrackerCount++] = _new_vertices[k+1];//capVertTracker.Add(_new_vertices[k+1]);
                                }
                                isDone = false;//if we've added a new point we aren't done
                            //} else if(_new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(_new_vertices[k])){// if so add the other
                            } else if(Vector3Compare(_new_vertices[k+1],capVertpolygonArray[capVertPolygonCount-1]) && !VectorArrayContains(capVertTrackerArray,_new_vertices[k])){// if so add the other
                                float dot = Vector3.Dot (
                                    new Vector3(
                                        capVertpolygonArray [capVertPolygonCount - 1].x - capVertpolygonArray [capVertPolygonCount - 2].x,
                                        capVertpolygonArray [capVertPolygonCount - 1].y - capVertpolygonArray [capVertPolygonCount - 2].y,
                                        capVertpolygonArray [capVertPolygonCount - 1].z - capVertpolygonArray [capVertPolygonCount - 2].z
                                            ).normalized,
                                                //(_new_vertices [k] - capVertpolygonArray [capVertPolygonCount - 1]).normalized);
                                                (new Vector3(
                                                    _new_vertices [k].x - capVertpolygonArray [capVertPolygonCount - 1].x,
                                                    _new_vertices [k].y - capVertpolygonArray [capVertPolygonCount - 1].y,
                                                    _new_vertices [k].z - capVertpolygonArray [capVertPolygonCount - 1].z
                                                    ).normalized));

                                if (dot == 1){//colinear edges move the previous poly point to the new point
                                            capVertpolygonArray[capVertPolygonCount-1] = _new_vertices[k];
                                            capVertTrackerArray[capVertTrackerCount++] = _new_vertices[k];//capVertTracker.Add(_new_vertices[k]);
                                        } else {
                                            capVertpolygonArray[capVertPolygonCount++] = _new_vertices[k];//capVertpolygon.Add(_new_vertices[k]);
                                            capVertTrackerArray[capVertTrackerCount++] = _new_vertices[k];//capVertTracker.Add(_new_vertices[k]);
                                        }
                                isDone = false;//if we've added a new point we aren't done
                            }
                        }
                    }//while loop looking for neighboring vertices that are not already tracked 
                    List<Vector3> tempList = new List<Vector3>();
                    tempList.AddRange(capVertpolygonArray);
                    //for (int i = tempList.Count - 1; i > capVertPolygonCount; i--)
                    tempList.RemoveRange(capVertPolygonCount, tempList.Count - capVertPolygonCount);
                    //Debug.Log("capVertPolygonCount:"+capVertPolygonCount.ToString()+"\nnewVertCount:"+_new_vertices.Count.ToString());

                    bool result = FillCap(tempList);
                    //i+=2;//dks //6/24/2018 commented this out because I'm wondering if it will fix some holes in the caps
                }//if contains new
            }//for i
        }

        static Vector3 previousCenterPoint = Vector3.zero;
		static bool FillCap(List<Vector3> vertices)
        {//send 3 adjacent points
            if (previousCenterPoint == Vector3.zero)
                previousCenterPoint = fillCapCenterBak;
			//capping finds the average (center) point
			int flipLeft = -1;// should be 0
			int flipRight = -1;// should be 1 but maybe check it just in case
			// center of the cap
			Vector3 center = fillCapCenterBak;//Vector3.zero;//weird backed up center point
            if (vertices.Count >= 3)
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    center.x += vertices[i].x;
                    center.y += vertices[i].y;
                    center.z += vertices[i].z;
                }
                center = center/vertices.Count;
                previousCenterPoint = center;
            } else {
                if (isDebug) Debug.Log("fillcap got fewer than 3 verts");
                    center = previousCenterPoint;//previousCenterPoint;
            }
            float dist = Mathf.Abs(_blade.GetDistanceToPoint(center));
            if (dist > 0.001)
            {
                if (isDebug) Debug.Log("fillcap re-centering center point, dist="+dist.ToString());
                center = _blade.ClosestPointOnPlane(center);
            }

            //foreach(Vector3 point in vertices)
            //	center += point;


			// you need an axis based on the cap
			Vector3 upward = Vector3.zero;
			// 90 degree turn
			upward.x = _blade.normal.y;
			upward.y = -_blade.normal.x;
			upward.z = _blade.normal.z;
			Vector3 left = Vector3.Cross(_blade.normal, upward);

			Vector3 displacement = Vector3.zero;
			Vector2 newUV_1 = Vector2.zero;
			Vector2 newUV_2 = Vector2.zero;

            Vector2 newUV2_1 = Vector2.one;
            Vector2 newUV2_2 = Vector2.one;    
            Vector2 hotUV2 = new Vector2(1,1);//Vector2.one; //setting V to 1 prevents it from displacing

			for(int i=0; i<vertices.Count; i++){
                //each face needs to be checked because I rearranged the points in the cutting operation which means it could be on either side
                //flipLeft = -1;//undoing my old optimization because it was failing
                //flipRight = -1;//undoing my old optimization because it let some backfacing tris through
				displacement = new Vector3(
                    vertices[i].x - center.x,
                    vertices[i].y - center.y,
                    vertices[i].z - center.z
                    );
				newUV_1 = Vector3.zero;
				newUV_1.x = 0.5f + Vector3.Dot(displacement, left);
				newUV_1.y = 0.5f + Vector3.Dot(displacement, upward);

				//newUV1.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

				displacement = new Vector3(
                    //vertices[(i+1) % vertices.Count] - center
                    vertices[(i+1) % vertices.Count].x - center.x,
                    vertices[(i+1) % vertices.Count].y - center.y,
                    vertices[(i+1) % vertices.Count].z - center.z
                    );
				newUV_2 = Vector3.zero;
				newUV_2.x = 0.5f + Vector3.Dot(displacement, left);
				newUV_2.y = 0.5f + Vector3.Dot(displacement, upward);
				//newUV2.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

				Vector3[] final_verts = new Vector3[]{vertices[i], vertices[(i+1) % vertices.Count], center};
				Vector3[] final_verts_backup = new Vector3[]{vertices[i], vertices[(i+1) % vertices.Count], center};

				Vector3[] final_norms = new Vector3[]{-_blade.normal, -_blade.normal, -_blade.normal};
				Vector3[] final_norms_right = new Vector3[]{_blade.normal, _blade.normal, _blade.normal};

				Vector2[] final_uvs   = new Vector2[]{newUV_1, newUV_2, new Vector2(0.5f, 0.5f)};
				Vector2[] final_uvs_backup   = new Vector2[]{newUV_1, newUV_2, new Vector2(0.5f, 0.5f)};

                Vector2[] final_uv2 = new Vector2[]{hotUV2, hotUV2, hotUV2};//this should make the new cap appear hot
				
				//Vector4   myTangent = new Vector4(1,0,0,-1);
				Vector3 myTangent = Vector3.Cross( -_blade.normal, Vector3.forward );

				if( myTangent.magnitude == 0 )
				{
					myTangent = Vector3.Cross( -_blade.normal, Vector3.up );
				}
				// Vector4[] final_tangents = new Vector4[]{ Vector4.zero, Vector4.zero, Vector4.zero};
				
				Vector4[] final_tangents = new Vector4[]{ myTangent, myTangent, myTangent};

				if (flipLeft == -1)
                {
					if(Vector3.Dot(Vector3.Cross(
                        new Vector3(
                            final_verts[1].x - final_verts[0].x,
                            final_verts[1].y - final_verts[0].y,
                            final_verts[1].z - final_verts[0].z
                            ), 
                        new Vector3(
                            final_verts[2].x - final_verts[0].x,
                            final_verts[2].y - final_verts[0].y,
                            final_verts[2].z - final_verts[0].z
                            )
                        )
                    , final_norms[0]) < 0)
						flipLeft = 1;
					else
						flipLeft = 0;
				}
				//if (flipLeft==1)
				//	FlipFace(final_verts, final_norms, final_uvs, final_tangents);
				

				_leftSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents,
					_capMatSub);

				if (flipRight == -1)
                {
					if ( Vector3.Dot(Vector3.Cross(
                         new Vector3(
                            final_verts[1].x - final_verts[0].x,
                            final_verts[1].y - final_verts[0].y,
                            final_verts[1].z - final_verts[0].z
                            ),
                         new Vector3(
                            final_verts[2].x - final_verts[0].x,
                            final_verts[2].y - final_verts[0].y,
                            final_verts[2].z - final_verts[0].z
                            )
                         ),
                          final_norms_right[0]) < 0
                    )
						flipRight = 1;
					else
						flipRight = 0;
				}
				final_norms = final_norms_right;

				if (flipRight == 1){
					if (flipLeft == 1){ //if left was flipped simply revert to unflipped
						final_verts = final_verts_backup;
						final_uvs = final_uvs_backup;
					} else {
						FlipFace(final_verts, final_norms, final_uvs, final_tangents);
					}
				}

				_rightSide.AddTriangle(final_verts, final_norms, final_uvs, final_uv2, final_tangents,_capMatSub);
				
				//}
			}
		return true;

		}

		        //need a function that returns the next available chunk in the list
        public static GameObject GetChunk(){
        	//return item 0 and then shift all items down
        	GameObject nextChunk = chunks[0];
        	//bool clash = false;
        	bool sacrifice = false;
        	if (currentVictim == nextChunk){
        		GameObject swap = chunks[0];
        		chunks[0]=chunks[1];
        		chunks[1]=swap;
        		nextChunk = chunks[0];
        		//clash = true;
        	}
            if (nextChunk == null)
            {
                nextChunk = MeshCutRepairChunk(0);
            }

            {
                if (nextChunk.activeInHierarchy)
                {
                    //find next inactive
                    bool found = false;
                    for (int i = 0; i < chunksPoolSize; i++)
                    {
                        if (!chunks[i].activeInHierarchy)//if it finds an available chunk move it to the first position
                        {
                            GameObject swap = chunks[0];
                            chunks[0] = chunks[i];
                            chunks[i] = swap;
                            nextChunk = chunks[0];
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        if (isDebug)
                            Debug.Log("sacrificing a cut because no chunks are available: " + nextChunk.name.ToString());
                        sacrifice = true;
                    }
                }
            }
            //shift the list
            GameObject swapShift = chunks[0];
            for(int j=0; j<chunksPoolSize-1;j++){//shift the list by 1
                chunks[j]=chunks[j+1];
            }
            chunks[chunksPoolSize-1]=swapShift;//moved to the end of the line

            nextChunk = chunks[chunksPoolSize-1];//making sure it's still assigned to the same object
			
            KillSpark ks = nextChunk.GetComponent<KillSpark>();
	        if (ks != null){
                MonoBehaviour.Destroy (ks);//this seems to be correcting the clingy Killspark script
                if (isDebug)
                	Debug.Log("Found KillSpark: " + nextChunk.name.ToString());
	        }
	        if (isDebug)
	        	Debug.Log("Getting Chunk: " + nextChunk.name.ToString());
            
            nextChunk.SetActive(true);
            //clean up the list
            int swapIndex = chunksPoolSize - 3;
            int endIndex = chunksPoolSize - 2;
            for (int i = 0; i < endIndex; i++)
            {
                if (chunks[swapIndex] == null) chunks[swapIndex] = MeshCutRepairChunk(swapIndex);//stupid bandaid hack!
                
                while (chunks[swapIndex].activeInHierarchy)
                {

                    swapIndex--;
                    if (chunks[swapIndex] == null) chunks[swapIndex] = MeshCutRepairChunk(swapIndex);
                    
                }//keep going backwards until you find a swappable item
                if (i < swapIndex)
                {
                    if (chunks[i].activeInHierarchy)
                    {
                        GameObject temp = chunks[i];
                        chunks[i] = chunks[swapIndex];
                        chunks[swapIndex--] = temp;
                        endIndex--;
                    }
                }
                else
                {
                    break;//leave for loop
                }
            }
            //kill what's necessary to have a buffer
            for (int i=0; i<chunksBufferSize; i++)
            {
                if (chunks[i].activeInHierarchy)
                    Destroy(chunks[i]);
            }
            if (sacrifice)
            {
                return null;
            }
        	return nextChunk;
        }

        public static void Destroy(GameObject go){

            if (!go.GetComponent<MeshCutChunk>()){//if it bleeds we can kill it
            	MonoBehaviour.Destroy(go);
            } else {
                Transform _goTransform = go.transform;
				_goTransform.position = Vector3.zero;
            	_goTransform.localPosition = Vector3.zero;
            	_goTransform.rotation = Quaternion.identity;
            	_goTransform.localRotation = Quaternion.identity;
            	_goTransform.localScale = Vector3.one;
	            go.GetComponent<MeshFilter>().mesh = null;
	            Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb) {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
	            MeshCollider mc = go.GetComponent<MeshCollider>();
	            if (mc)
	            	MonoBehaviour.Destroy (mc);
	            KillSpark ks = go.GetComponent<KillSpark>();
	            if (ks != null)
                	MonoBehaviour.Destroy (ks);
                JointConnection jc = go.GetComponent<JointConnection>();
                if (jc != null) {
                    jc.joint.connectedBody = null;
                    MonoBehaviour.Destroy (jc);
                }
                //Material[] mats = go.GetComponent<MeshRenderer>().materials;//this might be generating garbage
                //Material[] smats = go.GetComponent<MeshRenderer>().sharedMaterials;
                //if (mats!=null)//commented out because I think this was generating garbage
                	go.GetComponent<MeshRenderer>().materials = matsRefresh;
                Cuttable cc = go.GetComponent<Cuttable>();
                if (cc != null)
                	MonoBehaviour.Destroy(cc);
	           	go.SetActive(false);
            }
        }

        public static void Recycle(GameObject go){
            Transform _goTransform = go.transform;
            _goTransform.position = Vector3.zero;
            _goTransform.localPosition = Vector3.zero;
            _goTransform.rotation = Quaternion.identity;
            _goTransform.localRotation = Quaternion.identity;
            _goTransform.localScale = Vector3.one;
            go.GetComponent<MeshFilter>().mesh = null;
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb) {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc)
                MonoBehaviour.Destroy (mc);
            KillSpark ks = go.GetComponent<KillSpark>();
            if (ks != null)
                MonoBehaviour.Destroy (go.GetComponent<KillSpark> ());
            JointConnection jc = go.GetComponent<JointConnection>();
            if (jc != null) {
                jc.joint.connectedBody = null;
                MonoBehaviour.Destroy (jc);
            }
            //Material[] mats = go.GetComponent<MeshRenderer>().materials;//this might be generating garbage
            //Material[] smats = go.GetComponent<MeshRenderer>().sharedMaterials;
            //if (mats!=null)//commented out because I think this was generating garbage
                go.GetComponent<MeshRenderer>().materials = matsRefresh;
            //Cuttable cc = go.GetComponent<Cuttable>();
            //if (cc != null)
            //    MonoBehaviour.Destroy(cc);
            go.SetActive(false);
        }

        public static void RenameChildren(Transform parentTransform){
        	Transform[] transforms = parentTransform.GetComponentsInChildren<Transform>();
        	for (int i=1; i<transforms.Length; i++){
        		transforms[i].name = (transforms[i].name+":cut:"+numRenamed++.ToString());
        		//RenameChildren(transforms[i]);
        	}
        }

        public static float FastSqrtInvAroundOne(float x)
        {
            const float a0 = 15.0f / 8.0f;
            const float a1 = -5.0f / 4.0f;
            const float a2 = 3.0f / 8.0f;

            return a0 + a1 * x + a2 * x * x;
        }

        public static Vector3 FastNormalize(ref Vector3 v)
        {
            float len_sq = v.x * v.x + v.y * v.y + v.z * v.z;
            float len_inv = FastSqrtInvAroundOne(len_sq);
            return new Vector3(v.x * len_inv, v.y * len_inv, v.z * len_inv);
        }
        //      public static void CopyJoint(GameObject go){
        // go.transform.position = Vector3.zero;
        //      	go.transform.localPosition = Vector3.zero;
        //      	go.transform.rotation = Quaternion.identity;
        //      	go.transform.localRotation = Quaternion.identity;
        //      	go.transform.localScale = Vector3.one;
        //          go.GetComponent<MeshFilter>().mesh = null;
        //          Rigidbody rb = go.GetComponent<Rigidbody>();
        //          if (rb) {
        //              rb.velocity = Vector3.zero;
        //              rb.angularVelocity = Vector3.zero;
        //          }
        //              //	MonoBehaviour.Destroy(rb);//going to keep rb
        //          MeshCollider mc = go.GetComponent<MeshCollider>();
        //          if (mc)
        //          	MonoBehaviour.Destroy (mc);//not sure if I should delete the meshcollider
        //          KillSpark ks = go.GetComponent<KillSpark>();
        //          if (ks != null)
        //          	MonoBehaviour.Destroy (go.GetComponent<KillSpark> ());
        //          JointConnection jc = go.GetComponent<JointConnection>();
        //          if (jc != null) {
        //              jc.joint.connectedBody = null;
        //              MonoBehaviour.Destroy (jc);
        //          }
        //          Material[] mats = go.GetComponent<MeshRenderer>().materials;
        //          Material[] smats = go.GetComponent<MeshRenderer>().sharedMaterials;
        //          if (mats!=null)
        //          	go.GetComponent<MeshRenderer>().materials = matsRefresh;
        //          //if (smats != null)
        //          	//go.GetComponent<MeshRenderer>().sharedMaterials = new Material[0];
        //          Cuttable cc = go.GetComponent<Cuttable>();
        //          if (cc != null)
        //          	MonoBehaviour.Destroy(cc);
        //         	//go.SetActive(false);
        //      }
    }
}
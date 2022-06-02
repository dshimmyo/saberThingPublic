using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;//job system
using UnityEngine.Jobs;//job system
using Unity.Collections;//job system
using Unity.Burst;

public class SaberFloor : MonoBehaviour, IBurnable
{

    //private Texture2D workingTexture;
    private Renderer mioRenderer;
    //private Texture2D texture;
    public static Gradient burnGradient;
    //private Texture2D emissionTexture;
    //private Texture2D workingEmissionTexture;
    private Texture2D heatMapTexture;//new heatBurnAlpha, hba
    private Texture2D workingHeatMapTexture;//new hba
    private Color32 baseEmission = new Color32(0, 0, 0, 255);// Color32(0,0,0,255);//make the 
    [SerializeField] private bool isEmissionCooling = false;
    [SerializeField] private bool hotPixelsFound = false;
    [SerializeField] private bool applyEmissionUpdate = false;
    [SerializeField] private bool applyWorkingTextureUpdate = false;
    int spotWidth = 10; //spot width in pixels
    float spotUVWidth = .01f;
    int emissionSpotWidth = 10;//allows us to have a lower res emission map
    float updateInterval = .04f;//.04 -> 24fps
    float timeOfLastUpdate = 0;
    float updateTimer = 0;
    [SerializeField] private float minSpotRadiusMultiplier = .3f;
    [SerializeField] private float coolingFactor = .4f;
    [SerializeField] private float heatingFactor = .2f;//was .2
    [SerializeField] private bool useJobSystem = true;
    [SerializeField] private bool useJobbieSpots = false;
    [SerializeField] private bool usePaintLines = false;
    [SerializeField] private bool useLineJob = false;
    [SerializeField] private bool isFilterBeforeLineJob = false;
    private static Dictionary<string, float[]> blenders = new Dictionary<string, float[]>();
    private Color32 myBlack = new Color(.3f, .25f, .2f);
    private struct BurnSpot
    {
        public Vector2 pixelUV;
        public Vector2 pixelUV2;
        public Color32 color;
        public float probability;
        public int pixelWidth;

        public BurnSpot(Vector2 myPixelUV, int myWidth, Color32 myColor, float myProbability)
        {
            pixelUV.x = myPixelUV.x;
            pixelUV.y = myPixelUV.y;
            pixelUV2 = new Vector2(-1,-1);
            pixelWidth = myWidth;
            color = myColor;
            probability = myProbability;
        }

        public BurnSpot(Vector2 myPixelUV, Vector2 myPixelUV2, int myWidth, Color32 myColor, float myProbability)
        {
            pixelUV.x = myPixelUV.x;
            pixelUV.y = myPixelUV.y;
            pixelUV2.x = myPixelUV2.x;
            pixelUV2.y = myPixelUV2.y;
            pixelWidth = myWidth;
            color = myColor;
            probability = myProbability;
        }

    }
    private List<BurnSpot> burnSpotsQueue = new List<BurnSpot>();

    //private int _textureWidth = 0;
    //private int _textureHeight = 0;
    //private int _emissionTextureWidth = 0;
    //private int _emissionTextureHeight = 0;
    private int _heatMapTextureWidth = 0;
    private int _heatMapTextureHeight = 0;

    private int consecutiveUpdateFrames = 0;
    private float timeSinceLastCooldown = 0;
    [SerializeField] private bool useBruteForce = false;

    private bool altPixBool = false;
    private int altPixInt = 0; //try mod 4

    //private NativeArray<Color32> data;
    //private NativeArray<Color32> emissionData;
    private NativeArray<Color32> heatMapData;
    private NativeArray<float> heatArray;//I think this performs better as a nativeArray, it just needs to be disposed of 
    //private NativeArray<float> maxHeatArray;//use this as the alpha in the emmission alpha channel
    //private Color32[] data;
    //private Color32[] emissionData;
    //private float[] heatArray;


    public enum actions { EmissionUpdate, EmissionApply, ColorApply, CoolingAndColor, PaintSpotsAction, PaintSpotsJob, DummyApply,None };//three major function calls that should be alternated

    private actions[] actionQueue = new actions[10];//just in case
    //[SerializeField] 
    //private List<int> hotPixelMap = new List<int>();
    private HashSet<int> hotPixelHash = new HashSet<int>();
    private List<int> hotSpotMap = new List<int>();//to replace paintspots
    private static Dictionary<int, int> hotSpotMapIndexDictionary = new Dictionary<int, int>();//key: pixelIndex value: hotSpotMapIndex
    private List<float> hotSpotBlenders = new List<float>();

    private float colorUpdateTimer = 0;
    private int lastUpdateFrame = 0;

    private int MeasureUVs(int pixelWidth)
    {//return a width in pixels of a .1 unit spot
        Collider col = gameObject.GetComponent<Collider>();
        RaycastHit hit = new RaycastHit();
        RaycastHit hit1 = new RaycastHit();
        bool rayResult = false;
        bool rayResult1 = false;
        Renderer rend = GetComponent<Renderer>();
        Vector3 center = rend.bounds.center;
        Ray ray = new Ray(gameObject.transform.position + new Vector3(0, 5f, 0), Vector3.down);//origin, direction
        Ray ray1 = new Ray(gameObject.transform.position + new Vector3(0, 5f, 0) + (new Vector3(.1f, 0, 0)), Vector3.down);//origin, direction

        rayResult = col.Raycast(ray, out hit, 5);//ray, hitinfo, maxdistance
        rayResult1 = col.Raycast(ray1, out hit1, 5);
        if (rayResult && rayResult1)
        {
            Vector2 uv1 = hit.textureCoord;
            Vector2 uv2 = hit1.textureCoord;
            float uvdistance = Vector2.Distance(uv1, uv2);
            spotUVWidth = uvdistance * .5f;
            int pixelDistance = (int)Mathf.Round(uvdistance * pixelWidth);
            return pixelDistance;
        }
        else
        {
            return 10;
        }

    }

    private int MeasureUVsFromMesh(int pixelWidth)
    {
        //return pixel width of .1 units distance
        Mesh _mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] _mesh_vertices = _mesh.vertices;
        Vector2[] _mesh_uv = _mesh.uv;
        if (_mesh_vertices.Length >= 2)
        {
            Vector3 a = _mesh.vertices[0];
            Vector3 b = _mesh.vertices[_mesh.vertexCount - 1];
            Vector2 uvA = _mesh_uv[0];
            Vector2 uvB = _mesh_uv[_mesh.vertexCount - 1];
            float uvSize = Vector2.Distance(uvA, uvB) / (Vector3.Distance(transform.TransformPoint(a), transform.TransformPoint(b)));
            int pixelSize = (int)Mathf.Round((uvSize * .1f) * pixelWidth);
            return pixelSize;
        }
        else
        {
            return 0;
        }

    }
    /*public void OnDisable()
    {
        data.Dispose();
        emissionData.Dispose();
    }*/
    /*
    private void InitEmissionTextureData(Color32 color)
    {
        color.a = (byte) 0;
        emissionData = workingEmissionTexture.GetRawTextureData<Color32>();//using a local variable creates garbage but it's a lot faster
        int count = 0;
        for (int y = 0; y < _emissionTextureHeight; y++)
        {
            for (int x = 0; x < _emissionTextureWidth; x++)
            {
                emissionData[count++] = color;
            }
        }
        workingEmissionTexture.Apply();
    }*/

    private void InitHeatMapTextureData()
    {
        heatMapData = workingHeatMapTexture.GetRawTextureData<Color32>();
        int count = 0;
        for (int y = 0; y < _heatMapTextureHeight; y++)
        {
            for (int x = 0; x < _heatMapTextureWidth; x++)
            {
                heatMapData[count++] = Color.black;
            }
        }
        workingHeatMapTexture.Apply();
    }

    private void AddActionToQ(actions myAction, actions[] q)
    {
        if (myAction != q[0])//prevent
        {
            for (int i = 0; i < q.Length; i++)
            {
                if (actionQueue[i] == actions.None)
                {
                    actionQueue[i] = myAction;
                    break;
                }
            }
        }
    }

    public bool IsActionsInQ()
    {
        if (actionQueue[0] != actions.None)
            return true;
        return false;
    }

    public bool GetIsCooling()
    {
        return isEmissionCooling;
    }

    actions GetNextActionFromQ(actions[] q) //basically shifts the array left and removes the next item
    {
        actions nextAction = q[0];
        for (int i = 0; i < q.Length - 1; i++)
        {
            q[i] = q[i + 1];
        }
        q[q.Length - 1] = actions.None;
        return nextAction;
    }

    void Start()
    {

        for (int i = 0; i < actionQueue.Length; i++)
            actionQueue[i] = actions.None;
        GradientSetup();

        mioRenderer = GetComponent<Renderer>();
        //texture = mioRenderer.material.GetTexture("_BaseMap") as Texture2D;// change this to the new heatMap texture
        //emissionTexture = mioRenderer.material.GetTexture("_EmissionMap") as Texture2D;  //eventually retire this
        heatMapTexture = mioRenderer.material.GetTexture("_HeatMap") as Texture2D;
       
        //_textureWidth = texture.width;
        //_textureHeight = texture.height;
        //_emissionTextureWidth = emissionTexture.width;
        //_emissionTextureHeight = emissionTexture.height;
        _heatMapTextureWidth = heatMapTexture.width;
        _heatMapTextureHeight = heatMapTexture.height;

        //spotWidth = MeasureUVs(_textureWidth); //measure how many pixels per spot size of .1 meters? theoretically 10-ish
        spotWidth = MeasureUVsFromMesh(_heatMapTextureWidth);
        emissionSpotWidth = MeasureUVsFromMesh(_heatMapTextureWidth);
        int initSpotWidth = emissionSpotWidth;
        if (spotWidth > emissionSpotWidth)
            initSpotWidth = spotWidth;
        //workingTexture = new Texture2D(_textureWidth, _textureHeight);
        //workingEmissionTexture = new Texture2D(_emissionTextureWidth, _emissionTextureHeight);
        workingHeatMapTexture = new Texture2D(_heatMapTextureWidth, _heatMapTextureHeight);
        //Color32[] sourcePixels = texture.GetPixels32();
        //Color[] sourceEmissionPixels = emissionTexture.GetPixels();

        //workingTexture.SetPixels32(sourcePixels);
        //data = workingTexture.GetRawTextureData<Color32>();
        //workingTexture.Apply();

        //InitEmissionTextureData(new Color32(0, 0, 0, 255));
        InitHeatMapTextureData();
        //mioRenderer.material.SetTexture("_BaseMap", workingTexture);
        //mioRenderer.material.SetTexture("_EmissionMap", workingEmissionTexture);
        mioRenderer.material.SetTexture("_HeatMap", workingHeatMapTexture);

        //init heat array
        heatArray = new NativeArray<float>(_heatMapTextureWidth * _heatMapTextureHeight,Allocator.Persistent);
        for (int y = 0; y < _heatMapTextureHeight; y++)
        {
            for (int x = 0; x < _heatMapTextureWidth; x++)
            {
                heatArray[y * _heatMapTextureHeight + x] = 0;
            }
        }
        StartCoroutine(InitSpots(initSpotWidth));

    }

    public void OnApplicationQuit()
    {
        heatArray.Dispose();
        //maxHeatArray.Dispose();
        heatMapData.Dispose();
    }
    public void Burn(RaycastHit[] hits)
    {
        //do nothing until needed
    }
    public void Burn(RaycastHit hit)
    {//IBurnable interface
        //make a burn mark at the hit location;
        Vector2 pixelUV;
        pixelUV = hit.textureCoord;
        PaintSpots(pixelUV, Color.black);
    }

    public void Burn(RaycastHit hit, RaycastHit previousHit)//overloaded version for burning a line
    {//IBurnable interface
        //make a burn mark at the hit location;
        Vector2 pixelUV;
        pixelUV = hit.textureCoord;
        Vector2 pixelUV2;
        pixelUV2 = previousHit.textureCoord;//(previousHit.textureCoord + pixelUV) / 2;
        float uvDistanceBetweenSpots = Vector2.Distance(pixelUV, pixelUV2) * 1.5f;//2//1.5f;//trick them into overlapping
        if (uvDistanceBetweenSpots > spotUVWidth)//spotUVWidth is actually the radius
        {
            if (usePaintLines)
            {
                float lineSegmentWidthMultiplier = 20f;
                int numSpots = Mathf.CeilToInt(uvDistanceBetweenSpots / (spotUVWidth * lineSegmentWidthMultiplier));
                float lineRadiusMultiplier = Mathf.Lerp(1, .25f, uvDistanceBetweenSpots / (1.5f * 200f));//was 1, .05f

                Vector2 uvIncrement = (pixelUV2 - pixelUV) / numSpots;
                Vector2 newPixelUV = pixelUV;
                Vector2 previousPixel = pixelUV;
                for (int i = 0; i < numSpots; i++)
                {
                    newPixelUV += uvIncrement;
                    //PaintSpots(newPixelUV, Color.black, .95f);//red //.25 looks weird, .95 too expensive?
                    PaintLines(previousPixel, newPixelUV, Color.black, .95f * lineRadiusMultiplier);
                    previousPixel = newPixelUV;
                }
                //PaintLines(pixelUV, pixelUV2, Color.black, .95f);
            }
            else
            {
                int numSpots = Mathf.CeilToInt(uvDistanceBetweenSpots / spotUVWidth);
                Vector2 uvIncrement = (pixelUV2 - pixelUV) / numSpots;
                Vector2 newPixelUV = pixelUV;
                for (int i = 0; i < numSpots; i++)
                {
                    newPixelUV += uvIncrement;
                    PaintSpots(newPixelUV, Color.black, .95f);//red //.25 looks weird, .95 too expensive?
                }
            }
        }
        else
        {
            PaintSpots(pixelUV, Color.black);//boring single spot //green
        }

    }

    IEnumerator InitSpots(int dotRadius)
    {
        Debug.Log("SaberFloorInitSpots Start");
        for (int i = 2; i < dotRadius; i++)
        {
            InitSpot(i);
            yield return null;
        }
        Debug.Log("SaberFloorInitSpots End");

    }

    public static Color32 GetAshyColor(float signal)
    {
        float value = 1f - Mathf.Min(Mathf.Pow(signal / 2,2),1);
        return new Color(value,value,value);//Color32.Lerp(Color.white, Color.black, Mathf.Pow(signal / 2,2));
    }

    void InitSpot(int dotRadius)
    {
        Vector2 pixelUV = new Vector2(.5f, .5f);
        bool init = true;

        pixelUV.x *= _heatMapTextureWidth;
        pixelUV.y *= _heatMapTextureHeight;
        //check if this dotRadius is in the dictionary of blender arrays
        //blender arrays will be an array of floats with dotRadius number of elements
        //dictionary of float[dotRadius] using dotRadius as the key
        bool populateBlender = false;
        float[] blenderPopulate = new float[dotRadius * dotRadius * 4];//radius * 2 = length of each side of square
        int blenderPopulateCount = 0;
        float[] temp = null;
        if (blenders.TryGetValue(dotRadius.ToString(), out temp))
            blenderPopulate = temp;
        else
        {
            populateBlender = true;
            //Debug.Log("Polulating blender array for dotRadius "+ dotRadius.ToString());
        }
        int pixelX = (int)pixelUV.x;//hopefully get some speedup by casting only once
        int pixelY = (int)pixelUV.y;//hopefully get some speedup by casting only once
        int startIndexX = pixelX - dotRadius;
        int startIndexY = pixelY - dotRadius;
        int endIndexX = pixelX + dotRadius;
        int endIndexY = pixelY + dotRadius;
        for (int x = startIndexX; x < endIndexX; x++)
        {
            for (int y = startIndexY; y < endIndexY; y++)
            {
                if (y >= 0 && y < _heatMapTextureHeight && x >= 0 && x < _heatMapTextureWidth)
                {
                    float newPixelBlender = 0;
                    if (populateBlender)
                    {
                        float pixelDistance = Vector2.Distance(new Vector2(x, y), pixelUV);
                        newPixelBlender = Mathf.Lerp(1, 0, pixelDistance / (float)dotRadius);// setRange (pixelDistance,1,(float)dotRadius,1,0);
                        blenderPopulate[blenderPopulateCount++] = newPixelBlender;//populating blender
                    }
                }
            }
        }
        if (populateBlender)
            blenders.Add(dotRadius.ToString(), blenderPopulate);//after the array is populated
    }

    //remove populateblender stuff and ony put it into the init loop
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = false)]//gradient not supported by burst
    public struct PaintHotSpotJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int>HotPixelIndexMap;
        [ReadOnly]
        public NativeArray<float> Blenders;//
        [NativeDisableParallelForRestriction]
        public NativeArray<float> HeatSignals;
        //[NativeDisableParallelForRestriction]
        //public NativeArray<Color32> Data;//ColorData

        //[NativeDisableParallelForRestriction]//considered disabling
        //[WriteOnly]//considered disabling
        //public NativeArray<Color32> EmissionData;//considered disabling
        [NativeDisableParallelForRestriction]//considered disabling
        //[WriteOnly]
        public NativeArray<Color32> HeatMapData;
        public Color32 MyBlack;
        public float HeatingFactor;

        public void Execute(int index)
        {
            float newPixelBlender = Blenders[index];//
            int pixelIndex = HotPixelIndexMap[index];//
            float blendedHeatSignal = HeatSignals[pixelIndex] + newPixelBlender * HeatingFactor;// .2f;//compute the heat signal before applying color to the mainTexture
            //blendedHeatSignal = Mathf.Min(blendedHeatSignal, 2f);//compute the heat signal before applying color to the mainTexture
            
            Color32 ashyColor = GetAshyColor(blendedHeatSignal);
            Color32 ashToBlackEdgeColor = Color32.Lerp(ashyColor, MyBlack, newPixelBlender);
            //Color32 blendedPixelColor = Color32.Lerp(Data[pixelIndex], ashToBlackEdgeColor, newPixelBlender * .5f);
            Color32 heatMapColor = HeatMapData[pixelIndex];// Color.black;
            //Data[pixelIndex] = blendedPixelColor;//albedo map

            HeatSignals[pixelIndex] = blendedHeatSignal;

            //Color32 color;
            //color = GetFakeGradient(signal);//Loading from a static field `SaberFloor._color32Red` is not supported by burst
            //think it might be faster than calling a function
            Color32 color = Color.black;
            if (blendedHeatSignal <= 0)
            {
                color = Color.black;
            }
            else if (blendedHeatSignal < 1.5)//yellowToRedThreshold)
            {
                color = new Color(blendedHeatSignal * .667f, 0,0);
            }
            else if (blendedHeatSignal <= 2) //red hot at 1.5, 2 and beyond is yellow
            {
                float channel = (blendedHeatSignal - 1.5f) * .5f;
                color = new Color (1,(blendedHeatSignal - 1.5f) * .5f,0);
            } else if (blendedHeatSignal > 2)
            {
                float channel = blendedHeatSignal - 1f;
                color = new Color (channel, channel, 0);
            }
            //float ashyColorBlend = Mathf.Lerp((float) heatMapColor.g / 255f, ashToBlackEdgeColor.r
            heatMapColor.r = (byte) (Mathf.Min(blendedHeatSignal,1) * 255f);
            heatMapColor.g = (byte)(ashToBlackEdgeColor.r);//lerp it on top of the previous color using the alpha//(byte)(Mathf.Max(blendedHeatSignal, (float) heatMapColor.g / 255f) * 255f);//ashToBlackEdgeColor.g;
            heatMapColor.b = (byte)(255f * Mathf.Min(1f, newPixelBlender * .5f + (float)heatMapColor.b / 255f));//(byte)(Mathf.Max(newPixelBlender * .5, (float) heatMapColor.b/255f) * 255f);//alpha channel
            //EmissionData[pixelIndex] = color;//GetFakeGradient(blendedHeatSignal);
            HeatMapData[pixelIndex] = heatMapColor;


        }
    }
    private void ExecutePaintSpotsJob(ref NativeArray<float> myHeatArray, float[] blenders, /*ref NativeArray<Color32> myData, ref NativeArray<Color32> myEmissionData,*/ ref NativeArray<Color32> myHeatMapData)
    {
        NativeArray<int> hotSpotMapNativeArray = new NativeArray<int>(hotSpotMap.Count, Allocator.TempJob);
        NativeArray<float> nativeBlenders = new NativeArray<float>(blenders.Length, Allocator.TempJob);
        hotSpotMapNativeArray.CopyFrom(hotSpotMap.ToArray());
        nativeBlenders.CopyFrom(blenders);

        float heatLost = timeSinceLastCooldown / 20f / 2f * 2; // was 20/2 //% 4 //has been taking like 40 seconds to cool down

        PaintHotSpotJob job = new PaintHotSpotJob()
        {
            HotPixelIndexMap = hotSpotMapNativeArray,
            Blenders = nativeBlenders,
            HeatSignals = myHeatArray,//heatNativeArray,//, //parameters for job
            //Data = myData,//nativeData,
            //EmissionData = myEmissionData,//nativeEmissionData//myEmissionData//considered disabling because the cooling operation could theoretically handle this
            HeatMapData = myHeatMapData,
            MyBlack = myBlack,
            HeatingFactor = heatingFactor
        };
        JobHandle jobHandle = job.Schedule(hotSpotMap.Count, 16);//5 lines up with the alternating pixels...//4 was about 5ms I think, 16 was 4 instances and 11ms, not sure what that means, try a smaller batch size
        jobHandle.Complete();

        hotSpotMapNativeArray.Dispose();
        nativeBlenders.Dispose();

        for (int i = 0; i<hotSpotMap.Count; i++)//this is important but seems slow
        {
            //if (!hotPixelMap.Contains(hotSpotMap[i]))
            //    hotPixelMap.Add(hotSpotMap[i]);
            if (!hotPixelHash.Contains(hotSpotMap[i]))
                hotPixelHash.Add(hotSpotMap[i]);
        }

        //applyWorkingTextureUpdate = true;//test dks 07/15/2020
        applyEmissionUpdate = true;//considered disabling
        hotPixelsFound = true;

    }
    //https://www.randygaul.net/2014/07/23/distance-point-to-line-segment/
    //Given the vector d the distance from p to ab is just sqrt( dot( d, d ) ). 
    //The sqrt operation can be omit entirely to compute a distance squared. 
    //Our function may now look like:
    public static float DistanceSquaredPtLine( Vector2 a, Vector2 b, Vector2 p )
    {
        Vector2 n = b - a;
        Vector2 pa = a - p;
        Vector2 c = n * (Vector2.Dot( pa, n ) / Vector2.Dot( n, n ));
        Vector2 d = pa - c;
        return Vector2.Dot(d, d);// Mathf.Sqrt( Vector2.Dot( d, d ) );
    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = false)]//gradient not supported by burst
    public struct ComputeLineBlendersJob : IJobParallelFor
    {
        [ReadOnly]
        public Vector2 P1;
        [ReadOnly]
        public Vector2 P2;
        [ReadOnly]
        public NativeArray<Vector2>PixelCoordinateArray;//basically similar to the map concept
        [WriteOnly]
        public NativeArray<float> Blenders;//
        [ReadOnly]
        public NativeArray<float> SqrDistance;
        [ReadOnly]
        public float DotRadius;
        [ReadOnly]
        public float DotRadiusSq;
        [ReadOnly]
        public float Probability;

        public void Execute(int index)
        {

            Vector2 p = PixelCoordinateArray[index];
            float sqrDist = SqrDistance[index];// DistanceSquaredPtLine(P1,P2,p);
            float sqrSqrDist = sqrDist * sqrDist;

            if (sqrDist < 0)
                sqrDist = DistanceSquaredPtLine(P1, P2, p);
            if (sqrDist <= DotRadiusSq)
            {
                float p1MinusPSqrMag = (P1 - p).sqrMagnitude;
                float sinArg1 = p1MinusPSqrMag * .025f;//.025 should bring 3.14 down to about 8cm wave
                float sinArg2 = p1MinusPSqrMag * .005f;//.005 should be about 1.5cm wave
                float sinWave = Mathf.Sin(sinArg1) * 5f + Mathf.Sin(sinArg2) * 2.0f;//Mathf.Sin((P1 - p).sqrMagnitude * .3f) * 5f/*amp*/ + Mathf.Sin((P1 - p).sqrMagnitude * 1.5f) * 3f - 3;
                float dist = sqrSqrDist;//sqrDist;//Mathf.Sqrt(sqrDist);
                float dotRadiusPlusWave = DotRadius + sinWave;
                //float distDivRadius = dist / dotRadiusPlusWave;
                float distDivRadius = sqrDist / dotRadiusPlusWave;
                //float distDivRadius = sqrSqrDist / (DotRadiusSq + sinWave);

                float newPixelBlender = Mathf.Lerp(1, 0, distDivRadius);
                newPixelBlender *= newPixelBlender;//makes the signal strong in the center and then drop off around the radius
                newPixelBlender *= Probability;
                float pMinusP1DotP2MinusP1 = Vector2.Dot(p - P1, P2 - P1);
                float pMinusP2DotP1MinusP2 = Vector2.Dot(p - P2, P1 - P2);
                if (pMinusP1DotP2MinusP1 > 0 && pMinusP2DotP1MinusP2 > 0)
                {
                    Blenders[index]=newPixelBlender;
                }
                else if (pMinusP1DotP2MinusP1 <= 0)//outside of point pixelUV, make a half spot to cap the front
                {
                    float pixelDistance = (p - P1).magnitude;
                    newPixelBlender *= Mathf.Lerp(1, 0, pixelDistance / DotRadius);
                    Blenders[index]=newPixelBlender;
                }
                else if (pMinusP2DotP1MinusP2 <= 0)//outside of point pixelUV2, make a half spot to cap the back
                {
                    float pixelDistance = (p - P2).magnitude;
                    newPixelBlender *= Mathf.Lerp(1, 0, pixelDistance / DotRadius);    
                    Blenders[index]=newPixelBlender;
                }
                else Blenders[index]=0f;
            }//first filter : sqrDist <= DotRadius
        }
    }

    int[] lineJobPixelMap = new int[100];
    void PaintSpotsJobbie(Vector2 pixelUV, Vector2 pixelUV2,int dotRadius, Color32 bloodColor, float probability)
    {
        float localDotRadiusFloat = (float)dotRadius * probability;
        int localDotRadius = Mathf.CeilToInt(localDotRadiusFloat);

        float dotRadiusSq = localDotRadiusFloat * localDotRadiusFloat;
        
        bool isLine = false;
        pixelUV.x *= _heatMapTextureWidth;
        pixelUV.y *= _heatMapTextureHeight;

        if (pixelUV2.x >= 0){
            isLine = true;
            pixelUV2.x *= _heatMapTextureWidth;
        }
        if (pixelUV2.y >= 0)
            pixelUV2.y *= _heatMapTextureHeight;

        if (isLine)
        {

            int startIndexX = (int)Mathf.Min(pixelUV.x, pixelUV2.x) - localDotRadius;
            startIndexX = Mathf.Max(startIndexX,0);
            int startIndexY = (int)Mathf.Min(pixelUV.y, pixelUV2.y) - localDotRadius;
            startIndexY = Mathf.Max(startIndexY,0);
            int endIndexX = (int)Mathf.Max(pixelUV.x, pixelUV2.x) + localDotRadius;
            endIndexX = Mathf.Min(endIndexX, _heatMapTextureWidth);
            int endIndexY = (int)Mathf.Max(pixelUV.y, pixelUV2.y) + localDotRadius;
            endIndexY = Mathf.Min(endIndexY, _heatMapTextureHeight);
            int boundedAreaSize = (endIndexX - startIndexX) * (endIndexY - startIndexY);

            if (useLineJob)
            {
                //send an index map of all of the pixels in the bounded region
                //the job would return a same-sized array of the blender signals
                int numLinePixels = 0;
                //List<Vector2> pixelCoordinates = new List<Vector2>();
                NativeArray<Vector2> pixelCoordinatesMap = new NativeArray<Vector2>(boundedAreaSize, Allocator.TempJob);//send the bounded range of pixels, not the whole map
                //List<int> lineJobPixelMap = new List<int>();
                if (lineJobPixelMap.Length < boundedAreaSize)
                {
                    lineJobPixelMap = new int[boundedAreaSize];//trying to reuse the array as much as possible?
                }
                NativeArray<float> sqrDistMap = new NativeArray<float>(boundedAreaSize, Allocator.TempJob);

                //loop through every point
                for (int x = startIndexX; x < endIndexX; x++)
                {
                    for (int y = startIndexY; y < endIndexY; y++)
                    {
                        int pixelIndex = y * _heatMapTextureHeight + x;
                        Vector2 p = new Vector2(x, y);
                        bool isGood = true;
                        float sqrDist = -1;

                        if (isFilterBeforeLineJob)//measures the distance before sending to the job system
                        {
                            sqrDist = DistanceSquaredPtLine(pixelUV, pixelUV2, p);
                            if (sqrDist > localDotRadiusFloat)
                            {
                                isGood = false;
                            }
                        }

                        if (isGood) //populate the maps with index,coordinates,distance
                        {
                            lineJobPixelMap[numLinePixels] = pixelIndex;
                            pixelCoordinatesMap[numLinePixels] = new Vector2(x, y);
                            sqrDistMap[numLinePixels] = sqrDist;
                            numLinePixels++;

                        }

                    }
                }

                NativeArray<float> nativeBlendersArray = new NativeArray<float>(numLinePixels, Allocator.TempJob);//records the heat that we will add to each pixel

                ComputeLineBlendersJob job = new ComputeLineBlendersJob()
                {
                    P1 = pixelUV,
                    P2 = pixelUV2,
                    PixelCoordinateArray = pixelCoordinatesMap,// nativePixelCoordinateArray,
                    Blenders = nativeBlendersArray, //writeOnly
                    SqrDistance = sqrDistMap,//nativeSqrDistArray,
                    DotRadius = localDotRadiusFloat,
                    DotRadiusSq = dotRadiusSq,
                    Probability = Mathf.Lerp(.05f, 1f, probability)
                };
                JobHandle jobHandle = job.Schedule(numLinePixels, 16);//5 lines up with the alternating pixels...//4 was about 5ms I think, 16 was 4 instances and 11ms, not sure what that means, try a smaller batch size
                jobHandle.Complete();
                pixelCoordinatesMap.Dispose();
                sqrDistMap.Dispose();

                //lineJobPixelMap[numLinePixels] = pixelIndex;
                //pixelCoordinatesMap[numLinePixels] = new Vector2(x, y);
                //sqrDistMap[numLinePixels] = sqrDist;
                //nativeBlendersArray should line up with these 3 maps listed above
                //I remember hotspotmap is a constantly updated list of hot pixels
                for (int i=0; i< numLinePixels; i++)
                {
                    bool isGood = true;
                    float newPixelBlender = nativeBlendersArray[i];

                    if (!isFilterBeforeLineJob)//if not pre-filtered, need to filter out unchanged pixels
                    {
                        if (newPixelBlender <= 0)
                            isGood = false;//filter out cold pixels
                    }

                    if (isGood)
                    {
                        int pixelIndex = lineJobPixelMap[i];

                        int hotSpotMapIndex = -1;//hotSpotMap.IndexOf(pixelIndex);//really slow but still a lot faster than using contains and then IndexOf!
                        int temp;
                        if (hotSpotMapIndexDictionary.TryGetValue(pixelIndex, out temp))
                            hotSpotMapIndex = temp;
                        else
                            hotSpotMapIndex = hotSpotMap.IndexOf(pixelIndex);

                        if (hotSpotMapIndex > -1)//contains
                        {
                            hotSpotBlenders[hotSpotMapIndex] += newPixelBlender;
                        }
                        else
                        {
                            hotSpotMap.Add(pixelIndex);
                            hotSpotBlenders.Add(newPixelBlender);//multiply by .2 in the job system instead of here to save time
                        }
                    }
                }

                nativeBlendersArray.Dispose();

            }
            else
            {
                for (int x = startIndexX; x < endIndexX; x++)
                {
                    for (int y = startIndexY; y < endIndexY; y++)
                    {
                        Vector2 p = new Vector2 (x,y);
                        float sqrDist = DistanceSquaredPtLine(pixelUV,pixelUV2,p);
                        if (sqrDist <= dotRadiusSq)
                        {
                            float sinWave = Mathf.Sin((pixelUV - p).sqrMagnitude * .3f) * 5f/*amp*/ + Mathf.Sin((pixelUV - p).sqrMagnitude * 1.5f) * 3f - 3;

                            float dist = Mathf.Sqrt(sqrDist);
                            float dotRadiusPlusWave = (float)localDotRadius + sinWave;
                            float distDivRadius = dist / dotRadiusPlusWave;
                            float newPixelBlender = Mathf.Lerp(1, 0, distDivRadius);
                            newPixelBlender *= newPixelBlender;//makes the signal strong in the center and then drop off around the radius

                            //float dotRadiusPlusWaveSquared = Mathf.Pow((float)dotRadius + sinWave,2);
                            //float sqrDistDivRadiusSq = sqrDist / dotRadiusPlusWaveSquared;
                            //float newPixelBlender = Mathf.Lerp(1, 0, sqrDistDivRadiusSq);

                            float pMinusP1DotP2MinusP1 = Vector2.Dot(p - pixelUV, pixelUV2 - pixelUV);
                            float pMinusP2DotP1MinusP2 = Vector2.Dot(p - pixelUV2, pixelUV - pixelUV2);
                            if (pMinusP1DotP2MinusP1 > 0 && pMinusP2DotP1MinusP2 > 0)
                            {
                                if (newPixelBlender > 0)
                                {
                                    int pixelIndex = y * _heatMapTextureHeight + x;

                                    int hotSpotMapIndex = -1;//hotSpotMap.IndexOf(pixelIndex);//really slow but still a lot faster than using contains and then IndexOf!
                                    int temp;
                                    if (hotSpotMapIndexDictionary.TryGetValue(pixelIndex, out temp))
                                        hotSpotMapIndex = temp;
                                    else
                                        hotSpotMapIndex = hotSpotMap.IndexOf(pixelIndex);

                                    if (hotSpotMapIndex > -1)//contains
                                    {
                                        hotSpotBlenders[hotSpotMapIndex] += newPixelBlender;
                                    }
                                    else
                                    {
                                        hotSpotMap.Add(pixelIndex);
                                        hotSpotBlenders.Add(newPixelBlender);//multiply by .2 in the job system instead of here to save time
                                    }
                                }
                            }
                            else if (pMinusP1DotP2MinusP1 <= 0)//outside of point pixelUV, make a half spot to cap the front
                            {
                                float pixelDistance = (p - pixelUV).magnitude;
                                newPixelBlender *= Mathf.Lerp(1, 0, pixelDistance / (float)localDotRadius);
                                if (newPixelBlender > 0)
                                {
                                    int pixelIndex = y * _heatMapTextureHeight + x;
                                    int hotSpotMapIndex = hotSpotMap.IndexOf(pixelIndex);//slow
                                    if (hotSpotMapIndex > -1)//contains
                                    {
                                        hotSpotBlenders[hotSpotMapIndex] += newPixelBlender;
                                    }
                                    else
                                    {
                                        hotSpotMap.Add(pixelIndex);
                                        hotSpotBlenders.Add(newPixelBlender);//multiply by .2 in the job system instead of here to save time
                                    }
                                }
                            }
                            else if (pMinusP2DotP1MinusP2 <= 0)//outside of point pixelUV2, make a half spot to cap the back
                            {
                                float pixelDistance = (p - pixelUV2).magnitude;
                                newPixelBlender *= Mathf.Lerp(1, 0, pixelDistance / (float)localDotRadius);
                                if (newPixelBlender > 0)
                                {
                                    int pixelIndex = y * _heatMapTextureHeight + x;

                                    int hotSpotMapIndex = -1;//hotSpotMap.IndexOf(pixelIndex);//really slow but still a lot faster than using contains and then IndexOf!
                                    int temp;
                                    if (hotSpotMapIndexDictionary.TryGetValue(pixelIndex, out temp))
                                        hotSpotMapIndex = temp;
                                    else
                                        hotSpotMapIndex = hotSpotMap.IndexOf(pixelIndex);

                                    if (hotSpotMapIndex > -1)//contains
                                    {
                                        hotSpotBlenders[hotSpotMapIndex] += newPixelBlender;
                                    }
                                    else
                                    {
                                        hotSpotMap.Add(pixelIndex);
                                        hotSpotBlenders.Add(newPixelBlender);//multiply by .2 in the job system instead of here to save time
                                    }
                                }
                            }
                        }//first filter : sqrDist <= dotRadius
                    } //for loop y index
                }//for loop x index
            }//not useLineJob
        }
        else
        {
            bool populateBlender = false;
            float[] blenderPopulate = new float[localDotRadius * localDotRadius * 4];
            int blenderPopulateCount = 0;
            float[] tempBlender = null;
            if (blenders.TryGetValue(localDotRadius.ToString(), out tempBlender))
                blenderPopulate = tempBlender;
            else
            {
                populateBlender = true;//need to initialize these spots during startup instead of on the fly
            }

            int pixelX = (int)pixelUV.x;//hopefully get some speedup by casting only once
            int pixelY = (int)pixelUV.y;//hopefully get some speedup by casting only once
            int startIndexX = pixelX - localDotRadius;
            int startIndexY = pixelY - localDotRadius;
            int endIndexX = pixelX + localDotRadius;
            int endIndexY = pixelY + localDotRadius;
            float edgeWidth = .01f + (1f - probability) * .1f;

            for (int x = startIndexX; x < endIndexX; x++)
            {
                for (int y = startIndexY; y < endIndexY; y++)
                {
                    if (y >= 0 && y < _heatMapTextureHeight && x >= 0 && x < _heatMapTextureWidth)
                    {
                        float newPixelBlender = 0;//calculate the alpha of the spot
                        if (populateBlender)
                        {
                            float pixelDistance = Vector2.Distance(new Vector2(x, y), pixelUV);
                            newPixelBlender = Mathf.Lerp(1, 0, pixelDistance / (float)localDotRadius);//setRange (pixelDistance,1,(float)dotRadius,1,0);
                            newPixelBlender -= Random.Range(0f, .15f);//noise up the spot - moved noise to the cached version to save cpu work
                            blenderPopulate[blenderPopulateCount++] = newPixelBlender;//populating blender
                        }
                        else
                        {
                            newPixelBlender = blenderPopulate[blenderPopulateCount++];
                        }

                        if (newPixelBlender > edgeWidth)
                        {
                            int pixelIndex = y * _heatMapTextureHeight + x;

                            int hotSpotMapIndex = -1;//hotSpotMap.IndexOf(pixelIndex);//really slow but still a lot faster than using contains and then IndexOf!
                            int temp;
                            if (hotSpotMapIndexDictionary.TryGetValue(pixelIndex, out temp))
                                hotSpotMapIndex = temp;
                            else
                                hotSpotMapIndex = hotSpotMap.IndexOf(pixelIndex);

                            if (hotSpotMapIndex > -1)
                            {
                                hotSpotBlenders[hotSpotMapIndex] += newPixelBlender;
                            }
                            else
                            {
                                //load up the maps and send them off to a job system to crunch numbers in parallel
                                hotSpotMap.Add(pixelIndex);
                                hotSpotBlenders.Add(newPixelBlender);//multiply by .2 in the job system instead of here to save time
                            }
                        }
                    }
                }
            }

            if (populateBlender)//get rid of this populate blender shit, or put it into the start() method
                blenders.Add(localDotRadius.ToString(), blenderPopulate);//after the array is populated
        }
    }


    void PaintSpotsCombined(Vector2 pixelUV, int dotRadius, Color32 bloodColor, float probability)
    {

        pixelUV.x *= _heatMapTextureWidth;
        pixelUV.y *= _heatMapTextureHeight;
        //check if this dotRadius is in the dictionary of blender arrays
        //blender arrays will be an array of floats with dotRadius number of elements
        //dictionary of float[dotRadius] using dotRadius as the key
        bool populateBlender = false;
        float[] blenderPopulate = new float[dotRadius * dotRadius * 4];
        int blenderPopulateCount = 0;
        float[] temp = null;
        if (blenders.TryGetValue(dotRadius.ToString(), out temp))
            blenderPopulate = temp;
        else
        {
            populateBlender = true;//need to initialize these spots during startup instead of on the fly
        }

        //for each pixel in this small area
        //1) lookup spot signal from blenderPopulate array
        //2) send blenderPopulate and use it's index for the job
        // - send other-formatted arrays:
        // - nativeArray heatArray
        // - nativeArray emissionColorArray
        // - get the above to work before adding the edgeWidth noise thing

        int pixelX = (int)pixelUV.x;//hopefully get some speedup by casting only once
        int pixelY = (int)pixelUV.y;//hopefully get some speedup by casting only once
        int startIndexX = pixelX - dotRadius;
        int startIndexY = pixelY - dotRadius;
        int endIndexX = pixelX + dotRadius;
        int endIndexY = pixelY + dotRadius;

        float edgeWidth = .01f + (1f - probability) * .1f;

        for (int x = startIndexX; x < endIndexX; x++)
        {
            for (int y = startIndexY; y < endIndexY; y++)
            {
                if (y >= 0 && y < _heatMapTextureHeight && x >= 0 && x < _heatMapTextureWidth)
                {
                    float newPixelBlender = 0;//calculate the alpha of the spot
                    if (populateBlender)
                    {
                        float pixelDistance = Vector2.Distance(new Vector2(x, y), pixelUV);
                        newPixelBlender = Mathf.Lerp(1, 0, pixelDistance / (float)dotRadius);//setRange (pixelDistance,1,(float)dotRadius,1,0);
                        newPixelBlender -= Random.Range(0f, .15f);//noise up the spot - moved noise to the cached version to save cpu work
                        blenderPopulate[blenderPopulateCount++] = newPixelBlender;//populating blender
                    }
                    else
                    {
                        newPixelBlender = blenderPopulate[blenderPopulateCount++];
                    }

                    //newPixelBlender -= Random.Range(0f, .15f);//noise up the spot
                    if (newPixelBlender > edgeWidth)
                    {
                        int pixelIndex = y * _heatMapTextureHeight + x;

                        float blendedHeatSignal = heatArray[pixelIndex] + newPixelBlender * .2f;//compute the heat signal before applying color to the mainTexture

                        Color32 ashyColor = GetAshyColor(blendedHeatSignal);
                        Color32 ashToBlackEdgeColor = Color32.Lerp(ashyColor, Color.black, newPixelBlender);
                        //Color32 blendedPixelColor = Color32.Lerp(data[pixelIndex], ashToBlackEdgeColor, newPixelBlender);
                        //data[pixelIndex] = blendedPixelColor;
                        //emission spot
                        heatArray[pixelIndex] = blendedHeatSignal;
                        Color32 emissionAlphaAdd = GetFakeGradient(blendedHeatSignal);
                        emissionAlphaAdd.a = (byte) (255f * Mathf.Pow(newPixelBlender, 5.0f));// blendedPixelColor.r;//(byte) (maxHeatArray[pixelIndex] );//just testing, but not quite the same, need to accumulate heat data
                        //emissionData[pixelIndex] = emissionAlphaAdd;// GetFakeGradient(blendedHeatSignal);

                        if (!hotPixelHash.Contains(pixelIndex))
                            hotPixelHash.Add(pixelIndex);
                    }
                }
            }
        }
        if (populateBlender)
            blenders.Add(dotRadius.ToString(), blenderPopulate);//after the array is populated

        //applyWorkingTextureUpdate = true;//disabled dks 07/15/2020
        applyEmissionUpdate = true;

        hotPixelsFound = true;
    }

    void PaintSpots(Vector2 pixelUV, Color32 bloodColor)
    {
        PaintSpots(pixelUV, bloodColor, .995f);
    }

    void PaintSpots(Vector2 pixelUV, Color32 bloodColor, float probability)
    {
        burnSpotsQueue.Add(new BurnSpot(pixelUV, Random.Range((int)(spotWidth * minSpotRadiusMultiplier), spotWidth), bloodColor, probability));
    }

    void PaintLines(Vector2 pixelUV, Vector2 pixelUV2, Color32 bloodColor, float probability)
    {
        burnSpotsQueue.Add(new BurnSpot(pixelUV, pixelUV2, spotWidth, bloodColor, probability));
    }

    void GradientSetup()
    {
        if (burnGradient == null)
        {
            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;
            burnGradient = new Gradient();
            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[3];
            colorKey[0].color = Color.black;
            colorKey[0].time = 0.0f;
            colorKey[1].color = Color.red;
            colorKey[1].time = .5f;
            colorKey[2].color = Color.yellow;
            colorKey[2].time = 1.0f;
            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[3];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = .5f;
            alphaKey[2].alpha = 1.0f;
            alphaKey[2].time = 1.0f;
            burnGradient.SetKeys(colorKey, alphaKey);
        }
    }

    public static Color32 GetFakeGradient(float signal)
    {
        Color32 color = Color.black;
        //float yellowToRedThreshold = 1.5f;//.75f;//moving the yellow range above 1.5 since the max is already 2.0 and the red lingers longer which is cool
        if (signal <= 0)
        {
            return color;// = Color.black;
        }
        else if (signal < 1.5)//yellowToRedThreshold)
        {
            color = new Color(signal * .667f, 0,0);
            //color = Color32.Lerp(Color.black, Color.red, signal * .667f);// (1 / yellowToRedThreshold)); //burnGradient.Evaluate(HeatSignals[index]);
        }
        else if (signal <= 2) //red hot at 1.5, 2 and beyond is yellow
        {
            //color = Color32.Lerp(Color.red, Color.yellow, (signal - yellowToRedThreshold) * (1 / (1 - yellowToRedThreshold)));
            float channel = (signal - 1.5f) * .5f;
            color = new Color (1,channel,0);
            //color = Color32.Lerp(Color.red, Color.yellow, (signal - 1.5f) * .5f);
        } else if (signal > 2)
        {
            float channel = signal - 1f;
            color = new Color (channel, channel, 0);
            //color = Color.yellow * (signal - 1f);
        }
        return color;
    }

    /*
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = true)]//gradient not supported by burst
    public struct HeatCoolingJob : IJobParallelFor
    {
        public NativeArray<float> HeatSignals;
        public NativeArray<int> IsHot;
        [ReadOnly]
        public int AltPix;
        [ReadOnly]
        public float HeatLost;
        [ReadOnly]
        public int Width;

        public void Execute(int index)
        {
            if (HeatSignals[index] > 0 && ((index + (index / Width)) % 2 == AltPix))//switched to 3s//(index + (index / Width * 2)) % 4 == AltPix) every 4 pixels, +2 for every row
            {
                bool wasHot = false;
                float signal = HeatSignals[index];
                if (signal > 0)
                    wasHot = true;
                HeatSignals[index] -= HeatLost;// .025f;//cooling

                if (!wasHot)//assume color has been updated so you can mask out that pixel
                {
                    IsHot[index] = 0;//update only after it is already obsolete
                }
            }
        }
    }
    */

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = false)]//gradient not supported by burst
    public struct HeatCoolingAndColorJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> HeatSignals;
        //[NativeDisableParallelForRestriction]
        //public NativeArray<Color32> Data;
        [NativeDisableParallelForRestriction]
        public NativeArray<Color32> HeatMapData;
        [ReadOnly]
        public int AltPix;
        [ReadOnly]
        public float HeatLost;
        [ReadOnly]
        public int Width;
        [ReadOnly]
        public NativeArray<int> PixelMap;
        //public float emissionAlpha = 0;
        public void Execute(int index)
        {
            int newIndex = PixelMap[index];
            //byte emissionAlpha = 0;// = Data[index].a;
            if (((newIndex + (newIndex / Width)) % 2 == AltPix))//switched to 3s//(index + (index / Width * 2)) % 4 == AltPix) every 4 pixels, +2 for every row
            {
                bool wasHot = false;
                //emissionAlpha = Data[newIndex].a;//assume the accumulated heat is stored in the alpha
                HeatSignals[newIndex] -= HeatLost;// .025f;//cooling
                if (HeatSignals[newIndex] < 0)
                    HeatSignals[newIndex] = 0;//trying to fix donuts or lingering red

                float signal = HeatSignals[newIndex];//moved this down so I dont have to defer the pruning of cool pixels
                Color32 newHeatMap = HeatMapData[newIndex];

                //Color32 color = Color.black;
                signal = Mathf.Max(signal, 0f);
                //if (signal <= 0)
                //{
                //    color = Color.black;
                //}
                //else if (signal < 1.5)//yellowToRedThreshold)
                //{
                //    color = new Color(signal * .667f, 0,0);
                //}
                //else if (signal <= 2) //red hot at 1.5, 2 and beyond is yellow
                //{
                    //float channel = (signal - 1.5f) * .5f;
                //    color = new Color (1,(signal - 1.5f) * .5f,0);
                //} else if (signal > 2)
                //{
                //    float channel = signal - 1f;
                //    color = new Color (channel, channel, 0);
                //}
                //color.a = emissionAlpha;
                //Data[newIndex] = color;//emission, not color
                newHeatMap.r = (byte) (255f * Mathf.Min(1,signal));
                HeatMapData[newIndex] = newHeatMap;
                //if (!wasHot)//assume color has been updated so you can mask out that pixel
                //{
                //    IsHot[newIndex] = 0;//update only after it is already obsolete
                //}
            }
        }
    }

    private void ExecuteHeatCoolingAndColorJob(ref NativeArray<float> myHeatArray, float timeSinceLastCooldown/*, ref NativeArray<Color32> myData*/, ref NativeArray<Color32> myHeatMapData)
    {
        //NativeArray<float> heatNativeArray = new NativeArray<float>(myHeatArray.Length, Allocator.TempJob);
        NativeArray<int> hotPixelMapNativeArray = new NativeArray<int>(hotPixelHash.Count, Allocator.TempJob);
        //heatNativeArray.CopyFrom(myHeatArray);
        int[] hotPixelArray = new int[hotPixelHash.Count];
        hotPixelHash.CopyTo(hotPixelArray);
        hotPixelMapNativeArray.CopyFrom(hotPixelArray);

        //NativeArray<Color32> nativeData = new NativeArray<Color32>(myData.Length, Allocator.TempJob);
        //nativeData.CopyFrom(myData);

        float heatLost = timeSinceLastCooldown * coolingFactor;// .4f;/// 20f / 2f * 4f; // 

        HeatCoolingAndColorJob job = new HeatCoolingAndColorJob()
        {
            HeatSignals = myHeatArray,//heatNativeArray, //parameters for job
            //Data = myData,//nativeData,
            HeatMapData = myHeatMapData,
            AltPix = altPixInt,//alternatingPixels
            HeatLost = heatLost,
            Width = _heatMapTextureWidth,
            PixelMap = hotPixelMapNativeArray
        };
        JobHandle jobHandle = job.Schedule(hotPixelHash.Count, 16);//5 lines up with the alternating pixels...//4 was about 5ms I think, 16 was 4 instances and 11ms, not sure what that means, try a smaller batch size
        jobHandle.Complete();
        //heatNativeArray.CopyTo(myHeatArray);
        //heatNativeArray.Dispose();
        hotPixelMapNativeArray.Dispose();
        //nativeData.CopyTo(myData);
        //nativeData.Dispose();
        //need to add a wasCold list so that you can cull them out after the color job has run
    }

    IEnumerator UpdateEmission()
    {
        isEmissionCooling = true;
        bool useJobForColor = true;//using this to toggle getrawtexturedata behavior which is still slow but has potential
        bool localHotPixelsFound = false;
        int minX = _heatMapTextureWidth;
        int maxX = 0;
        int minY = _heatMapTextureHeight;
        int maxY = 0;

        Debug.Log("emissionCoolingA");
        altPixBool = !altPixBool;//alternates pixel updates
        altPixInt = (altPixInt + 1) % 2;//try alternating in 3s

        ExecuteHeatCoolingAndColorJob(ref heatArray, timeSinceLastCooldown/*, ref emissionData*/, ref heatMapData);//jobAlloc overflow

        yield return null;//just being nice
        timeSinceLastCooldown = 0;//reset after cooling operation

        HashSet<int> toRemove = new HashSet<int>();
        foreach (int z in hotPixelHash)
        {
            if (heatArray[z]>0)
            {
                if (!localHotPixelsFound)
                    localHotPixelsFound = true;
            }
            else
            {
                toRemove.Add(z);//hotPixelHash.Remove(z);
            }
        }
        foreach (int i in toRemove)
        {
            //Debug.Log("removing " + i.ToString() + " from hash");
            hotPixelHash.Remove(i);
        }
        /*
        for (int z = hotPixelMap.Count - 1; z >= 0; z--)
        {
            if (heatArray[hotPixelMap[z]] > 0)
            {

                if (!localHotPixelsFound)
                    localHotPixelsFound = true;

            }
            else
            {
                hotPixelMap.RemoveAt(z);
            }
        }*/

        if (localHotPixelsFound)
            applyEmissionUpdate = true; //AddActionToQ(actions.EmissionApply, actionQueue);//applyEmissionUpdate = true;

        // Debug.Log("emissionCoolingC");

        hotPixelsFound = localHotPixelsFound;
        isEmissionCooling = false;
    }

    void UpdateEmissionBruteForce()
    {
        //isEmissionCooling = true;
        bool useJobForColor = true;//using this to toggle getrawtexturedata behavior which is still slow but has potential
        bool localHotPixelsFound = false;
        int minX = _heatMapTextureWidth;
        int maxX = 0;
        int minY = _heatMapTextureHeight;
        int maxY = 0;

        Debug.Log("emissionCoolingBrute");
        altPixBool = !altPixBool;//alternates pixel updates
        altPixInt = (altPixInt + 1) % 2;//try alternating in 3s

        //ExecuteHeatCoolingAndColorJob(heatArray, timeSinceLastCooldown, emissionData);//jobAlloc overflow

        //need this loop to hopefully reset the min/max bounds, but now that I think about it, not sure if I need to yield so much
        timeSinceLastCooldown = 0;//doesn't really matter when this gets set because I'm using deltaTime

        HashSet<int> toRemove = new HashSet<int>();
        foreach (int z in hotPixelHash)
        {
            if (heatArray[z]>0)
                if (!localHotPixelsFound)
                    localHotPixelsFound = true;
            else
                toRemove.Add(z);//hotPixelHash.Remove(z);
        }
        foreach (int z in toRemove)
            hotPixelHash.Remove(z);

        if (localHotPixelsFound)
            applyEmissionUpdate = true;//AddActionToQ(actions.EmissionApply, actionQueue);/

        // Debug.Log("emissionCoolingC");

        hotPixelsFound = localHotPixelsFound;

        //isEmissionCooling = false;
    }

    //create a list of all of the hot pixel indices
    //send this pixel map into a job system
    //loop through each pixel in the map and process heat and color that way
    public bool GetNextActionIsTextureApply()
    {
        if (actionQueue[0] == actions.EmissionApply || actionQueue[0] == actions.ColorApply)
            return true;
        else
            return false;
    }

    public void DoNextAction()
    {
        actions nextAction = GetNextActionFromQ(actionQueue);
        switch (nextAction)
        {
            case actions.None:
                break;
            //case actions.ColorApply:
                //workingTexture.Apply();
                //applyWorkingTextureUpdate = false;
                //colorUpdateTimer = 0;
                //break;
            case actions.EmissionApply:
                //workingEmissionTexture.Apply();
                workingHeatMapTexture.Apply();
                applyEmissionUpdate = false;
                break;
            case actions.DummyApply:
                break;
            case actions.EmissionUpdate:
                if (useBruteForce)
                {
                    UpdateEmissionBruteForce();//no longer a coroutine
                }
                else if (!isEmissionCooling)
                {
                    StartCoroutine(UpdateEmission());
                }

                break;
            case actions.CoolingAndColor: //use before emissionUpdate
                ExecuteHeatCoolingAndColorJob(ref heatArray, timeSinceLastCooldown/*, ref emissionData*/, ref heatMapData);//jobAlloc overflow
                break;
            case actions.PaintSpotsAction:
                DoPaintSpots();
                break;
            case actions.PaintSpotsJob:
                DoExecuteSpots();
                break;
            default:
                break;
        }
    }

    void DoPaintSpots()
    {
        float timeLimit = .0005f; //.00025f//.005 seems just as slow and hurts the frame rate a little more, not worth it
        float burnTimerStart = Time.realtimeSinceStartup;
        float burnTimer = 0f;
        if (useJobbieSpots)
        {
            hotSpotBlenders.Clear();
            hotSpotMap.Clear();//
            hotSpotMapIndexDictionary.Clear();//cache pixel index for when it gets reused
        }
        while ((useJobbieSpots || burnTimer < timeLimit) && burnSpotsQueue.Count > 0)//jobbie uses all spots
        {
            BurnSpot bs = burnSpotsQueue[0];
            if (useJobbieSpots)
            {
                PaintSpotsJobbie(bs.pixelUV, bs.pixelUV2, bs.pixelWidth, bs.color, bs.probability);
            }
            else
            {
                PaintSpotsCombined(bs.pixelUV, bs.pixelWidth, bs.color, bs.probability);
            }
            burnSpotsQueue.RemoveAt(0);
            burnTimer = Time.realtimeSinceStartup - burnTimerStart;
        }
    }

    void DoExecuteSpots()
    {
        ExecutePaintSpotsJob(ref heatArray, hotSpotBlenders.ToArray()/*,ref data, ref emissionData*/, ref heatMapData);
    }

    void Update()
    {
        float _deltaTime = Time.deltaTime;
        timeSinceLastCooldown += _deltaTime;
        colorUpdateTimer += _deltaTime;

        float timeLimit = .001f; //.00025f
        float burnTimerStart = Time.realtimeSinceStartup;
        float burnTimer = 0f;

        if (burnSpotsQueue.Count > 0 && !isEmissionCooling)
        {
            AddActionToQ(actions.PaintSpotsAction, actionQueue);
            if (useJobbieSpots)
                AddActionToQ(actions.PaintSpotsJob, actionQueue);
        }

        if (hotPixelsFound)
        {//only check hot texture if there's something to cool down
            if (!isEmissionCooling && timeSinceLastCooldown > 0.04166f)//trying to slow it down
            {
                if (useBruteForce)
                {
                    AddActionToQ(actions.CoolingAndColor, actionQueue);
                }
                AddActionToQ(actions.EmissionUpdate, actionQueue);
            }
        }

        if (applyEmissionUpdate || applyWorkingTextureUpdate)
        {
            updateTimer = Time.realtimeSinceStartup - timeOfLastUpdate;
            if (updateTimer > updateInterval)//updateTimer is set to a max of 24fps
            { //limiting texture map updates to an acceptable interval
                consecutiveUpdateFrames += 1;
                if (applyEmissionUpdate && (Time.frameCount % 2 == 0))
                {
                    AddActionToQ(actions.EmissionApply, actionQueue);//workingEmissionTexture.Apply();
                }
                if (applyWorkingTextureUpdate && (Time.frameCount % 2 == 1) && colorUpdateTimer > .5)//colorUpdate can happen less frequently
                {//they should never update during the same frame
                    //AddActionToQ(actions.ColorApply, actionQueue);//workingTexture.Apply();
                }
                if (consecutiveUpdateFrames >= 2)//confusing setup, get rid of it
                {//if we hit 2 in a row restart the timer
                    timeOfLastUpdate = Time.realtimeSinceStartup;
                    consecutiveUpdateFrames = 0;
                    updateTimer = 0;
                }
            }
        }

    }

}

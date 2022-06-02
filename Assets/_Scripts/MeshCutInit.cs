using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutInit : MonoBehaviour {
    public BLINDED_AM_ME.MeshCut meshCut;// = new BLINDED_AM_ME.MeshCut();
    [SerializeField] int chunkPoolSize = 128;
    [SerializeField] int chunksBufferSize = 10;
    [SerializeField] bool isDebug = false;
    [SerializeField] private BLINDED_AM_ME.MeshCut.CappingStyles cappingStyle = BLINDED_AM_ME.MeshCut.CappingStyles.SkipColinear;
    private int cappingTestFramesInterval = 100;
    // Use this for initialization
    void Start () {
        meshCut = new BLINDED_AM_ME.MeshCut(chunkPoolSize,chunksBufferSize,isDebug);
        StartCoroutine(meshCut.MeshCutInitialize());
	}
    private void Update()
    {
        if (Time.frameCount % cappingTestFramesInterval == 0)//don't spam every frame
        {
            if (cappingStyle != BLINDED_AM_ME.MeshCut.GetCappingStyle())
            {
                BLINDED_AM_ME.MeshCut.SetCappingStyle(cappingStyle);
            }
        }
    }
}

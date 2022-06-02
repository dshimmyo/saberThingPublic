using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutInstance : MonoBehaviour
{
    public static bool isInitialized = false;
    public static int maxAllowedInstances = 64;
    public static List<GameObject> meshCutInstances = new List<GameObject>();

    public MeshCutInstance()
    {
    }

    private void Start()
    {
        if (gameObject.GetComponent<MeshCutChunk>() != null)
        {
            Debug.Log("MeshCutInstance was erroneously installed on a MeshCutChunk! " + gameObject.name);
        }
        else
        {
            MeshCutInstanceAdd();
            BLINDED_AM_ME.MeshCut.UpdateNumMeshCutInstances(this);
        }
    }

    public void MeshCutInstanceAdd()
    {

        if (!meshCutInstances.Contains(gameObject))
        {
            meshCutInstances.Add(gameObject);
            //Debug.Log("added new MeshCutInstance " + gameObject.name);
        }
        Joint[] js = gameObject.GetComponents<Joint>();
        foreach (Joint j in js)
        {
            if (j != null)
            {
                Rigidbody cb = j.connectedBody;
                if (cb != null)
                {
                    if (cb.gameObject.GetComponent<Cuttable>() && !cb.gameObject.GetComponent<MeshCutChunk>())
                    {
                        MeshCutInstance mci = cb.gameObject.GetComponent<MeshCutInstance>();
                        if (mci == null)
                            cb.gameObject.AddComponent<MeshCutInstance>();//do not recurse, this will check connections on its own
                    }
                }
            }
        }

        JointConnection[] jcs = gameObject.GetComponents<JointConnection>();
        foreach (JointConnection jc in jcs)
        { 
            if (jc != null)
            {
                Joint jj = jc.joint;
                if (jj != null)
                {
                    if (jj.gameObject.GetComponent<Cuttable>() && !jj.gameObject.GetComponent<MeshCutChunk>())
                    { 
                        MeshCutInstance mci = jj.gameObject.GetComponent<MeshCutInstance>();
                        if (mci == null)
                            jj.gameObject.AddComponent<MeshCutInstance>();//do not recurse, this will check connections on its own           
                    }
                }
            }
        }
    }

    public int GetNumMeshCutInstances()
    {
        int num = meshCutInstances.Count;
        for (int i=num-1; i >= 0; i--)
        {
            if (meshCutInstances[i] == null)
            {
                meshCutInstances.RemoveAt(i);
            }
        }
        return meshCutInstances.Count;
    }

    private void DestroyWithMeshcut(GameObject go)
    {
        Component[] mcc = go.GetComponentsInChildren(typeof(MeshCutChunk));
        for (int i=0; i>mcc.Length; i++)
        {
            BLINDED_AM_ME.MeshCut.Destroy(mcc[i].gameObject);
            //Debug.Log("MeshCutInstance saved a MeshCutChunk");//turns out this was not a necessary precaution
        }
        Destroy(go);
    }

    void Update()
    {
        if (!isInitialized)
        {
            if (BLINDED_AM_ME.MeshCut.isDoneInitializing)
            {
                maxAllowedInstances = BLINDED_AM_ME.MeshCut.GetChunksPoolSize();
                isInitialized = true;
            }
        }
        else
        {
            if (meshCutInstances.Count > maxAllowedInstances)
            {
                GameObject destroyMe = meshCutInstances[0];
                meshCutInstances.RemoveAt(0);
                if (destroyMe != null)
                {
                    //Debug.Log("MeshCutInstance Destroying " + destroyMe.name + " because there are " + meshCutInstances.Count.ToString() + " instances");
                    BLINDED_AM_ME.MeshCut.numMeshCutInstances--;
                    DestroyWithMeshcut(destroyMe);
                }
            }
        }
    }
}

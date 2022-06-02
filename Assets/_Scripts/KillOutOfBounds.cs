using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillOutOfBounds : MonoBehaviour {
    private List<GameObject> killList;
    private bool done = false;

    void Start () {
		killList = new List<GameObject>();
	}
	
    void OnTriggerEnter(Collider other){
        if (other.gameObject.tag == "Cuttable")
        {
            //killList.Add(other.gameObject);
            //BLINDED_AM_ME.MeshCut.Destroy (other.gameObject);
            DestroyThings(other.gameObject);
        }
        //else if (other.gameObject.tag == "Lightsaber")
        //    other.gameObject.transform.position = Vector3.up * 10f;
    }
    void OnTriggerStay(Collider other){
        if (other.gameObject.tag == "Cuttable") 
        {
            //killList.Add(other.gameObject);
            //BLINDED_AM_ME.MeshCut.Destroy (other.gameObject);
            DestroyThings(other.gameObject);
        }
        //else if (other.gameObject.tag == "Lightsaber") 
        //{
        //    other.gameObject.transform.position = Vector3.up * 5f;
        //    other.gameObject.GetComponent<Rigidbody> ().velocity = -Vector3.up; 
        //}
    }

    void DestroyThings(GameObject go)
    {
        Component[] mcc = go.GetComponentsInChildren(typeof(MeshCutChunk));
        for (int i=0; i>mcc.Length; i++)
        {
            BLINDED_AM_ME.MeshCut.Destroy(mcc[i].gameObject);
            Debug.Log("KillOutOfBounds maybe saved a MeshCutChunk");//turns out this was not a necessary precaution
        }
        BLINDED_AM_ME.MeshCut.Destroy (go);

    }

    /*
    void Update () {
        if (killList.Count > 0)
        {
            done = false;
            while (!done)
            {
                if (killList.Count > 0)
                {
                    if (killList[0] != null)
                        BLINDED_AM_ME.MeshCut.Destroy(killList[0]);
                    killList.RemoveAt(0);
                }
                else
                {
                    done = true;
                }
            }
        }
	}*/
}

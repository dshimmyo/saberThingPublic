using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadPool : MonoBehaviour {
    [SerializeField] private GameObject killSparkPrefab;
    public GameObject[] killSparks;
    public int numSpark = 5;
	// Use this for initialization
	void Start () {
        killSparks = new GameObject[numSpark];
        if (SaberGame.isQuest)
        killSparkPrefab = Resources.Load("prefabs/deathSparkQuest") as GameObject;
        else
        killSparkPrefab = Resources.Load("prefabs/deathSpark") as GameObject;
        for (int i=0; i<numSpark; i++){
            killSparks[i] = Instantiate(killSparkPrefab);
            killSparks[i].name = "deathSpark" + i.ToString();
        }
	}
	
    public void KillMe(GameObject go){
        for (int i=0; i<numSpark; i++){ //loop thru sparks to find one available, if one is, you set it up.
            if (!killSparks[i].activeInHierarchy)
                {
                    Vector3 center = go.GetComponent<Renderer> ().bounds.center;
                    killSparks[i].transform.position = center;
                    //BLINDED_AM_ME.MeshCut.Destroy (go);
                    killSparks[i].SetActive(true);
                    break;
                }
        }
        //Destroy(go.GetComponent<KillSpark>());//trying to remove the killspark component
        //BLINDED_AM_ME.MeshCut.Destroy (go);//this should also remove the killspark component
        //return true;

    }
	// Update is called once per frame
	void Update () {
		
	}
}

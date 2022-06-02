using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillSpark : MonoBehaviour {
    private float timer = 0;
    public float delayTime = 10;
    private bool isDone = false;
    //public GameObject[] killSparksPool;
    private GameObject Game;
	void Start () {
        MyStart();
        Game = GameObject.Find("Game");
	}

    void OnEnable(){
        MyStart();
    }

	void MyStart(){
        Cuttable c = gameObject.GetComponent<Cuttable>();
        if (c != null)
            delayTime = c.GetKillTime();
        timer = 0;
        delayTime += 5 * Random.Range(0,1);
        MeshCollider mc = GetComponent<MeshCollider> ();
        if (mc != null)
            delayTime *= Mathf.Min(mc.bounds.size.magnitude,1);
    }

	void Update () {
        timer += Time.deltaTime;
        if (timer > delayTime && !isDone) {
            Game.GetComponent<DeadPool>().KillMe(gameObject); //adds a killSpark prefab to that location
            BLINDED_AM_ME.MeshCut.Destroy(gameObject); //destroys the gameobject and removes the killspark script
            isDone = true;
        }
        //if (isDone)
            //Destroy(this);//little redundant, hacky, and sloppy //gets removed by meshcut.Destroy()
    }
}

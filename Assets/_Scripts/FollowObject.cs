using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using Unity.Jobs;//job system
//using UnityEngine.Jobs;//job system
//using Unity.Collections;//job system
//using Unity.Burst;
//
// The purpose of this script is to keep the follow sphere lagging behind the original by a specific delay frequency
// The second purpose is to record every position in between using the fixedUpdate frequency
// All of this can be achieved in the drawblur script instead of being split into multiple scripts
// The follow spheres also don't need to exist. they serve no purpose anymore now that we are logging the positions into an array
//
public class FollowObject : MonoBehaviour {
    public GameObject target;
    public float delay=0.04166667f;
    [SerializeField] public Vector3[] positions;//need to be public so drawblur can access them
    [SerializeField] public int numPositions=0;//need to be public so drawblur can access them

	void Start () {
        numPositions = Mathf.CeilToInt(delay / Time.fixedDeltaTime);//should be stable, even when paused
        positions = new Vector3[numPositions];
        for (int i=0; i< numPositions; i++ ){
            positions[i] = target.transform.position;
        }
	}
	
    void AddPosition(Vector3 pos){//add the position to the zero element
        for (int i=numPositions-1; i > 0; i--){
            positions[i]=positions[i-1];
        }
        positions[0]=pos;
    }
    Vector3 GetPosition(){ // get last position (fully delyed position)
        return positions[numPositions-1];
    }

	void FixedUpdate () {
        transform.position = GetPosition();//()previousPosition;
        AddPosition(target.transform.position);
	}
}

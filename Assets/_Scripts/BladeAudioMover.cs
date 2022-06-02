using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeAudioMover : MonoBehaviour {
    private Camera myCamera;
    [SerializeField] private GameObject bass;
    [SerializeField] private GameObject tip;
    private Transform _baseTransform;
    private Transform _tipTransform;
    private float maxLength;
    private float newDistance;
	void Start () 
    {
		myCamera = Camera.main;
        _baseTransform = bass.transform;
        _tipTransform = tip.transform;
        maxLength = Vector3.Distance(_baseTransform.position,_tipTransform.position);
	}
	
	void Update () 
    {
        Vector3 bladeVector = _tipTransform.position - _baseTransform.position;
		Vector3 newVec = Vector3.Project(myCamera.transform.position - _baseTransform.position, bladeVector );
        //if (Vector3.Dot(newVec,bladeVector) < 0)
        //{
        //    newDistance = 0;
        //} 
        //else
        //{
        //newDistance = newVec.magnitude;
        //}
        newDistance = Mathf.Clamp (newVec.magnitude,0,maxLength);
        newVec = newDistance * newVec.normalized;
        Vector3 newPos = _baseTransform.position + newVec;
        gameObject.transform.position = newPos;
	}
}

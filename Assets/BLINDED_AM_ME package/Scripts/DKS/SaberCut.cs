using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaberCut {

    public Vector3 point; //hit point of the cut entry
    public Vector3 normal;//I think this used to be the normal of the cutting plane
    public Vector3 hitDir;//direction the blade was moving upon entry
    public Vector3 direction;
    public float distance;
    public float time; //time at the cut entry
    //public Vector3 thirdPoint;
    public Vector3 pointA = Vector3.zero; // points A and B were being used as the 2nd and 3rd points to define the cut plane for planar cuts
    public Vector3 pointB = Vector3.zero;
    public Vector3 exitPoint;//single exit point for use in L-cuts
    public List<Vector3> midTopSample = new List<Vector3>(); //for theoretical L or curved cuts
    public List<Vector3> midBotSample = new List<Vector3>(); //for theoretical L or curved cuts
    public float boundsVolume = 1;
    public int cutAttemptCount = 0;
    public Transform bladeMeshTransform;
    public Mesh bladeMesh;

    public SaberCut (Vector3 newPoint, Vector3 newNormal, Vector3 newDirection, float newDistance, float newTime){
        point = newPoint;
        normal = newNormal;
        direction = newDirection;
        distance = newDistance;
        time = newTime;
    }
    // public SaberCut (Vector3 newPoint, Vector3 newNormal, Vector3 newDirection, float newDistance, float newTime, Vector3 newThirdPoint){
    //     point = newPoint;
    //     normal = newNormal;
    //     direction = newDirection;
    //     distance = newDistance;
    //     time = newTime;
    //     thirdPoint = newThirdPoint;
    // }
    public SaberCut (Vector3 newPoint, float newTime){
        point = newPoint;
        normal = Vector3.up;
        direction = Vector3.up;
        distance = 0;
        time = newTime;
    }
    public void AddTime (float newTime){
        time = newTime;
    }
    public float GetTime () {
        return time;
    }
    public void AddPoint (Vector3 newPoint){
        point = newPoint;
    }
    public void AddNormal (Vector3 newNormal){
        normal = newNormal;
    }
    public void AddHitDir (Vector3 newHitDir){
        hitDir = newHitDir;
    }
    public Vector3 GetHitDir(){
        return hitDir;
    }
    // public void AddThirdPoint (Vector3 newThirdPoint){
    //     thirdPoint = newThirdPoint;
    // }
    public void AddPointA (Vector3 newPointA){
        pointA = newPointA;
    }
    public void AddPointB (Vector3 newPointB){
        pointB = newPointB;
    }
    public void AddPointAandB (Vector3 newPointA, Vector3 newPointB){
        pointA = newPointA;
        pointB = newPointB;
        exitPoint = (pointA + pointB)/2;//calculates the center point for use in L-cuts
    }
    public void AddExitPoint (Vector3 newExitPoint){
        exitPoint = newExitPoint;
    }
    public void Clear (){
        midTopSample.Clear();
        midBotSample.Clear();
        point = Vector3.zero;
        normal = Vector3.up;
        //direction = Vector3.up;
        distance = 0;
        time = 0f;
    }
    public int GetMidPointsCount(){
        return midTopSample.Count;
    }
    public Vector3 GetMidTop(){
        return midTopSample[Mathf.FloorToInt(midTopSample.Count / 2)];
    }
    public Vector3 GetMidBot(){
        return midBotSample[Mathf.FloorToInt(midBotSample.Count / 2)];
    }
    public Vector3 GetMidThirdPoint(){ //to define a plane you'll need midtop (avg) midbot (avg) and midThirdPoint which is avg of entry and exit points
        return (point + exitPoint) / 2;
    }
    public void AddMidTop(Vector3 newMidTop){
        midTopSample.Add(newMidTop);
    }
    public void AddMidBot(Vector3 newMidBot){
        midBotSample.Add(newMidBot);
    }
    public void CalculateVolume(Vector3 bounds){
        boundsVolume = bounds.x * bounds.y * bounds.z;
    }
    public float GetVolume(){
        return boundsVolume;
    }
}

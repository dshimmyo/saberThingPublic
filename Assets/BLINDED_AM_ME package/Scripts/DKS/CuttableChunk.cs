using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttableChunk : Cuttable {
	// Use this for initialization
	void Start () {
		//CutSetup();
	}

    //void CutSetup() {
        //if this is cuttable metal you can set up the start behaviour of the hot metal shader
    //}
	
    //make an interface or abstract class to serve 2 purposes
    // 1- indicate that the object is cuttable and can be interacted with by the meshcut class
    // 2- give some kind of hook to setup the cuttable material, preferably in the Start() event
    // So CuttableMetal should still be able to be found as a "Cuttable" and 
	// Update is called once per frame
	// void Update () {
		
	// }
}

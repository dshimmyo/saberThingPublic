using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeparateDiscontiguous : MonoBehaviour {
    private Mesh oldMesh;
    private Mesh mesh;
    private bool done = false;
    private float timer = 0;
    private bool isCuttable = false;
	// Use this for initialization
	void Start () {

        gameObject.tag = "Untagged";//uncomment this when the script is ready for testing
		oldMesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh = gameObject.GetComponent<MeshFilter>().mesh; //start with a copy and then remove the parts you don't need
        int[] tris = mesh.triangles;

        while (!done){
            //use an ienumerator and run a recursive function to find contiguous tris on tri[0]
            done = true;
        }
        //after checking for contiguous pieces you can set the new pieces to "Cuttable"

        //Destroy(gameObject,1);
	}
	
	// Update is called once per frame
	void Update () {
        if (!isCuttable){
            timer += Time.deltaTime;
            if (timer > .5){
                gameObject.tag = "Cuttable";
                isCuttable = true;
            }
        }

	}
}

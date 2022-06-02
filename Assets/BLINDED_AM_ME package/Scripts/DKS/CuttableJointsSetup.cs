using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttableJointsSetup : CuttableMetal {
    //private Joint joint;

	// Use this for initialization
	void Start () {
		//joint = GetComponent<Joint>();
        SetupJoint(GetComponent<Joint>());
        gameObject.layer = 15;
        CuttableMetal cm = gameObject.AddComponent<CuttableMetal>();
        cm.InitUV2();
    }

    void SetupJoint(Joint joint){
        CuttableMetal cmParent = gameObject.GetComponent<CuttableMetal>();

        if (joint != null){
            Rigidbody rcb = joint.connectedBody;
            if (rcb != null) {
                GameObject cb = joint.connectedBody.gameObject;
                CuttableMetal cm = cb.GetComponent<CuttableMetal>();

                if (!cm)
                {
                    cm = cb.AddComponent<CuttableMetal> ();
                    cm.SetSmoothNormals(cmParent.GetSmoothNormals());
                    cm.angle = cmParent.GetSmoothNormalsAngle();
                }
                    cm.InitUV2();
                if (cb != null) {
                    JointConnection jc = cb.GetComponent<JointConnection> ();
                    if (!jc)
                        jc = cb.AddComponent<JointConnection> ();
                    jc.AddJoint (joint);
                    //jc.joint = joint;
                    //jc.point = //parent - connectedbody

                    Joint[] cbJoints = cb.GetComponents<Joint> ();
                    foreach (Joint cbj in cbJoints) {
                        SetupJoint (cbj);
                    }
                }
            }
        }
    }
	// Update is called once per frame
	//void Update () {
		
	//}
}

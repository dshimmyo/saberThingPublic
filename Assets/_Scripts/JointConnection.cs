using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//attach this to a thing that you are attaching to so that you can check to see if it's connected.
public class JointConnection : MonoBehaviour {
    public Joint joint;
    public Vector3 jointPosition;
    [SerializeField] private bool isAddMeshCutInstanceOnBreak = false;

    //private float maxTime = 1f;
    //private float timer = 0f;
    //public Vector3 point = Vector3.zero; //local space? too complicated
    public void Start(){


        //if (joint != null)
            //jointPosition = transform.InverseTransformPoint(gameObject.transform.position - joint.gameObject.transform.position + joint.anchor);
        //jointPosition = gameObject.transform.InverseTransformPoint(joint.gameObject.transform.position + joint.anchor);

    }

    public void SetAddMeshCutInstance(bool value)
    {
        isAddMeshCutInstanceOnBreak = value;
    }

    public void AddJoint(Joint newJoint){
        joint = newJoint;
        jointPosition = joint.connectedAnchor;//transform.InverseTransformPoint(joint.gameObject.transform.position + joint.connectedAnchor);
    }
    void Update()
    {  
        if (isAddMeshCutInstanceOnBreak){
            MeshCutInstance mci = gameObject.GetComponent<MeshCutInstance>();
            if (mci == null){
                if (joint == null)
                {
                    isAddMeshCutInstanceOnBreak = false;
                    gameObject.AddComponent<MeshCutInstance>();
                }
            }
        }

    }
}


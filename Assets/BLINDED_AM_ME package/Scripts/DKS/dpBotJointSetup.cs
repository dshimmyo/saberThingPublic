using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dpBotJointSetup : MonoBehaviour {
    [SerializeField] float skinWidth = .01f;
    Animator anim;
    JointConnection[] jc;
    Joint[] joints;
    [SerializeField] bool ignoreMissingJoints = false;
    [SerializeField] bool setupKinematics = true;
    bool killMePlease = false;//kill when there's no geo
    float emptyNestTimer = 0;
    //public bool isSetup = false;
    void Start () {
        //joint = GetComponent<Joint>();
        joints = GetComponentsInChildren<Joint>();
        SetupMeshColliders(gameObject);
        SetupJoints(gameObject);
        if (setupKinematics)
            SetupKinematics (gameObject);
        else
            UndoKinematics (gameObject);
        //if (setupKinematics)
            //anim = null;
        //else
            anim = GetComponent<Animator> ();
        //isSetup = true;
    }
    void Awake () {
        //SetupJoints (gameObject);
    }
    void OnEnable () {
        //SetupJoints (gameObject);
    }

    void EmptyNestCheck()
    {
        emptyNestTimer = 0;
        MeshRenderer[] mr = gameObject.GetComponentsInChildren<MeshRenderer>();
        if (mr.Length == 0)
        {
            killMePlease = true;
        }
    }
    void Update()
    {
        if (anim.enabled) 
        {
            for (int i = 0; i < joints.Length; i++) {
                if (joints [i] == null && !ignoreMissingJoints) {//if it finds a disconnected joint it goes to ragdoll
                    //Debug.Break();
                    anim.enabled = false;
                    UndoKinematics (gameObject);
                    break;
                }
            }
        }
        if (killMePlease)
        {
            Debug.Log("Killing myself");
            Destroy(gameObject);
        }
        if (emptyNestTimer > 5)
            EmptyNestCheck();
        emptyNestTimer += Time.deltaTime;
    }
    void SetupKinematics(GameObject go)
    {
        Rigidbody[] rc = go.GetComponentsInChildren<Rigidbody> ();
        EmptyNestCheck();
        if (rc.Length == 0)
            return;
        for (int i = 0; i < rc.Length; i++) {
            rc [i].isKinematic = true;
        }
    }
    void UndoKinematics(GameObject go)
    {
        Rigidbody[] rc = go.GetComponentsInChildren<Rigidbody> ();
        EmptyNestCheck();
        if (rc.Length == 0)
        {
            return;
        }
        for (int i = 0; i < rc.Length; i++) {
            rc [i].isKinematic = false;
        }
    }
    void SetupMeshColliders(GameObject go){
        MeshCollider[] mc = go.GetComponentsInChildren<MeshCollider> ();
        if (mc.Length == 0)
            return;
        for (int i = 0; i < mc.Length; i++) {
                mc[i].inflateMesh = true;
                mc[i].skinWidth = skinWidth;//.005f;//.01 is the default
                mc[i].gameObject.GetComponent<Cuttable>().SetBoundsVolume();
        }
    }
    void SetupJoints(GameObject go){
        JointConnection[] jc = go.GetComponentsInChildren<JointConnection> ();
        if (jc.Length == 0)
            return;
        for (int i = 0; i < jc.Length; i++) {
            if (jc[i].joint != null)
                jc[i].AddJoint (jc[i].joint);//redoing the addjoint script
                //Debug.Log("setting up dpJoint " + jc[i].gameObject.name);
        }
    }
    void SetupJoint(Joint joint){
        if (joint != null){
            Rigidbody rcb = joint.connectedBody;
            if (rcb != null) {
                GameObject cb = joint.connectedBody.gameObject;
                if (cb != null) {
                    Cuttable cm = cb.GetComponent<Cuttable>();
                    if (!cm)
                        cm = cb.AddComponent<CuttableMetal>();
                    cm.InitUV22();
                    JointConnection jc = cb.GetComponent<JointConnection> ();
                    if (!jc)
                        jc = cb.AddComponent<JointConnection> ();
                    jc.AddJoint (joint);
                    //jc.joint = joint;
                    //jc.point = //parent - connectedbody

                    Joint[] cbJoints = cb.GetComponents<Joint> ();
                    foreach (Joint cbj in cbJoints) {
                        SetupJoint (cbj);
                        cb.GetComponent<Cuttable> ().SetBoundsVolume ();
                    }
                }
            }
        }
    }
}

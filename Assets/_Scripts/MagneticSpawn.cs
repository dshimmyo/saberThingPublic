using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagneticSpawn : MonoBehaviour {
    [SerializeField]
    private GameObject spawnee;
    [SerializeField] float yOffset=0;
    private Joint fj;
    private bool isTriggerOccupied = false;
    private float respawnTimer = 0f;
    [SerializeField]
    private float respawnTime = 20f;
    private GameObject prefab;
    private string spawneeChildName="";//this gets populated when the first triggerEnter happens.
    private string origSpawneeChildName="";
    public enum JointType {Fixed,Spring,Configurable};
    private long numRenamed=0;
    [SerializeField] JointType myJointType;
    [SerializeField] bool usePrefab = true;
    [SerializeField] bool useStickyJoint = true;
    [SerializeField] float maxVolume = .75f;
    [SerializeField] float breakForce = 500000f;
    [SerializeField] bool ignoreTrigger = false;
    // Use this for initialization
	void Start () {
		respawnTimer = 0;
        SetUpMainJoint ();
        if (usePrefab)
            prefab = Resources.Load ("prefabs/" + spawnee.name) as GameObject;
        else
            prefab = spawnee;

    }
    void SetUpMainJoint(){
        SpringJoint sj;
        ConfigurableJoint cj;
        if (myJointType == JointType.Spring) {
            sj = gameObject.AddComponent<SpringJoint> () as SpringJoint;
            sj.spring = 100000000f;
            fj = sj;
        } else if (myJointType == JointType.Fixed){
            fj = gameObject.AddComponent<FixedJoint> () as FixedJoint;
        } else if (myJointType == JointType.Configurable){
            cj = gameObject.AddComponent<ConfigurableJoint> () as ConfigurableJoint;
            cj.xMotion = ConfigurableJointMotion.Limited;
            cj.yMotion = ConfigurableJointMotion.Limited;//was locked
            cj.zMotion = ConfigurableJointMotion.Limited;
            cj.angularXMotion = ConfigurableJointMotion.Limited;
            cj.angularYMotion = ConfigurableJointMotion.Limited;
            cj.angularZMotion = ConfigurableJointMotion.Limited;
            SoftJointLimit sjl = new SoftJointLimit();
            SoftJointLimitSpring sjls = new SoftJointLimitSpring ();
            sjl.limit = .0001f;
            sjl.bounciness = 0f;
            sjl.contactDistance = 1f;
            cj.linearLimit = sjl;
            sjls.spring = 0f;//1000000f;//10mil and above is bad
            sjls.damper = 0f;//1f;
            cj.linearLimitSpring = sjls;
            fj = cj;
        }
        fj.breakForce = breakForce;
        fj.breakTorque = breakForce;
    }
    void FixedUpdate () {
        isTriggerOccupied = false;//start fresh for each event loop
    }
    void AttachCollider(Collider other){
        if (other.gameObject.tag == "Cuttable"){
            if (fj != null)
            if (fj.connectedBody == null && other.gameObject.name == spawneeChildName && other.gameObject.transform.root.name == spawnee.name) {
                Debug.Log ("Attaching " + other.gameObject.name + " to " + gameObject.name);
                fj.connectedBody = other.gameObject.GetComponent<Rigidbody> ();
                if (!fj.connectedBody.gameObject.GetComponent<JointConnection>()){
                    JointConnection jc = fj.connectedBody.gameObject.AddComponent<JointConnection>();
                    jc.AddJoint (fj);
                    jc.SetAddMeshCutInstance(true);
                }
            }
        }
    }
   void OnTriggerEnter(Collider other){
        if (spawneeChildName == "") {//establishes the first connection, doesn't deal with duplicate names
            spawneeChildName = other.gameObject.name;
            origSpawneeChildName = spawneeChildName;
        }
        if (useStickyJoint)
            AttachCollider (other);
        isTriggerOccupied = true;

   }
    void OnTriggerExit(Collider other){
        //Destroy(other.gameObject.GetComponent<JointConnection>());//leave this for debugging
    }
    void OnTriggerStay(Collider other){
        isTriggerOccupied = true;
    }

    void Update ()
    {
        if (fj == null)
            SetUpMainJoint ();
        if (respawnTimer > respawnTime)
        {
            respawnTimer = 0;

            if (!isTriggerOccupied) {
                spawnee = Instantiate(prefab);//Instantiate(spawnee);
                Cuttable cc = spawnee.GetComponent<Cuttable>();
                if (cc != null)
                    cc.InitUV2();//maybe too hacky
                spawnee.SetActive(false);
                spawnee.name = prefab.name;
                spawnee.transform.position = new Vector3(gameObject.transform.position.x, yOffset, gameObject.transform.position.z);
                //respawnTimer = 0;
                RenameChildren(spawnee.transform);//breaks the fixed joint
                fj.connectedBody = null;
                spawnee.SetActive(true);
                if (useStickyJoint)
                    AttachCollider(GameObject.Find(spawneeChildName).GetComponent<Collider>());
                isTriggerOccupied = true;
            }
        }
        if (fj != null) 
        {
            if (fj.connectedBody != null)
            {
                float bodyVolume = fj.connectedBody.gameObject.GetComponent<Cuttable> ().GetBoundsVolume ();
                if (bodyVolume < maxVolume && fj.connectedBody.name != spawneeChildName) {
                    Destroy (fj.connectedBody.GetComponent<JointConnection> ());
                    fj.connectedBody = null;
                }
            }
        }
    }
    void RenameChildren(Transform parentTransform){
        Transform[] transforms = parentTransform.GetComponentsInChildren<Transform>();
        for (int i=1; i<transforms.Length; i++){
            string oldTransformName = transforms [i].name;
            MeshFilter mf = transforms[i].gameObject.GetComponent<MeshFilter>();
            Cuttable cc = transforms[i].gameObject.GetComponent<Cuttable>();
            if (cc != null)
                cc.InitUV2();
            if (mf != null)
                transforms[i].name = (transforms[i].name+":cut:"+numRenamed++.ToString());
            if (oldTransformName == origSpawneeChildName) {
                spawneeChildName = transforms[i].name;
                //AttachCollider (transforms [i].GetComponent<Collider> ());
            }
        }
        //if (transforms.Length == 1)
            //AttachCollider (transforms[0].GetComponent<Collider> ());
    }
    void LateUpdate ()
    {
        if (!isTriggerOccupied) {
            if (fj.connectedBody == null)
                respawnTimer += Time.deltaTime;
        } else {
            respawnTimer = 0f;
        }
    }

}

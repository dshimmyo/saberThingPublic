using UnityEngine;
//using UnityEditor;
using System;
using System.Collections;

/*
 * This class can be applied to any game object with a Mesh Filter component.
 * It will draw the normal lines for the component if there is an assigned mesh.
 */

public enum NormalMode{Vertex,Polygon};
public enum FaceNormalMode{AveragedVertices,Orthogonal};

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class DisplayMeshFilterNormals : MonoBehaviour {
	
	bool visible = true;

	public Color normalColor = Color.red;
	public float normalLength = 1f;
	public bool reversed = false;
	public NormalMode normalMode = NormalMode.Vertex;
	public FaceNormalMode faceNormalMode = FaceNormalMode.Orthogonal;
	public GameObject parentGameObject;

	public Mesh sourceMesh;
	public Mesh reversedMesh;


	GameObject normalsGameObject;
	QMesh mesh;			//custom data structure for storing mesh, flipped mesh and drawing normals


	MeshFilter filter;


	Mesh prevMesh;


	void Init()
	{
		mesh = new QMesh ();		//temporary flipped mesh

		if (filter != null) {

			if (filter.sharedMesh == null) {
				//assign source mesh to mesh filter if it isn't assigned
				filter.sharedMesh = sourceMesh;
			}
			else {
				//set source mesh to mesh filter
				sourceMesh = filter.sharedMesh;
			}

			//build reverse shared mesh
			reversedMesh = CopyMesh(sourceMesh,true);
			reversedMesh.name = "(flipped) "+filter.sharedMesh.name;

			//copy the current mesh to data structure
			Mesh copyMesh = CopyMesh(sourceMesh,false);
			mesh.AssignMesh(copyMesh);

			UpdateNormals();
		}

	}

	void Start ()
	{
		filter = GetComponent<MeshFilter> ();
		Init();		//initialize custom mesh data structure, source and reversed mesh
	}

	Mesh CopyMesh (Mesh m,bool reverseNormals)
	{
		Mesh newMesh = new Mesh ();

		Vector3[] vertices = new Vector3[m.vertices.Length];
		Vector3[] normals = new Vector3[m.normals.Length];
		Vector2[] uv = new Vector2[m.uv.Length];
		int[] triangles = new int[m.triangles.Length];
		BoneWeight[] boneWeights = new BoneWeight[m.boneWeights.Length];
		Matrix4x4[] bindPoses = new Matrix4x4[m.bindposes.Length];

		for (int i = 0; i < m.vertices.Length; i++) {
			vertices[i] = new Vector3(m.vertices[i].x,m.vertices[i].y,m.vertices[i].z);
		}
		for (int i = 0; i < m.normals.Length; i++) {
			normals[i]= new Vector3(m.normals[i].x,m.normals[i].y,m.normals[i].z);
			if (reverseNormals) normals [i] = -normals [i];
		}
		for (int i = 0; i < m.uv.Length; i++) {
			uv[i]= new Vector2(m.uv[i].x,m.uv[i].y);
		}
		for (int i = 0; i < m.triangles.Length; i++) {
			triangles[i]= m.triangles[i];
		}

		if (reverseNormals) {
			for (int i = 0; i < m.triangles.Length; i+=3) {
				int ta = triangles [i];
				int tb = triangles [i + 1];
				//int tc = triangles [i + 2];

				triangles [i + 0] = tb;
				triangles [i + 1] = ta;
			}
		}

		for (int i = 0; i < m.boneWeights.Length; i++) {
			boneWeights [i] = new BoneWeight ();
			boneWeights[i].boneIndex0 = m.boneWeights[i].boneIndex0;
			boneWeights[i].boneIndex1 = m.boneWeights[i].boneIndex1;
			boneWeights[i].boneIndex2 = m.boneWeights[i].boneIndex2;
			boneWeights[i].boneIndex3 = m.boneWeights[i].boneIndex3;
			boneWeights[i].weight0 = m.boneWeights[i].weight0;
			boneWeights[i].weight1 = m.boneWeights[i].weight1;
			boneWeights[i].weight2 = m.boneWeights[i].weight2;
			boneWeights[i].weight3 = m.boneWeights[i].weight3;
		}
		for (int i = 0; i < m.bindposes.Length; i++) {
			bindPoses [i] = m.bindposes [i];
		}

		newMesh.vertices = vertices;
		newMesh.normals = normals;
		newMesh.uv = uv;
		newMesh.triangles = triangles;
		newMesh.boneWeights = boneWeights;
		newMesh.bindposes = m.bindposes;
		newMesh.name = m.name;

		return newMesh;
	}


	void OnDestroy ()
	{
		
	}
	void OnEnable () {
		visible = true;
	}

	void OnDisable () {
		visible = false;
	}

	public void SaveMesh(){
		//copy source mesh again to reversed mesh in case it changed
		reversedMesh = CopyMesh(sourceMesh,true);
		reversedMesh.name = "(flipped) "+filter.sharedMesh.name;
	}
	public void UpdateNormals ()
	{
		
		if (Application.isPlaying&&reversed) {
			mesh.FlippedNormals();
			filter.sharedMesh = reversedMesh;
		}
		else {
			mesh.DefaultNormals();
			if (filter.sharedMesh == prevMesh) {
				filter.sharedMesh = sourceMesh;
			}
			else {
				sourceMesh = filter.sharedMesh;
			}
		}
		prevMesh = filter.sharedMesh;

	}
	void Update(){
		
	}


	public void Refresh ()
	{
		if (visible&&mesh.vertices != null&&filter!=null) {
			normalsGameObject = new GameObject ();
			try{
				normalsGameObject.transform.position = transform.position;
				normalsGameObject.transform.eulerAngles = transform.eulerAngles;
				normalsGameObject.transform.localScale = transform.lossyScale;

				UpdateNormals();

				mesh.calculateNormals(normalsGameObject.transform,normalLength,reversed,normalMode,faceNormalMode);
				mesh.DrawNormalLines(normalMode,normalColor);
			}
			catch(Exception e){
				DestroyImmediate(normalsGameObject);
			}
			DestroyImmediate(normalsGameObject);
		}
	}
	void OnDrawGizmos ()
	{
		Refresh();
	}


	public void ResetToParent(){
		
		if (parentGameObject!=null&&parentGameObject.GetComponent<DisplayGameObjectNormals>() != null) {
			DisplayGameObjectNormals dgon = parentGameObject.GetComponent<DisplayGameObjectNormals>();

			if (filter != null) {
				//Assign properties to Mesh Filters
				DisplayMeshFilterNormals d = this.GetComponent<DisplayMeshFilterNormals> ();

				d.normalColor = dgon.normalColor;
				d.faceNormalMode = dgon.faceNormalMode;
				d.normalLength = dgon.normalLength;
				d.normalMode = dgon.normalMode;
				d.reversed = dgon.reversed;

			}
		}
	}

}
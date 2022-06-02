using UnityEngine;
using System.Collections;

/*
 * This class is meant to be applied to a GameObject with Mesh Filters or Skinned Mesh Renderers 
 * in its hierarchy. It will traverse down the tree at runtime appending the relevant scripts
 * for drawing the normals on each mesh.
 */
[ExecuteInEditMode]
public class DisplayGameObjectNormals : MonoBehaviour {
	
	public Color normalColor = Color.red;
	public float normalLength = 0.1f;
	public bool reversed = false;
	public NormalMode normalMode = NormalMode.Vertex;
	public FaceNormalMode faceNormalMode = FaceNormalMode.Orthogonal;

	Color lastNormalColor;
	float lastNormalLength;
	bool lastReversed;

	NormalMode lastNormalMode;
	FaceNormalMode lastFaceNormalMode;


	void Start () {
		AppendDisplayNormalsComponent (transform);
	}


	void AppendDisplayNormalsComponent (Transform t)
	{
		if (t.gameObject.GetComponent<MeshFilter> () != null) {
			if (t.gameObject.GetComponent<DisplayMeshFilterNormals> () == null) {
				t.gameObject.AddComponent<DisplayMeshFilterNormals> ();
				UpdateDisplayNormals (t.gameObject, gameObject);
			}

		}
		else if (t.gameObject.GetComponent<SkinnedMeshRenderer> () != null) {
			if (t.gameObject.GetComponent<DisplaySkinnedMeshNormals> () == null) {
				t.gameObject.AddComponent<DisplaySkinnedMeshNormals>();
				UpdateDisplayNormals(t.gameObject,gameObject);

			}
		}

		for(int i=0;i<t.childCount;i++){
			AppendDisplayNormalsComponent(t.GetChild(i));
		}
	}

	void Update(){
		
	}
		
	public void OnEnable(){
		//AppendDisplayNormalsComponent (transform);
		SetDisplayNormalsState(gameObject,true);
	}

	public void OnDisable(){
		//DestroyNormals (transform);
		SetDisplayNormalsState(gameObject,false);
	}

	void SetDisplayNormalsState(GameObject g,bool state){
		
		if (g.GetComponent<DisplayMeshFilterNormals> () != null) {
			g.GetComponent<DisplayMeshFilterNormals> ().enabled = state;
		}
		else if (g.GetComponent<DisplaySkinnedMeshNormals> () != null) {
			g.GetComponent<DisplaySkinnedMeshNormals> ().enabled = state;

		}

		for (int i=0; i<g.transform.childCount; i++) {
			//recurse down the hierarchy updating all children
			SetDisplayNormalsState(g.transform.GetChild(i).gameObject,state);
		}
	}



	void UpdateDisplayNormals(GameObject g, GameObject parent){
		
		if (g.GetComponent<MeshFilter> () != null) {
			if (g.GetComponent<DisplayMeshFilterNormals> ()) {
				g.GetComponent<DisplayMeshFilterNormals> ().normalColor = normalColor;
				g.GetComponent<DisplayMeshFilterNormals> ().normalLength = normalLength;
				g.GetComponent<DisplayMeshFilterNormals> ().normalMode = normalMode;
				g.GetComponent<DisplayMeshFilterNormals> ().reversed = reversed;
				g.GetComponent<DisplayMeshFilterNormals> ().faceNormalMode = faceNormalMode;
				g.GetComponent<DisplayMeshFilterNormals> ().parentGameObject = parent;
			}
		}
		else  if (g.GetComponent<SkinnedMeshRenderer> () != null) {
			if (g.GetComponent<DisplaySkinnedMeshNormals> ()) {
				g.GetComponent<DisplaySkinnedMeshNormals> ().normalColor = normalColor;
				g.GetComponent<DisplaySkinnedMeshNormals> ().normalLength = normalLength;
				g.GetComponent<DisplaySkinnedMeshNormals> ().normalMode = normalMode;
				g.GetComponent<DisplaySkinnedMeshNormals> ().reversed = reversed;
				g.GetComponent<DisplaySkinnedMeshNormals> ().faceNormalMode = faceNormalMode;
				g.GetComponent<DisplaySkinnedMeshNormals> ().parentGameObject = parent;
			}
		}
	}

	bool EqualToGenericSettings(GameObject g){
		
		if (g.GetComponent<DisplayMeshFilterNormals> () != null) {
			DisplayMeshFilterNormals d = g.GetComponent<DisplayMeshFilterNormals> ();

			if(d.reversed==lastReversed&&
				d.normalColor == lastNormalColor&&
				d.normalLength == lastNormalLength&&
				d.normalMode ==lastNormalMode&&
				d.faceNormalMode ==lastFaceNormalMode){
				return true;
			}
			else{
				return false;
			}

			return false;
		}
		else if (g.GetComponent<DisplaySkinnedMeshNormals> () != null) {
			DisplaySkinnedMeshNormals d = g.GetComponent<DisplaySkinnedMeshNormals> ();
			if(d.reversed == lastReversed &&
				d.normalColor == lastNormalColor &&
				d.normalLength == lastNormalLength &&
				d.normalMode == lastNormalMode &&
				d.faceNormalMode == lastFaceNormalMode){
				return true;
			}
			else{
				return false;
			}
		}

		return true;

	}

	void UpdateMeshFilterNormals(GameObject g){
		if(EqualToGenericSettings(g)){
			//update if still considered generic
			if (!lastReversed && reversed) {
				g.GetComponent<DisplayMeshFilterNormals> ().reversed = true;

			}
			else if (lastReversed && !reversed) {
				g.GetComponent<DisplayMeshFilterNormals> ().reversed = false;
			}

			UpdateDisplayNormals(g,gameObject);
			g.GetComponent<DisplayMeshFilterNormals> ().Refresh();
		}
	}

	void UpdateSkinnedMeshFilterNormals(GameObject g){
		
		if(EqualToGenericSettings(g)){
			//update if still considered generic

			if (!lastReversed && reversed) {
				g.GetComponent<DisplaySkinnedMeshNormals> ().reversed = true;

			} else if (lastReversed && !reversed) {
				g.GetComponent<DisplaySkinnedMeshNormals> ().reversed = false;
			}

			UpdateDisplayNormals(g,gameObject);
			g.GetComponent<DisplaySkinnedMeshNormals> ().Refresh();

		}

	}

	void UpdateGenericDisplayNormals(GameObject g){
		
		if (g.GetComponent<DisplayMeshFilterNormals> () != null) {
			UpdateMeshFilterNormals (g);
		}
		else if (g.GetComponent<DisplaySkinnedMeshNormals> () != null) {
			UpdateSkinnedMeshFilterNormals (g);
		}

		for (int i=0; i<g.transform.childCount; i++) {
			//recurse down the hierarchy updating all children
			UpdateGenericDisplayNormals(g.transform.GetChild(i).gameObject);
		}

	}

	void OnDrawGizmos(){
		
		UpdateGenericDisplayNormals(this.gameObject);

		//keep track of changes
		lastNormalColor = normalColor;
		lastReversed = reversed;
		lastNormalMode = normalMode;
		lastFaceNormalMode = faceNormalMode;
		lastNormalLength = normalLength;

	}
	#region DESTROY
	public void DestroyNormals(Transform t){
		if (t.gameObject.GetComponent<MeshFilter> () != null) {
			if (t.gameObject.GetComponent<DisplayMeshFilterNormals> ()!=null) {
				DestroyImmediate (t.gameObject.GetComponent<DisplayMeshFilterNormals> ());
			}
		}

		else if (t.gameObject.GetComponent<SkinnedMeshRenderer> () != null) {
			if (t.gameObject.GetComponent<DisplaySkinnedMeshNormals> ()!=null) {
				DestroyImmediate (t.gameObject.GetComponent<DisplaySkinnedMeshNormals> ());
			}
		}

		for (int i = 0; i < t.childCount; i++) {
			DestroyNormals (t.GetChild (i));
		}
	}
	public void OnDestroy(){
		//destroy all children components
		DestroyNormals (transform);
	}
	#endregion

}
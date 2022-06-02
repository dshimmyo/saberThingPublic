using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(DisplaySkinnedMeshNormals))]
public class DisplaySkinnedMeshNormalsEditor : Editor {

	public override void OnInspectorGUI(){

		DisplaySkinnedMeshNormals myScript = (DisplaySkinnedMeshNormals)target;
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Normal Properties",EditorStyles.boldLabel);

		myScript.parentGameObject = (GameObject) EditorGUILayout.ObjectField ("Parent", myScript.parentGameObject, typeof(GameObject),false);
		myScript.sourceMesh = (Mesh) EditorGUILayout.ObjectField ("Source Mesh", myScript.sourceMesh, typeof(Mesh),false);
		myScript.normalColor = EditorGUILayout.ColorField ("Color", myScript.normalColor);
		myScript.normalLength = EditorGUILayout.FloatField ("Length", myScript.normalLength);
		myScript.reversed = EditorGUILayout.Toggle ("Reversed (Play Mode Only)", myScript.reversed);
		myScript.normalMode = (NormalMode) EditorGUILayout.EnumPopup ("Mode", myScript.normalMode);

		if (myScript.normalMode == NormalMode.Polygon) {
			myScript.faceNormalMode = (FaceNormalMode) EditorGUILayout.EnumPopup ("Face Normal Mode", myScript.faceNormalMode);	
		}
		if (myScript.parentGameObject != null) {
			if (GUILayout.Button ("Constrain Normals to Parent GameObject Settings")) {
				myScript.ResetToParent ();
			}
		}

		//save button
		if(GUILayout.Button("Export Reversed Mesh Asset"))
		{
			string path = EditorUtility.SaveFilePanelInProject( "Export Reversed Mesh Asset", "myasset" + ".asset", "asset", "Enter the name of the new asset:" );
			Debug.Log(path);
			myScript.SaveMesh();

			AssetDatabase.CreateAsset( myScript.reversedMesh, path );
			AssetDatabase.SaveAssets();
		}
		EditorUtility.SetDirty(myScript);
	}
}

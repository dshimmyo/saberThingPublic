using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(DisplayGameObjectNormals))]
public class DisplayGameObjectNormalsEditor : Editor {

	public override void OnInspectorGUI(){
		
		DisplayGameObjectNormals myScript = (DisplayGameObjectNormals)target;
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Normal Properties",EditorStyles.boldLabel);

		myScript.normalColor = EditorGUILayout.ColorField ("Color", myScript.normalColor);
		myScript.normalLength = EditorGUILayout.FloatField ("Length", myScript.normalLength);
		myScript.reversed = EditorGUILayout.Toggle ("Reversed (Play Mode Only)", myScript.reversed);
		myScript.normalMode = (NormalMode) EditorGUILayout.EnumPopup ("Mode", myScript.normalMode);

		if (myScript.normalMode == NormalMode.Polygon) {
			myScript.faceNormalMode = (FaceNormalMode) EditorGUILayout.EnumPopup ("Face Normal Mode", myScript.faceNormalMode);	
		}

		EditorUtility.SetDirty(myScript);

	}
}

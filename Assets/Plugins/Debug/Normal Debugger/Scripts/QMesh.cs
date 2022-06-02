using UnityEngine;
using System.Collections;

/* Data structure for storing the normal lines on the mesh
 * 
 */
public class QMesh : Object {
	protected Vector3[] assignedVertices;
	public Vector3[] assignedNormals;
	protected Vector2[] assignedUVs;
	protected int[] assignedTriangles;

	public Vector3[] vertices;
	public Vector3[] normals;
	public Vector2[] uvs;
	public int[] triangles;

	//normals are draw in global position so they need their starting and ending points store separately
	public Vector3[] startVertexNormalPosition;
	public Vector3[] endVertexNormalPosition;
	
	public Vector3[] startFaceNormalPosition;
	public Vector3[] endFaceNormalPosition;

	public Mesh mesh;

	public QMesh(){
		mesh = null;
		vertices = normals = null;
		triangles = null;
		startVertexNormalPosition = endVertexNormalPosition = startFaceNormalPosition = endFaceNormalPosition = null;
	}


	public void AssignMesh (Mesh m)
	{


		//assign mesh to the data structure
		mesh = new Mesh ();
		//mesh = m;

		//assignedVertices = mesh.vertices;
		//assignedNormals = mesh.normals;
		//assignedTriangles = mesh.triangles;

		vertices = new Vector3[m.vertices.Length];
		normals = new Vector3[m.normals.Length];
		uvs = new Vector2[m.uv.Length];
		triangles = new int[m.triangles.Length];

		assignedVertices = new Vector3[m.vertices.Length];
		assignedNormals = new Vector3[m.normals.Length];
		assignedUVs = new Vector2[m.uv.Length];
		assignedTriangles = new int[m.triangles.Length];

		//store original positions



		for (int i = 0; i < vertices.Length; i++) {
			assignedVertices [i] = new Vector3 (m.vertices [i].x, m.vertices [i].y, m.vertices [i].z);
			vertices [i] = new Vector3 (m.vertices [i].x, m.vertices [i].y, m.vertices [i].z);
			//Debug.Log(i+" "+vertices[i]);
		}
		for (int i = 0; i < normals.Length; i++) {
			assignedNormals [i] = new Vector3 (m.normals [i].x, m.normals [i].y, m.normals [i].z);
			normals [i] = new Vector3 (m.normals [i].x, m.normals [i].y, m.normals [i].z);
			//Debug.Log(i+" "+normals[i]);
		}
		for (int i = 0; i < uvs.Length; i++) {
			assignedUVs[i]= new Vector2(m.uv[i].x,m.uv[i].y);
			uvs [i] = new Vector2 (m.uv [i].x, m.uv [i].y);
		}
		for (int i = 0; i < triangles.Length; i++) {
			assignedTriangles[i] = m.triangles[i];
			triangles[i] = m.triangles[i];
			//Debug.Log(i+" "+triangles[i]);
		}

		//create arrays for the normal positions
		startVertexNormalPosition = new Vector3[vertices.Length];
		endVertexNormalPosition = new Vector3[vertices.Length];

		startFaceNormalPosition = new Vector3[triangles.Length/3];
		endFaceNormalPosition = new Vector3[triangles.Length/3];

		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.name = "blah";


	}
	


	public void calculateNormals(Transform t,float length, bool reversed,NormalMode mode,FaceNormalMode fmode){
		switch (mode) {
		case NormalMode.Polygon:
			calculateFaceNormalPositions(t,length,reversed,fmode);
			break;
		case NormalMode.Vertex:
			calculateVertexNormalPositions (t,length,reversed);
			break;
		default:
			break;
		}
		
	}


	void calculateVertexNormalPositions(Transform t,float length,bool reversed){
		//assign transform information to a temp game object
		/*
		GameObject temp = new GameObject ();
		temp.transform.localScale = t.localScale;
		temp.transform.position = t.position;
		temp.transform.rotation = t.rotation;
		*/
		//extract and store global scale inside the temp game object's localscale
		/*
		Transform parent = t.parent;
		while (parent!=null) {
			//temp.transform.localScale = new Vector3(temp.transform.localScale.x*parent.localScale.x,temp.transform.localScale.y*parent.localScale.y,temp.transform.localScale.z*parent.localScale.z);
			parent = parent.parent;
		}
		*/

		if (vertices != null) {
			for (int i = 0; i < vertices.Length; i++) {
				//transform starting vertices to global space
				startVertexNormalPosition [i] = vertices [i];
				//startVertexNormalPosition [i] = temp.transform.TransformPoint (startVertexNormalPosition [i]);
				startVertexNormalPosition [i] = t.TransformPoint (startVertexNormalPosition [i]);

				//draw end positions based on the normals length, only use the rotation value and add it to the starting position to get the ending global position of the line
				normals[i] = Vector3.Normalize(normals[i]);

				endVertexNormalPosition [i] = (normals [i] * length);
				//endVertexNormalPosition [i] = Quaternion.Euler (temp.transform.localEulerAngles.x, temp.transform.localEulerAngles.y, temp.transform.localEulerAngles.z) * endVertexNormalPosition [i];

				endVertexNormalPosition [i] = Quaternion.Euler (t.localEulerAngles.x, t.localEulerAngles.y, t.localEulerAngles.z) * endVertexNormalPosition [i];
				endVertexNormalPosition [i] += startVertexNormalPosition [i];

			}
		}
		//DestroyImmediate (temp);
	}

	void calculateFaceNormalPositions(Transform t,float length,bool reversed,FaceNormalMode mode){

		for (int m=0;m<mesh.subMeshCount;m++)
		{
			int[] triangles = mesh.GetTriangles(m);
		
			for (int i=0;i<triangles.Length;i+=3)
			{
				//draw line from center of face defined by the average of the 3 vertices of the triangle
				Vector3 centroid = (vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3f;
				centroid = t.TransformPoint(centroid);
				startFaceNormalPosition[i/3]= centroid;
				
				Vector3 normal = Vector3.zero;
				
				switch(mode){
				case FaceNormalMode.AveragedVertices:
					normal = (normals[triangles[i]]+normals[triangles[i+1]]+normals[triangles[i+2]])/3f;
					normal = Vector3.Normalize(normal);
					if(reversed&&Application.isPlaying) normal*=-1;
					break;
				case FaceNormalMode.Orthogonal:
					normal = Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]);
					normal = Vector3.Normalize(normal);
					if(reversed&&Application.isPlaying) normal*=-1;
					break;
				default:
					break;
					
				}
				
				endFaceNormalPosition[i/3]= normal*length;
				if(reversed&&Application.isPlaying) endFaceNormalPosition[i/3]*=-1f;
				endFaceNormalPosition[i/3] = Quaternion.Euler(t.localEulerAngles.x, t.localEulerAngles.y, t.localEulerAngles.z) * endFaceNormalPosition[i/3];
				endFaceNormalPosition[i/3]+=startFaceNormalPosition[i/3];
			}
			
		}
	}


	
	public void FlippedNormals ()
	{
		
		//flip normals
		if (assignedNormals != null) {
			for (int i = 0; i < assignedNormals.Length; i++) 
			{
				normals [i] = new Vector3 (-assignedNormals [i].x, -assignedNormals [i].y, -assignedNormals [i].z);
			}
			//assign new normals to the mesh
			mesh.normals = normals;
		}

		if (triangles != null) {
			for (int m = 0; m < mesh.subMeshCount; m++) {
				for (int i = 0; i < triangles.Length; i += 3) {
					
					triangles [i + 0] = assignedTriangles [i + 1];
					triangles [i + 1] = assignedTriangles [i + 0];
				}
				mesh.SetTriangles (triangles, m);
			}
		}

	}

	public void DefaultNormals(){
		//flip normals

		if (assignedNormals != null) {
			
			for (int i = 0; i < assignedNormals.Length; i++)
				normals [i] = new Vector3(assignedNormals[i].x,assignedNormals[i].y,assignedNormals[i].z);

			
			//assign new normals to the mesh
			mesh.normals = normals;
		}

		if (assignedTriangles != null) {
			for (int m = 0; m < mesh.subMeshCount; m++) {
				for (int i = 0; i < triangles.Length; i += 3) {
					triangles [i + 0] = assignedTriangles [i + 0];
					triangles [i + 1] = assignedTriangles [i + 1];
				}
				mesh.SetTriangles (triangles, m);
			}
		}

	}

	public void DrawNormalLines(NormalMode mode, Color color){
		Gizmos.color = color;

		switch (mode) {
		case NormalMode.Polygon:
			drawFaceNormalLines(color);
			break;
		case NormalMode.Vertex:
			drawVertexNormalLines(color);
			break;
		default:
			break;
		}
	}


	void drawFaceNormalLines(Color c){
		if (startFaceNormalPosition == null) return;

		for (int i = 0; i < startFaceNormalPosition.Length; i++) {
			Gizmos.DrawLine (startFaceNormalPosition [i], endFaceNormalPosition [i]);
		}
	}
	
	void drawVertexNormalLines(Color c){
		if (vertices == null) return;
		//Debug.Log(mesh.vertices.Length);

		for (int i = 0; i < mesh.vertices.Length; i++) {
			//if(i==0) Debug.Log(i+" "+startFaceNormalPosition[i]+" "+endVertexNormalPosition[i]);
			//Debug.Log(i+" "+startVertexNormalPosition[i]+" "+endVertexNormalPosition[i]);
			Gizmos.DrawLine (startVertexNormalPosition [i], endVertexNormalPosition [i]);
		}
	}


}

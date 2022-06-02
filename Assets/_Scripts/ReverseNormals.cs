using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class ReverseNormals : MonoBehaviour {
    [SerializeField] bool squashBottom = false;
    void Start () {
        MeshFilter filter = GetComponent(typeof (MeshFilter)) as MeshFilter;
        if (filter != null)
        {
            Mesh mesh = filter.mesh;

            Vector3[] normals = mesh.normals;
            for (int i=0;i<normals.Length;i++)
                normals[i] = -normals[i];
            mesh.normals = normals;

            for (int m=0;m<mesh.subMeshCount;m++)
            {
                int[] triangles = mesh.GetTriangles(m);

                for (int i=0;i<triangles.Length;i+=3)
                {
                    int temp = triangles[i + 0];
                    triangles[i + 0] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }

                mesh.SetTriangles(triangles, m);
            }

            if (squashBottom) {
                Vector3[] vertices = mesh.vertices;
                for (int v = 0; v < vertices.Length; v++)
                {
                    if (vertices[v].y < 0)
                        vertices[v].y = 0;
                }
                mesh.vertices = vertices;
            }
        }
        Destroy(this);
    }
}
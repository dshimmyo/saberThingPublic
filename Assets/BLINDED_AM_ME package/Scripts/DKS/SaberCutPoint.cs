using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaberCutPoint {
    public Vector3 vertex; 
    public Vector3 normal;
    public Vector2 uv;
    public Vector2 uv2;
    public Vector4 tangent;

    public SaberCutPoint (Vector3 newVertex, Vector3 newNormal, Vector2 newUv, Vector4 newTangent){
        vertex = newVertex;
        normal = newNormal;
        uv = newUv;
        tangent = newTangent;
    }
    public SaberCutPoint (Vector3 newVertex, Vector3 newNormal, Vector2 newUv, Vector2 newUv2, Vector4 newTangent){
        vertex = newVertex;
        normal = newNormal;
        uv = newUv;
        uv2 = newUv2;
        tangent = newTangent;
    }
}

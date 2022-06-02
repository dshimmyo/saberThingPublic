
//    MIT License
//    
//    Copyright (c) 2017 Dustin Whirle
//    
//    My Youtube stuff: https://www.youtube.com/playlist?list=PL-sp8pM7xzbVls1NovXqwgfBQiwhTA_Ya
//    
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//    
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//    
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace BLINDED_AM_ME{
	
	public class Mesh_Maker{

        // Mesh Values
        private Vector3[] _vertices;//  = new Vector3[100];//start with arbitrary number element?
        private Vector3[] _normals;//   = new Vector3[100];//new List<Vector3>();
        private Vector2[] _uvs;//       = new Vector2[100];//new List<Vector2>();
        private Vector2[] _uv2;//       = new Vector2[100];//new List<Vector2>();
        private Vector4[] _tangents;//  = new Vector4[100];//new List<Vector4>();
        private int numVertices = 0;
        private int numNormals = 0;
        private int numUVs = 0;
        private int numUV2 = 0;
        private int numTangents = 0;
		private List<List<int>> _subIndices = new List<List<int>>();

    public Mesh_Maker()
    {
        InitArrays(100);
        //Debug.Log("Mesh_Maker Constructor");
        //IsInitialized = true;
    }
    public Mesh_Maker(int size)
    {
        InitArrays(size);
        //Debug.Log("Mesh_Maker Constructor resize");
        //IsInitialized = true;
    }


		public int VertCount{

			get{
				return numVertices;//_vertices.Length;//
			}
		}

        private void AddToArray(Vector2 vec, ref Vector2[] vecArray, ref int count){
            vecArray[count++] = vec;
        }
        private void AddToArray3(Vector2 vec1,Vector2 vec2,Vector2 vec3, ref Vector2[] vecArray, ref int count){
            vecArray[count++] = vec1;
            vecArray[count++] = vec2;
            vecArray[count++] = vec3;
        }
        private void AddToArray(Vector3 vec, ref Vector3[] vecArray, ref int count){
            vecArray[count++] = vec;
        }
        private void AddToArray3(Vector3 vec1, Vector3 vec2, Vector3 vec3, ref Vector3[] vecArray, ref int count){
            vecArray[count++] = vec1;
            vecArray[count++] = vec2;
            vecArray[count++] = vec3;
        }
        private void AddToArray(Vector4 vec, ref Vector4[] vecArray, ref int count){
            vecArray[count++] = vec;
        }
        private void AddToArray3(Vector4 vec1, Vector4 vec2, Vector4 vec3, ref Vector4[] vecArray, ref int count){
            vecArray[count++] = vec1;
            vecArray[count++] = vec2;
            vecArray[count++] = vec3;
        }
        // since resizearrays generates garbage, can you instead predict the size of the array you'll need and then
        // create a new array in that size?
        public void ResizeArrays (int size)//creates a lot of garbage!!
        {
            ResizeArray(ref _vertices,size);
            ResizeArray(ref _normals,size);
            ResizeArray(ref _uvs,size);
            ResizeArray(ref _uv2,size);
            ResizeArray(ref _tangents,size);
        }

        public void InitArrays (int size)
        {
            _vertices  = new Vector3[size];//start with arbitrary number element?
            _normals   = new Vector3[size];//new List<Vector3>();
            _uvs       = new Vector2[size];//new List<Vector2>();
            _uv2       = new Vector2[size];//new List<Vector2>();
            _tangents  = new Vector4[size];//new List<Vector4>();
        }

        private void ResizeArray(ref Vector2[] vecArray, int newSize){
            System.Array.Resize(ref vecArray, newSize);    //resize array
        }
        private void ResizeArray(ref Vector3[] vecArray, int newSize){
            System.Array.Resize(ref vecArray, newSize);    //resize array
        }
        private void ResizeArray(ref Vector4[] vecArray, int newSize){
            System.Array.Resize(ref vecArray, newSize);    //resize array
        }
		public void AddTriangle(
			Vector3[] vertices,
			Vector3[] normals,
			Vector2[] uvs,
			int       submesh){

			int vertCount = numVertices;//_vertices.Length;//_vertices.Count;
            if (vertCount >= _vertices.Length - 10)
            {
                ResizeArrays(_vertices.Length+1024);
            }

			//AddToArray(vertices[0],ref _vertices, ref numVertices);//_vertices.Add(vertices[0]);
			//AddToArray(vertices[1],ref _vertices, ref numVertices);//_vertices.Add(vertices[1]);
			//AddToArray(vertices[2],ref _vertices, ref numVertices);//_vertices.Add(vertices[2]);
            //AddToArray3(vertices[0], vertices[1], vertices[2] ,ref _vertices, ref numVertices);//_vertices.Add(vertices[0]);
            _vertices[numVertices++] = vertices[0];
            _vertices[numVertices++] = vertices[1];
            _vertices[numVertices++] = vertices[2];

			//AddToArray(normals[0],ref _normals, ref numNormals);//_normals.Add(normals[0]);
			//AddToArray(normals[1],ref _normals, ref numNormals);//_normals.Add(normals[1]);
            //AddToArray(normals[2],ref _normals, ref numNormals);//_normals.Add(normals[2]);
            //AddToArray3(normals[0],normals[1],normals[2],ref _normals, ref numNormals);//_normals.Add(normals[2]);
            _normals[numNormals++] = normals[0];
            _normals[numNormals++] = normals[1];
            _normals[numNormals++] = normals[2];

			//AddToArray(uvs[0],ref _uvs, ref numUVs);//_uvs.Add(uvs[0]);
			//AddToArray(uvs[1],ref _uvs, ref numUVs);//_uvs.Add(uvs[1]);
            //AddToArray(uvs[2],ref _uvs, ref numUVs);//_uvs.Add(uvs[2]);
            //AddToArray3(uvs[0],uvs[1],uvs[2],ref _uvs, ref numUVs);//_uvs.Add(uvs[2]);
            _uvs[numUVs++] = uvs[0];
            _uvs[numUVs++] = uvs[1];
            _uvs[numUVs++] = uvs[2];

			if(_subIndices.Count < submesh+1){
				for(int i=_subIndices.Count; i<submesh+1; i++){
					_subIndices.Add(new List<int>());
				}
			}

			_subIndices[submesh].Add(vertCount);
			_subIndices[submesh].Add(vertCount+1);
			_subIndices[submesh].Add(vertCount+2);
		}

		public void AddTriangle(
			Vector3[] vertices,
			Vector3[] normals,
			Vector2[] uvs,
			Vector4[] tangents,
			int       submesh){

            int vertCount = numVertices;//_vertices.Length;//_vertices.Count;
            if (vertCount >= _vertices.Length - 10)
            {
                ResizeArrays(_vertices.Length+1024);
            }

            //AddToArray(vertices[0],ref _vertices, ref numVertices);//_vertices.Add(vertices[0]);
            //AddToArray(vertices[1],ref _vertices, ref numVertices);//_vertices.Add(vertices[1]);
            //AddToArray(vertices[2],ref _vertices, ref numVertices);//_vertices.Add(vertices[2]);
            //AddToArray3(vertices[0],vertices[1],vertices[2],ref _vertices, ref numVertices);//_vertices.Add(vertices[2]);
            _vertices[numVertices++] = vertices[0];
            _vertices[numVertices++] = vertices[1];
            _vertices[numVertices++] = vertices[2];
            //AddToArray(normals[0],ref _normals, ref numNormals);//_normals.Add(normals[0]);
            //AddToArray(normals[1],ref _normals, ref numNormals);//_normals.Add(normals[1]);
            //AddToArray(normals[2],ref _normals, ref numNormals);//_normals.Add(normals[2]);
            //AddToArray3(normals[0],normals[1],normals[2],ref _normals, ref numNormals);//_normals.Add(normals[2]);
            _normals[numNormals++] = normals[0];
            _normals[numNormals++] = normals[1];
            _normals[numNormals++] = normals[2];
            //AddToArray(uvs[0],ref _uvs, ref numUVs);//_uvs.Add(uvs[0]);
            //AddToArray(uvs[1],ref _uvs, ref numUVs);//_uvs.Add(uvs[1]);
            //AddToArray(uvs[2],ref _uvs, ref numUVs);//_uvs.Add(uvs[2]);
            //AddToArray3(uvs[0],uvs[1],uvs[2],ref _uvs, ref numUVs);//_uvs.Add(uvs[2]);
            _uvs[numUVs++] = uvs[0];
            _uvs[numUVs++] = uvs[1];
            _uvs[numUVs++] = uvs[2];
			//AddToArray(tangents[0],ref _tangents, ref numTangents);//_tangents.Add(tangents[0]);
			//AddToArray(tangents[1],ref _tangents, ref numTangents);//_tangents.Add(tangents[1]);
            //AddToArray(tangents[2],ref _tangents, ref numTangents);//_tangents.Add(tangents[2]);
            //AddToArray3(tangents[0],tangents[1],tangents[2],ref _tangents, ref numTangents);//_tangents.Add(tangents[2]);
            _tangents[numTangents++] = tangents[0];
            _tangents[numTangents++] = tangents[1];
            _tangents[numTangents++] = tangents[2];
			if(_subIndices.Count < submesh+1){
				for(int i=_subIndices.Count; i<submesh+1; i++){
					_subIndices.Add(new List<int>());
				}
			}

			_subIndices[submesh].Add(vertCount);
			_subIndices[submesh].Add(vertCount+1);
			_subIndices[submesh].Add(vertCount+2);

		}

        public void AddTriangle(
            Vector3[] vertices,
            Vector3[] normals,
            Vector2[] uvs,
            Vector2[] uv2,
            Vector4[] tangents,
            int       submesh){

            int vertCount = numVertices;//_vertices.Length;//_vertices.Count;
            if (vertCount >= _vertices.Length - 10)
            {
                ResizeArrays(_vertices.Length+1024);
            }

            //AddToArray(vertices[0],ref _vertices, ref numVertices);//_vertices.Add(vertices[0]);
            //AddToArray(vertices[1],ref _vertices, ref numVertices);//_vertices.Add(vertices[1]);
            //AddToArray(vertices[2],ref _vertices, ref numVertices);//_vertices.Add(vertices[2]);
            //AddToArray3(vertices[0],vertices[1],vertices[2],ref _vertices, ref numVertices);//_vertices.Add(vertices[2]);
            _vertices[numVertices++] = vertices[0];
            _vertices[numVertices++] = vertices[1];
            _vertices[numVertices++] = vertices[2];
            //AddToArray(normals[0],ref _normals, ref numNormals);//_normals.Add(normals[0]);
            //AddToArray(normals[1],ref _normals, ref numNormals);//_normals.Add(normals[1]);
            //AddToArray(normals[2],ref _normals, ref numNormals);//_normals.Add(normals[2]);
            //AddToArray3(normals[0],normals[1],normals[2],ref _normals, ref numNormals);//_normals.Add(normals[2]);
            _normals[numNormals++] = normals[0];
            _normals[numNormals++] = normals[1];
            _normals[numNormals++] = normals[2];
            //AddToArray(uvs[0],ref _uvs, ref numUVs);//_uvs.Add(uvs[0]);
            //AddToArray(uvs[1],ref _uvs, ref numUVs);//_uvs.Add(uvs[1]);
            //AddToArray(uvs[2],ref _uvs, ref numUVs);//_uvs.Add(uvs[2]);
            //AddToArray3(uvs[0],uvs[1],uvs[2],ref _uvs, ref numUVs);//_uvs.Add(uvs[2]);
            _uvs[numUVs++] = uvs[0];
            _uvs[numUVs++] = uvs[1];
            _uvs[numUVs++] = uvs[2];
            //AddToArray(uv2[0],ref _uv2, ref numUV2);//_uvs.Add(uvs[0]);
            //AddToArray(uv2[1],ref _uv2, ref numUV2);//_uvs.Add(uvs[1]);
            //AddToArray(uv2[2],ref _uv2, ref numUV2);//_uvs.Add(uvs[2]);          
            //AddToArray3(uv2[0],uv2[1],uv2[2],ref _uv2, ref numUV2);//_uvs.Add(uvs[2]);
            _uv2[numUV2++] = uv2[0];
            _uv2[numUV2++] = uv2[1];
            _uv2[numUV2++] = uv2[2];
            //AddToArray(tangents[0],ref _tangents, ref numTangents);//_tangents.Add(tangents[0]);
            //AddToArray(tangents[1],ref _tangents, ref numTangents);//_tangents.Add(tangents[1]);
            //AddToArray(tangents[2],ref _tangents, ref numTangents);//_tangents.Add(tangents[2]);
            //AddToArray3(tangents[0],tangents[1],tangents[2],ref _tangents, ref numTangents);//_tangents.Add(tangents[2]);
            _tangents[numTangents++] = tangents[0];
            _tangents[numTangents++] = tangents[1];
            _tangents[numTangents++] = tangents[2];
            if(_subIndices.Count < submesh+1){
                for(int i=_subIndices.Count; i<submesh+1; i++){
                    _subIndices.Add(new List<int>());
                }
            }

            _subIndices[submesh].Add(vertCount);
            _subIndices[submesh].Add(vertCount+1);
            _subIndices[submesh].Add(vertCount+2);

        }
		/// <summary>
		/// Creates and returns a new mesh
		/// </summary>
		public Mesh GetMesh(){
			
			Mesh shape = new Mesh();
			shape.name =  "Generated Mesh";
            ResizeArray(ref _vertices,numVertices);
            ResizeArray(ref _normals,numNormals);
            ResizeArray(ref _uvs,numUVs);
            ResizeArray(ref _uv2,numUV2);
			shape.SetVertices(new List<Vector3>(_vertices));
			shape.SetNormals(new List<Vector3>(_normals));
			shape.SetUVs(0, new List<Vector2>(_uvs));
			shape.SetUVs(1, new List<Vector2>(_uv2));//this was _uvs

			if(numTangents > 1)//this is weird. I guess there was a time that tangents were optional
			{
                ResizeArray(ref _tangents,numTangents);
                shape.SetTangents(new List<Vector4>(_tangents));
            }

			shape.subMeshCount = _subIndices.Count;

			for(int i=0; i<_subIndices.Count; i++)
				shape.SetTriangles(_subIndices[i], i);

			return shape;
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Creates and returns a new mesh with generated lightmap uvs (Editor Only)
		/// </summary>
		public Mesh GetMesh_GenerateSecondaryUVSet(){

			Mesh shape = GetMesh();
		
			// for light mapping
			UnityEditor.Unwrapping.GenerateSecondaryUVSet(shape);

			return shape;
		}

		/// <summary>
		/// Creates and returns a new mesh with generated lightmap uvs (Editor Only)
		/// </summary>
		public Mesh GetMesh_GenerateSecondaryUVSet( UnityEditor.UnwrapParam param){

			Mesh shape = GetMesh();

			// for light mapping
			UnityEditor.Unwrapping.GenerateSecondaryUVSet(shape, param);

			return shape;
		}

		#endif
	}
}
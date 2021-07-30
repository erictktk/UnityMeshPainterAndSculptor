using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITYEDITOR
using UnityEditor
#endif

namespace MeshSculptorSpace {
    public class Mesh : IEnumerable<Mesh.Vertex> {

        public UnityEngine.Mesh unityMesh;
        public Vertex[] vertices;
        public Face[] faces;
        //public Edges[] edges;

        public Vector3[] worldPositions { get; private set; }
        public Vector3[] worldNormals { get; private set; }
        public Vector3[] GetWorldPositionsCopy() {
            return worldPositions.ToArray();
        }
        public Vector3[] GetWorldNormalsCopy() {
            return worldNormals.ToArray();
        }

        public Mesh() {

        }

        /*
        public Mesh(UnityEngine.Mesh mesh, Transform transform = null) {
            vertices = new Vertex[mesh.vertices.Length];
            Vertex[] empty = new Vertex[4];
            int[] emptyFaces = new int[4];
            for (int i = 0; i < mesh.vertices.Length; i += 1) {
                vertices[i] = new Vertex(i, emptyFaces, empty, mesh.vertices[i], mesh.normals[i]);
            }
            if (transform != null) {
                Transform(transform);
            }
        }*/

        public void UpdateTransform(Transform t) {
            if (worldPositions == null) {
                worldPositions = new Vector3[vertices.Length];
                worldNormals = new Vector3[vertices.Length];
            }
            
            int i = 0;
            foreach (Vertex v in vertices) {
                v.transformedPosition = t.TransformPoint(v.position);
                v.transformedNormal = t.TransformVector(v.normal);

                worldPositions[i] = v.transformedPosition;
                worldNormals[i] = v.transformedNormal;

                i += 1;
            }
        }


        public Mesh(UnityEngine.Mesh mesh, Transform transform) {
            vertices = new Vertex[mesh.vertices.Length];
            faces = new Face[mesh.triangles.Length/3];

            //Debug.Log(mesh.triangles.Length);
            //Debug.Log(mesh.triangles.Length / 3);

            HashSet<int>[] vertexToFaces = Enumerable.Range(0, mesh.vertices.Length).Select((i) => new HashSet<int>()).ToArray();
            HashSet<int>[] faceToVertices = Enumerable.Range(0, faces.Length).Select((i) => new HashSet<int>()).ToArray();

            for (int i = 0; i < faces.Length; i += 1) {
                int verta = mesh.triangles[i * 3];
                int vertb = mesh.triangles[i * 3 + 1];
                int vertc = mesh.triangles[i * 3 + 2];

                vertexToFaces[verta].Add(i);
                vertexToFaces[vertb].Add(i);
                vertexToFaces[vertc].Add(i);

                faceToVertices[i].Add(verta);
                faceToVertices[i].Add(vertb);
                faceToVertices[i].Add(vertc);
            }

            HashSet<int>[] vertexMap = Enumerable.Range(0, mesh.vertices.Length).Select((i) => new HashSet<int>()).ToArray();
            for (int i = 0; i < vertexToFaces.Length; i += 1) {
                foreach (int face in vertexToFaces[i]) {
                    foreach (int vertex in faceToVertices[face]) {
                        if (i != vertex) {
                            vertexMap[i].Add(vertex);
                        }
                    }
                }
                vertices[i] = new Vertex(i, vertexToFaces[i].ToArray(), vertexMap[i].ToArray(), mesh.vertices[i], mesh.normals[i], transform);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /*
        public IEnumerator<Mesh.Face> GetEnumerator() {
            for (int i = 0; i < faces.Length; i += 1) {
                yield return faces[i];
            }
        }*/

        public IEnumerator<Mesh.Vertex> GetEnumerator() {
            for (int i = 0; i < vertices.Length; i += 1) {
                yield return vertices[i];
            }
        }

        public void Transform(Transform transform) {
            for (int i = 0; i < vertices.Length; i += 1) {
                vertices[i].transformedPosition = transform.TransformPoint(vertices[i].position);
                vertices[i].transformedNormal = transform.TransformVector(vertices[i].normal);
            }
        }

        public class Vertex {
            public int number { get; private set; }
            public int [] faceNums { get; private set; }
            public int[] neighbors { get; private set; }
            public Vector3 position { get; set; }
            public Vector3 normal { get; set; }

            public Vector3 transformedPosition { get; set; }
            public Vector3 transformedNormal { get; set; }

            public Vertex(int number, int [] faceNums, int [] neighbors, Vector3 position, Vector3 normal, Transform transform = null) {
                this.number = number;
                this.faceNums = faceNums;
                this.neighbors = neighbors;
                this.position = position;
                this.normal = normal;

                if (transform != null) {
                    transformedPosition = transform.TransformPoint(position);
                    transformedNormal = transform.TransformVector(normal);
                }
            }
        }

        public class Edge {

        }

        public class Face {
            public int number { get; private set; }
            public Vertex[] vertices { get; private set; }

            public Vector3 normal { get; private set; }


            public Face(){


            }
            public void CalculateNormal() {
                Vector3 edge1 = vertices[1].position - vertices[0].position;
                Vector3 edge2 = vertices[2].position - vertices[1].position;

                normal = Vector3.Cross(edge1, edge2).normalized;
            }

        }
        
    }
}

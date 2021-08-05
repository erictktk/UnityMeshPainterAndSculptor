using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITYEDITOR
using UnityEditor;
#endif 


[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class MeshSculptor : MonoBehaviour {

    public enum BrushMode { Sculpt, Smooth, VertexPaint };
    public BrushMode brushMode;

    #region defaultParameters
    public float radius = 1f;
    public AnimationCurve falloffCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [HideInInspector]
    public bool showWireframe = false;
    [SerializeField]
    [HideInInspector]
    public bool showVertexColor = false;
    #endregion

    #region Color Parameters
    [HideInInspector]
    public Color color = Color.white;
    [HideInInspector]
    [Range(0, 100)]
    public float opacity = 100f;
    #endregion

    #region Sculpt and Smooth Parameters
    [HideInInspector]
    public float strength = 1f;
    [HideInInspector]
    public float angleTolerance = 40f;
    #endregion

    #region help parameters
    [HideInInspector]
    public bool showTests = false;

    Material[] originalMaterials;
    Material unlitVertexColor;
    Material wireframe;

    public UnityEngine.Mesh paintedMesh { get; private set; }

    [SerializeField]
    [HideInInspector]
    protected Mesh originalMeshCopy;

    
    [HideInInspector]
    public MeshSculptorSpace.Mesh mesh;
    [SerializeField]
    [HideInInspector]
    new MeshRenderer renderer;
    #endregion

    #region test functions
    public void TestVertexColorShader() {
        Color32[] newColors = new Color32[paintedMesh.vertices.Length];
        Vector3 normal;
        for (int i = 0; i < paintedMesh.vertices.Length; i += 1) {
            normal = paintedMesh.normals[i];
            float x = System.Math.Abs(normal.x);
            float y = System.Math.Abs(normal.y);
            float z = System.Math.Abs(normal.z);
            if (x > y && x > z) {
                newColors[i] = Color.red;
            }
            else if (y > x && y > z) {
                newColors[i] = Color.blue;
            }
            else {
                newColors[i] = Color.green;
            }
        }
        paintedMesh.colors32 = newColors;
    }
    #endregion


    [HideInInspector]
    public Color[] vertexColors = null;

    [HideInInspector]
    public Material originalMaterial = null;

    public void ResetVertexColors() {
        Color32[] newColors = new Color32[paintedMesh.vertices.Length];
        for (int i = 0; i < paintedMesh.vertices.Length; i += 1) {
            newColors[i] = Color.black;
        }
        paintedMesh.colors32 = newColors;
    }

    public void FillColor(Color color) {
        Color32[] newColors = new Color32[paintedMesh.vertices.Length];
        for (int i = 0; i < paintedMesh.vertices.Length; i += 1) {
            newColors[i] = color;
        }
        paintedMesh.colors32 = newColors;
    }

    public void UpdateRendererComponent() {
        if (originalMaterials == null) {
            originalMaterials = renderer.materials;
        }

        List<Material> newMaterials = new List<Material>();

        if (showWireframe) {
            newMaterials.Add(wireframe);
        }
        if (showVertexColor) {
            newMaterials.Add(unlitVertexColor);
        }
        else {
            //newMaterials.AddRange(originalMaterials);
            newMaterials.Add(originalMaterial);
        }

        renderer.materials = newMaterials.ToArray();
    }

    public MeshSculptorSpace.Mesh GetMeshSculpterMesh() {
        return mesh;
    }

    #region history
    protected Stack<Vector3[]> vertsHistory = new Stack<Vector3[]>();
    #endregion

    // Use this for initialization
    void Start() {
        StoreInitialMesh();
        GetMesh();
        falloffCurve = AnimationCurve.Linear(0, 0, 1, 1);
        renderer = GetComponent<MeshRenderer>();
        originalMaterials = new Material[renderer.materials.Length];
        renderer.materials.CopyTo(originalMaterials, 0);
        InitMaterials();
        //InitVertexColors();
    }

    // Update is called once per frame

    
    void Update() {
        if (transform.hasChanged) {
            mesh.UpdateTransform(transform);
        }
    }

    public static string GetRelativePath(string fullPath, string basePath) {
        // Require trailing backslash for path
        if (!basePath.EndsWith("\\"))
            basePath += "\\";

        System.Uri baseUri = new System.Uri(basePath);
        System.Uri fullUri = new System.Uri(fullPath);

        System.Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

        return relativeUri.ToString().Replace("/", "\\");
    }

    public string GetDirectory() {
        string dataPath = Application.dataPath;
        string[] files = System.IO.Directory.GetDirectories(dataPath, "MeshSculptor", System.IO.SearchOption.AllDirectories);
        string[] ends = new string[files.Length];

        for (int i = 0; i < files.Length; i += 1) {
            files[i] = files[i].Replace('\\', '/');
            ends[i] = GetRelativePath(files[i], Application.dataPath);
        }

        if (ends.Length == 0) {
            throw new System.Exception("No folder MeshSculptor found.  Please rename folder containing this script to 'MeshSculptor'");
        }
        else if (ends.Contains("MeshSculptor")) {
            Debug.Log(ends[0]);
            return files[System.Array.FindIndex(ends, w => w == "MeshSculptor")];
        }
        else {
            throw new System.Exception("No folder MeshSculptor found.  Please rename folder containing this script to 'MeshSculptor'");
        }
    }

    void InitMaterials() {
        string dataPath = Application.dataPath;
        string directory = GetDirectory();

        if (this.originalMaterial == null) { 
        this.originalMaterial = GetComponent<MeshRenderer>().material;
        }

        string[] files = System.IO.Directory.GetFiles(directory, "UnlitVertexColor.mat");
        if (files.Length == 0) {
            throw new System.Exception("No material named UnlitVertexColor found, make sure your MeshSculptor directory includes the UnlitVertexColor material");
        }
        else {
            string unityDirectory = "Assets/" + GetRelativePath(directory, dataPath);
            unlitVertexColor = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(unityDirectory + "/UnlitVertexColor.mat");
            Debug.Log(unityDirectory);
        }
    }

    void StoreInitialMesh() {
        if (originalMeshCopy == null) {
            originalMeshCopy = transform.GetComponent<MeshFilter>().sharedMesh;
            Debug.Log("originalMeshCopy == null");
            mesh = new MeshSculptorSpace.Mesh(originalMeshCopy, transform);
        }
        if (mesh == null) {
            Debug.Log("mesh == null!");
            mesh = new MeshSculptorSpace.Mesh(originalMeshCopy, transform);
        }
    }

    public void ResetMesh() {
        paintedMesh = Instantiate(originalMeshCopy);
        GetComponent<MeshFilter>().sharedMesh = paintedMesh;
        GetComponent<MeshCollider>().sharedMesh = paintedMesh;
    }

    void GetMesh() {
        if (paintedMesh == null) {
            if (transform.GetComponent<MeshCollider>() != null) {
                paintedMesh = transform.GetComponent<MeshCollider>().sharedMesh;
            }
            else {
                Debug.Log("no mesh found!");
            }

            if (paintedMesh != null) {
                originalMeshCopy = (Mesh)Instantiate(paintedMesh);
            }
        }
    }

    Vector3 GetAverageNeighbor(int vertex, Vector3[] curPositions) {
        int[] neighbors = mesh.vertices[vertex].neighbors;
        int neighborCount = neighbors.Length;

        Vector3 avgNeighbor = Vector3.zero;
        for (int j = 0; j < neighborCount; j += 1) {
            avgNeighbor += paintedMesh.vertices[neighbors[j]];
            //avgNeighbor += curPositions[neighbors[j]];
        }

        avgNeighbor *= (1f / (float)neighborCount);

        return avgNeighbor;
    }

    public void TaubinSmoothing(Vector3 p, Vector3 n, bool doFalloff = false) {
        StoreInitialMesh();
        GetMesh();

        Vector3[] vertices = paintedMesh.vertices;
        Vector3[] normals = paintedMesh.normals;
        Vector3[] intermediateVertices = new Vector3[vertices.Length];
        Vector3[] oldVertices = paintedMesh.vertices;
        Vector3[] newVertices = paintedMesh.vertices;

        float strengthCoeff = strength / 10f;

        List<List<Vector3>> closePoints = new List<List<Vector3>>();

        List<Vector3> curList = new List<Vector3>();
        List<int> verticesToDo = new List<int>();
        List<float> strengths = new List<float>();

        for (int i = 0; i < vertices.Length; i += 1) {
            if ((transform.TransformPoint(vertices[i]) - p).magnitude < radius) {
                verticesToDo.Add(i);
                strengths.Add(1f);
            }
        }
        //smooth
        Vector3 pos, neighbor, neighborDelta;
        for (int i = 0; i < verticesToDo.Count; i += 1) {
            int curVertex = verticesToDo[i];
            pos = vertices[curVertex];
            neighbor = GetAverageNeighbor(curVertex, vertices);

            neighborDelta = neighbor - pos;

            intermediateVertices[curVertex] = vertices[curVertex] + neighborDelta * strengthCoeff * strengths[i];
        }
        //inflate
        for (int i = 0; i < verticesToDo.Count; i += 1) {
            int curVertex = verticesToDo[i];
            pos = intermediateVertices[curVertex];
            neighbor = GetAverageNeighbor(curVertex, intermediateVertices);

            neighborDelta = neighbor - pos;

            newVertices[curVertex] = intermediateVertices[curVertex] + -neighborDelta * strengthCoeff * strengths[i];
        }

        //implement history later//
        vertsHistory.Push(oldVertices);

        paintedMesh.vertices = newVertices;
        paintedMesh.RecalculateNormals();
        paintedMesh.RecalculateTangents();
        transform.GetComponent<MeshFilter>().sharedMesh = paintedMesh;
        transform.GetComponent<MeshCollider>().sharedMesh = paintedMesh;

    }

    public void PaintMesh(Vector3 p, Vector3 n) {
        GetMesh();

        Color32[] colors = paintedMesh.colors32;
        //Color32[] newColors = new Color32[colors.Length];
        Vector3[] vertices = paintedMesh.vertices;

        float strengthCoeff = Mathf.Clamp01((opacity) / 100f) * .3f;
        float complement = 1f - strengthCoeff;

        List<List<Vector3>> closePoints = new List<List<Vector3>>();

        List<Vector3> curList = new List<Vector3>();
        List<int> verticesToDo = new List<int>();
        List<float> strengths = new List<float>();

        for (int i = 0; i < vertices.Length; i += 1) {
            if ((mesh.worldPositions[i] - p).magnitude < radius) {
                verticesToDo.Add(i);
                strengths.Add(1f);
            }
        }

        Color curColor, newColor;

        for (int i = 0; i < verticesToDo.Count; i += 1) {
            int curVertex = verticesToDo[i];
            curColor = colors[curVertex];

            newColor = curColor * complement + color * strengthCoeff;

            //newColor = color;

            colors[curVertex] = newColor;
        }
        paintedMesh.colors32 = colors;
    }

    //don't use, shrinks mesh
    public void SimpleSmooth(Vector3 p, Vector3 n) {
        StoreInitialMesh();
        GetMesh();

        Vector3[] vertices = paintedMesh.vertices;
        Vector3[] normals = paintedMesh.normals;
        Vector3[] oldVertices = paintedMesh.vertices;
        Vector3[] newVertices = paintedMesh.vertices;

        bool wasChanged = false;

        List<List<Vector3>> closePoints = new List<List<Vector3>>();

        float u; Vector3 curP;
        float curDist;
        List<Vector3> curList = new List<Vector3>();

        for (int i = 0; i < vertices.Length; i += 1) {
            int curCount = 0;
            Vector3 newP = new Vector3(0, 0, 0);

            curP = vertices[i];

            if ((curP - p).magnitude < radius) {
                for (int j = 0; j < vertices.Length; j += 1) {
                    if (i != j) {
                        curDist = (vertices[i] - vertices[j]).magnitude;

                        u = (Mathf.Max(radius - curDist, 0) / radius);

                        if (u < 1 && u > 0) {
                            curCount += 1;
                            newP += vertices[j];
                        }
                    }
                }
                if (curCount > 0) {
                    wasChanged = true;
                    newP /= (float)curCount;
                    newVertices[i] = vertices[i] * .99f + newP * .01f + normals[i] * .0001f;
                }
                else {
                    newVertices[i] = vertices[i];
                }
            }

        }

        if (wasChanged) {
            vertsHistory.Push(oldVertices);

            paintedMesh.vertices = newVertices;
            paintedMesh.RecalculateNormals();
            paintedMesh.RecalculateTangents();
            transform.GetComponent<MeshFilter>().sharedMesh = paintedMesh;
            transform.GetComponent<MeshCollider>().sharedMesh = paintedMesh;
        }

    }

    public void Displace(Vector3 p, Vector3 n, float direction) {
        GetMesh();

        Vector3[] vertices = paintedMesh.vertices;
        Vector3[] normals = paintedMesh.normals;
        Vector3[] oldVertices = paintedMesh.vertices;

        bool wasChanged = false;

        Vector3 curP; Vector3 curN; float curDist; float u; float moveAmount; float curAngle;
        float curCount = 0;
        Vector3 avgN = new Vector3(0, 0, 0);

        List<int> verticesToDo = new List<int>();
        List<float> strengths = new List<float>();

        float dot;
        for (int i = 0; i < vertices.Length; i += 1) {
            //normal tests to see if brush would hit surface
            //dot hit normal and world normal of mesh should at least be greater than 0!
            if (Vector3.Dot(mesh.worldNormals[i], n) < .2f) {
                continue;
            }

            u = (mesh.worldPositions[i] - p).magnitude / radius;
            if (u < 1) {
                verticesToDo.Add(i);
                strengths.Add( falloffCurve.Evaluate(1-u) );
                avgN += paintedMesh.normals[i];
            }
        }

        if (curCount != 0) {
            avgN /= curCount;
        }

        for (int i = 0; i < verticesToDo.Count; i += 1) {
            int vertexNum = verticesToDo[i];
            curP = mesh.worldPositions[vertexNum];
            curN = mesh.worldNormals[vertexNum];

            vertices[vertexNum] += avgN * strengths[i] * strength / 2000f * direction;
            wasChanged = true;

            /*
            curAngle = Vector3.Angle(curN, n);


            
            if (curAngle < angleTolerance) {
                vertices[vertexNum] += avgN * strengths[i] * strength / 2000f * direction;
                wasChanged = true;
            }
            */

            /*
            //tests if u > .01 for error
            if (moveAmount > 0 && u > .01 && curAngle < angleTolerance) {
                curP = curP + avgN * strength / 1000f * direction;
                //set for undo operation
                wasChanged = true;
                vertices[i] = curP;
            }*/
        }

        if (wasChanged) {
            vertsHistory.Push(oldVertices);

            paintedMesh.vertices = vertices;
            paintedMesh.RecalculateNormals();
            paintedMesh.RecalculateTangents();
            transform.GetComponent<MeshFilter>().sharedMesh = paintedMesh;
            transform.GetComponent<MeshCollider>().sharedMesh = paintedMesh;
        }
    }
}


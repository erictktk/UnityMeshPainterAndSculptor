using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshSculpterSpace {
    [CustomEditor(typeof(MeshSculptor))]
    public class MeshSculpterEditor : Editor {

        MeshSculptor thing;
        private bool isMouseDown = false;
        private Vector3 cForward;

        public override void OnInspectorGUI() {
            thing = target as MeshSculptor;

            thing.showTests =EditorGUILayout.Toggle("Show Tests", thing.showTests);

            if (thing.showTests) {
                if (GUILayout.Button("Test Vertex Color Shader")) {
                    thing.TestVertexColorShader();
                }
                if (GUILayout.Button("Try To Get MeshSculptor Directory")) {
                    thing.GetDirectory();
                }
                EditorGUILayout.Space(); EditorGUILayout.Space();
            }

            if (GUILayout.Button("Reset Vertex Colors")) {
                thing.ResetVertexColors();
            }
            if (GUILayout.Button("Reset Mesh")) {
                ResetMeshChild();
            }
            DrawDefaultInspector();

            EditorGUILayout.Space(); EditorGUILayout.Space();

            MeshSculptor.BrushMode brushMode = thing.brushMode;
            switch (brushMode) {
                case MeshSculptor.BrushMode.VertexPaint:
                    EditorGUILayout.LabelField("Vertex Paint Mode", EditorStyles.boldLabel);
                    if (GUILayout.Button("Fill Color")) {
                        thing.FillColor(thing.color);
                    }
                    thing.color = EditorGUILayout.ColorField("Color", thing.color);
                    thing.opacity = EditorGUILayout.Slider("Opacity", thing.opacity, 0, 100f);
                    break;
                case MeshSculptor.BrushMode.Sculpt:
                    EditorGUILayout.LabelField("Sculpt Mode", EditorStyles.boldLabel);
                    thing.strength = EditorGUILayout.FloatField("Strength", thing.strength);
                    break;
                case MeshSculptor.BrushMode.Smooth:
                    EditorGUILayout.LabelField("Smooth Mode", EditorStyles.boldLabel);
                    thing.strength = EditorGUILayout.FloatField("Strength", thing.strength);
                    break;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Visualizations", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            thing.showVertexColor = EditorGUILayout.Toggle("Show Vertex Color", thing.showVertexColor);
            thing.showWireframe = EditorGUILayout.Toggle("Show Wireframe", thing.showWireframe);
            if (EditorGUI.EndChangeCheck()) {
                thing.UpdateRendererComponent();
            }
        }

        void ResetMeshChild() {
            thing = target as MeshSculptor;
            thing.ResetMesh();
        }

        void TestIfHitAnything() {
            Event e = Event.current;

            thing = target as MeshSculptor;
            Color color = Color.red;
            switch (thing.brushMode) {
                case MeshSculptor.BrushMode.Sculpt:
                    color = new Color(0, .4f, 1);
                    break;
                case MeshSculptor.BrushMode.Smooth:
                    color = new Color(.7f, .7f, .7f);
                    break;
                case MeshSculptor.BrushMode.VertexPaint:
                    color = new Color(0, 1, .3f);
                    break;
            }
            Handles.color = color;

            if (e.type == EventType.MouseMove) {
                SceneView.RepaintAll();
            }
            if (e.type == EventType.MouseDrag) {
                SceneView.RepaintAll();
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            //this doesn't work on my laptop for some reason, use above instead
            /*
            Vector2 mousePos = e.mousePosition;
            Camera c = Camera.current;
            mousePos.y = c.pixelHeight - mousePos.y;
            Vector3 p1 = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            Vector3 p2 = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
            Vector3 dir = p2 - p1;
            */

            //ray = new Ray(p1, dir);



            if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
                Vector3 p = hit.point;
                Vector3 n = hit.normal;

                float val = thing.radius < .3f ? .02f : .05f;

                //Debug.Log(val);

                Handles.DrawSolidDisc(p, n, val);

                DrawCircle(p, n, thing.radius, color);
            }
        }

        private void DrawCircle(Vector3 pos, Vector3 n, float radius = 5f, Color? color = null) {
            if (color == null) {
                Handles.color = Color.red;
            }
            else {
                Handles.color = (Color)color;
            }
            List<Vector3> points = new List<Vector3>();

            int num = 60;

            for (int i = 0; i < num; i += 1) {
                float u = ((float)i) / ((float)num - 1) * Mathf.PI * 2;

                float x = Mathf.Cos(u) * radius;
                float y = Mathf.Sin(u) * radius;

                Vector3 curPoint = new Vector3(x, y, 0);
                Quaternion rot = Quaternion.LookRotation(n);
                curPoint = rot * curPoint;
                curPoint += pos;
                points.Add(curPoint);
            }

            for (int i = 0; i < num; i += 1) {
                Handles.DrawAAPolyLine(3f, points[(i + 1) % num], points[i]);
            }
        }

        void GUIButtons() {
            if (GUI.Button(new Rect(10, 0, 80, 20), "Sculpt")) {
                thing.brushMode = MeshSculptor.BrushMode.Sculpt;
            }
            if (GUI.Button(new Rect(90, 0, 80, 20), "Smooth")) {
                thing.brushMode = MeshSculptor.BrushMode.Smooth;
            }
            if (GUI.Button(new Rect(170, 0, 80, 20), "Paint")) {
                thing.brushMode = MeshSculptor.BrushMode.VertexPaint;
            }

            switch (thing.brushMode) {
                
            }
        }

        void OnSceneGUI() {
            var e = Event.current;
            
            if (e.type == EventType.MouseUp) {
                isMouseDown = false;
            }

            TestIfHitAnything();
            //GUI.Button(new Rect(180, 0, 80, 20), "Paint");

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            RaycastHit hit;
            Vector3 p; Vector3 n;

            Handles.BeginGUI();
            GUIButtons();
            /*
            Rect rect = new Rect(10, 10, 100, 50);
            //GUI.Box(rect, "Button");

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
                //Selection.activeGameObject = myScript.selected;
            }*/

            SceneView.RepaintAll();

            if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
                p = hit.point;
                n = hit.normal;

                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                if ((e.type == EventType.MouseDown && e.button == 0) || (e.type == EventType.MouseDrag && isMouseDown)) {
                    isMouseDown = true;

                    if (e.type == EventType.MouseMove) {
                        SceneView.RepaintAll();
                    }
                    SceneView.RepaintAll();
                    Undo.RecordObject(thing, "Undo mesh edit");

                    float direction = e.shift ? -1 : 1;

                    switch (thing.brushMode) {
                        case MeshSculptor.BrushMode.VertexPaint:
                            thing.PaintMesh(p, n);
                            break;
                        case MeshSculptor.BrushMode.Sculpt:
                            thing.Displace(p, n, direction);
                            break;
                        case MeshSculptor.BrushMode.Smooth:
                            thing.TaubinSmoothing(p, n);
                            break;
                    }
                }
            }
            Handles.EndGUI();

        }
    }
}

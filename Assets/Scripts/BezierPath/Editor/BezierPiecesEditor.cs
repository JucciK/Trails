using System.Linq;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(BezierPieces))]
public class BezierPiecesEditor : Editor
{
    public BezierPieces current;
    public int selectedPiece = 0;

    private int handle1 = -1, handle2 = -1;
    private void OnSceneGUI()
    {
        Draw();
        Input();
    }

    public void Draw()
    {
        bool isDragging = GUIUtility.hotControl != 0;
        bool isShifting = Event.current.shift;
        if (selectedPiece<current.paths.Count)
        {
            BezierPiecePath path = current.paths[selectedPiece];

            Handles.color = Color.red;
            if (path.beziers.Count > 0) 
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newOrigin = Handles.PositionHandle(path.beziers[0].start, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log("Moving origin");
                        Vector3 s = path.beziers[0].start;
                    for (int i = 0; i < path.beziers.Count; i++)
                    {
                        path.beziers[i].start += newOrigin - s;
                        path.beziers[i].C1 += newOrigin - s;
                        path.beziers[i].C2 += newOrigin - s;
                        path.beziers[i].end += newOrigin - s;
                    }
                }

            }

            for (int i=0; i < path.beziers.Count; i++)
            {
                Handles.color = Color.yellow;
                Bezier b = path.beziers[i];
                Handles.DrawBezier(b.start, b.end, b.C1, b.C2, Color.yellow, null, 5f);
                    Vector3 start = b.start;
                if(i>0)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newStart = Handles.PositionHandle(start, Quaternion.identity);

                    if(EditorGUI.EndChangeCheck())
                    {
                        Debug.Log("Moved Start");
                        Undo.RecordObject(current, "Move endpoint");
                        b.C1 += (newStart - start);
                        b.start = newStart;

                        if (i > 0)
                        {

                            path.beziers[i - 1].C2 += (newStart - start);
                            path.beziers[i - 1].end = newStart;
                        }
                        else
                            handle2 = -1;
                    }

                }

                if(i==path.beziers.Count-1)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newEnd = Handles.PositionHandle(b.end, Quaternion.identity);
                    
                    if(EditorGUI.EndChangeCheck())
                    {
                        Debug.Log("Moved End");

                        Undo.RecordObject(current, "Move endpoint");
                        b.C2 += (newEnd - b.end);
                        b.end = newEnd;
                    }
                }



                Vector3 C1 = b.C1;
                float size = HandleUtility.GetHandleSize(C1) * 0.1f;
                EditorGUI.BeginChangeCheck();
                Vector3 newC1 = Handles.FreeMoveHandle(
                    C1,
                    size,
                    Vector3.zero,
                    Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(current, "Move handle");
                    if (isShifting)
                        newC1 = Floor((newC1-b.start) / 0.1f) * 0.1f+b.start;
                    b.C1 = newC1;
                    if (i > 0)
                    {
                        path.beziers[i - 1].C2 = start + (start - newC1);
                    }
                }

                Vector3 C2 = b.C2;
                EditorGUI.BeginChangeCheck();
                Vector3 newC2 = Handles.FreeMoveHandle(
                    C2,
                    size,
                    Vector3.zero,
                    Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(current, "Move handle");
                    if (isShifting)
                        newC2 = Floor((newC2-b.end) / 0.1f) * 0.1f+b.end;
                    b.C2 = newC2;
                    if (i < path.beziers.Count - 1)
                    {
                        path.beziers[i + 1].C1 = b.end + (b.end - newC2);
                    }
                    
                }

                Handles.color = Color.blue;
                Handles.DrawDottedLine(b.start, b.C1, 2f);
                Handles.DrawDottedLine(b.end, b.C2, 2f);

            }


        }
    }

    public static void MoveTo(Vector3 position, Quaternion rotation, float size = 10f)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        sceneView.pivot = position;   // look-at point
        sceneView.rotation = rotation;
        sceneView.size = size;        // zoom
        sceneView.Repaint();
    }

    public void Input()
    {

    }

    public Vector3 Floor(Vector3 i)
    {
        return new Vector3(Mathf.Floor(i.x), 0, Mathf.Floor(i.z));
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        int prevSelected = selectedPiece;

        selectedPiece = EditorGUILayout.IntSlider("Selected Path", selectedPiece, 0, current.paths.Count - 1);
        if(selectedPiece!=prevSelected)
        {
            SceneView.lastActiveSceneView.Repaint();
            MoveTo(current.paths[selectedPiece].Average(), Quaternion.FromToRotation(Vector3.forward,Vector3.down));
        }
        /*string empty = "";

        string newValue = GUILayout.TextField(selectedPiece.ToString());
        int newInt = selectedPiece;
        if (newValue != empty) {
            if (int.TryParse(newValue, out newInt))
            {
                if(newInt != selectedPiece)
                {
                    handle1 = current.paths[selectedPiece].beziers.Count - 1;
                    handle2 = -1;

                }
                selectedPiece = Mathf.Max(0, Mathf.Min(current.paths.Count - 1, newInt));

            }
        }
        else
        {
            selectedPiece = Mathf.Max(0, current.paths.Count - 1);
        }*/

        if (GUILayout.Button("New Bezier"))
        {
            if (current.paths[selectedPiece].beziers.Count > 0)
            {
                Bezier last = current.paths[selectedPiece].beziers.Last();
                current.paths[selectedPiece].beziers.Add(new Bezier(last.end, last.end + (last.end - last.C2), last.end + (last.end - last.C2) * 2, last.end + (last.end - last.C2) * 3));
            }
            else
            {
                Vector3 start = GetSceneViewCenterOnFloor(0);
                current.paths[selectedPiece].beziers.Add(new Bezier(start, start + Vector3.forward * 0.25f, start + Vector3.forward * 0.75f, start + Vector3.forward * 1f));
            }
        }

        if(GUILayout.Button("Copy Current"))
        {
            BezierPiecePath newpath = new BezierPiecePath();
            newpath.beziers = current.paths[selectedPiece].beziers.Select(b => new Bezier(b)).ToList();
            newpath.style = current.paths[selectedPiece].style;

            current.paths.Insert(selectedPiece + 1, newpath);
            selectedPiece += 1;
            SceneView.lastActiveSceneView.Repaint();

        }
        
        if(GUILayout.Button("Test length"))
        {
            current.generateTrajectory(selectedPiece);
        }
    }

    Vector3 GetSceneViewCenterOnFloor(float floorY = 0f)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
            return Vector3.zero;

        Camera cam = sv.camera;

        // Ray from center of the Scene view
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Floor plane (Y-up)
        Plane floor = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));

        if (floor.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private void OnEnable()
    {
        current = (BezierPieces)target;
        selectedPiece = Mathf.Max(0, current.paths.Count - 1);
        handle1 = -1;
        handle2 = -1;
    }
}

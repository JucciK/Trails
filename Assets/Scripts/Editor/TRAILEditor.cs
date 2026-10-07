using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Animations;

[CustomEditor(typeof(TRAILS))]
public class TRAILEditor : Editor
{
    private TRAILS trails;

    private void OnEnable()
    {
        trails = (TRAILS)target;
    }



    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        

        if(trails.showEdgeEditing )
        {
            EditorGUILayout.HelpBox("Click edge midpoints in the Scene view to enable or disable them", MessageType.Info);
        }
        if(GUILayout.Button("Clear"))
        {
            trails.mapGenerator.ClearMap();
            trails.rMap.Clear();
            trails.selectedEdges.Clear();
        }
        if (GUILayout.Button("Generate Environment"))
            trails.GenerateMap();
        if(GUILayout.Button("Select random edges"))
        {
            trails.selectEdges();
        }
        if (GUILayout.Button("Generate Paths"))
            trails.GeneratePath();
        if (GUILayout.Button("Generate Speed Profiles"))
            trails.GenerateSpeed();
        
    }

    private void OnSceneGUI()
    {
        if (!trails.showEdgeEditing)
        {
            return;
        }

        if(trails.rMap.edges.Count==0) 
        {
            return;
        }
        Camera camera = SceneView.currentDrawingSceneView?.camera;
        
        for(int i=0; i<trails.rMap.edges.Count;i++)
        {
            bool selected = trails.IsEdgeSeleceted(i);
            rEdge edge = trails.rMap.edges[i];
            if (edge.isWall || edge.isObstacle)
                continue;
            

            float size = HandleUtility.GetHandleSize(edge.middle) * 0.08f;
            Vector3 viewportPos = camera.WorldToViewportPoint(edge.middle);

            if (viewportPos.z <= 0)
                continue;

            if (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
                continue;

            Debug.DrawLine(edge.start, edge.end, selected ? Color.blue : Color.yellow);

            Handles.color = selected ? Color.green : Color.red;
            if (Handles.Button(edge.middle, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                Undo.RecordObject(trails, "Toggle Edge");

                trails.ToggleEdge(i);

                EditorUtility.SetDirty(trails);
            }
        }
    }

    
}

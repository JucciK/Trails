using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(ExtraBoundaries))]
public class ExtraBoundariesEditor : Editor
{
    public ExtraBoundaries extra;
    private void OnEnable()
    {
        extra = (ExtraBoundaries)target;
    }
    private void OnSceneGUI()
    {
        if(extra == null)
        {
            extra = (ExtraBoundaries)target;
            return;
        }
        Handles.color = Color.blue;
        Vector3[] points = new Vector3[extra.obstacleEdges.Count * 2];
        int i = 0;
        foreach (rEdge e in extra.obstacleEdges)
        {
            points[i] = e.start;
            points[i + 1] = e.end;
            i += 2;
        }

        extra.transform.parent.TransformPoints(points);
        for (i = 0; i < points.Length; i += 2)
        {
            Handles.DrawLine(points[i], points[i + 1]);
        }

        Vector3[] quadpoints = new Vector3[4];
        Vector4 quad = extra.quad;
        quadpoints[0] = new Vector3(quad.x, quad.y, 0);
        quadpoints[1] = new Vector3(quad.z, quad.y, 0);
        quadpoints[2] = new Vector3(quad.z, quad.w, 0);
        quadpoints[3] = new Vector3(quad.x, quad.w, 0);
        Handles.color = Color.black;

        extra.transform.parent.TransformPoints(quadpoints);
        Handles.DrawLine(quadpoints[0], quadpoints[1]);
        Handles.DrawLine(quadpoints[2], quadpoints[1]);
        Handles.DrawLine(quadpoints[2], quadpoints[3]);
        Handles.DrawLine(quadpoints[0], quadpoints[3]);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        extra.quad = EditorGUILayout.Vector4Field("quad", extra.quad);

        if(GUILayout.Button("Generate"))
        {
            extra.obstacleEdges.Clear();
            Vector4 quad = extra.quad;
            extra.obstacleEdges.Add(new rEdge(new Vector3(quad.x, quad.y, 0), new Vector3(quad.z, quad.y, 0)));
            extra.obstacleEdges.Add(new rEdge(new Vector3(quad.z, quad.y, 0), new Vector3(quad.z, quad.w, 0)));
            extra.obstacleEdges.Add(new rEdge(new Vector3(quad.z, quad.w, 0), new Vector3(quad.x, quad.w, 0)));
            extra.obstacleEdges.Add(new rEdge(new Vector3(quad.x, quad.w, 0), new Vector3(quad.x, quad.y, 0)));

        }
        if(GUILayout.Button("ToObstacles"))
        {
            foreach (rEdge edge in extra.obstacleEdges)
                edge.isObstacle = true;
        }
    }
}

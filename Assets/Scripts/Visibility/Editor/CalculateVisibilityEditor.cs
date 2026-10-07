using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(CalculateVisibility))]

public class CalculateVisibilityEditor : Editor
{
    private CalculateVisibility vis;
    private void OnSceneGUI()
    {
        Vector3 prev = vis.frustumFan[4];
        for(int i = 0; i<5; i++)
        {
            Handles.DrawLine(prev, vis.frustumFan[i]);
            prev = vis.frustumFan[i];
        }
    }
    private void OnEnable()
    {
        vis = (CalculateVisibility)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Calculate"))
            vis.calculateFrustum2d();
    }
}

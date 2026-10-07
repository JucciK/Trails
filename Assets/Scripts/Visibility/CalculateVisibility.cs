using System.Collections.Generic;
using UnityEngine;

public class CalculateVisibility : MonoBehaviour
{
    public Camera cam;
    float distance;

    public List<Vector3> frustumFan = new List<Vector3>();
    public Vector3[] frustumCorners
    {
        get
        {
            Vector3[] v = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, v);

            for(int i=0; i<4; i++)
            {
                v[i] = cam.transform.TransformPoint(v[i]);
            }
            return v;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        distance = cam.farClipPlane;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    Vector3 flatten(Vector3 v)
    {
        Debug.Log("Flattening");
        v.y = 0;
        return v;
    }
    public void calculateFrustum2d()
    {
        Vector3[] corners = frustumCorners;
        List<Vector3> v = new List<Vector3>();

        Debug.Log("Here");
        v.Add(flatten(cam.transform.position));
        Debug.Log("hh");
        for(int i=0; i<4; i++)
        {
            Debug.Log("flfl");
            v.Add(flatten(corners[i]));
        }
        Debug.Log("Here");
        Debug.Log(v.Count);
        frustumFan = GetConvexHullXZ(v);
    }



    public List<Vector3> GetConvexHullXZ(List<Vector3> points)
    {
        Debug.Log(points.Count);
        if (points.Count <= 3) return new List<Vector3>(points);
        Debug.Log("Here");
        points.Sort((a, b) =>
            a.x == b.x ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x)
        );

        List<Vector3> lower = new List<Vector3>();
        foreach (var p in points)
        {
            while (lower.Count >= 2 && CrossXZ(lower[^2], lower[^1], p) <= 0)
                lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }

        List<Vector3> upper = new List<Vector3>();
        for (int i = points.Count - 1; i >= 0; i--)
        {
            var p = points[i];
            while (upper.Count >= 2 && CrossXZ(upper[^2], upper[^1], p) <= 0)
                upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);

        return lower;
    }

    private float CrossXZ(Vector3 o, Vector3 a, Vector3 b)
    {
        float dx1 = a.x - o.x;
        float dz1 = a.z - o.z;
        float dx2 = b.x - o.x;
        float dz2 = b.z - o.z;
        return dx1 * dz2 - dz1 * dx2;
    }
}

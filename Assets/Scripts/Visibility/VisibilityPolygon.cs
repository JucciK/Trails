
using System.Collections.Generic;
using UnityEngine;

public class VisibilityPolygon : MonoBehaviour
{
    public rMap map;
    public float maxDistance = 100f;
    private const float EPS = 1e-12f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [EditorCools.Button]
    public void test()
    {
        getVisibility(transform.position);
    }
    public List<Vector3> getVisibility(Vector3 position)
    {
        List<rEdge> edges = new List<rEdge>();
        
        float maxSqr = 0;
        foreach (float angle in new float[] { 0, 45, 90, 135, 180, 225, 270 })
        {
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * maxDistance;

            float minD = float.MaxValue;
            foreach (var seg in map.edges)
            {
                if (!seg.isWall)
                    continue;
                if (RaySegmentIntersection(position, dir, seg.start, seg.end, out Vector3 inter))
                {
                    float d = (inter - position).sqrMagnitude;
                    if (d<minD)
                        minD = d;
                }
                
            }
            if (minD > maxSqr)
                maxSqr = minD;
        }
        float side = Mathf.Sqrt(maxSqr)*2;
        maxSqr = Mathf.Sqrt(side * side);
        maxSqr = maxSqr * maxSqr;
        Debug.Log($"Max distance: {maxSqr}");


        
        HashSet<Vector3> points = new HashSet<Vector3>();





        foreach (rEdge edge in map.edges)
        {
            if (!edge.isWall)
                continue;

            if ((edge.middle - position).sqrMagnitude > maxSqr)
                continue;

            edges.Add(edge);
            points.Add(edge.start);
            points.Add(edge.end);
        }

        var results = new List<(float angle, Vector3 point)>();

        foreach(var pt in points)
        {
            float baseAngle = Mathf.Atan2(pt.z-position.z, pt.x-position.x);

            foreach(float angle in new float[] {baseAngle- EPS, baseAngle, baseAngle+EPS})
            {
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle))*maxDistance;

                Vector3? closest = null;
                float minDist = float.MaxValue;
                Vector3 rayEnd = position + dir;
                foreach(var seg in edges)
                {
                    if(RaySegmentIntersection(position, dir, seg.start, seg.end, out Vector3 inter))
                    {
                        float dist = (inter - position).sqrMagnitude;
                        if(dist < minDist)
                        {
                            minDist = dist;
                            closest = inter;
                            Debug.DrawLine(seg.start, seg.end, Color.black, 15);
                        }
                    }
                }

                if (closest.HasValue)
                {
                    results.Add((angle, closest.Value));
                }
            }
        }

        results.Sort((a, b) => a.angle.CompareTo(b.angle));

        var polygon = new List<Vector3>();
        foreach (var (_, pt) in results)
        {
            if (polygon.Count == 0 || (polygon[polygon.Count - 1] - pt).sqrMagnitude > 1e-9f)
                polygon.Add(pt);
        }

        for(int i=1; i<polygon.Count; i++)
        {
            Debug.DrawLine(position-(polygon[i]-position), position-(polygon[i - 1] - position), Color.magenta, 10);
        }
        Debug.DrawLine(position - (polygon[0] - position), position - (polygon[polygon.Count - 1] - position), Color.magenta, 10);
        return polygon;
    }

    private static bool RaySegmentIntersection(Vector3 p, Vector3 dir, Vector3 a, Vector3 b, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        float x1 = a.x, z1 = a.z;
        float x2 = b.x, z2 = b.z;
        float px = p.x, pz = p.z;
        float dx = dir.x, dz = dir.z;

        float vx = x2 - x1, vz = z2 - z1;
        float denom = vx * dz - vz * dx;

        if (Mathf.Abs(denom) < 1e-9f)
            return false; // parallel or coincident

        float t2 = ((px - x1) * dz - (pz - z1) * dx) / denom;
        float t1 = ((x1 - px) * vz - (z1 - pz) * vx) / denom;

        if (t2 < 0 || t2 > 1 || t1 < 0)
            return false; // intersection not within segment or behind ray

        intersection = new Vector3(px + t1 * dx, 0, pz + t1 * dz);
        return true;
    }
}

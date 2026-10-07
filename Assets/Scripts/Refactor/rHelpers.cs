using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math.Optimization;
using System.Linq;
using Accord.Math.Geometry;
using System;
using UnityEditor.UI;
using System.Globalization;

public class rHelpers
{
    public static double[] initialGuess;
    public static int debug = 0;

    public static Vector3 CubicBezier(Vector3 P1, Vector3 C1, Vector3 C2, Vector3 P2, float t)
    {
        return Mathf.Pow(1 - t, 3) * P1 + 3 * Mathf.Pow(1 - t, 2) * t * C1 + 3 * (1 - t) * Mathf.Pow(t, 2) * C2 + Mathf.Pow(t, 3) * P2;
    }

    public static Vector3 CubicBezier(rMap.BezierPoint point)
    {
        return CubicBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
    }
    public static float SampleGaussian(float mean, float stdDev)
    {
        // Box-Muller transform to generate normal distributed random values
        float u1 = 1.0f - UnityEngine.Random.value; // Avoid 0
        float u2 = 1.0f - UnityEngine.Random.value;
        float standardNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return (float)(mean + stdDev * standardNormal);
    }

    public static TrajectoryResult fullBezier(Vector3[] points, Vector3[] C1, rPolygon[] quads, rMap map, bool dbg = false, int resolution = 20)
    {
        int curves = points.Length - 1;
        TrajectoryResult result = new TrajectoryResult(curves * resolution + 1);

        int outside = 0;
        int index = 0;
        Vector3 lp = points[0];
        Vector3 orientation = (points[1] - points[0]);
        Vector3 newOrientation = orientation;
        for (int i = 0; i < curves; i++)
        {
            if (dbg)
            {
                Debug.DrawLine(points[i], points[i] + C1[i], Color.blue, 30);
                Debug.DrawLine(points[i + 1], points[i + 1] - C1[i + 1], Color.green, 30);
            }
            //Debug.Log($"Bezier from: {points[i]} to {points[i + 1]} \t C1: {points[i] + C1[i]} \t C2 {points[i + 1] - C1[i + 1]}");
            for (int j = 0; j < resolution; j++)
            {
                float t = (float)j / resolution;
                result.path[index] = CubicBezier(points[i], points[i] + C1[i], points[i + 1] - C1[i + 1], points[i + 1], t);
                //Debug.DrawLine(points[i+1], points[i+1] - C1[i+1], Color.green,30);
                if (!quads[i].isInside(result.path[index]) && j > 1 && j < resolution - 1)
                {

                    if (debug == 0)
                        Debug.DrawRay(result.path[index], Vector3.up * 0.1f, Color.red, 1);
                    //Debug.Log($"Outside at: {result.path[index]} with index: {j} at curve: {i}");
                    //Debug.DrawLine(result.path[index], quads[i].middle, Color.red, 20);
                    //Debug.Log($"Outside at curve: {i} \t point: {j} \t position: {toReturn[index]} \t start: {points[i]} \t end: {points[i+1]} \t c1: {points[i]+C1[i]} \t c2: {points[i+1]-C1[i+1]}");
                    outside++;
                }
                if (debug == 0)
                    Debug.DrawLine(lp, result.path[index], Color.black, 1);
                newOrientation = result.path[index] - lp;
                /*result.distances[index] = getClosestPointToWall(map, result.path[index]);
                
                
                float len = newOrientation.magnitude;
                if(len>0)
                    result.yaws[index] = Vector3.SignedAngle(orientation, newOrientation, Vector3.up)/len;
                orientation = newOrientation;*/
                result.length += newOrientation.magnitude;
                lp = result.path[index];
                index++;
            }
        }
        result.outsidePoints = outside;
        result.path[curves * resolution] = points[points.Length - 1];
        debug = (debug + 1) % 1000;
        //return normalizePath(result, walls,0.1f);
        return result;//normalizePath(result,map,0.1f,dbg);
    }


    public static TrajectoryStats StatBezier(Vector3[] points, Vector3[] C1, rPolygon[] polys, rMap map, bool dbk = false, int resolution = 20)
    {
        int curves = points.Length - 1;

        int outside = 0;
        float maxYaws = 0;
        float minDistances = 0;
        float length = 0;
        float yawChanges = 0;

        Vector3 lastPoint = points[0];
        List<Vector3> ypoints = new List<Vector3>();
        ypoints.Add(lastPoint);
        List<float> curvatures = new List<float>();
        for (int i=0; i<curves; i++)
        {
            
            Vector3[] curvePoints = SingleBezier(points[i], points[i] + C1[i], points[i + 1] - C1[i + 1], points[i + 1],dbk,20);
            var (my, curvs, maxes) = bezierCurvatures(points[i], points[i] + C1[i], points[i + 1] - C1[i + 1], points[i + 1], map.maxYaw, dbk, 20);
            curvatures.AddRange(curvs);
            //yawChanges += my;
            maxYaws += maxes;
            if(dbk)
                Debug.DrawLine(points[i], points[i] + C1[i], Color.blue, 10);
            for(int j = 1; j<curvePoints.Length; j++)
            {
                
                ypoints.Add(curvePoints[j]);
                length += (curvePoints[j] - lastPoint).magnitude;
                if (!polys[i].isInside(curvePoints[j]) && j<resolution)
                {
                    outside++;
                    Debug.DrawLine(curvePoints[j], polys[i].middle, Color.magenta, 25);
                }

                float distance = getClosestPointToWall(map, curvePoints[j], dbk, true);
                if (distance < map.minimumDistanceToWall)
                    minDistances += map.minimumDistanceToWall - distance;

                
                lastPoint = curvePoints[j];
            }
        }
        yawChanges = sumFloats(curvatures.ToArray());
        //Debug.Log("Length:" + length);
        /*for(int i = 0; i<curvatures.Count; i++)
        {
            
            maxYaws += Mathf.Max(0,Mathf.Abs(curvatures[i]) - map.maxYaw);
            if(i>0)
                yawChanges += Mathf.Abs(curvatures[i] - curvatures[i - 1])* Mathf.Abs(curvatures[i] - curvatures[i - 1]);
        }*/
        //Debug.Log($"{outside} {maxYaws} {length} {minDistances} {yawChanges}");

        return new TrajectoryStats(outside, maxYaws, length, minDistances, yawChanges);
    }

    public static (float,List<float>, int) bezierCurvatures(Vector3 P1, Vector3 C1, Vector3 C2, Vector3 P2, float maxYaw, bool dbk = false, int resolution=20)
    {
        List<float> toReturn = new List<float>();
        float my = 0;
        int maxYaws = 0;
        for(int i=0; i<resolution;i++)
        {
            float t = (float)i / resolution;
            float c = getCurvature(P1, C1, C2, P2, t);
            //float y = Mathf.Abs(ComputeCurvatureRate(P1, C1, C2, P2, t));
            if (Mathf.Abs(c * 1.5f) > maxYaw)
            {
                maxYaws++;
                Debug.DrawRay(CubicBezier(P1, C1, C2, P2, t), Vector3.forward, Color.red);
            }
            my += c*c;//y * y* y* y;
            toReturn.Add(c);
        }
        return (my,toReturn, maxYaws);
    }
    public static List<float> computeCurvature(Vector3[] points)
    {
        List<float> curvatures = new List<float>();

        for (int i = 1; i < points.Length - 1; i++)
        {
            Vector3 p1 = points[i - 1];
            Vector3 p2 = points[i];
            Vector3 p3 = points[i + 1];

            float d12 = Vector3.Distance(p1, p2);
            float d23 = Vector3.Distance(p2, p3);
            float d13 = Vector3.Distance(p1, p3);

            if (d12 * d23 * d13 == 0)
            {
                curvatures.Add(0f);
                continue;
            }
            float crossProduct = (p2.x - p1.x) * (p3.z - p1.z) - (p2.z - p1.z) * (p3.x - p1.x);
            float curvature = crossProduct / (d12 * d23 * d13);
            curvatures.Add(curvature);
        }

        curvatures.Insert(0, 0f);
        curvatures.Add(0f);

        return curvatures;
    }

    public static float curvature3Points(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float d12 = Vector3.Distance(p1, p2);
        float d23 = Vector3.Distance(p2, p3);
        float d13 = Vector3.Distance(p1, p3);

        if (d12 * d23 * d13 == 0)
        {
            return 0;
        }
        float crossProduct = (p2.x - p1.x) * (p3.z - p1.z) - (p2.z - p1.z) * (p3.x - p1.x);
        float curvature = crossProduct / (d12 * d23 * d13);
        return curvature;
    }

    public static (Vector3 center, float radius) GetCircle(Vector3 A, Vector3 B, Vector3 C)
    {
        float x1 = A.x, z1 = A.z;
        float x2 = B.x, z2 = B.z;
        float x3 = C.x, z3 = C.z;

        float d = 2 * (x1 * (z2 - z3) + x2 * (z3 - z1) + x3 * (z1 - z2));
        if (Mathf.Abs(d) < 1e-6) return (Vector3.zero, float.MaxValue);

        float cx = ((x1 * x1 + z1 * z1) * (z2 - z3) +
                    (x2 * x2 + z2 * z2) * (z3 - z1) +
                    (x3 * x3 + z3 * z3) * (z1 - z2)) / d;

        float cz = ((x1 * x1 + z1 * z1) * (x3 - x2) +
                    (x2 * x2 + z2 * z2) * (x1 - x3) +
                    (x3 * x3 + z3 * z3) * (x2 - x1)) / d;

        Vector3 center = new Vector3(cx, 0, cz);
        float radius = Vector3.Distance(center, A);

        return (center, radius);
    }

    public static List<float> computeCurvature(List<Vector3> points)
    {
        List<float> curvatures = new List<float>();

        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 p1 = points[i - 1];
            Vector3 p2 = points[i];
            Vector3 p3 = points[i + 1];

            float d12 = Vector3.Distance(p1, p2);
            float d23 = Vector3.Distance(p2, p3);
            float d13 = Vector3.Distance(p1, p3);

            if (d12 * d23 * d13 == 0)
            {
                curvatures.Add(0f);
                continue;
            }
            float crossProduct = (p2.x - p1.x) * (p3.z - p1.z) - (p2.z - p1.z) * (p3.x - p1.x);
            float curvature = crossProduct / (d12 * d23 * d13);
            curvatures.Add(curvature);
        }

        curvatures.Insert(0, 0f);
        curvatures.Add(0f);

        return curvatures;
    }
    public static float curvature(Vector3 V1,Vector3 V2)
    {
        float len1 = V1.magnitude;
        float len2 = V2.magnitude;

        if (len1 < 1e-5f || len2 < 1e-5f)
            return 0;
        float dot = Vector3.Dot(V1.normalized,V2.normalized);
        dot = Mathf.Clamp(dot, -1f, 1f);

        float theta = Mathf.Acos(dot);

        float arcLength = len1 + len2;

        float curvaturePerM = theta / arcLength;
        float curvatureDegPerM = curvaturePerM * Mathf.Rad2Deg;
        float cross = (V1.x * V2.z) - (V1.z * V2.x);
        float sign = Mathf.Sign(cross);
        return curvatureDegPerM*sign;
    }
    public static Vector3 GetBezierSecondDerivative(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        return 6 * (1 - t) * (P2 - 2 * P1 + P0) +
               6 * t * (P3 - 2 * P2 + P1); ;
    }

    public static Vector3 GetBezierThirdDerivative(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3)
    {
        return 6 * (P3 - 3 * P2 + 3 * P1 - P0);
    }

    public static float ComputeCurvatureRate(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        Vector3 d1 = GetFirstDerivative(P0, P1, P2, P3, t);
        Vector3 d2 = GetBezierSecondDerivative(P0, P1, P2, P3, t);
        Vector3 d3 = GetBezierThirdDerivative(P0, P1, P2, P3);

        // Compute cross products in the XZ plane
        float cross_d1_d2 = d1.x * d2.z - d1.z * d2.x;
        float cross_d1_d3 = d1.x * d3.z - d1.z * d3.x;

        // Compute speed and squared speed
        float speedSquared = d1.x * d1.x + d1.z * d1.z;
        float speed = Mathf.Sqrt(speedSquared);

        // Avoid division by zero
        if (speedSquared == 0) return 0;

        // Compute rate of change of curvature (dκ/dt)
        float dCurvature_dt =
            (cross_d1_d3 * Mathf.Pow(speedSquared, 1.5f) -
             1.5f * cross_d1_d2 * Mathf.Sqrt(speedSquared) * (2 * (d1.x * d2.x + d1.z * d2.z)))
            / Mathf.Pow(speedSquared, 3);

        // Convert from dκ/dt to dκ/ds by dividing by arc-length derivative (ds/dt)
        return speed != 0 ? dCurvature_dt / speed : 0;
    }
    public static float getCurvature(
    Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        Vector3 firstd =
            GetFirstDerivative(P0, P1, P2, P3, t);

        Vector3 secondd =
            GetBezierSecondDerivative(P0, P1, P2, P3, t);

        float mag = firstd.magnitude;

        if (mag < 1e-6f)
        {
            Debug.LogError(
                $"Zero derivative at t={t}\n" +
                $"P0={P0}\nP1={P1}\nP2={P2}\nP3={P3}\n" +
                $"firstd={firstd}");
        }

        float crossProduct =
            Vector3.Cross(firstd, secondd).y;

        return crossProduct / Mathf.Pow(mag, 3);
    }

    public static Vector3 GetOsculatingCircleCenter(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        // Get the point on the Bezier curve at parameter t
        Vector3 point = CubicBezier(P0, P1, P2, P3, t);

        // Get the radius of curvature
        float radius = GetCurvatureRadius(P0, P1, P2, P3, t);

        // Get the first derivative (tangent vector) at t
        Vector3 firstDerivative = GetFirstDerivative(P0, P1, P2, P3, t);

        // Get the unit normal vector (perpendicular to the tangent)
        Vector3 normal = new Vector3(firstDerivative.z,0, -firstDerivative.x).normalized;

        // The center of the osculating circle is offset along the normal by the radius of curvature
        return point + normal * radius;
    }
    public static float GetCurvatureRadius(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        float curvature = getCurvature(P0, P1, P2, P3, t);
        if (curvature != 0)
        {
            return 1 / curvature;
        }
        return float.PositiveInfinity;  // For flat (zero curvature) parts of the curve
    }
    public static Vector3[] SingleBezier(Vector3 S1, Vector3 C1,Vector3 C2, Vector3 S2,bool dbk=false, int resolution = 20)
    {
        Vector3[] points = new Vector3[resolution+1];
        points[0] = S1;
        for (int i=1; i<resolution+1; i++)
        {
            
            float t = (float)i / resolution;
            points[i] = CubicBezier(S1, C1, C2, S2, t);
            if (dbk)
                Debug.DrawLine(points[i], points[i-1], Color.red, 10);

        }
        return points;
    }

    public static Vector3 pointOnLine(Vector3 p, Vector3 l1, Vector3 l2, float distance)
    {
        float dx = l2.x - l1.x;
        float dy = l2.z - l1.z;

        float A = dx * dx + dy * dy;
        float B = 2 * ((l1.x - p.x) * dx + (l1.z - p.z) * dy);
        float C = (l1.x - p.x) * (l1.x - p.x) + (l1.z - p.z) * (l1.z - p.z) - distance * distance;

        float dis = B * B - 4 * A * C;

        if(dis<0)
        {
            Debug.Log("No result");
            return Vector3.zero;
        }

        float t1 = (-B + Mathf.Sqrt(dis)) / (2 * A);
        float t2 = (-B - Mathf.Sqrt(dis)) / (2 * A);

        if((t1>1 || t1<0) && (t2>1 || t2<0))
        {
            Debug.Log("Both points outside of the line");
            return Vector3.zero;
        }
        Vector3 p1 = new Vector3(l1.x + t1 * dx, 0, l1.z + t1 * dy);
        if(t1>1 || t1<0)
            p1 = new Vector3(l1.x + t2 * dx, 0, l1.z + t2 * dy);

        return p1;
    }
    
    public static TrajectoryResult normalizePath(TrajectoryResult original, rMap map, float stepSize = 0.1f, bool dbg = false)
    {
        int newLength = (int)Mathf.Ceil(original.length / stepSize);
        float stepSize2 = stepSize * stepSize;
        List<Vector3> points = new List<Vector3>();
        float[] yaws;
        float[] distances;

        points.Add(original.path[0]);
        int i = 1;
        int j = 0;
        float len = 0;
        while(i<original.path.Length)
        {
            if((original.path[i]-points[j]).sqrMagnitude>stepSize2)
            {
                Vector3 newPoint = pointOnLine(points[j], original.path[i - 1], original.path[i], stepSize);
                points.Add(newPoint);
                len += (newPoint - points[j]).magnitude;
                j++;
            }
            else
                i++;
        }
        points.Add(original.path[original.path.Length - 1]);
        len += (points[points.Count-1]-points[points.Count-2]).magnitude;
        Vector3[] pointArray = points.ToArray();
        yaws = yawOverPath(pointArray);
        distances = distancesOverPath(map, pointArray, dbg);

        return new TrajectoryResult(pointArray, original.outsidePoints, yaws, distances, len);
    }
    
    /*public static TrajectoryResult normalizePath(TrajectoryResult trajectory, List<rEdge> walls, float stepSize = 0.1f)
    {
        int newLength = (int)Mathf.Floor(trajectory.length / stepSize);
        List<Vector3> points = new List<Vector3>();
        List<float> yaws = new List<float>();
        List<float> distances = new List<float>();
        int j = 0;

        Vector3 o = trajectory.path[1] - trajectory.path[0];
        Vector3 no = o;

        float newL = 0;
        Vector3 pp = trajectory.path[0];

        for(int i=1; i< trajectory.path.Length; i++)
        {
            if((trajectory.path[i]-pp).magnitude>stepSize)
            {
                Vector3 np = trajectory.path[i - 1];
                no = np - pp;
                points.Add(np);
                yaws.Add(Vector3.SignedAngle(no, o, Vector3.up) / stepSize);
                distances.Add(getClosestPointToWall(walls, np));
                pp = np;
                newL += no.magnitude;
            }
        }

        Vector3 npp = trajectory.path[trajectory.path.Length-1];
        no = npp - pp;
        points.Add(npp);
        yaws.Add(Vector3.SignedAngle(no, o, Vector3.up) / stepSize);
        distances.Add(getClosestPointToWall(walls, npp));
        newL += no.magnitude;*/

        /*
        points[0] = trajectory.path[0];
        distances[0] = getClosestPointToWall(walls, points[0]);
        Vector3 no = o;
        float newDistance = 0;
        for(int i=1; i<newLength; i++)
        {
            float distanceTravelled = stepSize;
            float nextStep = (trajectory.path[j] - points[i-1]).magnitude;
            while (nextStep<distanceTravelled && j<trajectory.path.Length-1)
            {
                j++;
                nextStep = (trajectory.path[j] - points[i - 1]).magnitude;
                Debug.Log($"nextStep: {nextStep}  j: {j}  StepSize: {stepSize}  i: {i}");
            }
            Debug.Log($"Picked point: {j}");
            points[i] = trajectory.path[j - 1];
            no = points[i] - points[i - 1];
            yaws[i] = Vector3.SignedAngle(no, o, Vector3.up) / stepSize;
            o = no;
            distances[i] = getClosestPointToWall(walls, points[i]);
            newDistance += no.magnitude;



        }
        points[newLength] = trajectory.path[trajectory.path.Length-1];
        no = points[newLength] - points[newLength - 1];
        yaws[newLength] = Vector3.SignedAngle(no, o, Vector3.up) / stepSize;
        distances[newLength] = getClosestPointToWall(walls, points[newLength]);
        newDistance += no.magnitude;*/

       /* return new TrajectoryResult(points.ToArray(), trajectory.outsidePoints, yaws.ToArray(), distances.ToArray(), newL);




    }*/

    public static float[] distancesOverPath(rMap map, Vector3[] points, bool dbg = false)
    {
        float[] distances = new float[points.Length];

        for(int i=0; i<points.Length; i++)
        {
            distances[i] = getClosestPointToWall(map, points[i],dbg);
        }
        return distances;
    }
    public static float getClosestPointToWall(rMap map, Vector3 point, bool dbg = false, bool includeObstacles = false)
    {
        float dist = float.MaxValue;
        List<int> walls = new List<int>();
        map.getWallsAround(point, ref walls, includeObstacles);
        //if(dbg)
            //Debug.Log(walls.Count);
        foreach (int w in walls)
        {
            rEdge e = map.edges[w];
            float d = DistanceFromPointToLineSegment(point, e.start, e.end);
            if (d < dist)
                dist = d;
        }
        //if (dbg)
        //    Debug.Log(dist);
        return dist;
    }
    public static List<rEdge> getWalls(rPolygon[] polygons)
    {
        List<rEdge> walls = new List<rEdge>();
        foreach(rPolygon poly in polygons)
            foreach(int i in poly.edges)
            {
                if (poly.map.edges[i].isWall)
                    walls.Add(poly.map.edges[i]);
            }
        return walls;
    }

    public static Vector3 getNormal(rEdge e, rPolygon q)
    {

        Vector3 v = (e.end - e.start);
        Vector3 r = rotate(v,90).normalized;
        Debug.DrawRay(e.middle, v*0.3f, Color.red, 10);
        if (q.isInside(e.middle+r*0.2f))
            return r;

        return -r;
    }

    public static float pointByDistance(Bezier b, float distance, float start = 0, float d = 0, float end = 1, int resolution = 20, int repeats = 4)
    {
        //Debug.Log($"{start} {d} {end}");
        Vector3 v = CubicBezier(b.start,b.C1,b.C2,b.end,start);
        float lastp = start;
        float startd = d;
        for(int i=0; i<=resolution; i++)
        {
            float newp = start + ((float)i / resolution) * (end - start);
            Vector3 np = CubicBezier(b.start, b.C1, b.C2, b.end, newp);
            float newDistance = (np - v).magnitude;
            if(d+newDistance>distance)
            {
                if (repeats > 0)
                    return pointByDistance(b, distance, lastp, d, newp, resolution, repeats - 1);
                else
                    return lastp + ((distance-d)/newDistance)*(newp-lastp);
            }
            d += newDistance;
            v = np;
            lastp = newp;
        }
        return end;

    }

    public static Vector3 tangentToBezier(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        float u = 1 - t;

        Vector3 tangent =
            3 * u * u * (P1 - P0) +
            6 * u * t * (P2 - P1) +
            3 * t * t * (P3 - P2);

        return tangent.normalized;
    }

    public static Vector3 tangentToBezier(rMap.BezierPoint point)
    {
        return tangentToBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
    }

    public static Vector3 GetFirstDerivative(
    Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        float u = 1f - t;
        return
            3f * u * u * (P1 - P0) +
            6f * u * t * (P2 - P1) +
            3f * t * t * (P3 - P2);
    }
    public static TrajectoryResult optimizeCubicBezierV2(List<int> path, Vector3 startPos, float desiredLength, rMap map, float maxControlDistance, float minControlDistance, Path original = null)
    {
        int edgeCount = path.Count;
        Vector3[] p = new Vector3[edgeCount];
        Vector3[] c = new Vector3[edgeCount];
        Vector3[] n = new Vector3[edgeCount];
        int e1 = path[edgeCount - 1];
        int e2 = path[edgeCount - 2];
        rTrack t = map.getTrack(e1, e2);
        int q = t.getPolygon();
        NonlinearConstraint[] constraints = new NonlinearConstraint[(edgeCount) * 3];
        rPolygon[] quads = new rPolygon[edgeCount];
        initialGuess = new double[edgeCount * 3];
        for (int i = 0; i < edgeCount; i++)
        {
            int ee1 = path[i];
            int ee2;

            rEdge edge1 = map.edges[ee1];
            if (i == edgeCount - 1)
                ee2 = path[i - 1];
            else
                ee2 = path[i + 1];

            rEdge edge2 = map.edges[ee2];
            t = map.getTrack(ee1, ee2);
            q = t.getPolygon();
            quads[i] = map.polygons[q];
            p[i] = edge1.middle;
            c[i] = getNormal(edge1, quads[i]);
            n[i] = (edge1.end - edge1.start).normalized;
            if (i == edgeCount - 1)
                c[i] = -c[i];
            int index2 = i;
            float width = ((edge1.end - edge1.start)).magnitude*0.4f;
            constraints[index2 * 3] = (new NonlinearConstraint((edgeCount) * 3, vars => Mathf.Abs((float)vars[index2 * 3]) - width , ConstraintType.LesserThanOrEqualTo)); ;
            constraints[index2 * 3 + 1] = (new NonlinearConstraint((edgeCount) * 3, vars => Mathf.Abs((float)vars[index2 * 3 + 1]) - 1f, ConstraintType.LesserThanOrEqualTo));
            constraints[index2 * 3 + 2] = (new NonlinearConstraint((edgeCount) * 3, vars => Mathf.Abs((float)vars[index2 * 3 + 2] - 1f) - 1f, ConstraintType.LesserThanOrEqualTo));
            initialGuess[index2 * 3] = UnityEngine.Random.Range(-1f,1f);
            initialGuess[index2 * 3 + 1] = 0;
            initialGuess[index2 * 3 + 2] = 0.5;
        }

        Vector3[] pp = new Vector3[p.Length];
        Vector3[] cc = new Vector3[c.Length];
        System.Func<double[], double> objective = var =>
        {
            pp[0] = startPos;
            cc[0] = c[0] * (maxControlDistance+minControlDistance)/2;
            pp[pp.Length - 1] = p[pp.Length - 1] + n[pp.Length - 1] * (float)var[(pp.Length-1) * 3];
            cc[pp.Length - 1] = c[pp.Length - 1] * (maxControlDistance + minControlDistance) / 2;
            float totalTurn = 0;
            float difference = 0;
            for (int i = 1; i < path.Count-1; i++)
            {
                pp[i] = p[i] + n[i] * (float)var[(i) * 3];
                //Debug.Log((float)var[(i) * 3 + 1]);
                //Debug.Log((float)var[(i) * 3 + 2]);
                //if (Mathf.Abs((float)var[(i) * 3 + 1]) < 0.3f)
                    //var[(i) * 3 + 1] = 0;
                cc[i] = rotate(c[i], (float)var[(i) * 3 + 1] * Mathf.Rad2Deg) * (minControlDistance + (float)var[(i) * 3 + 2]*(maxControlDistance-minControlDistance));
                difference += Mathf.Abs(Mathf.Abs((float)(var[(i) * 3]) - Mathf.Abs((float)var[(i-1) * 3])));
                //Debug.Log($"Index: {i}  cc: {cc[i]}  var: {var[i * 3]}   var2: {var[i * 3 + 1]}  var3: {var[i * 3 + 2]}  ii: {i * 3 + 2}");
            }
            TrajectoryResult result = fullBezier(pp, cc, quads, map);
            float length = pathLength(result.path);
            float maxYaws = 0;
            
            maxYaws = countMaxYaws(result.yaws, map.maxYaw);
            float minDistances = countMinDistances(result.distances, map.minimumDistanceToWall);
            //float length = CalculateBezierLength(start, start + startDir, C3, end + endDir, end);
            return Mathf.Pow((length) - desiredLength, 2) + result.outsidePoints * 10000 + maxYaws * 1000 + minDistances*10000+sumChanges(result.yaws)/result.yaws.Length;
            
        };

        NonlinearObjectiveFunction f = new NonlinearObjectiveFunction((edgeCount) * 3, objective);
        var optimizer = new Cobyla(f, constraints);
        optimizer.MaxIterations = 10000;
        bool success = false;
        success = optimizer.Minimize(initialGuess);

        /*
        bool success = optimizer.Minimize(initialGuess);
        Debug.Log(optimizer.Status);
        */

        TrajectoryResult result = new TrajectoryResult(0);
        if (success)
        {
            pp[0] = startPos;
            cc[0] = c[0] * (maxControlDistance + minControlDistance) / 2;
            pp[pp.Length - 1] = p[pp.Length - 1] + n[pp.Length - 1]* (float)optimizer.Solution[(pp.Length - 1)*3];

            
            cc[pp.Length - 1] = c[pp.Length - 1] * (maxControlDistance + minControlDistance) / 2;
            for (int i = 1; i < path.Count-1; i++)
            {
                //Debug.Log($"Point {i} {p[i]}\tpp {optimizer.Solution[(i - 1) * 3]}\tcc {optimizer.Solution[(i - 1) * 3 + 1]}");
                pp[i] = p[i] + n[i] * (float)optimizer.Solution[(i) * 3];
                //if (Mathf.Abs((float)optimizer.Solution[(i) * 3 + 1]) < 0.3f)
                //    optimizer.Solution[(i) * 3 + 1] = 0;
                cc[i] = rotate(c[i], (float)optimizer.Solution[(i) * 3 + 1] * Mathf.Rad2Deg) * (minControlDistance+(float)optimizer.Solution[(i) * 3 + 2] * (maxControlDistance - minControlDistance));
        }
            result = fullBezier(pp, cc, quads, map,false);
            
            Debug.Log($"Target length: {desiredLength}\tFinal length: {pathLength(result.path)}\tFinal YawSum: {sumFloats(result.yaws)}\t Outside points:{result.outsidePoints}");
            
        }
        return result;


    }

 

    public static float[] yawOverPath(Vector3[] points)
    {
        float[] yaws = new float[points.Length];
        Vector3 last = points[1] - points[0];
        Vector3 current;
        for (int i = 1; i < points.Length - 1; i++)
        {
            current = points[i + 1] - points[i];
            yaws[i] = Vector3.SignedAngle(last, current,Vector3.up);
            last = current;
        }
        return yaws;
    }

    
    public static Vector3 rotate(Vector3 v, float delta)
    {
        //Debug.Log(v + "," + delta);
        return Quaternion.Euler(0, delta, 0) * v;
    }

    public static float pathLength(Vector3[] path)
    {
        float length = 0;
        for (int i = 1; i < path.Length; i++)
        {
            length += (path[i] - path[i - 1]).magnitude;
        }
        return length;
    }

    public static float sumFloats(float[] f)
    {
        float s = 0;
        foreach (float ff in f)
        {
            s += Mathf.Abs(ff);
        }
        return s;
    }

    public static float sumChanges(float[] f)
    {
        float s = 0;
        for(int i =1; i<f.Length; i++)
        {
            s += Mathf.Abs(f[i] - f[i - 1]);
        }
        return s;
    }
    public static float countMaxYaws(float[] floats, float maxYaw)
    {
        float i = 0;
        foreach (float f in floats)
        {
            if (Mathf.Abs(f) > maxYaw)
                i += Mathf.Abs(f)-maxYaw;
        }
        return i;
    }
    public static float countMinDistances(float[] floats, float minDistance)
    {
        float i = 0;
        foreach (float f in floats)
        {
            if (f < minDistance)
                i += minDistance - f ;
        }
        return i;
    }

    public struct TrajectoryStats
    {
        public int outsidePoints;
        public float minDistances;
        public float maxYaws;
        public float length;
        public float yawChanges;
        
        public TrajectoryStats(int outsidePoints, float maxYaws, float length, float minDistances, float yawChanges)
        {

            this.outsidePoints = outsidePoints;
            this.maxYaws = maxYaws;
            this.length = length;
            this.minDistances = minDistances;
            this.yawChanges = yawChanges;
        }
    }
    public struct TrajectoryResult
    {
        public Vector3[] path;
        public float[] yaws;
        public float[] distances;
        public int outsidePoints;
        public float length;

        public TrajectoryResult(Vector3[] path, int outsidePoints, float[] yaw, float[] dist)
        {
            this.path = path;
            this.outsidePoints = outsidePoints;
            yaws = yaw;
            distances = dist;
            length = 0;
        }
        

        public TrajectoryResult(Vector3[] path, int outsidePoints, float[] yaw, float[] dist, float len)
        {
            this.path = path;
            this.outsidePoints = outsidePoints;
            yaws = yaw;
            distances = dist;
            length = len;
        }

        public TrajectoryResult(int length)
        {
            path = new Vector3[length];
            outsidePoints = 0;
            yaws = new float[length];
            distances = new float[length];
            this.length = 0;
        }

        public TrajectoryResult(int length, float d)
        {
            path = new Vector3[length];
            outsidePoints = 0;
            yaws = new float[length];
            distances = new float[length];
            this.length = d;
        }


    }



















    public static float DistanceFromPointToLineSegment(
        Vector3 point, // Coordinates of the point
        Vector3 l1, // Coordinates of the first endpoint of the line segment
        Vector3 l2  // Coordinates of the second endpoint of the line segment
    )
    {
        float x0 = point.x;
        float y0 = -point.z;

        float x1 = l1.x;
        float y1 = -l1.z;
        float x2 = l2.x;
        float y2 = -l2.z;
        // Calculate the squared length of the line segment
        float lineLengthSquared = Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2);

        // If the segment is a point
        if (lineLengthSquared == 0)
        {
            // Distance to the point
            return Mathf.Sqrt(Mathf.Pow(x0 - x1, 2) + Mathf.Pow(y0 - y1, 2));
        }

        // Calculate the projection scalar
        float t = ((x0 - x1) * (x2 - x1) + (y0 - y1) * (y2 - y1)) / lineLengthSquared;

        // Clamp t to the range [0, 1]
        t = Mathf.Max(0, Mathf.Min(1, t));

        // Calculate the closest point on the line segment
        float closestX = x1 + t * (x2 - x1);
        float closestY = y1 + t * (y2 - y1);

        // Calculate the distance from the point to the closest point
        return Mathf.Sqrt(Mathf.Pow(x0 - closestX, 2) + Mathf.Pow(y0 - closestY, 2));
    }




    public static Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
    // Start is called before the first frame update
    

    public static bool isClose(Vector3 s, Vector3 e, float d = 0.001f)
    {
        return (e-s).sqrMagnitude < d;
    }

    public static bool doIntersect(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        return doIntersect(toV2(p1), toV2(q1), toV2(p2), toV2(q2));
    }

    public static int orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

        if (val == 0) return 0;

        return val > 0 ? 1 : 2;
    }

    static bool onSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        return (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y));
    }

    public static Vector2 toV2(Vector3 c)
    {
        return new Vector2(c.x, -c.z);
    }

    public static bool doIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        int o1 = orientation(p1, q1, p2);
        int o2 = orientation(p1, q1, q2);
        int o3 = orientation(p2, q2, p1);
        int o4 = orientation(p2, q2, q1);

        // General case 
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases 
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1 
        if (o1 == 0 && onSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are collinear and q2 lies on segment p1q1 
        if (o2 == 0 && onSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2 
        if (o3 == 0 && onSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2 
        if (o4 == 0 && onSegment(p2, q1, q2)) return true;

        return false;
    }

    public static void getAdjacentTracks(rMap map, int edge, int poly, ref List<int> toReturn)
    {
        toReturn.Clear();
        for (int i = 0; i < map.tracks.Count; i++)
        {
            if(poly>-1)
                if (map.tracks[i].getPolygon() == poly)
                    continue;
            if (map.tracks[i].start == edge || map.tracks[i].end == edge)
                toReturn.Add(i);

        }
    }

    public static int getEnd(rMap map, int track, int edge)
    {
        if (map.tracks[track].start == edge)
            return 1;
        else if (map.tracks[track].end == edge)
            return -1;
        return 0;
    }

    public static int getNextEdge(rMap map, int track, int edge)
    {
        int end = getEnd(map, track, edge);
        if (end == 1)
            return map.tracks[track].end;
        else if (end == -1)
            return map.tracks[track].start;
        return -1;
    }


    public static List<SpeedProfile> orderSpeedProfiles(List<SpeedProfile> original, float center, float std, float avg=0.75f, int count=20)
    {
        List<SpeedProfile> originals = new List<SpeedProfile>(original);
        List<SpeedProfile> profiles = new List<SpeedProfile>();
        HashSet<SpeedProfile> differentPieces = new HashSet<SpeedProfile>();

        List<SpeedProfile> possible = new List<SpeedProfile>();
        for(int tries=0; tries<50;tries++)
        {
            originals = new List<SpeedProfile>(original);
            profiles = new List<SpeedProfile>();
            SpeedProfile picked = generateAcceleration(0,0);
            int totalTrials = 0;
            while (originals.Count > 0)
            {
                SpeedProfile n;
                bool found = false;
                for(int tr =0; tr<100; tr++)
                {
                    n= originals[UnityEngine.Random.Range(0, originals.Count - 1)];
                    if (!differentPieces.Contains(n))
                        differentPieces.Add(n);

                    if (!(n.start == picked.start && n.end == picked.end))
                    {
                        originals.Remove(n);
                        profiles.Add(generateAcceleration(picked.end, n.start));
                        profiles.Add(n);
                        picked = n;
                        found = true;
                        break;
                    }

                }
                if(!found)
                {
                    totalTrials++;
                    if(totalTrials>100)
                    {
                        Debug.Log("Total failure");
                        return new List<SpeedProfile>();
                    }
                    else
                    {
                        break;
                    }
                }
                
            }
            Debug.Log($"Originals: {originals.Count}\tPieces: {profiles.Count}");
            foreach (SpeedProfile p in differentPieces)
            {
                List<SpeedProfile> candidate = new List<SpeedProfile>(profiles);
                candidate.Add(generateAcceleration(picked.end, p.start));
                List<SpeedProfile> toReturn = new List<SpeedProfile>();
                toReturn.Add(generateAcceleration(0, original[0].start));
                toReturn.AddRange(candidate);
                toReturn.Add(generateAcceleration(candidate[candidate.Count - 1].end, 0));
                    Debug.Log(calculateAvgVelocity(toReturn));
                if (calculateAvgVelocity(toReturn) == avg)
                {
                   
                    return toReturn;
                }
            }
        }


        return new List<SpeedProfile>() ;
    }

    public static SpeedProfile generateAcceleration(float start, float end, float acceleration=0.5f)
    {
        float time = Mathf.Abs(end - start) / acceleration;
        return new SpeedProfile(start, end, time);
    }

    public static AnimationCurve profilesToCurve(List<SpeedProfile> profiles)
    {
        float totalTime = 0;
        foreach (SpeedProfile pf in profiles)
            totalTime += pf.length;
        List<Keyframe> keyFrames = new List<Keyframe>();
        float time = 0;
        foreach(SpeedProfile pf in profiles)
        {
            if (keyFrames.Count == 0)
                keyFrames.Add(new Keyframe(0, pf.start));
            time += pf.length / totalTime;
            keyFrames.Add(new Keyframe(time, pf.end));
        }


        AnimationCurve curve = new AnimationCurve(keyFrames.ToArray());
        return curve;
    }

    public static float calculateAvgVelocity(List<SpeedProfile> profiles)
    {
        float len = 0;
        float dist = 0;
        foreach(SpeedProfile pf in profiles)
        {
            len += pf.length;
            dist += pf.getDistance();
        }
        return dist / len;
    }
}

[System.Serializable]
public class PathState
{
    public int[] tracks;
    public bool[] usedTracks;

    public PathState(int[] tracks, bool[] usedTracks)
    {
        this.tracks = tracks;
        this.usedTracks = usedTracks;
    }

    public PathState(int[] tracks)
    {
        this.tracks = tracks;
        usedTracks = new bool[tracks.Length];
    }
}
[System.Serializable]
public class rEdge
{
    public Vector3 start;
    public Vector3 end;
    public bool isWall;
    public bool isObstacle;

    public rEdge(Vector3 s, Vector3 e)
    {
        start = s;
        end = e;

    }

    public rEdge(Vector3 start, Vector3 end, bool isWall) : this(start, end)
    {
        this.isWall = isWall;
    }

    public Vector3 normalized
    {
        get
        {
            return (end - start).normalized;
        }
    }

    public Vector3 normal
    {
        get
        {
            return rHelpers.rightAngle * normalized;
        }
    }

    public Vector3 middle
    {
        get
        {
            return (start + end) / 2;
        }
    }

    public int shareStart(rEdge other)
    {
        if (rHelpers.isClose(start, other.start))
            return 1;
        if (rHelpers.isClose(start, other.end))
            return -1;
        return 0;
    }

    public int shareEnd(rEdge other)
    {
        if (rHelpers.isClose(end, other.start))
            return 1;
        if (rHelpers.isClose(end, other.end))
            return -1;
        return 0;
    }

    public bool isSame(rEdge other)
    {
        return (rHelpers.isClose(start, other.start) && rHelpers.isClose(end, other.end)) || (rHelpers.isClose(end, other.start) && rHelpers.isClose(start, other.end));
    }
}

[System.Serializable]
public class rPolygon
{
    public rMap map;
    public int[] edges;

    public Vector3 middle
    {
        get
        {
            Vector3 m = Vector3.zero;
            foreach(int i in edges)
            {
                m += map.edges[i].middle;
            }
            return m / edges.Length;
        }
    }

    public rPolygon(int[] edges, rMap map)
    {
        this.map = map;
        this.edges = edges;
    }

    public bool hasEdge(int edge)
    {
        foreach (int i in edges)
            if (i == edge)
                return true;
        return false;
    }

    public bool isSame(rPolygon other)
    {
        if (other.map != this.map)
            return false;
        if (other.edges.Length != edges.Length)
            return false;

        foreach(int i in edges)
        {
            bool Found = false;
            foreach(int j in other.edges)
            {
                if (i == j)
                    Found = true;
            }
            if (!Found)
                return false;
        }
        return true;
    }

    public List<int> shareEdge(rPolygon other)
    {
        
        List<int> commonEdges = new List<int>();
        if (isSame(other))
            return commonEdges;

        foreach(int i in edges)
        {
            foreach(int j in other.edges)
            {
                if (i == j)
                    commonEdges.Add(i);
            }
        }
        return commonEdges;

    }

    public bool isInside(Vector3 p)
    {
        Vector3 outsidePoint = p+new Vector3(100, 0, 0);
        int count = 0;
        foreach(int i in edges)
        {
            if (rHelpers.doIntersect(p, outsidePoint, map.edges[i].start, map.edges[i].end))
                count++;
        }
        return (count % 2) == 1;
    }
}

[System.Serializable]
public class rTrack
{
    public rMap map;
    public int start, end;
    public List<int> startTracks, endTracks;

    public Vector3 middle
    {
        get
        {
            return (map.edges[start].middle + map.edges[end].middle) / 2;
        }
    }

    public rTrack(rMap map, int start, int end)
    {
        this.map = map;
        this.start = start;
        this.end = end;
    }

    public int getPolygon()
    {
        for(int i = 0; i<map.polygons.Count; i++)
        {
            if (map.polygons[i].hasEdge(start) && map.polygons[i].hasEdge(end))
                return i;
        }
        return -1;
    }

    

    public bool isSame(int e1, int e2)
    {
        return (start == e1 && end == e2) || (start == e2 && end == e1);
    }
}



[System.Serializable]
public class SpeedProfile
{
    public float start;
    public float end;
    public float length;

    public SpeedProfile(float start, float end, float length)
    {
        this.start = start;
        this.end = end;
        this.length = length;
    }
    public SpeedProfile(SpeedProfile original, float length)
    {
        start = original.start;
        end = original.end;
        this.length = length;
    }

    public float getDistance()
    {
        return length*(start + end) / 2 ;
    }
    
}



[System.Serializable]
public class Bezier
{
    public Vector3 start;
    public Vector3 C1;
    public Vector3 C2;
    public Vector3 end;
    public float length;
    public List<float> dist = new List<float>();
    public List<float> iis = new List<float>();
    public Bezier(Vector3 start, Vector3 c1, Vector3 c2, Vector3 end)
    {
        this.start = start;
        C1 = c1;
        C2 = c2;
        this.end = end;
        prepareLength(true);
    }

    public Bezier(Bezier original)
    {
        start = original.start;
        C1 = original.C1;
        C2 = original.C2;
        end = original.end;
        length = original.length;
        dist = original.dist;
        iis = original.iis;
    }

    public override string ToString()
    {
        return $"{start};{C1};{C2};{end}";
    }

    public Bezier(string line)
    {
        string[] parts = line.Split(';');

        start = parseVector3(parts[0]);
        C1 = parseVector3(parts[1]);
        C2 = parseVector3(parts[2]);
        end = parseVector3(parts[3]);

        prepareLength(true);
    }

    private static Vector3 parseVector3(string s)
    {
        s = s.Trim('(', ')');

        string[] values = s.Split(",");
        return new Vector3(
            float.Parse(values[0], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture),
            float.Parse(values[1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture),
            float.Parse(values[2], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture)
            );
    }

    public void prepareLength(bool setDistances = true)
    {
        length = 0;
        Vector3 p = start;
        if (dist is null)
        {
            dist = new List<float>();
            iis = new List<float>();
        }
        if (setDistances)
        {
            dist.Clear();
            dist.Add(0);
            iis.Clear();
            iis.Add(0);

        }
        for(int i=1; i<=100; i++)
        {
            Vector3 np = rHelpers.CubicBezier(start, C1, C2, end, (float)i / 100f);
            length += (np - p).magnitude;
            p = np;
            if (setDistances)
            {
                dist.Add(length);
                iis.Add((float)i / 100f);

            }
        }
    }

    public float positionByDistance(float d, out Vector3 p, out Vector3 t)
    {

        if (d <= 0)
        {
            p = start;
            t = rHelpers.GetFirstDerivative(start, C1, C2, end, 0);
            return -1;
        }
        float distance = 0;
        Vector3 prev = start;
        Vector3 prevT = rHelpers.GetFirstDerivative(start, C1, C2, end, 0);

        for(int i=1; i<100; i++)
        {
            float tt = (float)i / 100;
            Vector3 newPoint = getPoint(tt);
            Vector3 newT = rHelpers.GetFirstDerivative(start, C1, C2, end, tt);
            float dis = (newPoint - prev).magnitude;
            if(distance+dis > d)
            {
                float s = (d - distance) / dis;
                p = Vector3.Lerp(prev, newPoint, s);
                t = Vector3.Lerp(prevT, newT, s);
                return -1;
            }
            distance += dis;
            prev = newPoint;
            prevT = newT;
        }

        float dist = (end - prev).magnitude;
        t = rHelpers.GetFirstDerivative(start, C1, C2, end, 1);
        p = end;
        return distance+dist;

    }

    public float pointByDistance(float d)
    {
        //Debug.Log("Getting distance: " + d + " It has length: "+length);
        if (d <= 0) return 0;
        float distance = 0;
        Vector3 prev = start;
        float prevtt = 0;
        for (int i =1; i< 21; i++)
        {
            float tt = (float)i/20f;
            Vector3 n = getPoint(tt);
            float dist = (n-prev).magnitude;
            if(distance+dist>d)
            {
                float s = (d - distance) / dist;
                return Mathf.Lerp(prevtt, tt, s);
            }
                //Debug.Log(tt);
            distance += dist;
            //Debug.Log(distance);
            prev = n;
            prevtt = tt;
        }

        return 1;

        
        
        int index = dist.BinarySearch(d);
        
        if (index > -1)
            return iis[index];
        
        int insertionIndex = ~index;
        if (insertionIndex < 0)
            return 1;
        index = insertionIndex-1;
        //Debug.Log(d + ", "+index);
        float reminder = d - dist[index];
        float t = reminder/(dist[index + 1] - dist[index]);

        return iis[index] + (iis[index + 1] - iis[index]) * t;
    }

    public Vector3 getPoint(float t)
    {
        return rHelpers.CubicBezier(start, C1, C2, end, t);
    }

    public Vector3 point(float t)
    {
        return getPoint(t);
    }

    public float t_by_distance(float d)
    {
        if (dist == null || dist.Count < 2)
            return 0f;

        // Clamp outside the curve
        if (d <= dist[0])
            return 0f;

        if (d >= dist[dist.Count - 1])
            return 1f;

        int index = dist.BinarySearch(d);

        // Exact match
        if (index >= 0)
        {
            return index / (float)(dist.Count - 1);
        }

        // BinarySearch gives the insertion point.
        int upper = ~index;
        int lower = upper - 1;

        float d0 = dist[lower];
        float d1 = dist[upper];

        // Protect against duplicate distance samples
        if (Mathf.Abs(d1 - d0) < 1e-8f)
        {
            return lower / (float)(dist.Count - 1);
        }

        float frac = (d - d0) / (d1 - d0);

        float sampleIndex = lower + frac;

        return sampleIndex / (dist.Count - 1f);
    }

    public Vector3 point_by_distance(float d)
    {
        return point(t_by_distance(d));
    }

    public Vector3 first_derivative(float t)
    {
        return rHelpers.GetFirstDerivative(start, C1, C2, end, t);
    }

    public Vector3 first_d_by_d(float d)
    {
        return first_derivative(t_by_distance(d));
    }

    public Vector3 second_derivative(float t)
    {
        return rHelpers.GetBezierSecondDerivative(start, C1, C2, end, t);
    }

    public Vector3 third_derivative(float t)
    {
        return rHelpers.GetBezierThirdDerivative(start, C1, C2, end);
    }

    public float get_curvature(float t)
    {
        return rHelpers.getCurvature(start, C1, C2, end, t);

    }

    public float get_curvature_radius(float t)
    {
        return rHelpers.GetCurvatureRadius(start, C1, C2, end, t);
    }
    
    
}
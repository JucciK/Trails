using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BezierPieces : MonoBehaviour
{
    public static BezierPieces Instance;

    public List<BezierPiecePath> paths = new List<BezierPiecePath>();
    public AnimationCurve distanceCurve;
    public List<Vector3> trajectory;
    public List<Vector3> tangents;
    public float timeStep = 0.1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public robotPose getPose(float t)
    {
        int prev = Mathf.Min(Mathf.FloorToInt(t * 10),trajectory.Count - 1);
        float tt = t * 10 - prev;

        int next = Mathf.Min(prev + 1, trajectory.Count - 1);

        Vector3 pos = Vector3.Lerp(trajectory[prev], trajectory[next], tt);
        Vector3 tan = Vector3.Lerp(tangents[prev], tangents[next], tt);

        

        float linvel = (trajectory[next]-trajectory[prev]).magnitude/timeStep;
        float angvel = Vector3.Angle(tangents[prev], tangents[next]) / timeStep;

        return new robotPose(pos, tan, linvel, angvel, 0, 0);
    }

    
    public void generateTrajectory(int path)
    {
        BezierPiecePath p = paths[path];

        trajectory.Clear();
        tangents.Clear();
        distanceCurve = new AnimationCurve(p.distanceCurve.keys);
        float length = distanceCurve.keys[distanceCurve.keys.Length - 1].time;
        float innerDistance = 0;
        int currentBezier = 0;
        float prevDist = 0;


        for(float i = 0; i<=length; i+= timeStep)
        {
            float d = distanceCurve.Evaluate(i);
            Vector3 pos;
            Vector3 tan;
            float left = p.beziers[currentBezier].positionByDistance(innerDistance, out pos, out tan);
            if(left>0)
            {
                currentBezier++;
                if (currentBezier == p.beziers.Count)
                {
                    trajectory.Add(pos);
                    tangents.Add(tan);

                    Debug.DrawRay(pos, Vector3.up, Color.green, 10);
                    Debug.DrawRay(pos, tan*0.1f, Color.red, 10);
                    break;
                }
                innerDistance -= left;
                float l2 = p.beziers[currentBezier].positionByDistance(innerDistance, out pos, out tan);
            }
            trajectory.Add(pos);
            tangents.Add(tan);

            Debug.DrawRay(pos, Vector3.up, Color.green, 10);
            Debug.DrawRay(pos, tan * 0.1f, Color.red, 10);
            innerDistance += (d - prevDist);
            prevDist = d;
        }


    }
}


[System.Serializable]
public class BezierPiecePath
{
    public List<Bezier> beziers;
    public BezierPieceStyle style;
    public AnimationCurve distanceCurve;

    public BezierPiecePath(BezierPiecePath origin)
    {
        beziers = new List<Bezier>(origin.beziers);
        style = origin.style;
        distanceCurve = origin.distanceCurve;
    }

    public BezierPiecePath(List<Bezier> b, BezierPieceStyle s)
    {
        beziers = b;
        style = s;
        distanceCurve = new AnimationCurve();
    }

    public BezierPiecePath()
    {
        beziers = new List<Bezier>() { new Bezier(Vector3.zero, Vector3.forward * 0.25f, Vector3.forward * 0.75f, Vector3.forward), new Bezier(Vector3.forward, Vector3.forward * 1.25f, Vector3.forward * 1.75f, Vector3.forward*2) };
        style = BezierPieceStyle.CORRIDOR_STRAIGHT;
        distanceCurve = new AnimationCurve();
    }

    public Vector3 Average()
    {
        if(beziers.Count== 0) return Vector3.zero;
        Vector3 pos = Vector3.zero;
        for(int i=0; i<beziers.Count; i++)
        {
            pos += (beziers[i].start + beziers[i].end)/2;
        }
        return pos / beziers.Count;
    }
}

public enum BezierPieceStyle { CORRIDOR_STRAIGHT, CORRIDOR_TURN, OPEN_STRAIGHT, OPEN_TURN, OBSTACLES_IN_CORRIDOR, OBSTACLE_IN_CORRIDOR };
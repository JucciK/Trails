using Accord.Math;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class FollowPath : MonoBehaviour
{
    public static FollowPath Instance;
    private void Awake()
    {
        Instance = this;
    }

    public int selectedPath = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float time;
    public rMap map;
    float lastTime;
    UnityEngine.Vector3 RightWheelPos, LeftWheelPos;
    public Transform leftWheel, rightWheel;

    public AnimationCurve volumeBySpeed;
    public AnimationCurve pitchBySpeed;

    public AudioSource RightWheel, LeftWheel;
    public float LeftWheelSpeed;
    public float RightWheelSpeed;
    public bool running = false;
    public bool stopped = false;
    public float timeStopped;

    //public AnimationCurve velocityCurve, angularVelocityCurve, stopCurve;
    public float velocity, angularVelocity;
    public float linear_acc, angular_acc;

    public Transform[] wheels;
    public float[] wheelRotations = new float[4];

    float leftVolume, leftPitch, rightVolume, rightPitch;
    public float pitchSmooth = 1, volumeSmooth = 1;
    public bool ended = false;

    public robotPose[] poses = new robotPose[1002];
    public List<string> pathFiles = new List<string>();
    public string pathPath = "C:/Users/jkalliok/AppData/LocalLow/Ubicomp/Sickness/18_12_25_paths/";
    public bool analyzing = false;

    public List<PathPiece> pathPieces = new List<PathPiece>();
    private PathPiece currentPiece = null;

    public AnimationCurve clipTimeCurve;

    BezierPieces pieces;
    void Start()
    {
        time = Time.time;

    }

    [EditorCools.Button]
    public void startPath()
    {
        openPath(selectedPath);
        StartRun();
    }

    public void openPath(int num)
    {
        loadPathFromFile(pathPath, pathFiles[num]);
        Debug.Log($"Opened new track: {num} \t {getPose(0)}");
    }

    public void openPath(string path, string file)
    {
        loadPathFromFile(path, file);
    }

    public robotPose[] getPath(int course)
    {
        if (course>-1)
            loadPathFromFile(pathPath, pathFiles[course]);

        return poses;

    }
    [EditorCools.Button]
    public void getFileNames()
    {
        pathFiles.Clear();
        string[] files = System.IO.Directory.GetFiles(pathPath, "*.csv");
        foreach (string file in files)
        {
            string f = System.IO.Path.GetFileName(file);
            pathFiles.Add(f);
        }
    }
    
    public void loadPathFromFile(string path, string file)
    {
        
        int i = 0;
        Debug.Log(path);
        List<robotPose> poseList = new List<robotPose>();
        using (StreamReader streamReader = new StreamReader(System.IO.Path.Combine(path, "speeds", file)))
        {
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                if (line.Length > 1)
                {
                    string[] splits = line.Split(';');
                    float speed = float.Parse(splits[0],System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                    float linear_acc = float.Parse(splits[1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                    float angular = float.Parse(splits[3], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                    float angular_acc = float.Parse(splits[4], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                    float kappa = float.Parse(splits[5], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                    //float.TryParse(splits[5], out kappa, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                    poseList.Add(new robotPose(speed, angular, linear_acc, angular_acc, kappa));
                    i++;
                }
            }
        }
        poses = poseList.ToArray();
        i = 0;
        using (StreamReader streamReader = new StreamReader(System.IO.Path.Combine(path, "positions", file)))
        {
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                if(line.Length > 1)
                {
                    string[] splits = line.Split(";");
                    UnityEngine.Vector3 pos = new UnityEngine.Vector3(float.Parse(splits[0], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(splits[1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(splits[2], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture));
                    UnityEngine.Vector3 forw = new UnityEngine.Vector3(float.Parse(splits[3], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(splits[4], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(splits[5], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture));
                    poses[i].pos = pos;
                    poses[i].forward = forw.normalized;
                    i++;
                }
            }
        }
    }


    public void OnDisable()
    {

    }
    public void StartRun()
    {
        time = 0;
        running = true;
        RightWheel.Play();
        LeftWheel.Play();

    }

    public void stop()
    {
        stopped = true;
        timeStopped = time;

    }
    // Update is called once per frame
    void Update()
    {

        
        if (running)
        {
            float ftime = time;
            //float distance = map.getDistance(time);//map.distanceTimeCurve.Evaluate(ftime);
            time += Time.deltaTime;
            /*
            rMap.BezierPoint prev = map.getPointByDistance(distance - 0.01f);
            rMap.BezierPoint next = map.getPointByDistance(distance + 0.01f);
            rMap.BezierPoint point = map.getPointByDistance(distance);




            Vector3 position = (rHelpers.CubicBezier(prev) + rHelpers.CubicBezier(next)) / 2;//rHelpers.CubicBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
            Vector3 tangent = (rHelpers.tangentToBezier(prev) + rHelpers.tangentToBezier(next)) / 2;//rHelpers.tangentToBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);



            //Debug.Log($"{time}  {distance}  {point.t}  {position}");
            */

            robotPose curPose = getPose(ftime);

            UnityEngine.Vector3 position = curPose.pos;
            UnityEngine.Vector3 tangent = curPose.forward;

            
            angularVelocity = curPose.angularVelocity;
            transform.rotation = Quaternion.LookRotation(tangent, UnityEngine.Vector3.up);
            //Logging.Instance.Log($"position_data; {position}; {Vector3.SignedAngle(Vector3.forward, tangent, Vector3.up)}");


            UnityEngine.Vector3 LeftWheelNewPos = leftWheel.position;
            UnityEngine.Vector3 RightWheelNewPos = rightWheel.position;

            linear_acc = curPose.linearAcceleration;
            angular_acc = curPose.angularAcceleration;
            //Debug.Log(rHelpers.getClosestPointToWall(map, transform.position));
            if (!stopped)
                velocity = curPose.linearVelocity;
            else
            {
                float new_vel = (position - transform.position).magnitude / Time.deltaTime;

                linear_acc = (new_vel - velocity) / Time.deltaTime;
                angular_acc = Mathf.Lerp(angular_acc, 0, (time-timeStopped)/1.5f);
                angularVelocity = Mathf.Lerp(angularVelocity, 0, (time - timeStopped) / 1.5f);
                velocity = (position - transform.position).magnitude / Time.deltaTime;
            }

            //float curvature = rHelpers.getCurvature(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
            //angularVelocity = map.getAngularVelocity(time);//angularVelocity = velocity * curvature;

            LeftWheelSpeed = velocity + angularVelocity * 0.27f / 2;//Mathf.Lerp(LeftWheelSpeed, (LeftWheelNewPos - LeftWheelPos).magnitude / Time.deltaTime, Time.deltaTime * 10);
            RightWheelSpeed = velocity - angularVelocity * 0.27f / 2;// Mathf.Lerp(RightWheelSpeed, (RightWheelNewPos - RightWheelPos).magnitude / Time.deltaTime, Time.deltaTime * 10);

            rightVolume = Mathf.Lerp(rightVolume, volumeBySpeed.Evaluate(RightWheelSpeed), Time.deltaTime*volumeSmooth);
            rightPitch = Mathf.Lerp(rightPitch, pitchBySpeed.Evaluate(RightWheelSpeed),Time.deltaTime*pitchSmooth);

            RightWheel.volume = rightVolume;
            RightWheel.pitch = rightPitch;

            leftVolume = Mathf.Lerp(leftVolume, volumeBySpeed.Evaluate(LeftWheelSpeed), Time.deltaTime * volumeSmooth);
            leftPitch = Mathf.Lerp(leftPitch, pitchBySpeed.Evaluate(LeftWheelSpeed), Time.deltaTime * pitchSmooth);

            LeftWheel.volume = leftVolume;
            LeftWheel.pitch = leftPitch;

            for(int i = 0; i<4; i++)
            {
                float speed = velocity - angularVelocity*0.27f/2;
                if (i > 1)
                    speed = velocity + angularVelocity * 0.27f / 2;
                wheelRotations[i] += speed/0.05f * Time.deltaTime * Mathf.Rad2Deg;
                wheels[i].localRotation = Quaternion.Euler(wheelRotations[i], 0, 0);
            }    
            //Debug.Log($"LeftWheel: {LeftWheelSpeed}  RightWheel: {RightWheelSpeed}");
            //transform.rotation = pr.rotation;
            /*if(Time.time>lastTime+1)
            {
                lastTime = Time.time;
                Debug.Log((position - lastPos).magnitude);
                lastPos = position;
            }*/
            transform.position = position;

            if (time >= map.desiredTime && !ended)
            {
                ended = true;
            }
        }


    }

    

    public robotPose getPose(float time)
    {



        int prev = Mathf.Min(poses.Length - 1, Mathf.FloorToInt(time * 10));
        int next = Mathf.Min(poses.Length-1,prev + 1);

        float frac = (time * 10) - prev;
        //Debug.Log($"{prev} {next} {frac}");
        return new robotPose(poses[prev], poses[next], frac);
    }
    [EditorCools.Button]
    public void randomizePaths()
    {
        pathFiles.Shuffle();
    }
}
[System.Serializable]
public class robotPose
{
    public UnityEngine.Vector3 pos;
    public UnityEngine.Vector3 forward;
    public float linearVelocity, angularVelocity;
    public float linearAcceleration, angularAcceleration;
    public float pathCurvature;

    public robotPose(UnityEngine.Vector3 pos, UnityEngine.Vector3 forward, float lin, float ang, float linacc, float angacc, float kappa = 0)
    {
        this.pos = pos;
        this.forward = forward;
        this.linearVelocity = lin;
        this.angularVelocity = ang;
        linearAcceleration = linacc;
        angularAcceleration = angacc;
        pathCurvature = kappa;
    }

    public robotPose(robotPose prev, robotPose next, float fraq)
    {
        this.pos = UnityEngine.Vector3.Lerp(prev.pos, next.pos, fraq);
        this.forward = UnityEngine.Vector3.Slerp(prev.forward, next.forward, fraq);
        linearVelocity = Mathf.Lerp(prev.linearVelocity, next.linearVelocity, fraq);
        angularVelocity = Mathf.Lerp(prev.angularVelocity, next.angularVelocity, fraq);
        linearAcceleration = Mathf.Lerp(prev.linearAcceleration, next.linearAcceleration, fraq);
        angularAcceleration = Mathf.Lerp(prev.angularAcceleration, next.angularAcceleration, fraq);
        pathCurvature = Mathf.Lerp(prev.pathCurvature, next.pathCurvature, fraq);
    }

    public robotPose(float linearVelocity, float angularVelocity, float linacc, float angacc, float kappa = 0)
    {
        this.linearVelocity = linearVelocity;
        this.angularVelocity = angularVelocity;
        pos = UnityEngine.Vector3.zero;
        forward = UnityEngine.Vector3.forward;
        linearAcceleration = linacc;
        angularAcceleration = angacc;
        pathCurvature = kappa;
    }

    
}
[System.Serializable]
public class PathPiece
{
    public float start;
    public float end;
    public int course;

    public PathPiece(float start, float end, int course)
    {
        this.start = start;
        this.end = end;
        this.course = course;
    }
}
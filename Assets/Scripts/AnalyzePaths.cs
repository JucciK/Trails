using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class AnalyzePaths : MonoBehaviour
{

    public FollowPath followPath;
    public RaycastVisibility visibility;
    public rMap map;
    public string path;
    public bool fail;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        followPath = FollowPath.Instance;
        visibility = RaycastVisibility.Instance;
        //StartCoroutine(analyzePaths());
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator processFile(float maxCorrelation, float maxVIF)
    {
        fail = false;
        yield return null;
        robotPose[] poses = followPath.getPath(-1);
        float[] vis = new float[poses.Length];
        float[] lin_vel = new float[poses.Length];
        float[] ang_vel = new float[poses.Length];
        float[] lin_acc = new float[poses.Length];
        float[] ang_acc = new float[poses.Length];
        float[] dist = new float[poses.Length];
        float[] kappa = new float[poses.Length];
        for (int j = 0; j < poses.Length; j++)
        {
            vis[j] = visibility.getVisibility(poses[j].pos + Vector3.up * 1.5f);
            lin_vel[j] = poses[j].linearVelocity;
            ang_vel[j] = poses[j].angularVelocity;
            lin_acc[j] = poses[j].linearAcceleration;
            ang_acc[j] = poses[j].angularAcceleration;
            dist[j] = rHelpers.getClosestPointToWall(map, poses[j].pos);
            kappa[j] = poses[j].pathCurvature;
            yield return null;
        }
        string[] names = new string[] { "lin_vel", "lin_acc", "ang_vel", "ang_acc", "dist" };
        Statistics.Stats stats = Statistics.checkValues(new float[][] { lin_vel, lin_acc, ang_vel, ang_acc, dist}, names);


        for (int i = 0; i < names.Length; i++)
        {
            Debug.Log($"lin_vel vs {names[i]} = {stats.correlationMatrix[0][i]:F8}");
        }
        int a = 0;
        foreach(float vif in stats.VIFS)
        {
            if(vif>maxVIF)
            {
                Debug.Log("VIF too high:" + $"{names[a]}:  {vif}");
                Debug.Log(stats.VIFS);
                Debug.Log(stats.correlationMatrix);
                fail = true;
                break;
            }
            a++;
        }

        if(!fail)
        {
            for (int i = 0; i < stats.correlationMatrix.Length; i++)
            {
                // Start at i + 1:
                // - skips diagonal (correlation with itself = 1)
                // - avoids checking each pair twice
                for (int j = i + 1; j < stats.correlationMatrix[i].Length; j++)
                {
                    if (Math.Abs(stats.correlationMatrix[i][j]) > maxCorrelation)
                    {
                        fail = true;
                        Debug.Log($"{names[i]} - {names[j]}: " + $"{stats.correlationMatrix[i][j]:0.000}");

                        Debug.Log(stats.VIFS);
                        Debug.Log(stats.correlationMatrix);
                        break;
                    }
                }
                if (fail)
                    break;
            }
        }
    }

    public IEnumerator analyzePaths()
    {
        yield return null;
        using (StreamWriter writer = new StreamWriter(path))
        {
            for (int i = 0; i < followPath.pathFiles.Count; i++)
            {
                string stats = followPath.pathFiles[i]+"\n";
                Debug.Log($"Path: {i}");
                robotPose[] poses = followPath.getPath(i);
                float[] vis = new float[poses.Length];
                float[] lin_vel = new float[poses.Length];
                float[] ang_vel = new float[poses.Length];
                float[] lin_acc = new float[poses.Length];
                float[] ang_acc = new float[poses.Length];
                float[] dist = new float[poses.Length];
                float[] kappa = new float[poses.Length];

                for (int j = 0; j < poses.Length; j++)
                {
                    vis[j] = visibility.getVisibility(poses[j].pos + Vector3.up * 1.5f);
                    lin_vel[j] = poses[j].linearVelocity;
                    ang_vel[j] = poses[j].angularVelocity;
                    lin_acc[j] = poses[j].linearAcceleration;
                    ang_acc[j] = poses[j].angularAcceleration;
                    dist[j] = rHelpers.getClosestPointToWall(map, poses[j].pos);
                    kappa[j] = poses[j].pathCurvature;
                    yield return null;
                }

                //stats+=Statistics.checkValues(new float[][] { lin_vel, lin_acc, ang_vel, ang_acc, dist, kappa}, new string[] { "lin_vel", "lin_acc", "ang_vel", "ang_acc", "dist", "kappa"});

                writer.Write(stats);
                writer.Flush();
                yield return null;
            }

        
        }
        
    }
}

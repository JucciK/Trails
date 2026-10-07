using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Collections;
using System;



#if UNITY_EDITOR
using UnityEditor;
#endif


public class TRAILS : MonoBehaviour
{

    bool fail = false;
    public rMap rMap;
    public SpeedCurves speedCurves;
    public MapGenerator mapGenerator;
    public FollowPath followPath;
    public AnalyzePaths analyzePaths;
    
    
    public List<int> selectedEdges = new List<int>();
    [Header("Environment Generation")]

    public int minimum_size = 100;
    
    public void GenerateMap()
    {
        mapGenerator.TotalCount = minimum_size;
        mapGenerator.Generate();
        rMap.MapFromPieces();
        rMap.CombineObstacles();
        rMap.generatePolygons();
        rMap.generateTracks();
    }




    [Header("Mapping")]
    public bool showEdgeEditing;
    public float minimum_path_length = 300;
    public int pathCount = 10;
    public void GeneratePath()
    {
#if UNITY_EDITOR
        SessionState.SetBool("GeneratePaths", true);
        EditorApplication.isPlaying = true;
#endif
    }
    

    [Header("Trajectory Parameters")]
    public float COST_straight = 2f;
    public float COST_curve = -2f;
    public float min_distance_to_walls = 0.3f;

    [Header("Speed Profile")]
    public float average_velocity = 1f;
    public float[] distinct_velocities = new float[] { 0.5f, 1f, 1.5f };
    public float[] distinct_accelerations = new float[] { 0.25f, 1.0f, 2.0f };

    public float target_time = 50f;

    public int segment_count = 2;
    public float default_acceleration = 0.5f;


    public string filePath;
    float time;


    public float maxCorrelation = 0.4f;
    public float maxVIF = 2.5f;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_EDITOR
        if(SessionState.GetBool("GeneratePaths", false))
        {
            SessionState.SetBool("GeneratePaths", false);
            rMap.pathStarts = selectedEdges;
            rMap.targetPath = filePath;
            rMap.minLength = minimum_path_length;
            rMap.desiredLength = minimum_path_length;
            StartCoroutine(generatePaths());
        }
#endif
    }

    private IEnumerator generatePaths()
    {
        Debug.Log("Starting path generation");
        rMap.STOP = false;
        rMap.freshOverride = false;
        rMap.currentMessage = "Starting";
        rMap.prepareWalls();

        string debug = $"Generating {selectedEdges.Count} trajectories\n";
        //yield return StartCoroutine(rMap.generateManyTrajectories());
        for( int i=0; i<selectedEdges.Count; i++ )
        {
            float start = time;
            yield return StartCoroutine(generateOneFully(selectedEdges[i]));
            yield return null;
            float totalTime = time - start;

            if (fail)
            {
                Debug.LogWarning($"Failed with path edge: {selectedEdges[i]}");
                debug += $"failed after {totalTime}\n";
            }
            else
                debug += $"{totalTime}\n";
        }
        Debug.Log(debug);
        yield return null;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    // Update is called once per frame
    void Update()
    {
        time = Time.time;
    }
    public bool IsEdgeSeleceted(int edgeID)
    {
        return selectedEdges.Contains(edgeID);
    }

    public void ToggleEdge(int edgeID)
    {
        if(selectedEdges.Contains(edgeID))
        {
            selectedEdges.Remove(edgeID);
        }
        else
        {
            selectedEdges.Add(edgeID);
        }
    }

    public void GenerateSpeed()
    {
        string[] csvs = GetCsvFiles(filePath);
        speedCurves.avg_speed = average_velocity;
        speedCurves.d_speeds = distinct_velocities;
        speedCurves.d_accelerations = distinct_accelerations;
        speedCurves.segment_count = segment_count;
        speedCurves.default_acceleration = default_acceleration;
        speedCurves.total_length = target_time;
        foreach(string csv in csvs)
        {
            speedCurves.generateFiles(csv, target_time);
        }
    }

    public bool generateSingleSpeed(string csv)
    {
        speedCurves.avg_speed = average_velocity;
        speedCurves.d_speeds = distinct_velocities;
        speedCurves.d_accelerations = distinct_accelerations;
        speedCurves.segment_count = segment_count;
        speedCurves.default_acceleration = default_acceleration;
        speedCurves.total_length = target_time * average_velocity;
        return speedCurves.generateFiles(csv, target_time);
    }




    public static string[] GetCsvFiles(string folderPath)
    {
        return Directory.GetFiles(
            folderPath,
            "*.csv",
            SearchOption.TopDirectoryOnly
        );
    }


    public void selectEdges()
    {
        List<int> possibleEdges = new List<int>();

        for (int i = 0; i < rMap.edges.Count; i++)
        {
            if (rMap.edges[i].isWall || rMap.edges[i].isObstacle)
                continue;
            possibleEdges.Add(i);
        }

        if(possibleEdges.Count<pathCount)
        {
            Debug.LogWarning("Not enough edges");
            return;
        }

        selectedEdges.Clear();

        for(int i=0; i<pathCount; i++)
        {
            int a = possibleEdges[UnityEngine.Random.Range(0,possibleEdges.Count)];
            selectedEdges.Add(a);
            possibleEdges.Remove(a);
        }
        
    }


    public IEnumerator generateOneFully(int p)
    {
        rMap.targetPath = filePath;
        rMap.minLength = minimum_path_length;
        rMap.desiredLength = minimum_path_length;
        string file = $"path_{p}.csv";
        string filename = System.IO.Path.Combine(filePath, file);
        fail = true;
        
        for (int tries = 0; tries<100; tries++)
        {
            if (File.Exists(filename))
                File.Delete(filename);
            yield return null;
            rMap.STOP = false;
            yield return StartCoroutine(rMap.generateSingle(p));
            if (File.Exists(filename))
            {
                bool speedFail = true;
                for(int tries2=0; tries2<100; tries++)
                {
                    yield return null;
                    try
                    {
                        if(generateSingleSpeed(filename))
                        {
                            Debug.Log("Succesfully generated speed");
                            speedFail = false;

                        }
                        else
                        {
                            Debug.Log("Failed generating speed");
                        }
                    }
                    catch (Exception e)
                    {
                        string speedFile = System.IO.Path.Combine(filePath, "speeds", file);
                        string positionFile = System.IO.Path.Combine(filePath, "positions", file);
                        if (File.Exists(speedFile))
                            File.Delete(speedFile);
                        if (File.Exists(positionFile))
                            File.Delete(positionFile);
                        Debug.LogException(e);
                    }

                    if(!speedFail)
                    {
                        Debug.Log("Analyzing");
                        followPath.loadPathFromFile(filePath, file);
                        yield return null;
                        yield return StartCoroutine(analyzePaths.processFile(maxCorrelation, maxVIF));
                        Debug.Log("Done analyzing");
                        if(!analyzePaths.fail)
                        {
                            fail = false;

                            
                            break;
                        }

                        string speedFile = System.IO.Path.Combine(filePath, "speeds", file);
                        string positionFile = System.IO.Path.Combine(filePath, "positions", file);
                        if (File.Exists(speedFile))
                            File.Delete(speedFile);
                        if (File.Exists(positionFile))
                            File.Delete(positionFile);
                    }
                }

                if(speedFail == false && fail == false)
                {
                    break;

                }


            }

        }

        if (fail)
            Debug.LogWarning("Failed to generate files!!");
        else
            Debug.Log($"Succesfully analyzed path: {p}");
    }
}

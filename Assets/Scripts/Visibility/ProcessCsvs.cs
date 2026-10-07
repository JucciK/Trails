
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ProcessCsvs : MonoBehaviour
{
    public string folderName;
    public string[] files;

    public List<Pose> poses;
    public List<float> values;

    public RaycastVisibility visibility;

    public bool useRaycast = true;
    public Rect menuRect = new Rect(0, 0, 200, 10);
    public float width = 200;
    public float height = 0;
    public float margin = 10;

    [EditorCools.Button]
    public void getCsvs()
    {
        files = Directory.GetFiles(folderName, "*.csv");
    }


    public void ProcessFile(string filename)
    {
        files = new string[1] { filename };
        StartCoroutine(processCsv(0));
    }
    
    public void LoadCsv(int id)
    {
        string path = files[id];
        if (poses == null)
            poses = new List<Pose>();
        else
            poses.Clear();
        using (var reader = new StreamReader(path))
        {
            bool firstLine = true;
            while(!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (firstLine) { firstLine = false; continue; }

                string[] values = line.Split(',');

                float time = float.Parse(values[0], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float px = float.Parse(values[1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float py = float.Parse(values[2], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float pz = float.Parse(values[3], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float rx = float.Parse(values[4], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float ry = float.Parse(values[5], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float rz = float.Parse(values[6], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                float rw = float.Parse(values[7], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
                poses.Add(new Pose(time, px, py, pz, rx, ry, rz, rw));

            }
        }

        Debug.Log($"Loaded {poses.Count} poses from file {files[id]}");

    }

    public void WriteVolumes(int id)
    {
        string path = files[id]+"_volume.csv";

        using (var writer = new StreamWriter(path))
        {
            writer.WriteLine("time; volume");
            for (int i = 0; i < poses.Count; i++)
            {
                float time = poses[i].time;
                float volume = values[i];
                writer.WriteLine($"{time};{volume}");
            }
        }
    }


    public IEnumerator processCsv(int id)
    {
        LoadCsv(id);
        yield return null;
        values.Clear();
        foreach(Pose p in poses)
        {
            transform.position = p.position;
            transform.rotation = p.rotation;
            yield return null;

            values.Add(visibility.getVisibility(p.position));

            
        }

        WriteVolumes(id);

    }

    public IEnumerator processAll()
    {
        Debug.Log("Starting processing all");
        yield return null;
        for (int i = 0; i < files.Length; i++)
        {
            Debug.Log($"Processing file NO: {i}");
            yield return processCsv(i);
        }
    }

    [EditorCools.Button]
    public void Process()
    {
        StartCoroutine(processAll());
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //    Process();
    }

    /*private void OnGUI()
    {
        menuRect.width = width;
        menuRect.height = height;
        GUI.Window(0, menuRect, menuFunction, "Menu");
        height = 0;
    }*/


    public void menuFunction(int id)
    {
        GUI.enabled = true;
        pad(50);
        if (GUI.Button(getRect(30), "Start"))
        {
            Debug.Log("Starting");
            Process();
        }
        pad(30);
    }
    public void pad(float h = 10)
    {
        height += h;
    }
    public Rect getRect(float h, float w = -1, float x = -1, bool add = true)

    {
        if (x == -1)
            x = margin;
        if (w == -1)
            w = width - margin - x;
        Rect rect = new Rect(x, height, w, h);
        if (add)
            height += h;
        return rect;
    }
}
[System.Serializable]
public class Pose
{
    public float time;
    public Vector3 position;
    public Quaternion rotation;

    public Pose(float time, float px, float py, float pz, float rx, float ry, float rz, float rw)
    {
        this.time = time;
        this.position = new Vector3(px, py, pz);
        this.rotation = new Quaternion(rx, ry, rz, rw);
    }
}

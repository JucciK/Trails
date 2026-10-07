using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Accord.Math.Distances;
using UnityEngine;
using Accord.Math.Optimization;
using Accord.Math;
using UnityEditor;
using System.IO;
using UnityEngine.UIElements;

public class rMap : MonoBehaviour
{
    public bool STOP = false;
    public bool drawHere = false;

    public bool drawOnlyPath = false;
    public bool drawOnlyTrajectory = false;
    public List<rEdge> edges = new List<rEdge>();
    public List<rPolygon> polygons = new List<rPolygon>();
    public List<rTrack> tracks = new List<rTrack>();
    public List<int> path = new List<int>();
    public List<UnityEngine.Vector3> trajectory = new List<UnityEngine.Vector3>();
    public Dictionary<UnityEngine.Vector3Int, List<int>> wallsByGrid = new Dictionary<UnityEngine.Vector3Int, List<int>>();
    public Dictionary<UnityEngine.Vector3Int, List<int>> obstaclesByGrid = new Dictionary<UnityEngine.Vector3Int, List<int>>();
    public float totalTime;


    public int backTrackingCooldown = 5;
    public int pathLength = 50;
    public float minLength = 100;
    public float minimumDistanceToWall = 0.3f;
    public float distanceRandom = 2f;
    public float straightnessCost = 2f;
    public float curvynessCost = -1f;

    
    public static UnityEngine.Vector3Int[] around = new UnityEngine.Vector3Int[] { new UnityEngine.Vector3Int(-1, 0, -1), new UnityEngine.Vector3Int(0, 0, -1), new UnityEngine.Vector3Int(1, 0, -1) , new UnityEngine.Vector3Int(-1, 0, 0), new UnityEngine.Vector3Int(0, 0, 0), new UnityEngine.Vector3Int(1, 0, 0), new UnityEngine.Vector3Int(-1, 0, 1), new UnityEngine.Vector3Int(0, 0, 1), new UnityEngine.Vector3Int(1, 0, 1) };

    public string targetPath;


    public float bezierLength;

    public string currentMessage = "";
    public List<Bezier> beziers = new List<Bezier>();
    [System.Serializable]
    public class BezierList
    {
        public List<Bezier> Beziers;

        public BezierList()
        {
            Beziers = new List<Bezier>();
        }

        public BezierList(List<Bezier> beziers)
        {
            Beziers = new List<Bezier>();
            foreach (Bezier b in beziers)
                Beziers.Add(new Bezier(b));
        }

        public static implicit operator BezierList(List<Bezier> list)
        {
            return new BezierList(list);
        }

        public static implicit operator List<Bezier> (BezierList list)
            { return list.Beziers; }

        
    }




    bool optimizerSuccessfull = false;



    public double[] initialGuess;
    public bool freshOverride = false;
    public List<int> pathStarts = new List<int>();

    public List<string> pathFiles = new List<string>();
    public string pathPath = "C:/Users/jkalliok/AppData/LocalLow/Ubicomp/Sickness/18_12_25_paths/";


    public float startTime = 0;
    public List<float> times = new List<float>();



    public void startMany()
    {
        STOP = false;
        freshOverride = false;
        currentMessage = "Starting generation";
        prepareWalls();
        StartCoroutine(generateManyTrajectories());
    }
    public IEnumerator generateManyTrajectories()
    {
        yield return null;
        currentMessage = $"preparing walls";
        prepareWalls();
        times.Clear();


            foreach (int p in pathStarts)
            {
            Debug.Log(p);
            float startTime = Time.time;
            if (STOP)
            {
                Debug.Log("Stop enabled");
                break;
            }
                Debug.Log("Doing this");
                string filename = System.IO.Path.Combine(targetPath, $"path_{p}.csv");
                if (File.Exists(filename))
                {
                    currentMessage = $"{p}: path file already exists";
                Debug.Log(currentMessage);
                    continue;
                }
                currentMessage = $"{p}: generating path";
                for (int tryout = 0; tryout < 10; tryout++)
                {

                    if (generatePath(p))
                    {
                        bigFail = false;
                        currentMessage = $"{p}: optimizing bezier. Try out no:{tryout}";
                        yield return optimizeBezier();
                        if (bigFail)
                            continue;



                        currentMessage = $"{p}: saving to file";
                        if (!bigFail)
                            {
                                totalTime = Time.realtimeSinceStartup-startTime;
                                saveBezierToFile(filename);
                        break;
                            }

                        currentMessage = $"{p}: waiting for next";
                        Debug.Log("Here");

                    }
                    else
                    {
                        Debug.Log($"{p}: Failed to generate path");
                    }
                }

            yield return null;
            float total = Time.time - startTime;
            times.Add(total);
                Debug.Log($"{p}: Going for next one");
            }

        Debug.Log("Times taken generating:");
            foreach(float time in times)
        {
            Debug.Log(time);
        }
        }




    public IEnumerator generateSingle(int p)
    {
        prepareWalls();
        Debug.Log("Doing this");
        string filename = System.IO.Path.Combine(targetPath, $"path_{p}.csv");
        if (File.Exists(filename))
        {
            File.Delete(filename);
        }
        bool failed = true;
        currentMessage = $"{p}: generating path";
        for (int tryout = 0; tryout < 10; tryout++)
        {

            if (generatePath(p))
            {
                bigFail = false;
                currentMessage = $"{p}: optimizing bezier. Try out no:{tryout}";
                yield return optimizeBezier();
                if (bigFail)
                    continue;



                currentMessage = $"{p}: saving to file";
                if (!bigFail)
                {
                    totalTime = Time.realtimeSinceStartup - startTime;
                    saveBezierToFile(filename);
                    failed = false;
                    break;
                }
            }
            else
            {
                Debug.Log($"{p}: Failed to generate path");
            }
        }
        if (failed && File.Exists(filename))
            File.Delete(filename);
        yield return null;
    }


    
    public void saveBezierToFile(string path)
    {
        using(StreamWriter sw = new StreamWriter(path))
        {
            foreach(Bezier b in beziers)
            {
                sw.WriteLine(b.ToString());
            }
        }
    }

    public void loadBezierFromFile(string path)
    {
        beziers.Clear();
        using(StreamReader streamReader = new StreamReader(path))
        {
            string line;
            while((line = streamReader.ReadLine()) != null)
            {

            }
        }
    }

    
    public struct BezierPoint
    {
        public Bezier b;
        public float t;

        public BezierPoint(Bezier b, float t)
        {
            this.b = b;
            this.t = t;
        }
    }

    public BezierPoint getPointByDistance(float distance)
    {
        foreach(Bezier b in beziers)
        {
            if(b.length>distance)
            {
                float point = b.pointByDistance(distance);
                return new BezierPoint(b, point);
            }
            distance-=b.length;
        }
        return new BezierPoint(beziers.Last(),1);
    }
    public bool bigFail = false;
    public IEnumerator optimizeBezier()
    {
        bigFail = false;
        STOP = false;
        yield return null;
        Queue<rEdge> originalEdges = new Queue<rEdge>();

        for(int i = 0; i<path.Count;i++)
        {
            originalEdges.Enqueue(edges[path[i]]);
        }

        Queue<List<rEdge>> splits = new Queue<List<rEdge>>();
        splits.Enqueue(new List<rEdge>());
        while(originalEdges.Count>0)
        {
            rEdge edge = originalEdges.Dequeue();
            splits.Last().Add(edge);
            if(splits.Last().Count == maxPiecesPerOptimization && originalEdges.Count>0)
            {
                splits.Enqueue(new List<rEdge>());
                splits.Last().Add(edge);
            }
        }

        float distanceToAdd = distanceRandom;
        float distanceRemaining = desiredLength;
        UnityEngine.Vector3 startPosition = edges[path[0]].middle;
        rPolygon first = polygons[getTrack(path[0], path[1]).getPolygon()];
        UnityEngine.Vector3 C1 = rHelpers.getNormal(edges[path[0]], first) * (minControlLength+maxControlLength)/2;

        beziers.Clear();
        List<float> moreOrLess = new List<float>();
        int mid = Mathf.RoundToInt(splits.Count/4);
        for(int i=0; i< Mathf.CeilToInt(splits.Count / 2)+1; i++)
        {
            if (i < mid)
            {
                moreOrLess.Add(1);
                moreOrLess.Add(0.5f);
            }
            else
            {
                moreOrLess.Add(-1);
                moreOrLess.Add(-0.5f);

            }
        }
        moreOrLess.Shuffle();
        Queue<float> signs = new Queue<float>(moreOrLess);
        while(splits.Count>0)
        {
            currentMessage = $"Splits left: {splits.Count} last optimization was: {optimizerSuccessfull}";
            if (STOP)
                break;
            float length = distanceRemaining;
            float sign = signs.Dequeue();
            /*if (splits.Count>1)
            {
                length = distanceRemaining / splits.Count+distanceToAdd*sign;
            }*/
            List<rEdge> tr = splits.Dequeue();
            length = getSplitLength(tr) + distanceToAdd * sign;
            int tries = 0;
            bool fresh = true;
            float CurvynessMult = straightnessCost;
            if (sign > 0)
                CurvynessMult = curvynessCost;
            for(tries =0; tries<26; tries++)
            {

                if (STOP)
                    break;
                optimizerSuccessfull = false;
                yield return OptimizeSplit(tr, startPosition, C1, length,10000,true, 20, CurvynessMult);
                fresh = false;
                if(optimizerSuccessfull)
                {
                    Debug.Log("Success");
                    currentMessage = "Success";
                    startPosition = beziers.Last().end;
                    C1 = (startPosition - beziers.Last().C2);
                    distanceRemaining -= bezierLength;
#if UNITY_EDITOR
                    SceneView.lastActiveSceneView.LookAt(startPosition);
#endif
                    yield return null;
                    break;
                }
                else
                {
                    currentMessage = "Fail";
                    Debug.Log("Fail!!");
                    signs.Enqueue(sign);
                    sign = signs.Dequeue();
                    length = getSplitLength(tr) + distanceToAdd * sign;
                }
                yield return null;
            }
            if(tries>=10)
            {
                currentMessage = "Big fail";
                Debug.Log("Big fail");
                bigFail = true;
                break;
            }
        }


    }

    public float getSplitLength(List<rEdge> split)
    {
        UnityEngine.Vector3 pos = split[0].middle;
        float len = 0;
        for(int i=1; i<split.Count; i++)
        {
            UnityEngine.Vector3 newPos = split[i].middle;
            len += (newPos - pos).magnitude;
            pos = newPos;
        }
        return len;
    }
    /*public UnityEngine.Vector3 pointByDistance(float distance, int resolution = 100)
    {
        UnityEngine.Vector3 previousPoint
        for(int i=0; i<resolution; i++)
        {

        }
    }*/



    public IEnumerator OptimizeSplit(List<rEdge> split, UnityEngine.Vector3 startPosition, UnityEngine.Vector3 C1, float desiredLength, int iterations, bool fresh = true, int resolution=20, float CurvaturePenalty = 1)
    {
        yield return null;
        if(freshOverride)
        {
            fresh = true;
            freshOverride = false;
        }    
        int edgeCount = split.Count;

        UnityEngine.Vector3[] midPoints = new UnityEngine.Vector3[edgeCount];
        UnityEngine.Vector3[] normals = new UnityEngine.Vector3[edgeCount];
        UnityEngine.Vector3[] tangents = new UnityEngine.Vector3[edgeCount];
        int tries = 0;
        NonlinearConstraint[] constraints = new NonlinearConstraint[(edgeCount - 1) * 3];
        rPolygon[] polys = new rPolygon[edgeCount];
        
        initialGuess = new double[(edgeCount - 1) * 3];
        for (int i = 0; i < edgeCount; i++)
        {
            rEdge e1 = split[i];
            rEdge e2;
            if (i == edgeCount - 1)
                e2 = split[i - 1];
            else
                e2 = split[i + 1];
            rTrack track = getTrack(e1, e2);
            polys[i] = polygons[track.getPolygon()];

            midPoints[i] = e1.middle;
            normals[i] = rHelpers.getNormal(e1, polys[i]);
            if(i == edgeCount - 1)
                normals[i] = -normals[i];
            tangents[i] = (e1.end - e1.start)*0.4f;

            int index = i;
            float width = (e1.end-e1.start).magnitude * 0.4f;
            float widthsqr = width * width;
            if(i>0)
            {
                int varIndex = (i - 1) * 3;
                constraints[varIndex] = new NonlinearConstraint(initialGuess.Length, vars=> vars[varIndex] * vars[varIndex], ConstraintType.LesserThanOrEqualTo, 1);
                constraints[varIndex + 1] = new NonlinearConstraint(initialGuess.Length, vars => vars[varIndex + 1]* vars[varIndex + 1], ConstraintType.LesserThanOrEqualTo,1);
                constraints[varIndex + 2] = new NonlinearConstraint(initialGuess.Length, vars => -vars[varIndex + 2]*(1- vars[varIndex + 2]), ConstraintType.LesserThanOrEqualTo);
                
                initialGuess[varIndex] = 0;// Random.Range(-1, 1);
                initialGuess[varIndex + 1] = 0;
                initialGuess[varIndex + 2] = 0.5;
                
            }
        }

        UnityEngine.Vector3[] mids = new UnityEngine.Vector3[midPoints.Length];
        UnityEngine.Vector3[] cs = new UnityEngine.Vector3[midPoints.Length];

        System.Func<double[], double> objective = var =>
        {
            mids[0] = startPosition;
            cs[0] = C1;
            for (int i = 1; i < mids.Length; i++)
            {
                mids[i] = midPoints[i] + tangents[i] * (float)var[(i - 1) * 3];
                //Debug.Log(var[(i - 1) * 3 + 1]);
                if(i<mids.Length-1)
                    cs[i] = rHelpers.rotate(normals[i], (float)var[(i - 1) * 3 + 1] * Mathf.Rad2Deg) * (minControlLength + (float)var[(i - 1) * 3 + 2] * (maxControlLength - minControlLength));
                else
                    cs[i] = normals[i] * (minControlLength + (float)var[(i - 1) * 3 + 2] * (maxControlLength - minControlLength));

            }

            bool dbk = false;
            tries++;
            if(tries%100 == 0)
            {
                dbk = true;
                tries = 0;
            }
            rHelpers.TrajectoryStats trajectory = rHelpers.StatBezier(mids, cs, polys, this,dbk);
            float y = trajectory.yawChanges / (edgeCount - 1);
            float l = Mathf.Pow(trajectory.length - desiredLength, 2);
            if(tries == 0)
                Debug.Log("Yaws: " + y + " Target Length: "+desiredLength + " Final length: "+trajectory.length + " Max yaws: "+ trajectory.maxYaws + " MinDistances: " +trajectory.minDistances);
            
            return l*LengthScale + trajectory.outsidePoints * 100000 + trajectory.maxYaws * 10000 + trajectory.minDistances * 100000 + CurvaturePenalty*y;
        };

        NonlinearObjectiveFunction f = new NonlinearObjectiveFunction((edgeCount - 1) * 3, objective);
        var optimizer = new Cobyla(f, constraints);
        optimizer.MaxIterations = iterations;
        yield return null;
        bool success = optimizer.Minimize(initialGuess);
        yield return null;
        optimizerSuccessfull = false;
        if (success)
        {
            mids[0] = startPosition;
            cs[0] = C1;
            for (int i = 1; i < mids.Length; i++)
            {
                mids[i] = midPoints[i] + tangents[i] * (float)optimizer.Solution[(i - 1) * 3];
                if (i < mids.Length - 1)
                    cs[i] = rHelpers.rotate(normals[i], (float)optimizer.Solution[(i - 1) * 3 + 1] * Mathf.Rad2Deg) * (minControlLength + (float)optimizer.Solution[(i - 1) * 3 + 2] * (maxControlLength - minControlLength));
                    
                else
                    cs[i] = normals[i] * (minControlLength + (float)optimizer.Solution[(i - 1) * 3 + 2] * (maxControlLength - minControlLength));
            }

            rHelpers.TrajectoryStats trajectory = rHelpers.StatBezier(mids, cs, polys, this);
            if (trajectory.maxYaws<1 && trajectory.outsidePoints == 0)
            {
                bezierLength = 0;
                UnityEngine.Vector3 start = mids[0];
                optimizerSuccessfull = true;
                for (int i=0; i<edgeCount-1; i++)
                {
                    beziers.Add(new Bezier(mids[i], mids[i] + cs[i], mids[i + 1] - cs[i + 1], mids[i + 1]));

                    /*for(int j =1; j<21; j++)
                    {
                        UnityEngine.Vector3 p = rHelpers.CubicBezier(mids[i], mids[i] + cs[i], mids[i + 1] - cs[i + 1], mids[i + 1],j/20);
                        bezierLength += (p-start).magnitude;
                        start = p;
                    }*/
                }

            }
            else
            {
                Debug.Log($"Yaws: {trajectory.maxYaws} Distances: {trajectory.minDistances} \tOutside points: {trajectory.outsidePoints}");
                
                optimizerSuccessfull = false;
                freshOverride = true;
            }
                
            
        }
        else
        {
            Debug.Log(optimizer.Status);
        }
        
    }

    


    public float desiredLength;
    public float desiredTime;
    public float minControlLength;
    public float maxControlLength;
    public float maxYaw = 20;
    public bool stop;
    public int maxPiecesPerOptimization = 30;



    [Space(25f)]
    public float LengthScale;
    public float YawScale;

    public bool generating;
    // Start is called before the first frame update
    void Start()
    {
        prepareWalls();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    public class PosRot
    {
        public UnityEngine.Vector3 position;
        public Quaternion rotation;

        public PosRot(UnityEngine.Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    public List<int> getConnectedEdges(int edge)
    {
        List<int> newEdges = new List<int>();
        int current = edge;
        bool found = true;
        int side = 1;
        while (found)
        {
            found = false;
            float smallest = 9999;
            int smallestID = -1;
            int smallestEnd = 0;
            for(int i=0; i<edge; i++)
            {
                if (i == current)
                    continue;
                UnityEngine.Vector3 v = side * edges[current].end - edges[current].start;

                float cross = 99999;
                int end = 0;
                if (side == 1)
                    end = edges[current].shareEnd(edges[i]);
                else
                    end = edges[current].shareStart(edges[i]);

                if (end == 1)
                    cross = UnityEngine.Vector3.Cross(v, edges[i].end - edges[i].start).y;
                else if(end == -1)
                    cross = UnityEngine.Vector3.Cross(v, edges[i].start - edges[i].end).y;
                if(cross<smallest)
                {
                    smallest = cross;
                    smallestID = i;
                    smallestEnd = end;
                }
            }

            if (smallestID == -1)
                return newEdges;
            if(newEdges.Contains(smallestID)) return newEdges;
            if(smallestID==edge) return newEdges;

            newEdges.Add(smallestID);
            Debug.Log(smallestID);
            current = smallestID;
            side = smallestEnd;
            found = true;
        }
        return newEdges;
    }

    public int getConnectedEdge(int edge)
    {
        float smallest = 9999;
        int smallestID = -1;
        for (int i = 0; i < edges.Count; i++)
        {
            if (i == edge)
                continue;

            int end = edges[edge].shareEnd(edges[i]);

            float cross = 99999;
            if(end == 1)
            {
                cross = UnityEngine.Vector3.Cross(edges[edge].end - edges[edge].start, edges[i].end - edges[i].start).y;
            }
            else if(end == -1)
            {
                cross = UnityEngine.Vector3.Cross(edges[edge].end - edges[edge].start, edges[i].start - edges[i].end).y;
            }

            if(cross<smallest)
            {
                smallest = cross;
                smallestID = i;
            }
        }
        return smallestID;
    }
    
    public void prepareWalls()
    {
        for(int i=0; i<edges.Count; i++)
        {
            if (edges[i].isWall)
            {
                UnityEngine.Vector3Int pos = MapHelpers.PositionToGrid(edges[i].middle, 5);
                if (wallsByGrid.Keys.Contains(pos))
                {
                    wallsByGrid[pos].Add(i);
                }
                else
                {
                    wallsByGrid[pos] = new List<int> { i };
                }
            }
            if (edges[i].isObstacle)
            {
                UnityEngine.Vector3Int pos = MapHelpers.PositionToGrid(edges[i].middle, 5);
                if (obstaclesByGrid.Keys.Contains(pos))
                {
                    obstaclesByGrid[pos].Add(i);
                }
                else
                {
                    obstaclesByGrid[pos] = new List<int> { i };
                }
            }
        }
    }

    public void getWallsAround(UnityEngine.Vector3 pos, ref List<int> walls, bool includeObstacles = false)
    {
        walls.Clear();
        UnityEngine.Vector3Int mid = MapHelpers.PositionToGrid(pos,5);
        foreach(UnityEngine.Vector3Int v in around)
        {
            if (wallsByGrid.Keys.Contains(mid + v))
                walls.AddRange(wallsByGrid[mid + v]);
            if (includeObstacles && obstaclesByGrid.Keys.Contains(mid + v))
                walls.AddRange(obstaclesByGrid[mid + v]);

        }
        
    }
    
    private class NodeEqualityComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node t1, Node t2)
        {
            if (ReferenceEquals(t1, t2))
                return true;
            return t1.edge == t2.edge;
        }

        public int GetHashCode(Node t) => t.edge;
    }

    public bool generatePath(int startEdge)
    {
        try
        {
            List<Node> nodes = new List<Node>();

            HashSet<int> visitedTracks = new HashSet<int>();

            Stack<Node> stack = new Stack<Node>();
            int[] original = new int[backTrackingCooldown];
            for (int i = 0; i < backTrackingCooldown; i++)
                original[i] = -1;
            Node start = new Node(startEdge, original, -1);
            stack.Push(start);
            nodes.Add(start);

            float length = 0;

            while (stack.Count > 0 && length < minLength)
            {
                Node currentNode = stack.Peek();

                List<Node> neighbors = getChildren(currentNode, visitedTracks);

                if (neighbors.Count == 0)
                {
                    stack.Pop();
                    nodes.RemoveAt(nodes.Count - 1);
                }
                else
                {
                    Node nextNode = neighbors[Random.Range(0, neighbors.Count)];
                    visitedTracks.Add(nextNode.track);
                    stack.Push(nextNode);
                    nodes.Add(nextNode);
                }

                length = calculateLength(nodes);

            }
            if (nodes.Count < 0)
                return false;
            pathFromNodes(nodes);
            GetPathLength();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e.StackTrace);
            return false;
        }
        
        
    }

    public float calculateLength(List<Node> nodes)
    {
        float len = 0;
        UnityEngine.Vector3 prev = edges[nodes[0].edge].middle;
        for(int i=1; i<nodes.Count; i++)
        {
            UnityEngine.Vector3 next = edges[nodes[i].edge].middle;
            len += (next - prev).magnitude;
            prev = next;
        }
        return len;
    }

    public void generatePolygons()
    {
        Debug.Log("Starting generation");
        polygonGenerator();
    }

    /*public void polygonGenerator()
    {
        List<int> polygonEdges = new List<int>();
        foreach (Piece light in FindObjectsByType(typeof(Piece), FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            polygonEdges.Clear();
            UnityEngine.Vector3 lightPos = light.transform.position;
            for (int i = 0; i < edges.Count; i++)
            {
                UnityEngine.Vector3 mid = edges[i].middle;
                if ((lightPos - mid).sqrMagnitude > 30)
                    continue;
                bool blocked = false;
                for (int j = 0; j < edges.Count; j++)
                {
                    if (i == j)
                        continue;

                    if (rHelpers.doIntersect(lightPos, mid, edges[j].start, edges[j].end))
                    {
                        blocked = true;
                        break;
                    }

                }

                if (!blocked)
                {
                    polygonEdges.Add(i);
                }
            }
            if (polygonEdges.Count > 0)
                addPolygon(polygonEdges.ToArray());
        }
        polygonEdges.Clear();
        Debug.Log("DONE GENERATING");
    }*/

    public void polygonGenerator()
    {
        List<int> polygonEdges = new List<int>();

        foreach (Piece light in FindObjectsByType(
            typeof(Piece),
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None))
        {
            polygonEdges.Clear();

            UnityEngine.Vector3 lightPos = light.transform.position;

            for (int i = 0; i < edges.Count; i++)
            {
                UnityEngine.Vector3 mid = edges[i].middle;

                if ((lightPos - mid).sqrMagnitude > 30)
                    continue;

                bool blocked = false;

                for (int j = 0; j < edges.Count; j++)
                {
                    if (i == j)
                        continue;

                    if (rHelpers.doIntersect(
                        lightPos,
                        mid,
                        edges[j].start,
                        edges[j].end))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    polygonEdges.Add(i);
                }
            }

            // Fix all one-edge gaps.
            FillSingleEdgeGaps(polygonEdges);

            if (polygonEdges.Count > 0)
                addPolygon(polygonEdges.ToArray());
        }

        polygonEdges.Clear();

        Debug.Log("DONE GENERATING");
    }

    private void FillSingleEdgeGaps(
    List<int> polygonEdges,
    float tolerance = 0.001f)
    {
        bool addedEdge;

        do
        {
            addedEdge = false;

            // Find all endpoints that currently have only
            // one selected polygon edge connected to them.
            List<UnityEngine.Vector3> openEnds =
                FindOpenEnds(polygonEdges, tolerance);

            // Look through edges that weren't detected by
            // the visibility test.
            for (int i = 0; i < edges.Count; i++)
            {
                if (polygonEdges.Contains(i))
                    continue;

                rEdge candidate = edges[i];

                bool startIsOpen =
                    ContainsPoint(
                        openEnds,
                        candidate.start,
                        tolerance);

                bool endIsOpen =
                    ContainsPoint(
                        openEnds,
                        candidate.end,
                        tolerance);

                // A missing edge must connect TWO open ends.
                if (startIsOpen && endIsOpen)
                {
                    polygonEdges.Add(i);

                    Debug.Log(
                        $"Added missing polygon edge {i}");

                    addedEdge = true;

                    // Recalculate the open endpoints after
                    // changing the polygon.
                    break;
                }
            }

        } while (addedEdge);
    }

    private List<UnityEngine.Vector3> FindOpenEnds(
    List<int> polygonEdges,
    float tolerance)
    {
        List<UnityEngine.Vector3> openEnds =
            new List<UnityEngine.Vector3>();

        foreach (int edgeIndex in polygonEdges)
        {
            rEdge edge = edges[edgeIndex];

            if (CountConnections(
                    edge.start,
                    polygonEdges,
                    tolerance) == 1)
            {
                AddUniquePoint(
                    openEnds,
                    edge.start,
                    tolerance);
            }

            if (CountConnections(
                    edge.end,
                    polygonEdges,
                    tolerance) == 1)
            {
                AddUniquePoint(
                    openEnds,
                    edge.end,
                    tolerance);
            }
        }

        return openEnds;
    }


    private int CountConnections(
    UnityEngine.Vector3 point,
    List<int> polygonEdges,
    float tolerance)
    {
        int count = 0;

        foreach (int edgeIndex in polygonEdges)
        {
            rEdge edge = edges[edgeIndex];

            if (SamePoint(point, edge.start, tolerance) ||
                SamePoint(point, edge.end, tolerance))
            {
                count++;
            }
        }

        return count;
    }


    private bool ContainsPoint(
        List<UnityEngine.Vector3> points,
        UnityEngine.Vector3 point,
        float tolerance)
    {
        foreach (UnityEngine.Vector3 p in points)
        {
            if (SamePoint(p, point, tolerance))
                return true;
        }

        return false;
    }


    


    private bool SamePoint(
        UnityEngine.Vector3 a,
        UnityEngine.Vector3 b,
        float tolerance)
    {
        return (a - b).sqrMagnitude <= tolerance * tolerance;
    }

    public void GetPathLength()
    {
        float len = 0;
        Debug.Log(path.Count);
        UnityEngine.Vector3 previous = edges[path[0]].middle;
        for(int i=1; i<path.Count; i++)
        {
            UnityEngine.Vector3 next = edges[path[i]].middle;
            len += (next - previous).magnitude;
            previous = next;
        }
        Debug.Log($"Length: {len}");
    }
    public void GetTrajectoryLength()
    {
        float len = 0;
        UnityEngine.Vector3 previous = trajectory[0];
        for (int i = 1; i < trajectory.Count; i++)
        {
            UnityEngine.Vector3 next = trajectory[i];
            len += (next - previous).magnitude;
            previous = next;
        }
        Debug.Log($"Length: {len}");
    }

    public List<Node> getChildren(Node n, HashSet<int> visited)
    {
        List<Node> children = new List<Node>();
        for(int i =0; i<tracks.Count; i++)
        {
            if (visited.Contains(i))
                continue;
            rTrack t = tracks[i];
            int p = t.getPolygon();
            if (n.polygon.Contains(p))
                continue;
            if(t.start == n.edge)
            {

                children.Add(new Node(t.end, shift(n.polygon,p),i));
            }
            else if(t.end == n.edge)
            {
                children.Add(new Node(t.start, shift(n.polygon, p), i));
            }
        }
        return children;
    }

    public void pathFromNodes(List<Node> nodes)
    {
        path.Clear();
        foreach(Node n in nodes)
        {
            path.Add(n.edge);
        }

    }

    public int[] shift(int[] original, int i)
    {
        int[] toReturn = new int[original.Length];
        for(int j=0; j<original.Length-1; j++)
        {
            toReturn[j + 1] = original[j];
        }
        toReturn[0] = i;
        return toReturn;
    }

    public class Node
    {
        public int edge;
        public int[] polygon;
        public int track;

        public Node(int t, int[] p, int tr)
        {
            edge = t;
            polygon = p;
            track = tr;
        }
    }
    public void MapFromMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        int[] tris = mesh.GetTriangles(0);
        List<UnityEngine.Vector3> vertices = new List<UnityEngine.Vector3>();
        mesh.GetVertices(vertices);
        edges.Clear();
        polygons.Clear();
        tracks.Clear();

        for(int i=0; i<tris.Length; i+=3)
        {
            addEdge(fix(vertices[tris[i + 0]]), fix(vertices[tris[i + 1]]));
            addEdge(fix(vertices[tris[i + 1]]), fix(vertices[tris[i + 2]]));
            addEdge(fix(vertices[tris[i + 0]]), fix(vertices[tris[i + 2]]));
        }

    }

    private List<GameObject> AllChilds(GameObject root)
    {
        List<GameObject> result = new List<GameObject>();
        if (root.transform.childCount > 0)
        {
            foreach (Transform VARIABLE in root.transform)
            {
                Searcher(result, VARIABLE.gameObject);
            }
        }
        return result;
    }

    private void Searcher(List<GameObject> list, GameObject root)
    {
        list.Add(root);
        if (root.transform.childCount > 0)
        {
            foreach (Transform VARIABLE in root.transform)
            {
                Searcher(list, VARIABLE.gameObject);
            }
        }
    }

    public void MapFromPieces()
    {
        List<GameObject> objs = AllChilds(this.gameObject);
        List<Piece> pieces = new List<Piece>();

        foreach(GameObject obj in objs)
        {
            Piece p;
            bool success = obj.TryGetComponent<Piece>(out p);
            if(success)
            {
                pieces.Add(p);
            }
        }
        edges.Clear();
        polygons.Clear();
        tracks.Clear();
        List<int> polygonEdges = new List<int>();
        foreach (Piece p in pieces)
        {
            polygonEdges.Clear();
            int i = edges.Count();
            foreach(rEdge e in p.edgesInWorld())
            {
                rEdge found = addEdge(e.start, e.end, e.isWall);
                if(!(found is null))
                    polygonEdges.Add(edges.IndexOf(found));
            }
            if(polygonEdges.Count>0)
            {
                //addPolygon(polygonEdges.ToArray());
            }
            
            ExtraBoundaries[] extraBoundaries = p.transform.GetComponentsInChildren<ExtraBoundaries>();
            
            foreach(ExtraBoundaries eB in extraBoundaries)
            {
                foreach(rEdge e in eB.obstacleEdges)
                {
                    UnityEngine.Vector3 start = eB.transform.parent.TransformPoint(e.start);
                    UnityEngine.Vector3 end = eB.transform.parent.TransformPoint(e.end);
                    addEdge(start, end, false, true, true);

                }
            }

        }
    }

    public void MapFromChildren()
    {
        List<GameObject> objs = AllChilds(this.gameObject);
        List<Mesh> meshes = new List<Mesh>();
        List<int> submeshI = new List<int>();
        List<Transform> transforms = new List<Transform>();

        foreach(GameObject obj in objs)
        {
            MeshRenderer renderer;
            bool success = obj.TryGetComponent<MeshRenderer>(out renderer);
            if(success)
            {
                for(int i=0; i< renderer.materials.Length; i++)
                {
                    if(renderer.materials[i].name == "Floor (Instance)")
                    {
                        meshes.Add(obj.GetComponent<MeshFilter>().sharedMesh);
                        submeshI.Add(i);
                        Debug.Log(obj.transform.localToWorldMatrix);
                        transforms.Add(obj.transform);
                    }
                }

            }
        }
        edges.Clear();
        polygons.Clear();
        tracks.Clear();
        List<UnityEngine.Vector3> vertices = new List<UnityEngine.Vector3>();
        for (int i=0; i<meshes.Count; i++)
        {
            vertices.Clear();
            int[] tris = meshes[i].GetTriangles(submeshI[i]);
            meshes[i].GetVertices(vertices);
            Transform trans = transforms[i];
            
            for (int j = 0; j < tris.Length; j += 3)
            {
                addEdge(fix(trans.TransformPoint(vertices[tris[j + 0]])), fix(trans.TransformPoint(vertices[tris[j + 1]])));
                addEdge(fix(trans.TransformPoint(vertices[tris[j + 1]])), fix(trans.TransformPoint(vertices[tris[j + 2]])));
                addEdge(fix(trans.TransformPoint(vertices[tris[j + 0]])), fix(trans.TransformPoint(vertices[tris[j + 2]])));
            }
        }
    }

    public UnityEngine.Vector3 fix(UnityEngine.Vector3 v)
    {

        return v;// new UnityEngine.Vector3(v.x, 0, -v.y) * 100;
    }

    public rEdge addEdge(UnityEngine.Vector3 start, UnityEngine.Vector3 end, bool isWall=false, bool allowDiag = false, bool isObstacle=false)
    {
        rEdge edge = new rEdge(start, end, isWall);
        edge.isObstacle = isObstacle;
        foreach(rEdge e in edges)
        {
            if (e.isSame(edge))
            {
                e.isWall = e.isWall || isWall;
                e.isObstacle = e.isObstacle || isObstacle;
                Debug.Log("Already exists");
                return e;
            }
        }

        if(Mathf.Abs(start.x-end.x)<0.1f || Mathf.Abs(start.z - end.z) < 0.1f || allowDiag)
        {
        Debug.Log("Added edge");
        edges.Add(edge);
            return edge;
        }
        return null;
    }

    public void moveEdge(int i, List<int> visibleEdges, UnityEngine.Vector3 offset)
    {

        for (int ii = 0; ii < edges.Count; ii++)
        {
            if (ii == i)
                continue;
            int e = edges[i].shareStart(edges[ii]);
            if (e == 1)
            {
                edges[ii].start += offset;
            }
            else if (e == -1)
            {
                edges[ii].end += offset;
            }

            e = edges[i].shareEnd(edges[ii]);
            if (e == 1)
            {
                edges[ii].start += offset;
            }
            else if (e == -1)
            {
                edges[ii].end += offset;
            }
        }

        edges[i].start += offset;
        edges[i].end += offset;
    }

    public void moveEdgeStart(int i, List<int> visibleEdges, UnityEngine.Vector3 offset)
    {
        for (int ii = 0; ii < edges.Count; ii++)
        {
            if (ii == i)
                continue;
            int e = edges[i].shareStart(edges[ii]);
            if (e == 1)
            {
                edges[ii].start += offset;
            }
            else if (e == -1)
            {
                edges[ii].end += offset;
            }
        }
        edges[i].start += offset;
    }

    public void moveEdgeEnd(int i, List<int> visibleEdges, UnityEngine.Vector3 offset)
    {
        for (int ii = 0; ii < edges.Count; ii++)
        {
            if (ii == i)
                continue;
            int e = edges[i].shareEnd(edges[ii]);
            if (e == 1)
            {
                edges[ii].start += offset;
            }
            else if (e == -1)
            {
                edges[ii].end += offset;
            }
        }
        edges[i].end += offset;
    }
    public void moveEdges(List<int> edgeList, List<int> visibleEdges, UnityEngine.Vector3 offset)
    {
        List<int> movedStarts = new List<int>();
        List<int> movedEnds = new List<int>();
        for (int ii = 0; ii < edges.Count; ii++)
        {
            if (edgeList.Contains(ii))
                continue;

            foreach(int i in edgeList)
            {
                int e = edges[i].shareStart(edges[ii]);
                if (e == 1 && !movedStarts.Contains(ii))
                {
                    edges[ii].start += offset;
                    movedStarts.Add(ii);
                }
                else if (e == -1 && !movedEnds.Contains(ii))
                {
                    edges[ii].end += offset;
                    movedEnds.Add(ii);
                }

                e = edges[i].shareEnd(edges[ii]);
                if (e == 1 && !movedStarts.Contains(ii))
                {
                    edges[ii].start += offset;
                    movedStarts.Add(ii);
                }
                else if (e == -1 && !movedEnds.Contains(ii))
                {
                    edges[ii].end += offset;
                    movedEnds.Add(ii);
                }

            }
        }
        foreach(int i in edgeList)
        {
            edges[i].start += offset;
            edges[i].end += offset;
        }
        
    }

    public void removeEdge(rEdge e)
    {
        edges.Remove(e);
    }
    public void removeEdge(int i, bool safe = true)
    {
        if(safe)
        {
            List<rPolygon> toRemove = new List<rPolygon>();
            foreach (rPolygon p in polygons)
            {
                if (p.hasEdge(i))
                    toRemove.Add(p);
            }
            foreach (rPolygon p in toRemove)
                removePolygon(p);
            List<rTrack> trackRemove = new List<rTrack>();
            foreach (rTrack t in tracks)
                if (t.start == i || t.end == i)
                    trackRemove.Add(t);
            foreach (rTrack t in trackRemove)
                removeTrack(t);
        }
        

        edges.RemoveAt(i);
    }

    public void removeEdges(List<int> i)
    {
        i.Sort();
        for (int j = i.Count - 1; j > -1; j--)
             removeEdge(i[j], false);
    }

    

    public void addPolygon(int[] edges)
    {
        rPolygon pp = new rPolygon(edges, this);
        foreach(rPolygon p in polygons)
        {
            if(p.isSame(pp))
            {
                Debug.Log("Polygon already exists");
                return;
            }
        }
        polygons.Add(pp);
    }

    public void removePolygon(int p)
    {
        polygons.RemoveAt(p);
    }

    public void removePolygon(rPolygon p)
    {
        polygons.Remove(p);
    }


    public void optimize()
    {
        StartCoroutine(optimizeBezier());
    }
    
    
    public void addTrack(int s, int e)
    {
        rTrack t = new rTrack(this, s, e);

        foreach(rTrack tt in tracks)
        {
            if(tt.isSame(s,e))
            {
                Debug.Log("Track already exists");
                return;
            }
        }
        if(t.getPolygon()>-1)
            tracks.Add(t);
        else
        {
            Debug.Log("No common polygon");
        }
    }

    public void removeTrack(rTrack t)
    {
        tracks.Remove(t);
    }

    public void removeTrack(int s, int e)
    {
        int found = -1;
        for(int i=0; i<tracks.Count; i++)
        {
            if(tracks[i].isSame(s,e))
            {
                found = i;
                break;
            }
        }

        if(found>-1)
        {
            tracks.RemoveAt(found);
        }
    }

    public rTrack getTrack(int e1, int e2)
    {
        foreach(rTrack t in tracks)
        {
            if (t.isSame(e1, e2))
                return t;
        }
        return null;
    }
    public rTrack getTrack(rEdge e1, rEdge e2)
    {
        return getTrack(edges.IndexOf(e1), edges.IndexOf(e2));
    }

    public void CombineObstacles()
    {
        const float tolerance = 0.001f;

        List<rEdge> obstacleEdges = new List<rEdge>();
        List<rEdge> originalEdges = new List<rEdge>();

        // --------------------------------------------------------
        // Separate obstacle edges from ALL other edges.
        //
        // Obstacle edges are the cutters.
        // Every other edge can be split/shrunk by them.
        // --------------------------------------------------------

        foreach (rEdge edge in edges)
        {
            if (edge.isObstacle)
            {
                obstacleEdges.Add(edge);
            }
            else
            {
                originalEdges.Add(edge);
            }
        }


        // --------------------------------------------------------
        // STEP 1:
        // Split ALL non-obstacle edges at obstacle endpoints.
        // --------------------------------------------------------

        List<rEdge> splitEdges = new List<rEdge>();

        foreach (rEdge original in originalEdges)
        {
            List<UnityEngine.Vector3> splitPoints =
                new List<UnityEngine.Vector3>();

            splitPoints.Add(original.start);
            splitPoints.Add(original.end);

            foreach (rEdge obstacle in obstacleEdges)
            {
                if (PointOnEdge(
                    obstacle.start,
                    original,
                    tolerance))
                {
                    AddUniquePoint(
                        splitPoints,
                        obstacle.start,
                        tolerance);
                }

                if (PointOnEdge(
                    obstacle.end,
                    original,
                    tolerance))
                {
                    AddUniquePoint(
                        splitPoints,
                        obstacle.end,
                        tolerance);
                }
            }


            // ----------------------------------------------------
            // Sort split points along the original edge.
            // ----------------------------------------------------

            UnityEngine.Vector3 edgeVector =
                original.end - original.start;

            float edgeLength =
                edgeVector.magnitude;

            if (edgeLength <= tolerance)
                continue;

            UnityEngine.Vector3 direction =
                edgeVector / edgeLength;

            splitPoints.Sort((a, b) =>
            {
                float da =
                    UnityEngine.Vector3.Dot(
                        a - original.start,
                        direction);

                float db =
                    UnityEngine.Vector3.Dot(
                        b - original.start,
                        direction);

                return da.CompareTo(db);
            });


            // ----------------------------------------------------
            // Create the split pieces.
            // ----------------------------------------------------

            for (int i = 0;
                 i < splitPoints.Count - 1;
                 i++)
            {
                UnityEngine.Vector3 start =
                    splitPoints[i];

                UnityEngine.Vector3 end =
                    splitPoints[i + 1];

                if ((end - start).sqrMagnitude <=
                    tolerance * tolerance)
                {
                    continue;
                }

                rEdge piece =
                    new rEdge(
                        start,
                        end,
                        original.isWall);

                piece.isObstacle =
                    original.isObstacle;

                splitEdges.Add(piece);
            }
        }


        // --------------------------------------------------------
        // STEP 2:
        // Remove any split edge portion covered by an obstacle.
        // --------------------------------------------------------

        List<rEdge> finalEdges =
            new List<rEdge>();

        foreach (rEdge piece in splitEdges)
        {
            bool coveredByObstacle = false;

            foreach (rEdge obstacle in obstacleEdges)
            {
                if (EdgesOverlap(
                    piece,
                    obstacle,
                    tolerance))
                {
                    coveredByObstacle = true;
                    break;
                }
            }

            if (!coveredByObstacle)
            {
                finalEdges.Add(piece);
            }
        }


        // --------------------------------------------------------
        // STEP 3:
        // Add the obstacle boundaries themselves.
        // --------------------------------------------------------

        finalEdges.AddRange(obstacleEdges);


        // --------------------------------------------------------
        // STEP 4:
        // Replace original list.
        // --------------------------------------------------------

        edges.Clear();
        edges.AddRange(finalEdges);


        // --------------------------------------------------------
        // STEP 5:
        // Cleanup.
        // --------------------------------------------------------

        RemoveZeroLengthEdges(tolerance);
        RemoveDuplicateEdges();

        Debug.Log(
            $"CombineObstacles finished: " +
            $"{originalEdges.Count} original edges -> " +
            $"{splitEdges.Count} split pieces -> " +
            $"{finalEdges.Count} edges including obstacles. " +
            $"{obstacleEdges.Count} obstacle edges.");
    }

    // ============================================================
    // IS A POINT ON AN EDGE?
    // ============================================================

    private bool PointOnEdge(
        UnityEngine.Vector3 point,
        rEdge edge,
        float tolerance)
    {
        UnityEngine.Vector3 edgeVector =
            edge.end - edge.start;

        float edgeLength =
            edgeVector.magnitude;

        if (edgeLength <= tolerance)
            return false;

        UnityEngine.Vector3 direction =
            edgeVector / edgeLength;


        // --------------------------------------------------------
        // Distance from point to the infinite line.
        // --------------------------------------------------------

        float lineDistance =
            UnityEngine.Vector3.Cross(
                point - edge.start,
                direction).magnitude;

        if (lineDistance > tolerance)
            return false;


        // --------------------------------------------------------
        // Position along the line.
        // --------------------------------------------------------

        float distanceAlong =
            UnityEngine.Vector3.Dot(
                point - edge.start,
                direction);


        // Point must actually lie within the segment.
        return
            distanceAlong >= -tolerance &&
            distanceAlong <= edgeLength + tolerance;
    }


    // ============================================================
    // DO TWO COLLINEAR EDGES OVERLAP?
    // ============================================================

    private bool EdgesOverlap(
        rEdge a,
        rEdge b,
        float tolerance)
    {
        UnityEngine.Vector3 aVector =
            a.end - a.start;

        float aLength =
            aVector.magnitude;

        if (aLength <= tolerance)
            return false;

        UnityEngine.Vector3 aDirection =
            aVector / aLength;


        UnityEngine.Vector3 bVector =
            b.end - b.start;

        float bLength =
            bVector.magnitude;

        if (bLength <= tolerance)
            return false;


        // --------------------------------------------------------
        // Must be parallel.
        // --------------------------------------------------------

        float parallel =
            Mathf.Abs(
                UnityEngine.Vector3.Dot(
                    aDirection,
                    bVector.normalized));

        if (parallel < 0.999f)
            return false;


        // --------------------------------------------------------
        // Must lie on the same line.
        // --------------------------------------------------------

        float distance1 =
            UnityEngine.Vector3.Cross(
                b.start - a.start,
                aDirection).magnitude;

        float distance2 =
            UnityEngine.Vector3.Cross(
                b.end - a.start,
                aDirection).magnitude;

        if (distance1 > tolerance ||
            distance2 > tolerance)
        {
            return false;
        }


        // --------------------------------------------------------
        // Project B onto A's line.
        // --------------------------------------------------------

        float b1 =
            UnityEngine.Vector3.Dot(
                b.start - a.start,
                aDirection);

        float b2 =
            UnityEngine.Vector3.Dot(
                b.end - a.start,
                aDirection);

        float bMin =
            Mathf.Min(b1, b2);

        float bMax =
            Mathf.Max(b1, b2);


        // A occupies:
        //
        // [0, aLength]
        //
        // Find actual overlap.
        float overlapStart =
            Mathf.Max(0f, bMin);

        float overlapEnd =
            Mathf.Min(aLength, bMax);


        // Require actual line overlap.
        // Merely sharing one endpoint doesn't count.
        return overlapEnd - overlapStart > tolerance;
    }


    // ============================================================
    // ADD POINT WITHOUT DUPLICATES
    // ============================================================

    private void AddUniquePoint(
        List<UnityEngine.Vector3> points,
        UnityEngine.Vector3 point,
        float tolerance)
    {
        foreach (UnityEngine.Vector3 existing in points)
        {
            if ((existing - point).sqrMagnitude <=
                tolerance * tolerance)
            {
                return;
            }
        }

        points.Add(point);
    }


    // ============================================================
    // REMOVE ZERO-LENGTH EDGES
    // ============================================================

    private void RemoveZeroLengthEdges(
        float tolerance)
    {
        float toleranceSquared =
            tolerance * tolerance;

        for (int i = edges.Count - 1; i >= 0; i--)
        {
            if ((edges[i].end - edges[i].start).sqrMagnitude
                <= toleranceSquared)
            {
                edges.RemoveAt(i);
            }
        }
    }


    // ============================================================
    // REMOVE EXACT DUPLICATE EDGES
    // ============================================================

    private void RemoveDuplicateEdges()
    {
        for (int i = edges.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if (!edges[i].isSame(edges[j]))
                    continue;

                // Preserve information from both edges.
                edges[j].isWall =
                    edges[j].isWall ||
                    edges[i].isWall;

                edges[j].isObstacle =
                    edges[j].isObstacle ||
                    edges[i].isObstacle;

                edges.RemoveAt(i);

                break;
            }
        }
    }

    /*public void CombineObstacles()
    {
        for (int o = 0; o < edges.Count; o++)
        {
            if (edges[o].isObstacle)
            {
                rEdge edge = edges[o];
                UnityEngine.Vector3 VoriginalStart = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 VoriginalEnd = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 VedgeStart = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 VedgeEnd = UnityEngine.Vector3.zero;

                for (int i = 0; i < edges.Count; i++)
                {
                    if (i == o)
                        continue;
                    rEdge e = edges[i];
                    int originalEnd = 0;
                    int edgeEnd = edge.shareStart(e);
                    if (edgeEnd != 0)
                    {
                        originalEnd = 1;
                        VoriginalStart = edge.start;
                        VoriginalEnd = edge.end;
                        if (edgeEnd == 1)
                        {
                            VedgeStart = e.start;
                            VedgeEnd = e.end;
                        }
                        else
                        {
                            VedgeStart = e.end;
                            VedgeEnd = e.start;
                        }
                    }
                    else
                    {
                        edgeEnd = edge.shareEnd(e);
                        if (edgeEnd != 0)
                        {
                            originalEnd = -1;
                            VoriginalStart = edge.end;
                            VoriginalEnd = edge.start;
                            if (edgeEnd == 1)
                            {
                                VedgeStart = e.start;
                                VedgeEnd = e.end;
                            }
                            else
                            {
                                VedgeStart = e.end;
                                VedgeEnd = e.start;

                            }
                        }
                    }

                    if (originalEnd != 0)
                    {
                        //Debug.Log(UnityEngine.Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized));
                        //Debug.Log($"({VoriginalStart}, {VoriginalEnd}) - ({VedgeStart}, {VedgeEnd})");
                        if (UnityEngine.Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized) > 0.9f)
                        {
                            bool isWall = e.isWall;
                            edges[o].isWall = isWall;
                            edges[i].start = VoriginalEnd;
                            edges[i].end = VedgeEnd;
                            //map.removeEdge(i, false);
                            //map.addEdge(VoriginalEnd, VedgeEnd, isWall);

                        }
                    }
                }
            }

            if (edges[o].isWall)
            {
                rEdge edge = edges[o];
                UnityEngine.Vector3 VoriginalStart = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 VoriginalEnd = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 VedgeStart = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 VedgeEnd = UnityEngine.Vector3.zero;

                for (int i = 0; i < edges.Count; i++)
                {
                    if (i == o)
                        continue;
                    rEdge e = edges[i];
                    int originalEnd = 0;
                    int edgeEnd = edge.shareStart(e);
                    if (edgeEnd != 0)
                    {
                        originalEnd = 1;
                        VoriginalStart = edge.start;
                        VoriginalEnd = edge.end;
                        if (edgeEnd == 1)
                        {
                            VedgeStart = e.start;
                            VedgeEnd = e.end;
                        }
                        else
                        {
                            VedgeStart = e.end;
                            VedgeEnd = e.start;
                        }
                    }
                    else
                    {
                        edgeEnd = edge.shareEnd(e);
                        if (edgeEnd != 0)
                        {
                            originalEnd = -1;
                            VoriginalStart = edge.end;
                            VoriginalEnd = edge.start;
                            if (edgeEnd == 1)
                            {
                                VedgeStart = e.start;
                                VedgeEnd = e.end;
                            }
                            else
                            {
                                VedgeStart = e.end;
                                VedgeEnd = e.start;

                            }
                        }
                    }

                    if (originalEnd != 0)
                    {
                        //Debug.Log(UnityEngine.Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized));
                        //Debug.Log($"({VoriginalStart}, {VoriginalEnd}) - ({VedgeStart}, {VedgeEnd})");
                        if (UnityEngine.Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized) > 0.9f)
                        {
                            bool isWall = e.isWall;
                            // edges[o].isWall = isWall;
                            edges[i].start = VoriginalEnd;
                            edges[i].end = VedgeEnd;
                            //map.removeEdge(i, false);
                            //map.addEdge(VoriginalEnd, VedgeEnd, isWall);

                        }
                    }
                }
            }
        }
    }*/

    public void Clear()
    {
        edges.Clear();
        polygons.Clear();
        tracks.Clear();
        path.Clear();
        trajectory.Clear();
    }


    public void generateTracks()
    {
        Debug.Log("Generating tracks");
        tracks.Clear();

        foreach (rPolygon p in polygons)
        {
            List<int> commonEdges = new List<int>();
            foreach (rPolygon p2 in polygons)
            {
                if (p != p2)
                {
                    commonEdges.AddRange(p.shareEdge(p2));
                }
            }

            foreach (int i in commonEdges)
            {
                foreach (int j in commonEdges)
                {
                    if (i != j && !edges[i].isWall && !edges[j].isWall && !edges[i].isObstacle && !edges[j].isObstacle)
                        addTrack(i, j);
                }
            }
        }
    }
}

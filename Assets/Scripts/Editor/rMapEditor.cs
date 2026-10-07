using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
[CustomEditor(typeof(rMap))]
public class rMapEditor : Editor
{
    public enum EditorMode { Edge, Edges, Polygons, Tracks, Path, CurvatureChecker}
    rMap map;
    public bool doDraw = true;
    public List<int> polygonEdges = new List<int>();
    List<int> visibleEdges = new List<int>();

    public int selectedEdgeIndex;
    public int selectedTrackIndex = -1;
    public List<int> selectedEdges = new List<int>();
    public int selectedPath = 0;
    string selectedPathString = "";
    public List<Transform> corners = new List<Transform>();
    int selectedCorner = 0;

    EditorMode currentMode = EditorMode.Edge;
    private void OnSceneGUI()
    {
        if (doDraw)
        {
            Draw();
            Input();
        }
    }

    public int getEdge(Event current, bool checkCurrent = false)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        float enter = 0;
        Vector3 pos = Vector3.zero;

        Plane plane = new Plane(Vector3.up, 0);
        if (plane.Raycast(ray, out enter))
        {
            pos = ray.GetPoint(enter);
            List<int> closePoints = new List<int>();
            foreach (int i in visibleEdges)
            {
                if ((pos - map.edges[i].middle).sqrMagnitude < 0.05f)
                {
                        closePoints.Add(i);
                }
            }
            if (closePoints.Count == 0)
                return -1;
            int closest = 0;
            float cl = 9999;
            for(int i =0; i<closePoints.Count; i++)
            {
                float d = (pos - map.edges[closePoints[i]].middle).sqrMagnitude;
                if (d<cl)
                {
                    cl = d;
                    closest = i;
                }
            }
            return closePoints[closest];
        }
        return -1;
    }

    public Vector3 mousePosition(Event current)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        float enter = 0;
        Vector3 pos = Vector3.zero;

        Plane plane = new Plane(Vector3.up, 0);
        if (plane.Raycast(ray, out enter))
        {
            pos = ray.GetPoint(enter);
            return pos;
        }
        return Vector3.zero;
    }
    public int getPolygon(Event current)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        float enter = 0;
        Vector3 pos = Vector3.zero;

        Plane plane = new Plane(Vector3.up, 0);
        if (plane.Raycast(ray, out enter))
        {
            pos = ray.GetPoint(enter);

            for (int i = 0; i < map.polygons.Count; i++)
            {
                if ((pos - map.polygons[i].middle).sqrMagnitude < 0.05f)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public int getTrack(Event current)
    {
        Vector3 pos = mousePosition(current);
        for(int i=0; i<map.tracks.Count; i++)
        {
            Vector3 mid = map.tracks[i].middle;
            if ((mid - pos).sqrMagnitude < 0.05f)
                return i;
        }
        return -1;
    }

    public void CombineObstacles()
    {
        for (int o = 0; o < map.edges.Count; o++)
        {
            if (map.edges[o].isObstacle)
            {
                rEdge edge = map.edges[o];
                Vector3 VoriginalStart = Vector3.zero;
                Vector3 VoriginalEnd = Vector3.zero;
                Vector3 VedgeStart = Vector3.zero;
                Vector3 VedgeEnd = Vector3.zero;

                for (int i = 0; i < map.edges.Count; i++)
                {
                    if (i == o)
                        continue;
                    rEdge e = map.edges[i];
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
                        //Debug.Log(Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized));
                        //Debug.Log($"({VoriginalStart}, {VoriginalEnd}) - ({VedgeStart}, {VedgeEnd})");
                        if (Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized) > 0.9f)
                        {
                            bool isWall = e.isWall;
                            map.edges[o].isWall = isWall;
                            map.edges[i].start = VoriginalEnd;
                            map.edges[i].end = VedgeEnd;
                            //map.removeEdge(i, false);
                            //map.addEdge(VoriginalEnd, VedgeEnd, isWall);

                        }
                    }
                }
            }
        }
    }

    public void Input()
    {
        if (map.drawHere == false)
            return;
        Event current = Event.current;
        switch (currentMode)
        {
            case EditorMode.Edge:
                if (current.type == EventType.KeyDown)
                {
                    if (current.keyCode == KeyCode.Space)
                    {
                        int i = getEdge(current, false);
                        if (i > -1)
                        {
                            selectedEdgeIndex = i;
                        }
                    }
                    if (current.keyCode == KeyCode.X)
                    {
                        Undo.RecordObject(map, "Remove edge");
                        if (selectedEdgeIndex == -1)
                            selectedEdgeIndex = getEdge(current, false);
                        if (selectedEdgeIndex > -1)
                            map.removeEdge(selectedEdgeIndex);
                        selectedEdgeIndex = -1;
                    }
                    if (current.keyCode == KeyCode.Z)
                    {
                        Undo.RecordObject(map, "Switch Wall");
                        if (selectedEdgeIndex == -1)
                            selectedEdgeIndex = getEdge(current, true);
                        if (selectedEdgeIndex > -1)
                        {
                            map.edges[selectedEdgeIndex].isWall = !map.edges[selectedEdgeIndex].isWall;
                        }
                        selectedEdgeIndex = -1;
                    }
                    if(current.keyCode == KeyCode.V)
                    {
                        map.pathStarts.Add(selectedEdgeIndex);
                    }
                }
                break;
            case EditorMode.Edges:
                if (current.type == EventType.KeyDown)
                {
                    if (current.keyCode == KeyCode.Space)
                    {
                        int i = getEdge(current, false);
                        if (i > -1)
                        {
                            if (selectedEdges.Contains(i))
                                selectedEdges.Remove(i);
                            else
                                selectedEdges.Add(i);
                        }
                    }
                    if(current.keyCode == KeyCode.I)
                    {
                        rEdge edge = map.edges[selectedEdges[0]];
                        Vector3 VoriginalStart = Vector3.zero;
                        Vector3 VoriginalEnd = Vector3.zero;
                        Vector3 VedgeStart = Vector3.zero;
                        Vector3 VedgeEnd = Vector3.zero;

                        for(int i = 0; i<map.edges.Count;i++)
                        {
                            if (i == selectedEdges[0])
                                continue;
                            rEdge e = map.edges[i];
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

                            if(originalEnd!=0)
                            {
                                Debug.Log(Vector3.Dot((VoriginalEnd - VoriginalStart).normalized, (VedgeEnd - VedgeStart).normalized));
                                Debug.Log($"({VoriginalStart}, {VoriginalEnd}) - ({VedgeStart}, {VedgeEnd})");
                                if(Vector3.Dot((VoriginalEnd-VoriginalStart).normalized, (VedgeEnd-VedgeStart).normalized) >0.9f)
                                {
                                    selectedEdges.Add(i);
                                }
                            }
                        }
                    }
                    if (current.keyCode == KeyCode.X)
                    {
                        Undo.RecordObject(map, "Remove edge");

                        map.removeEdges(selectedEdges);
                        selectedEdges.Clear();
                    }
                    if (current.keyCode == KeyCode.Z)
                    {
                        //Undo.RecordObject(map, "Switch Wall");

                        for (int i = 0; i < selectedEdges.Count; i++)
                            map.edges[selectedEdges[i]].isWall = !map.edges[selectedEdges[i]].isWall;
                        selectedEdges.Clear();
                    }
                    if (current.keyCode == KeyCode.O)
                    {
                        //Undo.RecordObject(map, "Switch Wall");

                        for (int i = 0; i < selectedEdges.Count; i++)
                            map.edges[selectedEdges[i]].isObstacle = !map.edges[selectedEdges[i]].isObstacle;
                        selectedEdges.Clear();
                    }

                    if (current.keyCode == KeyCode.Period)
                    {
                        if(selectedEdges.Count==2)
                        {
                            Undo.RecordObject(map, "Split Edges");
                            rEdge e1 = map.edges[selectedEdges[0]];
                            rEdge e2 = map.edges[selectedEdges[1]];
                            Vector3 start = e1.middle;
                            Vector3 end = e2.middle;

                            map.addEdge(e1.start, start, e1.isWall, true);
                            map.addEdge(start, e1.end, e1.isWall, true);
                            map.addEdge(e2.start, end, e2.isWall, true);
                            map.addEdge(end, e2.end, e2.isWall, true);
                            map.addEdge(start, end, false, true);

                            map.removeEdge(e1);
                            map.removeEdge(e2);

                        }
                        if(selectedEdges.Count == 1)
                        {
                            Undo.RecordObject(map, "Split Edge");
                            rEdge e1 = map.edges[selectedEdges[0]];
                            Vector3 mid = e1.middle;
                            map.addEdge(e1.start, mid);
                            map.addEdge(mid, e1.end);
                            map.removeEdge(e1);
                        }

                        if(selectedEdges.Count == 3)
                        {
                            Undo.RecordObject(map, "Split Edges");
                            rEdge e1 = map.edges[selectedEdges[0]];
                            rEdge e2 = map.edges[selectedEdges[1]];
                            rEdge e4 = map.edges[selectedEdges[2]];
                            Vector3 start = e1.middle;
                            Vector3 end = e2.middle;

                            map.addEdge(e1.start, start, e1.isWall, true);
                            map.addEdge(start, e1.end, e1.isWall, true);
                            map.addEdge(e2.start, end, e2.isWall, true);
                            map.addEdge(end, e2.end, e2.isWall, true);
                            rEdge e3 = map.addEdge(start, end, false, true);

                            start = e3.middle;
                            end = e4.middle;

                            map.addEdge(e3.start, start, e3.isWall, true);
                            map.addEdge(start, e3.end, e3.isWall, true);
                            map.addEdge(e4.start, end, e4.isWall, true);
                            map.addEdge(end, e4.end, e4.isWall, true);
                            map.addEdge(start, end, false, true);
                            map.removeEdge(e1);
                            map.removeEdge(e2);
                            map.removeEdge(e3);
                            map.removeEdge(e4);
                        }
                        selectedEdges.Clear();

                    }
                    if(current.keyCode == KeyCode.N)
                    {
                        if(selectedEdges.Count == 2)
                        {
                            Vector3 start = map.edges[selectedEdges[0]].start;
                            Vector3 end = map.edges[selectedEdges[1]].start;
                            float d = (map.edges[selectedEdges[0]].start - map.edges[selectedEdges[1]].start).magnitude;
                            float dd = (map.edges[selectedEdges[0]].start - map.edges[selectedEdges[1]].end).magnitude;
                            if(dd<d)
                            {
                                d = dd;
                                start = map.edges[selectedEdges[0]].start;
                                end = map.edges[selectedEdges[1]].end;
                            }
                            dd = (map.edges[selectedEdges[0]].end - map.edges[selectedEdges[1]].start).magnitude;
                            if (dd < d)
                            {
                                d = dd;
                                start = map.edges[selectedEdges[0]].end;
                                end = map.edges[selectedEdges[1]].start;
                            }
                            dd = (map.edges[selectedEdges[0]].end - map.edges[selectedEdges[1]].end).magnitude;
                            if (dd < d)
                            {
                                d = dd;
                                start = map.edges[selectedEdges[0]].end;
                                end = map.edges[selectedEdges[1]].end;
                            }
                            Undo.RecordObject(map,"Close Gap");
                            map.addEdge(start, end, map.edges[selectedEdges[0]].isWall,true);
                            selectedEdges.Clear();


                        }
                        else if(selectedEdges.Count>0)
                        {
                            List<Vector3> points = new List<Vector3>();

                            foreach(int i in selectedEdges)
                            {
                                bool start = false;
                                bool end = false;
                                foreach(int j in selectedEdges)
                                {
                                    if (i == j)
                                        continue;

                                    if (map.edges[i].shareStart(map.edges[j]) != 0)
                                        start = true;
                                    if (map.edges[i].shareEnd(map.edges[j]) != 0)
                                        end = true;
                                }
                                if (!start)
                                    points.Add(map.edges[i].start);
                                if (!end)
                                    points.Add(map.edges[i].end);
                            }
                            Undo.RecordObject(map, "Close Loop");
                            foreach(Vector3 i in points)
                                foreach(Vector3 j in points)
                                {
                                    if (i == j)
                                        continue;
                                    map.addEdge(i, j, false, true);
                                    if(!selectedEdges.Contains(map.edges.Count - 1))
                                        selectedEdges.Add(map.edges.Count - 1);
                                }
                        }
                        
                    }
                    if(current.keyCode == KeyCode.V)
                    {
                        if (selectedEdges.Count > 0)
                        {
                            List<int> next = map.getConnectedEdges(selectedEdges[0]);
                            if (next.Count > 0)
                                selectedEdges.AddRange(next);
                        }
                    }
                    if (current.keyCode == KeyCode.W)
                    {
                        Undo.RecordObject(map, "moveStart");
                        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                        float enter = 0;
                        Vector3 pos = Vector3.zero;

                        Plane plane = new Plane(Vector3.up, 0);
                        if (plane.Raycast(ray, out enter))
                        {
                            pos = ray.GetPoint(enter);
                            map.edges[selectedEdges[0]].start = pos;
                        }
                    }
                    if (current.keyCode == KeyCode.S)
                    {
                        Undo.RecordObject(map, "moveEnd");
                        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                        float enter = 0;
                        Vector3 pos = Vector3.zero;

                        Plane plane = new Plane(Vector3.up, 0);
                        if (plane.Raycast(ray, out enter))
                        {
                            pos = ray.GetPoint(enter);
                            map.edges[selectedEdges[0]].end = pos;
                        }
                    }
                    if(current.keyCode == KeyCode.D)
                    {

                        SceneView.lastActiveSceneView.LookAt(corners[selectedCorner].position);
                        selectedCorner = (selectedCorner + 1) % corners.Count;
                    }
                    if (current.keyCode == KeyCode.Comma)
                    {
                        
                        if(selectedEdges.Count == 2)
                        {
                            Undo.RecordObject(map, "Join Edges");
                            rEdge e1 = map.edges[selectedEdges[0]];
                            rEdge e2 = map.edges[selectedEdges[1]];

                            int i = e1.shareEnd(e2);
                            int j = e1.shareStart(e2);
                            bool found = false;
                            if(i == 1)
                            {
                                map.addEdge(e1.start, e2.end, e1.isWall && e2.isWall, true);
                                found = true;
                            }
                            else if(i==-1)
                            {
                                map.addEdge(e1.start, e2.start, e1.isWall && e2.isWall, true);
                                found = true;
                            }
                            else if(j==1)
                            {
                                map.addEdge(e1.end, e2.end, e1.isWall && e2.isWall, true);
                                found = true;

                            }
                            else if(j==-1)
                            {
                                map.addEdge(e1.end, e2.start, e1.isWall && e2.isWall, true);
                                found = true;
                            }

                            if(found)
                            {
                                map.removeEdge(e1);
                                map.removeEdge(e2);
                            }
                        }
                        else if(selectedEdges.Count>2)
                        {
                            Undo.RecordObject(map, "Dissolving multiple edges");
                            List<rEdge> edges = new List<rEdge>();
                            foreach (int i in selectedEdges)
                                edges.Add(map.edges[i]);
                            Vector3 start = edges[0].start;

                            int j = edges[0].shareStart(edges[1]);
                            if (j != 0)
                                start = edges[0].end;

                            Vector3 end = edges[edges.Count - 1].start;
                            j = edges[edges.Count - 1].shareStart(edges[edges.Count - 2]);
                            if (j != 0)
                                end = edges[edges.Count - 1].end;

                            map.addEdge(start, end, edges[0].isWall);
                            foreach (rEdge e in edges)
                                map.removeEdge(e);

                        }
                        else if(selectedEdges.Count == 1)
                        {
                            //Debug.Log("Here");
                            Undo.RecordObject(map, "Dissolving edge");
                            rEdge e = map.edges[selectedEdges[0]];
                            rEdge[] startEdges = new rEdge[2];
                            rEdge[] endEdges = new rEdge[2];
                            int starts = 0;
                            int ends = 0;
                            foreach (int ii in visibleEdges)
                            {
                                if (ii == selectedEdges[0])
                                    continue;
                                rEdge ee = map.edges[ii];
                                if (e.shareStart(ee) != 0)
                                {
                                    startEdges[starts] = ee;
                                    starts++;
                                }
                                if (e.shareEnd(ee) != 0)
                                {
                                    endEdges[ends] = ee;
                                    ends++;
                                }
                            }
                            rEdge e1;
                            rEdge e2;
                            bool found = false;
                            int i;
                            int j;
                            if (starts == 2)
                            {
                                e1 = startEdges[0];
                                e2 = startEdges[1];
                                i = e1.shareEnd(e2);
                                j = e1.shareStart(e2);
                                if (i == 1)
                                {
                                    map.addEdge(e1.start, e2.end);
                                    found = true;
                                }
                                else if (i == -1)
                                {
                                    map.addEdge(e1.start, e2.start);
                                    found = true;
                                }
                                else if (j == 1)
                                {
                                    map.addEdge(e1.end, e2.end);
                                    found = true;

                                }
                                else if (j == -1)
                                {
                                    map.addEdge(e1.end, e2.start);
                                    found = true;
                                }

                                if (found)
                                {
                                    map.removeEdge(e1);
                                    map.removeEdge(e2);
                                }
                            }

                            if (ends == 2)
                            {
                                e1 = endEdges[0];
                                e2 = endEdges[1];
                                i = e1.shareEnd(e2);
                                j = e1.shareStart(e2);
                                if (i == 1)
                                {
                                    map.addEdge(e1.start, e2.end);
                                    found = true;
                                }
                                else if (i == -1)
                                {
                                    map.addEdge(e1.start, e2.start);
                                    found = true;
                                }
                                else if (j == 1)
                                {
                                    map.addEdge(e1.end, e2.end);
                                    found = true;

                                }
                                else if (j == -1)
                                {
                                    map.addEdge(e1.end, e2.start);
                                    found = true;
                                }

                                if (found)
                                {
                                    map.removeEdge(e1);
                                    map.removeEdge(e2);
                                }
                            }
                            map.removeEdge(e);

                        }
                        
                        selectedEdges.Clear();
                    }
                    if (current.keyCode == KeyCode.Minus)
                    {
                        selectedEdges.Clear();

                    }
                    if(current.keyCode == KeyCode.RightControl)
                    {
                        Debug.Log("Straightening");
                        Undo.RecordObject(map, "Straightening edge");
                        foreach (int i in selectedEdges)
                        {
                            float[] dif = { Mathf.Abs(map.edges[i].start.x - map.edges[i].end.x), Mathf.Abs(map.edges[i].start.z - map.edges[i].end.z) };
                            if(dif[0]<dif[1])
                            {
                                Debug.Log("X");
                                float mid = (map.edges[i].start.x + map.edges[i].end.x) / 2;

                                map.moveEdgeStart(i, visibleEdges, new Vector3(mid, 0, map.edges[i].start.z) - map.edges[i].start);
                                map.moveEdgeEnd(i, visibleEdges, new Vector3(mid, 0, map.edges[i].end.z) - map.edges[i].end);
                            }
                            else
                            {
                                Debug.Log("Z");
                                float mid = (map.edges[i].start.z + map.edges[i].end.z) / 2;
                                map.moveEdgeStart(i, visibleEdges, new Vector3(map.edges[i].start.x,0,mid) - map.edges[i].start);
                                map.moveEdgeEnd(i, visibleEdges, new Vector3(map.edges[i].end.x,0,mid) - map.edges[i].end);
                            }
                            
                        }
                    }
                }
                break;
            case EditorMode.Polygons:
                if (current.type == EventType.KeyDown)
                {
                    if (current.keyCode == KeyCode.Space)
                    {
                        int i = getEdge(current, false);
                        if (i > -1)
                        {
                            if (!polygonEdges.Contains(i))
                                polygonEdges.Add(i);
                            else
                                polygonEdges.Remove(i);
                        }
                    }
                    if (current.keyCode == KeyCode.C)
                    {
                        if (polygonEdges.Count > 0)
                        {
                            Undo.RecordObject(map, "Add Polygon");
                            map.addPolygon(polygonEdges.ToArray());
                            polygonEdges.Clear();
                        }
                    }
                    if (current.keyCode == KeyCode.X)
                    {
                        Undo.RecordObject(map, "Remove Polygon");
                        int p = getPolygon(current);
                        if (p > -1)
                        {
                            map.removePolygon(p);
                        }
                    }
                    if(current.keyCode == KeyCode.LeftAlt)
                    {
                        selectedEdgeIndex = getPolygon(current);
                        Debug.Log(selectedEdgeIndex);
                        
                    }
                    if(current.keyCode == KeyCode.B)
                    {
                        Undo.RecordObject(map, "AutoCreatePolygons");
                        if(!Application.isPlaying)
                        {
                            foreach(Piece light in FindObjectsByType(typeof(Piece),FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                            {
                                polygonEdges.Clear();
                                Vector3 lightPos = light.transform.position;
                                for (int i=0; i<map.edges.Count; i++)
                                {
                                    Vector3 mid = map.edges[i].middle;
                                    if ((lightPos - mid).sqrMagnitude > 30)
                                        continue;
                                    bool blocked = false;
                                    for (int j= 0; j < map.edges.Count; j++)
                                    {
                                        if (i == j)
                                            continue;

                                        if (rHelpers.doIntersect(lightPos, mid, map.edges[j].start, map.edges[j].end))
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
                                    map.addPolygon(polygonEdges.ToArray());
                            }

                            polygonEdges.Clear();
                        }
                        else
                        map.generatePolygons();

                    }
                    if(current.keyCode == KeyCode.V)
                    {
                        polygonEdges.Clear();
                        Vector3 mousePos = mousePosition(current);
                        foreach(int i in visibleEdges)
                        {
                            Vector3 mid = map.edges[i].middle;
                            bool blocked = false;
                            foreach(int j in visibleEdges)
                            {
                                if (i == j)
                                    continue;

                                if (rHelpers.doIntersect(mousePos, mid, map.edges[j].start, map.edges[j].end))
                                {
                                    blocked = true;
                                    break;
                                }

                            }

                            if(!blocked)
                            {
                                polygonEdges.Add(i);
                            }
                        }
                    }
                }
                break;
            case EditorMode.Tracks:
                if (current.type == EventType.KeyDown)
                {
                    if (current.keyCode == KeyCode.Space)
                    {
                        int i = getEdge(current, false);
                        if (i > -1)
                        {
                            if (selectedEdgeIndex == -1)
                                selectedEdgeIndex = i;
                            else
                            {
                                Undo.RecordObject(map, "Add Track");
                                map.addTrack(selectedEdgeIndex, i);
                                selectedEdgeIndex = i;
                            }
                        }

                    }

                    if(current.keyCode == KeyCode.V)
                    {
                        int i = getTrack(current);
                        if(selectedTrackIndex == i)
                        {
                            selectedTrackIndex = -1;
                            
                        }
                        else
                            selectedTrackIndex = i;
                    }
                }
                break;
            case EditorMode.Path:
                if (current.type == EventType.KeyDown)
                {
                    if (current.keyCode == KeyCode.Space)
                    {
                        int i = getEdge(current, false);
                        if (i > -1)
                        {

                            if (selectedEdgeIndex == -1)
                                selectedEdgeIndex = i;
                            else if(selectedEdgeIndex == i)
                            {
                                selectedEdgeIndex = -1;
                            }
                            else
                            {
                                Undo.RecordObject(map, "Add Path");
                                map.path.Add(i);
                                selectedEdgeIndex = i;
                            }
                        }
                    }
                    
                }
                break;
        }
    }

    public void Draw()
    {
        if (map.drawHere == false)
            return;
        visibleEdges.Clear();
        Vector3 leftBottom = Vector3.zero;

        Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0f, 0f, 1.0f));
        float enter = 0;

        Plane plane = new Plane(Vector3.up, 0);
        if (plane.Raycast(ray, out enter))
        {
            leftBottom = ray.GetPoint(enter);
        }

        Vector3 rightTop = Vector3.zero;
        ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(1f, 1f, 1.0f));
        enter = 0;
        if (plane.Raycast(ray, out enter))
        {
            rightTop = ray.GetPoint(enter);
        }
        if (map!=null)
        {
            

            if(!map.drawOnlyPath && !map.drawOnlyTrajectory)
            {
                for (int i = 0; i < map.edges.Count; i++)
                {
                    rEdge e = map.edges[i];
                    Vector3 mid = e.middle;
                    if (mid.x < leftBottom.x || mid.x > rightTop.x || mid.z < leftBottom.z || mid.z > rightTop.z)
                        continue;
                    visibleEdges.Add(i);
                    Handles.color = Color.yellow;
                    if (selectedEdgeIndex == i || selectedEdges.Contains(i))
                        Handles.color = Color.blue;
                    else if (e.isWall)
                        Handles.color = Color.magenta;
                    else if (e.isObstacle)
                        Handles.color = Color.cyan;

                    Handles.DrawLine(e.start, e.end);
                    if (polygonEdges.Contains(i))
                        Handles.color = Color.cyan;
                    else
                        Handles.color = Color.red;

                    Handles.DrawSolidDisc(e.middle, Vector3.up, 0.03f);

                    if (i == selectedEdgeIndex || selectedEdges.Contains(i))
                    {
                        Vector3 newPos = Handles.PositionHandle(e.middle, Quaternion.identity);

                        if ((newPos - e.middle).sqrMagnitude > 0.0001f)
                        {
                            Undo.RecordObject(map, "Moved edge");
                            if (currentMode == EditorMode.Edge)
                                map.moveEdge(i, visibleEdges, newPos - e.middle);
                            else if (currentMode == EditorMode.Edges)
                                map.moveEdges(selectedEdges, visibleEdges, newPos - e.middle);
                        }
                    }
                }

                if (currentMode == EditorMode.Polygons)
                {
                    Handles.color = Color.blue;
                    for (int i = 0; i < map.polygons.Count; i++)
                    {
                        if (selectedEdgeIndex == i)
                            Handles.color = Color.red;
                        else
                            Handles.color = Color.blue;
                        for (int j = 0; j < map.polygons[i].edges.Length; j++)
                        {
                            rEdge e = map.edges[map.polygons[i].edges[j]];
                            Vector3 mid = e.middle;
                            if (mid.x < leftBottom.x || mid.x > rightTop.x || mid.z < leftBottom.z || mid.z > rightTop.z)
                                continue;
                            Handles.DrawLine(e.start, e.end);
                            Vector3 n = e.normal;
                            Handles.DrawLine(e.middle, e.middle + n);
                        }


                        Handles.DrawSolidDisc(map.polygons[i].middle, Vector3.up, 0.1f);
                    }
                    for(int i=0; i<map.polygons.Count;i++)
                    {
                        if(selectedEdgeIndex == i)
                        {
                            Handles.color = Color.red;
                            for (int j = 0; j < map.polygons[i].edges.Length; j++)
                            {
                                rEdge e = map.edges[map.polygons[i].edges[j]];
                                Vector3 mid = e.middle;
                                if (mid.x < leftBottom.x || mid.x > rightTop.x || mid.z < leftBottom.z || mid.z > rightTop.z)
                                    continue;
                                Handles.DrawLine(e.start, e.end);
                                Vector3 n = e.normal;
                                Handles.DrawLine(e.middle, e.middle + n);
                            }
                            break;
                        }
                    }
                    Handles.color = Color.green;
                    for (int i = 0; i < polygonEdges.Count; i++)
                    {
                        rEdge e = map.edges[polygonEdges[i]];
                        Handles.DrawLine(e.start, e.end);
                    }
                }
                if (currentMode == EditorMode.Tracks || currentMode == EditorMode.Path)
                {
                    for (int i = 0; i < map.tracks.Count; i++)
                    {
                        Handles.color = Color.green;
                        if (selectedTrackIndex == i)
                            Handles.color = Color.blue;
                        rEdge e1 = map.edges[map.tracks[i].start];
                        Vector3 mid = e1.middle;
                        if (mid.x < leftBottom.x || mid.x > rightTop.x || mid.z < leftBottom.z || mid.z > rightTop.z)
                            continue;
                        rEdge e2 = map.edges[map.tracks[i].end];
                        mid = e2.middle;
                        if (mid.x < leftBottom.x || mid.x > rightTop.x || mid.z < leftBottom.z || mid.z > rightTop.z)
                            continue;
                        Handles.DrawLine(e1.middle, e2.middle, 3);
                        Handles.DrawSolidDisc(map.tracks[i].middle, Vector3.up, 0.1f);
                    }

                    if (selectedTrackIndex > -1)
                    {
                        rTrack t = map.tracks[selectedTrackIndex];
                        rEdge e1 = map.edges[t.start];
                        rEdge e2 = map.edges[t.end];
                        rPolygon p = map.polygons[t.getPolygon()];

                        Vector3 n1 = rHelpers.getNormal(e1, p);
                        Vector3 n2 = rHelpers.getNormal(e2, p);
                        Handles.color = Color.magenta;
                        Handles.DrawLine(e1.middle, e1.middle + n1);
                        Handles.DrawLine(e2.middle, e2.middle + n2);


                    }

                }
                Handles.color = Color.white;
                if (currentMode == EditorMode.Path)
                    for (int i = 0; i < map.path.Count - 1; i++)
                    {
                        Handles.DrawLine(map.edges[map.path[i]].middle, map.edges[map.path[i + 1]].middle, 5);
                    }


                Handles.color = Color.black;
                for (int i = 0; i < map.trajectory.Count - 1; i++)
                {
                    Handles.DrawLine(map.trajectory[i], map.trajectory[i + 1], 8);
                }

                if (currentMode == EditorMode.CurvatureChecker)
                {
                    float distance = positionAlongPath-0.2f;// map.distanceTimeCurve.Evaluate(positionAlongPath - 0.2f);
                    var point = map.getPointByDistance(distance);
                    Vector3 p0 = rHelpers.CubicBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);

                    distance = positionAlongPath;// map.distanceTimeCurve.Evaluate(positionAlongPath);
                    point = map.getPointByDistance(distance);
                    Vector3 p1 = rHelpers.CubicBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
                    float realRadius = rHelpers.GetCurvatureRadius(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
                    Vector3 realCenter = rHelpers.GetOsculatingCircleCenter(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
                    float ss = rHelpers.ComputeCurvatureRate(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
                    distance = positionAlongPath + 0.2f;// map.distanceTimeCurve.Evaluate(positionAlongPath + 0.2f);
                    point = map.getPointByDistance(distance);
                    Vector3 p2 = rHelpers.CubicBezier(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
                    var (center, radius) = rHelpers.GetCircle(p0, p1, p2);


                    Handles.color = Color.white;
                    Handles.DrawWireDisc(center, Vector3.up, radius);
                    Handles.color = Color.magenta;
                    Handles.DrawWireDisc(realCenter, Vector3.up, Mathf.Abs(realRadius));
                    Handles.color = Color.black;
                    Handles.DrawSolidDisc(p0, Vector3.up, 0.1f);
                    Handles.DrawSolidDisc(p1, Vector3.up, 0.1f);
                    Handles.DrawSolidDisc(p2, Vector3.up, 0.1f);

                    float velocity = (Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2)) / 0.4f;

                    float angularVelocity = velocity / realRadius;

                    Debug.Log($"Velocity: {velocity} \tAngular velocity: {angularVelocity} {radius} {realRadius}");
                    Debug.Log("Distance: " + rHelpers.getClosestPointToWall(map, p1, true, true));
                    Debug.Log("CurvatureSpeed: " + ss);
                }
            }
            else if (map.drawOnlyPath)
            {
                Handles.color = Color.white;
                for (int i = 0; i < map.path.Count - 1; i++)
                {
                    Handles.DrawLine(map.edges[map.path[i]].middle, map.edges[map.path[i + 1]].middle, 5);
                }
            }
            else
            {
                Handles.color = Color.black;
                for (int i = 0; i < map.trajectory.Count - 1; i++)
                {
                    Handles.DrawLine(map.trajectory[i], map.trajectory[i + 1], 8);
                }
            }
            

        }
    }

    public float positionAlongPath = 0.1f;
    Vector3 circleCenter( Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float x0 = p0.x;
        float y0 = p0.z;
        float x1 = p1.x;
        float y1 = p1.z;
        float x2 = p2.x;
        float y2 = p2.z;

        float offset = x1 * x1 + y1 * y1;
        float bc = (x0 * x0 + y0 * y0 - offset) / 2.0f;
        float cd = (offset - x2 * x2 - y2 * y2) / 2.0f;
        float det = (x0 - x1) * (y1 - y2) - (x1 - x2) * (y0 - y1);
        float invDet = 1 / det;
        float cx = (bc * (y1 - y2) - cd * (y0 - y1)) * invDet;
        float cy = (cd * (x0 - x1) - bc * (x1 - x2)) * invDet;
        return new Vector3(cd, 0, cy);
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        currentMode = (EditorMode)EditorGUILayout.EnumPopup(currentMode);
        selectedEdgeIndex = EditorGUILayout.IntField(selectedEdgeIndex);
        if(GUILayout.Button("From Pieces"))
        {
            Undo.RecordObject(map, "Generated map from pieces");
            map.MapFromPieces();
            SceneView.RepaintAll();
            selectedEdgeIndex = -1;
            selectedEdges.Clear();
            CombineObstacles();
        }
        if(GUILayout.Button("Generate Tracks"))
        {
            Undo.RecordObject(map, "Generate Tracks");
            map.generateTracks();
            return;
            map.tracks.Clear();

            foreach(rPolygon p in map.polygons)
            {
                List<int> commonEdges = new List<int>();
                foreach (rPolygon p2 in map.polygons)
                {
                    if(p!=p2)
                    {
                         commonEdges.AddRange(p.shareEdge(p2));
                    }
                }

                foreach(int i in commonEdges)
                {
                    foreach(int j in commonEdges)
                    {
                        if (i != j && !map.edges[i].isWall && !map.edges[j].isWall && !map.edges[i].isObstacle && !map.edges[j].isObstacle)
                            map.addTrack(i, j);
                    }
                }
            }
        }
        if(GUILayout.Button("Generate Path"))
        {
            Undo.RecordObject(map, "Generated path");
            map.generatePath(selectedEdgeIndex);
        }
        if(GUILayout.Button("Prepare walls"))
        {
            map.prepareWalls();
        }
        
        if(GUILayout.Button("Calculate length"))
        {
            float l = 0;
            foreach (Bezier b in map.beziers)
                l += b.length;
            Debug.Log(l);
        }
        if(GUILayout.Button("Get corners"))
        {
            Transform[] trans = map.GetComponentsInChildren<Transform>();
            corners.Clear();
            selectedCorner = 0;
            foreach(Transform t in trans)
            {
                if (t.name.Contains("Corridor.Corner"))
                    corners.Add(t);
            }
        }

        GUILayout.Space(10);
        float last = positionAlongPath;
        positionAlongPath = GUILayout.HorizontalSlider(positionAlongPath, 0.1f, map.desiredLength - 0.1f);

        string empty = "";
        GUILayout.Space(30);
        string newValue = GUILayout.TextField(positionAlongPath.ToString("F4"));
        float newfloat = 0;
        if (newValue != empty)
        {
            if (float.TryParse(newValue, out newfloat))
            {
                positionAlongPath = newfloat;
            }
        }
        else
            positionAlongPath = 0.1f;
        if (positionAlongPath != last)
            SceneView.RepaintAll();
        GUILayout.Space(10);

        

        if (GUILayout.Button("Generate many"))
            map.startMany();
        
    }

    private void OnEnable()
    {
        map = (rMap)target;
        selectedEdgeIndex = -1;
        polygonEdges = new List<int>();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Piece))]

public class PieceEditor : Editor
{
    

    Piece piece;
    private void OnSceneGUI()
    {
        Vector3Int origin = MapHelpers.PositionToGrid(piece.transform.position);
        Handles.color = Color.black;
        Handles.DrawSolidDisc(MapHelpers.GridToPosition(origin), Vector3.up, 0.2f);
        
        Vector3Int pos = origin + MapHelpers.North;
        Handles.color = Tile.colors[(int)piece.North];
        Handles.DrawSolidDisc(MapHelpers.GridToPosition(pos), Vector3.up, 0.25f);

        pos = origin + MapHelpers.South;
        Handles.color = Tile.colors[(int)piece.South];
        Handles.DrawSolidDisc(MapHelpers.GridToPosition(pos), Vector3.up, 0.25f);

        pos = origin + MapHelpers.East;
        Handles.color = Tile.colors[(int)piece.East];
        Handles.DrawSolidDisc(MapHelpers.GridToPosition(pos), Vector3.up, 0.25f);

        pos = origin + MapHelpers.West;
        Handles.color = Tile.colors[(int)piece.West];
        Handles.DrawSolidDisc(MapHelpers.GridToPosition(pos), Vector3.up, 0.25f);
        
        foreach(rEdge e in piece.edgesInWorld())
        {
            if(e.isWall)
                Handles.color = Color.magenta;
            else
                Handles.color = Color.red;
            Handles.DrawLine(e.start, e.end,2);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Fix"))
            piece.TransferTypes();
        if(GUILayout.Button("Rotate"))
        {
            piece.transform.Rotate(0, 0, 90);

            
            ConnectionType temp = piece.North;
            piece.North = piece.West;
            piece.West = piece.South;
            piece.South = piece.East;
            piece.East = temp;

        }
        if (GUILayout.Button("Extras"))
            piece.applyExtras();
        if(GUILayout.Button("Replace"))
        {
            Piece newPiece = piece.Replacer;
            Debug.Log(newPiece);
            Vector3Int position = piece.gridPos;
            Debug.Log(position);
            MapGenerator.instance.AddPiece(newPiece, position);
            MapGenerator.instance.RemovePiece(piece);
            piece = null;
        }

        if(GUILayout.Button("FloorMesh"))
        {
            Mesh mesh = piece.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer rend = piece.GetComponent<MeshRenderer>();
            List<Material> mats = new List<Material>();
            rend.GetSharedMaterials(mats);
            int submesh = 0;
            for(int i =0; i<mats.Count; i++)
            {
                Debug.Log(mats[i].name);
                if (mats[i].name.Contains("Floor"))
                {
                    submesh = i;
                    break;
                }
            }


            int[] tris = mesh.GetTriangles(submesh);
            List<Vector3> vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            piece.edges.Clear();

            for (int i = 0; i < tris.Length; i += 3)
            {
                
                addEdge(vertices[tris[i + 0]], vertices[tris[i + 1]]);
                addEdge(vertices[tris[i + 1]], vertices[tris[i + 2]]);
                addEdge(vertices[tris[i + 2]], vertices[tris[i + 0]]);
                //addEdge(fix(vertices[tris[i + 0]]), fix(vertices[tris[i + 1]]));
                //addEdge(fix(vertices[tris[i + 1]]), fix(vertices[tris[i + 2]]));
                //addEdge(fix(vertices[tris[i + 0]]), fix(vertices[tris[i + 2]]));
            }
        }
    }

    public void addEdge(Vector3 start, Vector3 end)
    {
        if (Mathf.Abs(start.x - end.x) > 0.001 && Mathf.Abs(start.y - end.y) > 0.001)
            return;
        rEdge n = new rEdge(start, end);
        foreach(rEdge e in piece.edges)
        {
            if (e.isSame(n))
                return;
        }
        piece.edges.Add(n);
    }

    private void OnEnable()
    {
        piece = (Piece)target;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public List<Tile> tiles = new List<Tile>();
    public int prefabIndex = 0;
    public float Weight = 1;
    public List<PieceWeight> overrideWeights = new List<PieceWeight>();
    public ConnectionType North, South, West, East;
    public ExtraGroup[] extras;
    public Piece Replacer;
    public List<rEdge> edges = new List<rEdge>();
    public Vector3Int gridPos
    {
        get
        {
            return MapHelpers.PositionToGrid(transform.position);
        }
        set
        {
            transform.position = MapHelpers.GridToPosition(value);
        }
    }
    public void TransferTypes()
    {
        North = tiles[0].North;
        South = tiles[0].South;
        West = tiles[0].West;
        East = tiles[0].East;
    }

    
    public List<rEdge> edgesInWorld()
    {
        List<rEdge> worldEdges = new List<rEdge>();
        foreach(rEdge e in edges)
        {
            worldEdges.Add(new rEdge(transform.TransformPoint(e.start), transform.TransformPoint(e.end), e.isWall));
        }
        return worldEdges;
    }
    public void applyExtras()
    {
        foreach(ExtraGroup group in extras)
        {
            if (Random.value < group.probability)
            {
                group.obj.SetActive(true);
                ExtraPieces e;

                if (group.obj.TryGetComponent<ExtraPieces>(out e))
                {
                    e.applyExtras();
                }
            }
            else
                group.obj.SetActive(false);
        }
    }

    /*public List<Vector3Int> getSpots()
    {
        List<Vector3Int> spots = new List<Vector3Int>();
        Vector3Int origin = MapHelpers.PositionToGrid(transform.position);
        foreach (Tile t in tiles)
        {
            Vector3Int pos = origin + t.position;
            if (t.North != ConnectionType.Blocked)
                spots.Add(pos + MapHelpers.North);
            if (t.South != ConnectionType.Blocked)
                spots.Add(pos + MapHelpers.South);
            if (t.West != ConnectionType.Blocked)
                spots.Add(pos + MapHelpers.West);
            if (t.East != ConnectionType.Blocked)
                spots.Add(pos + MapHelpers.East);
        }

        return spots;
    }

    public List<PosTile> getSpots(Vector3Int position)
    {
        List<PosTile> spots = new List<PosTile>();
        Vector3Int origin = position;
        foreach (Tile t in tiles)
        {
            Vector3Int pos = origin + t.position;
            spots.Add(new PosTile(pos,t));
        }

        return spots;
    }

    public struct PosTile
    {
        public Vector3Int position;
        public Tile tile;

        public PosTile(Vector3Int position, Tile tile)
        {
            this.position = position;
            this.tile = tile;
        }
    }*/

    [System.Serializable]
    public class ExtraGroup
    {
        public string Name;
        public GameObject obj;
        public float probability = 0f;
    }
}
public enum ConnectionType { DoorWay, CorridorOpen, Blocked, HallWallNorth, HallWallSouth, HallWallEast, HallWallWest, HallOpen, Free, SmallCorridor};
[System.Serializable]
public class Tile
{
    public Vector3Int position;
    
    public static Color[] colors = { Color.blue, Color.green, Color.red, Color.magenta, Color.magenta, Color.magenta, Color.magenta, Color.cyan , Color.white, Color.gray};
    public ConnectionType North, South, West, East;

    
}

[System.Serializable]
public class PieceWeight
{
    public List<int> prefabIndex = new List<int>();
    public float Weight;
}

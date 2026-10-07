using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour, ISerializationCallbackReceiver
{
    public static MapGenerator instance;

    void Awake()
    {
        instance = this;
    }
    public Piece startPiece;
    public Dictionary<Vector3Int, MapTile> tiles = new Dictionary<Vector3Int, MapTile>();
    public List<Piece> pieces = new List<Piece>();
    public List<Piece> prefabs;
    public int TotalCount = 20;


    List<TileType> Keys = new List<TileType>();
    List<List<Piece>> values = new List<List<Piece>>();
    public Dictionary<TileType, List<Piece>> PieceGroups = new Dictionary<TileType, List<Piece>>();


    public void OnBeforeSerialize()
    {
        Keys.Clear();
        values.Clear();

        foreach(var k in PieceGroups)
        {
            Keys.Add(k.Key);
            values.Add(k.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        PieceGroups = new Dictionary<TileType, List<Piece>>();
        for(int i=0; i<Keys.Count; i++)
        {
            PieceGroups.Add(Keys[i], values[i]);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    //[EditorCools.Button]
    public void setInstance()
    {
        instance = this;
    }
    //[EditorCools.Button]
    public void setIndicies()
    {
        for(int i=0; i<prefabs.Count; i++)
        {
            prefabs[i].GetComponent<Piece>().prefabIndex = i;
        }
    }
    //[EditorCools.Button]
    public void print()
    {
        List<Piece> l = PieceGroups[new TileType(ConnectionType.Free, ConnectionType.Free, ConnectionType.CorridorOpen, ConnectionType.Free)];
        printList(l);
    }

    //[EditorCools.Button]
    public void PrepareGroups()
    {
        PieceGroups.Clear();
        foreach(Piece p in prefabs)
        {
            TileType type = new TileType(p.North, p.South, p.West, p.East);
            Debug.Log(type.ToString());
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(ConnectionType.Free, p.South, p.West, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(p.North, ConnectionType.Free, p.West, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(p.North, p.South, ConnectionType.Free, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(p.North, p.South, p.West, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(ConnectionType.Free, ConnectionType.Free, p.West, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }
            type = new TileType(ConnectionType.Free, p.South, ConnectionType.Free, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }
            type = new TileType(ConnectionType.Free, p.South, p.West, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }



            type = new TileType(p.North, ConnectionType.Free, ConnectionType.Free, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }
            type = new TileType(p.North, ConnectionType.Free, p.West, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }


            type = new TileType(p.North, p.South, ConnectionType.Free, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }


            type = new TileType(p.North, ConnectionType.Free, ConnectionType.Free, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(ConnectionType.Free, p.South, ConnectionType.Free, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(ConnectionType.Free, ConnectionType.Free, p.West, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(ConnectionType.Free, ConnectionType.Free, ConnectionType.Free, p.East);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }

            type = new TileType(ConnectionType.Free, ConnectionType.Free, ConnectionType.Free, ConnectionType.Free);
            if (PieceGroups.ContainsKey(type))
                PieceGroups[type].Add(p);
            else
            {
                PieceGroups[type] = new List<Piece> { p };
            }
        }
        string s = "";
        foreach(TileType t in PieceGroups.Keys)
        {
            s+=t.ToString() + "\n";
            s+=listToString(PieceGroups[t]);
        }
    }

    private class TileTypeEqualityComparer : IEqualityComparer<TileType>
    {
        public bool Equals(TileType t1, TileType t2)
        {
            if (ReferenceEquals(t1, t2))
                return true;
            return t1.North == t2.North && t1.South == t2.South && t1.West == t2.West && t1.East == t2.East;
        }

        public int GetHashCode(TileType t) => (int)t.North + 100 * (int)t.South + 10000*(int)t.West + 1000000*(int)t.East;
    }

    public struct TileType
    {
        public ConnectionType North, South, West, East;

        public TileType(ConnectionType north, ConnectionType south, ConnectionType west, ConnectionType east)
        {
            North = north;
            South = south;
            West = west;
            East = east;
        }
        public override string ToString()
        {
            return $"{North}\t{South}\t{West}\t{East}";
        }
        public override bool Equals(object obj)
        {
            TileType t2 = (TileType)obj;
            return North == t2.North && South == t2.South && West == t2.West && East == t2.East;
        }
        public override int GetHashCode()
        {
            return (int)North + 100 * (int)South + 10000 * (int)West + 1000000 * (int)East;
        }

    }

    public class MapTile
    {
        public bool occupied;
        public ConnectionType North, South, West, East;
        public int lastIndex = 0;

        public MapTile()
        {
            occupied = false;
            North = South = West = East = ConnectionType.Free;
        }

        public override string ToString()
        {
            return $"{North}\t{South}\t{West}\t{East}";
        }

        public TileType toType()
        {
            return new TileType(North, South, West, East);
        }
    }

    public void PreparePosition(Vector3Int pos)
    {
        if (!tiles.ContainsKey(pos))
            tiles.Add(pos, new MapTile());
        if (!tiles.ContainsKey(pos+MapHelpers.North))
            tiles.Add(pos + MapHelpers.North, new MapTile());
        if (!tiles.ContainsKey(pos + MapHelpers.South))
            tiles.Add(pos + MapHelpers.South, new MapTile());
        if (!tiles.ContainsKey(pos + MapHelpers.West))
            tiles.Add(pos + MapHelpers.West, new MapTile());
        if (!tiles.ContainsKey(pos + MapHelpers.East))
            tiles.Add(pos + MapHelpers.East, new MapTile());
    }

    public List<Vector3Int> getFreeSpots(Piece piece, bool prepare = true)
    {
        List<Vector3Int> toReturn = new List<Vector3Int>() ;
        Vector3Int origin = piece.gridPos;
        if(prepare)
            PreparePosition(origin);
        if (piece.North != ConnectionType.Blocked && !tiles[origin+MapHelpers.North].occupied)
            toReturn.Add(origin + MapHelpers.North);
        if (piece.South != ConnectionType.Blocked && !tiles[origin + MapHelpers.South].occupied)
            toReturn.Add(origin + MapHelpers.South);
        if (piece.West != ConnectionType.Blocked && !tiles[origin + MapHelpers.West].occupied)
            toReturn.Add(origin + MapHelpers.West);
        if (piece.East != ConnectionType.Blocked && !tiles[origin + MapHelpers.East].occupied)
            toReturn.Add(origin + MapHelpers.East);
        return toReturn;
    }
    public bool blockWithPiece(Piece piece, bool prepare = true)
    {
        Vector3Int origin = piece.gridPos;
        if(prepare)
            PreparePosition(origin);
        tiles[origin].occupied = true;
        if (tiles[origin + MapHelpers.North].South == ConnectionType.Free)
            tiles[origin + MapHelpers.North].South = piece.North;
        else if (tiles[origin + MapHelpers.North].South != piece.North)
            return false;

        if (tiles[origin + MapHelpers.South].North == ConnectionType.Free)
            tiles[origin + MapHelpers.South].North = piece.South;
        else if (tiles[origin + MapHelpers.South].North != piece.South)
            return false;

        if (tiles[origin + MapHelpers.West].East == ConnectionType.Free)
            tiles[origin + MapHelpers.West].East = piece.West;
        else if (tiles[origin + MapHelpers.West].East != piece.West)
            return false;

        if (tiles[origin + MapHelpers.East].West == ConnectionType.Free)
            tiles[origin + MapHelpers.East].West = piece.East;
        else if (tiles[origin + MapHelpers.East].West != piece.East)
            return false;

        tiles[origin].lastIndex = piece.prefabIndex;
        return true;
    }

    public List<Piece> getPossiblePieces(Vector3Int position)
    {
        MapTile tile = tiles[position];
        TileType t = tile.toType();
        List<Piece> p;
        PieceGroups.TryGetValue(t, out p);
        if (p is null)
            return new List<Piece>();
        return p;
    }

    public int countOpenings(Vector3Int pos, Piece piece)
    {
        int count = 0;

        if (!tiles[pos + MapHelpers.North].occupied && piece.North != ConnectionType.Blocked)
            count++;
        if (!tiles[pos + MapHelpers.South].occupied && piece.South != ConnectionType.Blocked)
            count++;
        if (!tiles[pos + MapHelpers.West].occupied && piece.West != ConnectionType.Blocked)
            count++;
        if (!tiles[pos + MapHelpers.East].occupied && piece.East != ConnectionType.Blocked)
            count++;

        return count;
    }

    public Piece getRandomPiece(List<Piece> pieces, Vector3Int pos, bool notClosing, int lastIndex, int freeSpots)
    {
        if(!notClosing)
        {
            int smallest = 10000;
            int index = 0;
            for(int i=0; i<pieces.Count; i++)
            {
                int openings = countOpenings(pos, pieces[i]);
                if(openings<smallest)
                {
                    smallest = openings;
                    index = i;
                }
            }
            return pieces[index];
        }

        float total = 0;
        foreach (Piece p in pieces)
        {
            if (notClosing && freeSpots <= 1)
                if(countOpenings(pos,p)==0)
                    continue;
            total += getOverrideWeight(p, lastIndex, p.Weight);
        }

        float f = Random.Range(0f, total);
        foreach (Piece p in pieces)
        {
            if (notClosing && freeSpots <= 1)
                if (countOpenings(pos, p)==0)
                    continue;
            float w = getOverrideWeight(p, lastIndex, p.Weight);
            if (f <= w)
                return p;
            f -= w;
        }
        return pieces[pieces.Count - 1];
    }

    public float getOverrideWeight(Piece piece, int lastIndex,float original)
    {
        if (piece.overrideWeights.Count == 0)
            return original;
        foreach(PieceWeight pw in piece.overrideWeights)
        {
            if(pw.prefabIndex.Contains(lastIndex))
            {
                return pw.Weight;
            }
        }

        return original;
    }
    [EditorCools.Button]
    public void Generate()
    {
        setIndicies();
        PrepareGroups();
        for(int fulltries = 0; fulltries < 10000; fulltries++)
        {
            ClearMap();
            tiles = new Dictionary<Vector3Int, MapTile>();
            pieces = new List<Piece>();

            blockWithPiece(startPiece);
            List<Vector3Int> freeSpots = getFreeSpots(startPiece);
            int tries = 0;
            Vector3Int pos;
            List<Piece> possiblePieces;
            bool fail = false;
            string s = "";


            foreach (TileType t in PieceGroups.Keys)
            {
                s += t.ToString() + "\n";
                s += listToString(PieceGroups[t]);
            }
            while (freeSpots.Count > 0 && tries < 10000 && fail==false)
            {
                if (pieces.Count > TotalCount)
                    tries++;
                pos = freeSpots[0];
                freeSpots.RemoveAt(0);
                if (tiles[pos].occupied)
                {
                    continue;
                }
                PreparePosition(pos);
                possiblePieces = new List<Piece>(getPossiblePieces(pos));
                if (possiblePieces.Count == 0)
                {
                    bool contains = PieceGroups.ContainsKey(tiles[pos].toType());
                    if (contains)
                    {
                        List<Piece> l = PieceGroups[tiles[pos].toType()];
                        printList(l);
                    }
                    fail = true;
                    continue;
                }
                Piece piece = getRandomPiece(possiblePieces, pos, pieces.Count < TotalCount, tiles[pos].lastIndex, freeSpots.Count);
                GameObject obj = GameObject.Instantiate(piece.gameObject, transform);
                Piece realPiece = obj.GetComponent<Piece>();
                realPiece.gridPos = pos;
                realPiece.applyExtras();
                pieces.Add(realPiece);
                blockWithPiece(realPiece, false);
                freeSpots.AddRange(getFreeSpots(realPiece));
                possiblePieces.Clear();

            }
            if (pieces.Count < TotalCount)
                fail = true;
            if(!fail)
                return;
        }

        Debug.Log("Failed");
        

        
    }

    public void AddPiece(Piece piece, Vector3Int pos)
    {
        GameObject obj = GameObject.Instantiate(piece.gameObject, transform);
        Piece realPiece = obj.GetComponent<Piece>();
        realPiece.gridPos = pos;
        realPiece.applyExtras();
        pieces.Add(realPiece);
        blockWithPiece(realPiece, false);
    }

    public void RemovePiece(Piece piece)
    {
        pieces.Remove(piece);
        GameObject.DestroyImmediate(piece.gameObject);
    }

    /*public int checkOpenings(Vector3Int position, Piece piece)
    {
        int openings = 0;

        List<Piece.PosTile> spots = piece.getSpots(position);
        
        foreach(Piece.PosTile s in spots)
        {
            if (s.tile.North != ConnectionType.Blocked && !tiles.ContainsKey(s.position + MapHelpers.North))
                openings++;
            if (s.tile.South != ConnectionType.Blocked && !tiles.ContainsKey(s.position + MapHelpers.South))
                openings++;
            if (s.tile.West != ConnectionType.Blocked && !tiles.ContainsKey(s.position + MapHelpers.West))
                openings++;
            if (s.tile.East != ConnectionType.Blocked && !tiles.ContainsKey(s.position + MapHelpers.East))
                openings++;
        }
        return openings;
    }
    public bool tryAddPiece(Vector3Int position, Piece piece)
    {
        List<Piece.PosTile> spots = piece.getSpots(position);
        Debug.Log($"Checking piece: {piece} at position: {position}");
        bool possible = true;
        foreach(Tile t in piece.tiles)
        {
            if (tiles.ContainsKey(position + t.position))
                return false;
        }
        foreach(Piece.PosTile s in spots)
        {

            if(!checkTile(s.position, s.tile.North, Direction.North) || !checkTile(s.position, s.tile.South, Direction.South) || !checkTile(s.position, s.tile.West, Direction.West) || !checkTile(s.position, s.tile.East, Direction.East))
            {
                possible = false;
                break;
            }
        }
        return possible;
    }



    public bool checkTile(Vector3Int position, ConnectionType type, Direction d)
    {
        switch(d)
        {
            case Direction.North:
                if (!tiles.ContainsKey(position + MapHelpers.North))
                    return true;
                else if (tiles[position + MapHelpers.North].South == type)
                    return true;
                break;
            case Direction.South:
                if (!tiles.ContainsKey(position + MapHelpers.South))
                    return true;
                else if (tiles[position + MapHelpers.South].North == type)
                    return true;
                break;
            case Direction.West:
                if (!tiles.ContainsKey(position + MapHelpers.West))
                    return true;
                else if (tiles[position + MapHelpers.West].East == type)
                    return true;
                break;
            case Direction.East:
                if (!tiles.ContainsKey(position + MapHelpers.East))
                    return true;
                else if (tiles[position + MapHelpers.East].West == type)
                    return true;
                break;
        }
        return false;
    }*/
    [EditorCools.Button]
    public void ClearMap()
    {
        foreach (Piece p in pieces)
        {
            GameObject.DestroyImmediate(p.gameObject);
        }
        pieces.Clear();
    }
    public void printList(List<Piece> pieces)
    {
        foreach (Piece p in pieces)
            Debug.Log(p);
    }

    public string listToString(List<Piece> pieces)
    {
        string s = "";
        foreach (Piece p in pieces)
            s += p + "\n";
        return s;
    }


    public enum Direction { North, South,West,East}

    
}

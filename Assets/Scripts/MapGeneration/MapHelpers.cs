using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapHelpers
{
    public static Vector3Int North = new Vector3Int(0, 0, 1);
    public static Vector3Int South = -North;
    public static Vector3Int East = new Vector3Int(1, 0, 0);
    public static Vector3Int West = -East;
    public static Vector3Int PositionToGrid(Vector3 position, float scale = 3)
    {
        return new Vector3Int(floatToInt(position.x+0.5f*scale, scale), floatToInt(position.y + 0.5f * scale, scale), floatToInt(position.z + 0.5f * scale, scale));
    }

    public static Vector3 GridToPosition(Vector3Int position, float scale = 3)
    {
        return new Vector3(position.x, position.y, position.z) * scale;
    }

    public static int floatToInt(float f, float scale = 3)
    {
        return Mathf.FloorToInt(f / scale);
    }

    

    public static List<Vector3Int> AppendList(List<Vector3Int> l1, List<Vector3Int> l2)
    {
        foreach(Vector3Int p in l2)
        {
            if (!l1.Contains(p))
                l1.Add(p);
        }
        return l1;
    }
}

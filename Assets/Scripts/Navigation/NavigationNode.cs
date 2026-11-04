
using UnityEngine;

public class NavigationNode
{
    public int X { get; }
    public int Z { get; }
    public Vector3 Position { get; }
    public bool Walkable { get; }
    public bool IsSafeZone { get; }

    public NavigationNode(
        int x,
        int z,
        Vector3 position,
        bool walkable,
        bool isSafeZone)
    {
        X = x;
        Z = z;
        Position = position;
        Walkable = walkable;
        IsSafeZone = isSafeZone;
    }
}

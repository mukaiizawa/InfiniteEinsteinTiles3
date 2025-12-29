using UnityEngine;

public static class XVector2
{

    public static Vector2 ToVector2(this (float x, float y) p)
    {
        return new Vector2(p.x, p.y);
    }

    public static float CrossProduct(this Vector2 p, Vector2 q)
    {
        return p.x * q.y - p.y * q.x;
    }

    public static bool NearlyEqual(this Vector2 p, Vector2 q)
    {
        return (p - q).sqrMagnitude < GlobalData.Tolerance;
    }

}

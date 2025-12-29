using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Edge
{

    public Vector2 P { get; private set; }
    public Vector2 Q { get; private set; }
    public Vector2 PQ { get; private set; }

    public Edge(Vector2 p, Vector2 q)
    {
        if (p.x < q.x || (Mathf.Abs(p.x - q.x) < GlobalData.Tolerance && p.y < q.y))
        {
            P = p;
            Q = q;
        }
        else
        {
            P = q;
            Q = p;
        }
        PQ = Q - P;
    }

    public Vector2 GetAlignmentVector(Edge e)
    {
        return e.P - this.P;
    }

    public bool NearlyEqual(Edge e)
    {
        return P.NearlyEqual(e.P) && Q.NearlyEqual(e.Q);
    }

    public bool IsIntersect(Edge e)
    {
        return Vector2.Angle(this.PQ, e.PQ) > 10;    // Theoretically, 30 is sufficient.
    }

    public override string ToString()
    {
        return $"[{P}, {Q}]";
    }

}

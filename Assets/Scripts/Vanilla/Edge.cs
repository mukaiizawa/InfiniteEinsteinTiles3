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

    // Strict equality for skipping shared edges during collision detection (post-snap)
    public bool StrictlyEqual(Edge e)
    {
        float tol = 0.0001f;
        return (P - e.P).sqrMagnitude < tol && (Q - e.Q).sqrMagnitude < tol;
    }

    // Whether the edges share a vertex (strict post-snap comparison)
    public bool SharesVertex(Edge e)
    {
        float tol = 0.0001f;
        return (P - e.P).sqrMagnitude < tol
            || (P - e.Q).sqrMagnitude < tol
            || (Q - e.P).sqrMagnitude < tol
            || (Q - e.Q).sqrMagnitude < tol;
    }

    public bool IsIntersect(Edge e)
    {
        float d1 = PQ.CrossProduct(e.P - P);
        float d2 = PQ.CrossProduct(e.Q - P);
        float d3 = e.PQ.CrossProduct(P - e.P);
        float d4 = e.PQ.CrossProduct(Q - e.P);
        return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f))
            && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
    }

    public override string ToString()
    {
        return $"[{P}, {Q}]";
    }

}

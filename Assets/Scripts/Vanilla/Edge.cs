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

    // スナップ用: 寛容な閾値で辺の一致を判定
    public bool NearlyEqual(Edge e)
    {
        return P.NearlyEqual(e.P) && Q.NearlyEqual(e.Q);
    }

    // 衝突判定用: スナップ後の共有辺のみスキップするための厳密な一致判定
    public bool StrictlyEqual(Edge e)
    {
        float tol = 0.0001f;
        return (P - e.P).sqrMagnitude < tol && (Q - e.Q).sqrMagnitude < tol;
    }

    // 頂点を共有しているか（スナップ後の厳密な一致）
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

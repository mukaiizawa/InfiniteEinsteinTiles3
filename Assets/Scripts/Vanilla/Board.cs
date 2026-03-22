using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[System.Serializable]
public class Board
{

    public TileMemory[] PlacedTiles;
    public Color[] ColorPalette;

    public Board()
    {
        this.ColorPalette = Colors.MakeDefaultColorPalette();
    }

    public Board(TileMemory[] tiles, Color[] colors)
    {
        this.PlacedTiles = tiles;
        this.ColorPalette = colors;
    }

    public int PlacedTileCount()
    {
        if (PlacedTiles == null) return 0;
        return PlacedTiles.Length;
    }

    public Edge[] OuterEdges()
    {
        var allEdges = PlacedTiles.SelectMany(t => t.Edges()).ToList();
        var outer = new List<Edge>();
        for (int i = 0; i < allEdges.Count; i++)
        {
            bool shared = false;
            for (int j = 0; j < allEdges.Count; j++)
            {
                if (i == j) continue;
                if (allEdges[i].StrictlyEqual(allEdges[j]))
                {
                    shared = true;
                    break;
                }
            }
            if (!shared) outer.Add(allEdges[i]);
        }
        return outer.ToArray();
    }

}

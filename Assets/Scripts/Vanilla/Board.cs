using System;

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

}

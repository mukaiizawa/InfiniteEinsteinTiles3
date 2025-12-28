using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using static Colors;

[System.Serializable]
public class TileMemory
{

    public Vector2 Position;
    public int Rotation;    // [0, 11]
    public Color Color;

    public TileMemory()
    {
        this.Position = Vector2.zero;
        this.Rotation = 0;
        this.Color = Colors.Tile;
    }

    public TileMemory(Vector2 pos, Color color) : this()
    {
        this.Position = pos;
        this.Color = color;
    }

    public TileMemory CopyFrom(TileMemory memory)
    {
        this.Position = new Vector2(memory.Position.x, memory.Position.y);
        this.Rotation = memory.Rotation;
        this.Color = memory.Color;
        return this;
    }

    public override string ToString()
    {
        return Position + "," + Rotation;
    }

}

# Architecture: Tile Placement, Collision Detection & Puzzle Completion

This document describes the algorithms for vertex snapping, collision detection, and puzzle completion checking.

## Placement Flow Overview

```
User places tile(s)
  → PutTiles(tiles)
    → First tile (and no frame): no snapping or collision check (free placement)
    → Otherwise:
        1. TryVertexSnap()  — vertex snapping (finds closest vertex pair within tolerance)
           In Puzzle mode, frame outer-edge vertices are also snap targets.
        2. Per-tile collision filtering — each tile is individually checked via HasCollision()
           Tiles that pass are placed; tiles that collide are discarded.
        → If at least one tile can be placed, placement is confirmed and pushed to the history stack
        → If no tile can be placed, the entire placement is rejected
    → In Puzzle mode, after placement, IsPuzzleSolved() checks completion
```

## 1. Vertex Snapping (TryVertexSnap)

**Purpose**: Compute an offset `dr` that snaps a new tile's vertex to the nearest existing vertex.

**Algorithm**:
1. Collect all snap-target vertices: vertices of all existing placed tiles. In Puzzle mode, vertices of the answer board's outer edges (`_answerOuterEdges`) are also included.
2. For each vertex of each new tile, compute the squared distance to every snap-target vertex.
3. Track the closest pair. If the closest distance is within `GlobalData.Tolerance = 0.31f`, return the offset `dr = targetVertex - newVertex`.
4. The caller applies `dr` to all tiles in the group, then individually checks each for collision via `HasCollision()`.

## 2. Collision Detection (HasCollision)

**Purpose**: Determine whether the newly placed tile overlaps with any existing tiles or crosses the puzzle frame boundary.

**Early Skip**: If the squared distance between the positions of a tile pair is >= `CollisionDistSq = 12f` (roughly twice the circumscribed circle radius of a tile), the pair cannot collide and is skipped.

**Multi-stage Detection** (Stages 1-3 are executed for each existing tile; Stage 4 is executed once):

### Stage 1: Position Coincidence Detection
If the squared distance between positions is `< 0.01f`, the tiles are treated as overlapping (covers both exact duplicates and different-rotation overlaps).

### Stage 2: Edge Intersection Test
Check whether any edges of the new tile intersect with edges of the existing tile.
- Skip shared edges (edges common to adjacent tiles) via `Edge.StrictlyEqual()`
- Skip edges that share a vertex via `Edge.SharesVertex()` (vertex-touching between adjacent tiles is not an intersection)
- Test remaining edge pairs for intersection using `Edge.IsIntersect()`, which is cross-product-based

**Edge.IsIntersect implementation**:
```
d1 = PQ x (e.P - P)
d2 = PQ x (e.Q - P)
d3 = e.PQ x (P - e.P)
d4 = e.PQ x (Q - e.P)
Intersection iff d1 and d2 have opposite signs AND d3 and d4 have opposite signs
```
When an endpoint lies exactly on the other segment (d = 0), it is not counted as an intersection (safely excludes shared-vertex cases).

### Stage 3: Interior Point Containment Test
Complements the edge intersection test for cases it cannot detect — specifically when the tile is a concave polygon and one tile fits entirely within the concavity of another.

- For each vertex, generate a sample point offset slightly inward (`inset = 0.05f`) toward the tile's position.
- Test whether that sample point lies inside the other tile using ray casting.
- This check is performed in both directions (new -> existing and existing -> new).

**TileMemory.ContainsPoint (Ray Casting)**:
For each edge of the polygon, count how many times a rightward ray from the test point crosses the edge. An odd count means the point is inside.

### Stage 4: Frame Boundary Check (Puzzle mode only)
If `_answerOuterEdges` is set, check whether any edge of the new tile intersects with any outer edge of the puzzle frame. Uses the same `StrictlyEqual`/`SharesVertex`/`IsIntersect` logic as Stage 2. This prevents tiles from being placed outside the puzzle boundary.

## 3. Puzzle Completion (IsPuzzleSolved)

**Purpose**: Determine whether the player has completed the puzzle.

**Outer Edges**: The answer board's outer edges are edges that belong to exactly one tile in `_answerBoard.PlacedTiles` (i.e., not shared between two adjacent answer tiles). These form the perimeter of the target shape. Computed by `Board.OuterEdges()` and cached in `_answerOuterEdges` at puzzle load time.

**Completion Check**: The puzzle is solved when every outer edge of the answer board is matched by an edge of the player's placed tiles (via `Edge.StrictlyEqual()`). This means the player's tiling completely fills the target shape's boundary.

## 4. Edge Method Summary

| Method | Threshold | Usage |
|---|---|---|
| `StrictlyEqual` | `0.0001f` | Shared-edge skipping during collision detection; puzzle completion matching |
| `SharesVertex` | `0.0001f` | Shared-vertex skipping during collision detection |
| `IsIntersect` | (none) | Determined solely by cross-product signs. Endpoint coincidence is not treated as intersection |

## 5. Tile Shape Data

- 14-vertex spectre-shaped polygon (`TileMemory.VerticesTable`)
- Vertex coordinates are precomputed for all 12 rotations (30 degree increments)
- `EdgeCount = 14` (equal to the vertex count; the polygon is closed, so edge count = vertex count)
- `Edges()`: returns the 14 edges (used for snapping, collision detection, and puzzle completion)
- `Vertices()`: vertex array in world coordinates

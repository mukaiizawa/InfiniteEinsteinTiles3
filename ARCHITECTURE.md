# Architecture: Tile Placement & Collision Detection

This document describes the algorithms for edge-snapping alignment and collision detection when placing tiles.

## Placement Flow Overview

```
User places tile(s)
  → UpdateBoardWithHistory(Action.Put, tiles)
    → First tile: no snapping or collision check (free placement)
    → Subsequent tiles:
        1. FindAlignment()  — edge snapping (checks only the snapping tile for collision)
        2. Per-tile collision filtering — each tile is individually checked via HasCollisionSingle()
           Tiles that pass are placed; tiles that collide are discarded.
           Already-accepted tiles are added to existingMems so subsequent tiles
           in the same group are also checked against them.
        → If at least one tile can be placed, placement is confirmed and pushed to the history stack
        → If no tile can be placed, the entire placement is rejected
```

## 1. Edge Snapping (FindAlignment)

**Purpose**: Compute an offset `dr` that snaps a new tile flush against an existing tile's edge.

**Algorithm**:
1. Enumerate all edges of each new tile in the group.
2. For each edge, compare against every edge of every existing tile using `Edge.NearlyEqual()` (with a lenient threshold of `GlobalData.Tolerance = 0.31f`).
3. When a matching edge pair is found, compute the offset `dr` via `Edge.GetAlignmentVector()`.
4. Call `HasCollisionSingle()` for the snapping tile only (the tile whose edge matched) at the tentative position (original position + `dr`):
   - No collision → adopt this offset and return `true`.
   - Collision → try the next edge pair.
5. If no valid placement is found across all edge pairs, return `false` (placement rejected).

**Note**: `FindAlignment` only validates the snapping tile itself. The caller (`UpdateBoardWithHistory`) then individually checks each remaining tile in the group via `HasCollisionSingle()`, placing only the non-colliding subset.

**Edge.NearlyEqual**: Two edges are considered matching if their respective endpoints P and Q are each within `Tolerance`. The `Edge` constructor canonicalizes endpoints so that P.x ≤ Q.x, ensuring a unique orientation for comparison.

## 2. Collision Detection (HasCollision)

**Purpose**: Determine whether the newly placed tile(s) overlap with any existing tiles after snapping.

**Early Skip**: If the squared distance between the centroids of a tile pair is ≥ `CollisionDistSq = 12f` (roughly twice the circumscribed circle radius of a tile), the pair cannot collide and is skipped.

**Multi-stage Detection** (executed sequentially for each tile pair):

### Stage 1: Exact Duplicate Detection
Same `Position` and same `Rotation` → the tiles are identical, so this is a collision.

### Stage 2: Centroid Coincidence Detection
If the squared distance between centroids is `< 0.01f` → the tiles are treated as overlapping, even if their rotations differ.

### Stage 3: Edge Intersection Test
Check whether any edges of the new tile intersect with edges of the existing tile.
- Skip shared edges (edges common to adjacent tiles) via `Edge.StrictlyEqual()`
- Skip edges that share a vertex via `Edge.SharesVertex()` (vertex-touching between adjacent tiles is not an intersection)
- Test remaining edge pairs for intersection using `Edge.IsIntersect()`, which is cross-product-based

**Edge.IsIntersect implementation**:
```
d1 = PQ × (e.P - P)
d2 = PQ × (e.Q - P)
d3 = e.PQ × (P - e.P)
d4 = e.PQ × (Q - e.P)
Intersection ⟺ d1 and d2 have opposite signs AND d3 and d4 have opposite signs
```
When an endpoint lies exactly on the other segment (d = 0), it is not counted as an intersection (safely excludes shared-vertex cases).

### Stage 4: Interior Point Containment Test
Complements the edge intersection test for cases it cannot detect — specifically when the tile is a concave polygon and one tile fits entirely within the concavity of another.

- For each vertex, generate a sample point offset slightly inward (`inset = 0.05f`) toward the centroid.
- Test whether that sample point lies inside the other tile using ray casting.
- This check is performed in both directions (new → existing and existing → new).

**TileMemory.ContainsPoint (Ray Casting)**:
For each edge of the polygon, count how many times a rightward ray from the test point crosses the edge. An odd count means the point is inside.

## 3. Edge Method Summary

| Method | Threshold | Usage |
|---|---|---|
| `NearlyEqual` | `Tolerance (0.31f)` | Edge matching during snapping. Absorbs user drag imprecision |
| `StrictlyEqual` | `0.0001f` | Shared-edge skipping during collision detection. High precision since snapping has already been applied |
| `SharesVertex` | `0.0001f` | Shared-vertex skipping during collision detection |
| `IsIntersect` | (none) | Determined solely by cross-product signs. Endpoint coincidence is not treated as intersection |

## 4. Tile Shape Data

- 14-vertex hat-shaped polygon (`TileMemory.VerticesTable`)
- Vertex coordinates are precomputed for all 12 rotations (30° increments)
- `EdgeCount = 14` (equal to the vertex count; the polygon is closed, so edge count = vertex count)
- `Edges()`: returns the 14 edges (used for both snapping and collision detection)
- `Centroid()`: arithmetic mean of all vertices
- `Vertices()`: vertex array in world coordinates

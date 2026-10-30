using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Pathfinder : MonoBehaviour
{
    static public Pathfinder S;     // One per scene; guards reach it through here

    [Header("Inscribed")]
    public Vector2 gridSize = new Vector2(40f, 30f);   // World area covered, centered on this object
    public float nodeSize = 0.5f;        // Smaller = more precise but slower
    public float agentRadius = 0.55f;  // Clearance a guard needs from walls
    public LayerMask obstacleMask;     // Layers guards can't walk through (Walls)
    public bool showGizmos = true;    // Shows the grid in the Scene view

    private bool[,] walkable;
    private int gridW, gridH;
    private Vector2 origin;  // World position of node (0,0)

    void Awake()
    {
        S = this;
    }

    void Start()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        Physics2D.SyncTransforms();

        gridW = Mathf.Max(1, Mathf.RoundToInt(gridSize.x / nodeSize));
        gridH = Mathf.Max(1, Mathf.RoundToInt(gridSize.y / nodeSize));
        origin = (Vector2)transform.position
                 - new Vector2(gridW, gridH) * nodeSize * 0.5f
                 + Vector2.one * nodeSize * 0.5f;

        walkable = new bool[gridW, gridH];
        for (int x = 0; x < gridW; x++)
        {
            for (int y = 0; y < gridH; y++)
            {
                // A node is blocked if a guard standing there would overlap a wall
                walkable[x, y] = Physics2D.OverlapCircle(NodeToWorld(x, y), agentRadius, obstacleMask) == null;
            }
        }
    }

    Vector2 NodeToWorld(int x, int y)
    {
        return origin + new Vector2(x, y) * nodeSize;
    }

    Vector2Int WorldToNode(Vector2 p)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((p.x - origin.x) / nodeSize), 0, gridW - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((p.y - origin.y) / nodeSize), 0, gridH - 1);
        return new Vector2Int(x, y);
    }

    int Idx(Vector2Int n)
    {
        return n.y * gridW + n.x;
    }

    // If a point sits inside a blocked cell (e.g., hugging a wall), find a nearby open one
    Vector2Int NearestWalkable(Vector2Int n)
    {
        if (walkable[n.x, n.y]) return n;

        for (int r = 1; r <= 8; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;   // Ring cells only
                    int nx = n.x + dx;
                    int ny = n.y + dy;
                    if (nx < 0 || ny < 0 || nx >= gridW || ny >= gridH) continue;
                    if (walkable[nx, ny]) return new Vector2Int(nx, ny);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    int Heuristic(int a, int b)
    {
        int dx = Mathf.Abs(a % gridW - b % gridW);
        int dy = Mathf.Abs(a / gridW - b / gridW);
        return 14 * Mathf.Min(dx, dy) + 10 * Mathf.Abs(dx - dy);
    }

    // Returns the points to walk through, or null if no route exists
    public List<Vector2> FindPath(Vector2 startWorld, Vector2 endWorld)
    {
        if (walkable == null) BuildGrid();

        Vector2Int start = NearestWalkable(WorldToNode(startWorld));
        Vector2Int end = NearestWalkable(WorldToNode(endWorld));
        if (start.x < 0 || end.x < 0) return null;

        int total = gridW * gridH;
        int[] gCost = new int[total];
        int[] parent = new int[total];
        bool[] closed = new bool[total];
        for (int i = 0; i < total; i++)
        {
            gCost[i] = int.MaxValue;
            parent[i] = -1;
        }

        int startI = Idx(start);
        int endI = Idx(end);
        gCost[startI] = 0;
        List<int> open = new List<int> { startI };

        while (open.Count > 0)
        {
            // Take the open node with the lowest total estimated cost
            int bestK = 0;
            int bestF = int.MaxValue;
            for (int k = 0; k < open.Count; k++)
            {
                int f = gCost[open[k]] + Heuristic(open[k], endI);
                if (f < bestF)
                {
                    bestF = f;
                    bestK = k;
                }
            }
            int cur = open[bestK];
            open[bestK] = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);

            if (cur == endI) return BuildPath(parent, cur, startWorld);

            closed[cur] = true;
            int cx = cur % gridW;
            int cy = cur / gridW;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= gridW || ny >= gridH) continue;
                    if (!walkable[nx, ny]) continue;

                    // Don't cut diagonally across a wall corner
                    if (dx != 0 && dy != 0 && (!walkable[cx + dx, cy] || !walkable[cx, cy + dy])) continue;

                    int ni = ny * gridW + nx;
                    if (closed[ni]) continue;

                    int newCost = gCost[cur] + ((dx != 0 && dy != 0) ? 14 : 10);
                    if (newCost < gCost[ni])
                    {
                        bool alreadyOpen = gCost[ni] != int.MaxValue;
                        gCost[ni] = newCost;
                        parent[ni] = cur;
                        if (!alreadyOpen) open.Add(ni);
                    }
                }
            }
        }
        return null;    // Nowhere to go
    }

    List<Vector2> BuildPath(int[] parent, int endI, Vector2 startWorld)
    {
        List<Vector2> nodes = new List<Vector2>();
        for (int i = endI; i != -1; i = parent[i])
        {
            nodes.Add(NodeToWorld(i % gridW, i / gridW));
        }
        nodes.Reverse();
        nodes[0] = startWorld; // Start from the guard's real position
        return Smooth(nodes);
    }

    // Skips unnecessary points so guards walk straight lines instead of zigzagging 
    List<Vector2> Smooth(List<Vector2> nodes)
    {
        if (nodes == null || nodes.Count <= 2) return nodes;

        List<Vector2> result = new List<Vector2>();
        result.Add(nodes[0]);
        int anchor = 0;

        while (anchor < nodes.Count - 1)
        {
            int farthest = anchor + 1;
            for (int i = nodes.Count - 1; i > anchor + 1; i--)
            {
                if (LineIsClear(nodes[anchor], nodes[i]))
                {
                    farthest = i;
                    break;
                }
            }
            result.Add(nodes[farthest]);
            anchor = farthest;
        }
        return result;
    }

    bool LineIsClear(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        float dist = d.magnitude;
        if (dist < 0.001f) return true;

        // Use a conservative radius check so rays next to wall padding don't hit at distance 0
        RaycastHit2D hit = Physics2D.CircleCast(a, agentRadius * 0.4f, d / dist, dist, obstacleMask);
        return hit.collider == null;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, gridSize.y, 0f));

        if (walkable == null) return;

        // Red squares show where guards can't walk
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        for (int x = 0; x < gridW; x++)
        {
            for (int y = 0; y < gridH; y++)
            {
                if (!walkable[x, y])
                {
                    Gizmos.DrawCube(NodeToWorld(x, y), Vector3.one * nodeSize * 0.9f);
                }
            }
        }
    }
}
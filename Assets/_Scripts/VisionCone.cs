using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [Header("Inscribed")]
    public float viewDistance = 5f;
    [Range(10f, 180f)]
    public float viewAngle = 60f;           // Total width of the visible cone (degrees)
    public float trackDistance = 9f;        // Invisible radius: an engaged guard keeps tracking the player inside this
    public LayerMask obstacleMask;          // Layers that block sight (walls, fences)
    public int meshSegments = 20;           // Higher = smoother cone edge
    public Color patrolColor = new Color(1f, 1f, 0f, 0.25f);    // Yellow
    public Color alertColor = new Color(1f, 0.5f, 0f, 0.35f);  // Orange
    public Color chaseColor = new Color(1f, 0f, 0f, 0.4f);     // Red
    public Color searchColor = new Color(1f, 0.7f, 0f, 0.3f);   // Amber

    [Header("Dynamic")]
    public bool canSeePlayer = false;       // Inside the visible cone: starts chase
    public bool canTrackPlayer = false;     // Also true in the wider invisible zone: keeps chase going

    private PolicePatrol patrol;
    private Transform player;
    private PlayerHide playerHide;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    void Start()
    {
        patrol = GetComponent<PolicePatrol>();

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHide = playerGO.GetComponent<PlayerHide>();
        }

        // Build a child object that holds the visible cone mesh
        GameObject coneGO = new GameObject("VisionConeMesh");
        coneGO.transform.SetParent(transform, false);
        MeshFilter meshFilter = coneGO.AddComponent<MeshFilter>();
        meshRenderer = coneGO.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.sortingOrder = 1;      // Draw on top of the floor
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    void Update()
    {
        canSeePlayer = CheckVisibleCone();

        // A guard who is already engaged also tracks the player in the wider invisible zone
        bool engaged = patrol.state == PolicePatrol.eState.alert
                    || patrol.state == PolicePatrol.eState.chase
                    || patrol.state == PolicePatrol.eState.checkBush;
        canTrackPlayer = canSeePlayer || (engaged && CheckTrackZone());

        // The cone's color shows what the guard is currently doing
        Color c = patrolColor;
        switch (patrol.state)
        {
            case PolicePatrol.eState.alert: c = alertColor; break;
            case PolicePatrol.eState.chase: c = chaseColor; break;
            case PolicePatrol.eState.checkBush: c = chaseColor; break;
            case PolicePatrol.eState.search: c = searchColor; break;
        }
        meshRenderer.material.color = c;

        BuildMesh();
    }

    bool PlayerHiddenOrMissing()
    {
        return player == null || (playerHide != null && playerHide.isHidden);
    }

    bool HasLineOfSight(Vector2 toPlayer, float dist)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, dist, obstacleMask);
        return hit.collider == null;
    }

    // The visible cone: distance, angle, and no wall in the way
    bool CheckVisibleCone()
    {
        if (PlayerHiddenOrMissing()) return false;

        Vector2 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist > viewDistance) return false;
        if (Vector2.Angle(patrol.facing, toPlayer) > viewAngle * 0.5f) return false;

        return HasLineOfSight(toPlayer, dist);
    }

    // The invisible zone: any direction within trackDistance, as long as no wall is in the way
    bool CheckTrackZone()
    {
        if (PlayerHiddenOrMissing()) return false;

        Vector2 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist > trackDistance) return false;

        return HasLineOfSight(toPlayer, dist);
    }

    void BuildMesh()
    {
        Vector3[] verts = new Vector3[meshSegments + 2];
        int[] tris = new int[meshSegments * 3];

        verts[0] = Vector3.zero;            // The tip of the cone is the guard

        // Local cone calculation relative to the guard's transform direction
        float startAngle = -viewAngle * 0.5f;
        float step = viewAngle / meshSegments;
        Vector3 s = transform.lossyScale;   // Cancel out the guard's scale so the cone stays true size

        for (int i = 0; i <= meshSegments; i++)
        {
            float localAngleDeg = startAngle + step * i;
            float a = localAngleDeg * Mathf.Deg2Rad;

            // Raycast in local angle converted to world direction for wall clipping
            Vector3 localDir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector3 worldDir = transform.TransformDirection(localDir);

            RaycastHit2D hit = Physics2D.Raycast(transform.position, worldDir, viewDistance, obstacleMask);
            float dist = (hit.collider != null) ? hit.distance : viewDistance;

            verts[i + 1] = new Vector3(
                Mathf.Cos(a) * dist / s.x,
                Mathf.Sin(a) * dist / s.y,
                0f);
        }

        for (int i = 0; i < meshSegments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 2;
            tris[i * 3 + 2] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
    }

    // Shows the invisible tracking radius in the Scene view when the guard is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, trackDistance);
    }
}
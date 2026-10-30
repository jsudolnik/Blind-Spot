using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    // Tracks current state of the camera object
    public enum eState { intact, beingCut, destroyed }

    [Header("Inscribed")]
    public float cutTime = 2f;                  // Seconds the player must hold E to disable the camera
    public Color intactColor = Color.red;       // Color tint when active
    public Color destroyedColor = Color.gray;   // Color tint when disabled

    [Header("Audio")]
    public AudioClip alarmSound;                // Sound played when camera spots the player

    [Header("Aiming (how the art turns)")]
    public Transform barrel;                    // Visual camera head transform
    public Transform muzzle;                    // Muzzle/lens position for vision origin
    public float muzzleDistance = 0.6f;          // Fallback origin offset if no muzzle transform assigned
    public float barrelArtOffset = 0f;          

    [Header("Vision")]
    public float facingAngle = 270f;            // Base direction
    public float viewDistance = 5f;             // Max distance vision cone reaches
    [Range(10f, 180f)]
    public float viewAngle = 45f;               // Field of view in degrees
    public float sweepAngle = 0f;               // How far back and forth the camera rotates
    public float sweepSpeed = 1f;               // Speed of back-and-forth panning
    public float sweepOffset = 0f;               // Panning offset timer
    public LayerMask obstacleMask;             // Layers that block camera vision (walls, obstacles)
    public int meshSegments = 20;               // Smoothness of vision cone rendering
    public Color idleConeColor = new Color(1f, 0.2f, 0.2f, 0.2f);   // Idle vision cone color
    public Color spottedConeColor = new Color(1f, 0f, 0f, 0.5f);     // Vision cone color when player detected

    [Header("Alert")]
    public float alertInterval = 3f;            // Cooldown between repeating alarm sound/guard alerts

    [Header("Dynamic")]
    public eState state = eState.intact;
    public float cutProgress = 0f;
    public bool canSeePlayer = false;
    public float currentAngle = 0f;

    private bool playerInRange = false;
    private SpriteRenderer[] sprites;
    private GameManager gm;
    private Transform player;
    private PlayerHide playerHide;
    private GameObject coneObject;
    private MeshRenderer coneRenderer;
    private Mesh coneMesh;
    private float alertTimer = 0f;

    Transform Aimer
    {
        get { return (barrel != null && barrel != transform) ? barrel : transform; } // Helper property to determine transform to rotate
    }

    void Start()
    {
        // Cache visual renders and references
        sprites = GetComponentsInChildren<SpriteRenderer>();
        SetColor(intactColor);
        gm = FindFirstObjectByType<GameManager>();

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHide = playerGO.GetComponent<PlayerHide>();
        }

        coneObject = new GameObject("CameraCone_" + name);
        MeshFilter meshFilter = coneObject.AddComponent<MeshFilter>();
        coneRenderer = coneObject.AddComponent<MeshRenderer>();
        coneRenderer.material = new Material(Shader.Find("Sprites/Default"));  // Dynamically create a runtime GameObject for rendering the 2D vision cone mesh
        coneRenderer.sortingOrder = 1;
        coneMesh = new Mesh();
        meshFilter.mesh = coneMesh;
    }

    void OnDestroy()
    {
        // Clean up procedural cone mesh object when destroyed
        if (coneObject != null) Destroy(coneObject);
    }

    void Update()
    {
        // Stop processing vision and interactions if destroyed
        if (state == eState.destroyed) return;

        UpdateVision();
        UpdateCutting();
    }

    // Applies target color to all sprite renderers attached to the camera
    void SetColor(Color c)
    {
        foreach (SpriteRenderer s in sprites) s.color = c;
    }

    // Calculates start origin point of the vision cone
    Vector2 ConeOriginAt(float angleDeg)
    {
        if (muzzle != null) return muzzle.position;

        float a = angleDeg * Mathf.Deg2Rad;
        return (Vector2)Aimer.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * muzzleDistance;
    }

    // Rotates camera barrel, calculates vision checks, and updates procedural mesh
    void UpdateVision()
    {
        // Calculate panning movement over time
        currentAngle = facingAngle + Mathf.Sin(Time.time * sweepSpeed + sweepOffset) * sweepAngle;

        Aimer.rotation = Quaternion.Euler(0f, 0f, currentAngle + barrelArtOffset);

        Vector2 origin = ConeOriginAt(currentAngle);
        coneObject.transform.position = new Vector3(origin.x, origin.y, transform.position.z);

        // Perform line-of-sight check
        canSeePlayer = CheckSeesPlayer(origin);
        coneRenderer.material.color = canSeePlayer ? spottedConeColor : idleConeColor;
        BuildMesh();

        // Handle alarm triggering and dispatching nearest guard
        if (alertTimer > 0f) alertTimer -= Time.deltaTime;
        if (canSeePlayer && alertTimer <= 0f)
        {
            if (alarmSound != null)
            {
                AudioSource.PlayClipAtPoint(alarmSound, transform.position, 0.9f);
            }

            AlertNearestGuard();
            alertTimer = alertInterval;
        }
    }

    // Evaluates distance, angle cone, and raycast collisions to see player
    bool CheckSeesPlayer(Vector2 origin)
    {
        if (player == null) return false;
        if (playerHide != null && playerHide.isHidden) return false;

        Vector2 toPlayer = (Vector2)player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        float a = currentAngle * Mathf.Deg2Rad;
        Vector2 facing = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        if (Vector2.Angle(facing, toPlayer) > viewAngle * 0.5f) return false;

        // Verify line-of-sight is clear of walls
        RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, dist, obstacleMask);
        return hit.collider == null;
    }

    // Finds closest police guard and alerts them to player's position
    void AlertNearestGuard()
    {
        PolicePatrol[] guards = FindObjectsByType<PolicePatrol>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PolicePatrol nearest = null;
        float best = float.MaxValue;

        foreach (PolicePatrol g in guards)
        {
            float d = Vector2.Distance(g.transform.position, player.position);
            if (d < best)
            {
                best = d;
                nearest = g;
            }
        }

        if (nearest != null) nearest.AlertTo(player.position);
    }

    // Handles holding Left Mouse Button to cut camera wires
    void UpdateCutting()
    {
        if (playerInRange && Input.GetMouseButton(0)) // 0 = Left Mouse Button
        {
            state = eState.beingCut;
            cutProgress += Time.deltaTime;
            SetColor(Color.Lerp(intactColor, destroyedColor, cutProgress / cutTime));

            if (cutProgress >= cutTime) DestroyCamera();
        }
        else if (state == eState.beingCut)
        {
            // Reset progress if player lets go of Left Mouse Button prematurely
            state = eState.intact;
            cutProgress = 0f;
            SetColor(intactColor);
        }
    }

    // Sets state to destroyed and notifies GameManager
    void DestroyCamera()
    {
        state = eState.destroyed;
        SetColor(destroyedColor);
        canSeePlayer = false;
        coneRenderer.enabled = false;
        if (gm != null) gm.CameraDestroyed();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    
    void BuildMesh()
    {
        Vector3[] verts = new Vector3[meshSegments + 2];
        int[] tris = new int[meshSegments * 3];

        verts[0] = Vector3.zero;

        float startAngle = currentAngle - viewAngle * 0.5f; // Generates vision cone arc mesh dynamically each frame
        float step = viewAngle / meshSegments;

        for (int i = 0; i <= meshSegments; i++)
        {
            float a = (startAngle + step * i) * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * viewDistance, Mathf.Sin(a) * viewDistance, 0f);
        }

        for (int i = 0; i < meshSegments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 2;
            tris[i * 3 + 2] = i + 1;
        }

        coneMesh.Clear();
        coneMesh.vertices = verts;
        coneMesh.triangles = tris;
    }

    void OnValidate()
    {
        // Align rotation in scene view when adjusting angle fields in Editor
        Aimer.rotation = Quaternion.Euler(0f, 0f, facingAngle + barrelArtOffset);
    }
}
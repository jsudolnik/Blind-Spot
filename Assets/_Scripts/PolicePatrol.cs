using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolicePatrol : MonoBehaviour
{
    // States for guard AI decision tree
    public enum eState { patrol, alert, chase, search, checkBush }

    [Header("Inscribed")]
    public Transform[] waypoints;           // Patrol path nodes
    public float patrolSpeed = 2f;          // Movement speed while patrolling
    public float chaseSpeed = 4f;           // Speed when chasing player
    public float waitTime = 1f;             // Pause duration at each waypoint
    public float arriveDistance = 0.1f;     // Distance threshold to consider waypoint reached
    public float alertTime = 0.5f;          // Reaction delay before chase begins
    public float loseSightTime = 3f;        // How long guard pursues last known position after losing sight
    public float searchTime = 4f;           // How long guard spins/searches before returning to patrol
    public float searchLookSpeed = 90f;     // Rotation speed during search state (deg/sec)
    public float bushReactionTime = 0.5f;   // Grace period to check bush if player hides while being chased
    public float bushCatchDistance = 0.8f;  // Distance required to catch player in bush

    [Header("Audio")]
    public AudioClip alertSound;            // Guard reaction sound when spotting player

    [Header("Pathfinding")]
    public float repathInterval = 0.25f;    // Cooldown between recalculating a path
    public float repathDistance = 0.5f;     // Target distance change needed to force repath
    public float pathPointArrive = 0.15f;   // Threshold to advance to next waypoint node in path

    [Header("Dynamic")]
    public eState state = eState.patrol;
    public int currentWaypoint = 0;
    public Vector2 facing = Vector2.down;
    public Vector2 lastKnownPos;

    private Rigidbody2D rb;
    private VisionCone cone;
    private Transform player;
    private PlayerHide playerHide;
    private GameManager gm;
    private Vector2 bushPos;
    private float waitTimer = 0f;
    private float stateTimer = 0f;

    private eState prevState = eState.patrol;
    private List<Vector2> path;
    private int pathIndex = 0;
    private Vector2 pathTarget;
    private float repathTimer = 0f;

    private Vector2 spawnPos;
    private Vector2 spawnFacing;

    void Start()
    {
        // Cache components and starting positions
        rb = GetComponent<Rigidbody2D>();
        cone = GetComponent<VisionCone>();
        gm = FindFirstObjectByType<GameManager>();

        spawnPos = transform.position;
        spawnFacing = facing;
        lastKnownPos = transform.position;

        // Locate player and stealth components in scene
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHide = playerGO.GetComponent<PlayerHide>();
        }
    }

    // Returns whether the attached vision cone currently detects the player
    public bool SeesPlayer()
    {
        return cone != null && cone.canTrackPlayer;
    }

    // Directs guard to move toward and inspect a specific bush
    public void InvestigateBush(Vector2 pos)
    {
        bushPos = pos;
        state = eState.checkBush;
    }

    // External alert (e.g., from security camera) sending guard to investigate position
    public void AlertTo(Vector2 pos)
    {
        lastKnownPos = pos;
        if (state == eState.patrol || state == eState.search)
        {
            state = eState.chase;
            stateTimer = loseSightTime + Vector2.Distance(transform.position, pos) / chaseSpeed + 1f;
        }
    }

    void FixedUpdate()
    {
        // Handle state change triggers (audio cues, music registration, resetting paths)
        if (state != prevState)
        {
            // Play reaction audio when entering alert state
            if (state == eState.alert && alertSound != null)
            {
                AudioSource.PlayClipAtPoint(alertSound, transform.position, 0.8f);
            }

            // Register/Unregister guard with global MusicManager for dynamic chase music
            if (MusicManager.S != null)
            {
                bool wasChasing = (prevState == eState.chase || prevState == eState.checkBush);
                bool isChasing = (state == eState.chase || state == eState.checkBush);
                if (wasChasing != isChasing)
                {
                    MusicManager.S.RegisterChase(this, isChasing);
                }
            }

            path = null;
            prevState = state;
        }

        // Continually update last known player position if within line of sight
        bool sees = SeesPlayer();
        if (sees && player != null) lastKnownPos = player.position;

        // Execute active state behavior
        switch (state)
        {
            case eState.patrol: DoPatrol(sees); break;
            case eState.alert: DoAlert(sees); break;
            case eState.chase: DoChase(sees); break;
            case eState.search: DoSearch(sees); break;
            case eState.checkBush: DoCheckBush(sees); break;
        }

        // Update physics rotation and forward vector based on current movement direction
        if (facing.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
            transform.right = facing;
        }
    }

    // Patrol logic: moves between waypoints or stays at spawn post if no waypoints exist
    void DoPatrol(bool sees)
    {
        if (sees)
        {
            state = eState.alert;
            stateTimer = alertTime;
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            float distToPost = MoveTo(spawnPos, patrolSpeed);
            if (distToPost <= arriveDistance) facing = spawnFacing;
            return;
        }

        // Find next valid non-null waypoint
        int safetyCounter = 0;
        while (waypoints[currentWaypoint] == null && safetyCounter < waypoints.Length)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            safetyCounter++;
        }
        if (waypoints[currentWaypoint] == null) return;

        // Handle pause delay at reached waypoint
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        // Advance to next waypoint upon arrival
        float dist = MoveTo(waypoints[currentWaypoint].position, patrolSpeed);
        if (dist <= arriveDistance)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            waitTimer = waitTime;
            path = null;
        }
    }

    // Alert logic: brief hesitation and face player before starting full chase
    void DoAlert(bool sees)
    {
        if (!sees)
        {
            state = eState.patrol;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.sqrMagnitude > 0.0001f) facing = toPlayer.normalized;

        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
        {
            state = eState.chase;
            stateTimer = loseSightTime;
        }
    }

    // Chase logic: pursues player or last known position using pathfinding
    void DoChase(bool sees)
    {
        if (sees) stateTimer = loseSightTime;
        else stateTimer -= Time.fixedDeltaTime;

        float dist = MoveTo(lastKnownPos, chaseSpeed);

        // Handle reaching last known position after losing sight
        if (!sees && dist <= arriveDistance)
        {
            // Check if player recently ducked into a nearby bush
            bool justHid = playerHide != null && playerHide.isHidden
                           && Time.time - playerHide.hideStartTime < bushReactionTime;

            if (justHid)
            {
                bushPos = playerHide.currentBush.transform.position;
                state = eState.checkBush;
            }
            else
            {
                state = eState.search;
                stateTimer = searchTime;
            }
        }
        else if (stateTimer <= 0f)
        {
            state = eState.search;
            stateTimer = searchTime;
        }
    }

    // Search logic: spins in place looking for player before returning to patrol
    void DoSearch(bool sees)
    {
        if (sees)
        {
            state = eState.chase;
            stateTimer = loseSightTime;
            return;
        }

        // Rotate facing direction around search circle
        facing = (Vector2)(Quaternion.Euler(0f, 0f, searchLookSpeed * Time.fixedDeltaTime) * (Vector3)facing);

        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
        {
            currentWaypoint = NearestWaypoint();
            waitTimer = 0f;
            state = eState.patrol;
            path = null;
        }
    }

    // Bush inspection logic: approaches target bush and catches player if still hiding inside
    void DoCheckBush(bool sees)
    {
        if (sees)
        {
            state = eState.chase;
            stateTimer = loseSightTime;
            return;
        }

        float dist = MoveTo(bushPos, chaseSpeed);

        if (dist <= bushCatchDistance)
        {
            if (playerHide != null && playerHide.isHidden && gm != null)
            {
                gm.KillPlayer();
            }
            else
            {
                lastKnownPos = bushPos;
                state = eState.search;
                stateTimer = searchTime;
            }
        }
    }

    // Handles movement along A* path nodes or fallback direct movement
    float MoveTo(Vector2 target, float speed)
    {
        float distToTarget = Vector2.Distance(rb.position, target);
        if (distToTarget <= arriveDistance) return distToTarget;

        Pathfinder pf = Pathfinder.S;
        if (pf == null)
        {
            StepToward(target, speed);
            return distToTarget;
        }

        // Recalculate path if target moved or path is unassigned
        repathTimer -= Time.fixedDeltaTime;
        bool needPath = path == null || path.Count == 0
                        || (Vector2.Distance(target, pathTarget) > repathDistance && repathTimer <= 0f);
        if (needPath)
        {
            path = pf.FindPath(rb.position, target);
            if (path == null) path = new List<Vector2>();
            pathIndex = 0;
            pathTarget = target;
            repathTimer = repathInterval;
        }

        // Advance path index as guard reaches individual nodes
        while (pathIndex < path.Count
               && Vector2.Distance(rb.position, path[pathIndex]) <= pathPointArrive)
        {
            pathIndex++;
        }

        if (pathIndex < path.Count)
        {
            StepToward(path[pathIndex], speed);
        }
        else
        {
            // Direct raycast test to target if path nodes are exhausted
            Vector2 dir = (target - rb.position).normalized;
            LayerMask blockingLayers = LayerMask.GetMask("Walls", "Windows");
            RaycastHit2D hit = Physics2D.Raycast(rb.position, dir, distToTarget, blockingLayers);

            if (hit.collider == null)
            {
                StepToward(target, speed);
            }
        }

        return distToTarget;
    }

    // Applies physics position movement toward given target point
    void StepToward(Vector2 point, float speed)
    {
        Vector2 toPoint = point - rb.position;
        float dist = toPoint.magnitude;

        if (dist > arriveDistance)
        {
            facing = toPoint / dist;
            rb.MovePosition(Vector2.MoveTowards(rb.position, point, speed * Time.fixedDeltaTime));
        }
    }

    // Utility function finding closest waypoint index to resume patrol route
    int NearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return 0;

        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float d = Vector2.Distance(rb.position, waypoints[i].position);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    // Kills player immediately if guard physically collides with them
    void OnCollisionStay2D(Collision2D coll)
    {
        if (coll.gameObject.CompareTag("Player") && gm != null)
        {
            gm.KillPlayer();
        }
    }

    // Cleans up chase music registration if guard object is disabled or destroyed
    void OnDisable()
    {
        if (MusicManager.S != null)
        {
            MusicManager.S.RegisterChase(this, false);
        }
    }
}
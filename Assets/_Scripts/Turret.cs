using UnityEngine;

public class Turret : MonoBehaviour
{
    public enum eState { dormant, telegraph, cooldown }

    [Header("Inscribed")]
    public Bullet bulletPrefab;
    public float activationRange = 8f;      // Wakes up when the player gets this close
    public float telegraphTime = 0.8f;      // Warning before each shot
    public float cooldownTime = 1.5f;       // Pause after a shot
    public float bulletSpeed = 8f;
    public LayerMask wallMask;              // Layers that block vision and stop bullets (Walls)

    [Header("Audio")]
    public AudioClip whirSound;             // Telegraph warning sound
    public AudioClip fireSound;             // Firing sound
   
    [Header("Barrel Setup (Optional)")]
    public Transform barrel;                // Leave empty if using a single gun sprite
    public Transform firePoint;             // Leave empty to shoot from center

    [Header("Look")]
    public Color disguisedColor = new Color(0.35f, 0.5f, 0.3f);
    public Color revealedColor = Color.red;
    public Sprite disguisedSprite;
    public Sprite revealedSprite;

    [Header("Dynamic")]
    public eState state = eState.dormant;

    private Transform player;
    private PlayerHide playerHide;
    private SpriteRenderer sr;
    private LineRenderer aimLine;
    private AudioSource audioSrc;
    private float timer = 0f;
    private Vector2 aimDir = Vector2.right;

    void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHide = playerGO.GetComponent<PlayerHide>();
        }

        sr = GetComponent<SpriteRenderer>();

        // Audio Source setup
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.loop = true;
        audioSrc.playOnAwake = false;

        // Line Renderer setup for aiming laser
        aimLine = gameObject.AddComponent<LineRenderer>();
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = new Color(1f, 0f, 0f, 0.8f);
        aimLine.endColor = new Color(1f, 0f, 0f, 0.3f);
        aimLine.startWidth = 0.12f;
        aimLine.endWidth = 0.12f;
        aimLine.positionCount = 2;
        aimLine.sortingOrder = 1;
        aimLine.enabled = false;

        SetDisguised();
    }

    void Update()
    {
        if (player == null) return;

        bool hidden = playerHide != null && playerHide.isHidden;
        bool canSeePlayer = HasLineOfSight();
        bool inRange = !hidden
                       && Vector2.Distance(player.position, transform.position) <= activationRange
                       && canSeePlayer;

        Transform rotTarget = barrel != null ? barrel : transform;

        if (inRange && state != eState.telegraph)
        {
            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 toPlayer = ((Vector2)player.position - origin).normalized;
            rotTarget.right = toPlayer;
        }

        switch (state)
        {
            case eState.dormant:
                if (inRange) StartTelegraph();
                break;

            case eState.telegraph:
                // Cancel if player hides OR steps behind a wall during telegraph
                if (hidden || !canSeePlayer)
                {
                    CancelTelegraph();
                    break;
                }

                // Smoothly tracking player while warming up to fire
                Vector2 firePos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
                aimDir = ((Vector2)player.position - firePos).normalized;
                rotTarget.right = aimDir;

                UpdateLaser(firePos);

                timer -= Time.deltaTime;
                Blink();
                if (timer <= 0f)
                {
                    Fire();
                    aimLine.enabled = false;
                    sr.color = revealedColor;
                    state = eState.cooldown;
                    timer = cooldownTime;
                }
                break;

            case eState.cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (inRange)
                    {
                        StartTelegraph();
                    }
                    else
                    {
                        state = eState.dormant;
                        SetDisguised();
                    }
                }
                break;
        }
    }

    bool HasLineOfSight()
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float dist = toPlayer.magnitude;

        // Cast ray toward player to verify no object on wallMask is in the way
        RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, dist, wallMask);
        return hit.collider == null;
    }

    void StartTelegraph()
    {
        state = eState.telegraph;
        timer = telegraphTime;
        SetRevealed();

        if (whirSound != null && audioSrc != null)
        {
            audioSrc.clip = whirSound;
            audioSrc.Play();
        }

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        aimDir = ((Vector2)player.position - origin).normalized;

        Transform rotTarget = barrel != null ? barrel : transform;
        rotTarget.right = aimDir;

        UpdateLaser(origin);
        aimLine.enabled = true;
    }

    void UpdateLaser(Vector2 origin)
    {
        float len = activationRange + 2f;
        RaycastHit2D hit = Physics2D.Raycast(origin, aimDir, len, wallMask);
        Vector2 end = hit.collider != null ? hit.point : origin + aimDir * len;

        aimLine.SetPosition(0, origin);
        aimLine.SetPosition(1, end);
    }

    void CancelTelegraph()
    {
        if (audioSrc != null && audioSrc.isPlaying) audioSrc.Stop();

        aimLine.enabled = false;
        sr.color = revealedColor;
        state = eState.cooldown;
        timer = cooldownTime;
    }

    void Blink()
    {
        bool on = Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
        sr.color = on ? revealedColor : Color.white;
    }

    void Fire()
    {
        if (audioSrc != null && audioSrc.isPlaying) audioSrc.Stop();

        if (fireSound != null)
        {
            AudioSource.PlayClipAtPoint(fireSound, transform.position, 0.8f);
        }

        SpawnBullet(aimDir);
    }

    void SpawnBullet(Vector2 dir)
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Bullet b = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        b.direction = dir;
        b.speed = bulletSpeed;
        b.wallMask = wallMask;
    }

    void SetDisguised()
    {
        sr.color = disguisedColor;
        if (disguisedSprite != null) sr.sprite = disguisedSprite;
    }

    void SetRevealed()
    {
        sr.color = revealedColor;
        if (revealedSprite != null) sr.sprite = revealedSprite;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}
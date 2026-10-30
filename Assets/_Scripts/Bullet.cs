using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Inscribed")]
    public float lifetime = 6f;             // Destroyed after this many seconds even if it hits nothing

    [Header("Dynamic")]
    public Vector2 direction = Vector2.right;   // Set by the Turret that fires it
    public float speed = 8f;                    // ^
    public LayerMask wallMask;                  // ^

    private Rigidbody2D rb;
    private GameManager gm;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;    
        gm = FindFirstObjectByType<GameManager>();
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    // Also checked while overlapping, so a player who leaves a bush while a bullet is on top of them still gets hit
    void OnTriggerStay2D(Collider2D other)
    {
        HandleHit(other);
    }

    void HandleHit(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHide hide = other.GetComponent<PlayerHide>(); //Doesn't kill player if in a bush, as if the turret misses
            if (hide != null && hide.isHidden) return;

            if (gm != null) gm.KillPlayer();
            Destroy(gameObject);
        }
        else if (((1 << other.gameObject.layer) & wallMask.value) != 0)
        {
            Destroy(gameObject);                    // Stopped by a wall
        }
    }
}
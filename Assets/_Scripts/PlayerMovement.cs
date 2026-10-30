using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Inscribed")]
    public float moveSpeed = 5f;

    [Header("Audio")]
    public AudioClip footstepSound;
    public float stepInterval = 0.35f;      // Time between footstep sounds

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float stepTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Read WASD/arrow input every frame
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical");   

        moveInput = new Vector2(h, v).normalized; // normalized so diagonal isn't faster

        // Rotate character towards movement direction when moving
        if (moveInput.sqrMagnitude > 0.01f)
        {
            transform.right = moveInput; 

            // Play footstep sounds at intervals while moving
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                if (footstepSound != null)
                {
                    AudioSource.PlayClipAtPoint(footstepSound, transform.position, 0.35f);
                }
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f; // Reset step timer so sound plays immediately when walking starts
        }
    }

    void FixedUpdate()
    {
        // Move via physics b/c of RB2D
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
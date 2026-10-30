using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    [Header("Inscribed")]
    public Color lockedColor = Color.red;
    public Color unlockedColor = Color.green;

    [Header("Dynamic")]
    public bool isLocked = true;

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private GameManager gm;

    void Awake()
    {
        FindComponents();
    }

    void Start()
    {
        gm = FindFirstObjectByType<GameManager>();
        if (sr != null) sr.color = lockedColor;
        if (col != null) col.isTrigger = false;     // Solid while locked
    }

    // Looks the components up whenever they're missing, so it works even if Start never ran
    void FindComponents()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<BoxCollider2D>();
    }

    public void Unlock()
    {
        // A prefab asset in the Project window isn't part of any scene
        if (!gameObject.scene.IsValid())
        {
            Debug.LogError("GameManager's Gate slot points at a PREFAB in the Project window. " +
                           "Drag the Gate from the HIERARCHY into that slot instead.");
            return;
        }

        FindComponents();
        if (sr == null || col == null)
        {
            Debug.LogError("The Gate object '" + name + "' needs BOTH a Sprite Renderer and a Box Collider 2D.");
            return;
        }

        isLocked = false;
        sr.color = unlockedColor;
        col.isTrigger = true;       // Player can now walk through
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLocked && other.CompareTag("Player"))
        {
            if (gm != null) gm.LevelComplete();
        }
    }
}
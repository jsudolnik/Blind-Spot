using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Bush : MonoBehaviour
{
    [Header("Collider Settings")]
    public Vector2 triggerSize = new Vector2(0.5f, 0.5f); // Default trigger area

    private BoxCollider2D boxCollider;

    void Awake()
    {
        SetupCollider();
    }

    void OnValidate()
    {
        SetupCollider();
    }

    private void SetupCollider()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        boxCollider.isTrigger = true;
        boxCollider.size = triggerSize;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHide hide = other.GetComponent<PlayerHide>();
        if (hide == null) return;

        PolicePatrol[] guards = FindObjectsByType<PolicePatrol>(); // Checks before hiding if the guard saw a player enter
        foreach (PolicePatrol guard in guards)
        {
            if (guard.SeesPlayer())
            {
                guard.InvestigateBush(transform.position);
            }
        }

        hide.Hide(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return; //hide/unhide

        PlayerHide hide = other.GetComponent<PlayerHide>();
        if (hide != null) hide.Unhide();
    }

}
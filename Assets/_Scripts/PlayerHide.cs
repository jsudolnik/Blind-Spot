using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHide : MonoBehaviour
{
    [Header("Inscribed")]
    public float hiddenAlpha = 0.4f;        // transparency of player when hiding

    [Header("Audio")]
    public AudioClip bushEnterSound;
    public AudioClip bushExitSound;

    [Header("Dynamic")]
    public bool isHidden = false;
    public float hideStartTime = -100f;     // Time.time when the player entered the bush
    public Bush currentBush;

    private SpriteRenderer sr;
    private Color baseColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    public void Hide(Bush bush)
    {
        if (!isHidden && bushEnterSound != null)
        {
            AudioSource.PlayClipAtPoint(bushEnterSound, transform.position, 0.6f);
        }

        isHidden = true;
        hideStartTime = Time.time;
        currentBush = bush;

        Color c = baseColor;
        c.a = hiddenAlpha;
        sr.color = c;
    }

    public void Unhide()
    {
        if (isHidden && bushExitSound != null)
        {
            AudioSource.PlayClipAtPoint(bushExitSound, transform.position, 0.6f);
        }

        isHidden = false;
        currentBush = null;
        sr.color = baseColor;
    }
}
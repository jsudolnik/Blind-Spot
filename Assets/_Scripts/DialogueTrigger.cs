using UnityEngine;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    [TextArea(3, 6)]
    public string[] dialogueLines;

    [Header("Trigger Settings")]
    public bool triggerOnStart = false;     
    public bool triggerOnce = true;

    [Header("UI References (Drag existing UI here)")]
    public GameObject dialoguePanel;        
    public TMP_Text dialogueText;           

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool hasTriggered = false;

    private PlayerMovement playerMovement;
    private Rigidbody2D playerRb;

    void Start()
    {
        // Automatically hide panel on start in case it was left active in editor
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (triggerOnStart)
        {
            TriggerDialogue();
        }
    }

    void Update()
    {
        // Advance lines with Spacebar 
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        if ((hasTriggered && triggerOnce) || isDialogueActive) return;
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        hasTriggered = true;
        isDialogueActive = true;
        currentLineIndex = 0;

        // Freeze player movement during dialogue
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerMovement = playerGO.GetComponent<PlayerMovement>();
            playerRb = playerGO.GetComponent<Rigidbody2D>();

            if (playerMovement != null) playerMovement.enabled = false;
            if (playerRb != null) playerRb.linearVelocity = Vector2.zero; 
        }

        // Show UI panel and first line
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        DisplayCurrentLine();
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void DisplayCurrentLine()
    {
        if (dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;

        // Hide UI panel
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Unfreeze player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f);
            Gizmos.DrawCube((Vector2)transform.position + box.offset, box.size);
        }
    }
}
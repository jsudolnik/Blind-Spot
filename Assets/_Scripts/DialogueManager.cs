using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    private string[] currentLines;
    private int currentLineIndex = 0;
    public bool IsDialogueActive { get; private set; } = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (!IsDialogueActive) return;

        // Press Space to advance to the next line or close dialogue
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    public void StartDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        currentLines = lines;
        currentLineIndex = 0;
        IsDialogueActive = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        Time.timeScale = 0f; // Pause guards, turrets, and player while thinking

        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        if (dialogueText != null)
        {
            dialogueText.text = currentLines[currentLineIndex] + "\n\n<size=70%><color=#A8A8A8>[Press SPACE]</color></size>";
        }
    }

    void AdvanceDialogue()
    {
        currentLineIndex++;
        if (currentLineIndex < currentLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        Time.timeScale = 1f; // Resume gameplay
    }
}

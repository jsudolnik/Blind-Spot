using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    static public int DEATHS = 0;           // Static so the count survives scene reloads

    [Header("Inscribed")]
    public Gate gate;
    public TMP_Text deathText;              // On-screen "Deaths: N" label
    public float restartDelay = 0f;         // Seconds before restarting
    public DialogueTrigger unlockDialogue;  // Dialogue triggered on camera completion

    [Header("Audio")]
    public AudioClip unlockSound;           // Played when all cameras are destroyed
    public AudioClip deathSound;            // Optional: Played when player dies

    [Header("Dynamic")]
    public int camerasRemaining;

    private bool restarting = false;
    private bool levelDone = false;

    void Start()
    {
        Time.timeScale = 1f;

        // Count every SecurityCamera in the scene so levels can have any number
        camerasRemaining = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None).Length;
        UpdateDeathText();
    }

    void Update()
    {
        if (levelDone) return;

        // R restarts the stage instantly and does NOT count as a death
        if (Input.GetKeyDown(KeyCode.R)) RestartStage();

        // ESC returns to the Main Menu (Build Index 0)
        if (Input.GetKeyDown(KeyCode.Escape)) MainMenu();
    }

    public void CameraDestroyed()
    {
        camerasRemaining--;
        if (camerasRemaining <= 0)
        {
            if (unlockSound != null)
            {
                Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(unlockSound, soundPos, 1f);
            }

            // Fire dialogue when gate unlocks
            if (unlockDialogue != null)
            {
                unlockDialogue.TriggerDialogue();
            }

            if (gate != null)
            {
                gate.Unlock();
            }
        }
    }

    public void LevelComplete()
    {
        if (levelDone || restarting) return;
        levelDone = true;

        NextLevel();
    }

    public void KillPlayer()
    {
        if (restarting || levelDone) return;

        if (deathSound != null)
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(deathSound, soundPos, 1f);
        }

        DEATHS++;
        UpdateDeathText();
        RestartStage();
    }

    void RestartStage()
    {
        if (restarting) return;
        restarting = true;
        Invoke("ReloadScene", restartDelay);
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void NextLevel()
    {
        Time.timeScale = 1f;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            MainMenu();
        }
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void UpdateDeathText()
    {
        if (deathText != null) deathText.text = "Deaths: " + DEATHS;
    }

    static public void RESET_DEATHS()
    {
        DEATHS = 0;
    }
}
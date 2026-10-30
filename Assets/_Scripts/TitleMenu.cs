using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject mainPanel;            // Title text + Play button
    public GameObject levelSelectPanel;     // Level buttons + Back button
    public Button playButton;
    public Button backButton;
    public Button[] levelButtons;           // Element 0 = Level 1, element 1 = Level 2, ...
    public int firstLevelBuildIndex = 1;    // Build Profiles index of Level 1 (the title screen is 0)

    void Start()
    {
        Time.timeScale = 1f;
        ShowMain();

        // Buttons are wired here in code, so nothing needs setting up in the On Click list
        if (playButton != null) playButton.onClick.AddListener(ShowLevelSelect);
        if (backButton != null) backButton.onClick.AddListener(ShowMain);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int buildIndex = firstLevelBuildIndex + i;      // Copied so each button keeps its own number

            // Grey out buttons for levels that aren't in the build yet
            levelButtons[i].interactable = buildIndex < SceneManager.sceneCountInBuildSettings;
            levelButtons[i].onClick.AddListener(() => LoadLevel(buildIndex));
        }
    }

    void ShowMain()
    {
        mainPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    void ShowLevelSelect()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    void LoadLevel(int buildIndex)
    {
        GameManager.RESET_DEATHS();         // A new run starts with zero deaths
        SceneManager.LoadScene(buildIndex);
    }
}
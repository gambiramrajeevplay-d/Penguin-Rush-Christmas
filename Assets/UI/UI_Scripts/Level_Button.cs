using Script;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Level_Button : MonoBehaviour
{
    [Header("Level Details")]
    [SerializeField] private string sceneNameToLoad = "MainGame";
    [SerializeField] private int levelToLoad = 1; // 1-based
    [SerializeField] private bool isPaidLevel = false;

    [Header("Visuals")]
    public GameObject lockImage;

    [Header("Menus")]
    [SerializeField] private LevelSelectionUI levelSelectionUI;

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject loadingScreentab;

    [Header("Controls Panel (TV only)")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private Button controlsOkButton;

    private Button levelButton;
    private bool isLoading = false;

    private void Awake()
    {
        EnsureInitialized();
        levelButton.interactable = false;
    }

    private void Start()
    {
        EnsureInitialized();

        levelButton.onClick.RemoveAllListeners();
        levelButton.onClick.AddListener(LoadLevel);

        // Controls panel should be hidden at start
        if (controlsPanel)
            controlsPanel.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (levelButton == null)
            levelButton = GetComponent<Button>();
    }

    // Called from LevelSelectionUI
    public void SetUnlocked(bool unlocked)
    {
        EnsureInitialized();

        int unlockedAll = PlayerPrefs.GetInt(StringsData.unlockedAllLevels, 0);

        levelButton.onClick.RemoveAllListeners();

        // Level 1 is always free
        if (levelToLoad == 1)
        {
            levelButton.interactable = true;

            if (lockImage)
                lockImage.SetActive(false);

            levelButton.onClick.AddListener(LoadLevel);
            return;
        }

        // Special case: Level 5 after completing Level 4
        if (isPaidLevel && levelToLoad == 5)
        {
            int playerLevel = PlayerPrefs.GetInt(StringsData.playerLevel, 1);

            if (playerLevel >= 5)
            {
                levelButton.interactable = true;

                if (lockImage)
                    lockImage.SetActive(false);

                levelButton.onClick.AddListener(() =>
                {
                    if (unlockedAll == 1)
                    {
                        LoadLevel();
                    }
                    else
                    {
                        if (levelSelectionUI != null)
                            levelSelectionUI.ShowUnlockAllPanel();
                    }
                });

                return;
            }
        }

        // Paid level that has not been purchased
        if (isPaidLevel && unlockedAll == 0)
        {
            levelButton.interactable = true;

            if (lockImage)
                lockImage.SetActive(true);

            levelButton.onClick.AddListener(() =>
            {
                if (levelSelectionUI != null)
                    levelSelectionUI.ShowUnlockAllPanel();
            });

            return;
        }

        // Normal level
        levelButton.interactable = unlocked;

        if (lockImage)
            lockImage.SetActive(!unlocked);

        if (unlocked)
            levelButton.onClick.AddListener(LoadLevel);
    }

    // Compatibility method
    public void UnLockLevel()
    {
        int playerLevel = PlayerPrefs.GetInt(StringsData.playerLevel, 1);
        bool unlocked = playerLevel >= levelToLoad;

        SetUnlocked(unlocked);
    }

    private void LoadLevel()
    {
        if (isLoading)
            return;

        int unlockedAll = PlayerPrefs.GetInt(StringsData.unlockedAllLevels, 0);

        if (isPaidLevel && unlockedAll == 0)
        {
            if (levelSelectionUI != null)
                levelSelectionUI.ShowUnlockAllPanel();

            return;
        }

        isLoading = true;

        // Save selected level
        PlayerPrefs.SetInt(StringsData.levelToLoad, levelToLoad);
        PlayerPrefs.Save();

        // Load the configured scene directly by name.
        // No tutorial redirection.
        string targetScene = sceneNameToLoad;

        // Configure loading screen
        SetLoadingScreen(true);

        StartCoroutine(LoadLevelWithDelay(targetScene));
    }

    private IEnumerator LoadLevelWithDelay(string sceneName)
    {
        // Loading screen is visible during this time
        yield return new WaitForSeconds(3f);

        bool isTv = AndroidTV.IsAndroidOrFireTv();

        // TV only: show controls panel and wait for OK
        if (isTv && controlsPanel != null && controlsOkButton != null)
        {
            // Loading finished -> hide loading screens
            SetLoadingScreen(false);

            controlsPanel.SetActive(true);

            controlsOkButton.onClick.RemoveAllListeners();
            controlsOkButton.onClick.AddListener(() => OnControlsOkClicked(sceneName));

            // Select OK button so TV remote / controller can press it
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(controlsOkButton.gameObject);
            }
        }
        else
        {
            // Mobile / Tab (or no panel assigned): load the level directly
            SceneManager.LoadScene(sceneName);
        }
    }

    private void OnControlsOkClicked(string sceneName)
    {
        controlsOkButton.onClick.RemoveAllListeners();

        if (controlsPanel)
            controlsPanel.SetActive(false);

        SceneManager.LoadScene(sceneName);
    }

    private void SetLoadingScreen(bool show)
    {
        if (!show)
        {
            if (loadingScreen)
                loadingScreen.SetActive(false);

            if (loadingScreentab)
                loadingScreentab.SetActive(false);

            return;
        }

        if (AndroidTV.IsAndroidOrFireTv())
        {
            if (loadingScreen)
                loadingScreen.SetActive(true);

            if (loadingScreentab)
                loadingScreentab.SetActive(false);
        }
        else
        {
            if (loadingScreen)
                loadingScreen.SetActive(false);

            if (loadingScreentab)
                loadingScreentab.SetActive(true);
        }
    }

    public int GetLevelNumberSafe()
    {
        return Mathf.Max(1, levelToLoad);
    }
}
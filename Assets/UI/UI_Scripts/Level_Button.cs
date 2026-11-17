using Script;
using System.Collections;
using UnityEngine;
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

    private Button levelButton;

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
        int unlockedAll = PlayerPrefs.GetInt(StringsData.unlockedAllLevels, 0);

        if (isPaidLevel && unlockedAll == 0)
        {
            if (levelSelectionUI != null)
                levelSelectionUI.ShowUnlockAllPanel();

            return;
        }

        // Save selected level
        PlayerPrefs.SetInt(StringsData.levelToLoad, levelToLoad);
        PlayerPrefs.Save();

        // Load the configured scene directly by name.
        // No tutorial redirection.
        string targetScene = sceneNameToLoad;

        // Configure loading screen
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

        StartCoroutine(LoadLevelWithDelay(targetScene));
    }

    private IEnumerator LoadLevelWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(3f);

        SceneManager.LoadScene(sceneName);
    }

    public int GetLevelNumberSafe()
    {
        return Mathf.Max(1, levelToLoad);
    }
}
using Script;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Levels")]
    [SerializeField] private Level_Button[] levels;

    [Header("UnlockFullGameUI")]
    [SerializeField] private GameObject unlockFullGamePanel;
    [SerializeField] private Button unlockFullGamePanelCloseButton;

    [Header("Menus")]
    [SerializeField] private GameObject horseSelectionMenu;

    private Animator animator;
    private ButtonHighlighter buttonHighlighter;
    private Button levelButtonToHighlight;

    private bool isInitialized = false;

    [Header("Debug / Testing")]
    [SerializeField] private bool forceUnlockAllLevels = false;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI currencyText;



    private void OnEnable()
    {
        if (CurrecnyManager.instance != null)
        {
            CurrecnyManager.instance.OnCurrencyChanged += UpdateCoins;
            UpdateCoins(CurrecnyManager.instance.GetCurrency());
        }


        if (!isInitialized)
            return; // ⛔ prevent first-open bug

        //if (animator != null)
        //    animator.SetTrigger("Entry");



        UnlockLevels(); // ✅ highlight on reopen
        UpdateCurrencyText();
    }
    private void OnDisable()
    {
        if (CurrecnyManager.instance != null)
        {
            CurrecnyManager.instance.OnCurrencyChanged -= UpdateCoins;
        }
    }
    void UpdateCoins(int amount)
    {
        currencyText.text = amount.ToString();
    }

    private void Start()
    {
        UpdateCurrencyText();



        Debug.Log("[LevelSelectionUI][Start] Initializing LevelSelectionUI.");

        // ✅ Ensure player level exists
        if (!PlayerPrefs.HasKey(StringsData.playerLevel))
        {
            PlayerPrefs.SetInt(StringsData.playerLevel, 1);
            PlayerPrefs.Save();
            Debug.Log("[LevelSelectionUI] Player level initialized to 1.");
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(GoToHorseSelection);
        else
            Debug.LogWarning("[LevelSelectionUI] closeButton is not assigned.");

        buttonHighlighter = GetComponent<ButtonHighlighter>();
        if (buttonHighlighter == null)
            Debug.LogWarning("[LevelSelectionUI] ButtonHighlighter not found on the same GameObject.");

        isInitialized = true;

        UnlockLevels(); // refresh when enabled
    }

    public void UnlockLevels()
    {
        if (buttonHighlighter == null)
            buttonHighlighter = GetComponent<ButtonHighlighter>();

        int playerLevel = PlayerPrefs.GetInt(StringsData.playerLevel, 1);

        bool fullGameUnlocked =
            forceUnlockAllLevels ||
            PlayerPrefs.GetInt(StringsData.unlockedAllLevels, 0) == 1;

        // 1️⃣ LOCK / UNLOCK LEVELS
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                continue;

            // 🚨 LEVEL 1 ALWAYS FREE
            bool shouldUnlock = (i == 0) || fullGameUnlocked || (i < playerLevel);

            // Apply unlock logic
            levels[i].SetUnlocked(shouldUnlock);

            // 🔥 FORCE FIX (prevents prefab or paid logic issues)
            Button btn = levels[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = shouldUnlock;
            }
        }

        // 2️⃣ SAFETY: FIRST LEVEL MUST ALWAYS BE INTERACTABLE
        if (levels.Length > 0 && levels[0] != null)
        {
            Button firstButton = levels[0].GetComponent<Button>();
            if (firstButton != null)
                firstButton.interactable = true;
        }

        // 3️⃣ HIGHLIGHT CURRENT PLAYABLE LEVEL (CORRECT FIX)
        int highlightIndex = Mathf.Clamp(playerLevel - 1, 0, levels.Length - 1);

        Button targetButton = null;

        // 🔍 Find nearest valid interactable button (fallback safety)
        for (int i = highlightIndex; i >= 0; i--)
        {
            if (levels[i] == null) continue;

            Button btn = levels[i].GetComponent<Button>();
            if (btn != null && btn.interactable)
            {
                targetButton = btn;
                break;
            }
        }

        // 🎯 Apply highlight
        if (targetButton != null && buttonHighlighter != null && EventSystem.current != null)
        {
            buttonHighlighter.defaultButton = targetButton.gameObject;
            buttonHighlighter.enabled = true;

            EventSystem.current.SetSelectedGameObject(targetButton.gameObject);
            buttonHighlighter.HighlightButton(targetButton);
        }
    }


    public void UpdateCurrencyText(int amount = 0)
    {
        if (currencyText != null && CurrecnyManager.instance != null)
        {
            currencyText.text =
                CurrecnyManager.instance.GetCurrency().ToString();
        }
    }

    public void HighlightButton(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogWarning($"[LevelSelectionUI] Invalid levelIndex {levelIndex}");
            return;
        }

        levelButtonToHighlight = levels[levelIndex].GetComponent<Button>();

        if (levelButtonToHighlight == null)
        {
            Debug.LogWarning($"[LevelSelectionUI] No Button component found on level {levelIndex}");
            return;
        }

        if (!levelButtonToHighlight.interactable)
        {
            Debug.LogWarning($"[LevelSelectionUI] Cannot highlight level {levelIndex} - button not interactable");
            return;
        }

        if (buttonHighlighter == null)
        {
            Debug.LogWarning("[LevelSelectionUI] ButtonHighlighter is null");
            return;
        }

        buttonHighlighter.defaultButton = levels[levelIndex].gameObject;
        buttonHighlighter.enabled = true;
        buttonHighlighter.HighlightButton(levelButtonToHighlight);

        Debug.Log($"[LevelSelectionUI] Successfully highlighted level {levelIndex}");
    }

    public void GoToHorseSelection()
    {
        Debug.Log("LevelSelectionUI.GoToHorseSelection");

        if (horseSelectionMenu != null)
            horseSelectionMenu.SetActive(true);
        else
            Debug.LogWarning("horseSelectionMenu not assigned.");

        gameObject.SetActive(false);
    }

    public void ShowUnlockAllPanel()
    {
        Debug.Log("ShowUnlockAllPanel");

        if (unlockFullGamePanel != null)
        {
            unlockFullGamePanel.SetActive(true);
            gameObject.SetActive(false);
        }
        else
            Debug.LogWarning("unlockFullGamePanel not assigned.");
    }

    public void CloseUnlockFullGamePanel()
    {
        if (unlockFullGamePanel != null)
        {
            unlockFullGamePanel.SetActive(false); // 🔴 turn OFF unlock panel
        }

        gameObject.SetActive(true); // 🔵 turn ON level selection

        UnlockLevels(); // refresh & highlight again
    }

    // 🏆 FULL GAME PURCHASE
    public void OnFullGameUnlocked()
    {
        Debug.Log("[LevelSelectionUI] Full game unlocked!");

        PlayerPrefs.SetInt(StringsData.unlockedAllLevels, 1);
        PlayerPrefs.SetInt(StringsData.playerLevel, levels.Length);
        PlayerPrefs.Save();

        UnlockLevels();
    }
}

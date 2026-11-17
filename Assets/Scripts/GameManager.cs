using System;
using System.Collections;
using Script;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI - Level Pass")]
    [FormerlySerializedAs("finishPanel")]
    [SerializeField] private GameObject levelPassPanel;
    [SerializeField] private Button levelPassHomeButton;
    [Tooltip("Shows the coins earned on the Level Pass panel, e.g. +100.")]
    [SerializeField] private TMP_Text levelPassRewardText;

    [Header("UI - Level Fail")]
    [SerializeField] private GameObject levelFailPanel;
    [SerializeField] private Button failRestartButton;
    [Tooltip("Shows the coins earned on the Level Fail panel, e.g. +0.")]
    [SerializeField] private TMP_Text levelFailRewardText;

    [Header("Rewards")]
    [Tooltip("Bonus coins added when the player passes the level. Total reward = coins collected + this.")]
    [SerializeField] private int levelPassRewardCoins = 100;
    [Tooltip("Bonus coins added when the player fails the level. Total reward = coins collected + this.")]
    [SerializeField] private int levelFailRewardCoins = 0;
    [Tooltip("Optional. Found automatically in the scene if left empty.")]
    [SerializeField] private CoinCollector coinCollector;

    [Header("Home Scene")]
    [Tooltip("Scene loaded by the Level Pass Home button. If empty, the build index below is used.")]
    [SerializeField] private string homeSceneName = "";
    [Tooltip("Build index of the main menu scene. Only used when Home Scene Name is empty.")]
    [SerializeField] private int homeSceneBuildIndex = 0;

    [Header("UI - Touch Controls (Mobile / Tablet only)")]
    [Tooltip("Parent panel that holds the left / right buttons. Hidden on TV and desktop.")]
    [SerializeField] private GameObject touchControlsPanel;
    [Tooltip("Hold this button to steer left.")]
    [SerializeField] private GameObject leftButton;
    [Tooltip("Hold this button to steer right.")]
    [SerializeField] private GameObject rightButton;
    [Tooltip("Editor only: show the touch panel in the Unity Editor so you can test it with the mouse.")]
    [SerializeField] private bool showTouchControlsInEditor = true;
    [Tooltip("Hide the touch panel when the level is passed or failed.")]
    [SerializeField] private bool hideTouchControlsOnEnd = true;

    [Header("Timing")]
    [SerializeField] private float panelDelayAfterStop = 0.3f;
    [SerializeField] private float failPanelDelayAfterStop = 0.3f;

    [Header("Level Progress")]
    [Tooltip("1-based level number of THIS scene (Level 1 = 1). Set to 0 to detect it from the scene's build index.")]
    [SerializeField] private int currentLevelNumber = 0;
    [Tooltip("Only used when Current Level Number is 0. Build index of Level 1 (e.g. 1 if the menu scene is index 0).")]
    [SerializeField] private int firstLevelBuildIndex = 1;
    [Tooltip("Total number of levels in the game. The unlocked level will never go above this. 0 = no limit.")]
    [SerializeField] private int totalLevels = 0;

    [Header("Audio")]
    [Tooltip("Plays the level pass / fail sounds. Created automatically if left empty.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio - Level Pass")]
    [FormerlySerializedAs("finishSound")]
    [SerializeField] private AudioClip levelPassSound;

    [Range(0f, 1f)]
    [FormerlySerializedAs("finishVolume")]
    [SerializeField] private float levelPassVolume = 1f;

    [Header("Audio - Level Fail")]
    [SerializeField] private AudioClip levelFailSound;

    [Range(0f, 1f)]
    [SerializeField] private float levelFailVolume = 1f;

    // Names kept so other scripts (e.g. SledgeController) keep working.
    public bool IsFinished { get; private set; }
    public bool IsFailed { get; private set; }

    // True when the player either passes or fails the level.
    public bool IsGameFinished => IsFinished || IsFailed;

    // ---------- Touch input (read by SledgeController) ----------

    private bool leftHeld;
    private bool rightHeld;

    // -1 = left, +1 = right, 0 = none (or both held).
    public float TouchHorizontal =>
        (leftHeld ? -1f : 0f) + (rightHeld ? 1f : 0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        if (levelPassPanel != null)
            levelPassPanel.SetActive(false);

        if (levelFailPanel != null)
            levelFailPanel.SetActive(false);

        if (levelPassHomeButton != null)
            levelPassHomeButton.onClick.AddListener(GoHome);

        if (failRestartButton != null)
            failRestartButton.onClick.AddListener(RestartGame);

        SetupTouchControls();
    }

    private void OnDestroy()
    {
        if (levelPassHomeButton != null)
            levelPassHomeButton.onClick.RemoveListener(GoHome);

        if (failRestartButton != null)
            failRestartButton.onClick.RemoveListener(RestartGame);

        if (Instance == this)
            Instance = null;
    }

    // ---------- Touch controls ----------

    // Mobile / tablet = show. Android TV / Fire TV / desktop = hide.
    private bool ShouldShowTouchControls()
    {
#if UNITY_EDITOR
        return showTouchControlsInEditor;
#else
        if (!Application.isMobilePlatform)
            return false;

        // AndroidTV helper only works on Android, so only ask it there.
        if (Application.platform == RuntimePlatform.Android && AndroidTV.IsAndroidOrFireTv())
            return false;

        return true;
#endif
    }

    private void SetupTouchControls()
    {
        bool show = ShouldShowTouchControls();

        if (touchControlsPanel != null)
            touchControlsPanel.SetActive(show);

        leftHeld = false;
        rightHeld = false;

        if (!show)
            return;

        AddHoldEvents(leftButton, held => leftHeld = held);
        AddHoldEvents(rightButton, held => rightHeld = held);
    }

    // Makes a UI object report "held" on press and "released" on lift / finger leaving it.
    private void AddHoldEvents(GameObject target, Action<bool> setHeld)
    {
        if (target == null)
            return;

        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        AddTrigger(trigger, EventTriggerType.PointerDown, () => setHeld(true));
        AddTrigger(trigger, EventTriggerType.PointerUp, () => setHeld(false));
        AddTrigger(trigger, EventTriggerType.PointerExit, () => setHeld(false));
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private void EndTouchControls()
    {
        leftHeld = false;
        rightHeld = false;

        if (hideTouchControlsOnEnd && touchControlsPanel != null)
            touchControlsPanel.SetActive(false);
    }

    // ---------- Level progress ----------

    // 1-based number of the level being played.
    private int GetCurrentLevelNumber()
    {
        if (currentLevelNumber > 0)
            return currentLevelNumber;

        return SceneManager.GetActiveScene().buildIndex - firstLevelBuildIndex + 1;
    }

    // Called ONLY when the player passes the level. A failed level never calls this.
    // StringsData.playerLevel = how many levels are unlocked (LevelSelectionUI unlocks index < playerLevel),
    // so passing Level N makes it N + 1, which unlocks Level N + 1.
    private void UnlockNextLevel()
    {
        int passedLevel = GetCurrentLevelNumber();

        if (passedLevel < 1)
        {
            Debug.LogWarning("[GameManager] Could not work out the current level number. " +
                             "Set 'Current Level Number' in the Inspector. Next level not unlocked.");
            return;
        }

        int savedLevel = PlayerPrefs.GetInt(StringsData.playerLevel, 1);
        int newLevel = passedLevel + 1;

        if (totalLevels > 0)
            newLevel = Mathf.Min(newLevel, totalLevels);

        // Never lower progress (e.g. when replaying an older level).
        if (newLevel > savedLevel)
        {
            PlayerPrefs.SetInt(StringsData.playerLevel, newLevel);
            PlayerPrefs.Save();

            Debug.Log("[GameManager] Level " + passedLevel + " passed. Unlocked up to level " + newLevel + ".");
        }
    }

    // ---------- Rewards ----------

    // Coins the player picked up during this level.
    private int GetCollectedCoins()
    {
        if (coinCollector == null)
            coinCollector = FindObjectOfType<CoinCollector>();

        return coinCollector != null ? coinCollector.GetLevelCoins() : 0;
    }

    // Total reward = coins collected in the level + the pass / fail bonus.
    // Gives the coins through CurrecnyManager and shows "+total" on the given text.
    private void GiveReward(int bonusCoins, TMP_Text rewardText)
    {
        int amount = Mathf.Max(0, GetCollectedCoins() + bonusCoins);

        if (rewardText != null)
            rewardText.text = "+" + amount;

        if (amount <= 0)
            return;

        if (CurrecnyManager.instance != null)
            CurrecnyManager.instance.AddRewardCoins(amount);
        else
            Debug.LogWarning("[GameManager] CurrecnyManager not found, coins not added.");
    }

    // ---------- Level end ----------

    // Called when the player passes the level.
    public void OnPlayerFinished(float stopDuration)
    {
        if (IsGameFinished)
            return;

        IsFinished = true;
        EndTouchControls();

        // Level passed -> unlock the next level (saved right away).
        UnlockNextLevel();

        // Level passed -> collected coins + bonus (e.g. 30 + 100 = +130), saved right away.
        GiveReward(levelPassRewardCoins, levelPassRewardText);

        if (Pauser.instance != null)
            Pauser.LockPause();

        StartCoroutine(
            ShowLevelPassPanelRoutine(stopDuration + panelDelayAfterStop)
        );
    }

    // Called when the player fails the level.
    public void OnPlayerFailed(float stopDuration)
    {
        if (IsGameFinished)
            return;

        IsFailed = true;
        EndTouchControls();

        // Level failed -> progress is NOT changed, next level stays locked.
        // Reward is only the collected coins + fail bonus (e.g. 5 + 0 = +5).
        GiveReward(levelFailRewardCoins, levelFailRewardText);

        if (Pauser.instance != null)
            Pauser.LockPause();

        StartCoroutine(
            ShowFailPanelRoutine(stopDuration + failPanelDelayAfterStop)
        );
    }

    private IEnumerator ShowLevelPassPanelRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (levelPassPanel != null)
            levelPassPanel.SetActive(true);

        PlaySound(levelPassSound, levelPassVolume);
        UnlockCursor();
    }

    private IEnumerator ShowFailPanelRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (levelFailPanel != null)
            levelFailPanel.SetActive(true);

        PlaySound(levelFailSound, levelFailVolume);
        UnlockCursor();
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
            return;

        // Respect the player's mute setting.
        AudioManagerPause.Initialize();
        if (AudioManagerPause.IsMuted)
            return;

        audioSource.Stop();
        audioSource.PlayOneShot(clip, volume);
    }

    private void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ---------- Buttons ----------

    // Level Pass Home button: back to the main menu scene.
    public void GoHome()
    {
        Time.timeScale = 1f;

        IsFinished = false;
        IsFailed = false;

        if (!string.IsNullOrEmpty(homeSceneName))
            SceneManager.LoadScene(homeSceneName);
        else
            SceneManager.LoadScene(homeSceneBuildIndex);
    }

    // Level Fail restart button: reload the current level.
    public void RestartGame()
    {
        Time.timeScale = 1f;

        IsFinished = false;
        IsFailed = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
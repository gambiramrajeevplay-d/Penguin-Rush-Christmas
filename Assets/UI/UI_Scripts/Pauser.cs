using Script;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Pauser : MonoBehaviour
{
    public static Pauser instance;

    [Header("UI")]
    public GameObject PausePannel;
    public GameObject LevelObject;
    public GameObject PauseButton;

    [Header("Sound UI")]
    [Tooltip("The Image whose sprite swaps between sound on / off.")]
    public Image soundIcon;
    public Sprite sound_on;
    public Sprite sound_off;

    public static bool PauseLocked = false;

    private bool isPaused;
    private bool instructionPanelLock;
    private bool isApplicationFocused = true;

    private bool isTVPlatform;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        isTVPlatform = AndroidTV.IsAndroidOrFireTv();

        AudioManagerPause.Initialize();
        ApplySoundSetting();
    }

    private void OnEnable()
    {
        UpdatePauseButton();
        UpdateSoundIcon();
    }

    private void Start()
    {
        PauseLocked = false;
        isPaused = false;
        Time.timeScale = 1f;

        PrepareSoundIcon();

        UpdatePauseButton();
        ApplySoundSetting();
        UpdateSoundIcon();
    }

    private void Update()
    {
        if (PauseLocked ||
            instructionPanelLock ||
            IsGameFinished() ||
            !isApplicationFocused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    private bool IsGameFinished()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.IsGameFinished;
    }

    public void Pause()
    {
        if (PauseLocked ||
            instructionPanelLock ||
            IsGameFinished() ||
            isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;

        if (LevelObject != null)
            LevelObject.SetActive(false);

        if (PausePannel != null)
            PausePannel.SetActive(true);

        UpdatePauseButton();
        UpdateSoundIcon();
    }

    public void Resume()
    {
        if (PauseLocked ||
            instructionPanelLock ||
            IsGameFinished())
        {
            return;
        }

        if (PausePannel != null)
            PausePannel.SetActive(false);

        if (LevelObject != null)
            LevelObject.SetActive(true);

        isPaused = false;
        Time.timeScale = 1f;

        UpdatePauseButton();
        ApplySoundSetting();
        UpdateSoundIcon();
    }

    public void SetInstructionPanelLock(bool locked)
    {
        instructionPanelLock = locked;

        if (locked)
        {
            if (PausePannel != null)
                PausePannel.SetActive(false);

            isPaused = false;
            Time.timeScale = 1f;
        }

        UpdatePauseButton();
    }

    public bool IsInstructionPanelLocked =>
        instructionPanelLock;

    public bool IsPaused => isPaused;

    public void MM()
    {
        Time.timeScale = 1f;

        PauseLocked = false;
        instructionPanelLock = false;
        isPaused = false;

        SaveScript.cameFromGameplay = true;
        AudioManagerPause.SaveSettings();

        SceneManager.LoadScene("UI");
    }

    // =====================
    // SOUND
    // =====================

    // Hook this to the sound button's OnClick (ONLY ONCE - two hooks would toggle twice = no change).
    public void ToggleSound()
    {
        AudioManagerPause.IsMuted =
            !AudioManagerPause.IsMuted;

        // Save so a later AudioManagerPause.Initialize() can't reset it.
        AudioManagerPause.SaveSettings();

        ApplySoundSetting();
        UpdateSoundIcon();

        Debug.Log("[Pauser] Sound " + (AudioManagerPause.IsMuted ? "MUTED" : "ON"));
    }

    private void ApplySoundSetting()
    {
        bool muted = AudioManagerPause.IsMuted;

        // Master volume.
        AudioListener.volume = muted ? 0f : 1f;

        // Some AudioSources ignore the listener volume (ignoreListenerVolume),
        // so AudioListener.volume alone doesn't silence them. Mute every source directly.
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
                sources[i].mute = muted;
        }
    }

    // If the icon is a Button's graphic with "Sprite Swap", the button overrides our sprite.
    private void PrepareSoundIcon()
    {
        if (soundIcon == null)
        {
            Debug.LogWarning("[Pauser] soundIcon is not assigned, the icon can't change.");
            return;
        }

        if (sound_on == null || sound_off == null)
            Debug.LogWarning("[Pauser] sound_on / sound_off sprite is not assigned.");

        Button button = soundIcon.GetComponentInParent<Button>();

        if (button != null &&
            button.targetGraphic == soundIcon &&
            button.transition == Selectable.Transition.SpriteSwap)
        {
            button.transition = Selectable.Transition.None;
            Debug.Log("[Pauser] Sound button used Sprite Swap, which overrides the icon. Switched it to None.");
        }
    }

    private void UpdateSoundIcon()
    {
        if (soundIcon == null)
            return;

        Sprite target = AudioManagerPause.IsMuted ? sound_off : sound_on;

        if (target != null)
            soundIcon.sprite = target;
    }

    private void UpdatePauseButton()
    {
        if (PauseButton == null)
            return;

        // Never show pause button on Android TV / Fire TV.
        if (isTVPlatform)
        {
            PauseButton.SetActive(false);
            return;
        }

        bool shouldShow =
            !PauseLocked &&
            !instructionPanelLock &&
            !IsGameFinished() &&
            !isPaused;

        PauseButton.SetActive(shouldShow);
    }

    public static void LockPause()
    {
        PauseLocked = true;

        if (instance != null)
        {
            if (instance.PausePannel != null)
                instance.PausePannel.SetActive(false);

            if (instance.LevelObject != null)
                instance.LevelObject.SetActive(true);

            instance.isPaused = false;
            instance.UpdatePauseButton();
        }

        Time.timeScale = 1f;
    }

    public static void UnlockPause()
    {
        PauseLocked = false;

        if (instance != null)
            instance.UpdatePauseButton();
    }

    private void OnApplicationFocus(bool focus)
    {
        isApplicationFocused = focus;

        if (!focus)
        {
            if (!PauseLocked &&
                !instructionPanelLock &&
                !isPaused &&
                !IsGameFinished())
            {
                Pause();
            }
        }
        else
        {
            ApplySoundSetting();
            UpdateSoundIcon();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
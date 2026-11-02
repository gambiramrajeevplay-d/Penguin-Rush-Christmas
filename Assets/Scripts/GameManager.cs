using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI - Finish")]
    [Tooltip("Panel shown when the player reaches the finish line.")]
    [SerializeField] private GameObject finishPanel;
    [Tooltip("Button on the finish panel that restarts the scene.")]
    [SerializeField] private Button restartButton;

    [Header("UI - Level Fail")]
    [Tooltip("Panel shown when the player hits an obstacle.")]
    [SerializeField] private GameObject levelFailPanel;
    [Tooltip("Button on the level fail panel that restarts the scene.")]
    [SerializeField] private Button failRestartButton;

    [Header("Timing")]
    [Tooltip("Extra seconds to wait after the sledge has stopped before the finish panel appears.")]
    [SerializeField] private float panelDelayAfterStop = 0.3f;
    [Tooltip("Extra seconds to wait after the sledge has stopped before the level fail panel appears.")]
    [SerializeField] private float failPanelDelayAfterStop = 0.3f;

    [Header("Audio")]
    [Tooltip("Optional. If empty, an AudioSource is created on this object automatically.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Optional sound played when the level fail panel appears.")]
    [SerializeField] private AudioClip levelFailSound;
    [Range(0f, 1f)]
    [SerializeField] private float levelFailVolume = 1f;
    [Tooltip("Optional sound played when the finish panel appears.")]
    [SerializeField] private AudioClip finishSound;
    [Range(0f, 1f)]
    [SerializeField] private float finishVolume = 1f;

    public bool IsFinished { get; private set; }
    public bool IsFailed { get; private set; }

    private void Awake()
    {
        // Simple scene-level singleton (not kept between scenes, so a restart is clean).
        Instance = this;
        Time.timeScale = 1f;

        SetupAudioSource();
    }

    // Uses the assigned AudioSource, then one already on this object, or creates one.
    private void SetupAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D sound.
    }

    private void Start()
    {
        if (finishPanel != null)
            finishPanel.SetActive(false);

        if (levelFailPanel != null)
            levelFailPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (failRestartButton != null)
            failRestartButton.onClick.AddListener(RestartGame);
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (failRestartButton != null)
            failRestartButton.onClick.RemoveListener(RestartGame);

        if (Instance == this)
            Instance = null;
    }

    // Called by the FinishLine when the player enters it.
    public void OnPlayerFinished(float stopDuration)
    {
        // Ignore if the level already ended (finished or failed).
        if (IsFinished || IsFailed)
            return;

        IsFinished = true;
        StartCoroutine(ShowFinishPanelRoutine(stopDuration + panelDelayAfterStop));
    }

    // Called by the SledgeController when it hits an obstacle.
    public void OnPlayerFailed(float stopDuration)
    {
        // Ignore if the level already ended (finished or failed).
        if (IsFinished || IsFailed)
            return;

        IsFailed = true;
        StartCoroutine(ShowFailPanelRoutine(stopDuration + failPanelDelayAfterStop));
    }

    private IEnumerator ShowFinishPanelRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (finishPanel != null)
            finishPanel.SetActive(true);

        PlaySound(finishSound, finishVolume);
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
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }

    // Make sure the mouse can click the buttons.
    private void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Hooked to both buttons automatically, also usable from the Inspector.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
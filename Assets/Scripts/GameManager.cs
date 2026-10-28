using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Panel shown when the player reaches the finish line.")]
    [SerializeField] private GameObject finishPanel;
    [Tooltip("Button on the panel that restarts the scene.")]
    [SerializeField] private Button restartButton;

    [Header("Timing")]
    [Tooltip("Extra seconds to wait after the sledge has stopped before the panel appears.")]
    [SerializeField] private float panelDelayAfterStop = 0.3f;

    public bool IsFinished { get; private set; }

    private void Awake()
    {
        // Simple scene-level singleton (not kept between scenes, so a restart is clean).
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (finishPanel != null)
            finishPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (Instance == this)
            Instance = null;
    }

    // Called by the FinishLine when the player enters it.
    public void OnPlayerFinished(float stopDuration)
    {
        if (IsFinished)
            return;

        IsFinished = true;
        StartCoroutine(ShowPanelRoutine(stopDuration + panelDelayAfterStop));
    }

    private IEnumerator ShowPanelRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (finishPanel != null)
            finishPanel.SetActive(true);

        // Make sure the mouse can click the button.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Hooked to the button automatically, also usable from the Inspector.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
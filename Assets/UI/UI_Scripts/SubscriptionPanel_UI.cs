
using UnityEngine;
using UnityEngine.UI;

public class SubscriptionPanel_UI : MonoBehaviour
{
    public Button closeButton;
    public AudioSource mainMenuAudioPlayer;
    public GameObject mainMenu;
    public GameObject carSelectionMenu;
    public AudioSource subaudio;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        AudioManagerPause.Initialize();
    }

    private void OnEnable()
    {
        AudioManagerPause.OnMuteStateChanged += HandleMuteChanged;

        if (animator != null)
            animator.SetTrigger("Entry");

        if (mainMenuAudioPlayer != null)
            mainMenuAudioPlayer.Stop();

        if (subaudio != null)
        {
            subaudio.loop = true;

            if (!AudioManagerPause.IsMuted && !subaudio.isPlaying)
                subaudio.Play();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseUI);
            closeButton.onClick.AddListener(CloseUI);
        }

        if (!SaveScript.canShowSubScriptionPanel &&
            !SaveScript.cameFromGameplay)
        {
            CloseUI();
        }
    }

    public void CloseUI()
    {
        SaveScript.canShowSubScriptionPanel = false;

        bool cameFromGameplay = SaveScript.cameFromGameplay;

        StopSubscriptionAudio();
        gameObject.SetActive(false);

        if (cameFromGameplay)
        {
            if (mainMenu != null)
                mainMenu.SetActive(false);

            if (carSelectionMenu != null)
                carSelectionMenu.SetActive(true);

            SaveScript.cameFromGameplay = false;
        }
        else
        {
            if (mainMenu != null)
                mainMenu.SetActive(true);
        }

        ResumeMainMenuAudio();
    }

    private void HandleMuteChanged()
    {
        if (AudioManagerPause.IsMuted)
        {
            if (mainMenuAudioPlayer != null)
                mainMenuAudioPlayer.Pause();

            if (subaudio != null)
                subaudio.Pause();
        }
        else if (!gameObject.activeInHierarchy)
        {
            ResumeMainMenuAudio();
        }
        else
        {
            if (subaudio != null && !subaudio.isPlaying)
                subaudio.UnPause();
        }
    }


    private void ResumeMainMenuAudio()
    {
        AudioManagerPause.Initialize();

        if (AudioManagerPause.IsMuted)
            return;

        if (mainMenuAudioPlayer == null)
        {
            Debug.LogWarning(
                "Main-menu AudioSource is not assigned.");
            return;
        }

        if (!mainMenuAudioPlayer.isPlaying)
            mainMenuAudioPlayer.Play();
    }


    private void StopSubscriptionAudio()
    {
        if (subaudio != null)
            subaudio.Stop();
    }

    private void OnDisable()
    {
        AudioManagerPause.OnMuteStateChanged -= HandleMuteChanged;
        StopSubscriptionAudio();
    }
}

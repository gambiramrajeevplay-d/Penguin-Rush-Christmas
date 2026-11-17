using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button moreGamesButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button storeCloseButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button carSelectionClose;

    [Header("Menus")]
    [SerializeField] private GameObject storeMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject subscriptionPanel;
    [SerializeField] private GameObject carSelectionMenu;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject levelselectionPanel;

    [Header("Spin Wheel")]
    [Tooltip("The spin wheel panel. Turned off by OffSpinWheel(), turned on by OpenSpinWheel().")]
    [SerializeField] private GameObject spinWheel;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Daily Spin")]
    [Tooltip("Dot/badge on the spin wheel button. Shown when a spin is available.")]
    [SerializeField] private GameObject spinIndicator;

    [Header("Sound")]
    public Image sound_;
    public Sprite sound_on, sound_off;

    private Animator animator;

    public static MainMenu instance;

    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f;

        AudioManagerPause.Initialize();
    }

    private void OnEnable()
    {
        if (CurrecnyManager.instance != null)
        {
            CurrecnyManager.instance.OnCurrencyChanged += UpdateCoins;
            UpdateCoins(CurrecnyManager.instance.GetCurrency());
        }

        UpdateSpinIndicator();
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
        if (currencyText != null)
            currencyText.text = amount.ToString();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        UpdateSoundIcon();

        // Buttons
        startButton.onClick.AddListener(OnStartButtonPressed);
        storeButton.onClick.AddListener(OpenStoreMenu);
        settingsButton.onClick.AddListener(OpenSettingsMenu);

        storeCloseButton.onClick.AddListener(OpenMainMenu);
        settingsCloseButton.onClick.AddListener(OnSettingClose);
        carSelectionClose.onClick.AddListener(OnCarSelectionClose);
        backButton.onClick.AddListener(OpenSubscriptionPanel);

        UpdateCurrencyText();
        UpdateSpinIndicator();

        // CORE ENTRY FLOW
        HandleSubscriptionEntryFlow();
    }

    // =====================
    // SUBSCRIPTION ENTRY LOGIC
    // =====================
    void HandleSubscriptionEntryFlow()
    {
        DisableAllMenus();

        bool comingFromGame =
            PlayerPrefs.GetInt("ShowSubscriptionPanel", 0) == 1;

        bool hasSubscription =
            PlayerPrefs.GetInt(StringsData.hasSubscription, 0) == 1;

        if (comingFromGame)
        {
            // Clear immediately
            PlayerPrefs.DeleteKey("ShowSubscriptionPanel");
            PlayerPrefs.Save();

            SaveScript.cameFromGameplay = true;

            if (!hasSubscription)
                subscriptionPanel.SetActive(true);
            else
                carSelectionMenu.SetActive(true);

            return; // skip main menu
        }

        // Normal fresh launch
        SaveScript.cameFromGameplay = false;
        mainMenu.SetActive(true);
    }

    // =====================
    // MAIN FLOW
    // =====================
    public void OnStartButtonPressed()
    {
        SaveScript.openedGameforFirstTime = true;
        carSelectionMenu.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
        storeMenu.SetActive(false);
        settingsMenu.SetActive(false);
        subscriptionPanel.SetActive(false);

        UpdateSpinIndicator();
        PlayMainMenuMusic();
    }

    // =====================
    // STORE / SETTINGS
    // =====================
    public void OpenStoreMenu()
    {
        storeMenu.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void OpenSettingsMenu()
    {
        settingsMenu.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void OnSettingClose()
    {
        settingsMenu.SetActive(false);
        mainMenu.SetActive(true);
        UpdateSpinIndicator();
        PlayMainMenuMusic();
    }

    // =====================
    // CAR SELECTION
    // =====================
    public void OnCarSelectionClose()
    {
        carSelectionMenu.SetActive(false);
        mainMenu.SetActive(true);
        UpdateSpinIndicator();
        PlayMainMenuMusic();
    }

    // =====================
    // SUBSCRIPTION
    // =====================
    public void OpenSubscriptionPanel()
    {
        subscriptionPanel.SetActive(true);
        mainMenu.SetActive(false);
        storeMenu.SetActive(false);
        settingsMenu.SetActive(false);
    }

    // =====================
    // CURRENCY
    // =====================
    public void UpdateCurrencyText(int amount = 0)
    {
        if (currencyText != null && CurrecnyManager.instance != null)
        {
            currencyText.text =
                CurrecnyManager.instance.GetCurrency().ToString();
        }
    }

    /// <summary>Kept for callers that want to force a coin refresh.</summary>
    public void UpdateTotalCoins()
    {
        UpdateCurrencyText();
    }

    // =====================
    // SPIN WHEEL PANEL
    // =====================
    public void OpenSpinWheel()
    {
        if (spinWheel != null)
            spinWheel.SetActive(true);

        mainMenu.SetActive(false);
    }

    /// <summary>Turns the spin wheel panel off, turns the main menu on and refreshes the indicator.</summary>
    public void OffSpinWheel()
    {
        if (spinWheel != null)
            spinWheel.SetActive(false);

        mainMenu.SetActive(true);

        UpdateSpinIndicator();
        PlayMainMenuMusic();
    }

    // =====================
    // DAILY SPIN
    // =====================
    /// <summary>
    /// Shows the spin indicator when the player has a free daily spin
    /// or a saved extra spin. Called by SpinWheelManager and on menu changes.
    /// </summary>
    public void UpdateSpinIndicator()
    {
        if (spinIndicator != null)
            spinIndicator.SetActive(SpinWheelManager.IsSpinAvailable());
    }

    // =====================
    // EXTRA
    // =====================
    public void MoreGames()
    {
        Application.OpenURL(
            "https://www.amazon.com/Games-PlayD-Game-Studio-Private-Limited/s?rh=n%3A9209902011%2Cp_4%3APlayD+Game+Studio+Private+Limited"
        );
    }

    public void Sound_on()
    {
        AudioManagerPause.IsMuted = !AudioManagerPause.IsMuted;
        UpdateSoundIcon();
        UpdateMainMenuMusic();
    }

    private void UpdateSoundIcon()
    {
        if (sound_ != null)
        {
            sound_.sprite = AudioManagerPause.IsMuted
                ? sound_off
                : sound_on;
        }
    }

    public void UpdateMainMenuMusic()
    {
        if (AudioManagerPause.IsMuted)
            return;

        // Do not play main-menu music behind the subscription panel.
        if (subscriptionPanel != null &&
            subscriptionPanel.activeInHierarchy)
            return;

        AudioSource music = null;

        // Find the main-menu music source from the subscription UI.
        SubscriptionPanel_UI subscriptionUI =
            subscriptionPanel != null
                ? subscriptionPanel.GetComponent<SubscriptionPanel_UI>()
                : null;

        if (subscriptionUI != null)
            music = subscriptionUI.mainMenuAudioPlayer;

        if (music != null && !music.isPlaying)
            music.Play();
    }

    public void PlayMainMenuMusic()
    {
        AudioManagerPause.Initialize();

        if (AudioManagerPause.IsMuted)
            return;

        // Never play menu music behind the subscription panel.
        if (subscriptionPanel != null &&
            subscriptionPanel.activeInHierarchy)
            return;

        if (mainMenu == null || !mainMenu.activeInHierarchy)
            return;

        SubscriptionPanel_UI subscriptionUI =
            subscriptionPanel != null
                ? subscriptionPanel.GetComponent<SubscriptionPanel_UI>()
                : null;

        if (subscriptionUI == null)
            return;

        AudioSource music = subscriptionUI.mainMenuAudioPlayer;

        if (music != null && !music.isPlaying)
            music.Play();
    }

    void DisableAllMenus()
    {
        mainMenu.SetActive(false);
        storeMenu.SetActive(false);
        settingsMenu.SetActive(false);
        subscriptionPanel.SetActive(false);
        carSelectionMenu.SetActive(false);
        levelselectionPanel.SetActive(false);

        if (spinWheel != null)
            spinWheel.SetActive(false);
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CarControllerUI : MonoBehaviour
{
    private const int TOTAL_CARS = 8;
    private const int SPECIAL_CAR_INDEX = 1;
    private const int SPECIAL_CAR_PRICE = 2500;

    [Header("Car Preview Objects (SCENE OBJECTS)")]
    [SerializeField] private GameObject[] allCars = new GameObject[TOTAL_CARS];

    [Header("UI")]
    [SerializeField] private TMP_Text carNameText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Image accel;
    [SerializeField] private Image boost;
    [SerializeField] private Image handling;

    [Header("Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject selectButton;
    [SerializeField] private GameObject unlock2500Button;
    [SerializeField] private GameObject[] buyButtons;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject carSelectionPanel;
    [SerializeField] private GameObject levelselectionPanel;

    [Header("Stats")]
    [SerializeField] private float[] accelVal;
    [SerializeField] private float[] boostVal;
    [SerializeField] private float[] handlingVal;

    [Header("Unlock Panels")]
    [SerializeField] private GameObject unlockAllPanel;
    [SerializeField] private GameObject unlockDummyPanel;

    [Header("Dummy Unlock Settings")]
    [SerializeField] private int unlockFourCount = 4;

    // Alternates every time a car is selected:
    // 1st select -> Unlock Dummy, 2nd -> Unlock All, 3rd -> Unlock Dummy, ...
    // Static so it survives scene reloads (e.g. returning from gameplay) during the same session.
    private static bool showUnlockAllNext = false;

    [Header("Not Enough Coins Popup")]
    [SerializeField] private GameObject notEnoughCoinsPopup;
    [SerializeField] private float notEnoughPopupDuration = 5f;
    private Coroutine notEnoughPopupCoroutine;

    [Header("Names")]
    [SerializeField]
    private string[] carNames =
    {
        //"Nitrohawk",
        //"Hyperion GT",
        //"BlazeRunner",
        //"AeroVex",
        //"Stormflare",
        //"TurboSpectre",
        //"Nitro Beast",
        //"Shadow RX",
        //"Vortex Z"
    };

    [Header("Temporary Unlock (SESSION ONLY)")]
    [SerializeField] private bool tempUnlockAllCars = false;

    public int presentCar = 0;

    // =========================
    // UNITY
    // =========================

    void Start()
    {
        PlayerPrefs.SetInt("car0", 1);

        leftButton.onClick.AddListener(Left);
        rightButton.onClick.AddListener(Right);
        closeButton.onClick.AddListener(OnClose);

        presentCar = PlayerPrefs.GetInt("selectedTruck", 0);

        RefreshCar();
    }

    void OnEnable()
    {
        RefreshCar();

        // Subscribe to coin changes
        if (CurrecnyManager.instance != null)
        {
            CurrecnyManager.instance.OnCurrencyChanged += UpdateCoins;
            UpdateCoins(CurrecnyManager.instance.GetCurrency());
        }
    }

    void OnDisable()
    {
        // Unsubscribe (important)
        if (CurrecnyManager.instance != null)
        {
            CurrecnyManager.instance.OnCurrencyChanged -= UpdateCoins;
        }
    }

    // =========================
    // CURRENCY
    // =========================

    void UpdateCoins(int amount)
    {
        if (currencyText != null)
            currencyText.text = amount.ToString();
    }

    // =========================
    // NAVIGATION
    // =========================

    public void Left()
    {
        presentCar = (presentCar - 1 + TOTAL_CARS) % TOTAL_CARS;
        RefreshCar();
    }

    public void Right()
    {
        presentCar = (presentCar + 1) % TOTAL_CARS;
        RefreshCar();
    }

    void RefreshCar()
    {
        TurnOffAllCars();

        if (allCars[presentCar] != null)
            allCars[presentCar].SetActive(true);

        UpdateCarName();
        UpdateStatsUI();
        UpdateButtons();

        PlayerPrefs.SetInt("selectedTruck", presentCar);
        PlayerPrefs.Save();
    }

    void TurnOffAllCars()
    {
        foreach (var car in allCars)
            if (car != null) car.SetActive(false);
    }

    // =========================
    // UI
    // =========================

    void UpdateCarName()
    {
        if (carNameText != null && presentCar < carNames.Length)
            carNameText.text = carNames[presentCar];
    }

    void UpdateStatsUI()
    {
        if (presentCar < accelVal.Length)
            accel.fillAmount = accelVal[presentCar];

        if (presentCar < boostVal.Length)
            boost.fillAmount = boostVal[presentCar];

        if (presentCar < handlingVal.Length)
            handling.fillAmount = handlingVal[presentCar];
    }

    // =========================
    // UNLOCK LOGIC
    // =========================

    bool IsUnlocked(int index)
    {
        if (tempUnlockAllCars) return true;
        if (index == 0) return true;
        return PlayerPrefs.GetInt("car" + index, 0) == 1;
    }

    void UpdateButtons()
    {
        bool unlocked = IsUnlocked(presentCar);

        selectButton.SetActive(false);
        unlock2500Button.SetActive(false);

        foreach (var b in buyButtons)
            if (b != null) b.SetActive(false);

        if (unlocked)
        {
            selectButton.SetActive(true);
            return;
        }

        if (presentCar == SPECIAL_CAR_INDEX)
        {
            unlock2500Button.SetActive(true);
            if (presentCar < buyButtons.Length && buyButtons[presentCar] != null)
                buyButtons[presentCar].SetActive(true);
            return;
        }

        if (presentCar < buyButtons.Length && buyButtons[presentCar] != null)
            buyButtons[presentCar].SetActive(true);
    }

    // =========================
    // DUMMY UNLOCK FLOW
    // =========================

    public void OnDummyContinueClicked()
    {
        if (unlockDummyPanel != null)
            unlockDummyPanel.SetActive(false);

        if (levelselectionPanel != null)
            levelselectionPanel.SetActive(true);
    }

    public void UnlockFourCarsDummy()
    {
        int unlocked = 0;
        for (int i = 1; i < TOTAL_CARS && unlocked < unlockFourCount; i++)
        {
            if (PlayerPrefs.GetInt("car" + i, 0) == 0)
            {
                PlayerPrefs.SetInt("car" + i, 1);
                unlocked++;
            }
        }
        PlayerPrefs.Save();
    }

    public void UnlockAllCarsDummy()
    {
        for (int i = 1; i < TOTAL_CARS; i++)
            PlayerPrefs.SetInt("car" + i, 1);

        PlayerPrefs.Save();
    }

    // =========================
    // BUTTON ACTIONS
    // =========================

    public void buy()
    {
        PlayerPrefs.SetInt("car" + presentCar.ToString(), 1);
        PlayerPrefs.Save();
        UpdateButtons();
    }

    public void UnlockSpecialCar()
    {
        if (CurrecnyManager.instance == null) return;

        if (!CurrecnyManager.instance.SpendCurrency(SPECIAL_CAR_PRICE))
        {
            ShowNotEnoughCoinsPopup();
            return;
        }

        PlayerPrefs.SetInt("car" + SPECIAL_CAR_INDEX, 1);
        PlayerPrefs.Save();

        UpdateButtons();
        StartCoroutine(ForceSelectButton());
    }

    IEnumerator ForceSelectButton()
    {
        yield return null;
        if (selectButton != null && selectButton.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectButton);
        }
    }

    void ShowNotEnoughCoinsPopup()
    {
        if (notEnoughCoinsPopup == null) return;

        if (notEnoughPopupCoroutine != null)
            StopCoroutine(notEnoughPopupCoroutine);

        notEnoughPopupCoroutine = StartCoroutine(NotEnoughCoinsRoutine());
    }

    IEnumerator NotEnoughCoinsRoutine()
    {
        notEnoughCoinsPopup.SetActive(true);
        yield return new WaitForSeconds(notEnoughPopupDuration);
        notEnoughCoinsPopup.SetActive(false);
        notEnoughPopupCoroutine = null;
    }

    public void SelectCar()
    {
        PlayerPrefs.SetInt("selectedTruck", presentCar);
        PlayerPrefs.Save();

        carSelectionPanel.SetActive(false);
        levelselectionPanel.SetActive(false);
        TurnOffAllCars();

        // Alternate: Dummy -> All -> Dummy -> All ...
        GameObject panelToShow = showUnlockAllNext ? unlockAllPanel : unlockDummyPanel;

        // Fallback to the other panel if the chosen one isn't assigned.
        if (panelToShow == null)
            panelToShow = showUnlockAllNext ? unlockDummyPanel : unlockAllPanel;

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
        else
        {
            // No unlock panel assigned at all, go straight to level selection.
            levelselectionPanel.SetActive(true);
        }

        // Flip for the next time the player selects a car.
        showUnlockAllNext = !showUnlockAllNext;
    }

    // DO NOT REMOVE
    public static void CloseUnlockPanelStatic()
    {
        CarControllerUI ui = FindObjectOfType<CarControllerUI>(true);
        if (ui == null) return;

        if (ui.unlockAllPanel != null)
            ui.unlockAllPanel.SetActive(false);

        if (ui.unlockDummyPanel != null)
            ui.unlockDummyPanel.SetActive(false);

        if (ui.levelselectionPanel != null)
            ui.levelselectionPanel.SetActive(true);
    }

    public void OnClose()
    {
        carSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        levelselectionPanel.SetActive(false);
        TurnOffAllCars();
    }

    // =========================
    // TEMP UNLOCK
    // =========================

    public void EnableTempUnlockAllCars()
    {
        tempUnlockAllCars = true;
        UpdateButtons();
    }

    public void DisableTempUnlockAllCars()
    {
        tempUnlockAllCars = false;
        UpdateButtons();
    }
}
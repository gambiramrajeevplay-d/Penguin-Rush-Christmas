using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpinWheelManager : MonoBehaviour
{
    [System.Serializable]
    public class WheelReward
    {
        public string rewardName;
        public int rewardAmount = 100;

        [Header("Reward Settings")]
        public bool isCharacterReward = false;
    }

    // Shared keys so MainMenu can read the spin state without needing this panel to be active.
    public const string LastSpinDateKey = "DailySpinLastClaimDate";
    public const string ExtraSpinsKey = "DailySpinExtraSpins";

    [Header("Wheel")]
    public RectTransform wheel;
    public Button spinButton;

    [Header("Rewards - Clockwise From Top")]
    public WheelReward[] rewards;

    [Header("Spin Again Reward")]
    public string spinAgainRewardName = "Spin Again";

    [Header("Wheel Alignment")]
    public float rewardCenterOffset = 0f;

    [Header("Spin Settings")]
    [Min(1)] public int fullRotations = 6;
    [Min(0.1f)] public float rotationSpeed = 720f;
    [Min(0.1f)] public float spinDuration = 6f;

    [Header("Reward UI")]
    public TMP_Text rewardText;
    public GameObject rewardPanel;

    [Header("Daily Spin Settings")]
    public bool enableTesting = false;
    public bool allowSpinAgainInTesting = false;

    [Header("Visit Again UI")]
    public GameObject visitAgain;
    public float visitAgainDuration = 3f;

    [Header("Testing")]
    public bool forceSpinAgainReward = false;

    private bool isSpinning;
    private bool hasSpunToday;

    private float initialWheelAngle;
    private Vector2 initialAnchoredPosition;
    private Vector3 initialLocalPosition;
    private Vector3 initialLocalScale;

    // Extra spins (from "Spin Again") are saved so the main menu indicator stays correct.
    private int extraSpinsAvailable
    {
        get { return PlayerPrefs.GetInt(ExtraSpinsKey, 0); }
        set
        {
            PlayerPrefs.SetInt(ExtraSpinsKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    // ---------- Static helpers (used by MainMenu) ----------

    private static string Today()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    /// <summary>True if the player has a free daily spin or a saved extra spin.</summary>
    public static bool IsSpinAvailable()
    {
        bool spunToday = PlayerPrefs.GetString(LastSpinDateKey, "") == Today();
        bool hasExtra = PlayerPrefs.GetInt(ExtraSpinsKey, 0) > 0;
        return !spunToday || hasExtra;
    }

    private void NotifyMenu()
    {
        if (MainMenu.instance != null)
            MainMenu.instance.UpdateSpinIndicator();
    }

    // ---------- Unity ----------

    private void Start()
    {
        if (wheel == null)
        {
            Debug.LogError("SpinWheelManager: Assign the Wheel RectTransform.");
            enabled = false;
            return;
        }

        if (rewards == null || rewards.Length < 2)
        {
            Debug.LogError("SpinWheelManager: Add at least two reward sections.");
            enabled = false;
            return;
        }

        initialWheelAngle = wheel.localEulerAngles.z;
        initialAnchoredPosition = wheel.anchoredPosition;
        initialLocalPosition = wheel.localPosition;
        initialLocalScale = wheel.localScale;

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (spinButton != null)
            spinButton.onClick.AddListener(SpinWheel);

        if (visitAgain != null)
            visitAgain.SetActive(false);

        CheckDailySpin();
        UpdateDailySpinUI();
        NotifyMenu();
    }

    private void OnEnable()
    {
        // Refresh when the panel is reopened (e.g. date changed while the app was in background).
        if (wheel != null)
        {
            CheckDailySpin();
            UpdateDailySpinUI();
        }
    }

    private void CheckDailySpin()
    {
        if (enableTesting)
        {
            hasSpunToday = false;
            return;
        }

        hasSpunToday = PlayerPrefs.GetString(LastSpinDateKey, "") == Today();
    }

    private List<int> GetValidRewardIndices()
    {
        List<int> validRewards = new List<int>();

        for (int i = 0; i < rewards.Length; i++)
        {
            if (!rewards[i].isCharacterReward)
                validRewards.Add(i);
        }

        return validRewards;
    }

    private int GetRandomValidRewardIndex()
    {
        List<int> validRewards = GetValidRewardIndices();

        if (validRewards.Count == 0)
        {
            Debug.LogError("SpinWheelManager: No valid rewards available!");
            return -1;
        }

        return validRewards[UnityEngine.Random.Range(0, validRewards.Count)];
    }

    public void SpinWheel()
    {
        if (isSpinning)
            return;

        if (rewards == null || rewards.Length < 2)
            return;

        if (GetValidRewardIndices().Count == 0)
        {
            Debug.LogError("SpinWheelManager: All rewards are character rewards.");
            return;
        }

        CheckDailySpin();

        if (hasSpunToday &&
            extraSpinsAvailable <= 0 &&
            !(enableTesting && allowSpinAgainInTesting))
        {
            UpdateDailySpinUI();

            if (visitAgain != null)
                StartCoroutine(ShowVisitAgain());

            Debug.Log("Today's free spin has already been used.");
            return;
        }

        if (spinButton != null)
            spinButton.interactable = false;

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (extraSpinsAvailable > 0)
        {
            extraSpinsAvailable--;
        }
        else if (!enableTesting)
        {
            PlayerPrefs.SetString(LastSpinDateKey, Today());
            PlayerPrefs.Save();

            hasSpunToday = true;
        }

        NotifyMenu();

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator ShowVisitAgain()
    {
        visitAgain.SetActive(true);

        yield return new WaitForSeconds(visitAgainDuration);

        visitAgain.SetActive(false);
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        UpdateDailySpinUI();

        int rewardIndex;

        if (forceSpinAgainReward)
        {
            rewardIndex = Array.FindIndex(
                rewards,
                reward =>
                    !reward.isCharacterReward &&
                    string.Equals(
                        reward.rewardName.Trim(),
                        spinAgainRewardName.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
            );

            if (rewardIndex < 0)
            {
                Debug.LogError("SpinWheelManager: Valid Spin Again reward not found.");
                isSpinning = false;
                UpdateDailySpinUI();
                yield break;
            }
        }
        else
        {
            rewardIndex = GetRandomValidRewardIndex();

            if (rewardIndex < 0)
            {
                isSpinning = false;
                UpdateDailySpinUI();
                yield break;
            }
        }

        float sectionAngle = 360f / rewards.Length;

        float desiredAngle = initialWheelAngle
                             + rewardIndex * sectionAngle
                             + rewardCenterOffset;

        float startAngle = wheel.localEulerAngles.z;

        float extraAngle = Mathf.Repeat(desiredAngle - startAngle, 360f);

        float minimumAngle = fullRotations * 360f + extraAngle;

        float speedBasedAngle = rotationSpeed * spinDuration * 0.5f;

        float totalRotation = Mathf.Max(minimumAngle, speedBasedAngle);

        totalRotation = extraAngle +
                        Mathf.Max(
                            fullRotations * 360f,
                            Mathf.Ceil((totalRotation - extraAngle) / 360f) * 360f
                        );

        float targetAngle = startAngle + totalRotation;

        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / spinDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 4f);
            float angle = Mathf.Lerp(startAngle, targetAngle, easedProgress);

            wheel.anchoredPosition = initialAnchoredPosition;
            wheel.localPosition = initialLocalPosition;
            wheel.localScale = initialLocalScale;
            wheel.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        wheel.anchoredPosition = initialAnchoredPosition;
        wheel.localPosition = initialLocalPosition;
        wheel.localScale = initialLocalScale;
        wheel.localRotation = Quaternion.Euler(0f, 0f, targetAngle);

        WheelReward selectedReward = rewards[rewardIndex];

        bool isSpinAgain = string.Equals(
            selectedReward.rewardName.Trim(),
            spinAgainRewardName.Trim(),
            StringComparison.OrdinalIgnoreCase
        );

        if (isSpinAgain)
            extraSpinsAvailable++;

        // Coins go through CurrecnyManager. It saves to PlayerPrefs and fires
        // OnCurrencyChanged, which MainMenu listens to, so the UI updates itself.
        if (selectedReward.rewardAmount > 0)
        {
            if (CurrecnyManager.instance != null)
                CurrecnyManager.instance.AddRewardCoins(selectedReward.rewardAmount);
            else
                Debug.LogWarning("SpinWheelManager: CurrecnyManager not found, coins not added.");
        }

        if (rewardText != null)
        {
            rewardText.text = selectedReward.rewardAmount > 0
                ? "Received " + selectedReward.rewardAmount + " Candies"
                : selectedReward.rewardName;
        }

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        isSpinning = false;
        UpdateDailySpinUI();
        NotifyMenu();

        Debug.Log(
            "Spin result: " + selectedReward.rewardName +
            ", Coins: " + selectedReward.rewardAmount +
            ", Extra Spins: " + extraSpinsAvailable
        );
    }

    private void UpdateDailySpinUI()
    {
        if (spinButton != null)
            spinButton.interactable = !isSpinning;
    }

    public void RefreshDailySpin()
    {
        CheckDailySpin();
        UpdateDailySpinUI();
        NotifyMenu();
    }

    /// <summary>
    /// Hook this to the spin wheel's Close/Back button.
    /// Turns off the spin wheel panel and turns the main menu back on.
    /// Ignored while the wheel is spinning.
    /// </summary>
    public void CloseSpinWheel()
    {
        if (isSpinning)
            return;

        CloseRewardPanel();

        if (visitAgain != null)
            visitAgain.SetActive(false);

        if (MainMenu.instance != null)
            MainMenu.instance.OffSpinWheel();
        else
            gameObject.SetActive(false);
    }

    public void CloseRewardPanel()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    public bool HasExtraSpinsAvailable
    {
        get { return extraSpinsAvailable > 0; }
    }

    private void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(SpinWheel);
    }
}
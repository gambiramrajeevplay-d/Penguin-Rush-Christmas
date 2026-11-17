using System;
using UnityEngine;

public class CurrecnyManager : MonoBehaviour
{
    public static CurrecnyManager instance;

    public event Action<int> OnCurrencyChanged;

    [Header("Currency")]
    [SerializeField] private int currentAmount;

    private const string COIN_KEY = "TotalCoins";

    // 🔥 AUTO-CREATE BEFORE SCENE LOADS
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("CurrencyManager");
            obj.AddComponent<CurrecnyManager>();
        }
    }

    private void Awake()
    {
        // ✅ Singleton safety
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ Load saved coins
        currentAmount = PlayerPrefs.GetInt(COIN_KEY, 0);
    }

    // ---------------- COINS ----------------

    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;

        currentAmount += amount;
        Save();
    }
    public void AddRewardCoins(int amount)
    {
        AddCurrency(amount);

        Debug.Log("Reward Coins Added: " + amount);
    }
    public bool SpendCurrency(int amount)
    {
        if (currentAmount < amount)
            return false;

        currentAmount -= amount;
        Save();
        return true;
    }

    public int GetCurrency()
    {
        return currentAmount;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(COIN_KEY, currentAmount);
        PlayerPrefs.Save();

        // 🔥 Notify UI automatically
        OnCurrencyChanged?.Invoke(currentAmount);
    }

    // ---------------- UNLOCKS ----------------

    public void UnlockCar(int index)
    {
        PlayerPrefs.SetInt("car" + index, 1);
        PlayerPrefs.Save();
    }

    public bool IsCarUnlocked(int index)
    {
        return PlayerPrefs.GetInt("car" + index, 0) == 1;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IAP_Handler : MonoBehaviour
{

    public static IAP_Handler instance;

    public bool hasSubscription;
    public GameObject purchaseFailedUI;

    private Button defaultButtonToSelect;

    private CurrecnyManager currencyManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.Log("IAP Handler Already Exsists!");
        }
        if (PlayerPrefs.GetInt(StringsData.hasSubscription) == 1)
        {
            hasSubscription = true;
        }
        else
        {
            hasSubscription = false;
        }


    }

    private void Start()
    {
        currencyManager = CurrecnyManager.instance;
    }

    public void Buy_coinpacks(int val)
    {
        currencyManager?.AddCurrency(val);

        if (MainMenu.instance != null)
        {
            MainMenu.instance.UpdateCurrencyText();
        }
    }
    public void failedTest(Button _buttonToSelect)
    {
        if (_buttonToSelect != null)
        {
            defaultButtonToSelect = _buttonToSelect;
        }
        purchaseFailedUI.SetActive(true);
    }
    public void subscribedAny()
    {
        //full game access
        //unlock all vehicles
        //
        for (int i = 1; i < 9; i++)
        {
            if (PlayerPrefs.GetInt("car" + i, 0) == 0)
            {
                PlayerPrefs.SetInt("car" + i, 1);
                //unlocked++;
            }
        }
        hasSubscription = true;
        //  PlayerPrefs.SetInt(StringsData.hasSubscription, 1);
        UnlockFullGame();




    }

    public void UnlockFullGame()
    {
        UnlockAllCars();
        UnlockAllLevels();
    }

    public void UnlockAllCharacters()
    {
        for (int i = 1; i < 9; i++)
        {
            if (PlayerPrefs.GetInt("car" + i, 0) == 0)
            {
                PlayerPrefs.SetInt("car" + i, 1);
                //unlocked++;
            }
        }
    }

    public void BuyFourCars()
    {
        if (CurrecnyManager.instance == null) return;

        int unlocked = 0;

        for (int i = 1; i < 9 && unlocked < 4; i++)
        {
            if (!CurrecnyManager.instance.IsCarUnlocked(i))
            {
                CurrecnyManager.instance.UnlockCar(i);
                unlocked++;
            }
        }

        Debug.Log("Unlocked 4 cars via IAP");
    }

    public void UnlockAllCars()
    {
        if (CurrecnyManager.instance == null) return;

        for (int i = 0; i < 9; i++) // 0 to 8 (total cars)
        {
            CurrecnyManager.instance.UnlockCar(i);
        }

        PlayerPrefs.Save();
        Debug.Log("All cars unlocked via IAP");
    }

    public void BuyCharacter(int _characterIndex)
    {
        PlayerPrefs.SetInt("car" + _characterIndex, 1);
        PlayerPrefs.Save();

        Debug.Log("Car " + _characterIndex + " unlocked");
    }
    public void UnlockFourCarsDummy()
    {
        if (CurrecnyManager.instance == null) return;

        int unlocked = 0;

        for (int i = 1; i < 9 && unlocked < 4; i++)
        {
            if (!CurrecnyManager.instance.IsCarUnlocked(i))
            {
                CurrecnyManager.instance.UnlockCar(i);
                unlocked++;
            }
        }
        PlayerPrefs.Save();
    }
    public void UnlockAllLevels()
    {
        PlayerPrefs.SetInt("FullGame", 1);// modes unlock
        PlayerPrefs.SetInt(StringsData.unlockedAllLevels, 1);// enemy unlock


    }

    public void RemoveAds()
    {


    }
}



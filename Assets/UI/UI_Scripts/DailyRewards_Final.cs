using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;


public class DailyRewards_Final : MonoBehaviour
{
    private int dayValue;
    public GameObject rewards;

    public GameObject subscriptionPanel;
    private bool rewardClaimed = false;

    // Start is called before the first frame update
    void Start()
    {
        dayValue = PlayerPrefs.GetInt("DayValue", 0);
        CheckDailyRewards();
        SetDailyRewardButtonInteraction();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Close()
    {
       // MenuScript.instance.Sub.SetActive(true);
        if(subscriptionPanel != null)
        {
            subscriptionPanel.SetActive(true); 
        }
        rewards.SetActive(false);
    }
    System.DateTime NextRewardTime, FirstRewardTime;
    public void CheckDailyRewards()
    {
        //PlayerPrefs.GetString("Day", DateTime.Now.ToString());
        //PlayerPrefs.SetString("Day", DateTime.Now.ToString());
        //Show Daily Rewards Screen
       

        if (DateLoader.Date == null && dayValue < 7)
        {
            //First Time run
            if (PlayerPrefs.GetString("Day") == "")
            {
                

                ButtonHighlighter buttonHighlighter = rewards.GetComponent<ButtonHighlighter>();
                if (buttonHighlighter != null && rewardButtons != null && rewardButtons.Count > dayValue && rewardButtons[dayValue].gameObject != null)
                {

                    buttonHighlighter.defaultButton = rewardButtons[dayValue].gameObject;
                    rewards.SetActive(true);    



                    Debug.Log("You got a reward today");
                }

                Debug.Log("You got a reward today");

            }
            else
            {
                string s = PlayerPrefs.GetString("Day");
                NextRewardTime = Convert.ToDateTime(s);

                if (NextRewardTime.Subtract(DateTime.Now).Hours <= 0)
                {

                    rewards.SetActive(true);
                    ButtonHighlighter buttonHighlighter = rewards.GetComponent<ButtonHighlighter>();
                    if (buttonHighlighter != null && rewardButtons != null && rewardButtons.Count > dayValue && rewardButtons[dayValue].gameObject != null)
                    {
                        buttonHighlighter.defaultButton = rewardButtons[dayValue].gameObject;
                        rewards.SetActive(true);

                        Debug.Log("You got a reward today");
                    }
                    Debug.Log("You got a reward today");
                }
                else
                {
                    rewards.SetActive(false);
                    //MenuScript.instance.Sub.SetActive(true);
                    Debug.LogWarning("You already claimed reward");
                    if (subscriptionPanel != null)
                    {
                        subscriptionPanel.SetActive(true);
                    }


                }
            }

            DateLoader.Date = PlayerPrefs.GetString("Day");
        }
        else
        {
            //MenuScript.instance.Sub.SetActive(true);
            if (subscriptionPanel != null)
            {
                subscriptionPanel.SetActive(true);
            }
            print("Condition checking");
        }


        //Show Spin wheel if wheels > 0
    }
    public List<Button> rewardButtons = new List<Button>();
    public List<int> dailyCoins = new List<int> { 100, 150, 200, 250, 300, 350, 400 };
    public List<Sprite> dailySprites = new List<Sprite>();
    public GameObject rewardPopupPanel;
    public TextMeshProUGUI rewardPopupText;
    public Image rewardImage;
    public void RewardClaimButton()
    {
        // 🔥 BLOCK DOUBLE CLAIM
        if (rewardClaimed)
            return;

        rewardClaimed = true;

        FirstRewardTime = DateTime.Now;

        NextRewardTime =
            FirstRewardTime.AddDays(1);

        PlayerPrefs.SetString(
            "Day",
            NextRewardTime.ToString()
        );

        int coinsForToday =
            dailyCoins[dayValue];

        if (dayValue < 7)
        {
            CurrecnyManager.instance?.
                AddCurrency(coinsForToday);

            ShowRewardPopup(coinsForToday);
        }
        else
        {
            CurrecnyManager.instance
                .UnlockCar(2);

            ShowRewardPopup();
        }

        dayValue++;

        PlayerPrefs.SetInt(
            "DayValue",
            dayValue
        );

        if (dayValue >= 7)
        {
            dayValue = 0;

            PlayerPrefs.SetInt(
                "DayValue",
                dayValue
            );
        }

        PlayerPrefs.Save();

        // 🔥 DISABLE BUTTONS
        foreach (Button b in rewardButtons)
        {
            b.interactable = false;
        }
    }
    public void UnhighlightAllButtons()
    {
        foreach(Button _button in rewardButtons)
        {
           ButtonHighlighter buttonHighlighter = rewards.GetComponent<ButtonHighlighter>();
           // buttonHighlighter.UnHighlightButton(_button);
        }
    }
    void SetDailyRewardButtonInteraction()
    {
        foreach (Button b in rewardButtons)
        {
            b.interactable = false;
        }
        rewardButtons[dayValue].interactable = true;
    }
    void ShowRewardPopup(int coins)
    {
        rewardPopupText.text = $" Received {coins} coins...";
        //rewardImage.sprite = dailySprites[dayValue];  // Change the sprite based on the current day
        rewardPopupPanel.SetActive(true);
        foreach (Button b in rewardButtons)
        {
            b.interactable = false;
        }// Activate the panel to show the message

    }

    void ShowRewardPopup()
    {
        rewardPopupText.text = "Unlocked Car";
        rewardPopupPanel.SetActive(true);
        foreach (Button b in rewardButtons)
        {
            b.interactable = false;
        }
    }

    public void Okay()
    {
        rewardPopupPanel.SetActive(false);
        Close();
        //MenuScript.instance.Sub.SetActive(true);
        
    }
    public int GetCoinsForToday()
    {
        return dailyCoins[dayValue];
    }
    // Hide the panel after delay



}

public static class DateLoader
{
    public static string Date = null;
    public static int volume = 1;
}





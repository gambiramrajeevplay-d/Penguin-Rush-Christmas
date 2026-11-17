using Script;
using UnityEngine;

public class ControlsManager : MonoBehaviour
{
    public GameObject tvControls;
    public GameObject tabControls;

    private const string FIRST_TIME_KEY = "FirstTimeStartShown";

    private void Start()
    {
        int currentLevel = PlayerPrefs.GetInt(StringsData.levelToLoad, 1);
        bool isFirstTime = PlayerPrefs.GetInt(FIRST_TIME_KEY, 0) == 0;

        //  Not first level or already shown → do nothing
        if (currentLevel != 1 || !isFirstTime)
            return;

        //  Show correct controls once
        if (AndroidTV.IsAndroidOrFireTv())
        {
            tvControls.SetActive(true);
            tabControls.SetActive(false);
        }
        else
        {
            tabControls.SetActive(true);
            tvControls.SetActive(false);
        }

        //  Mark as shown
        PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
        PlayerPrefs.Save();

        //  Auto hide after 4 seconds (optional)
        Invoke(nameof(HideControls), 10f);
    }

    private void HideControls()
    {
        if (tvControls) tvControls.SetActive(false);
        if (tabControls) tabControls.SetActive(false);
    }
}

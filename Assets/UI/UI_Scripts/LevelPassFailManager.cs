using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPassFailManager : MonoBehaviour
{
    public void OnHomeButtonClicked()
    {
        // 🛑 SAFETY RESET
        Time.timeScale = 1f;

        // 🔔 TELL UI WE CAME FROM LEVEL (WIN OR FAIL)
        PlayerPrefs.SetInt("ShowSubscriptionPanel", 1);
        PlayerPrefs.Save();

        SaveScript.cameFromGameplay=true;

        // 🏠 LOAD UI SCENE
        SceneManager.LoadScene("UI");
    }
}

using Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InstructionsUI : MonoBehaviour
{
    [SerializeField] private Button okayButton;
  //  [SerializeField] private Button backButtonButton;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject tabLoadingScreen;

    private void Start()
    {
        okayButton.onClick.AddListener(OnOkayClick);
    }

    private void OnOkayClick()
    {
        StartCoroutine(LoadLevelWithDelay());
    }

    private IEnumerator LoadLevelWithDelay()
    {

        if (AndroidTV.IsAndroidOrFireTv())
        {
            // Show TV loading panel
           loadingScreen.SetActive(true);
        }
        else
        {
            // Show tablet loading panel
             tabLoadingScreen.SetActive(true);
           
        }
      
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(1);
    }
}

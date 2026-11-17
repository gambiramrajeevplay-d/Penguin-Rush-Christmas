using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonHandler : MonoBehaviour
{
    public GameObject exitPopup;
    //public string homeSceneName = "SpalshScreen"; // Mee Home scene name
    public AudioSource sound;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {


            exitPopup.SetActive(true); // Show popup
            sound.Pause();

        }
    }

    public void OnClickYes()
    {
        Application.Quit();
    }

    public void OnClickNo()
    {
        exitPopup.SetActive(false);
        sound.Play();
    }
}
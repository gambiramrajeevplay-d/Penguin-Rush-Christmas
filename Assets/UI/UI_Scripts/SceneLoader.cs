using Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;

    public string sceneToLoad;
    public GameObject controlsImage;

    public Animator animator;

    public AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        animator = GetComponent<Animator>();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if(!AndroidTV.IsAndroidOrFireTv())
        {
            controlsImage.SetActive(false);
        }
        if(SaveScript.isGameMuted == false)
        {
            audioSource.enabled = true;
            audioSource.Play();
        }
    }


    public void LoadScene(string _sceneToLoad)
    {
        sceneToLoad = _sceneToLoad;
        animator.SetTrigger("Start");
    }

    

    public void LoadSceneAnimationTrigger()
    {
        if (sceneToLoad == string.Empty)
        {
            sceneToLoad = StringsData.mainMenuScene;
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    public void DisableLoadingScreen()
    {
        animator.SetTrigger("Mid");
        StartCoroutine(DisableUI()); 
    }

    private IEnumerator DisableUI()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }
}

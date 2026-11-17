using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LoadingBarImage : MonoBehaviour
{
    public Image fillImage; // Assign UI Image with Fill Method
    public float loadingTime = 3f;

    private void Start()
    {
        StartCoroutine(FillImage());
    }

    IEnumerator FillImage()
    {
        float elapsed = 0f;
        while (elapsed < loadingTime)
        {
            elapsed += Time.deltaTime;
            fillImage.fillAmount = Mathf.Clamp01(elapsed / loadingTime);
            yield return null;
        }
        fillImage.fillAmount = 1f;
        Debug.Log("Loading Complete!");
    }
}

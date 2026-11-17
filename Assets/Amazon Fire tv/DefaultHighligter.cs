using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DefaultHighligter : MonoBehaviour
{
    public static DefaultHighligter instance;
    public GameObject DefaultObj;
    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(DefaultObj);
    }
    public void Defaulter(GameObject nextbutton)
    {
        EventSystem.current.SetSelectedGameObject(nextbutton);
    }
}

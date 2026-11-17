using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentMove : MonoBehaviour
{
    public static ContentMove instance;

    public RectTransform content;
    public int selectedIndex = 0;
    public float smoothSpeed = 2.5f;
    public GameObject SelectedObj;
    public float[] positions;

    [Tooltip("Tick this if the content moves down when it should move up (or the reverse)")]
    public bool invertDirection = true;

    private void Awake()
    {
        instance = this;
    }

    public void Content_move()
    {
        if (positions == null || positions.Length == 0 || SelectedObj == null)
            return;

        levelid level = SelectedObj.GetComponent<levelid>();
        if (level == null)
            return;

        int id = level.id;
        if (id < 0 || id >= positions.Length)
            return;

        float targetY = invertDirection ? -positions[id] : positions[id];
        Vector2 targetPosition = new Vector2(content.anchoredPosition.x, targetY);

        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
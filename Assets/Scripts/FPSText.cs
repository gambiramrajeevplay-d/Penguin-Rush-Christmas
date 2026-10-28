using System.Reflection;
using UnityEngine;

// Shows the FPS in a UI text. Works with both the legacy UI Text and TextMeshPro
// (it only needs a component with a "text" property), so it compiles either way.
public class FPSText : MonoBehaviour
{
    [Header("Target Text")]
    [Tooltip("Drag your Text or TextMeshPro object here. Leave empty to use one on this same object.")]
    [SerializeField] private Component targetText;

    [Header("Display")]
    [SerializeField] private bool showFrameTime = true;
    [SerializeField] private bool showMinMax = false;
    [Tooltip("Colors the text green / yellow / red depending on the FPS.")]
    [SerializeField] private bool useColors = true;

    [Header("Update")]
    [Tooltip("Seconds between each refresh. Higher = steadier number.")]
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Colors")]
    [SerializeField] private float goodFPS = 55f;
    [SerializeField] private float okFPS = 30f;
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color okColor = Color.yellow;
    [SerializeField] private Color badColor = Color.red;

    private PropertyInfo textProperty;
    private PropertyInfo colorProperty;
    private Component resolvedText;

    private float accumulatedTime;
    private int frameCount;

    private float minFPS = float.MaxValue;
    private float maxFPS;

    private void Awake()
    {
        ResolveText();
    }

    private void ResolveText()
    {
        resolvedText = null;
        textProperty = null;
        colorProperty = null;

        // 1) The assigned component (or any component on the same object as it).
        // 2) A text component on this object.
        Component source = targetText != null ? targetText : this;

        foreach (Component c in source.GetComponents<Component>())
        {
            if (c == null || c == this)
                continue;

            PropertyInfo text = c.GetType().GetProperty("text", typeof(string));

            if (text != null && text.CanWrite)
            {
                resolvedText = c;
                textProperty = text;
                colorProperty = c.GetType().GetProperty("color", typeof(Color));
                return;
            }
        }

        Debug.LogWarning("FPSText: no Text / TextMeshPro found. Assign one in 'Target Text'.", this);
    }

    private void Update()
    {
        if (resolvedText == null)
            return;

        // Unscaled time so the counter stays correct even if timeScale changes.
        accumulatedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (accumulatedTime < updateInterval)
            return;

        float fps = frameCount / accumulatedTime;
        float ms = 1000f / Mathf.Max(0.01f, fps);

        minFPS = Mathf.Min(minFPS, fps);
        maxFPS = Mathf.Max(maxFPS, fps);

        accumulatedTime = 0f;
        frameCount = 0;

        string text = Mathf.RoundToInt(fps) + " FPS";

        if (showFrameTime)
            text += " (" + ms.ToString("F1") + " ms)";

        if (showMinMax)
            text += "\nmin " + Mathf.RoundToInt(minFPS) + "  max " + Mathf.RoundToInt(maxFPS);

        textProperty.SetValue(resolvedText, text);

        if (useColors && colorProperty != null)
        {
            Color color = fps >= goodFPS ? goodColor : (fps >= okFPS ? okColor : badColor);
            colorProperty.SetValue(resolvedText, color);
        }
    }

    public void ResetMinMax()
    {
        minFPS = float.MaxValue;
        maxFPS = 0f;
    }
}
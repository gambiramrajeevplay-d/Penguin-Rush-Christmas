
using UnityEngine;
using UnityEngine.UI;

public class CrossBlinkEffect : MonoBehaviour
{
    public enum TargetType
    {
        UI_Image,
        Sprite_Renderer
    }

    [Header("Blink Settings")]
    public TargetType targetType = TargetType.UI_Image;
    public float blinkSpeed = 2f;
    [Range(0f, 1f)] public float minimumAlpha = 0.2f;
    [Range(0f, 1f)] public float maximumAlpha = 1f;
    public bool startBlinking = true;

    private Image uiImage;
    private SpriteRenderer spriteRenderer;
    private bool isBlinking;

    private void Awake()
    {
        uiImage = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        isBlinking = startBlinking;
    }

    private void Update()
    {
        if (!isBlinking)
            return;

        float alpha = Mathf.Lerp(
            minimumAlpha,
            maximumAlpha,
            (Mathf.Sin(Time.unscaledTime * blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f
        );

        if (targetType == TargetType.UI_Image && uiImage != null)
        {
            Color color = uiImage.color;
            color.a = alpha;
            uiImage.color = color;
        }
        else if (targetType == TargetType.Sprite_Renderer && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    public void StartBlinking()
    {
        isBlinking = true;
    }

    public void StopBlinking()
    {
        isBlinking = false;
        SetAlpha(maximumAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (targetType == TargetType.UI_Image && uiImage != null)
        {
            Color color = uiImage.color;
            color.a = alpha;
            uiImage.color = color;
        }
        else if (targetType == TargetType.Sprite_Renderer && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}

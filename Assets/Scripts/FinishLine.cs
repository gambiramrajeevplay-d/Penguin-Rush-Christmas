using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Stopping")]
    [Tooltip("Seconds the sledge takes to slow down to a full stop.")]
    [SerializeField] private float stopDuration = 3f;

    private bool triggered;

    private void Reset()
    {
        // Make sure the collider acts as a trigger when the script is first added.
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        SledgeController sledge = other.GetComponentInParent<SledgeController>();

        // Accept the player by component or by tag.
        if (sledge == null && !other.CompareTag(playerTag))
            return;

        triggered = true;

        if (sledge != null)
            sledge.StopSmoothly(stopDuration);

        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerFinished(stopDuration);
        else
            Debug.LogWarning("FinishLine: no GameManager found in the scene.");
    }
}
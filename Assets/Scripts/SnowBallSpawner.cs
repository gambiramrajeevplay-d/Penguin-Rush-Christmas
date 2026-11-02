
using System.Collections;
using UnityEngine;

public class SnowBallSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject snowBallPrefab;
    [SerializeField] private LayerMask groundLayer;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float spawnDistanceAhead = 10f;
    [SerializeField] private float spawnHeight = 30f;
    [SerializeField] private float spawnHeightOffset = 0.05f;

    [Header("Snowball Cleanup")]
    [SerializeField] private float snowBallLifetime = 20f;

    private Transform player;
    private Coroutine spawnCoroutine;

    private void Start()
    {
        FindPlayer();

        if (player == null)
        {
            Debug.LogError(
                "SnowBallSpawner: No GameObject found with tag '" + playerTag + "'."
            );
            enabled = false;
            return;
        }

        if (snowBallPrefab == null)
        {
            Debug.LogError("SnowBallSpawner: Assign the Snowball Prefab.");
            enabled = false;
            return;
        }

        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Refresh the reference in case the player was replaced.
            if (player == null)
                FindPlayer();

            if (player != null)
                SpawnSnowBall();
        }
    }

    private void SpawnSnowBall()
    {
        if (player == null || snowBallPrefab == null)
            return;

        // Player moves forward along +Z.
        Vector3 spawnPosition = player.position
                                + Vector3.forward * spawnDistanceAhead;

        // Cast down to find the Ground surface.
        Vector3 rayOrigin = new Vector3(
            spawnPosition.x,
            player.position.y + spawnHeight,
            spawnPosition.z
        );

        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                spawnHeight * 2f,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning(
                "SnowBallSpawner: No Ground surface found at " + spawnPosition
            );
            return;
        }

        // Calculate the snowball's world-space radius.
        SphereCollider sphere =
            snowBallPrefab.GetComponent<SphereCollider>();

        float radius = 0.5f;

        if (sphere != null)
        {
            Vector3 scale = snowBallPrefab.transform.lossyScale;

            radius = sphere.radius * Mathf.Max(
                scale.x,
                Mathf.Max(scale.y, scale.z)
            );
        }

        // Position the snowball on the ground.
        spawnPosition.y = hit.point.y + radius + spawnHeightOffset;

        GameObject snowBall = Instantiate(
            snowBallPrefab,
            spawnPosition,
            snowBallPrefab.transform.rotation
        );

        if (snowBallLifetime > 0f)
            Destroy(snowBall, snowBallLifetime);
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
}

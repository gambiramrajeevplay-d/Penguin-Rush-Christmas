using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [Header("All Truck Prefabs")]
    public GameObject[] truckPrefabs;

    private void Awake()
    {
        if (truckPrefabs == null || truckPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawn] No truckPrefabs assigned!");
            return;
        }

        int selectedTruck = 0;

        if (PlayerPrefs.HasKey("selectedTruck"))
        {
            selectedTruck = PlayerPrefs.GetInt("selectedTruck", 0);
        }
        else if (PlayerPrefs.HasKey("car"))
        {
            selectedTruck = PlayerPrefs.GetInt("car", 0);
        }

        if (selectedTruck < 0 || selectedTruck >= truckPrefabs.Length)
        {
            selectedTruck = 0;
        }

        GameObject truckToSpawn = truckPrefabs[selectedTruck];
        if (truckToSpawn == null)
        {
            Debug.LogError($"[PlayerSpawn] truckPrefabs[{selectedTruck}] is NULL.");
            return;
        }

        // 🔹 Spawn EXACTLY the same way
        GameObject spawnedTruck =
            Instantiate(truckToSpawn, transform.position, transform.rotation);

        // 🔹 ONLY THIS: put inside Level-1
        GameObject level1 = GameObject.Find("Level");
        if (level1 != null)
        {
            spawnedTruck.transform.SetParent(level1.transform);
        }
        else
        {
            Debug.LogError("Level-1 GameObject not found in scene!");
        }

        Debug.Log($"[PlayerSpawn] Spawned truck index {selectedTruck}: {truckToSpawn.name}");
    }
}

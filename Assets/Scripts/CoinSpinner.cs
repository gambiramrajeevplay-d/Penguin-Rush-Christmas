using UnityEngine;

public class CoinSpinner : MonoBehaviour
{
    public float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(Vector3.down, rotationSpeed * Time.deltaTime, Space.World);
    }
}

using UnityEngine;
using TMPro;

public class CoinCollector : MonoBehaviour
{
    private TextMeshProUGUI coinText;

    [Header("Coin Pickup Sound")]
    public AudioClip coinClip;
    [Range(0f, 1f)]
    [SerializeField] private float coinVolume = 1f;
    [Tooltip("Plays the coin pickup sound. Created automatically if left empty.")]
    [SerializeField] private AudioSource pickupSource;

    private int levelCoins = 0; // Level-only coins

    void Awake()
    {
        SetupPickupSource();
    }

    // Creates a dedicated AudioSource for the coin pickup sound.
    private void SetupPickupSource()
    {
        if (pickupSource == null)
            pickupSource = gameObject.AddComponent<AudioSource>();

        pickupSource.playOnAwake = false;
        pickupSource.loop = false;
        pickupSource.spatialBlend = 0f;
    }

    void Start()
    {
        // Reset level coins
        levelCoins = 0;

        // Find Coin UI
        GameObject textObj = GameObject.FindGameObjectWithTag("CoinText");
        if (textObj != null)
        {
            coinText = textObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("CoinText not found! Make sure tag is set.");
        }

        UpdateCoinUI(); // will show 0 at start
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            // Prevent double collection
            if (other.GetComponent<CollectedFlag>() != null)
                return;

            other.gameObject.AddComponent<CollectedFlag>();

            // Play particles
            ParticleSystem[] particles = other.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.transform.parent = null;
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration);
            }

            // Play sound
            PlayCoinSound();

            // Add to LEVEL coins (paid out by GameManager when the level ends)
            levelCoins++;

            UpdateCoinUI();

            // Destroy coin
            Destroy(other.gameObject);
        }
    }

    private void PlayCoinSound()
    {
        if (pickupSource == null || coinClip == null)
            return;

        // Respect the player's mute setting.
        AudioManagerPause.Initialize();
        if (AudioManagerPause.IsMuted)
            return;

        pickupSource.pitch = Random.Range(0.95f, 1.05f);
        pickupSource.PlayOneShot(coinClip, coinVolume);
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = levelCoins.ToString(); // ONLY LEVEL COINS
        }
    }

    public int GetLevelCoins()
    {
        return levelCoins;
    }
}

// Prevent double trigger
public class CollectedFlag : MonoBehaviour { }
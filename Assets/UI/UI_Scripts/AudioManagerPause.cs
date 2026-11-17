
using System;
using UnityEngine;

public class AudioManagerPause : MonoBehaviour
{
    private const string MuteKey = "AudioMuted";

    private static bool _isMuted;
    private static bool isInitialized;

    public static event Action OnMuteStateChanged;

    public static bool IsMuted
    {
        get
        {
            Initialize();
            return _isMuted;
        }
        set
        {
            Initialize();

            if (_isMuted == value)
            {
                ApplyVolume();
                return;
            }

            _isMuted = value;
            ApplyVolume();
            SaveSettings();

            OnMuteStateChanged?.Invoke();
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (!isInitialized)
        {
            _isMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            isInitialized = true;
        }

        ApplyVolume();
    }

    private static void ApplyVolume()
    {
        AudioListener.volume = _isMuted ? 0f : 1f;
    }

    public static void SaveSettings()
    {
        Initialize();
        PlayerPrefs.SetInt(MuteKey, _isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSettings();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}

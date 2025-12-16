using UnityEngine;

/// <summary>
/// Manages background music and sound effects
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip level1Music;
    public AudioClip level2Music;
    public AudioClip level3Music;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private AudioSource musicSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;
    }

    void Start()
    {
        // Try to load from Resources if not assigned
        if (level1Music == null)
            level1Music = Resources.Load<AudioClip>("Audio/level1");
        if (level2Music == null)
            level2Music = Resources.Load<AudioClip>("Audio/level2");
        if (level3Music == null)
            level3Music = Resources.Load<AudioClip>("Audio/level3");

        // Start playing music if clip is assigned
        if (level1Music != null)
        {
            PlayMusic(level1Music);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }
}

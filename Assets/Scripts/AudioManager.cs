using UnityEngine;
using System.Collections.Generic;

/// <summary>Enhanced audio manager with procedural sound effects</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum SFXType
    {
        Dash,
        PowerUp,
        Combo,
        LevelUp,
        GameOver,
        NearMiss
    }

    public AudioClip level1Music, level2Music, level3Music;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    AudioSource musicSource;
    List<AudioSource> sfxSources = new List<AudioSource>();
    Dictionary<SFXType, AudioClip> sfxClips = new Dictionary<SFXType, AudioClip>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        // Create pool of SFX sources
        for (int i = 0; i < 8; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = sfxVolume;
            sfxSources.Add(src);
        }

        // Generate procedural sound effects
        GenerateSoundEffects();
    }

    void Start()
    {
        level1Music ??= Resources.Load<AudioClip>("Audio/level1");
        level2Music ??= Resources.Load<AudioClip>("Audio/level2");
        level3Music ??= Resources.Load<AudioClip>("Audio/level3");

        if (level1Music) PlayMusic(level1Music);
    }

    void GenerateSoundEffects()
    {
        // Dash sound - quick swoosh
        sfxClips[SFXType.Dash] = GenerateSwoosh(0.15f, 800f, 200f);

        // Power-up sound - rising tone
        sfxClips[SFXType.PowerUp] = GeneratePowerUp(0.3f);

        // Combo sound - quick chime
        sfxClips[SFXType.Combo] = GenerateChime(0.2f, 880f);

        // Level up sound - triumphant fanfare
        sfxClips[SFXType.LevelUp] = GenerateFanfare(0.5f);

        // Game over sound - descending tone
        sfxClips[SFXType.GameOver] = GenerateGameOver(0.8f);

        // Near miss sound - quick beep
        sfxClips[SFXType.NearMiss] = GenerateBeep(0.1f, 440f);
    }

    AudioClip GenerateSwoosh(float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float envelope = Mathf.Sin(t * Mathf.PI); // Fade in and out
            float noise = Random.Range(-1f, 1f) * 0.3f;
            float wave = Mathf.Sin(2 * Mathf.PI * freq * t * duration) * 0.7f;
            data[i] = (wave + noise) * envelope * 0.5f;
        }

        var clip = AudioClip.Create("Swoosh", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GeneratePowerUp(float duration)
    {
        int sampleRate = 44100;
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        float[] freqs = { 440f, 554f, 659f, 880f }; // A, C#, E, A (major chord rising)

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            int freqIndex = Mathf.Min((int)(t * freqs.Length), freqs.Length - 1);
            float freq = freqs[freqIndex];
            float envelope = 1f - t * 0.5f;
            float wave = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);
            data[i] = wave * envelope * 0.4f;
        }

        var clip = AudioClip.Create("PowerUp", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerateChime(float duration, float freq)
    {
        int sampleRate = 44100;
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Exp(-t * 8f); // Quick decay
            float wave = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);
            float harmonic = Mathf.Sin(2 * Mathf.PI * freq * 2 * i / sampleRate) * 0.5f;
            data[i] = (wave + harmonic) * envelope * 0.3f;
        }

        var clip = AudioClip.Create("Chime", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerateFanfare(float duration)
    {
        int sampleRate = 44100;
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        float[] freqs = { 523f, 659f, 784f, 1047f }; // C, E, G, C (octave up)
        float noteLength = duration / freqs.Length;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            int noteIndex = Mathf.Min((int)(t * freqs.Length), freqs.Length - 1);
            float noteT = (t * freqs.Length) % 1f;
            float freq = freqs[noteIndex];
            float envelope = (1f - noteT * 0.3f) * (1f - t * 0.3f);
            float wave = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);
            float harmonic = Mathf.Sin(2 * Mathf.PI * freq * 1.5f * i / sampleRate) * 0.3f;
            data[i] = (wave + harmonic) * envelope * 0.35f;
        }

        var clip = AudioClip.Create("Fanfare", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerateGameOver(float duration)
    {
        int sampleRate = 44100;
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(400f, 100f, t); // Descending pitch
            float envelope = 1f - t;
            float wave = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);
            float distortion = Mathf.Sin(2 * Mathf.PI * freq * 0.5f * i / sampleRate) * 0.3f;
            data[i] = (wave + distortion) * envelope * 0.4f;
        }

        var clip = AudioClip.Create("GameOver", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerateBeep(float duration, float freq)
    {
        int sampleRate = 44100;
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);
            data[i] = wave * envelope * 0.3f;
        }

        var clip = AudioClip.Create("Beep", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlaySFX(SFXType type)
    {
        if (!sfxClips.ContainsKey(type)) return;

        // Find available source
        AudioSource availableSource = null;
        foreach (var src in sfxSources)
        {
            if (!src.isPlaying)
            {
                availableSource = src;
                break;
            }
        }

        if (availableSource == null)
            availableSource = sfxSources[0]; // Fallback to first

        availableSource.clip = sfxClips[type];
        availableSource.volume = sfxVolume;
        availableSource.pitch = Random.Range(0.95f, 1.05f); // Slight pitch variation
        availableSource.Play();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        foreach (var src in sfxSources)
            src.volume = sfxVolume;
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();
}

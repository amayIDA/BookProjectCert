using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the ambient score and the sound effects of the Talent Wheel.
/// The music and effects in Resources/NationalInitiative were rendered from the Web Audio code of the HTML file.
/// A second set of soft "asmr_" sounds (glass ticks, bubble taps, chimes, air) and a calm breathing bed
/// make every touch feel gentle.
/// </summary>
public class NIAudio : MonoBehaviour
{
    public static NIAudio Instance { get; private set; }

    [Range(0f, 2f)] public float sfxVolume = 1.4f;
    [Range(0f, 1f)] public float musicVolume = 0.85f;
    [Range(0f, 1f)] public float bedVolume = 0.35f;

    AudioSource idleMusic;
    AudioSource activeMusic;
    AudioSource bed;
    readonly List<AudioSource> voices = new List<AudioSource>();
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    bool on = true;
    float fade;
    float level = 0.2f;
    float targetLevel = 0.2f;
    float master;

    public bool IsOn
    {
        get { return on; }
    }

    public static NIAudio Ensure()
    {
        if (Instance != null)
            return Instance;

        NIAudio found = FindAnyObjectByType<NIAudio>();
        if (found != null)
            return found;

        GameObject go = new GameObject("NIAudio");
        return go.AddComponent<NIAudio>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (FindAnyObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        idleMusic = MakeSource("MusicIdle", true);
        activeMusic = MakeSource("MusicActive", true);
        bed = MakeSource("Bed", true);
        for (int i = 0; i < 12; i++)
            voices.Add(MakeSource("Voice" + i, false));

        idleMusic.clip = Load("music_idle");
        activeMusic.clip = Load("music_active");
        bed.clip = Load("asmr_bed");
        double start = AudioSettings.dspTime + 0.2;
        if (idleMusic.clip != null && activeMusic.clip != null)
        {
            idleMusic.PlayScheduled(start);
            activeMusic.PlayScheduled(start);
        }

        if (bed.clip != null)
            bed.PlayScheduled(start);

        master = on ? 1f : 0f;
    }

    AudioSource MakeSource(string objectName, bool loop)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    AudioClip Load(string clipName)
    {
        AudioClip clip;
        if (clips.TryGetValue(clipName, out clip))
            return clip;

        clip = Resources.Load<AudioClip>("NationalInitiative/" + clipName);
        clips[clipName] = clip;
        return clip;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        fade = Mathf.Min(1f, fade + dt / 6f);
        level += (targetLevel - level) * (1f - Mathf.Exp(-dt / 1.2f));
        master = Mathf.MoveTowards(master, on ? 1f : 0f, dt / 0.3f);

        float active = Mathf.Clamp01((level - 0.2f) / 0.8f);
        float gain = fade * master * musicVolume;
        idleMusic.volume = gain * Mathf.Cos(active * Mathf.PI * 0.5f);
        activeMusic.volume = gain * Mathf.Sin(active * Mathf.PI * 0.5f);
        bed.volume = fade * master * bedVolume;
    }

    /// <summary>0.2 is the quiet idle score and 1 is the busy score.</summary>
    public void SetLevel(float value)
    {
        targetLevel = Mathf.Clamp(value, 0.2f, 1f);
    }

    public void SetOn(bool value)
    {
        on = value;
    }

    AudioSource FreeVoice()
    {
        for (int i = 0; i < voices.Count; i++)
        {
            if (!voices[i].isPlaying)
                return voices[i];
        }

        return voices[0];
    }

    /// <summary>
    /// Names: place, tap, reveal, ui, lift, connect, tick, flip0, flip1, flip2 and the soft asmr_ tick, tap, press, chime,
    /// chime_low, swish, exhale, settle, sparkle, rise, drop.
    /// </summary>
    public void Play(string clipName, float volume = 1f)
    {
        PlayVaried(clipName, 1f, 1f, volume);
    }

    /// <summary>Plays a sound with a small random pitch change so repeated touches never sound identical.</summary>
    public void PlayVaried(string clipName, float pitchMin, float pitchMax, float volume)
    {
        if (!on)
            return;

        AudioClip clip = Load(clipName);
        if (clip == null)
            return;

        AudioSource voice = FreeVoice();
        voice.clip = clip;
        voice.pitch = Mathf.Approximately(pitchMin, pitchMax) ? pitchMin : Random.Range(pitchMin, pitchMax);
        voice.volume = Mathf.Clamp01(volume * sfxVolume * 0.6f);
        voice.Play();
    }
}

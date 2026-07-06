using UnityEngine;
using UnityEngine.SceneManagement;

// singleton audio manager — persists across scenes
// handles background music and pooled SFX (avoids per-hit garbage allocation)
// setup: attach to a GameObject in your first scene, add an AudioSource, assign clips in Inspector
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip   endlessMusicClip;
    [SerializeField] private AudioClip   mazeMusicClip;
    [SerializeField] private AudioClip   gameOverMusicClip;
    [SerializeField] private AudioClip   menuMusicClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume   = 1.0f;

    private const string MUSIC_VOL_KEY = "MusicVolume";
    private const string SFX_VOL_KEY   = "SFXVolume";

    [Header("SFX Pool")]
    [Tooltip("Number of simultaneous SFX sources. Increase for action-heavy scenes.")]
    [SerializeField] private int sfxPoolSize = 8;

    private AudioSource[] sfxPool;
    private int           sfxPoolIndex = 0;

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

        // load saved volume preferences
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, musicVolume);
        sfxVolume   = PlayerPrefs.GetFloat(SFX_VOL_KEY,   sfxVolume);

        BuildSFXPool();
    }

    void Start()
    {
        if (musicSource != null)
        {
            musicSource.loop   = true;
            musicSource.volume = musicVolume;
        }
        PlayMusicForCurrentMode();
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForCurrentMode(); // switch music automatically when a new scene loads
    }

    private void BuildSFXPool()
    {
        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject go = new GameObject($"SFX_Pool_{i}");
            go.transform.SetParent(transform);
            sfxPool[i]             = go.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
            sfxPool[i].volume      = sfxVolume;
        }
    }

    // plays a sound effect using a round-robin pool — no garbage allocation
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPoolSize;

        source.clip   = clip;
        source.volume = sfxVolume;
        source.Play();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (AudioSource src in sfxPool)
            if (src != null) src.volume = sfxVolume;
        PlayerPrefs.SetFloat(SFX_VOL_KEY, sfxVolume);
    }

    public void PlayMusicForCurrentMode()
    {
        if (GameManager.Instance == null)
        {
            PlayMusic(menuMusicClip);
            return;
        }

        AudioClip clip = GameManager.Instance.CurrentMode == GameManager.GameMode.Maze
            ? mazeMusicClip
            : endlessMusicClip;

        PlayMusic(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return; // already playing this clip

        musicSource.clip   = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayGameOverMusic() => PlayMusic(gameOverMusicClip);
    public void PlayMenuMusic()     => PlayMusic(menuMusicClip);

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, musicVolume);
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}

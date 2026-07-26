using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    private AudioSource audioSource;

    [Header("Audio")]
    [SerializeField] private AudioClip bgmClip;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private bool loop = true;

    [Header("Scene Exceptions")]
    [SerializeField] private string[] excludedScenes = new string[] { "Ending_Scene", "Main_Menu" };

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (string excluded in excludedScenes)
        {
            if (scene.name == excluded)
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();
                return;
            }
        }

        if (bgmClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
            audioSource.volume = volume;
    }

    public static void PauseBGM()
    {
        if (instance != null && instance.audioSource != null && instance.audioSource.isPlaying)
            instance.audioSource.Pause();
    }

    public static void ResumeBGM()
    {
        if (instance != null && instance.audioSource != null && !instance.audioSource.isPlaying && instance.bgmClip != null)
            instance.audioSource.UnPause();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

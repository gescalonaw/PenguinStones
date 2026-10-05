using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    public AudioClip gameplayMusic;

    static readonly string[] GameplayScenes = { "GameScene", "Workshop" };

    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource      = GetComponent<AudioSource>();
        audioSource.loop = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene.name);
    }

    void HandleScene(string sceneName)
    {
        bool isGameplayScene = System.Array.IndexOf(GameplayScenes, sceneName) >= 0;

        if (!isGameplayScene)
        {
            audioSource.Stop();
            return;
        }

        if (!audioSource.isPlaying || audioSource.clip != gameplayMusic)
        {
            audioSource.clip = gameplayMusic;
            audioSource.Play();
        }
    }
}

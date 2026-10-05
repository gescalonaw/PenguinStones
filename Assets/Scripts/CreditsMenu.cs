using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditsMenu : MonoBehaviour
{
    public Button backButton;

    public AudioSource audioSource;
    public AudioClip woodClickSound;

    void Start()
    {
        backButton?.onClick.AddListener(() => LoadSceneWithSound("MainMenuScene"));
    }

    void PlayClickSound()
    {
        if (audioSource != null && woodClickSound != null)
            audioSource.PlayOneShot(woodClickSound);
    }

    void LoadSceneWithSound(string sceneName)
    {
        PlayClickSound();
        StartCoroutine(LoadSceneAfterSound(sceneName));
    }

    IEnumerator LoadSceneAfterSound(string sceneName)
    {
        yield return new WaitForSeconds(woodClickSound != null ? woodClickSound.length : 0f);
        SceneManager.LoadScene(sceneName);
    }
}

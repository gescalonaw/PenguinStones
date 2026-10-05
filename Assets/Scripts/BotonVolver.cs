using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BotonVolver : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip woodClickSound;

    public void Volver()
    {
        if (audioSource != null && woodClickSound != null)
            audioSource.PlayOneShot(woodClickSound);
        StartCoroutine(VolverAfterSound());
    }

    IEnumerator VolverAfterSound()
    {
        yield return new WaitForSeconds(woodClickSound != null ? woodClickSound.length : 0f);
        SceneManager.LoadScene("GameScene");
    }
}
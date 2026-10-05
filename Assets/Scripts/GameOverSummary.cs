using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverSummary : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI reasonText;
    public TextMeshProUGUI daysText;
    public TextMeshProUGUI moneyText;

    [Header("Botones")]
    public Button restartButton;
    public Button mainMenuButton;

    public AudioSource audioSource;
    public AudioClip woodClickSound;

    void Start()
    {
        var gm = GameManager.Instance;

        if (gm != null)
        {
            if (reasonText != null)
                reasonText.text = $"No pudiste pagar el arriendo del Día {gm.currentDay}";

            if (daysText != null)
                daysText.text = $"Días sobrevividos:  {gm.currentDay}";

            if (moneyText != null)
                moneyText.text = $"Dinero final:        ${gm.money}";
        }

        restartButton?.onClick.AddListener(Restart);
        mainMenuButton?.onClick.AddListener(GoToMainMenu);
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

    void Restart()
    {
        GameManager.Instance?.ResetGame();
        LoadSceneWithSound("GameScene");
    }

    void GoToMainMenu()
    {
        GameManager.Instance?.ResetGame();
        LoadSceneWithSound("MainMenuScene");
    }
}

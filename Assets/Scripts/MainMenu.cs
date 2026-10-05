using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button continueButton;
    public Button creditsButton;
    public Button tutorialButton;

    public AudioSource audioSource;
    public AudioClip woodClickSound;

    void Start()
    {
        startButton?.onClick.AddListener(NuevaPartida);
        creditsButton?.onClick.AddListener(() => LoadSceneWithSound("CreditsScene"));

        if (continueButton != null)
        {
            bool haySave = GameManager.HasSave();
            continueButton.gameObject.SetActive(haySave);
            continueButton.onClick.AddListener(ContinuarPartida);
        }

        // Si no asignas un botón de Tutorial en el Inspector, se crea uno automáticamente.
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(Tutorial);
        else
            CreateTutorialButtonFallback();
    }

    void Tutorial()
    {
        GameManager.RequestTutorial();
        LoadSceneWithSound("GameScene");
    }

    // Crea un botón "TUTORIAL" por código para que la función esté disponible
    // aunque no hayas añadido el botón manualmente en la escena del menú.
    void CreateTutorialButtonFallback()
    {
        Canvas canvas = startButton != null
            ? startButton.GetComponentInParent<Canvas>()
            : FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("TutorialButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        if (startButton != null)
        {
            var srt = startButton.GetComponent<RectTransform>();
            rt.anchorMin = srt.anchorMin;
            rt.anchorMax = srt.anchorMax;
            rt.pivot     = srt.pivot;
            rt.sizeDelta = srt.sizeDelta.sqrMagnitude > 0f ? srt.sizeDelta : new Vector2(260f, 70f);
            rt.anchoredPosition = srt.anchoredPosition + new Vector2(0f, -(rt.sizeDelta.y + 20f));
        }
        else
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 70f);
            rt.anchoredPosition = new Vector2(0f, -160f);
        }

        go.GetComponent<Image>().color = new Color(0.45f, 0.30f, 0.15f, 0.95f);

        var label = new GameObject("Text", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var txt = label.AddComponent<TextMeshProUGUI>();
        txt.text      = "TUTORIAL";
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize  = 32f;
        txt.color     = Color.white;

        tutorialButton = go.GetComponent<Button>();
        tutorialButton.onClick.AddListener(Tutorial);
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

    void NuevaPartida()
    {
        GameManager.RequestNewGame();
        LoadSceneWithSound("GameScene");
    }

    void ContinuarPartida()
    {
        GameManager.RequestLoadGame();
        LoadSceneWithSound("GameScene");
    }
}

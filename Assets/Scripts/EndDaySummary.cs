using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndDaySummary : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI correctOrdersText;
    public TextMeshProUGUI failedOrdersText;
    public TextMeshProUGUI moneyEarnedText;
    public TextMeshProUGUI rentText;
    public TextMeshProUGUI finalMoneyText;

    [Header("Botones")]
    public Button continueButton;
    public Button mainMenuButton;

    public AudioSource audioSource;
    public AudioClip woodClickSound;

    const int RENT          = 1200;
    const int RENT_INTERVAL = 2;   // cada 2 días: días 1, 3, 5 …

    static bool IsRentDay(int day) => day % RENT_INTERVAL == 1;

    void Start()
    {
        var gm = GameManager.Instance;

        if (gm == null)
        {
            Populate(1, 0, 0, 1000, true);
            continueButton?.onClick.AddListener(() => LoadSceneWithSound("GameScene"));
            mainMenuButton?.onClick.AddListener(GoToMainMenu);
            return;
        }

        bool rentDue      = IsRentDay(gm.currentDay);
        int  moneyBefore  = gm.money;
        int  finalMoney   = rentDue ? moneyBefore - RENT : moneyBefore;

        if (rentDue)
            gm.money = finalMoney;

        Populate(gm.currentDay, gm.ordersCorrectToday, gm.ordersFailedToday, moneyBefore, rentDue);

        if (rentDue && finalMoney < 0)
        {
            continueButton?.onClick.AddListener(GoToGameOver);
        }
        else
        {
            // Avanzamos al día siguiente y lo guardamos ya como punto de retomado,
            // así "Continuar partida" queda en el día correcto aunque el jugador
            // salga al menú (o cierre el juego) desde este resumen en vez de pulsar
            // "Continuar".
            gm.PrepareNewDay();
            gm.SaveGame();
            continueButton?.onClick.AddListener(NextDay);
        }

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

    void Populate(int day, int correct, int failed, int moneyBefore, bool rentDue)
    {
        int finalMoney = rentDue ? moneyBefore - RENT : moneyBefore;

        if (titleText != null)
            titleText.text = $"Fin del Día {day}";

        if (correctOrdersText != null)
            correctOrdersText.text = $"Pedidos correctos:   {correct}";

        if (failedOrdersText != null)
            failedOrdersText.text = $"Pedidos fallidos:    {failed}";

        if (moneyEarnedText != null)
            moneyEarnedText.text = $"Dinero recaudado:    ${moneyBefore}";

        if (rentText != null)
            rentText.text = rentDue
                ? $"Arriendo:           -${RENT}"
                : $"Sin arriendo hoy    (próx. Día {day + 1})";

        if (finalMoneyText != null)
        {
            if (rentDue && finalMoney < 0)
            {
                finalMoneyText.text  = $"¡No puedes pagar el arriendo!  ${finalMoney}";
                finalMoneyText.color = new Color(0.95f, 0.15f, 0.15f);
            }
            else
            {
                finalMoneyText.text  = $"Saldo final:         ${finalMoney}";
                finalMoneyText.color = finalMoney >= 0
                    ? new Color(0.08f, 0.70f, 0.15f)
                    : new Color(0.95f, 0.15f, 0.15f);
            }
        }
    }

    void NextDay()
    {
        // El día ya se avanzó y se guardó en Start(); aquí solo cargamos la escena.
        LoadSceneWithSound("GameScene");
    }

    void GoToGameOver()
    {
        LoadSceneWithSound("GameOverScene");
    }

    void GoToMainMenu()
    {
        LoadSceneWithSound("MainMenuScene");
    }
}

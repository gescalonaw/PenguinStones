using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("Textos UI")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI orderText;
    public TextMeshProUGUI dayText;

    [Header("Botón Taller")]
    public Button goToWorkshopButton;

    [Header("Botón Entregar")]
    public Button btnEntregar;
    public TextMeshProUGUI resultText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip woodClickSound;
    public AudioClip missingStoneSound;

    void Start()
    {
        if (goToWorkshopButton != null)
            goToWorkshopButton.onClick.AddListener(GoToWorkshop);

        if (btnEntregar != null)
        {
            btnEntregar.onClick.AddListener(PlayClickSound);
            btnEntregar.onClick.AddListener(Entregar);
        }

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.ui = this;

        UpdateAll();
    }

    void Update()
    {
        if (timerText != null && GameManager.Instance != null)
            timerText.text = Mathf.Ceil(GameManager.Instance.timeRemaining) + "s";
    }

    public void UpdateMoney()
    {
        if (moneyText != null && GameManager.Instance != null)
            moneyText.text = "$" + GameManager.Instance.money;
    }

    static readonly System.Collections.Generic.Dictionary<string, string[]> colorHints = new()
    {
        { "roja",     new[] {
            "Del color del fuego", "Ardiente, como las brasas", "Que me recuerde a la sangre", "Roja como el atardecer",
            "Del color de mis mejillas cuando siento vergüenza", "Que combine con las fresas maduras",
            "Como la ira que a veces no logro controlar", "Del color de una manzana recién cortada"
        } },
        { "azul",     new[] {
            "Fría como el hielo", "Del color del océano", "Como el cielo en verano", "Que me recuerde al río",
            "Que tenga la calma de un día sin preocupaciones", "Del color de mis jeans favoritos",
            "Como la melancolía de una tarde lluviosa", "Que combine con las olas del mar en la mañana",
            "Del color de unos ojos que no logro olvidar", "Tan serena como el silencio antes de dormir"
        } },
        { "verde",    new[] {
            "Fresca, como el bosque", "Del color de la hierba", "Que huela a naturaleza", "Como las hojas en primavera",
            "Que tenga la esperanza de un nuevo comienzo", 
            "Como el silencio de un jardín al amanecer", "Que combine con la palta",
            "Del color de la suerte que tanto necesito", "Tan viva como el musgo tras la lluvia"
        } },
        { "amarilla", new[] {
            "Brillante como el sol", "Dorada y luminosa", "Que ilumine la habitación", "Como la arena en verano",
            "Que tenga la alegría de un día sin nubes", "Del color de los girasoles al mediodía",
            "Como la energía que necesito para empezar el día", "Que combine con la miel recién servida",
            "Del color de un recuerdo feliz de mi infancia"
        } },
    };

    static readonly System.Collections.Generic.Dictionary<string, string[]> shapeHints = new()
    {
        { "corazon",  new[] {
            "con forma de lo que siento por ti", "romántica", "que represente el amor", "como lo que late en mi pecho",
            "que simbolice lo que no me atrevo a decir", "con la forma de una promesa",
            "que tenga la ternura de un abrazo", "como el dibujo que hacía de niño en las cartas",
            "que hable sin palabras", "tan frágil como una confesión"
        } },
        { "cuadrado", new[] {
            "firme y ordenada", "con esquinas bien definidas", "estable, como una caja", "que no tenga curvas",
            "que tenga la disciplina de una rutina perfecta", "tan predecible como mis mañanas de lunes",
            "con la solidez de una promesa cumplida", "que no deje nada al azar",
            "tan justa y equilibrada como una buena decisión", "que se sostenga sola, sin tambalear"
        } },
        { "circulo",  new[] {
            "suave y sin esquinas", "perfecta como la luna llena", "redonda y armoniosa", "que ruede si la empujas",
            "que represente un ciclo que nunca termina", "tan infinita como mis ganas de seguir intentando",
            "sin principio ni fin, como nuestra amistad", "que gire sin parar, como mis pensamientos",
            "tan completa que no le falta nada", "como un abrazo que envuelve por completo"
        } },
    };

    public void UpdateOrder()
    {
        if (orderText == null || GameManager.Instance == null) return;
        var gm = GameManager.Instance;

        string colorHint = colorHints.TryGetValue(gm.requiredColor, out var ch)
            ? ch[Random.Range(0, ch.Length)] : gm.requiredColor;
        string shapeHint = shapeHints.TryGetValue(gm.requiredShape, out var sh)
            ? sh[Random.Range(0, sh.Length)] : gm.requiredShape;

        orderText.text = $"{colorHint}... y {shapeHint}.\"";
    }

    public void UpdateDay()
    {
        if (dayText != null && GameManager.Instance != null)
            dayText.text = "Día " + GameManager.Instance.currentDay;
    }

    public void UpdateAll()
    {
        UpdateMoney();
        UpdateOrder();
        UpdateDay();
    }

    void Entregar()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (string.IsNullOrEmpty(gm.playerColor) || string.IsNullOrEmpty(gm.playerShape))
        {
            if (audioSource != null && missingStoneSound != null)
                audioSource.PlayOneShot(missingStoneSound);

            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.text  = "¡Haz piedra primero!";
                resultText.color = Color.yellow;
                Invoke(nameof(HideResult), 1.5f);
            }
            return;
        }

        bool ok = gm.EntregarPedido();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text  = ok ? "¡Correcto! +$100" : "Incorrecto";
            resultText.color = ok ? Color.green : Color.red;
            Invoke(nameof(HideResult), 1.5f);
        }
    }

    void HideResult()
    {
        if (resultText != null)
            resultText.gameObject.SetActive(false);
    }

    void PlayClickSound()
    {
        if (audioSource != null && woodClickSound != null)
            audioSource.PlayOneShot(woodClickSound);
    }

    void GoToWorkshop()
    {
        PlayClickSound();
        StartCoroutine(GoToWorkshopAfterSound());
    }

    IEnumerator GoToWorkshopAfterSound()
    {
        yield return new WaitForSeconds(woodClickSound != null ? woodClickSound.length : 0f);
        GameManager.Instance?.GoToWorkshop();
    }
}

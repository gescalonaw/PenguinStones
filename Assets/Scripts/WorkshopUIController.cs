using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WorkshopUIController : MonoBehaviour
{
    // Corrutina que aplica el aspecto final de la piedra al terminar la animación
    Coroutine pendingRoutine;

    [Header("Info")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI selectionText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI dayText;

    [Header("Piedra")]
    public Image stoneImage;
    // NUEVO: Agregamos una referencia al Animator de la piedra
    public Animator stoneAnimator;

    [Header("Sonidos")]
    public AudioSource sfxSource;      // fuente para efectos (si lo dejas vacío se crea sola)
    public AudioClip sfxCambioColor;   // suena al pintar la piedra
    public AudioClip sfxCambioForma;   // suena al transformar la piedra
    [Range(0f, 1f)] public float sfxVolumen = 1f; // volumen de los efectos

    [Header("Botones Color")]
    public Button btnRojo;
    public Button btnAzul;
    public Button btnVerde;
    public Button btnAmarillo;

    [Header("Botones Forma")]
    public Button btnCorazon;
    public Button btnCuadrado;
    public Button btnCirculo;

    static readonly Color ColorRojo     = new Color(0.85f, 0.15f, 0.15f);
    static readonly Color ColorAzul     = new Color(0.15f, 0.35f, 0.90f);
    static readonly Color ColorVerde    = new Color(0.15f, 0.75f, 0.20f);
    static readonly Color ColorAmarillo = new Color(0.95f, 0.85f, 0.10f);

    void Start()
    {
        btnRojo?.onClick.AddListener(() => PickColor("roja"));
        btnAzul?.onClick.AddListener(() => PickColor("azul"));
        btnVerde?.onClick.AddListener(() => PickColor("verde"));
        btnAmarillo?.onClick.AddListener(() => PickColor("amarilla"));

        btnCorazon?.onClick.AddListener(() => PickShape("corazon"));
        btnCuadrado?.onClick.AddListener(() => PickShape("cuadrado"));
        btnCirculo?.onClick.AddListener(() => PickShape("circulo"));

        resultText?.gameObject.SetActive(false);

        // Si no asignaste una fuente de sonido, creamos una automáticamente
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        RefreshUI();
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, sfxVolumen);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (moneyText is not null)
            moneyText.text = "$" + gm.money;
        if (timerText is not null)
            timerText.text = Mathf.Ceil(gm.timeRemaining) + "s";
    }

    void PickColor(string color)
    {
        var gm = GameManager.Instance;
        if (gm is not null && gm.playerColor != color)
        {
            gm.playerColor = color;

            string trigger = TriggerForColor(color);
            if (stoneAnimator != null && trigger != null)
            {
                // Disparamos la animación y aplicamos la piedra coloreada AL TERMINAR,
                // no ahora. Así durante la animación no se ve el color de golpe.
                stoneAnimator.SetTrigger(trigger);
                PlaySfx(sfxCambioColor);
                RefreshTexts(); // los textos sí se actualizan ya

                if (pendingRoutine != null) StopCoroutine(pendingRoutine);
                pendingRoutine = StartCoroutine(AplicarPiedraTrasAnimacion());
                return;
            }
        }
        RefreshUI();
    }

    static string TriggerForColor(string color) => color switch
    {
        "roja"     => "CambioRojo",
        "azul"     => "CambioAzul",
        "verde"    => "CambioVerde",
        "amarilla" => "CambioAmarillo",
        _          => null,
    };

    // Espera a que la animación del Animator termine (vuelva al estado Idle) y
    // recién ahí actualiza el aspecto de la piedra. Sirve para color y forma.
    IEnumerator AplicarPiedraTrasAnimacion()
    {
        // 1) Espera a que arranque la animación (que salga de Idle)
        float arranque = 1f;
        while (arranque > 0f && EstaEnIdle())
        {
            arranque -= Time.deltaTime;
            yield return null;
        }
        // 2) Espera a que termine (que vuelva a Idle), con tope de seguridad
        float timeout = 10f;
        while (timeout > 0f && !EstaEnIdle())
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        pendingRoutine = null;
        RefreshUI(); // ahora sí: la piedra queda con la forma/color elegidos
    }

    bool EstaEnIdle()
        => stoneAnimator == null || stoneAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle");

    void PickShape(string shape)
    {
        var gm = GameManager.Instance;
        if (gm is not null && gm.playerShape != shape)
        {
            gm.playerShape = shape;

            if (stoneAnimator != null)
            {
                // Igual que el color: la piedra con la nueva forma aparece AL TERMINAR
                stoneAnimator.SetTrigger("CambioForma");
                PlaySfx(sfxCambioForma);
                RefreshTexts();

                if (pendingRoutine != null) StopCoroutine(pendingRoutine);
                pendingRoutine = StartCoroutine(AplicarPiedraTrasAnimacion());
                return;
            }
        }
        RefreshUI();
    }

    void RefreshUI()
    {
        RefreshTexts();

        var gm = GameManager.Instance;
        if (gm != null && stoneImage is not null)
            UpdateStoneVisual(gm.playerShape, gm.playerColor);
    }

    void RefreshTexts()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        string color = string.IsNullOrEmpty(gm.playerColor) ? "?" : gm.playerColor;
        string shape = string.IsNullOrEmpty(gm.playerShape) ? "?" : gm.playerShape;
        if (selectionText is not null)
            selectionText.text = $"Tu piedra:\n{color} + {shape}";

        if (dayText is not null)
            dayText.text = "Día " + gm.currentDay;
    }

    void UpdateStoneVisual(string shape, string color)
    {
        // Nota importante: Esta función cambia el sprite instantáneamente.
        // Si tienes una animación de Animator corriendo, el Animator tomará el control 
        // temporalmente de 'stoneImage.sprite' y mostrará la animación sobreescribiendo 
        // estos valores de abajo hasta que termine y vuelva a 'Idle'.
        
        bool hasColor = !string.IsNullOrEmpty(color);

        // 1) Sprite dedicado de forma + color (ej. circulo_azul): ya viene coloreado
        if (!string.IsNullOrEmpty(shape) && hasColor)
        {
            Sprite s = Resources.Load<Sprite>($"Shapes/{shape}_{color}");
            if (s != null)
            {
                stoneImage.sprite = s;
                stoneImage.color = Color.white;
                return;
            }
        }

        // 2) Forma sin sprite de color: usamos la versión sincolor y la teñimos
        if (!string.IsNullOrEmpty(shape))
        {
            Sprite sinColor = Resources.Load<Sprite>($"Shapes/{shape}_sincolor");
            if (sinColor != null)
            {
                stoneImage.sprite = sinColor;
                stoneImage.color = hasColor ? GetColor(color) : Color.white;
                return;
            }
        }

        // 3) Piedra por defecto: también la teñimos con el color elegido
        Sprite def = Resources.Load<Sprite>("Shapes/Piedra");
        stoneImage.sprite = def;
        stoneImage.color = hasColor ? GetColor(color) : Color.white;
    }

    static Color GetColor(string name) => name switch
    {
        "roja"     => ColorRojo,
        "azul"     => ColorAzul,
        "verde"    => ColorVerde,
        "amarilla" => ColorAmarillo,
        _          => new Color(0.65f, 0.65f, 0.65f),
    };
}
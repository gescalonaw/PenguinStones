using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Guía paso a paso para el "día tutorial".
// Se crea por código (no necesita configurarse en el editor) y dibuja un panel
// que persiste sobre todas las escenas mientras el tutorial está activo.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    enum Step { GoToWorkshop, PickColor, PickShape, ReturnToShop, Deliver, Done }
    Step step = Step.GoToWorkshop;

    // Pedido guiado (debe coincidir con el pedido fijo de GameManager en modo tutorial).
    const string TargetColor = "azul";
    const string TargetShape = "corazon";

    TextMeshProUGUI instructionText;
    GameObject menuButton;

    int baselineCorrect;
    int baselineFailed;

    public static void Begin()
    {
        if (Instance != null) return;
        var go = new GameObject("TutorialManager");
        Instance = go.AddComponent<TutorialManager>();
        DontDestroyOnLoad(go);
    }

    public static void End()
    {
        GameManager.TutorialMode = false;
        if (Instance != null) Destroy(Instance.gameObject);
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateInstruction();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si el jugador vuelve al menú, el tutorial termina.
        if (scene.name == "MainMenuScene") { End(); return; }
        UpdateInstruction();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        string scene = SceneManager.GetActiveScene().name;

        switch (step)
        {
            case Step.GoToWorkshop:
                if (scene == "Workshop") Advance(Step.PickColor);
                break;

            case Step.PickColor:
                if (gm.playerColor == TargetColor) Advance(Step.PickShape);
                break;

            case Step.PickShape:
                if (gm.playerShape == TargetShape) Advance(Step.ReturnToShop);
                break;

            case Step.ReturnToShop:
                if (scene == "GameScene")
                {
                    baselineCorrect = gm.ordersCorrectToday;
                    baselineFailed  = gm.ordersFailedToday;
                    Advance(Step.Deliver);
                }
                break;

            case Step.Deliver:
                if (gm.ordersCorrectToday > baselineCorrect)
                {
                    Advance(Step.Done);
                }
                else if (gm.ordersFailedToday > baselineFailed)
                {
                    // Entregó algo incorrecto: volvemos a guiarlo desde el taller.
                    Advance(Step.GoToWorkshop);
                }
                break;
        }
    }

    void Advance(Step next)
    {
        step = next;
        UpdateInstruction();
    }

    void UpdateInstruction()
    {
        if (instructionText == null) return;

        if (menuButton != null) menuButton.SetActive(step == Step.Done);

        switch (step)
        {
            case Step.GoToWorkshop:
                instructionText.text =
                    "El Oso quiere una piedra AZUL con forma de CORAZÓN.\n" +
                    "Pulsa el botón TALLER para fabricarla.";
                break;
            case Step.PickColor:
                instructionText.text =
                    "Estás en el taller. Paso 1 de 2:\n" +
                    "Pulsa el botón AZUL para dar color a la piedra.";
                break;
            case Step.PickShape:
                instructionText.text =
                    "¡Bien! Paso 2 de 2:\n" +
                    "Pulsa el botón CORAZÓN para darle forma.";
                break;
            case Step.ReturnToShop:
                instructionText.text =
                    "Tu piedra azul con forma de corazón está lista.\n" +
                    "Pulsa VOLVER para regresar a la tienda.";
                break;
            case Step.Deliver:
                instructionText.text =
                    "Último paso: pulsa ENTREGAR para darle la piedra al cliente.";
                break;
            case Step.Done:
                instructionText.text =
                    "¡Excelente! Completaste tu primer pedido.\n" +
                    "Así funciona cada día: escucha al cliente, fabrica la piedra y entrégala.";
                break;
        }
    }

    // ---- Construcción del overlay por código ----

    void BuildOverlay()
    {
        var canvasGO = new GameObject("TutorialCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Panel inferior (no bloquea los clics del juego).
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot     = new Vector2(0.5f, 0f);
        prt.sizeDelta = new Vector2(0f, 220f);
        prt.anchoredPosition = new Vector2(0f, 0f);
        var pImg = panel.GetComponent<Image>();
        pImg.enabled = false;          // sin fondo: solo se ve el texto
        pImg.raycastTarget = false;

        // Fondo que se ajusta SOLO al tamaño del texto (recuadro que envuelve las letras).
        var bgGO = new GameObject("InstructionBg", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        bgGO.transform.SetParent(panel.transform, false);
        var bgrt = bgGO.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0.5f, 0f);
        bgrt.anchorMax = new Vector2(0.5f, 0f);
        bgrt.pivot     = new Vector2(0.5f, 0f);
        bgrt.anchoredPosition = new Vector2(0f, 20f);

        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);   // recuadro oscuro semitransparente
        bgImg.raycastTarget = false;

        var layout = bgGO.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(35, 35, 20, 20);   // márgenes alrededor del texto
        layout.childAlignment        = TextAnchor.MiddleCenter;
        layout.childControlWidth     = true;
        layout.childControlHeight    = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = bgGO.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Texto de instrucción (dentro del recuadro).
        var textGO = new GameObject("Instruction", typeof(RectTransform));
        textGO.transform.SetParent(bgGO.transform, false);
        instructionText = textGO.AddComponent<TextMeshProUGUI>();
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.fontSize  = 40f;
        instructionText.fontStyle = FontStyles.Bold;
        instructionText.color     = Color.white;
        instructionText.raycastTarget = false;
        instructionText.enableWordWrapping = false;   // el recuadro se ajusta al ancho del texto
        // Fuerza letras blancas sin contorno sobre el recuadro oscuro.
        var mat = instructionText.fontMaterial;
        mat.SetColor(ShaderUtilities.ID_FaceColor,    Color.white);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);

        var buttonPrefab = Resources.Load<GameObject>("TutorialMenuButton");
        if (buttonPrefab != null)
        {
            menuButton = Instantiate(buttonPrefab, canvasGO.transform);
            var b = menuButton.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(GoToMenu);
        }
        else
        {
            menuButton = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            menuButton.transform.SetParent(canvasGO.transform, false);
            var brt = menuButton.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot     = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(460f, 110f);
            brt.anchoredPosition = new Vector2(0f, 0f);
            menuButton.GetComponent<Image>().color = new Color(0.45f, 0.30f, 0.15f, 1f);
            menuButton.GetComponent<Button>().onClick.AddListener(GoToMenu);

            var blabel = new GameObject("Text", typeof(RectTransform));
            blabel.transform.SetParent(menuButton.transform, false);
            var blrt = blabel.GetComponent<RectTransform>();
            blrt.anchorMin = Vector2.zero;
            blrt.anchorMax = Vector2.one;
            blrt.offsetMin = Vector2.zero;
            blrt.offsetMax = Vector2.zero;
            var blabelText = blabel.AddComponent<TextMeshProUGUI>();
            blabelText.text      = "Volver al menú";
            blabelText.alignment = TextAlignmentOptions.Center;
            blabelText.fontSize  = 44f;
            blabelText.color     = Color.white;
        }

        menuButton.SetActive(false);
    }

    void GoToMenu()
    {
        GameManager.TutorialMode = false;
        SceneManager.LoadScene("MainMenuScene");
        // OnSceneLoaded se encargará de destruir este objeto.
    }
}

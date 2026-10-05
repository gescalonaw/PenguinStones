using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Estado del Juego")]
    public int money = 1000;
    public int currentDay = 1;

    [Header("Timer")]
    public float timeRemaining = 60f;
    public bool timerRunning = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    [Range(0f, 3f)] public float incorrectSoundVolume = 1.5f;

    AudioClip tickClip;
    AudioClip epicClip;

    [Header("Orden Actual (persiste entre escenas)")]
    public string currentCustomer;
    public string requiredColor;
    public string requiredShape;
    public string playerColor;
    public string playerShape;
    public int currentReward;

    [Header("Estadísticas del Día")]
    public int ordersCorrectToday;
    public int ordersFailedToday;

    [Header("Datos de Pedidos")]
    public string[] customers = { "Oso", "Zorro", "Conejo", "Gato" };
    public string[] colors    = { "roja", "azul", "verde", "amarilla" };
    readonly string[] shapes  = { "corazon", "cuadrado", "circulo" };

    [Header("Referencias (se resuelven automáticamente)")]
    public OrderManager orderManager;
    public UIController ui;

    public static GameManager Instance;

    private bool gameStarted  = false;
    private bool pendingNewDay = false;

    // Modo tutorial: pedido fijo, sin presión de tiempo y sin tocar el guardado.
    public static bool TutorialMode = false;

    enum PendingAction { None, NewGame, LoadGame, Tutorial }
    static PendingAction pendingAction = PendingAction.None;

    // Se puede llamar antes de que GameManager exista (ej. desde el Menú Principal,
    // que no tiene GameManager hasta que se carga GameScene por primera vez).
    public static void RequestNewGame()
    {
        if (Instance != null) Instance.ResetGame();
        else pendingAction = PendingAction.NewGame;
    }

    public static void RequestLoadGame()
    {
        if (Instance != null) Instance.LoadGame();
        else pendingAction = PendingAction.LoadGame;
    }

    public static void RequestTutorial()
    {
        if (Instance != null) Instance.StartTutorial();
        else pendingAction = PendingAction.Tutorial;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.orderManager = GetComponent<OrderManager>();
            Instance.ui           = GetComponent<UIController>();
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        tickClip = GenerateBeepClip(880f, 0.12f);
        epicClip = GenerateSweepClip(500f, 1600f, 0.6f);

        if (pendingAction == PendingAction.NewGame)
            ResetGame();
        else if (pendingAction == PendingAction.LoadGame)
            LoadGame();
        else if (pendingAction == PendingAction.Tutorial)
            StartTutorial();

        pendingAction = PendingAction.None;
    }

    static AudioClip GenerateBeepClip(float frequency, float duration)
    {
        int sampleRate  = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t    = (float)i / sampleRate;
            float fade = 1f - (float)i / sampleCount; // evita el "click" al cortar
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade;
        }

        AudioClip clip = AudioClip.Create("Beep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateSweepClip(float startFrequency, float endFrequency, float duration)
    {
        int sampleRate  = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        int fadeOutStart = Mathf.FloorToInt(sampleCount * 0.85f);

        for (int i = 0; i < sampleCount; i++)
        {
            float t    = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFrequency, endFrequency, t);
            phase     += 2f * Mathf.PI * freq / sampleRate;

            float fade = i < fadeOutStart ? 1f : 1f - (float)(i - fadeOutStart) / (sampleCount - fadeOutStart);
            samples[i] = Mathf.Sin(phase) * fade;
        }

        AudioClip clip = AudioClip.Create("EpicBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;

        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();
        if (ui == null)
            ui = FindObjectOfType<UIController>();

        if (!gameStarted || pendingNewDay)
        {
            pendingNewDay = false;
            gameStarted   = true;
            AssignNewOrder();
            if (orderManager != null) orderManager.GenerateOrder();
            StartTimer(60f);
            if (ui != null) ui.UpdateAll();
        }
        else
        {
            // Volviendo del taller con tiempo restante
            if (orderManager != null)
            {
                orderManager.customerName  = currentCustomer;
                orderManager.requiredColor = requiredColor;
                orderManager.requiredShape = requiredShape;
                orderManager.playerColor   = playerColor;
                orderManager.playerShape   = playerShape;
                orderManager.currentReward = currentReward;
                orderManager.ShowCurrentCustomer();
            }
            timerRunning = true;
            if (ui != null) ui.UpdateAll();
        }
    }

    void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            timerRunning  = false;
            OnTimeEnded();
        }
    }

    void PlayTickSound()
    {
        if (audioSource != null && tickClip != null)
            audioSource.PlayOneShot(tickClip);
    }

    void PlayEpicBeep()
    {
        if (audioSource != null && epicClip != null)
            audioSource.PlayOneShot(epicClip);
    }

    void AssignNewOrder()
    {
        if (TutorialMode)
        {
            // Pedido fijo para que la guía sea siempre la misma.
            // (azul + corazon coinciden entre taller y pedido; rojo/amarillo no.)
            currentCustomer = "Oso";
            requiredColor   = "azul";
            requiredShape   = "corazon";
            currentReward   = 100;
            playerColor     = "";
            playerShape     = "";
            return;
        }

        currentCustomer = customers[Random.Range(0, customers.Length)];
        requiredColor   = colors[Random.Range(0, colors.Length)];
        requiredShape   = shapes[Random.Range(0, shapes.Length)];
        currentReward   = 100;
        playerColor     = "";
        playerShape     = "";
    }

    public bool EntregarPedido()
    {
        bool completado = playerColor == requiredColor && playerShape == requiredShape;

        if (completado)
        {
            ordersCorrectToday++;
            money += currentReward;
            Debug.Log($"¡Pedido completado! +{currentReward} | Total: ${money}");
            if (ui != null) ui.UpdateMoney();
            if (audioSource != null && correctSound != null)
                audioSource.PlayOneShot(correctSound);
        }
        else
        {
            ordersFailedToday++;
            Debug.Log($"Incorrecto. Querían {requiredColor}+{requiredShape}, entregaste {playerColor}+{playerShape}");
            if (audioSource != null && incorrectSound != null)
                audioSource.PlayOneShot(incorrectSound, incorrectSoundVolume);
        }

        AssignNewOrder();
        if (orderManager != null) orderManager.GenerateOrder();
        if (ui != null) ui.UpdateAll();
        return completado;
    }

    public void StartTimer(float duration)
    {
        timeRemaining = duration;

        // En el tutorial no hay presión de tiempo: el reloj queda congelado.
        if (TutorialMode)
        {
            timerRunning = false;
            CancelInvoke(nameof(PlayTickSound));
            return;
        }

        timerRunning  = true;

        CancelInvoke(nameof(PlayTickSound));
        for (int secondsLeft = 5; secondsLeft >= 1; secondsLeft--)
        {
            float delay = duration - secondsLeft;
            if (delay >= 0f)
                Invoke(nameof(PlayTickSound), delay);
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
        if (ui != null) ui.UpdateMoney();
    }

    public void OnTimeEnded()
    {
        timerRunning = false;
        Debug.Log($"¡Tiempo! Día {currentDay} terminado. Correctos: {ordersCorrectToday} | Fallidos: {ordersFailedToday}");
        PlayEpicBeep();
        SceneManager.LoadScene("EndDayScene");
    }

    // Llamado por EndDaySummary antes de cargar GameScene
    public void PrepareNewDay()
    {
        currentDay++;
        ordersCorrectToday = 0;
        ordersFailedToday  = 0;
        pendingNewDay      = true;
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("save_money", money);
        PlayerPrefs.SetInt("save_day",   currentDay);
        PlayerPrefs.Save();
        Debug.Log($"Partida guardada: Día {currentDay}, ${money}");
    }

    public void LoadGame()
    {
        TutorialMode       = false;
        money              = PlayerPrefs.GetInt("save_money", 1000);
        currentDay         = PlayerPrefs.GetInt("save_day",   1);
        ordersCorrectToday = 0;
        ordersFailedToday  = 0;
        gameStarted        = false;
        pendingNewDay      = true;
        Debug.Log($"Partida cargada: Día {currentDay}, ${money}");
    }

    public static bool HasSave() => PlayerPrefs.HasKey("save_money");

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey("save_money");
        PlayerPrefs.DeleteKey("save_day");
        PlayerPrefs.Save();
    }

    // Inicia el día guiado. No borra la partida guardada del jugador.
    public void StartTutorial()
    {
        TutorialMode       = true;
        money              = 1000;
        currentDay         = 1;
        timeRemaining      = 60f;
        timerRunning       = false;
        ordersCorrectToday = 0;
        ordersFailedToday  = 0;
        currentCustomer    = "";
        requiredColor      = "";
        requiredShape      = "";
        playerColor        = "";
        playerShape        = "";
        currentReward      = 0;
        gameStarted        = false;
        pendingNewDay      = false;
        orderManager       = null;
        ui                 = null;

        TutorialManager.Begin();
    }

    public void ResetGame()
    {
        TutorialMode       = false;
        DeleteSave();
        money              = 1000;
        currentDay         = 1;
        timeRemaining      = 60f;
        timerRunning       = false;
        ordersCorrectToday = 0;
        ordersFailedToday  = 0;
        currentCustomer    = "";
        requiredColor      = "";
        requiredShape      = "";
        playerColor        = "";
        playerShape        = "";
        currentReward      = 0;
        gameStarted        = false;
        pendingNewDay      = false;
        orderManager       = null;
        ui                 = null;
    }

    public void GoToWorkshop()
    {
        SceneManager.LoadScene("Workshop");
    }

    public void ReturnToShop()
    {
        SceneManager.LoadScene("GameScene");
    }
}

using UnityEngine;
using System.Collections.Generic;

public class OrderManager : MonoBehaviour
{
    [Header("Pedido Actual")]
    public string customerName;
    public string requiredColor;
    public string requiredShape;
    public int currentReward;

    [Header("Lo que hizo el jugador")]
    public string playerColor;
    public string playerShape;

    [Header("GameObjects de Clientes en Escena")]
    public GameObject goOso;
    public GameObject goZorro;
    public GameObject goConejo;
    public GameObject goGato;

    private Dictionary<string, GameObject> customerObjects;

    void Awake()
    {
        customerObjects = new Dictionary<string, GameObject>
        {
            { "Oso",    goOso    },
            { "Zorro",  goZorro  },
            { "Conejo", goConejo },
            { "Gato",   goGato   }
        };
    }

    // Lee los datos del pedido desde GameManager y muestra el cliente
    public void GenerateOrder()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        customerName  = gm.currentCustomer;
        requiredColor = gm.requiredColor;
        requiredShape = gm.requiredShape;
        currentReward = gm.currentReward;
        playerColor   = "";
        playerShape   = "";

        ShowCurrentCustomer();
        Debug.Log($"{customerName} quiere una piedra: {requiredColor} con forma de {requiredShape}");
    }

    public void ShowCurrentCustomer()
    {
        foreach (var entry in customerObjects)
        {
            if (entry.Value != null)
                entry.Value.SetActive(entry.Key == customerName);
        }
    }

    public void SetPlayerColor(string color)
    {
        playerColor = color;
        if (GameManager.Instance != null)
            GameManager.Instance.playerColor = color;
    }

    public void SetPlayerShape(string shape)
    {
        playerShape = shape;
        if (GameManager.Instance != null)
            GameManager.Instance.playerShape = shape;
    }
}

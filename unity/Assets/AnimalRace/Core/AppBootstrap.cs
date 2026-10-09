using UnityEngine;

public sealed class AppBootstrap : MonoBehaviour
{
    [SerializeField]
    private AndroidSerialService serialService;

    [SerializeField]
    private DisplayStateController displayController;

    public AndroidSerialService SerialService
    {
        get { return serialService; }
        set { serialService = value; }
    }

    public DisplayStateController DisplayController
    {
        get { return displayController; }
        set { displayController = value; }
    }

    private void Awake()
    {
        if (serialService == null)
        {
            serialService = FindObjectOfType<AndroidSerialService>();
        }
        if (displayController == null)
        {
            displayController = FindObjectOfType<DisplayStateController>();
        }

        ReconnectManager reconnect = GetComponent<ReconnectManager>();
        if (reconnect == null)
        {
            reconnect = gameObject.AddComponent<ReconnectManager>();
        }
        reconnect.SerialService = serialService;
    }
}

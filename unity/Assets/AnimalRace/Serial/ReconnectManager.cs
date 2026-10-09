using UnityEngine;

public sealed class ReconnectManager : MonoBehaviour
{
    [SerializeField]
    private AndroidSerialService serialService;

    [SerializeField]
    private float retryIntervalSeconds = 2f;

    private float nextRetryAt;

    public AndroidSerialService SerialService
    {
        get { return serialService; }
        set { serialService = value; }
    }

    private void Awake()
    {
        if (serialService == null)
        {
            serialService = FindObjectOfType<AndroidSerialService>();
        }
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (serialService == null || serialService.IsOpen || Time.realtimeSinceStartup < nextRetryAt)
        {
            return;
        }

        nextRetryAt = Time.realtimeSinceStartup + retryIntervalSeconds;
        serialService.OpenSerial();
#endif
    }
}

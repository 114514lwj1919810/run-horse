using System;
using UnityEngine;

public sealed class AndroidSerialService : MonoBehaviour
{
    private const string BridgeClassName = "com.example.serialplugin.UnitySerialBridge";
    private const string ReceiverObjectName = "SerialReceiver";
    private const string ErrorQueuePrefix = "\u0001ERROR:";
    private const int NormalBaudRate = 115200;

    [SerializeField]
    private SerialDebugPanel debugPanel;

    private AndroidJavaClass serialBridge;
    private MainThreadMessageQueue messageQueue;
    private readonly SerialLineBuffer lineBuffer = new SerialLineBuffer();

    public event Action<string> OnLineReceived;
    public event Action<string> OnLineSent;
    public event Action<bool> OnConnectionChanged;

    public bool IsOpen { get; private set; }
    public string LastSentText { get; private set; }
    public int SuccessfulSendCount { get; private set; }

    public SerialDebugPanel DebugPanel
    {
        get { return debugPanel; }
        set { debugPanel = value; }
    }

    private void Awake()
    {
        gameObject.name = ReceiverObjectName;

        messageQueue = GetComponent<MainThreadMessageQueue>();
        if (messageQueue == null)
        {
            messageQueue = gameObject.AddComponent<MainThreadMessageQueue>();
        }

        if (debugPanel == null)
        {
            debugPanel = FindObjectOfType<SerialDebugPanel>();
        }

        if (debugPanel != null)
        {
            debugPanel.SerialService = this;
            debugPanel.SetConnectionState(false);
        }
    }

    private void Start()
    {
        OpenSerial();
    }

    private void Update()
    {
        if (messageQueue == null)
        {
            return;
        }

        string queued;
        while (messageQueue.TryDequeue(out queued))
        {
            if (queued.StartsWith(ErrorQueuePrefix, StringComparison.Ordinal))
            {
                HandleSerialFailure(queued.Substring(ErrorQueuePrefix.Length));
                break;
            }

            lineBuffer.AppendCallbackLine(queued);
            string line;
            while (lineBuffer.TryReadLine(out line))
            {
                DispatchLineOnMainThread(line);
            }
        }
    }

    public bool OpenSerial()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (serialBridge != null)
        {
            return IsOpen;
        }

        try
        {
            serialBridge = new AndroidJavaClass(BridgeClassName);
            serialBridge.CallStatic(
                "configureUnityReceiver",
                ReceiverObjectName,
                "OnSerialReceived",
                "OnSerialError");
            bool opened = serialBridge.CallStatic<bool>("openSerial");
            SetConnectionState(opened);

            if (!opened)
            {
                AddError("openSerial returned false for /dev/ttyS2 at " + NormalBaudRate);
            }

            return opened;
        }
        catch (Exception exception)
        {
            SetConnectionState(false);
            AddError("Open exception: " + exception.Message);
            Debug.LogException(exception);
            DisposeBridge();
            return false;
        }
#else
        SetConnectionState(false);
        Debug.Log("AndroidSerialService: editor mode, serial port remains closed.");
        return false;
#endif
    }

    public bool SendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            AddError("Refused to send an empty line");
            return false;
        }

        if (debugPanel != null)
        {
            debugPanel.AddTx(text);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (serialBridge == null || !IsOpen)
        {
            AddError("Send failed: serial port is closed");
            return false;
        }

        try
        {
            bool success = serialBridge.CallStatic<bool>("sendText", text);
            if (!success)
            {
                AddError("sendText returned false: " + text);
            }
            else
            {
                RecordSuccessfulSend(text);
            }

            return success;
        }
        catch (Exception exception)
        {
            AddError("Send exception: " + exception.Message);
            Debug.LogException(exception);
            return false;
        }
#else
        Debug.Log("AndroidSerialService editor TX: " + text);
        RecordSuccessfulSend(text);
        return true;
#endif
    }

    public void OnSerialReceived(string rawLine)
    {
        if (messageQueue != null)
        {
            messageQueue.Enqueue(rawLine ?? string.Empty);
        }
    }

    public void OnSerialError(string message)
    {
        if (messageQueue != null)
        {
            messageQueue.Enqueue(ErrorQueuePrefix + (message ?? "Unknown serial error"));
        }
    }

    private void DispatchLineOnMainThread(string line)
    {
        if (debugPanel != null)
        {
            debugPanel.AddRx(line);
        }

        Debug.Log("AndroidSerialService RX: " + line);
        Action<string> handler = OnLineReceived;
        if (handler != null)
        {
            handler(line);
        }
    }

    public void CloseSerial()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (serialBridge != null)
        {
            try
            {
                serialBridge.CallStatic("closeSerial");
            }
            catch (Exception exception)
            {
                AddError("Close exception: " + exception.Message);
                Debug.LogException(exception);
            }
        }
#endif

        SetConnectionState(false);
        DisposeBridge();
    }

    private void SetConnectionState(bool isOpen)
    {
        bool changed = IsOpen != isOpen;
        IsOpen = isOpen;

        if (debugPanel != null)
        {
            debugPanel.SetConnectionState(isOpen);
        }

        if (changed)
        {
            Action<bool> handler = OnConnectionChanged;
            if (handler != null)
            {
                handler(isOpen);
            }
        }
    }

    private void RecordSuccessfulSend(string text)
    {
        LastSentText = text;
        SuccessfulSendCount++;

        Action<string> handler = OnLineSent;
        if (handler != null)
        {
            handler(text);
        }
    }

    private void AddError(string message)
    {
        Debug.LogError("AndroidSerialService: " + message);

        if (debugPanel != null)
        {
            debugPanel.AddError(message);
        }
    }

    private void HandleSerialFailure(string message)
    {
        AddError(message);
        lineBuffer.Clear();
        if (messageQueue != null)
        {
            messageQueue.Clear();
        }
        CloseSerial();
    }

    private void DisposeBridge()
    {
        if (serialBridge != null)
        {
            serialBridge.Dispose();
            serialBridge = null;
        }
    }

    private void OnApplicationQuit()
    {
        CloseSerial();
    }

    private void OnDestroy()
    {
        CloseSerial();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SerialDebugPanel : MonoBehaviour
{
    private enum EntryKind
    {
        Rx,
        Tx,
        Error
    }

    private sealed class LogEntry
    {
        public string Timestamp;
        public string Message;
        public EntryKind Kind;
    }

    public Font uiFont;

    [SerializeField]
    private AndroidSerialService serialService;

    [SerializeField]
    private int maxEntries = 150;

    [SerializeField]
    private bool startVisible;

    private readonly List<LogEntry> entries = new List<LogEntry>();
    private int rxCount;
    private int txCount;
    private int errorCount;
    private bool isOpen;
    private bool isVisible;
    private bool scrollToEnd;
    private Vector2 scrollPosition;
    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle logStyle;
    private GUIStyle buttonStyle;
    private Texture2D panelTexture;
    private float nextSendAllowedAt;

    public static SerialDebugPanel Instance { get; private set; }

    public AndroidSerialService SerialService
    {
        get { return serialService; }
        set { serialService = value; }
    }

    public bool StartVisible
    {
        get { return startVisible; }
        set { startVisible = value; }
    }

    private void Awake()
    {
        Instance = this;
        maxEntries = Mathf.Clamp(maxEntries, 100, 200);
        isVisible = startVisible;

        if (serialService == null)
        {
            serialService = FindObjectOfType<AndroidSerialService>();
        }

        if (serialService != null)
        {
            serialService.DebugPanel = this;
        }
    }

    public void SetConnectionState(bool open)
    {
        isOpen = open;
    }

    public void AddRx(string rawLine)
    {
        rxCount++;
        AddEntry(EntryKind.Rx, rawLine);
    }

    public void AddTx(string rawLine)
    {
        txCount++;
        AddEntry(EntryKind.Tx, rawLine);
    }

    public void AddError(string message)
    {
        errorCount++;
        AddEntry(EntryKind.Error, message);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            isVisible = !isVisible;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitApplication();
        }
    }

    private void AddEntry(EntryKind kind, string message)
    {
        string normalized = (message ?? string.Empty).TrimEnd('\r', '\n');
        entries.Add(new LogEntry
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
            Message = normalized,
            Kind = kind
        });

        while (entries.Count > maxEntries)
        {
            entries.RemoveAt(0);
        }

        scrollToEnd = true;
    }

    private void OnGUI()
    {
        // Keep the service panel above the main game IMGUI on every platform.
        GUI.depth = -100;
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));

        if (!isVisible)
        {
            Rect hiddenHotspot = new Rect(1208f, 0f, 72f, 72f);
            if (Event.current.type == EventType.MouseUp &&
                hiddenHotspot.Contains(Event.current.mousePosition))
            {
                isVisible = true;
                Event.current.Use();
            }
            GUI.matrix = previousMatrix;
            return;
        }

        Rect panelRect = new Rect(790f, 20f, 470f, 250f);
        GUI.Box(panelRect, GUIContent.none, panelStyle);

        string connection = isOpen ? "OPEN" : "CLOSED";
        string header = string.Format(
            "SERIAL: {0} 115200  RX:{1} TX:{2} ERR:{3}",
            connection,
            rxCount,
            txCount,
            errorCount);

        GUI.Label(new Rect(806f, 31f, 365f, 34f), header, headerStyle);

        if (GUI.Button(new Rect(1180f, 29f, 62f, 32f), "隐藏", buttonStyle))
        {
            isVisible = false;
        }

        Rect viewRect = new Rect(806f, 70f, 436f, 128f);
        float contentHeight = Mathf.Max(viewRect.height - 4f, entries.Count * 25f + 4f);
        Rect contentRect = new Rect(0f, 0f, 414f, contentHeight);

        if (scrollToEnd)
        {
            scrollPosition.y = Mathf.Max(0f, contentHeight - viewRect.height);
            scrollToEnd = false;
        }

        scrollPosition = GUI.BeginScrollView(viewRect, scrollPosition, contentRect);
        for (int index = 0; index < entries.Count; index++)
        {
            LogEntry entry = entries[index];
            GUI.Label(
                new Rect(2f, index * 25f, 405f, 25f),
                FormatEntry(entry),
                logStyle);
        }
        GUI.EndScrollView();

        if (GUI.Button(new Rect(806f, 207f, 140f, 44f), "退出", buttonStyle))
        {
            ExitApplication();
            GUI.matrix = previousMatrix;
            return;
        }

        if (GUI.Button(new Rect(1082f, 207f, 160f, 44f), "发送 A", buttonStyle))
        {
            if (Time.unscaledTime < nextSendAllowedAt)
            {
                GUI.matrix = previousMatrix;
                return;
            }

            nextSendAllowedAt = Time.unscaledTime + 0.4f;

            if (serialService == null)
            {
                AddError("AndroidSerialService is missing");
            }
            else
            {
                serialService.SendText("A\r\n");
            }
        }

        GUI.matrix = previousMatrix;
    }

    private void ExitApplication()
    {
        if (serialService != null)
        {
            serialService.CloseSerial();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private string FormatEntry(LogEntry entry)
    {
        string prefix;
        string color;

        switch (entry.Kind)
        {
            case EntryKind.Rx:
                prefix = "RX";
                color = "#63D5A5";
                break;
            case EntryKind.Tx:
                prefix = "TX";
                color = "#65B7FF";
                break;
            default:
                prefix = "ERR";
                color = "#FF746C";
                break;
        }

        return string.Format(
            "{0} <color={1}>[{2}]</color> {3}",
            entry.Timestamp,
            color,
            prefix,
            entry.Message);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        panelTexture.SetPixel(0, 0, new Color(0.035f, 0.045f, 0.055f, 0.88f));
        panelTexture.Apply();

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft
        };
        headerStyle.normal.textColor = Color.white;

        logStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 17,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            wordWrap = false
        };
        logStyle.normal.textColor = new Color(0.9f, 0.92f, 0.94f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter
        };
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (panelTexture != null)
        {
            Destroy(panelTexture);
            panelTexture = null;
        }
    }
}

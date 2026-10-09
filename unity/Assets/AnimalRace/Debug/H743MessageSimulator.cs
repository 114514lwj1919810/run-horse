using UnityEngine;

public sealed class H743MessageSimulator : MonoBehaviour
{
    [SerializeField]
    private DisplayStateController displayController;

    [TextArea(3, 5)]
    public string simulatedLine =
        "SHOW_BETTING:seq=10,round=1,credit=600,total_bet=100,selected=4,bets=0|0|0|100|0|0,odds=24|24|8|6|3|3";

    public Font uiFont;

    private int simulatedWinner = 4;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle textAreaStyle;
    private bool isVisible = true;
    private uint nextSequence = 20U;

    public DisplayStateController DisplayController
    {
        get { return displayController; }
        set { displayController = value; }
    }

    private void Awake()
    {
        if (displayController == null)
        {
            displayController = FindObjectOfType<DisplayStateController>();
        }
    }

    public bool Submit(string line)
    {
        if (displayController == null)
        {
            return false;
        }

        simulatedLine = line;
        return displayController.ProcessRawLine(line);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));

        if (!isVisible)
        {
            if (GUI.Button(new Rect(1136f, 76f, 124f, 42f), "模拟器", buttonStyle))
            {
                isVisible = true;
            }

            GUI.matrix = previousMatrix;
            return;
        }

        GUILayout.BeginArea(new Rect(760f, 166f, 500f, 534f), GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("H743 PC 消息模拟器", labelStyle);
        if (GUILayout.Button("收起", buttonStyle, GUILayout.Width(84f), GUILayout.Height(30f)))
        {
            isVisible = false;
        }
        GUILayout.EndHorizontal();
        simulatedLine = GUILayout.TextArea(simulatedLine, textAreaStyle, GUILayout.Height(82f));

        GUILayout.BeginHorizontal();
        DrawButton("HELLO", "hello");
        DrawButton("FULL_STATE", "FULL_STATE:seq=9,state=BETTING,round=1,credit=600,total_bet=100,selected=4,bets=0|0|0|100|0|0,odds=24|24|8|6|3|3,winner=0,win_total=0,win_remaining=0,last_win=0,race_seq=0,result_seq=0,collect_seq=0,collect_active=0,pending_payout=0,fault=0");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawButton("IDLE", "SHOW_IDLE:seq=9,round=1,credit=600,last_win=0,win=0");
        DrawButton("BETTING", "SHOW_BETTING:seq=10,round=1,credit=600,total_bet=100,selected=4,bets=0|0|0|100|0|0,odds=24|24|8|6|3|3");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("RACE 1~6", buttonStyle))
        {
            SubmitNext("SHOW_RACE:seq=11,round=1,winner=" + simulatedWinner + ",duration_ms=15000,bets=0|0|0|100|0|0,odds=24|24|8|6|3|3");
            simulatedWinner = simulatedWinner >= 6 ? 1 : simulatedWinner + 1;
        }
        DrawButton("RESULT", "SHOW_RESULT:seq=12,round=1,result=WIN,winner=4,odds=6,win_total=600,win_remaining=600,credit=500,duration_ms=3000");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawButton("WIN READY", "SHOW_WIN_READY:seq=13,round=1,win=600,credit=500");
        DrawButton("WIN UPDATE", "WIN_TRANSFER_UPDATE:seq=14,round=1,win=500,credit=600,delta=100,source=WIN_COLLECT");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawButton("WIN DONE", "WIN_TRANSFER_DONE:seq=15,round=1,win=0,credit=1100,fast=1");
        DrawButton("SETTING", "SHOW_SETTING:seq=16,index=1,total=16,item=RACE_DURATION_MS,value=15000,min=5000,max=60000");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawButton("PAYOUT", "SHOW_PAYOUT:seq=17,balance_before=1100,coins=11,remainder=0");
        DrawButton("DONE", "SHOW_PAYOUT_DONE:seq=18,coins=11,balance=0");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawButton("FAULT", "SHOW_FAULT:seq=19,code=3,detail=coin_sensor_timeout,recoverable=1");
        DrawButton("错误报文", "SHOW_RACE:seq=20,round=1,winner=9");
        GUILayout.EndHorizontal();

        if (GUILayout.Button("发送输入框报文", buttonStyle, GUILayout.Height(34f)))
        {
            Submit(simulatedLine);
        }

        GUILayout.EndArea();
        GUI.matrix = previousMatrix;
    }

    private void DrawButton(string label, string line)
    {
        if (GUILayout.Button(label, buttonStyle, GUILayout.Height(30f)))
        {
            SubmitNext(line);
        }
    }

    private void SubmitNext(string line)
    {
        int start = line.IndexOf("seq=");
        if (start >= 0)
        {
            int valueStart = start + 4;
            int end = line.IndexOf(',', valueStart);
            if (end < 0)
            {
                end = line.Length;
            }
            line = line.Substring(0, valueStart) + nextSequence + line.Substring(end);
            nextSequence++;
        }
        Submit(line);
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 20,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        textAreaStyle = new GUIStyle(GUI.skin.textArea)
        {
            font = uiFont,
            fontSize = 15,
            wordWrap = true
        };
    }
#endif
}

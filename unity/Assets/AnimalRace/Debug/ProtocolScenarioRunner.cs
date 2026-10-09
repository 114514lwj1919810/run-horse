using UnityEngine;

public sealed class ProtocolScenarioRunner : MonoBehaviour
{
    [SerializeField]
    private DisplayStateController displayController;

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

    public bool RunWinningRound()
    {
        if (displayController == null)
        {
            return false;
        }

        string[] lines =
        {
            "SHOW_BETTING:seq=100,round=2,credit=30,total_bet=20,selected=4,bets=0|0|0|20|0|0,odds=24|24|8|6|3|3",
            "SHOW_RACE:seq=101,round=2,winner=4,duration_ms=15000,bets=0|0|0|20|0|0,odds=24|24|8|6|3|3",
            "SHOW_RESULT:seq=102,round=2,result=WIN,winner=4,odds=6,win_total=120,win_remaining=120,credit=30,duration_ms=3000",
            "SHOW_WIN_READY:seq=103,round=2,win=120,credit=30",
            "WIN_TRANSFER_UPDATE:seq=104,round=2,win=80,credit=70,delta=40,source=WIN_COLLECT",
            "WIN_TRANSFER_DONE:seq=105,round=2,win=0,credit=150,fast=1"
        };

        for (int index = 0; index < lines.Length; index++)
        {
            if (!displayController.ProcessRawLine(lines[index]))
            {
                return false;
            }
        }
        return true;
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ProtocolV31Tests
{
    [MenuItem("Animal Race/Run V3.1 Protocol Tests")]
    public static void RunFromMenu()
    {
        RunAll();
        Debug.Log("V31_PROTOCOL_TESTS_PASSED");
    }

    public static void RunBatch()
    {
        RunAll();
        Debug.Log("V31_PROTOCOL_TESTS_PASSED");
    }

    private static void RunAll()
    {
        TestLineBuffer();
        TestParserCompatibility();
        TestEarlyResultIsDeferred();
        TestEarlyNextPageIsDeferredThroughBothTimers();
        TestNextPageArrivingAfterResultCompletionIsAppliedImmediately();
        TestResultAfterRaceOverIsAppliedImmediately();
        TestWinCollectionFlow();
        TestFullStateRecovery();
        TestStandbyDemoProtocol();
        TestSerialErrorReleasesConnectionForReconnect();
    }

    private static void TestEarlyResultIsDeferred()
    {
        var root = new GameObject("V31EarlyResultTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;

            Require(display.ProcessRawLine(
                "SHOW_RACE:seq=60,round=4,winner=3,duration_ms=15000," +
                "bets=10|0|20|0|20|0,odds=24|24|8|6|3|3"),
                "race was rejected");
            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=61,round=4,result=WIN,winner=3,odds=8," +
                "win_total=160,win_remaining=160,credit=0,duration_ms=3000"),
                "early result was rejected");
            Require(display.CurrentPage == DisplayPage.Race && display.CurrentSequence == 60,
                "early result skipped the race presentation");

            Require(display.CompleteRacePresentation(4),
                "deferred result was not released after the race");
            Require(display.CurrentPage == DisplayPage.Result && display.CurrentSequence == 61 &&
                    display.WinPoints == 160,
                "deferred result values were not applied");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestEarlyNextPageIsDeferredThroughBothTimers()
    {
        var root = new GameObject("V31EarlyNextPageTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;

            Require(display.ProcessRawLine(
                "SHOW_RACE:seq=70,round=5,winner=4,duration_ms=15000," +
                "bets=10|0|0|0|0|0,odds=24|24|8|6|3|3"), "race was rejected");
            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=71,round=5,result=LOSE,winner=4,odds=6," +
                "win_total=0,win_remaining=0,credit=20,duration_ms=3000"),
                "early result was rejected");
            Require(display.ProcessRawLine(
                "SHOW_BETTING:seq=72,round=5,credit=20,total_bet=0,selected=1," +
                "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3"),
                "early betting was rejected");
            Require(display.CurrentPage == DisplayPage.Race && display.CurrentSequence == 70,
                "early next page interrupted the configured race duration");

            Require(display.CompleteRacePresentation(5), "buffered result was not released");
            Require(display.CurrentPage == DisplayPage.Result && display.CurrentSequence == 71,
                "result page was skipped after the race duration");
            Require(display.CompleteResultPresentation(5), "buffered betting page was not released");
            Require(display.CurrentPage == DisplayPage.Betting && display.CurrentSequence == 72,
                "betting page did not wait for the result duration");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestResultAfterRaceOverIsAppliedImmediately()
    {
        var root = new GameObject("V31LateResultTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;

            Require(display.ProcessRawLine(
                "SHOW_RACE:seq=80,round=6,winner=2,duration_ms=15000," +
                "bets=0|10|0|0|0|0,odds=24|24|8|6|3|3"), "race was rejected");
            Require(!display.CompleteRacePresentation(6),
                "race completion unexpectedly found an early result");
            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=81,round=6,result=WIN,winner=2,odds=24," +
                "win_total=240,win_remaining=240,credit=20,duration_ms=3000"),
                "late result was rejected");
            Require(display.CurrentPage == DisplayPage.Result && display.CurrentSequence == 81,
                "result sent after RACE_OVER was buffered forever");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestNextPageArrivingAfterResultCompletionIsAppliedImmediately()
    {
        var root = new GameObject("V31LateNextPageTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;

            Require(display.ProcessRawLine(
                "SHOW_RACE:seq=75,round=6,winner=4,duration_ms=15000," +
                "bets=10|0|0|0|0|0,odds=24|24|8|6|3|3"), "race was rejected");
            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=76,round=6,result=WIN,winner=4,odds=6," +
                "win_total=60,win_remaining=60,credit=20,duration_ms=3000"),
                "early result was rejected");
            Require(display.CompleteRacePresentation(6), "buffered result was not released");

            Require(!display.CompleteResultPresentation(6),
                "result completion unexpectedly found an early next page");
            Require(display.ProcessRawLine(
                "SHOW_WIN_READY:seq=77,round=6,win=60,credit=20"),
                "late win-ready page was rejected");
            Require(display.CurrentPage == DisplayPage.WinReady && display.CurrentSequence == 77,
                "next page arriving after RESULT_OVER was buffered forever");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestLineBuffer()
    {
        var buffer = new SerialLineBuffer();
        IList<string> first = buffer.Append("SHOW_IDLE:seq=1,round=1\r");
        IList<string> second = buffer.Append("\nSHOW_WIN_READY:seq=2,round=1,win=60,credit=0\r\n");
        Require(first.Count == 0, "fragment was emitted before LF");
        Require(second.Count == 2, "fragmented and packed lines were not recovered");
        Require(second[1].StartsWith("SHOW_WIN_READY"), "second frame was corrupted");

        buffer.Clear();
        buffer.MaximumLineLength = 8;
        buffer.Append("123456789\r\n");
        Require(buffer.DroppedLineCount == 1, "oversize frame was not counted");
    }

    private static void TestParserCompatibility()
    {
        var parser = new GameProtocolParser();
        ProtocolMessage message;
        string error;

        Require(parser.TryParse(
            "SHOW_BETTING:seq=10,round=1,credit=30,total_bet=20,selected=4," +
            "bets=0|0|0|20|0|0,odds=24|24|8|6|3|3,unknown_future=1",
            out message, out error), "current H743 SHOW_BETTING failed: " + error);
        var betting = message as ShowBettingMessage;
        Require(betting != null && betting.Credit == 30 && betting.Bets[3] == 20,
            "credit betting values changed");

        Require(parser.TryParse(
            "SHOW_BETTING:seq=11,round=1,balance=40,total_bet=10,selected=1," +
            "bets=10|0|0|0|0|0,odds=24|24|8|6|3|3",
            out message, out error), "legacy balance alias failed: " + error);

        Require(parser.TryParse(
            "SHOW_RESULT:seq=12,round=1,result=WIN,winner=4,odds=6," +
            "win_total=120,win_remaining=120,credit=30,duration_ms=3000",
            out message, out error), "V3.1 SHOW_RESULT failed: " + error);
        var result = message as ShowResultMessage;
        Require(result != null && result.WinTotal == 120 && result.WinRemaining == 120 && result.Credit == 30,
            "WIN result values changed");
    }

    private static void TestWinCollectionFlow()
    {
        var root = new GameObject("V31WinFlowTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;

            Require(display.BeginHandshake(), "UNITY_READY could not be sent in editor mode");
            Require(serial.LastSentText.Contains("protocol=" + ProtocolValidation.ProtocolVersion), "UNITY_READY version mismatch");

            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=20,round=2,result=WIN,winner=4,odds=6," +
                "win_total=120,win_remaining=120,credit=30,duration_ms=3000"),
                "result was rejected");
            Require(display.Balance == 30 && display.WinPoints == 120,
                "result incorrectly moved WIN into CREDIT");

            Require(display.ProcessRawLine(
                "SHOW_WIN_READY:seq=21,round=2,win=120,credit=30"),
                "WIN_WAIT_START was rejected");
            Require(display.CurrentPage == DisplayPage.WinReady,
                "WIN_WAIT_START did not select win-ready view");

            Require(display.ProcessRawLine(
                "WIN_TRANSFER_UPDATE:seq=22,round=2,win=80,credit=70,delta=40,source=WIN_COLLECT"),
                "WIN_TRANSFER_UPDATE was rejected");
            Require(display.CurrentPage == DisplayPage.WinCollecting &&
                    display.WinPoints == 80 && display.Balance == 70,
                "absolute collect snapshot was not applied");

            Require(display.ProcessRawLine(
                "WIN_TRANSFER_UPDATE:seq=22,round=2,win=10,credit=999,delta=70,source=WIN_COLLECT"),
                "duplicate UPDATE should still be accepted and ACKed");
            Require(display.WinPoints == 80 && display.Balance == 70,
                "duplicate seq changed authoritative display values");

            Require(display.ProcessRawLine(
                "WIN_TRANSFER_DONE:seq=23,round=2,win=0,credit=150,fast=1"),
                "WIN_TRANSFER_DONE was rejected");
            Require(display.WinPoints == 0 && display.Balance == 150 &&
                    display.CurrentPage == DisplayPage.WinCollecting &&
                    !display.CollectActive && display.CollectFastFinished,
                "fast collect result was not held at its absolute final values");

            Require(display.ProcessRawLine(
                "SHOW_BETTING:seq=24,round=2,credit=150,total_bet=0,selected=1," +
                "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3"),
                "post-collect betting message was rejected");
            Require(display.CurrentPage == DisplayPage.WinCollecting,
                "next round replaced the fast collect result before its hold finished");

            Require(display.CompleteWinTransferPresentation(2),
                "fast collect result could not complete after its hold");
            Require(display.CurrentPage == DisplayPage.Betting && display.Balance == 150,
                "buffered betting page was not applied after the collect result hold");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestFullStateRecovery()
    {
        var root = new GameObject("V31RecoveryTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;

            Require(display.ProcessRawLine(
                "FULL_STATE:seq=105,mode=RUN,phase=WIN_COLLECTING," +
                "resume_phase=WIN_COLLECTING,round=8,credit=57,total_bet=0,selected=4," +
                "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3,winner=4," +
                "win_total=100,win_remaining=63,last_win=0,race_seq=101," +
                "race_remaining_ms=0,result_seq=102,result_remaining_ms=0," +
                "collect_seq=103,collect_active=1,collect_remaining_ms=1100," +
                "payout_target=0,payout_done=0,setting_index=0,fault=0"),
                "formal V3.1 FULL_STATE failed");
            Require(display.CurrentMode == DisplayMode.Run &&
                    display.CurrentPhase == DisplayPhase.WinCollecting &&
                    display.CurrentPage == DisplayPage.WinCollecting,
                "FULL_STATE did not restore collecting phase");
            Require(display.CurrentSequence == 103 && display.WinPoints == 63 &&
                    display.Balance == 57 && display.CollectRemainingMilliseconds == 1100,
                "FULL_STATE collecting transaction values changed");

            Require(display.ProcessRawLine(
                "FULL_STATE:seq=106,state=BETTING,round=9,credit=150,total_bet=0,selected=1," +
                "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3,winner=0,win_total=0," +
                "win_remaining=0,last_win=120,race_seq=0,result_seq=0,collect_seq=0," +
                "collect_active=0,pending_payout=0,fault=0"),
                "current H743 FULL_STATE failed");
            Require(display.CurrentPage == DisplayPage.Betting && display.Balance == 150,
                "current H743 state alias was not restored");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestStandbyDemoProtocol()
    {
        var root = new GameObject("V32StandbyDemoTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;
            var animation = root.AddComponent<AnimalRaceAnimationController>();
            animation.DisplayController = display;
            animation.SerialService = serial;

            Require(display.ProcessRawLine(
                "SHOW_RACE:seq=300,round=77,winner=5,duration_ms=15000," +
                "bets=0|0|0|0|0|0,odds=24|15|10|6|3|3,demo=1"),
                "standby race was rejected");
            Require(display.CurrentPage == DisplayPage.Race && display.IsStandbyDemo,
                "standby race was not marked as demo");
            animation.BeginRace(300, 77, 5, 15000);
            animation.FinishRace();
            Require(serial.LastSentText.Contains("DEMO_RACE_OVER:seq=300,round=77"),
                "standby race emitted a financial RACE_OVER response");

            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=301,round=77,result=LOSE,winner=5,odds=3," +
                "win_total=0,win_remaining=0,credit=0,duration_ms=3000,demo=1"),
                "standby result was rejected");
            Require(display.CurrentPage == DisplayPage.Result && display.IsStandbyDemo &&
                    display.WinnerHistory[display.WinnerHistory.Length - 1] == 0,
                "standby result polluted paid-game winner history");
            Require(Mathf.Approximately(ResultDisplay.GetPresentationDurationSeconds(3000, true), 3f) &&
                    Mathf.Approximately(ResultDisplay.GetPresentationDurationSeconds(3000, false), 1.5f),
                "standby result does not hold for exactly three seconds");

            var sender = new ProtocolSender(serial);
            Require(sender.SendDemoResultOver(301, 77) &&
                    serial.LastSentText.Contains("DEMO_RESULT_OVER:seq=301,round=77"),
                "standby result completion response is missing");

            Require(display.ProcessRawLine(
                "SHOW_IDLE:seq=302,round=77,credit=0,last_win=0,win=0"),
                "standby cancel idle was rejected");
            Require(!display.IsStandbyDemo && display.CurrentPage == DisplayPage.Idle,
                "authoritative idle did not cancel standby demo");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestSerialErrorReleasesConnectionForReconnect()
    {
        var root = new GameObject("V31SerialReconnectTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            MethodInfo awake = typeof(AndroidSerialService).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo setConnection = typeof(AndroidSerialService).GetMethod(
                "SetConnectionState", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo update = typeof(AndroidSerialService).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(awake != null && setConnection != null && update != null,
                "serial connection test hooks were not found");
            awake.Invoke(serial, null);
            setConnection.Invoke(serial, new object[] { true });

            int closeEvents = 0;
            serial.OnConnectionChanged += open =>
            {
                if (!open) closeEvents++;
            };
            serial.OnSerialError("simulated receive failure");
            serial.OnSerialReceived("SHOW_IDLE:seq=99,round=1,credit=0");
            update.Invoke(serial, null);

            var queue = root.GetComponent<MainThreadMessageQueue>();
            Require(!serial.IsOpen && closeEvents == 1,
                "receive failure did not release the connection for reconnect");
            Require(queue != null && queue.Count == 0,
                "stale callbacks survived a failed serial connection");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception("V3.1 TEST FAILED: " + message);
        }
    }
}

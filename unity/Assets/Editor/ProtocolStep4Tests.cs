using System;
using UnityEditor;
using UnityEngine;

public static class ProtocolStep4Tests
{
    private const string ReadyLine =
        "UNITY_READY:app=AnimalRace,version=1.2.0,protocol=" + ProtocolValidation.ProtocolVersion + "\r\n";

    [MenuItem("Animal Race/Run Step 4 Handshake Tests")]
    public static void RunFromMenu()
    {
        RunAll();
        Debug.Log("STEP4_TESTS_PASSED");
    }

    public static void RunBatch()
    {
        RunAll();
        Debug.Log("STEP4_TESTS_PASSED");
    }

    private static void RunAll()
    {
        var root = new GameObject("ProtocolStep4Test");
        var serial = root.AddComponent<AndroidSerialService>();
        var display = root.AddComponent<DisplayStateController>();
        display.SerialService = serial;

        try
        {
            Require(display.BeginHandshake(), "Unity did not actively send UNITY_READY");
            Require(serial.LastSentText == ReadyLine, "UNITY_READY text is incorrect");
            Require(!display.HandshakeComplete, "Handshake completed before FULL_STATE");

            Require(display.ProcessRawLine("hello"), "Lowercase hello was rejected");
            Require(serial.LastSentText == ReadyLine, "hello did not retain READY response");

            const string bettingState =
                "FULL_STATE:seq=7,state=BETTING,round=12,balance=650,total_bet=150,selected=4," +
                "bets=0|0|0|150|0|0,odds=24|24|8|6|3|3,pending_payout=0,fault=0";
            Require(display.ProcessRawLine(bettingState), "BETTING FULL_STATE failed");
            Require(display.HandshakeComplete, "FULL_STATE did not complete handshake");
            Require(display.CurrentPage == DisplayPage.Betting, "BETTING state restored wrong page");
            Require(display.CurrentRound == 12 && display.Balance == 650, "Authority values changed");
            Require(display.Bets[3] == 150 && display.SelectedId == 4, "Bet snapshot changed");
            Require(serial.LastSentText == "ACK:seq=7,status=OK\r\n", "FULL_STATE ACK is incorrect");

            Require(display.ProcessRawLine(bettingState), "Duplicate FULL_STATE failed");
            Require(display.Balance == 650 && display.TotalBet == 150, "Duplicate state changed values");

            Require(display.ProcessRawLine("hello"), "Restart hello failed");
            Require(!display.HandshakeComplete, "Restart hello did not reset handshake");
            Require(serial.LastSentText == ReadyLine, "Restart hello did not send READY");

            Require(display.ProcessRawLine(
                "FULL_STATE:seq=8,state=WAIT_RACE_OVER,round=12,balance=500,total_bet=150,selected=4," +
                "bets=0|0|0|150|0|0,odds=24|24|8|6|3|3,pending_payout=0,fault=0"),
                "RACE FULL_STATE failed");
            Require(display.CurrentPage == DisplayPage.Race, "Race state restored wrong page");
            Require(serial.LastSentText == "ACK:seq=8,status=OK\r\n", "Race state ACK is incorrect");
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
            throw new Exception("STEP4 TEST FAILED: " + message);
        }
    }
}

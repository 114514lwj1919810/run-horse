using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ProtocolStep2Tests
{
    [MenuItem("Animal Race/Run Step 2 Protocol Tests")]
    public static void RunFromMenu()
    {
        RunAll();
        Debug.Log("STEP2_TESTS_PASSED");
    }

    public static void RunBatch()
    {
        RunAll();
        Debug.Log("STEP2_TESTS_PASSED");
    }

    private static void RunAll()
    {
        TestLineBuffer();
        TestParserCommands();
        TestDisplayPagesAndAnimalMapping();
        TestInvalidMessagesDoNotChangePage();
    }

    private static void TestLineBuffer()
    {
        var buffer = new SerialLineBuffer();

        IList<string> first = buffer.Append("SHOW_IDLE:seq=1,round=1\r");
        Require(first.Count == 0, "Line buffer emitted an incomplete line");

        IList<string> second = buffer.Append(
            "\nSHOW_RACE:seq=2,round=1,winner=4,duration_ms=15000\r\nPART");
        Require(second.Count == 2, "Line buffer did not emit two complete lines");
        Require(second[0] == "SHOW_IDLE:seq=1,round=1", "Line buffer did not remove CR");
        Require(second[1].StartsWith("SHOW_RACE:"), "Line buffer split the wrong frame");
        Require(buffer.PendingText == "PART", "Line buffer did not retain the partial tail");
    }

    private static void TestParserCommands()
    {
        var parser = new GameProtocolParser();

        RequireType<HelloMessage>(parser, "HELLO:device=1,fw=2.1.0,protocol=1");
        RequireType<HelloMessage>(parser, "hello");
        RequireType<FullStateMessage>(
            parser,
            "FULL_STATE:seq=1,state=IDLE,round=0,balance=0,total_bet=0,selected=1,bets=0|0|0|0|0|0,odds=24|24|8|6|3|3,pending_payout=0,fault=0");
        RequireType<ShowIdleMessage>(
            parser,
            "SHOW_IDLE:seq=2,round=1,balance=600,last_win=0");
        RequireType<ShowBettingMessage>(
            parser,
            "SHOW_BETTING:seq=10,round=1,balance=600,total_bet=100,selected=4,bets=0|0|0|100|0|0,odds=24|24|8|6|3|3");
        RequireType<ShowRaceMessage>(
            parser,
            "SHOW_RACE:seq=11,round=1,winner=4,duration_ms=15000");
        RequireType<ShowResultMessage>(
            parser,
            "SHOW_RESULT:seq=12,round=1,result=WIN,winner=4,odds=6,win_points=600,balance=1100");
        RequireType<ShowSettingMessage>(
            parser,
            "SHOW_SETTING:seq=13,item=race_duration_ms,value=15000,min=10000,max=30000");
        RequireType<ShowPayoutMessage>(
            parser,
            "SHOW_PAYOUT:seq=14,balance_before=1100,coins=11,remainder=0");
        RequireType<ShowPayoutDoneMessage>(
            parser,
            "SHOW_PAYOUT_DONE:seq=15,coins=11,balance=0");
        RequireType<ShowFaultMessage>(
            parser,
            "SHOW_FAULT:seq=16,code=3,detail=coin_sensor_timeout");
    }

    private static void TestDisplayPagesAndAnimalMapping()
    {
        var gameObject = new GameObject("ProtocolStep2TestDisplay");
        var display = gameObject.AddComponent<DisplayStateController>();

        try
        {
            Require(display.ProcessRawLine(
                "SHOW_IDLE:seq=9,round=1,balance=600,last_win=0"), "SHOW_IDLE failed");
            Require(display.CurrentPage == DisplayPage.Idle, "SHOW_IDLE did not select IDLE page");

            Require(display.ProcessRawLine(
                "SHOW_BETTING:seq=10,round=1,balance=600,total_bet=100,selected=4,bets=0|0|0|100|0|0,odds=24|24|8|6|3|3"), "SHOW_BETTING failed");
            Require(display.CurrentPage == DisplayPage.Betting, "SHOW_BETTING did not select BETTING page");
            Require(display.Balance == 600, "Display changed message balance");
            Require(display.TotalBet == 100, "Display changed message total_bet");
            Require(display.SelectedId == 4, "Display changed selected animal");
            Require(display.Bets[3] == 100, "Display changed bet array");
            Require(display.Odds[3] == 6, "Display changed odds array");

            string[] expectedNames = { "狮子", "大象", "猩猩", "斑马", "长颈鹿", "骆驼" };
            for (int winner = 1; winner <= AnimalCatalog.Count; winner++)
            {
                string raceLine = string.Format(
                    "SHOW_RACE:seq={0},round=1,winner={1},duration_ms=15000",
                    10 + winner,
                    winner);
                Require(display.ProcessRawLine(raceLine), "SHOW_RACE failed for winner " + winner);
                Require(display.CurrentPage == DisplayPage.Race, "SHOW_RACE did not select RACE page");
                Require(display.WinnerId == winner, "Winner id changed during display mapping");
                Require(display.WinnerName == expectedNames[winner - 1], "Wrong animal mapping for winner " + winner);
                Require(
                    !string.IsNullOrEmpty(AnimalCatalog.GetResourceName(winner)),
                    "Missing resource mapping for winner " + winner);
            }

            display.CompleteRacePresentation(1);
            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=20,round=1,result=WIN,winner=4,odds=6,win_points=600,balance=1100"), "SHOW_RESULT failed");
            Require(display.CurrentPage == DisplayPage.Result, "SHOW_RESULT did not select RESULT page");
            Require(display.WinPoints == 600 && display.Balance == 1100, "Display recalculated result values");

            Require(display.ProcessRawLine(
                "SHOW_SETTING:seq=21,item=race_duration_ms,value=15000,min=10000,max=30000"), "SHOW_SETTING failed");
            Require(display.CurrentPage == DisplayPage.Setting, "SHOW_SETTING did not select SETTING page");

            Require(display.ProcessRawLine(
                "SHOW_PAYOUT:seq=22,balance_before=1100,coins=11,remainder=0"), "SHOW_PAYOUT failed");
            Require(display.CurrentPage == DisplayPage.Payout, "SHOW_PAYOUT did not select PAYOUT page");

            Require(display.ProcessRawLine(
                "SHOW_FAULT:seq=23,code=3,detail=coin_sensor_timeout"), "SHOW_FAULT failed");
            Require(display.CurrentPage == DisplayPage.Fault, "SHOW_FAULT did not select FAULT page");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    private static void TestInvalidMessagesDoNotChangePage()
    {
        var gameObject = new GameObject("ProtocolStep2InvalidTest");
        var display = gameObject.AddComponent<DisplayStateController>();

        try
        {
            Require(display.ProcessRawLine(
                "SHOW_IDLE:seq=1,round=1,balance=100,last_win=0"), "Valid setup message failed");
            DisplayPage pageBeforeError = display.CurrentPage;

            Require(!display.ProcessRawLine("UNKNOWN:seq=2"), "Unknown command was accepted");
            Require(display.CurrentPage == pageBeforeError, "Unknown command changed the display page");

            Require(
                !display.ProcessRawLine("SHOW_RACE:seq=3,round=1,winner=4"),
                "Missing duration_ms was accepted");
            Require(display.CurrentPage == pageBeforeError, "Missing-field command changed the display page");

            Require(
                !display.ProcessRawLine("SHOW_BETTING:seq=4,round=1,balance=100,total_bet=50,selected=1,bets=50|0,odds=24|24|8|6|3|3"),
                "Short bets array was accepted");
            Require(display.CurrentPage == pageBeforeError, "Invalid array changed the display page");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    private static void RequireType<T>(GameProtocolParser parser, string line)
        where T : ProtocolMessage
    {
        ProtocolMessage message;
        string error;
        Require(parser.TryParse(line, out message, out error), "Parser rejected " + line + ": " + error);
        Require(message is T, "Parser returned wrong message type for " + line);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception("STEP2 TEST FAILED: " + message);
        }
    }
}

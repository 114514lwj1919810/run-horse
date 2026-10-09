using System;
using UnityEditor;
using UnityEngine;

public static class ProtocolStep5Tests
{
    [MenuItem("Animal Race/Run Step 5 Main Scene Tests")]
    public static void RunFromMenu()
    {
        RunAll();
        Debug.Log("STEP5_TESTS_PASSED");
    }

    public static void RunBatch()
    {
        RunAll();
        Debug.Log("STEP5_TESTS_PASSED");
    }

    private static void RunAll()
    {
        var root = new GameObject("ProtocolStep5Test");
        var serial = root.AddComponent<AndroidSerialService>();
        var display = root.AddComponent<DisplayStateController>();
        var animation = root.AddComponent<AnimalRaceAnimationController>();
        var result = root.AddComponent<ResultDisplay>();
        var payout = root.AddComponent<PayoutDisplay>();
        var fault = root.AddComponent<FaultDisplay>();
        var settings = root.AddComponent<SettingsDisplay>();

        display.SerialService = serial;
        animation.DisplayController = display;
        animation.SerialService = serial;
        result.DisplayController = display;
        result.SerialService = serial;
        payout.DisplayController = display;
        fault.DisplayController = display;
        settings.DisplayController = display;

        try
        {
            Require(display.ProcessRawLine(
                "SHOW_BETTING:seq=21,round=2,balance=600,total_bet=100,selected=4," +
                "bets=0|0|0|100|0|0,odds=24|24|8|6|3|3"),
                "SHOW_BETTING failed");
            Require(display.CurrentPage == DisplayPage.Betting, "Betting mode was not selected");
            Require(display.Balance == 600 && display.TotalBet == 100, "Betting authority values changed");
            Require(display.SelectedId == 4 && display.Bets[3] == 100, "Selected animal changed");

            Require(display.ProcessRawLine(
                "SHOW_RACE:seq=30,round=2,winner=4,duration_ms=15000"),
                "SHOW_RACE failed");
            Require(display.CurrentPage == DisplayPage.Race, "Race mode was not selected");
            Require(display.WinnerId == 4 && display.RaceDurationMilliseconds == 15000,
                "Race command values changed");

            float winnerPosition = AnimalRaceAnimationController.EvaluatePosition(4, 4, 1f);
            Require(
                AnimalRaceUI.RacePositionToScreenX(0f) > AnimalRaceUI.RacePositionToScreenX(1f),
                "Race layout is not moving from the right-side start toward the left finish");
            Require(!AnimalRaceUI.IsFinishVisible(0.54f), "Finish line became visible too early");
            Require(AnimalRaceUI.IsFinishVisible(0.60f), "Finish line did not enter during the late race");
            Require(
                AnimalRaceUI.GetFinishLineX(1f) > AnimalRaceUI.GetFinishLineX(0.60f),
                "Fixed finish line did not pass behind the tracked animals");
            for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
            {
                if (animalId != 4)
                {
                    Require(
                        winnerPosition > AnimalRaceAnimationController.EvaluatePosition(animalId, 4, 1f),
                        "Specified winner did not finish first");
                }
            }

            Require(display.ProcessRawLine(
                "SHOW_RESULT:seq=31,round=2,result=WIN,winner=4,odds=6," +
                "win_points=600,balance=1100,duration_ms=3000"),
                "SHOW_RESULT failed");
            Require(result.IsVisible, "Result overlay was not selected");
            Require(display.ResultDurationMilliseconds == 3000,
                "H743 result duration was not applied");
            Require(result.Detail.Contains("尚未加入 CREDIT"), "V3.1 WIN result copy is missing");
            Require(!result.Detail.Contains("自动出币"), "Result incorrectly claims automatic payout");

            Require(display.ProcessRawLine(
                "SHOW_PAYOUT:seq=40,balance_before=1100,coins=11,remainder=0"),
                "SHOW_PAYOUT failed");
            Require(payout.IsVisible && !display.PayoutComplete, "Payout started as already complete");
            Require(payout.Headline == "正在出币", "Payout progress copy is incorrect");

            Require(display.ProcessRawLine(
                "SHOW_PAYOUT_DONE:seq=41,coins=11,balance=0"),
                "SHOW_PAYOUT_DONE failed");
            Require(display.PayoutComplete && payout.Headline == "出币完成",
                "Payout completion was not applied");

            Require(display.ProcessRawLine(
                "SHOW_FAULT:seq=50,code=3,detail=coin_sensor_timeout"),
                "SHOW_FAULT failed");
            Require(fault.IsVisible && fault.Detail.Contains("出币传感器等待超时"),
                "Fault overlay copy is incorrect");

            Require(display.ProcessRawLine(
                "SHOW_SETTING:seq=60,index=1,total=20,item=RACE_DURATION,value=15,min=10,max=30,unit=SECOND"),
                "SHOW_SETTING failed");
            Require(settings.IsVisible && settings.ItemName == "比赛时间",
                "Settings overlay was not selected");

            Require(display.ProcessRawLine(
                "SHOW_IDLE:seq=61,round=2,balance=1100,last_win=600"),
                "H743 SHOW_IDLE format failed");
            Require(display.CurrentPage == DisplayPage.Idle && display.WinTotalPoints == 600,
                "H743 idle state was not applied");

            Require(display.ProcessRawLine(
                "SHOW_SETTING:seq=62,index=1,total=16,item=RACE_DURATION_MS," +
                "value=15000,min=5000,max=60000"),
                "H743 SHOW_SETTING format failed");
            Require(settings.IsVisible && settings.ItemName == "比赛时间",
                "H743 setting item was not applied");

            Require(display.ProcessRawLine(
                "SHOW_PAYOUT:seq=63,balance_before=1100,coins=11,remainder=0"),
                "H743 SHOW_PAYOUT format failed");
            Require(payout.IsVisible && display.PayoutCoins == 11,
                "H743 payout state was not applied");

            Require(display.ProcessRawLine(
                "SHOW_PAYOUT_DONE:seq=64,coins=11,balance=0"),
                "H743 SHOW_PAYOUT_DONE format failed");
            Require(display.PayoutComplete && display.Balance == 0,
                "H743 payout completion was not applied");

            Require(display.ProcessRawLine(
                "FULL_STATE:seq=65,state=BETTING,round=2,balance=500,total_bet=100,selected=4," +
                "bets=0|0|0|100|0|0,odds=24|24|8|6|3|3,pending_payout=0,fault=0"),
                "FULL_STATE restore failed");
            Require(display.CurrentPage == DisplayPage.Betting && !settings.IsVisible,
                "Main game did not restore after settings");
            Require(root.scene == display.gameObject.scene && root.scene == animation.gameObject.scene,
                "Step 5 components do not share one Unity scene");
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
            throw new Exception("STEP5 TEST FAILED: " + message);
        }
    }
}

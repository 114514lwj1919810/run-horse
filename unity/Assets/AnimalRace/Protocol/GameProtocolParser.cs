using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class GameProtocolParser
{
    public bool TryParse(string rawLine, out ProtocolMessage message, out string error)
    {
        message = null;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            error = "Protocol line is empty";
            return false;
        }

        string line = rawLine.Trim();
        if (string.Equals(line, "hello", StringComparison.Ordinal))
        {
            message = new HelloMessage(line, 0, "current-h743", "3.1-compatible");
            return true;
        }

        int separatorIndex = line.IndexOf(':');
        if (separatorIndex <= 0)
        {
            error = "Missing COMMAND separator: " + line;
            return false;
        }

        string command = line.Substring(0, separatorIndex).Trim().ToUpperInvariant();
        Dictionary<string, string> fields;
        if (!TryParseFields(line.Substring(separatorIndex + 1), out fields, out error))
        {
            return false;
        }

        if (command == "F")
        {
            string[] shortKeys = {"s","p","r","c","t","i","b","o","w","wt","wr","l","rs","es","cs","ca","pp","f","pt","pd","rt","et","ct"};
            string[] longKeys = {"seq","state","round","credit","total_bet","selected","bets","odds","winner","win_total","win_remaining","last_win","race_seq","result_seq","collect_seq","collect_active","pending_payout","fault","payout_target","payout_done","race_remaining_ms","result_remaining_ms","collect_remaining_ms"};
            var expanded = new Dictionary<string,string>(StringComparer.Ordinal);
            for (int i=0;i<shortKeys.Length;i++) {
                string value;
                if (!fields.TryGetValue(shortKeys[i], out value)) { error="Incomplete compact state: "+shortKeys[i]; return false; }
                expanded.Add(longKeys[i],value);
            }
            fields=expanded; command="FULL_STATE";
        }

        switch (command)
        {
            case "HELLO": return TryParseHello(line, fields, out message, out error);
            case "FULL_STATE": return TryParseFullState(line, fields, out message, out error);
            case "SHOW_IDLE": return TryParseShowIdle(line, fields, out message, out error);
            case "SHOW_BETTING": return TryParseShowBetting(line, fields, out message, out error);
            case "BALANCE_UPDATE": return TryParseBalanceUpdate(line, fields, out message, out error);
            case "SHOW_RACE": return TryParseShowRace(line, fields, out message, out error);
            case "SHOW_RESULT": return TryParseShowResult(line, fields, out message, out error);
            case "SHOW_WIN_READY": return TryParseShowWinReady(line, fields, out message, out error);
            case "WIN_TRANSFER_UPDATE": return TryParseWinTransferUpdate(line, fields, out message, out error);
            case "WIN_TRANSFER_DONE": return TryParseWinTransferDone(line, fields, out message, out error);
            case "SHOW_SETTING": return TryParseShowSetting(line, fields, out message, out error);
            case "SHOW_PAYOUT": return TryParseShowPayout(line, fields, out message, out error);
            case "SHOW_PAYOUT_PROGRESS":
            case "PAYOUT_PROGRESS": return TryParseShowPayoutProgress(line, fields, out message, out error);
            case "SHOW_PAYOUT_DONE": return TryParseShowPayoutDone(line, fields, out message, out error);
            case "SHOW_FAULT": return TryParseShowFault(line, fields, out message, out error);
            case "PING": return TryParseLink(line, fields, ProtocolCommand.Ping, out message, out error);
            case "PONG": return TryParseLink(line, fields, ProtocolCommand.Pong, out message, out error);
            default:
                error = "Unknown command: " + command;
                return false;
        }
    }

    private static bool TryParseFields(string fieldText, out Dictionary<string, string> fields, out string error)
    {
        fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(fieldText))
        {
            error = "Command has no fields";
            return false;
        }

        string[] tokens = fieldText.Split(',');
        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index].Trim();
            int equalsIndex = token.IndexOf('=');
            if (equalsIndex <= 0)
            {
                error = "Invalid key=value field: " + token;
                return false;
            }

            string key = token.Substring(0, equalsIndex).Trim();
            string value = token.Substring(equalsIndex + 1).Trim();
            if (key.Length == 0 || value.Length == 0)
            {
                error = "Empty key or value: " + token;
                return false;
            }
            if (fields.ContainsKey(key))
            {
                error = "Duplicate field: " + key;
                return false;
            }
            fields.Add(key, value);
        }
        return true;
    }

    private static bool TryParseHello(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        int deviceId;
        string firmware;
        string protocol;
        if (!TryGetInt(fields, "device", out deviceId, out error) ||
            !TryGetString(fields, "fw", out firmware, out error) ||
            !TryGetString(fields, "protocol", out protocol, out error))
        {
            return false;
        }
        message = new HelloMessage(rawLine, deviceId, firmware, protocol);
        return true;
    }

    private static bool TryParseFullState(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        string phase;
        int credit;
        int totalBet;
        int selectedId;
        int[] bets;
        int[] odds;

        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetEitherString(fields, "phase", "state", out phase, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error) ||
            !TryGetNonNegativeInt(fields, "total_bet", out totalBet, out error) ||
            !TryGetAnimalId(fields, "selected", out selectedId, out error) ||
            !TryGetSixValues(fields, "bets", true, out bets, out error) ||
            !TryGetSixValues(fields, "odds", false, out odds, out error))
        {
            return false;
        }

        string mode = GetOptionalString(fields, "mode", InferModeFromLegacyState(phase));
        string resumePhase = GetOptionalString(fields, "resume_phase", phase);
        int winner = GetOptionalInt(fields, "winner", 0);
        int winTotal = GetOptionalInt(fields, "win_total", 0);
        int winRemaining = GetOptionalInt(fields, "win_remaining", GetOptionalInt(fields, "win", winTotal));
        int lastWin = GetOptionalInt(fields, "last_win", 0);
        uint raceSequence = GetOptionalUInt(fields, "race_seq", 0U);
        int raceRemaining = GetOptionalInt(fields, "race_remaining_ms", 0);
        uint resultSequence = GetOptionalUInt(fields, "result_seq", 0U);
        int resultRemaining = GetOptionalInt(fields, "result_remaining_ms", 0);
        uint collectSequence = GetOptionalUInt(fields, "collect_seq", 0U);
        bool collectActive = GetOptionalInt(fields, "collect_active", 0) != 0;
        int collectRemaining = GetOptionalInt(fields, "collect_remaining_ms", 0);
        int payoutDone = GetOptionalInt(fields, "payout_done", 0);
        int payoutTarget = fields.ContainsKey("payout_target")
            ? GetOptionalInt(fields, "payout_target", 0)
            : GetOptionalInt(fields, "pending_payout", 0) + payoutDone;
        int settingIndex = GetOptionalInt(fields, "setting_index", 0);
        int fault = GetOptionalInt(fields, "fault", 0);

        message = new FullStateMessage(
            rawLine, sequence, mode, phase, resumePhase, round, credit, totalBet,
            selectedId, bets, odds, winner, winTotal, winRemaining, lastWin,
            raceSequence, raceRemaining, resultSequence, resultRemaining,
            collectSequence, collectActive, collectRemaining, payoutTarget,
            payoutDone, settingIndex, fault);
        return true;
    }

    private static bool TryParseShowIdle(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int credit;
        int lastWin;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error) ||
            !TryGetNonNegativeInt(fields, "last_win", out lastWin, out error))
        {
            return false;
        }
        message = new ShowIdleMessage(rawLine, sequence, round, credit, lastWin, GetOptionalInt(fields, "win", 0));
        return true;
    }

    private static bool TryParseShowBetting(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int credit;
        int totalBet;
        int selectedId;
        int[] bets;
        int[] odds;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error) ||
            !TryGetNonNegativeInt(fields, "total_bet", out totalBet, out error) ||
            !TryGetAnimalId(fields, "selected", out selectedId, out error) ||
            !TryGetSixValues(fields, "bets", true, out bets, out error) ||
            !TryGetSixValues(fields, "odds", false, out odds, out error))
        {
            return false;
        }
        message = new ShowBettingMessage(rawLine, sequence, round, credit, totalBet, selectedId, bets, odds);
        return true;
    }

    private static bool TryParseBalanceUpdate(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int credit;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error))
        {
            return false;
        }
        round = GetOptionalUInt(fields, "round", 0U);
        message = new BalanceUpdateMessage(rawLine, sequence, round, credit,
            GetOptionalInt(fields, "win", 0), GetOptionalInt(fields, "delta", 0),
            GetOptionalString(fields, "source", "UNKNOWN"));
        return true;
    }

    private static bool TryParseShowRace(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int winner;
        int duration;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetAnimalId(fields, "winner", out winner, out error) ||
            !TryGetPositiveInt(fields, "duration_ms", out duration, out error))
        {
            return false;
        }
        int[] bets = GetOptionalSixValues(fields, "bets", true, new int[AnimalCatalog.Count]);
        int[] odds = GetOptionalSixValues(fields, "odds", false, new[] { 24, 24, 8, 6, 3, 3 });
        message = new ShowRaceMessage(rawLine, sequence, round, winner, duration, bets, odds,
            GetOptionalInt(fields, "demo", 0) != 0);
        return true;
    }

    private static bool TryParseShowResult(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        string result;
        int winner;
        int odds;
        int winTotal;
        int credit;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetString(fields, "result", out result, out error) ||
            !TryGetAnimalId(fields, "winner", out winner, out error) ||
            !TryGetPositiveInt(fields, "odds", out odds, out error) ||
            !TryGetEitherNonNegativeInt(fields, "win_total", "win_points", out winTotal, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error))
        {
            return false;
        }
        result = result.ToUpperInvariant();
        if (result != "WIN" && result != "LOSE")
        {
            error = "Field result must be WIN or LOSE";
            return false;
        }
        int remaining = GetOptionalInt(fields, "win_remaining", winTotal);
        int duration = GetOptionalInt(fields, "duration_ms", 3500);
        if (duration <= 0)
        {
            error = "Field duration_ms must be positive";
            return false;
        }
        message = new ShowResultMessage(rawLine, sequence, round, result, winner, odds, winTotal, remaining, credit, duration,
            GetOptionalInt(fields, "demo", 0) != 0);
        return true;
    }

    private static bool TryParseShowWinReady(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int win;
        int credit;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetNonNegativeInt(fields, "win", out win, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error))
        {
            return false;
        }
        message = new ShowWinReadyMessage(rawLine, sequence, round, win, credit);
        return true;
    }

    private static bool TryParseWinTransferUpdate(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int win;
        int credit;
        int delta;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetNonNegativeInt(fields, "win", out win, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error) ||
            !TryGetNonNegativeInt(fields, "delta", out delta, out error))
        {
            return false;
        }
        message = new WinTransferUpdateMessage(rawLine, sequence, round, win, credit, delta,
            GetOptionalString(fields, "source", "WIN_COLLECT"));
        return true;
    }

    private static bool TryParseWinTransferDone(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        uint round;
        int win;
        int credit;
        int fast;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetUInt(fields, "round", out round, out error) ||
            !TryGetNonNegativeInt(fields, "win", out win, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error) ||
            !TryGetNonNegativeInt(fields, "fast", out fast, out error))
        {
            return false;
        }
        message = new WinTransferDoneMessage(rawLine, sequence, round, win, credit, fast != 0);
        return true;
    }

    private static bool TryParseShowSetting(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        string item;
        string value;
        string minimum;
        string maximum;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetString(fields, "item", out item, out error) ||
            !TryGetString(fields, "value", out value, out error) ||
            !TryGetString(fields, "min", out minimum, out error) ||
            !TryGetString(fields, "max", out maximum, out error))
        {
            return false;
        }
        message = new ShowSettingMessage(rawLine, sequence,
            GetOptionalInt(fields, "index", 0), GetOptionalInt(fields, "total", 0),
            item, value, minimum, maximum, GetOptionalString(fields, "resume_phase", string.Empty));
        return true;
    }

    private static bool TryParseShowPayout(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        int balanceBefore;
        int coins;
        int remainder;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetNonNegativeInt(fields, "balance_before", out balanceBefore, out error) ||
            !TryGetNonNegativeInt(fields, "coins", out coins, out error) ||
            !TryGetNonNegativeInt(fields, "remainder", out remainder, out error))
        {
            return false;
        }
        message = new ShowPayoutMessage(rawLine, sequence, balanceBefore, coins, remainder);
        return true;
    }

    private static bool TryParseShowPayoutProgress(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        int target;
        int done;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetNonNegativeInt(fields, "target", out target, out error) ||
            !TryGetNonNegativeInt(fields, "done", out done, out error))
        {
            return false;
        }
        message = new ShowPayoutProgressMessage(rawLine, sequence, target, done,
            GetOptionalInt(fields, "remainder", Math.Max(0, target - done)));
        return true;
    }

    private static bool TryParseShowPayoutDone(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        int coins;
        int credit;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetNonNegativeInt(fields, "coins", out coins, out error) ||
            !TryGetEitherNonNegativeInt(fields, "credit", "balance", out credit, out error))
        {
            return false;
        }
        message = new ShowPayoutDoneMessage(rawLine, sequence, coins, credit);
        return true;
    }

    private static bool TryParseShowFault(string rawLine, Dictionary<string, string> fields, out ProtocolMessage message, out string error)
    {
        message = null;
        uint sequence;
        int code;
        string detail;
        if (!TryGetUInt(fields, "seq", out sequence, out error) ||
            !TryGetNonNegativeInt(fields, "code", out code, out error) ||
            !TryGetString(fields, "detail", out detail, out error))
        {
            return false;
        }
        message = new ShowFaultMessage(rawLine, sequence, code, detail,
            GetOptionalInt(fields, "recoverable", 0) != 0);
        return true;
    }

    private static bool TryParseLink(string rawLine, Dictionary<string, string> fields, ProtocolCommand command, out ProtocolMessage message, out string error)
    {
        error = string.Empty;
        message = new LinkMessage(command, rawLine, GetOptionalUInt(fields, "seq", 0U));
        return true;
    }

    private static bool TryGetString(Dictionary<string, string> fields, string key, out string value, out string error)
    {
        if (!fields.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
        {
            error = "Missing field: " + key;
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static bool TryGetEitherString(Dictionary<string, string> fields, string primary, string legacy, out string value, out string error)
    {
        if (fields.ContainsKey(primary))
        {
            return TryGetString(fields, primary, out value, out error);
        }
        return TryGetString(fields, legacy, out value, out error);
    }

    private static bool TryGetInt(Dictionary<string, string> fields, string key, out int value, out string error)
    {
        string text;
        if (!TryGetString(fields, key, out text, out error))
        {
            value = 0;
            return false;
        }
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = "Field " + key + " is not an integer: " + text;
            return false;
        }
        return true;
    }

    private static bool TryGetUInt(Dictionary<string, string> fields, string key, out uint value, out string error)
    {
        string text;
        if (!TryGetString(fields, key, out text, out error))
        {
            value = 0U;
            return false;
        }
        if (!uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = "Field " + key + " is not an unsigned integer: " + text;
            return false;
        }
        return true;
    }

    private static bool TryGetNonNegativeInt(Dictionary<string, string> fields, string key, out int value, out string error)
    {
        if (!TryGetInt(fields, key, out value, out error))
        {
            return false;
        }
        if (value < 0)
        {
            error = "Field " + key + " must be non-negative";
            return false;
        }
        return true;
    }

    private static bool TryGetEitherNonNegativeInt(Dictionary<string, string> fields, string primary, string legacy, out int value, out string error)
    {
        if (fields.ContainsKey(primary))
        {
            return TryGetNonNegativeInt(fields, primary, out value, out error);
        }
        return TryGetNonNegativeInt(fields, legacy, out value, out error);
    }

    private static bool TryGetPositiveInt(Dictionary<string, string> fields, string key, out int value, out string error)
    {
        if (!TryGetInt(fields, key, out value, out error))
        {
            return false;
        }
        if (value <= 0)
        {
            error = "Field " + key + " must be positive";
            return false;
        }
        return true;
    }

    private static bool TryGetAnimalId(Dictionary<string, string> fields, string key, out int value, out string error)
    {
        if (!TryGetInt(fields, key, out value, out error))
        {
            return false;
        }
        if (!AnimalCatalog.IsValidId(value))
        {
            error = "Field " + key + " must be between 1 and " + AnimalCatalog.Count;
            return false;
        }
        return true;
    }

    private static bool TryGetSixValues(Dictionary<string, string> fields, string key, bool allowZero, out int[] values, out string error)
    {
        values = null;
        string text;
        if (!TryGetString(fields, key, out text, out error))
        {
            return false;
        }
        string[] parts = text.Split('|');
        if (parts.Length != AnimalCatalog.Count)
        {
            error = "Field " + key + " must contain six values";
            return false;
        }
        values = new int[AnimalCatalog.Count];
        for (int index = 0; index < parts.Length; index++)
        {
            int parsed;
            if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ||
                parsed < 0 || (!allowZero && parsed == 0))
            {
                error = "Field " + key + " contains an invalid value: " + parts[index];
                return false;
            }
            values[index] = parsed;
        }
        return true;
    }

    private static int[] GetOptionalSixValues(Dictionary<string, string> fields, string key, bool allowZero, int[] fallback)
    {
        if (!fields.ContainsKey(key))
        {
            return (int[])fallback.Clone();
        }
        int[] values;
        string ignored;
        return TryGetSixValues(fields, key, allowZero, out values, out ignored)
            ? values
            : (int[])fallback.Clone();
    }

    private static string GetOptionalString(Dictionary<string, string> fields, string key, string fallback)
    {
        string value;
        return fields.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string InferModeFromLegacyState(string state)
    {
        string upper = (state ?? string.Empty).ToUpperInvariant();
        if (upper == "SETTING") return "SETTING";
        if (upper == "FAULT") return "FAULT";
        return "RUN";
    }

    private static int GetOptionalInt(Dictionary<string, string> fields, string key, int fallback)
    {
        string value;
        int parsed;
        return fields.TryGetValue(key, out value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) &&
               parsed >= 0
            ? parsed
            : fallback;
    }

    private static uint GetOptionalUInt(Dictionary<string, string> fields, string key, uint fallback)
    {
        string value;
        uint parsed;
        return fields.TryGetValue(key, out value) &&
               uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : fallback;
    }
}

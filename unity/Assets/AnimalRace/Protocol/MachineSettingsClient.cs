using System;
using System.Collections.Generic;
using System.Globalization;

// V3.2 management frames are independent of animation sequence numbers.
public sealed class MachineSettingsClient
{
    public const int Count = 37;
    public readonly int[] Values = new int[Count];
    public Dictionary<string, ulong> Account = new Dictionary<string, ulong>();
    public bool HasSettings { get; private set; }
    public bool HasAccount { get; private set; }
    public bool StorageReady { get; private set; }
    public uint Revision { get; private set; }
    public string Error { get; private set; }
    public bool Busy { get { return pending != null; } }
    public event Action SettingsChanged;
    public event Action<uint, string> Result;
    public event Action<string> MenuKey;
    public event Action<bool> ServiceState;
    public event Action Disconnected;
    public Func<string, bool> Send;
    private uint nextRequest = (uint)(DateTime.UtcNow.Ticks & 0x7fffffff);
    private uint pendingId;
    private string pending;
    private double sentAt;
    private int attempts;
    private static readonly string[] AccountKeys = { "coin_in", "coin_out", "in_value", "out_value", "committed", "bets", "rounds", "jackpots", "tickets", "seconds", "reports", "report_in", "report_out", "report_in_value", "report_out_value", "pace", "big_debt", "credit", "win", "pending" };

    public void Reset()
    {
        HasSettings = HasAccount = StorageReady = false;
        pending = null; Error = "等待设备同步";
        Revision=0;
        if (Disconnected != null) Disconnected();
    }
    public uint Request(string command, string suffix)
    {
        if (Busy) { Error = "正在等待设备确认"; return 0; }
        if (++nextRequest == 0) nextRequest = 1;
        string line = command + ":req=" + nextRequest.ToString(CultureInfo.InvariantCulture) + suffix;
        if (Send == null || !Send(line)) { Error = "设备未连接"; return 0; }
        pendingId = nextRequest; pending = line; attempts = 1; sentAt = Now(); Error = "等待设备确认";
        return pendingId;
    }
    public uint Apply(int[] values, uint revision)
    {
        if (!HasSettings || !StorageReady || !Validate(values)) { Error = "设备参数或存储未就绪"; return 0; }
        string[] parts = new string[Count];
        for (int i = 0; i < Count; i++) parts[i] = values[i].ToString(CultureInfo.InvariantCulture);
        return Request("SETTINGS_APPLY", ",base_rev=" + revision + ",values=" + string.Join("|", parts));
    }
    public void Tick()
    {
        if (pending == null || Now() - sentAt < 2) return;
        sentAt = Now();
        if (attempts++ >= 4) {
            uint id = pendingId; pending = null; Error = "确认超时，请重新读取设备参数";
            if (Send != null) Send("SETTINGS_GET");
            if (Result != null) Result(id, "TIMEOUT");
        } else if (Send != null) Send(pending);
    }
    private static double Now() { return DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond; }
    public static bool IsFrame(string line)
    {
        return line != null && (line.StartsWith("SETTINGS_SNAPSHOT:", StringComparison.Ordinal) ||
            line.StartsWith("ACCOUNT_SNAPSHOT:", StringComparison.Ordinal) || line.StartsWith("CONTROL_RESULT:", StringComparison.Ordinal) ||
            line.StartsWith("MENU_KEY:", StringComparison.Ordinal) || line.StartsWith("SERVICE_STATE:", StringComparison.Ordinal) || line.StartsWith("A:", StringComparison.Ordinal));
    }
    private static ulong Unsigned(Dictionary<string, string> f, string key)
    {
        string text; ulong n;
        if (!f.TryGetValue(key, out text) || !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out n))
            throw new FormatException("Missing or invalid " + key);
        return n;
    }
    public bool Process(string line)
    {
        try {
            if (string.IsNullOrEmpty(line) || line.Length > 1024 || line.IndexOf(':')<=0) throw new FormatException("Invalid frame");
            int colon = line.IndexOf(':'); string command = line.Substring(0, colon);
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string pair in line.Substring(colon + 1).TrimEnd('\r', '\n').Split(',')) {
                int equals = pair.IndexOf('=');
                if (equals <= 0 || equals == pair.Length - 1) throw new FormatException("Invalid field");
                fields.Add(pair.Substring(0, equals), pair.Substring(equals + 1));
            }
            if (command == "A") {
                if (Unsigned(fields,"v")!=1) throw new FormatException("Unsupported account schema");
                string[] values=fields["d"].Split('|');
                if (values.Length!=AccountKeys.Length) throw new FormatException("Incomplete account");
                fields=new Dictionary<string,string>();
                for(int i=0;i<values.Length;i++) fields.Add(AccountKeys[i],values[i]);
                command="ACCOUNT_SNAPSHOT";
            }
            if (command == "SETTINGS_SNAPSHOT") {
                if (Unsigned(fields, "schema") != 1) throw new FormatException("Unsupported settings schema");
                uint rev = checked((uint)Unsigned(fields, "rev"));
                ulong storage = Unsigned(fields, "storage"); if (storage > 1) throw new FormatException("Invalid storage state");
                string[] parts = fields["values"].Split('|'); if (parts.Length != Count) throw new FormatException("Incomplete settings");
                var values = new int[Count];
                for (int i = 0; i < Count; i++) if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out values[i])) throw new FormatException("Invalid setting");
                if (!Validate(values)) throw new FormatException("Settings outside schema limits");
                Array.Copy(values, Values, Count); Revision = rev; StorageReady = storage == 1; HasSettings = true;
                Error = StorageReady ? string.Empty : "设备存储未就绪";
                if (SettingsChanged != null) SettingsChanged();
            } else if (command == "ACCOUNT_SNAPSHOT") {
                var account = new Dictionary<string, ulong>();
                foreach (string key in AccountKeys)
                    account.Add(key, Unsigned(fields, key));
                Account = account; HasAccount = true;
            } else if (command == "CONTROL_RESULT") {
                uint id = checked((uint)Unsigned(fields, "req")); string status = fields["status"];
                if(Unsigned(fields,"rev")>uint.MaxValue || Array.IndexOf(new[]{"OK","STORAGE_ERROR","INVALID_VALUE","BALANCE_NOT_EMPTY","BUSY","AUTH_REQUIRED","CONFLICT"},status)<0)
                    throw new FormatException("Invalid control result");
                if (pending == null || id != pendingId) return true;
                pending = null; Error = status == "OK" ? string.Empty : Translate(status);
                if (Result != null) Result(id, status);
            } else if (command == "MENU_KEY") {
                string key = fields["key"];
                if (Array.IndexOf(new[] { "UP", "DOWN", "LEFT", "RIGHT", "CONFIRM", "BACK", "NONE" }, key) < 0) throw new FormatException("Invalid menu key");
                if (MenuKey != null) MenuKey(key);
            } else if (command == "SERVICE_STATE") {
                ulong open = Unsigned(fields, "open"); if (open > 1) throw new FormatException("Invalid service state");
                if (ServiceState != null) ServiceState(open == 1);
            } else return false;
            return true;
        } catch (Exception e) {
            if (!(e is FormatException) && !(e is OverflowException) && !(e is ArgumentException) && !(e is KeyNotFoundException)) throw;
            Error = "设备数据无效: " + e.Message; return false;
        }
    }
    public static bool Validate(int[] v)
    {
        if (v == null || v.Length != Count) return false;
        int[] low = {5000,1000,1,1,100,1,0,1,1,1,1,1,1,0,1,0,1,0,0,100,100,1,0,1,0,0,0,0,0,0,0,0,100,0,0,0,0};
        int[] high = {60000,10000,100,100,60000,1000,10000,99,99,99,99,99,99,16,99999999,1,5,30,6,10000,10000,99,1,8,16,1,1,1,1,99900,99999999,99999999,1000000,1000,27,255,255};
        for (int i = 0; i < Count; i++) if (v[i] < low[i] || v[i] > high[i]) return false;
        return Array.IndexOf(new[]{1,2,5,10,20,50,100},v[2])>=0 && Array.IndexOf(new[]{1,5,10,20,50,100},v[3])>=0 &&
            v[19]%100==0 && v[20]%100==0 && (v[35]<28 || v[35]==255) && (v[36]<16 || v[36]==255) &&
            ((v[35]==255)==(v[36]==255)) && v[34]!=v[35];
    }
    public static string Translate(string status)
    {
        switch (status) {
        case "STORAGE_ERROR": return "EEPROM保存失败";
        case "INVALID_VALUE": return "参数组合无效";
        case "BALANCE_NOT_EMPTY": return "余额清空后才能修改币值和计数端口";
        case "BUSY": return "请等待本局结束";
        case "AUTH_REQUIRED": return "请先验证系统密码";
        case "CONFLICT": return "参数已变化，请重新读取";
        default: return status;
        }
    }
}

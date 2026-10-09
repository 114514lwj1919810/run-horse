using System;
using System.IO;
using System.Linq;
using System.Globalization;

public static class ProtocolInteropTests
{
    private static void Check(bool ok,string text) { if(!ok) throw new Exception(text); }
    public static void Main(string[] args)
    {
        var f=File.ReadAllLines(args[0]); var c=new MachineSettingsClient();
        Check(c.Process(f[0]) && c.HasSettings && c.Values.Length==37,"37 settings parsed from production C");
        Check(c.Values[31]==0 && c.Values[6]==9500,"masked password/target");
        Check(c.Process(f[1]) && c.Account.Count==20,"20 account fields");
        var p=new GameProtocolParser(); ProtocolMessage m; string error;
        Check(p.TryParse(f[2],out m,out error),error); Check(m is FullStateMessage,"F routing");
        Check(c.Process(f[3]),"maximum settings values");
        Check(c.Values[0]==60000 && c.Values[14]==99999999 && c.Values[36]==255,"all settings width");
        Check(p.TryParse(f[4],out m,out error),error); Check(m is ShowRaceMessage,"locked race");
        Check(p.TryParse(f[5],out m,out error),error); Check(m is ShowResultMessage,"locked result");
        Check(!c.Process(f[0]+",rev=2"),"duplicate field");
        Check(!c.Process(f[0].Replace("schema=1","schema=2")),"unknown schema");
        Check(!c.Process("A:v=1,d=1|2"),"incomplete account");
        Check(!p.TryParse("F:s=1,p=IDLE",out m,out error),"incomplete F");
        Check(c.Process(f[0]),"reload default");
        string sent=null; c.Send=s=>{sent=s;return true;}; uint req=c.Apply(c.Values,c.Revision);
        Check(req!=0 && c.Busy && sent.Contains("base_rev=1"),"versioned apply");
        Check(c.Process("CONTROL_RESULT:req="+(req+1)+",status=OK,rev=2") && c.Busy,"unrelated ack");
        Check(!c.Process("CONTROL_RESULT:req="+req+",status=OK") && c.Busy,"incomplete ack");
        Check(c.Process("CONTROL_RESULT:req="+req+",status=OK,rev=2") && !c.Busy,"matching ack");
        string maxAccount="A:v=1,d="+string.Join("|",Enumerable.Repeat(ulong.MaxValue.ToString(CultureInfo.InvariantCulture),20).ToArray());
        Check(c.Process(maxAccount) && c.Account["in_value"]==ulong.MaxValue,"64-bit account preserved");
        Check(maxAccount.Length<1024,"maximum account frame bounded");
        var b=new SerialLineBuffer(); string packed=string.Join("\r\n",f)+"\r\n"+maxAccount+"\r\n";
        for(int i=0;i<packed.Length;i+=7) b.Append(packed.Substring(i,Math.Min(7,packed.Length-i)));
        string line; int count=0; while(b.TryReadLine(out line)) count++;
        Check(count==f.Length+1 && b.DroppedLineCount==0,"split and joined frames");
        b.Append(new string('x',1025)+"\r\nhello\r\n");
        Check(b.DroppedLineCount==1 && b.TryReadLine(out line) && line=="hello","oversize recovery");
        bool reset=false;c.Disconnected+=()=>{reset=true;};c.Reset();
        Check(reset && !c.HasSettings && !c.HasAccount && !c.Busy,"reset stale settings");
        Console.WriteLine("PASS C -> C# fixtures: 37 settings, 20 accounts, F/race/result, maximum values, ACKs, reset, fragmented frames");
        Console.WriteLine("Default frame bytes: settings="+f[0].Length+", account="+f[1].Length+", state="+f[2].Length+"; maximum 64-bit account="+maxAccount.Length);
    }
}

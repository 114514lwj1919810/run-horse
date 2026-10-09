using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GD32SyncTests
{
    private static void Check(bool ok,string reason) { if(!ok) throw new Exception("GD32 V3.2: "+reason); }
    public static void RunBatch()
    {
        string file=Path.Combine(Application.dataPath,"AnimalRace/Tests/protocol-fixtures.txt");
        string[] frames=File.ReadAllLines(file);
        var client=new MachineSettingsClient();
        Check(client.Process(frames[0]),"C settings snapshot");
        Check(client.HasSettings && client.StorageReady && client.Values.Length==37,"37 settings");
        Check(client.Values[31]==0 && client.Values[6]==9500,"masked password and target RTP");
        Check(client.Process(frames[1]) && client.Account.Count==20,"20 real account fields");
        var parser=new GameProtocolParser(); ProtocolMessage message; string error;
        Check(parser.TryParse(frames[2],out message,out error),"compact C state: "+error);
        Check(message is FullStateMessage,"compact F decoded as full state");
        Check(!parser.TryParse("F:s=1,p=IDLE",out message,out error),"truncated F rejected");
        Check(!client.Process("A:v=1,d=1|2|3"),"truncated account rejected");
        Check(!client.Process(frames[0].Replace("schema=1","schema=2")),"schema rejected");
        Check(!client.Process(frames[0]+",rev=2"),"duplicate keys rejected");
        int[] original=(int[])client.Values.Clone();
        Check(!client.Process(frames[0].Replace("|255|255","|27|255")),"half-configured payout rejected");
        Check(client.Values[35]==original[35],"bad snapshot leaves authoritative values intact");
        string sent=null; uint completed=0;
        client.Send=value=>{sent=value; return true;};
        client.Result+=(id,status)=>{completed=id;};
        uint request=client.Apply(client.Values,client.Revision);
        Check(request!=0 && client.Busy && sent.StartsWith("SETTINGS_APPLY:"),"versioned write");
        Check(client.Process("CONTROL_RESULT:req="+(request+1)+",status=OK,rev=2") && client.Busy,"foreign acknowledgement ignored");
        Check(!client.Process("CONTROL_RESULT:req="+request+",status=OK"),"incomplete acknowledgement ignored");
        Check(client.Busy,"incomplete acknowledgement does not finish write");
        Check(client.Process("CONTROL_RESULT:req="+request+",status=OK,rev=2") && !client.Busy && completed==request,"matching acknowledgement");
        Check(client.Process(frames[0].Replace("rev=1","rev=2")) && client.Revision==2,"saved snapshot");
        bool disconnected=false; client.Disconnected+=()=>{disconnected=true;}; client.Reset();
        Check(disconnected && !client.HasSettings && !client.HasAccount && !client.Busy,"disconnect invalidates device state");
        var buffer=new SerialLineBuffer();
        string packed=string.Join("\r\n",frames)+"\r\n";
        for(int i=0;i<packed.Length;i+=7) buffer.Append(packed.Substring(i,Math.Min(7,packed.Length-i)));
        int count=0; string line; while(buffer.TryReadLine(out line)) count++;
        Check(count==frames.Length && buffer.DroppedLineCount==0,"fragmented UART fixtures");
        TestDisplay(frames);
        ProtocolStep2Tests.RunBatch();
        ProtocolStep4Tests.RunBatch();
        ProtocolV31Tests.RunBatch();
        RacePresentationTests.RunBatch();
        Debug.Log("GD32_V32_SYNC_TESTS_PASSED");
    }
    private static void TestDisplay(string[] frames)
    {
        var root=new GameObject("GD32SyncTest");
        try {
            var serial=root.AddComponent<AndroidSerialService>();
            var display=root.AddComponent<DisplayStateController>(); display.SerialService=serial;
            var menu=root.AddComponent<SettingsDisplay>(); menu.DisplayController=display;
            typeof(SettingsDisplay).GetMethod("BindDevice",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,null);
            Check(display.ProcessRawLine("hello") && display.ProcessRawLine(frames[2]),"handshake with compact F");
            Check(display.HandshakeComplete,"handshake complete");
            Check(display.ProcessRawLine(frames[0]) && display.ProcessRawLine(frames[1]),"management frames routed");
            Check(display.ProcessRawLine(frames[4]),"C race frame");
            uint raceSequence=display.CurrentSequence;
            Check(display.ProcessRawLine("BALANCE_UPDATE:seq=1000,round=1,credit=500,win=0,delta=10,source=COIN"),"coin during race");
            Check(display.CurrentSequence==raceSequence && display.Balance==500,"coin must not restart race");
            Check(display.ProcessRawLine(frames[2]),"restore idle");
            Check(display.ProcessRawLine("SERVICE_STATE:open=1") && menu.ShouldRender,"device menu open");
            Check(display.ProcessRawLine("MENU_KEY:key=DOWN"),"hardware menu key");
            display.Machine.Reset(); Check(!menu.ShouldRender,"disconnect closes stale local menu");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}

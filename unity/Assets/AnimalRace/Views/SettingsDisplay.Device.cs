using System;
using UnityEngine;

public sealed partial class SettingsDisplay
{
    private MachineSettingsClient boundDevice;
    private MachineSettingsClient Device { get { return displayController == null ? null : displayController.Machine; } }
    private int[] draft = new int[MachineSettingsClient.Count];
    private uint draftRevision, saveRequest, authRequest, reportRequest;
    private bool saveAccepted, authenticatedHere, reloadOnSnapshot;
    private SettingsPage returnPage;
    private int returnRow;
    private string deviceStatus = string.Empty;
    private static readonly int[] AdvancedFields = {6,17,5,32,33,34,35,36,0,1,4};
    private static readonly string[] AdvancedNames = {"目标返奖率","小奖节奏","退币积分/币","单局奖励上限","亏空降率","投币输入","出币光眼","出币电机","比赛时间(ms)","结果时间(ms)","单局押注上限"};
    private static readonly int[] AdvancedMin = {0,0,1,100,0,0,0,0,5000,1000,100};
    private static readonly int[] AdvancedMax = {10000,30,1000,1000000,1000,27,27,15,60000,10000,60000};
    private static readonly int[] AdvancedStep = {100,1,1,100,50,1,1,1,500,500,100};

    private void BindDevice()
    {
        if (Device == boundDevice) return;
        UnbindDevice(); boundDevice = Device;
        if (boundDevice == null) return;
        boundDevice.SettingsChanged += DeviceSettings;
        boundDevice.Result += DeviceResult;
        boundDevice.ServiceState += DeviceService;
        boundDevice.MenuKey += DeviceKey;
        boundDevice.Disconnected += DeviceDisconnected;
        boundDevice.Send = displayController.SendMachineCommand;
    }
    private void UnbindDevice()
    {
        if (boundDevice == null) return;
        boundDevice.SettingsChanged -= DeviceSettings; boundDevice.Result -= DeviceResult;
        boundDevice.ServiceState -= DeviceService; boundDevice.MenuKey -= DeviceKey;
        boundDevice.Disconnected -= DeviceDisconnected;
        boundDevice = null;
    }
    private void DeviceDisconnected()
    {
        saveRequest=authRequest=reportRequest=draftRevision=0;
        saveAccepted=authenticatedHere=reloadOnSnapshot=false;
        localMenuVisible=false; deviceStatus="等待设备重新同步";
    }
    private void DeviceService(bool open)
    {
        if (open) {
            if (!localMenuVisible) { LoadValues(); Open(SettingsPage.Root, 0); authenticatedHere = false; }
        } else { localMenuVisible = false; currentPage = SettingsPage.Root; selectedRow = 0; }
    }
    private void DeviceSettings()
    {
        if(Device==null || !Device.HasSettings) return;
        AudioListener.volume=Device.Values[Device.Values[22]==0?13:24]/16f;
        if(reloadOnSnapshot) { reloadOnSnapshot=false; LoadValues(); }
        if (saveRequest != 0 && saveAccepted && Device.Revision != draftRevision) {
            saveRequest = 0; saveAccepted = false; LoadValues(); deviceStatus = "已保存到设备"; Open(returnPage, returnRow);
        } else if (!localMenuVisible || draftRevision == 0) LoadValues();
    }
    private void DeviceResult(uint id, string status)
    {
        deviceStatus = status == "OK" ? string.Empty : MachineSettingsClient.Translate(status);
        if (id == saveRequest) {
            saveAccepted = status == "OK";
            if (!saveAccepted) { saveRequest = 0; if (status == "CONFLICT" || status == "TIMEOUT" || status == "STORAGE_ERROR") reloadOnSnapshot=true; }
            else DeviceSettings();
        }
        if (id == authRequest) {
            authRequest = 0; passwordError = status != "OK";
            if (!passwordError) { authenticatedHere = true; Open(SettingsPage.SystemSettings, 0); }
        }
        if (id == reportRequest) { reportRequest = 0; if(status=="OK") Open(SettingsPage.SingleMenu, 3); }
    }
    private void LoadValues()
    {
        if (Device == null || !Device.HasSettings) return;
        draft = (int[])Device.Values.Clone(); draftRevision = Device.Revision;
        machineMode=draft[22]; branchNumber=draft[23]; branchVolume=draft[24]; branchType=draft[25]; branchSource=draft[26];
        coinPointIndex=Array.IndexOf(CoinPoints,draft[2]); difficultyIndex=draft[16]-1; ticketCoins=draft[21];
        lionMaxBet=draft[19]; otherMaxBet=draft[20]; minimumBetIndex=Array.IndexOf(MinimumBets,draft[3]);
        singleVolume=draft[13]; multiplierIndex=draft[18]; singleType=draft[27]; singleSource=draft[28];
        runningMinutes=draft[29]; systemMachineNumber=draft[14]; systemFormula=draft[30]; systemPassword=0;
    }
    private void SaveAndReturn(SettingsPage page, int row)
    {
        if(Device==null || !Device.HasSettings) { deviceStatus="等待设备参数"; return; }
        draft[22]=machineMode; draft[23]=branchNumber; draft[24]=branchVolume; draft[25]=branchType; draft[26]=branchSource;
        draft[2]=CoinPoints[coinPointIndex]; draft[16]=difficultyIndex+1; draft[21]=ticketCoins;
        draft[19]=lionMaxBet; draft[20]=otherMaxBet; draft[3]=MinimumBets[minimumBetIndex];
        draft[13]=singleVolume; draft[18]=multiplierIndex; draft[27]=singleType; draft[28]=singleSource;
        draft[29]=runningMinutes; draft[14]=systemMachineNumber; draft[30]=systemFormula; draft[31]=systemPassword;
        if(!MachineSettingsClient.Validate(draft)) { deviceStatus="参数组合无效"; return; }
        returnPage=page; returnRow=row; saveAccepted=false;
        saveRequest=Device.Apply(draft,draftRevision);
        deviceStatus=saveRequest==0?Device.Error:"正在保存到设备";
    }
    private void RequestReport(bool clear)
    {
        if(Device==null || !Device.HasAccount) { deviceStatus="等待设备账目"; return; }
        if(!authenticatedHere) { deviceStatus="请先在系统设置验证密码"; OpenSystemPassword(); return; }
        reportRequest=Device.Request(clear?"ACCOUNT_CLEAR":"ACCOUNT_REPORT",",base_rev="+Device.Account["reports"]);
    }
    private void DeviceKey(string key)
    {
        if(!ShouldRender || Device==null || !Device.HasSettings || Device.Busy) return;
        if(currentPage==SettingsPage.SystemPassword) {
            if(key=="UP") ChangePasswordDigit(1); else if(key=="DOWN") ChangePasswordDigit(-1);
            else if(key=="LEFT") MovePasswordCursor(-1); else if(key=="RIGHT") MovePasswordCursor(1);
            else if(key=="CONFIRM") ConfirmPassword(); else if(key=="BACK") Open(SettingsPage.SingleMenu,4);
        } else if(currentPage==SettingsPage.SystemValueEditor) {
            if(key=="UP") ChangeEditorDigit(1); else if(key=="DOWN") ChangeEditorDigit(-1);
            else if(key=="LEFT") editorCursor=Mathf.Max(0,editorCursor-1); else if(key=="RIGHT") editorCursor=Mathf.Min(editorDigitCount-1,editorCursor+1);
            else if(key=="CONFIRM") ConfirmSystemValueEditor(); else if(key=="BACK") Open(SettingsPage.SystemSettings,(int)editorTarget+1);
        } else if(currentPage==SettingsPage.ReportConfirm) {
            if(key=="LEFT") reportYes=true; else if(key=="RIGHT") reportYes=false;
            else if(key=="CONFIRM") ConfirmReport(); else if(key=="BACK") Open(SettingsPage.SingleMenu,3);
        } else {
            if(key=="UP") MoveSelection(-1); else if(key=="DOWN") MoveSelection(1);
            else if(key=="LEFT") SelectAdjustmentDirection(-1); else if(key=="RIGHT") SelectAdjustmentDirection(1);
            else if(key=="CONFIRM") ConfirmSelection(); else if(key=="BACK") NavigateBack();
        }
    }
    private void AdjustAdvanced(int direction)
    {
        if(selectedRow>=AdvancedFields.Length) return;
        int field=AdvancedFields[selectedRow], value=draft[field];
        if(field==35 || field==36) {
            if(value==255) value=direction>0?0:AdvancedMax[selectedRow];
            else { value+=direction; if(value<0 || value>AdvancedMax[selectedRow]) value=255; }
            draft[field]=value;
        } else draft[field]=Step(value,direction,AdvancedMin[selectedRow],AdvancedMax[selectedRow],AdvancedStep[selectedRow]);
    }
    private void DrawAdvanced()
    {
        int start=(selectedRow/6)*6;
        for(int i=start;i<Mathf.Min(start+6,AdvancedFields.Length+2);i++) {
            float y=124+(i-start)*35;
            string label=i<AdvancedFields.Length?AdvancedNames[i]:(i==AdvancedFields.Length?"保存并退出":"不保存退出");
            DrawColored(new Rect(115,y,205,24),label,menuStyle,i==selectedRow?Cyan():Color.white);
            if(i==selectedRow) DrawCursor(80,y);
            if(i>=AdvancedFields.Length) continue;
            int field=AdvancedFields[i],v=draft[field];
            string value=v.ToString();
            if(field==6 || field==33) value=(v/100m).ToString("0.00")+"%";
            else if(field==17) value+="%";
            else if(field>=34) value=v==255?"未配置":(field==36?"OUT":"IN")+(v+1);
            DrawColored(new Rect(355,y,175,24),value,smallStyle,i==selectedRow?Cyan():Color.white);
        }
    }
    private void DrawDeviceStatus()
    {
        string status=deviceStatus;
        if(Device==null || !Device.HasSettings) status="等待GD32参数同步";
        else if(!Device.StorageReady) status="EEPROM未就绪，无法保存或开始新一局";
        else if(Device.Busy) status="等待GD32确认";
        if(!string.IsNullOrEmpty(status)) DrawColored(new Rect(72,369,500,24),status,smallStyle,Color.white);
    }
    private void DrawLiveAccount()
    {
        string[] names={"真实投币","实际吐币","累计币差","投入积分","已承诺返奖","实际退币积分","累计下注","比赛局数","大奖次数","运行分钟","报账次数","当前余额"};
        string[] values=new string[names.Length];
        for(int i=0;i<values.Length;i++) values[i]="--";
        if(Device!=null && Device.HasAccount) {
            var a=Device.Account;
            values=new[]{a["coin_in"].ToString(),a["coin_out"].ToString(),((decimal)a["coin_in"]-a["coin_out"]).ToString(),
                a["in_value"].ToString(),a["committed"].ToString(),a["out_value"].ToString(),a["bets"].ToString(),
                a["rounds"].ToString(),a["jackpots"].ToString(),(a["seconds"]/60).ToString(),a["reports"].ToString(),a["credit"].ToString()};
        }
        for(int i=0;i<names.Length;i++) {
            float x=80+(i/6)*260, y=122+(i%6)*30;
            DrawColored(new Rect(x,y,112,24),names[i],smallStyle,Cyan());
            var numericStyle=new GUIStyle(smallStyle);
            while(numericStyle.fontSize>9 && numericStyle.CalcSize(new GUIContent(values[i])).x>132) numericStyle.fontSize--;
            DrawColored(new Rect(x+110,y,140,24),values[i],numericStyle,Color.white);
        }
        DrawAccountExit(310);
    }
}

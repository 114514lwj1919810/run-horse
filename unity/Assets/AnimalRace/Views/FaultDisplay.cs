using UnityEngine;

public sealed class FaultDisplay : MonoBehaviour
{
    [SerializeField]
    private DisplayStateController displayController;

    public DisplayStateController DisplayController
    {
        get { return displayController; }
        set { displayController = value; }
    }

    public bool IsVisible
    {
        get { return displayController != null && displayController.CurrentPage == DisplayPage.Fault; }
    }

    public string Detail
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }

            string explanation = displayController.FaultDetail == "coin_sensor_timeout"
                ? "出币传感器等待超时"
                : displayController.FaultDetail;
            return "错误码：" + displayController.FaultCode + "\n" + explanation +
                   "\n游戏已暂停\n请联系维护人员";
        }
    }
}

using UnityEngine;

public sealed class PayoutDisplay : MonoBehaviour
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
        get { return displayController != null && displayController.CurrentPage == DisplayPage.Payout; }
    }

    public string Headline
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }

            return displayController.PayoutComplete ? "出币完成" : "正在出币";
        }
    }

    public string Detail
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }

            if (displayController.PayoutComplete)
            {
                return "已出 " + displayController.PayoutCoins + " 枚\n剩余余额 " +
                       displayController.Balance + " 积分";
            }

            return "应出数量：" + displayController.PayoutCoins + " 枚\n" +
                   "剩余积分：" + displayController.Remainder + "\n" +
                   "请勿断电或进行下注操作";
        }
    }
}

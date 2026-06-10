using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItem : MonoBehaviour
{
    public enum UpgradeType
    {
        one,
        five,
        ten
    }

    public UpgradeType upgradeType = UpgradeType.one;

    public TextMeshProUGUI MultiplayerBuy;
    [SerializeField] private TextMeshProUGUI startValueTxt;
    [SerializeField] private TextMeshProUGUI EndValueTxt;
    [SerializeField] private UpgradeController controller;
    [SerializeField] private Button buyButton;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<UpgradeController>();
        }

        if (buyButton == null)
        {
            buyButton = GetComponentInChildren<Button>(true);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
        }
    }

    public void SetController(UpgradeController owner)
    {
        controller = owner;
    }

    public int GetDelta()
    {
        switch (upgradeType)
        {
            case UpgradeType.one:
                return 1;
            case UpgradeType.five:
                return 5;
            case UpgradeType.ten:
                return 10;
            default:
                return 1;
        }
    }

    public void Refresh(int currentLevel, int nextLevel, string costText)
    {
        if (startValueTxt != null)
        {
            startValueTxt.text = currentLevel.ToString();
        }

        if (EndValueTxt != null)
        {
            EndValueTxt.text = nextLevel.ToString();
        }

        if (MultiplayerBuy != null)
        {
            MultiplayerBuy.text = costText;
        }
    }

    public void Buy()
    {
        if (controller == null)
        {
            return;
        }

        controller.TryPurchase(this);
    }
}

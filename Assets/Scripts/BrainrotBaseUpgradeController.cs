using TMPro;
using UnityEngine;

public class BrainrotBaseUpgradeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrainrotBaseManager baseManager;
    [SerializeField] private UpgradeController money;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text priceText;

    [Header("Upgrade Costs for Slots 11-30")]
    [SerializeField] private double[] slotUpgradeCosts =
    {
        1_000_000d,   // 11
        10_000_000d,  // 12
        25_000_000d,  // 13
        50_000_000d,  // 14
        100_000_000d, // 15
        250_000_000d, // 16
        500_000_000d, // 17
        750_000_000d, // 18
        1_000_000_000d, // 19
        2_500_000_000d, // 20
        5_000_000_000d, // 21
        7_500_000_000d, // 22
        10_000_000_000d, // 23
        12_500_000_000d, // 24
        25_000_000_000d, // 25
        50_000_000_000d, // 26
        100_000_000_000d, // 27
        250_000_000_000d, // 28
        500_000_000_000d, // 29
        750_000_000_000d  // 30
    };

    private void Awake()
    {
        if (baseManager == null)
        {
            baseManager = FindFirstObjectByType<BrainrotBaseManager>();
        }

        if (money == null)
        {
            money = FindFirstObjectByType<UpgradeController>();
        }

        RefreshUI();
    }

    public void TryUpgradeBase()
    {
        if (baseManager == null || money == null)
        {
            return;
        }

        int currentSlots = baseManager.UnlockedSlots;
        if (currentSlots >= baseManager.MaxSlots)
        {
            return;
        }

        double cost = GetCostForSlot(currentSlots + 1);
        if (cost <= 0d)
        {
            return;
        }

        if (!money.TrySpendCoins(cost))
        {
            return;
        }

        baseManager.SetUnlockedSlots(currentSlots + 1);
        RefreshUI();
    }

    private double GetCostForSlot(int slotNumber)
    {
        if (slotNumber <= 10)
        {
            return 0d;
        }

        int index = slotNumber - 11;
        if (slotUpgradeCosts == null || index < 0 || index >= slotUpgradeCosts.Length)
        {
            return 0d;
        }

        return slotUpgradeCosts[index];
    }

    public void RefreshUI()
    {
        if (baseManager == null)
        {
            return;
        }

        int currentSlots = baseManager.UnlockedSlots;
        int maxSlots = baseManager.MaxSlots;

        if (levelText != null)
        {
            int upgradeLevel = Mathf.Max(0, currentSlots - 10);
            int maxUpgrade = Mathf.Max(0, maxSlots - 10);
            levelText.text = $"{upgradeLevel}/{maxUpgrade}";
        }

        if (priceText != null)
        {
            double cost = GetCostForSlot(currentSlots + 1);
            priceText.text = cost > 0d ? UpgradeController.FormatCurrency(cost) : "MAX";
        }
    }
}

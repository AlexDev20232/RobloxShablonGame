using TMPro;
using UnityEngine;

public class BrainrotBaseUpgradeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrainrotBaseManager baseManager;
    [SerializeField] private UpgradeController money;
    [SerializeField] private BaseProgressionConfig progressionConfig;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text priceText;

    [Header("Slot Upgrade Pricing")]
    [SerializeField] private int firstPaidSlotNumber = 6;
    [SerializeField] private double[] slotUpgradeCosts =
    {
        500d,          // 6
        2_500d,        // 7
        10_000d,       // 8
        50_000d,       // 9
        250_000d,      // 10
        1_000_000d,    // 11
        10_000_000d,   // 12
        25_000_000d,   // 13
        50_000_000d,   // 14
        100_000_000d,  // 15
        250_000_000d,  // 16
        500_000_000d,  // 17
        750_000_000d,  // 18
        1_000_000_000d // 19
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

    public bool CanUpgrade()
    {
        return baseManager != null && money != null && baseManager.UnlockedSlots < baseManager.MaxSlots && GetNextSlotCost() > 0d;
    }

    public double GetNextSlotCost()
    {
        if (baseManager == null)
        {
            return 0d;
        }

        return GetCostForSlot(baseManager.GetNextSlotNumber());
    }

    public void TryUpgradeBase()
    {
        if (baseManager == null || money == null)
        {
            return;
        }

        if (!baseManager.CanUnlockMoreSlots())
        {
            return;
        }

        double cost = GetNextSlotCost();
        if (cost <= 0d)
        {
            return;
        }

        if (!money.TrySpendCoins(cost))
        {
            return;
        }

        baseManager.TryUnlockNextSlot();
        RefreshUI();
    }

    private double GetCostForSlot(int slotNumber)
    {
        if (progressionConfig != null)
        {
            return progressionConfig.GetSlotCost(slotNumber);
        }

        if (slotNumber < firstPaidSlotNumber)
        {
            return 0d;
        }

        int index = slotNumber - firstPaidSlotNumber;
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
            levelText.text = $"{currentSlots}/{maxSlots}";
        }

        if (priceText != null)
        {
            double cost = GetNextSlotCost();
            priceText.text = cost > 0d ? UpgradeController.FormatCurrency(cost) : "MAX";
        }
    }
}

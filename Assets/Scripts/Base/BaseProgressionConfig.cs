using UnityEngine;

[CreateAssetMenu(menuName = "Brainrot Template/Base Progression Config")]
public class BaseProgressionConfig : ScriptableObject
{
    [Header("Rebirth")]
    [SerializeField] private float baseMultiplier = 1f;
    [SerializeField] private float multiplierPerRebirth = 0.5f;

    [Header("Slot Prices")]
    [SerializeField] private int firstPaidSlotNumber = 11;
    [SerializeField] private double[] slotUpgradeCosts =
    {
        50_000d,
        125_000d,
        300_000d,
        750_000d,
        1_500_000d,
        4_000_000d,
        10_000_000d,
        25_000_000d,
        60_000_000d,
        150_000_000d,
        350_000_000d,
        800_000_000d,
        1_500_000_000d,
        3_000_000_000d,
        7_500_000_000d,
        15_000_000_000d,
        35_000_000_000d,
        80_000_000_000d,
        175_000_000_000d,
        400_000_000_000d
    };

    public int FirstPaidSlotNumber => Mathf.Max(1, firstPaidSlotNumber);
    public float BaseMultiplier => Mathf.Max(0f, baseMultiplier);
    public float MultiplierPerRebirth => Mathf.Max(0f, multiplierPerRebirth);

    public float GetMultiplier(int rebirthLevel)
    {
        return BaseMultiplier + MultiplierPerRebirth * Mathf.Max(0, rebirthLevel);
    }

    public double GetSlotCost(int slotNumber)
    {
        if (slotNumber < FirstPaidSlotNumber)
        {
            return 0d;
        }

        int index = slotNumber - FirstPaidSlotNumber;
        if (slotUpgradeCosts == null || index < 0 || index >= slotUpgradeCosts.Length)
        {
            return 0d;
        }

        return System.Math.Max(0d, slotUpgradeCosts[index]);
    }
}

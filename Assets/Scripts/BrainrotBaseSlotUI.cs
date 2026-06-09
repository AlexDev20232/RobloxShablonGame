using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrainrotBaseSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text storedText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private BrainrotBaseSlot slot;

    private void Awake()
    {
        if (slot == null)
        {
            slot = GetComponentInParent<BrainrotBaseSlot>();
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(HandleUpgradeClick);
        }
    }

    public void SetName(string value)
    {
        if (nameText != null)
        {
            nameText.text = value;
        }
    }

    public void SetRarity(string value)
    {
        if (rarityText != null)
        {
            rarityText.text = value;
        }
    }

    public void SetLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"level {level} > level{level+1}";
        }
    }

    public void SetUpgradeCost(string value)
    {
        if (upgradeCostText != null)
        {
            upgradeCostText.text = value;
        }
    }

    public void SetIncome(string value)
    {
        if (incomeText != null)
        {
            incomeText.text = value;
        }
    }

    public void SetStored(string value)
    {
        if (storedText != null)
        {
            storedText.text = value;
        }
    }

    public void SetEmpty()
    {
        if (nameText != null)
        {
            nameText.text = string.Empty;
        }

        if (rarityText != null)
        {
            rarityText.text = string.Empty;
        }

        if (levelText != null)
        {
            levelText.text = "Lv.1";
        }

        if (upgradeCostText != null)
        {
            upgradeCostText.text = "$0";
        }

        if (incomeText != null)
        {
            incomeText.text = "$0/s";
        }

        if (storedText != null)
        {
            storedText.text = "$0";
        }
    }

    private void HandleUpgradeClick()
    {
        if (slot != null)
        {
            slot.TryUpgrade();
        }
    }
}

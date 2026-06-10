using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebirthSystem : MonoBehaviour
{
    private enum RebirthRequirementType
    {
        SpeedLevel,
        Coins
    }

    [Header("Settings")]
    [SerializeField] private BaseProgressionConfig progressionConfig;
    [SerializeField] private int maxRebirthLevel = 20;
    [SerializeField] private bool resetSpeedOnRebirth = true;
    [SerializeField] private int startingSpeedLevel = 0;
    [SerializeField] private RebirthRequirementType requirementType = RebirthRequirementType.SpeedLevel;
    [SerializeField] private int baseSpeedRequirement = 40;
    [SerializeField] private int speedRequirementStep = 10;
    [SerializeField] private double baseCoinRequirement = 100_000d;
    [SerializeField] private double coinRequirementGrowth = 2d;

    [Header("Save Keys")]
    [SerializeField] private string rebirthKey = "RebirthLevel";

    [Header("References")]
    [SerializeField] private UpgradeController speedController;
    [SerializeField] private BrainrotBaseManager baseManager;

    [Header("UI")]
    [SerializeField] private TMP_Text currentRebirthText;
    [SerializeField] private TMP_Text nextRebirthText;
    [SerializeField] private TMP_Text currentMultiplierText;
    [SerializeField] private TMP_Text nextMultiplierText;
    [SerializeField] private TMP_Text speedProgressText;
    [SerializeField] private Slider speedSlider;

    private int _rebirthLevel;

    public int CurrentRebirth => _rebirthLevel;
    public float CurrentMultiplier => progressionConfig != null ? progressionConfig.GetMultiplier(_rebirthLevel) : 1f + 0.5f * _rebirthLevel;

    private void Awake()
    {
        if (speedController == null)
        {
            speedController = FindFirstObjectByType<UpgradeController>();
        }

        if (baseManager == null)
        {
            baseManager = FindFirstObjectByType<BrainrotBaseManager>();
        }

        _rebirthLevel = PlayerPrefs.GetInt(rebirthKey, 0);
        ApplyRebirthEffects();
        RefreshUI();
    }

    public int GetRequiredSpeedForNext()
    {
        return baseSpeedRequirement + Mathf.Max(0, _rebirthLevel) * speedRequirementStep;
    }

    public double GetRequiredCoinsForNext()
    {
        double growth = System.Math.Max(1.01d, coinRequirementGrowth);
        return System.Math.Round(baseCoinRequirement * System.Math.Pow(growth, Mathf.Max(0, _rebirthLevel)), System.MidpointRounding.AwayFromZero);
    }

    public float GetNextMultiplier()
    {
        int next = Mathf.Clamp(_rebirthLevel + 1, 0, maxRebirthLevel);
        return progressionConfig != null ? progressionConfig.GetMultiplier(next) : 1f + 0.5f * next;
    }

    public bool CanRebirth()
    {
        if (_rebirthLevel >= maxRebirthLevel || speedController == null)
        {
            return false;
        }

        if (requirementType == RebirthRequirementType.Coins)
        {
            return speedController.CurrentCoins >= GetRequiredCoinsForNext();
        }

        return speedController.CurrentSpeedLevel >= GetRequiredSpeedForNext();
    }

    public void TryRebirth()
    {
        if (!CanRebirth())
        {
            return;
        }

        if (requirementType == RebirthRequirementType.Coins && !speedController.TrySpendCoins(GetRequiredCoinsForNext()))
        {
            return;
        }

        _rebirthLevel++;
        PlayerPrefs.SetInt(rebirthKey, _rebirthLevel);
        PlayerPrefs.Save();

        if (resetSpeedOnRebirth && speedController != null)
        {
            speedController.SetSpeedLevel(startingSpeedLevel);
        }

        ApplyRebirthEffects();
        RefreshUI();
    }

    private void ApplyRebirthEffects()
    {
        if (baseManager != null)
        {
            baseManager.ApplyRebirthLevel(_rebirthLevel);
        }
    }

    public void RefreshUI()
    {
        int required = GetRequiredSpeedForNext();
        int currentSpeed = speedController != null ? speedController.CurrentSpeedLevel : 0;
        double requiredCoins = GetRequiredCoinsForNext();
        double currentCoins = speedController != null ? speedController.CurrentCoins : 0d;

        if (currentRebirthText != null)
        {
            currentRebirthText.text = $"Rebirth {_rebirthLevel}";
        }

        if (nextRebirthText != null)
        {
            nextRebirthText.text = _rebirthLevel >= maxRebirthLevel ? "MAX" : $"Rebirth {_rebirthLevel + 1}";
        }

        if (currentMultiplierText != null)
        {
            currentMultiplierText.text = $"{CurrentMultiplier:0.##}x";
        }

        if (nextMultiplierText != null)
        {
            nextMultiplierText.text = $"{GetNextMultiplier():0.##}x";
        }

        if (speedProgressText != null)
        {
            speedProgressText.text = requirementType == RebirthRequirementType.Coins
                ? $"{UpgradeController.FormatCurrency(currentCoins)}/{UpgradeController.FormatCurrency(requiredCoins)}"
                : $"Speed {currentSpeed}/{required}";
        }

        if (speedSlider != null)
        {
            if (requirementType == RebirthRequirementType.Coins)
            {
                speedSlider.minValue = 0f;
                speedSlider.maxValue = 1f;
                speedSlider.value = requiredCoins > 0d ? Mathf.Clamp01((float)(currentCoins / requiredCoins)) : 0f;
            }
            else
            {
                speedSlider.minValue = 0f;
                speedSlider.maxValue = required;
                speedSlider.value = currentSpeed;
            }
        }
    }
}

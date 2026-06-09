using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebirthSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxRebirthLevel = 20;
    [SerializeField] private int startingSpeedLevel = 18;

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
    public float CurrentMultiplier => 1f + 0.5f * _rebirthLevel;

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
        return 40 + Mathf.Max(0, _rebirthLevel) * 10;
    }

    public float GetNextMultiplier()
    {
        int next = Mathf.Clamp(_rebirthLevel + 1, 0, maxRebirthLevel);
        return 1f + 0.5f * next;
    }

    public void TryRebirth()
    {
        if (_rebirthLevel >= maxRebirthLevel)
        {
            return;
        }

        int required = GetRequiredSpeedForNext();
        int currentSpeed = speedController != null ? speedController.CurrentSpeedLevel : 0;
        if (currentSpeed < required)
        {
            return;
        }

        _rebirthLevel++;
        PlayerPrefs.SetInt(rebirthKey, _rebirthLevel);
        PlayerPrefs.Save();

        if (speedController != null)
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

        if (currentRebirthText != null)
        {
            currentRebirthText.text = $"Rebirth {_rebirthLevel}";
        }

        if (nextRebirthText != null)
        {
            nextRebirthText.text = $"Rebirth {_rebirthLevel + 1}";
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
            speedProgressText.text = $"Speed {currentSpeed}/{required}";
        }

        if (speedSlider != null)
        {
            speedSlider.minValue = 0f;
            speedSlider.maxValue = required;
            speedSlider.value = currentSpeed;
        }
    }
}

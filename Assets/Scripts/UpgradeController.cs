using System;
using System.Globalization;
using UnityEngine;

public class UpgradeController : MonoBehaviour
{
    [SerializeField] private UpgradeItem[] upgradeItems;

    [Header("Speed")]
    [SerializeField] private SimpleRunIdleController playerController;
    [SerializeField] private SimpleRobloxController robloxController;
    [SerializeField] private bool autoFindPlayerController = true;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float speedPerLevel = 0.1f;
    [SerializeField] private float sprintMultiplier = 1.65f;

    [Header("Starting Values")]
    [SerializeField] private int startingSpeedLevel = 0;
    [SerializeField] private double startingCoins = 0d;

    [Header("Save Keys")]
    [SerializeField] private string speedLevelKey = "SpeedLevel";
    [SerializeField] private string coinsKey = "Coins";

    private int _speedLevel;
    private double _coins;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa" };

    private void Awake()
    {
        ResolvePlayerControllers();
        BindUpgradeItems();
        Load();
        ApplySpeed();
        RefreshUI();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public int CurrentSpeedLevel => _speedLevel;
    public double CurrentCoins => _coins;

    public void TryPurchase(UpgradeItem item)
    {
        if (item == null)
        {
            return;
        }

        int delta = item.GetDelta();
        if (delta <= 0)
        {
            return;
        }

        long cost = GetCostForDelta(delta);
        if (_coins < cost)
        {
            return;
        }

        _coins -= cost;
        _speedLevel += delta;

        Save();
        ApplySpeed();
        RefreshUI();
    }

    public void AddCoins(double amount)
    {
        if (amount <= 0d)
        {
            return;
        }

        _coins += amount;
        Save();
        RefreshUI();
    }

    public void SetCoins(double amount)
    {
        _coins = Math.Max(0d, amount);
        Save();
        RefreshUI();
    }

    public void SetSpeedLevel(int level)
    {
        _speedLevel = Mathf.Max(0, level);
        Save();
        ApplySpeed();
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (upgradeItems == null)
        {
            return;
        }

        for (int i = 0; i < upgradeItems.Length; i++)
        {
            UpgradeItem item = upgradeItems[i];
            if (item == null)
            {
                continue;
            }

            int delta = item.GetDelta();
            if (delta <= 0)
            {
                continue;
            }

            long cost = GetCostForDelta(delta);
            item.Refresh(_speedLevel, _speedLevel + delta, FormatCurrency(cost));
        }
    }

    private void BindUpgradeItems()
    {
        if (upgradeItems == null || upgradeItems.Length == 0)
        {
            upgradeItems = GetComponentsInChildren<UpgradeItem>(true);
        }

        if (upgradeItems == null)
        {
            return;
        }

        for (int i = 0; i < upgradeItems.Length; i++)
        {
            if (upgradeItems[i] != null)
            {
                upgradeItems[i].SetController(this);
            }
        }
    }

    private void ResolvePlayerControllers()
    {
        if (!autoFindPlayerController)
        {
            return;
        }

        if (robloxController == null)
        {
            robloxController = FindFirstObjectByType<SimpleRobloxController>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<SimpleRunIdleController>();
        }
    }

    private void Load()
    {
        _speedLevel = PlayerPrefs.GetInt(speedLevelKey, startingSpeedLevel);

        if (PlayerPrefs.HasKey(coinsKey))
        {
            string raw = PlayerPrefs.GetString(coinsKey, "0");
            if (!double.TryParse(raw, NumberStyles.Float, Invariant, out _coins))
            {
                _coins = startingCoins;
            }
        }
        else
        {
            _coins = startingCoins;
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(speedLevelKey, _speedLevel);
        PlayerPrefs.SetString(coinsKey, _coins.ToString("R", Invariant));
        PlayerPrefs.Save();
    }

    private void ApplySpeed()
    {
        float target = baseMoveSpeed + _speedLevel * speedPerLevel;
        float walk = Mathf.Max(0.01f, target);

        if (robloxController != null)
        {
            float sprint = walk * Mathf.Max(1f, sprintMultiplier);
            robloxController.SetMoveSpeeds(walk, sprint);
        }

        if (playerController != null)
        {
            playerController.moveSpeed = walk;
        }
    }

    private long GetCostForDelta(int delta)
    {
        long baseCost = GetBaseCost(_speedLevel);
        double multiplier = 1d;

        switch (delta)
        {
            case 1:
                multiplier = 1d;
                break;
            case 5:
                multiplier = 6.95d;
                break;
            case 10:
                multiplier = 21.88d;
                break;
            default:
                multiplier = delta;
                break;
        }

        return RoundToLong(baseCost * multiplier);
    }

    private static long GetBaseCost(int level)
    {
        double raw = 14.85d * Math.Pow(1.1653d, level);
        return RoundToLong(raw);
    }

    private static long RoundToLong(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return long.MaxValue;
        }

        double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded <= 0d)
        {
            return 0L;
        }

        if (rounded >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)rounded;
    }

    public static string FormatCurrency(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return "$0";
        }

        bool negative = value < 0d;
        double abs = Math.Abs(value);

        int suffixIndex = 0;
        while (abs >= 1000d && suffixIndex < Suffixes.Length - 1)
        {
            abs /= 1000d;
            suffixIndex++;
        }

        string format = suffixIndex == 0 ? "0" : "0.##";
        string number = abs.ToString(format, Invariant);
        string prefix = negative ? "-$" : "$";
        return prefix + number + Suffixes[suffixIndex];
    }

    public static string FormatCurrency(long value)
    {
        return FormatCurrency((double)value);
    }

    public bool TrySpendCoins(double amount)
    {
        if (amount <= 0d)
        {
            return true;
        }

        if (_coins < amount)
        {
            return false;
        }

        _coins -= amount;
        Save();
        RefreshUI();
        return true;
    }
}

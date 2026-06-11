using System;
using System.Collections.Generic;
using UnityEngine;

public class BrainrotDefinition : MonoBehaviour
{
    private static readonly List<BrainrotDefinition> Active = new List<BrainrotDefinition>();

    [Header("Display")]
    public string title;
    public string displayName = "Brainrot";
    public BrainrotRarity rarity = BrainrotRarity.Common;
    public BrainrotType type = BrainrotType.Normal;
    public float incomePerSecond = 1f;
    public float incomeLevelMultiplier = 1.25f;
    public double upgradeCost1 = 25000d;
    public double upgradeCostGrowth = 1.5d;
    public Sprite indexIcon;
    public string indexId;

    [Header("UI Anchor (optional)")]
    public Transform uiAnchor;

    public static IReadOnlyList<BrainrotDefinition> ActiveList => Active;
    public bool IsCarried { get; private set; }
    public bool IsInInventory { get; private set; }
    public bool IsInBase { get; private set; }

    public string GetIndexId()
    {
        if (!string.IsNullOrWhiteSpace(indexId))
        {
            return indexId;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        string fallback = name;
        const string cloneSuffix = "(Clone)";
        if (fallback.EndsWith(cloneSuffix))
        {
            fallback = fallback.Replace(cloneSuffix, string.Empty).Trim();
        }

        return fallback;
    }

    public string GetCollectionId()
    {
        return type + ":" + GetIndexId();
    }

    public double GetIncomeForLevel(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        double baseIncome = Math.Max(0d, incomePerSecond);
        double growth = Math.Max(1d, incomeLevelMultiplier);
        return baseIncome * Math.Pow(growth, safeLevel - 1);
    }

    public double GetUpgradeCostForLevel(int currentLevel)
    {
        int levelIndex = Mathf.Max(0, currentLevel - 1);
        double baseCost = Math.Max(0d, upgradeCost1);
        double growth = Math.Max(1.01d, upgradeCostGrowth);
        return Math.Round(baseCost * Math.Pow(growth, levelIndex), MidpointRounding.AwayFromZero);
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
        {
            Active.Add(this);
        }
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public void SetCarried(bool value)
    {
        IsCarried = value;
        if (value)
        {
            IsInInventory = false;
            IsInBase = false;
        }
    }

    public void SetInInventory(bool value)
    {
        IsInInventory = value;
        if (value)
        {
            IsCarried = false;
            IsInBase = false;
        }
    }

    public void SetInBase(bool value)
    {
        IsInBase = value;
        if (value)
        {
            IsCarried = false;
            IsInInventory = false;
        }
    }
}

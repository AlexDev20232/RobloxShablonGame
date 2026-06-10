using TMPro;
using UnityEngine;

public class BaseStatsSign : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrainrotBaseManager baseManager;
    [SerializeField] private BrainrotBaseUpgradeController baseUpgrade;
    [SerializeField] private RebirthSystem rebirthSystem;

    [Header("Text")]
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text nextSlotCostText;
    [SerializeField] private TMP_Text slotProgressText;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.2f;

    private float _nextRefreshTime;

    private void Awake()
    {
        ResolveReferences();
        Refresh();
    }

    private void OnEnable()
    {
        if (baseManager != null)
        {
            baseManager.SlotsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (baseManager != null)
        {
            baseManager.SlotsChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (Time.time < _nextRefreshTime)
        {
            return;
        }

        _nextRefreshTime = Time.time + Mathf.Max(0.05f, refreshInterval);
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        if (multiplierText != null)
        {
            float multiplier = rebirthSystem != null ? rebirthSystem.CurrentMultiplier : 1f;
            multiplierText.text = "x" + multiplier.ToString("0.##") + " Multi";
        }

        if (nextSlotCostText != null)
        {
            bool canUpgrade = baseUpgrade != null && baseUpgrade.CanUpgrade();
            nextSlotCostText.text = canUpgrade
                ? UpgradeController.FormatCurrency(baseUpgrade.GetNextSlotCost())
                : "MAX";
        }

        if (slotProgressText != null && baseManager != null)
        {
            slotProgressText.text = "Slots " + baseManager.UnlockedSlots + "/" + baseManager.MaxSlots;
        }
    }

    private void ResolveReferences()
    {
        if (baseManager == null)
        {
            baseManager = FindFirstObjectByType<BrainrotBaseManager>();
        }

        if (baseUpgrade == null)
        {
            baseUpgrade = FindFirstObjectByType<BrainrotBaseUpgradeController>();
        }

        if (rebirthSystem == null)
        {
            rebirthSystem = FindFirstObjectByType<RebirthSystem>();
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (multiplierText == null)
        {
            multiplierText = FindText(texts, "$0/s", "multi", "mult");
        }

        if (nextSlotCostText == null)
        {
            nextSlotCostText = FindText(texts, "$100.000", "cost", "price");
        }

        if (slotProgressText == null)
        {
            slotProgressText = FindText(texts, "slots", "slot");
        }
    }

    private static TMP_Text FindText(TMP_Text[] texts, params string[] markers)
    {
        if (texts == null || markers == null)
        {
            return null;
        }

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            string value = (text.text + " " + text.name).ToLowerInvariant();
            for (int j = 0; j < markers.Length; j++)
            {
                if (!string.IsNullOrWhiteSpace(markers[j]) && value.Contains(markers[j].ToLowerInvariant()))
                {
                    return text;
                }
            }
        }

        return null;
    }
}

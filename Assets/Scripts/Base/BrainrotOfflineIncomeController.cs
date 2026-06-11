using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrainrotOfflineIncomeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrainrotBaseManager baseManager;
    [SerializeField] private UpgradeController money;

    [Header("Offline Rules")]
    [SerializeField] private bool enabledOfflineIncome = true;
    [SerializeField] private double maxOfflineSeconds = 21_600d;
    [SerializeField] private double offlineEfficiency = 1d;
    [SerializeField] private bool autoCollectWhenNoPopup = false;

    [Header("Save Key")]
    [SerializeField] private string lastSeenUtcTicksKey = "BrainrotLastSeenUtcTicks";

    [Header("UI Optional")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Button collectButton;

    private double _pending;
    private double _elapsedSeconds;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

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

        if (collectButton != null)
        {
            collectButton.onClick.RemoveListener(CollectPending);
            collectButton.onClick.AddListener(CollectPending);
        }

        SetPopupVisible(false);
    }

    private void Start()
    {
        CalculatePending();
        SaveCurrentTimestamp();
    }

    private void OnDestroy()
    {
        if (collectButton != null)
        {
            collectButton.onClick.RemoveListener(CollectPending);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveCurrentTimestamp();
        }
    }

    private void OnApplicationQuit()
    {
        SaveCurrentTimestamp();
    }

    public void CollectPending()
    {
        if (_pending <= 0d)
        {
            SetPopupVisible(false);
            return;
        }

        if (money != null)
        {
            money.AddCoins(_pending);
        }

        _pending = 0d;
        _elapsedSeconds = 0d;
        SetPopupVisible(false);
        SaveCurrentTimestamp();
    }

    private void CalculatePending()
    {
        _pending = 0d;
        _elapsedSeconds = 0d;

        if (!enabledOfflineIncome || baseManager == null)
        {
            return;
        }

        if (!TryLoadLastSeen(out DateTime lastSeenUtc))
        {
            return;
        }

        double elapsed = Math.Max(0d, (DateTime.UtcNow - lastSeenUtc).TotalSeconds);
        elapsed = Math.Min(elapsed, Math.Max(0d, maxOfflineSeconds));
        if (elapsed <= 1d)
        {
            return;
        }

        double rate = baseManager.GetTotalIncomePerSecond();
        _pending = Math.Max(0d, rate * elapsed * Math.Max(0d, offlineEfficiency));
        _elapsedSeconds = elapsed;

        if (_pending <= 0d)
        {
            return;
        }

        RefreshUI();

        if (popupRoot != null)
        {
            SetPopupVisible(true);
        }
        else if (autoCollectWhenNoPopup)
        {
            CollectPending();
        }
    }

    private void RefreshUI()
    {
        if (amountText != null)
        {
            amountText.text = UpgradeController.FormatCurrency(_pending);
        }

        if (timeText != null)
        {
            timeText.text = FormatTime(_elapsedSeconds);
        }
    }

    private void SetPopupVisible(bool visible)
    {
        if (popupRoot != null && popupRoot.activeSelf != visible)
        {
            popupRoot.SetActive(visible);
        }
    }

    private bool TryLoadLastSeen(out DateTime value)
    {
        value = default;
        string raw = PlayerPrefs.GetString(lastSeenUtcTicksKey, string.Empty);
        if (!long.TryParse(raw, NumberStyles.Integer, Invariant, out long ticks))
        {
            return false;
        }

        if (ticks <= 0L)
        {
            return false;
        }

        value = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    private void SaveCurrentTimestamp()
    {
        PlayerPrefs.SetString(lastSeenUtcTicksKey, DateTime.UtcNow.Ticks.ToString(Invariant));
        PlayerPrefs.Save();
    }

    private static string FormatTime(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        if (time.TotalHours >= 1d)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }

        return $"{time.Minutes}m {time.Seconds}s";
    }
}

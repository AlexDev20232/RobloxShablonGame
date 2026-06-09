using System.Globalization;
using TMPro;
using UnityEngine;

public class PlayerStatsDisplay : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private UpgradeController controller;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [SerializeField] private string coinsPrefix = "";
    [SerializeField] private string speedPrefix = "Speed: ";

    [Header("Fallback Save Keys")]
    [SerializeField] private string coinsKey = "Coins";
    [SerializeField] private string speedLevelKey = "SpeedLevel";

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.2f;

    private double _lastCoins = double.NaN;
    private int _lastSpeed = int.MinValue;
    private float _nextRefresh;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<UpgradeController>();
        }
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        if (refreshInterval <= 0f || Time.unscaledTime >= _nextRefresh)
        {
            ForceRefresh();
        }
    }

    public void ForceRefresh()
    {
        _nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);

        double coins;
        int speed;

        if (controller != null)
        {
            coins = controller.CurrentCoins;
            speed = controller.CurrentSpeedLevel;
        }
        else
        {
            coins = ReadCoins();
            speed = PlayerPrefs.GetInt(speedLevelKey, 0);
        }

        if (coins != _lastCoins)
        {
            _lastCoins = coins;
            if (coinsText != null)
            {
                coinsText.text = coinsPrefix + UpgradeController.FormatCurrency(coins);
            }
        }

        if (speed != _lastSpeed)
        {
            _lastSpeed = speed;
            if (speedLevelText != null)
            {
                speedLevelText.text = speedPrefix + speed.ToString(Invariant);
            }
        }
    }

    private double ReadCoins()
    {
        if (!PlayerPrefs.HasKey(coinsKey))
        {
            return 0d;
        }

        string raw = PlayerPrefs.GetString(coinsKey, "0");
        if (double.TryParse(raw, NumberStyles.Float, Invariant, out double value))
        {
            return value;
        }

        return 0d;
    }
}

using TMPro;
using UnityEngine;

public class BrainrotBaseIncomeCollector : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private BrainrotBaseManager baseManager;
    [SerializeField] private RebirthSystem rebirth;
    [SerializeField] private UpgradeController money;

    [Header("UI")]
    [SerializeField] private TMP_Text storedText;
    [SerializeField] private TMP_Text rateText;

    [Header("Collection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool collectOnTrigger = true;

    private double _stored;

    private void Awake()
    {
        if (baseManager == null)
        {
            baseManager = FindFirstObjectByType<BrainrotBaseManager>();
        }

        if (rebirth == null)
        {
            rebirth = FindFirstObjectByType<RebirthSystem>();
        }

        if (money == null)
        {
            money = FindFirstObjectByType<UpgradeController>();
        }
    }

    private void Update()
    {
        double rate = GetTotalRate();
        _stored += rate * Time.deltaTime;

        if (storedText != null)
        {
            storedText.text = UpgradeController.FormatCurrency(_stored);
        }

        if (rateText != null)
        {
            rateText.text = UpgradeController.FormatCurrency(rate) + "/s";
        }
    }

    private double GetTotalRate()
    {
        if (baseManager == null)
        {
            return 0d;
        }

        double total = 0d;
        foreach (BrainrotBaseSlot slot in baseManager.GetSlots())
        {
            if (slot == null)
            {
                continue;
            }

            total += slot.GetIncomePerSecond();
        }

        return total;
    }

    public void Collect()
    {
        if (_stored <= 0d)
        {
            return;
        }

        if (money != null)
        {
            money.AddCoins(_stored);
        }

        _stored = 0d;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!collectOnTrigger)
        {
            return;
        }

        if (other.CompareTag(playerTag))
        {
            Collect();
        }
    }
}

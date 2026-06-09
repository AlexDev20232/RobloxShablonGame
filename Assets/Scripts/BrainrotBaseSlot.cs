using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BrainrotBaseSlot : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private int slotIndex;
    [SerializeField] private Transform placePoint;
    [SerializeField] private Transform faceTarget;
    [SerializeField] private bool faceTargetOnPlace = true;
    [SerializeField] private float facingYawOffset = 180f;

    [Header("Prompt")]
    [SerializeField] private PurchasePrompt promptPrefab;
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private string placeText = "Place Brainrot";
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private KeyCode holdKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    [Header("UI")]
    [SerializeField] private BrainrotBaseSlotUI slotUI;

    [Header("References")]
    [SerializeField] private BrainrotInventory inventory;

    [Header("Stored Income")]
    [SerializeField] private bool updateStoredUi = true;
    [SerializeField] private float uiRefreshInterval = 0.25f;

    private PurchasePrompt _promptInstance;
    private BrainrotDefinition _occupant;
    private int _level = 1;
    private bool _unlocked = true;
    private bool _playerInside;
    private float _holdTimer;
    private float _nextUiRefreshTime;
    private double _stored;

    private UpgradeController _money;
    private RebirthSystem _rebirth;

    public int SlotIndex => slotIndex;
    public bool IsUnlocked => _unlocked;
    public bool IsOccupied => _occupant != null;

    private void Awake()
    {
        if (placePoint == null)
        {
            placePoint = transform;
        }

        if (promptAnchor == null)
        {
            promptAnchor = transform;
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<BrainrotInventory>();
        }

        _money = FindFirstObjectByType<UpgradeController>();
        _rebirth = FindFirstObjectByType<RebirthSystem>();

        RefreshUI();
    }

    private void Update()
    {
        if (!_unlocked)
        {
            SetPromptActive(false);
            return;
        }

        if (_occupant != null)
        {
            _stored += GetIncomePerSecond() * Time.deltaTime;
            if (Time.time >= _nextUiRefreshTime)
            {
                _nextUiRefreshTime = Time.time + Mathf.Max(0.05f, uiRefreshInterval);
                UpdateStoredUI();
            }

            SetPromptActive(false);
            return;
        }

        if (!_playerInside || inventory == null || !inventory.IsCarrying)
        {
            ResetHold();
            SetPromptActive(false);
            return;
        }

        EnsurePrompt();
        SetPromptActive(true);
        _promptInstance.SetName(placeText);

        if (!IsHoldPressed())
        {
            ResetHold();
            return;
        }

        _holdTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_holdTimer / Mathf.Max(0.01f, holdDuration));
        _promptInstance.SetHoldProgress(progress);

        if (progress >= 1f)
        {
            _holdTimer = 0f;
            _promptInstance.SetHoldProgress(0f);
            PlaceFromInventory();
        }
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public void SetUnlocked(bool unlocked)
    {
        _unlocked = unlocked;
        if (gameObject.activeSelf != unlocked)
        {
            gameObject.SetActive(unlocked);
        }

        RefreshUI();
    }

    public double GetIncomePerSecond()
    {
        if (_occupant == null)
        {
            return 0d;
        }

        double income = _occupant.incomePerSecond;
        income *= Math.Pow(_occupant.incomeLevelMultiplier, Mathf.Max(0, _level - 1));

        float mult = _rebirth != null ? _rebirth.CurrentMultiplier : 1f;
        return income * mult;
    }

    public void TryUpgrade()
    {
        if (_occupant == null || _money == null)
        {
            return;
        }

        double cost = GetUpgradeCost();
        if (!_money.TrySpendCoins(cost))
        {
            return;
        }

        _level++;
        RefreshUI();
    }

    private double GetUpgradeCost()
    {
        if (_occupant == null)
        {
            return 0d;
        }

        int levelIndex = Mathf.Max(0, _level - 1);
        double baseCost = Math.Max(0d, _occupant.upgradeCost1);
        double growth = Math.Max(1.01d, _occupant.upgradeCostGrowth);
        return Math.Round(baseCost * Math.Pow(growth, levelIndex), MidpointRounding.AwayFromZero);
    }

    private void PlaceFromInventory()
    {
        if (_occupant != null || inventory == null)
        {
            return;
        }

        if (!inventory.TryTakeCarriedForPlacement(out BrainrotDefinition def))
        {
            return;
        }

        _occupant = def;
        _level = 1;
        _stored = 0d;

        def.SetInBase(true);
        def.gameObject.SetActive(true);

        Transform target = placePoint != null ? placePoint : transform;
        Vector3 worldScale = def.transform.lossyScale;
        def.transform.SetParent(target, false);
        def.transform.localPosition = Vector3.zero;
        def.transform.localRotation = Quaternion.identity;

        RestoreWorldScale(def.transform, worldScale);
        ApplyPlacedRotation(def.transform);
        BrainrotIndexData.Unlock(def);

        BrainrotLifetime lifetime = def.GetComponent<BrainrotLifetime>();
        if (lifetime != null)
        {
            lifetime.SetPaused(true);
        }

        BrainrotUI ui = def.GetComponentInChildren<BrainrotUI>();
        if (ui != null)
        {
            ui.SetTimerVisible(false);
        }

        RefreshUI();
        SetPromptActive(false);
    }

    public void RefreshUI()
    {
        if (slotUI == null)
        {
            return;
        }

        if (_occupant == null)
        {
            slotUI.SetEmpty();
            return;
        }

        slotUI.SetName(_occupant.displayName);
        slotUI.SetRarity(_occupant.rarity.ToString());
        slotUI.SetLevel(_level);
        slotUI.SetUpgradeCost(UpgradeController.FormatCurrency(GetUpgradeCost()));
        slotUI.SetIncome(UpgradeController.FormatCurrency(GetIncomePerSecond()) + "/s");
        slotUI.SetUpgradeInteractable(_money != null);
        UpdateStoredUI();
    }

    public void CollectStored()
    {
        if (_stored <= 0d)
        {
            return;
        }

        if (_money != null)
        {
            _money.AddCoins(_stored);
        }

        _stored = 0d;
        UpdateStoredUI();
    }

    private void UpdateStoredUI()
    {
        if (!updateStoredUi || slotUI == null)
        {
            return;
        }

        slotUI.SetStored(UpgradeController.FormatCurrency(_stored));
    }

    private static void RestoreWorldScale(Transform target, Vector3 worldScale)
    {
        if (target == null)
        {
            return;
        }

        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 p = parent.lossyScale;
        float x = Mathf.Abs(p.x) < 0.0001f ? 1f : worldScale.x / p.x;
        float y = Mathf.Abs(p.y) < 0.0001f ? 1f : worldScale.y / p.y;
        float z = Mathf.Abs(p.z) < 0.0001f ? 1f : worldScale.z / p.z;
        target.localScale = new Vector3(x, y, z);
    }

    private void ApplyPlacedRotation(Transform placed)
    {
        if (placed == null)
        {
            return;
        }

        if (!faceTargetOnPlace)
        {
            placed.rotation = (placePoint != null ? placePoint.rotation : transform.rotation) *
                              Quaternion.Euler(0f, facingYawOffset, 0f);
            return;
        }

        Transform target = faceTarget;
        if (target == null)
        {
            BrainrotBaseManager manager = GetComponentInParent<BrainrotBaseManager>();
            if (manager != null)
            {
                target = manager.transform;
            }
        }

        Vector3 direction = target != null
            ? target.position - placed.position
            : transform.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        placed.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) *
                          Quaternion.Euler(0f, facingYawOffset, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        _playerInside = false;
        ResetHold();
        SetPromptActive(false);
    }

    private void EnsurePrompt()
    {
        if (_promptInstance != null || promptPrefab == null)
        {
            return;
        }

        _promptInstance = Instantiate(promptPrefab, promptAnchor != null ? promptAnchor : transform);
        _promptInstance.transform.localPosition = Vector3.zero;
        _promptInstance.transform.localRotation = Quaternion.identity;
        _promptInstance.transform.localScale = Vector3.one;
        _promptInstance.SetName(placeText);
        _promptInstance.SetHoldProgress(0f);
    }

    private void SetPromptActive(bool active)
    {
        if (_promptInstance == null)
        {
            return;
        }

        if (_promptInstance.gameObject.activeSelf != active)
        {
            _promptInstance.gameObject.SetActive(active);
        }
    }

    private void ResetHold()
    {
        _holdTimer = 0f;
        if (_promptInstance != null)
        {
            _promptInstance.SetHoldProgress(0f);
        }
    }

    private bool IsHoldPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.eKey.isPressed;
        }
#endif

        return Input.GetKey(holdKey);
    }
}

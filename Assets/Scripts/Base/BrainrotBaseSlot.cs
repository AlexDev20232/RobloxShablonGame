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

    [Header("Level Visual")]
    [SerializeField] private float levelScaleStep = 0.05f;
    [SerializeField] private float maxLevelScaleMultiplier = 1.5f;

    private PurchasePrompt _promptInstance;
    private BrainrotDefinition _occupant;
    private int _level = 1;
    private bool _unlocked = true;
    private bool _playerInside;
    private float _holdTimer;
    private float _nextUiRefreshTime;
    private double _stored;
    private Vector3 _baseOccupantScale = Vector3.one;

    private UpgradeController _money;
    private RebirthSystem _rebirth;

    public event Action<BrainrotBaseSlot> Changed;

    public int SlotIndex => slotIndex;
    public bool IsUnlocked => _unlocked;
    public bool IsOccupied => _occupant != null;
    public BrainrotDefinition Occupant => _occupant;
    public int Level => _level;
    public double StoredAmount => _stored;
    public string OccupantCollectionId => _occupant != null ? _occupant.GetCollectionId() : string.Empty;

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

        double income = _occupant.GetIncomeForLevel(_level);

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
        ApplyLevelScale();
        RefreshUI();
        NotifyChanged();
    }

    private double GetUpgradeCost()
    {
        if (_occupant == null)
        {
            return 0d;
        }

        return _occupant.GetUpgradeCostForLevel(_level);
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

        Place(def, 1, 0d, true);
        SetPromptActive(false);
    }

    public bool Place(BrainrotDefinition def, int level = 1, double storedAmount = 0d, bool unlockIndex = true)
    {
        if (def == null || _occupant != null)
        {
            return false;
        }

        _occupant = def;
        _level = Mathf.Max(1, level);
        _stored = Math.Max(0d, storedAmount);

        def.SetInBase(true);
        def.gameObject.SetActive(true);

        Transform target = placePoint != null ? placePoint : transform;
        Vector3 worldScale = def.transform.lossyScale;
        def.transform.SetParent(target, false);
        def.transform.localPosition = Vector3.zero;
        def.transform.localRotation = Quaternion.identity;

        RestoreWorldScale(def.transform, worldScale);
        _baseOccupantScale = def.transform.localScale;
        ApplyLevelScale();
        ApplyPlacedRotation(def.transform);

        if (unlockIndex)
        {
            BrainrotIndexData.Unlock(def);
        }

        BrainrotSkinApplier skin = def.GetComponentInChildren<BrainrotSkinApplier>(true);
        if (skin != null)
        {
            skin.Apply();
        }

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
        NotifyChanged();
        return true;
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

    public double CollectStored()
    {
        if (_stored <= 0d)
        {
            return 0d;
        }

        double collected = _stored;
        if (_money != null)
        {
            _money.AddCoins(collected);
        }

        _stored = 0d;
        UpdateStoredUI();
        NotifyChanged();
        return collected;
    }

    private void UpdateStoredUI()
    {
        if (!updateStoredUi || slotUI == null)
        {
            return;
        }

        slotUI.SetStored(UpgradeController.FormatCurrency(_stored));
    }

    private void ApplyLevelScale()
    {
        if (_occupant == null)
        {
            return;
        }

        float step = Mathf.Max(0f, levelScaleStep);
        float maxMultiplier = Mathf.Max(1f, maxLevelScaleMultiplier);
        float multiplier = Mathf.Min(maxMultiplier, 1f + step * Mathf.Max(0, _level - 1));
        _occupant.transform.localScale = _baseOccupantScale * multiplier;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
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

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class BrainrotBaseManager : MonoBehaviour
{
    [Header("Shared Config")]
    [SerializeField] private TemplateGameConfig gameConfig;

    [Header("Slots")]
    [SerializeField] private BrainrotBaseSlot[] slots;
    [SerializeField] private bool autoCollectSlots = true;
    [SerializeField] private int startingUnlockedSlots = 10;
    [SerializeField] private int maxSlots = 30;
    [SerializeField] private bool unlockFirstFloorByDefault = true;
    [SerializeField] private string firstFloorName = "FirstFloor";
    [SerializeField] private bool autoUnlockFromRebirth = false;

    [Header("Floors")]
    [SerializeField] private bool hideLockedFloors = true;
    [SerializeField] private string[] floorNames = { "FirstFloor", "2Floor", "3Floor" };

    [Header("Placed Brainrots")]
    [SerializeField] private BrainrotCatalog catalog;
    [SerializeField] private bool persistPlacedBrainrots = true;
    [SerializeField] private float placedSaveInterval = 3f;

    [Header("Save Keys")]
    [SerializeField] private bool persistUnlockedSlots = true;
    [SerializeField] private bool keepSavedSlotsAtLeastDefault = true;
    [SerializeField] private string unlockedSlotsKey = "BaseUnlockedSlots";
    [SerializeField] private string placedSlotsKey = "BasePlacedBrainrots";

    private int _unlockedSlots;
    private float _nextPlacedSaveTime;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public event Action SlotsChanged;

    public TemplateGameConfig GameConfig => gameConfig;
    public int UnlockedSlots => _unlockedSlots;
    public int SlotCount => slots != null ? slots.Length : 0;
    public int MaxSlots
    {
        get
        {
            int configured = Mathf.Max(1, maxSlots);
            return SlotCount > 0 ? Mathf.Clamp(configured, 1, SlotCount) : configured;
        }
    }

    private void Awake()
    {
        CollectSlots();
        SortSlots();
        ApplySharedConfig();
        BindSlotEvents();
        int defaultSlots = GetDefaultUnlockedSlotCount();
        int savedSlots = persistUnlockedSlots ? PlayerPrefs.GetInt(unlockedSlotsKey, defaultSlots) : defaultSlots;
        if (keepSavedSlotsAtLeastDefault)
        {
            savedSlots = Mathf.Max(savedSlots, defaultSlots);
        }

        ApplyUnlockedSlots(savedSlots, false);
        LoadPlacedBrainrots();
    }

    private void Update()
    {
        if (!persistPlacedBrainrots || Time.time < _nextPlacedSaveTime)
        {
            return;
        }

        SavePlacedBrainrots();
    }

    private void OnDisable()
    {
        if (persistPlacedBrainrots)
        {
            SavePlacedBrainrots();
        }
    }

    public void ApplyRebirthLevel(int rebirthLevel)
    {
        if (!autoUnlockFromRebirth)
        {
            return;
        }

        int target = Mathf.Clamp(GetDefaultUnlockedSlotCount() + Mathf.Max(0, rebirthLevel), 0, MaxSlots);
        if (target > _unlockedSlots)
        {
            SetUnlockedSlots(target);
        }
    }

    public void SetUnlockedSlots(int count)
    {
        ApplyUnlockedSlots(count, true);
    }

    public bool CanUnlockMoreSlots()
    {
        return _unlockedSlots < MaxSlots;
    }

    public int GetNextSlotNumber()
    {
        return Mathf.Clamp(_unlockedSlots + 1, 1, MaxSlots);
    }

    public bool TryUnlockNextSlot()
    {
        if (!CanUnlockMoreSlots())
        {
            return false;
        }

        SetUnlockedSlots(_unlockedSlots + 1);
        return true;
    }

    public int GetNextUnlockFloor()
    {
        if (slots == null || slots.Length == 0 || _unlockedSlots >= MaxSlots)
        {
            return 0;
        }

        int index = Mathf.Clamp(_unlockedSlots, 0, slots.Length - 1);
        return GetFloorOrder(slots[index]);
    }

    private void ApplyUnlockedSlots(int count, bool save)
    {
        int clamped = Mathf.Clamp(count, 0, MaxSlots);
        bool changed = _unlockedSlots != clamped;
        _unlockedSlots = clamped;

        if (slots == null)
        {
            return;
        }

        HashSet<Transform> visibleFloors = new HashSet<Transform>();

        for (int i = 0; i < slots.Length; i++)
        {
            BrainrotBaseSlot slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            bool unlocked = i < _unlockedSlots;
            slot.SetUnlocked(unlocked);
            slot.SetSlotIndex(i);

            if (unlocked)
            {
                Transform floor = GetFloorRoot(slot);
                if (floor != null)
                {
                    visibleFloors.Add(floor);
                }
            }
        }

        ApplyFloorVisibility(visibleFloors);

        if (save && persistUnlockedSlots)
        {
            PlayerPrefs.SetInt(unlockedSlotsKey, _unlockedSlots);
            PlayerPrefs.Save();
        }

        if (changed)
        {
            SlotsChanged?.Invoke();
        }
    }

    public IEnumerable<BrainrotBaseSlot> GetSlots()
    {
        return slots ?? Array.Empty<BrainrotBaseSlot>();
    }

    public double GetTotalIncomePerSecond()
    {
        double total = 0d;
        if (slots == null)
        {
            return total;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            BrainrotBaseSlot slot = slots[i];
            if (slot != null && slot.IsUnlocked)
            {
                total += slot.GetIncomePerSecond();
            }
        }

        return total;
    }

    public double GetTotalStoredAmount()
    {
        double total = 0d;
        if (slots == null)
        {
            return total;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            BrainrotBaseSlot slot = slots[i];
            if (slot != null)
            {
                total += slot.StoredAmount;
            }
        }

        return total;
    }

    public double CollectAllStored()
    {
        double total = 0d;
        if (slots == null)
        {
            return total;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            BrainrotBaseSlot slot = slots[i];
            if (slot != null)
            {
                total += slot.CollectStored();
            }
        }

        return total;
    }

    private void SortSlots()
    {
        CollectSlots();

        if (slots == null || slots.Length <= 1)
        {
            return;
        }

        System.Array.Sort(slots, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int floorCompare = GetFloorOrder(a).CompareTo(GetFloorOrder(b));
            if (floorCompare != 0)
            {
                return floorCompare;
            }

            int indexCompare = a.SlotIndex.CompareTo(b.SlotIndex);
            if (indexCompare != 0)
            {
                return indexCompare;
            }

            Vector3 aPosition = a.transform.position;
            Vector3 bPosition = b.transform.position;

            int yCompare = aPosition.y.CompareTo(bPosition.y);
            if (yCompare != 0)
            {
                return yCompare;
            }

            int zCompare = aPosition.z.CompareTo(bPosition.z);
            if (zCompare != 0)
            {
                return zCompare;
            }

            return aPosition.x.CompareTo(bPosition.x);
        });
    }

    private void CollectSlots()
    {
        if (!autoCollectSlots && slots != null && slots.Length > 0)
        {
            return;
        }

        slots = GetComponentsInChildren<BrainrotBaseSlot>(true);
    }

    private void BindSlotEvents()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            slots[i].Changed -= HandleSlotChanged;
            slots[i].Changed += HandleSlotChanged;
        }
    }

    private void ApplySharedConfig()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetSharedConfig(gameConfig);
            }
        }
    }

    private void HandleSlotChanged(BrainrotBaseSlot slot)
    {
        MarkPlacedSaveDirty();
    }

    private void MarkPlacedSaveDirty()
    {
        if (!persistPlacedBrainrots)
        {
            return;
        }

        _nextPlacedSaveTime = Mathf.Min(_nextPlacedSaveTime, Time.time + 0.25f);
    }

    private void SavePlacedBrainrots()
    {
        if (!persistPlacedBrainrots || slots == null)
        {
            return;
        }

        BrainrotBaseSaveData data = new BrainrotBaseSaveData();
        List<BrainrotBaseSlotSaveData> savedSlots = new List<BrainrotBaseSlotSaveData>();

        for (int i = 0; i < slots.Length; i++)
        {
            BrainrotBaseSlot slot = slots[i];
            if (slot == null || !slot.IsOccupied)
            {
                continue;
            }

            savedSlots.Add(new BrainrotBaseSlotSaveData
            {
                slotIndex = slot.SlotIndex,
                collectionId = slot.OccupantCollectionId,
                level = slot.Level,
                stored = slot.StoredAmount.ToString("R", Invariant)
            });
        }

        data.slots = savedSlots.ToArray();
        PlayerPrefs.SetString(placedSlotsKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        _nextPlacedSaveTime = Time.time + Mathf.Max(0.25f, placedSaveInterval);
    }

    private void LoadPlacedBrainrots()
    {
        if (!persistPlacedBrainrots || catalog == null || slots == null || !PlayerPrefs.HasKey(placedSlotsKey))
        {
            return;
        }

        BrainrotBaseSaveData data;
        try
        {
            data = JsonUtility.FromJson<BrainrotBaseSaveData>(PlayerPrefs.GetString(placedSlotsKey));
        }
        catch (Exception)
        {
            return;
        }

        if (data == null || data.slots == null)
        {
            return;
        }

        for (int i = 0; i < data.slots.Length; i++)
        {
            BrainrotBaseSlotSaveData slotData = data.slots[i];
            BrainrotBaseSlot slot = FindSlot(slotData.slotIndex);
            if (slot == null || !slot.IsUnlocked || string.IsNullOrWhiteSpace(slotData.collectionId))
            {
                continue;
            }

            if (!catalog.TryInstantiate(slotData.collectionId, null, out BrainrotDefinition definition))
            {
                continue;
            }

            double stored = ParseDouble(slotData.stored);
            if (!slot.Place(definition, slotData.level, stored, false))
            {
                Destroy(definition.gameObject);
            }
        }

        _nextPlacedSaveTime = Time.time + Mathf.Max(0.25f, placedSaveInterval);
    }

    private BrainrotBaseSlot FindSlot(int slotIndex)
    {
        if (slots == null)
        {
            return null;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].SlotIndex == slotIndex)
            {
                return slots[i];
            }
        }

        return null;
    }

    private static double ParseDouble(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, Invariant, out double result))
        {
            return Math.Max(0d, result);
        }

        return 0d;
    }

    public int GetDefaultUnlockedSlotCount()
    {
        int fallback = Mathf.Clamp(startingUnlockedSlots, 0, MaxSlots);
        if (!unlockFirstFloorByDefault || slots == null || slots.Length == 0)
        {
            return fallback;
        }

        int firstFloorSlots = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (IsOnFirstFloor(slots[i]))
            {
                firstFloorSlots++;
            }
        }

        return firstFloorSlots > 0 ? Mathf.Clamp(firstFloorSlots, 0, MaxSlots) : fallback;
    }

    private int GetFloorOrder(BrainrotBaseSlot slot)
    {
        Transform floor = GetFloorRoot(slot);
        if (floor == null)
        {
            return int.MaxValue;
        }

        string currentName = floor.name;
        if (!string.IsNullOrWhiteSpace(firstFloorName) && currentName == firstFloorName)
        {
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(currentName) && currentName.EndsWith("Floor", StringComparison.OrdinalIgnoreCase))
        {
            string number = currentName.Substring(0, currentName.Length - "Floor".Length);
            if (int.TryParse(number, out int parsed))
            {
                return Mathf.Max(1, parsed);
            }
        }

        return Mathf.RoundToInt(floor.position.y * 10f);
    }

    private Transform GetFloorRoot(BrainrotBaseSlot slot)
    {
        if (slot == null)
        {
            return null;
        }

        Transform current = slot.transform;
        while (current != null && current != transform)
        {
            if (IsFloorName(current.name))
            {
                return current;
            }

            current = current.parent;
        }

        return slot.transform.parent;
    }

    private void ApplyFloorVisibility(HashSet<Transform> visibleFloors)
    {
        if (!hideLockedFloors || floorNames == null)
        {
            return;
        }

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child == null || child == transform || !IsConfiguredFloorName(child.name))
            {
                continue;
            }

            bool visible = visibleFloors != null && visibleFloors.Contains(child);
            if (child.gameObject.activeSelf != visible)
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    private bool IsFloorName(string value)
    {
        if (IsConfiguredFloorName(value))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(value) &&
               value.EndsWith("Floor", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsConfiguredFloorName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || floorNames == null)
        {
            return false;
        }

        for (int i = 0; i < floorNames.Length; i++)
        {
            if (value == floorNames[i])
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOnFirstFloor(BrainrotBaseSlot slot)
    {
        if (slot == null || string.IsNullOrWhiteSpace(firstFloorName))
        {
            return false;
        }

        Transform current = slot.transform;
        while (current != null && current != transform)
        {
            if (current.name == firstFloorName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}

[Serializable]
public class BrainrotBaseSaveData
{
    public BrainrotBaseSlotSaveData[] slots;
}

[Serializable]
public class BrainrotBaseSlotSaveData
{
    public int slotIndex;
    public string collectionId;
    public int level = 1;
    public string stored = "0";
}

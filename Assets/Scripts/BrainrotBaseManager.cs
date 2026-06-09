using System.Collections.Generic;
using UnityEngine;

public class BrainrotBaseManager : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private BrainrotBaseSlot[] slots;
    [SerializeField] private int startingUnlockedSlots = 10;
    [SerializeField] private int maxSlots = 30;
    [SerializeField] private bool unlockFirstFloorByDefault = true;
    [SerializeField] private string firstFloorName = "FirstFloor";
    [SerializeField] private bool autoUnlockFromRebirth = false;

    [Header("Save Keys")]
    [SerializeField] private bool persistUnlockedSlots = true;
    [SerializeField] private bool keepSavedSlotsAtLeastDefault = true;
    [SerializeField] private string unlockedSlotsKey = "BaseUnlockedSlots";

    private int _unlockedSlots;

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
        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<BrainrotBaseSlot>(true);
        }

        SortSlots();
        int defaultSlots = GetDefaultUnlockedSlotCount();
        int savedSlots = persistUnlockedSlots ? PlayerPrefs.GetInt(unlockedSlotsKey, defaultSlots) : defaultSlots;
        if (keepSavedSlotsAtLeastDefault)
        {
            savedSlots = Mathf.Max(savedSlots, defaultSlots);
        }

        ApplyUnlockedSlots(savedSlots, false);
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

    private void ApplyUnlockedSlots(int count, bool save)
    {
        int clamped = Mathf.Clamp(count, 0, MaxSlots);
        _unlockedSlots = clamped;

        if (slots == null)
        {
            return;
        }

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
        }

        if (save && persistUnlockedSlots)
        {
            PlayerPrefs.SetInt(unlockedSlotsKey, _unlockedSlots);
            PlayerPrefs.Save();
        }
    }

    public IEnumerable<BrainrotBaseSlot> GetSlots()
    {
        return slots;
    }

    private void SortSlots()
    {
        if (slots == null || slots.Length <= 1)
        {
            return;
        }

        System.Array.Sort(slots, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

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

    private int GetDefaultUnlockedSlotCount()
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

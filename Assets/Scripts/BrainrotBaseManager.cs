using System.Collections.Generic;
using UnityEngine;

public class BrainrotBaseManager : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private BrainrotBaseSlot[] slots;
    [SerializeField] private int startingUnlockedSlots = 10;
    [SerializeField] private int maxSlots = 30;
    [SerializeField] private bool autoUnlockFromRebirth = true;

    private int _unlockedSlots;

    public int UnlockedSlots => _unlockedSlots;
    public int MaxSlots => Mathf.Max(1, maxSlots);

    private void Awake()
    {
        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<BrainrotBaseSlot>(true);
        }

        SortSlots();
        SetUnlockedSlots(Mathf.Clamp(startingUnlockedSlots, 0, MaxSlots));
    }

    public void ApplyRebirthLevel(int rebirthLevel)
    {
        if (!autoUnlockFromRebirth)
        {
            return;
        }

        int target = Mathf.Clamp(10 + Mathf.Max(0, rebirthLevel), 0, MaxSlots);
        if (target > _unlockedSlots)
        {
            SetUnlockedSlots(target);
        }
    }

    public void SetUnlockedSlots(int count)
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
            return a.SlotIndex.CompareTo(b.SlotIndex);
        });
    }
}

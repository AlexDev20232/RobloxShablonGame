using UnityEngine;
using UnityEngine.UI;

public class BrainrotInventory : MonoBehaviour
{
    [Header("Carry")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Button dropButton;

    [Header("Inventory")]
    [SerializeField] private int maxSlots = 7;
    [SerializeField] private Transform inventoryRoot;
    [SerializeField] private BrainrotInventorySlotUI[] slotUIs;
    [SerializeField] private bool hideEmptySlots = true;

    private BrainrotDefinition[] _slots;
    private BrainrotDefinition _carried;
    private int _selectedSlot = -1;
    private int _carriedSlotIndex = -1;

    public bool IsCarrying => _carried != null;

    public BrainrotDefinition CurrentCarried => _carried;

    private void Awake()
    {
        int size = Mathf.Max(1, maxSlots);
        _slots = new BrainrotDefinition[size];

        if (dropButton != null)
        {
            dropButton.gameObject.SetActive(false);
            dropButton.onClick.AddListener(DropCarried);
        }

        BindSlots();
        RefreshSlotsUI();
    }

    private void BindSlots()
    {
        if (slotUIs == null || slotUIs.Length == 0)
        {
            slotUIs = GetComponentsInChildren<BrainrotInventorySlotUI>(true);
        }

        if (slotUIs == null)
        {
            return;
        }

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null)
            {
                continue;
            }

            slotUIs[i].Bind(this, i);
        }
    }

    public bool Pickup(BrainrotDefinition target)
    {
        if (target == null || _carried != null || holdPoint == null)
        {
            return false;
        }

        _carried = target;
        _carriedSlotIndex = -1;
        _carried.SetCarried(true);
        BrainrotIndexData.Unlock(_carried);

        AttachToHold(_carried.transform);
        SetBrainrotTimer(_carried, false);

        return true;
    }

    public bool StoreCarried()
    {
        return TryStoreCarried(out _);
    }

    public bool TryStoreCarried(out BrainrotDefinition stored)
    {
        stored = null;

        if (_carried == null)
        {
            return false;
        }

        if (_carriedSlotIndex >= 0)
        {
            return false;
        }

        int slot = FindFirstEmptySlot();
        if (slot < 0)
        {
            return false;
        }

        BrainrotDefinition def = _carried;
        _carried = null;
        _selectedSlot = -1;
        _carriedSlotIndex = -1;

        def.SetCarried(false);
        def.SetInInventory(true);

        StashToInventoryRoot(def);
        SetBrainrotTimer(def, false);

        _slots[slot] = def;
        RefreshSlotUI(slot);
        ClearSelection();

        stored = def;
        return true;
    }

    public void DropCarried()
    {
        if (_carried == null || holdPoint == null)
        {
            return;
        }

        BrainrotDefinition def = _carried;
        _carried = null;
        int slotIndex = _carriedSlotIndex;
        _carriedSlotIndex = -1;

        def.SetCarried(false);
        def.transform.SetParent(null);
        def.transform.position = holdPoint.position;
        def.transform.rotation = holdPoint.rotation;
        def.gameObject.SetActive(true);
        SetBrainrotTimer(def, true);

        if (slotIndex >= 0 && slotIndex < _slots.Length && _slots[slotIndex] == def)
        {
            _slots[slotIndex] = null;
            RefreshSlotUI(slotIndex);
            if (_selectedSlot == slotIndex)
            {
                ClearSelection();
            }
        }

    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _slots.Length)
        {
            return;
        }

        if (_carried != null)
        {
            if (_carriedSlotIndex >= 0)
            {
                if (_carriedSlotIndex == index)
                {
                    ReturnCarriedToInventorySlot();
                    _selectedSlot = -1;
                    UpdateSelectionUI();
                    return;
                }

                ReturnCarriedToInventorySlot();
            }
            else
            {
                return;
            }
        }

        BrainrotDefinition def = _slots[index];
        if (def == null || holdPoint == null)
        {
            _selectedSlot = -1;
            UpdateSelectionUI();
            return;
        }

        _selectedSlot = index;
        UpdateSelectionUI();

        def.SetInInventory(false);
        def.SetCarried(true);
        def.gameObject.SetActive(true);
        _carried = def;
        _carriedSlotIndex = index;

        AttachToHold(def.transform);
        SetBrainrotTimer(def, false);
    }

    private void AttachToHold(Transform target)
    {
        target.SetParent(holdPoint, false);
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
    }

    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private void StashToInventoryRoot(BrainrotDefinition def)
    {
        if (def == null)
        {
            return;
        }

        Transform parent = inventoryRoot != null ? inventoryRoot : transform;
        def.transform.SetParent(parent, false);
        def.transform.localPosition = Vector3.zero;
        def.transform.localRotation = Quaternion.identity;
        def.gameObject.SetActive(false);
    }

    private void ReturnCarriedToInventorySlot()
    {
        if (_carried == null || _carriedSlotIndex < 0 || _carriedSlotIndex >= _slots.Length)
        {
            _carried = null;
            _carriedSlotIndex = -1;
            return;
        }

        BrainrotDefinition def = _carried;
        if (_slots[_carriedSlotIndex] != def)
        {
            _carried = null;
            _carriedSlotIndex = -1;
            return;
        }

        def.SetCarried(false);
        def.SetInInventory(true);
        StashToInventoryRoot(def);
        SetBrainrotTimer(def, false);

        _carried = null;
        _carriedSlotIndex = -1;
    }

    private void ClearSelection()
    {
        _selectedSlot = -1;
        UpdateSelectionUI();
    }

    private void UpdateSelectionUI()
    {
        if (slotUIs == null)
        {
            return;
        }

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null)
            {
                continue;
            }

            slotUIs[i].SetSelected(i == _selectedSlot);
        }
    }

    private void RefreshSlotsUI()
    {
        if (slotUIs == null)
        {
            return;
        }

        for (int i = 0; i < slotUIs.Length; i++)
        {
            RefreshSlotUI(i);
        }
    }

    private void RefreshSlotUI(int index)
    {
        if (slotUIs == null || index < 0 || index >= slotUIs.Length)
        {
            return;
        }

        BrainrotInventorySlotUI ui = slotUIs[index];
        if (ui == null)
        {
            return;
        }

        BrainrotDefinition def = index < _slots.Length ? _slots[index] : null;
        ui.SetName(def != null ? def.displayName : string.Empty);
        if (hideEmptySlots)
        {
            ui.SetVisible(def != null);
        }
    }

    private static void SetBrainrotTimer(BrainrotDefinition def, bool visible)
    {
        if (def == null)
        {
            return;
        }

        BrainrotLifetime lifetime = def.GetComponent<BrainrotLifetime>();
        if (lifetime != null)
        {
            lifetime.SetPaused(!visible);
        }

        BrainrotUI ui = def.GetComponentInChildren<BrainrotUI>();
        if (ui != null)
        {
            ui.SetTimerVisible(visible);
        }
    }

    public bool TryTakeCarriedForPlacement(out BrainrotDefinition def)
    {
        def = _carried;
        if (def == null)
        {
            return false;
        }

        if (_carriedSlotIndex >= 0 && _carriedSlotIndex < _slots.Length)
        {
            if (_slots[_carriedSlotIndex] == def)
            {
                _slots[_carriedSlotIndex] = null;
                RefreshSlotUI(_carriedSlotIndex);
                if (_selectedSlot == _carriedSlotIndex)
                {
                    _selectedSlot = -1;
                    UpdateSelectionUI();
                }
            }
        }

        _carried = null;
        _carriedSlotIndex = -1;
        return true;
    }
}

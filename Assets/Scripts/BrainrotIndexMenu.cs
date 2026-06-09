using System.Collections.Generic;
using UnityEngine;

public class BrainrotIndexMenu : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BrainrotDefinition[] entries;

    [Header("UI")]
    [SerializeField] private BrainrotIndexItemUI itemPrefab;
    [SerializeField] private Transform itemsRoot;

    [Header("Filter")]
    [SerializeField] private BrainrotType currentType = BrainrotType.Normal;
    [SerializeField] private bool showAllTypes = false;

    private readonly List<BrainrotIndexItemUI> _spawned = new List<BrainrotIndexItemUI>();

    private void OnEnable()
    {
        BrainrotIndexData.OnChanged += RebuildList;
        RebuildList();
    }

    private void OnDisable()
    {
        BrainrotIndexData.OnChanged -= RebuildList;
    }

    public void SetTypeFilter(int typeIndex)
    {
        if (typeIndex < 0)
        {
            showAllTypes = true;
        }
        else
        {
            showAllTypes = false;
            currentType = (BrainrotType)typeIndex;
        }

        RebuildList();
    }

    public void SetTypeFilter(BrainrotType type)
    {
        showAllTypes = false;
        currentType = type;
        RebuildList();
    }

    public void ShowAllTypes()
    {
        showAllTypes = true;
        RebuildList();
    }

    public void RebuildList()
    {
        ClearItems();

        if (entries == null || entries.Length == 0 || itemPrefab == null || itemsRoot == null)
        {
            return;
        }

        List<BrainrotDefinition> list = new List<BrainrotDefinition>(entries);
        list.RemoveAll(def => def == null);

        if (!showAllTypes)
        {
            list.RemoveAll(def => def.type != currentType);
        }

        list.Sort(CompareByRarity);

        for (int i = 0; i < list.Count; i++)
        {
            BrainrotDefinition def = list[i];
            BrainrotIndexItemUI item = Instantiate(itemPrefab, itemsRoot);
            item.Bind(def, BrainrotIndexData.IsUnlocked(def));
            _spawned.Add(item);
        }
    }

    private void ClearItems()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                Destroy(_spawned[i].gameObject);
            }
        }

        _spawned.Clear();
    }

    private static int CompareByRarity(BrainrotDefinition a, BrainrotDefinition b)
    {
        if (a == null && b == null)
        {
            return 0;
        }

        if (a == null)
        {
            return 1;
        }

        if (b == null)
        {
            return -1;
        }

        int rarityCompare = a.rarity.CompareTo(b.rarity);
        if (rarityCompare != 0)
        {
            return rarityCompare;
        }

        return string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal);
    }
}

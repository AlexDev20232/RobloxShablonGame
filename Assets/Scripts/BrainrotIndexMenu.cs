using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BrainrotIndexMenu : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BrainrotDefinition[] entries;
    [SerializeField] private bool autoPopulateEntries = true;
    [SerializeField] private bool includeActiveDefinitions = true;
    [SerializeField] private bool includeSpawnerPrefabs = true;
    [SerializeField] private string editorPrefabFolder = "Assets/BrainrotPrefab";

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

        if (itemPrefab == null || itemsRoot == null)
        {
            return;
        }

        List<BrainrotDefinition> list = BuildEntryList();
        if (list.Count == 0)
        {
            return;
        }

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

    private List<BrainrotDefinition> BuildEntryList()
    {
        List<BrainrotDefinition> list = new List<BrainrotDefinition>();

        AddEntries(list, entries);
        if (!autoPopulateEntries)
        {
            return list;
        }

        if (includeActiveDefinitions)
        {
            AddEntries(list, BrainrotDefinition.ActiveList);
        }

        if (includeSpawnerPrefabs)
        {
            AddSpawnerPrefabEntries(list);
        }

#if UNITY_EDITOR
        AddEditorPrefabFolderEntries(list);
#endif

        return list;
    }

    private void AddSpawnerPrefabEntries(List<BrainrotDefinition> list)
    {
        BrainrotSpawner[] spawners = FindObjectsByType<BrainrotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            BrainrotSpawner spawner = spawners[i];
            if (spawner == null || spawner.prefabs == null)
            {
                continue;
            }

            for (int j = 0; j < spawner.prefabs.Count; j++)
            {
                GameObject prefab = spawner.prefabs[j];
                if (prefab == null)
                {
                    continue;
                }

                AddEntry(list, prefab.GetComponentInChildren<BrainrotDefinition>(true));
            }
        }
    }

    private static void AddEntries(List<BrainrotDefinition> list, IEnumerable<BrainrotDefinition> source)
    {
        if (source == null)
        {
            return;
        }

        foreach (BrainrotDefinition def in source)
        {
            AddEntry(list, def);
        }
    }

    private static void AddEntry(List<BrainrotDefinition> list, BrainrotDefinition def)
    {
        if (def == null)
        {
            return;
        }

        string id = def.GetIndexId();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].GetIndexId() == id)
            {
                return;
            }
        }

        list.Add(def);
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Entries From Prefab Folder")]
    private void RefreshEntriesFromPrefabFolder()
    {
        List<BrainrotDefinition> list = new List<BrainrotDefinition>();
        AddEditorPrefabFolderEntries(list);
        entries = list.ToArray();
        EditorUtility.SetDirty(this);
    }

    private void AddEditorPrefabFolderEntries(List<BrainrotDefinition> list)
    {
        if (string.IsNullOrWhiteSpace(editorPrefabFolder) || !AssetDatabase.IsValidFolder(editorPrefabFolder))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { editorPrefabFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            AddEntry(list, prefab.GetComponentInChildren<BrainrotDefinition>(true));
        }
    }
#endif
}

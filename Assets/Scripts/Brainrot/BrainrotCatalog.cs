using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Brainrot Template/Brainrot Catalog")]
public class BrainrotCatalog : ScriptableObject
{
    [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

    public IReadOnlyList<GameObject> Prefabs => prefabs;

    public void AddDefinitionsTo(List<BrainrotDefinition> target)
    {
        if (target == null || prefabs == null)
        {
            return;
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            BrainrotDefinition definition = GetDefinition(prefabs[i]);
            if (definition != null)
            {
                AddUnique(target, definition);
            }
        }
    }

    public bool TryGetDefinition(string collectionId, out BrainrotDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(collectionId) || prefabs == null)
        {
            return false;
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            BrainrotDefinition candidate = GetDefinition(prefabs[i]);
            if (candidate == null)
            {
                continue;
            }

            if (Matches(candidate, collectionId, out _))
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryInstantiate(string collectionId, Transform parent, out BrainrotDefinition instance)
    {
        instance = null;
        if (string.IsNullOrWhiteSpace(collectionId) || prefabs == null)
        {
            return false;
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            BrainrotDefinition definition = GetDefinition(prefab);
            if (definition == null)
            {
                continue;
            }

            if (!Matches(definition, collectionId, out BrainrotType requestedType))
            {
                continue;
            }

            GameObject spawned = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
            instance = spawned.GetComponentInChildren<BrainrotDefinition>(true);
            if (instance != null)
            {
                instance.type = requestedType;
            }

            return instance != null;
        }

        return false;
    }

    private static BrainrotDefinition GetDefinition(GameObject prefab)
    {
        return prefab != null ? prefab.GetComponentInChildren<BrainrotDefinition>(true) : null;
    }

    private static bool Matches(BrainrotDefinition definition, string collectionId, out BrainrotType requestedType)
    {
        requestedType = definition != null ? definition.type : BrainrotType.Normal;
        if (definition == null || string.IsNullOrWhiteSpace(collectionId))
        {
            return false;
        }

        if (definition.GetCollectionId() == collectionId || definition.GetIndexId() == collectionId)
        {
            return true;
        }

        int separator = collectionId.IndexOf(':');
        if (separator <= 0 || separator >= collectionId.Length - 1)
        {
            return false;
        }

        string typeName = collectionId.Substring(0, separator);
        string id = collectionId.Substring(separator + 1);
        if (!System.Enum.TryParse(typeName, true, out BrainrotType parsedType))
        {
            return false;
        }

        requestedType = parsedType;
        return definition.GetIndexId() == id;
    }

    private static void AddUnique(List<BrainrotDefinition> target, BrainrotDefinition definition)
    {
        string id = definition.GetCollectionId();
        for (int i = 0; i < target.Count; i++)
        {
            if (target[i] != null && target[i].GetCollectionId() == id)
            {
                return;
            }
        }

        target.Add(definition);
    }
}

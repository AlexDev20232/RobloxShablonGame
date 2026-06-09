using System;
using UnityEngine;

public static class BrainrotIndexData
{
    public static event Action OnChanged;

    private const string KeyPrefix = "BrainrotIndex_";

    public static bool IsUnlocked(BrainrotDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        return IsUnlocked(definition.GetIndexId());
    }

    public static bool IsUnlocked(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1;
    }

    public static void Unlock(BrainrotDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        Unlock(definition.GetIndexId());
    }

    public static void Unlock(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        string key = KeyPrefix + id;
        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            return;
        }

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}

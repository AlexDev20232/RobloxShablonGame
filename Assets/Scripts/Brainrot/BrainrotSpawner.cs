using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrainrotSpawner : MonoBehaviour
{
    [Header("Spawn Pool")]
    public BrainrotRarity rarity = BrainrotRarity.Common;
    public bool overridePrefabRarity = false;
    public List<GameObject> prefabs = new List<GameObject>();
    public int maxActive = 8;

    [Header("Lifetime")]
    public float initialLifetimeMin = 30f;
    public float initialLifetimeMax = 60f;
    public float respawnLifetime = 60f;
    public float respawnDelay = 3f;

    [Header("Spawn Area")]
    public BoxCollider spawnArea;
    public Transform[] spawnPoints;
    public float minSpawnSpacing = 1.5f;
    public int spawnAttempts = 20;
    public Vector2 randomYRotation = new Vector2(0f, 360f);

    [Header("UI")]
    public GameObject uiCanvasPrefab;
    public string uiAnchorName = "UIAnchor";

    private readonly List<BrainrotLifetime> _active = new List<BrainrotLifetime>();

    private void Awake()
    {
        if (spawnArea == null)
        {
            spawnArea = GetComponent<BoxCollider>();
        }
    }

    private void Start()
    {
        int count = Mathf.Max(0, maxActive);
        for (int i = 0; i < count; i++)
        {
            float lifetime = GetInitialLifetime();
            SpawnBrainrot(lifetime);
        }
    }

    private float GetInitialLifetime()
    {
        float min = Mathf.Max(1f, initialLifetimeMin);
        float max = Mathf.Max(min, initialLifetimeMax);
        return Random.Range(min, max);
    }

    private void SpawnBrainrot(float lifetime)
    {
        PruneNulls();

        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[BrainrotSpawner] No prefabs assigned on '{name}'.", this);
            return;
        }

        if (!TryGetSpawnPosition(out Vector3 pos))
        {
            Debug.LogWarning($"[BrainrotSpawner] Failed to find spawn position on '{name}'.", this);
            return;
        }

        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        float yRotation = Random.Range(randomYRotation.x, randomYRotation.y);
        Quaternion rotation = Quaternion.Euler(0f, yRotation, 0f);
        GameObject instance = Instantiate(prefab, pos, rotation);

        BrainrotDefinition definition = instance.GetComponent<BrainrotDefinition>();
        if (definition == null)
        {
            definition = instance.AddComponent<BrainrotDefinition>();
        }

        if (overridePrefabRarity)
        {
            definition.rarity = rarity;
        }

        BrainrotUI uiInstance = null;
        if (uiCanvasPrefab != null)
        {
            Transform anchor = definition.uiAnchor != null ? definition.uiAnchor : FindAnchor(instance.transform);
            GameObject uiObject = Instantiate(uiCanvasPrefab, anchor != null ? anchor : instance.transform);
            uiObject.transform.localPosition = Vector3.zero;
            uiObject.transform.localRotation = Quaternion.identity;
            uiObject.transform.localScale = Vector3.one;
            uiInstance = uiObject.GetComponent<BrainrotUI>();
        }

        BrainrotLifetime lifetimeComp = instance.GetComponent<BrainrotLifetime>();
        if (lifetimeComp == null)
        {
            lifetimeComp = instance.AddComponent<BrainrotLifetime>();
        }

        lifetimeComp.Initialize(definition, uiInstance, lifetime, HandleExpired);
        _active.Add(lifetimeComp);
    }

    private void HandleExpired(BrainrotLifetime lifetime)
    {
        _active.Remove(lifetime);

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (respawnDelay <= 0f)
        {
            SpawnBrainrot(respawnLifetime);
            return;
        }

        StartCoroutine(RespawnAfterDelay(respawnDelay));
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBrainrot(respawnLifetime);
    }

    private void PruneNulls()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i] == null)
            {
                _active.RemoveAt(i);
            }
        }
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            for (int attempt = 0; attempt < spawnAttempts; attempt++)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (point == null)
                {
                    continue;
                }

                Vector3 candidate = point.position;
                if (IsPositionClear(candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        if (spawnArea == null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                for (int attempt = 0; attempt < spawnAttempts; attempt++)
                {
                    Bounds b = rend.bounds;
                    Vector3 candidate = new Vector3(
                        Random.Range(b.min.x, b.max.x),
                        b.max.y,
                        Random.Range(b.min.z, b.max.z));
                    if (IsPositionClear(candidate))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            position = Vector3.zero;
            return false;
        }

        Vector3 size = spawnArea.size;
        Vector3 center = spawnArea.center;

        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            Vector3 local = center;
            local.x += Random.Range(-size.x * 0.5f, size.x * 0.5f);
            local.z += Random.Range(-size.z * 0.5f, size.z * 0.5f);
            local.y += size.y * 0.5f;

            Vector3 candidate = spawnArea.transform.TransformPoint(local);
            if (IsPositionClear(candidate))
            {
                position = candidate;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsPositionClear(Vector3 candidate)
    {
        if (minSpawnSpacing <= 0f || _active.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < _active.Count; i++)
        {
            BrainrotLifetime life = _active[i];
            if (life == null)
            {
                continue;
            }

            float dist = Vector3.Distance(life.transform.position, candidate);
            if (dist < minSpawnSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private Transform FindAnchor(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(uiAnchorName))
        {
            Transform found = FindChildByName(root, uiAnchorName);
            if (found != null)
            {
                return found;
            }
        }

        return root;
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildByName(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

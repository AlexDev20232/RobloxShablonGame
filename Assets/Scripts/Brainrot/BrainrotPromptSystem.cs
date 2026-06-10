using UnityEngine;

public class BrainrotPromptSystem : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private PurchasePrompt promptPrefab;
    [SerializeField] private bool hideWhenNoTarget = true;

    [Header("Distance")]
    [SerializeField] private Transform player;
    [SerializeField] private float maxDistance = 6f;

    [Header("Visibility")]
    [SerializeField] private bool requireInView = true;
    [SerializeField] private bool useCameraFov = true;
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Header("Position")]
    [SerializeField] private bool useUiAnchor = false;

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.1f;

    private PurchasePrompt _promptInstance;
    private BrainrotDefinition _current;
    private Camera _camera;
    private float _nextScanTime;

    public BrainrotDefinition CurrentTarget => _current;
    public PurchasePrompt PromptInstance => _promptInstance;

    private void Awake()
    {
        if (player == null)
        {
            player = transform;
        }

        _camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
    }

    private void OnEnable()
    {
        EnsurePrompt();
        SetPromptActive(false);
        _nextScanTime = 0f;
    }

    private void Update()
    {
        if (_camera == null)
        {
            _camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + Mathf.Max(0.02f, scanInterval);
            UpdateTarget();
        }
    }

    private void UpdateTarget()
    {
        BrainrotDefinition best = FindBestCandidate();
        if (best == _current)
        {
            return;
        }

        _current = best;

        if (_current == null)
        {
            SetPromptActive(false);
            return;
        }

        EnsurePrompt();
        SetPromptActive(true);
        UpdatePromptForCurrent();
    }

    private void UpdatePromptForCurrent()
    {
        if (_current == null || _promptInstance == null)
        {
            return;
        }

        _promptInstance.SetName(GetDisplayName(_current));

        Vector3 worldCenter = useUiAnchor && _current.uiAnchor != null
            ? _current.uiAnchor.position
            : GetCenter(_current);

        Vector3 localPos = _current.transform.InverseTransformPoint(worldCenter);
        _promptInstance.Attach(_current.transform, localPos);
    }

    private BrainrotDefinition FindBestCandidate()
    {
        var list = BrainrotDefinition.ActiveList;
        if (list == null || list.Count == 0)
        {
            return null;
        }

        Vector3 origin = GetDistanceOrigin();
        Vector3 rayOrigin = GetRayOrigin();
        float maxDistSqr = maxDistance * maxDistance;

        BrainrotDefinition best = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            BrainrotDefinition def = list[i];
            if (def == null || !def.isActiveAndEnabled)
            {
                continue;
            }

            if (def.IsCarried || def.IsInInventory || def.IsInBase)
            {
                continue;
            }

            Vector3 center = GetCenter(def);
            float distSqr = (center - origin).sqrMagnitude;
            if (distSqr > maxDistSqr)
            {
                continue;
            }

            if (requireInView && !IsInView(center, rayOrigin))
            {
                continue;
            }

            if (requireLineOfSight && !HasLineOfSight(center, def.transform, rayOrigin))
            {
                continue;
            }

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                best = def;
            }
        }

        return best;
    }

    private Vector3 GetDistanceOrigin()
    {
        if (player != null)
        {
            return player.position;
        }

        return transform.position;
    }

    private Vector3 GetRayOrigin()
    {
        if (_camera != null)
        {
            return _camera.transform.position;
        }

        if (player != null)
        {
            return player.position;
        }

        return transform.position;
    }

    private bool IsInView(Vector3 worldPos, Vector3 origin)
    {
        Vector3 toTarget = worldPos - origin;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        Vector3 forward = _camera != null
            ? _camera.transform.forward
            : (player != null ? player.forward : transform.forward);

        float limit = viewAngle;
        if (useCameraFov && _camera != null)
        {
            limit = _camera.fieldOfView * 0.5f;
        }

        return Vector3.Angle(forward, toTarget) <= limit;
    }

    private bool HasLineOfSight(Vector3 worldPos, Transform target, Vector3 origin)
    {
        Vector3 dir = worldPos - origin;
        float dist = dir.magnitude;
        if (dist <= 0.001f)
        {
            return true;
        }

        dir /= dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.transform;
            return hitTransform == target || hitTransform.IsChildOf(target);
        }

        return true;
    }

    private static Vector3 GetCenter(BrainrotDefinition def)
    {
        if (def == null)
        {
            return Vector3.zero;
        }

        Renderer rend = def.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            return rend.bounds.center;
        }

        Collider col = def.GetComponentInChildren<Collider>();
        if (col != null)
        {
            return col.bounds.center;
        }

        return def.transform.position;
    }

    private static string GetDisplayName(BrainrotDefinition def)
    {
        if (def == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(def.displayName))
        {
            return def.displayName;
        }

        if (!string.IsNullOrWhiteSpace(def.title))
        {
            return def.title;
        }

        return def.name;
    }

    private void EnsurePrompt()
    {
        if (_promptInstance != null || promptPrefab == null)
        {
            return;
        }

        _promptInstance = Instantiate(promptPrefab);
    }

    private void SetPromptActive(bool active)
    {
        if (_promptInstance == null)
        {
            return;
        }

        if (!hideWhenNoTarget)
        {
            active = true;
        }

        if (_promptInstance.gameObject.activeSelf != active)
        {
            _promptInstance.gameObject.SetActive(active);
        }
    }

    public void SetHoldProgress(float progress)
    {
        if (_promptInstance == null)
        {
            return;
        }

        _promptInstance.SetHoldProgress(progress);
    }

    private void OnValidate()
    {
        if (maxDistance < 0f)
        {
            maxDistance = 0f;
        }

        if (scanInterval < 0.02f)
        {
            scanInterval = 0.02f;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchasePrompt : MonoBehaviour
{
    [Header("Scale")]
    public float minSize = 0.5f;
    public float maxSize = 2.0f;
    public float maxDistance = 15f;

    [Tooltip("Smooth rotation speed (0 = instant).")]
    public float rotationSpeed = 0f;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public Image holdFillImage;

    private Transform _cam;
    private Vector3 _initialScale;
    private Vector3 _localPos;

    private void Awake()
    {
        CacheCamera();
        _initialScale = transform.localScale;
        _localPos = transform.localPosition;
        SnapToCamera();
        SetHoldProgress(0f);
    }

    private void OnEnable()
    {
        CacheCamera();
        SnapToCamera();
        SetHoldProgress(0f);
    }

    public void Attach(Transform parent, Vector3 localPosition)
    {
        if (parent != null)
        {
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
        }

        _localPos = transform.localPosition;
        SnapToCamera();
    }

    public void SetName(string value)
    {
        if (nameText != null)
        {
            nameText.text = value;
        }
    }

    private void Update()
    {
        CacheCamera();
        if (_cam == null)
        {
            return;
        }

        if (transform.parent)
            transform.position = transform.parent.TransformPoint(_localPos);

        UpdateOrientation();
        UpdateScale();
    }

    private void UpdateOrientation()
    {
        Quaternion target = Quaternion.LookRotation(_cam.position - transform.position);

        transform.rotation = rotationSpeed <= 0
            ? target
            : Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private void SnapToCamera()
    {
        if (_cam == null)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(_cam.position - transform.position);
    }

    private void UpdateScale()
    {
        float dist  = Vector3.Distance(transform.position, _cam.position);
        float t     = Mathf.Clamp01(dist / maxDistance);
        float scale = Mathf.Lerp(minSize, maxSize, t);

        transform.localScale = _initialScale * scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        Gizmos.DrawLine(transform.position,
                        transform.position + transform.forward * 0.5f);
    }

    private void CacheCamera()
    {
        if (_cam == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                _cam = cam.transform;
            }
        }
    }

    public void SetHoldProgress(float value)
    {
        if (holdFillImage == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(value);
        holdFillImage.fillAmount = clamped;
        if (!holdFillImage.gameObject.activeSelf && clamped > 0f)
        {
            holdFillImage.gameObject.SetActive(true);
        }
        else if (holdFillImage.gameObject.activeSelf && clamped <= 0f)
        {
            holdFillImage.gameObject.SetActive(false);
        }
    }
}
    

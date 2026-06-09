using UnityEngine;

[DisallowMultipleComponent]
public class BrainrotIdleAnimator : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 1.35f;
    [SerializeField] private float yawAngle = 7f;
    [SerializeField] private float tiltAngle = 3f;
    [SerializeField] private bool randomizePhase = true;

    private Transform _lastParent;
    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private float _phase;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        _phase = randomizePhase ? Random.value * Mathf.PI * 2f : 0f;
        CacheBasePose();
    }

    private void OnEnable()
    {
        CacheBasePose();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (target.parent != _lastParent)
        {
            CacheBasePose();
        }

        float t = Time.time * Mathf.Max(0.01f, bobSpeed) + _phase;
        float bob = Mathf.Sin(t) * Mathf.Max(0f, bobAmplitude);
        float yaw = Mathf.Sin(t * 0.55f) * yawAngle;
        float tilt = Mathf.Sin(t * 0.9f) * tiltAngle;

        target.localPosition = _baseLocalPosition + Vector3.up * bob;
        target.localRotation = _baseLocalRotation * Quaternion.Euler(tilt, yaw, 0f);
    }

    private void OnDisable()
    {
        if (target == null)
        {
            return;
        }

        target.localPosition = _baseLocalPosition;
        target.localRotation = _baseLocalRotation;
    }

    private void CacheBasePose()
    {
        if (target == null)
        {
            return;
        }

        _lastParent = target.parent;
        _baseLocalPosition = target.localPosition;
        _baseLocalRotation = target.localRotation;
    }
}

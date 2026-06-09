using UnityEngine;

public class WaveMover : MonoBehaviour
{
    [Header("Move Settings")]
    public float speed = 3f;
    public Transform targetPoint;
    public Vector3 direction = Vector3.forward;
    public bool useTargetPoint = true;

    [Header("Despawn Settings")]
    public float reachDistance = 0.3f;
    public float maxLifeTime = 30f;

    private float _lifeTimer;

    private void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (maxLifeTime > 0f && _lifeTimer >= maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (useTargetPoint && targetPoint != null)
        {
            Vector3 toTarget = targetPoint.position - transform.position;
            float dist = toTarget.magnitude;

            if (dist <= reachDistance)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 moveDir = toTarget.normalized;
            transform.position += moveDir * speed * Time.deltaTime;
        }
        else
        {
            Vector3 moveDir = direction;
            if (moveDir.sqrMagnitude < 0.0001f) moveDir = Vector3.forward;
            moveDir.Normalize();

            transform.position += moveDir * speed * Time.deltaTime;

            if (targetPoint != null)
            {
                float dist = Vector3.Distance(transform.position, targetPoint.position);
                if (dist <= reachDistance)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    public void Configure(Transform target, float newSpeed, Vector3 newDirection, bool useTarget)
    {
        targetPoint = target;
        speed = newSpeed;
        direction = newDirection;
        useTargetPoint = useTarget;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Очень простая орбитальная камера:
/// - вращение вокруг цели (yaw/pitch)
/// - сглаженное следование
/// - зум колесом
/// - опциональная коллизия (SphereCast)
/// - вращение по ЛКМ (mouseButton = 0)
/// - (важно) не вращает, если клик по UI
/// </summary>
[DisallowMultipleComponent]
public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Distance / Zoom")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 14f;

    [Tooltip("Скорость зума. Для InputSystem скролл часто 120 за 'щелчок'.")]
    [SerializeField] private float zoomSensitivity = 0.01f;

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.05f;

    [Header("Mouse Control")]
    [Tooltip("Если true - камера вращается только когда зажата кнопка мыши.")]
    [SerializeField] private bool rotateOnlyWhileMouseHeld = true;

    [Tooltip("0 = ЛКМ, 1 = ПКМ, 2 = СКМ.")]
    [SerializeField] private int mouseButton = 0; // ЛКМ по умолчанию

    [Tooltip("Если true - прячем и лочим курсор пока вращаем камеру.")]
    [SerializeField] private bool lockCursorWhileRotating = false;

    [Tooltip("Если true - не вращаем и не зумим камеру, когда курсор над UI.")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    [Header("Collision (optional)")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Tooltip("Лучше НЕ включать сюда слой игрока. Игрока проще исключить слоями.")]
    [SerializeField] private float collisionRadius = 0.25f;

    [Tooltip("Немного отодвинуть камеру от стены.")]
    [SerializeField] private float collisionPadding = 0.15f;

    // -------------------- RUNTIME --------------------
    private float yaw;
    private float pitch = 20f;

    private Vector3 smoothedPivot;
    private Vector3 pivotVel;

    private bool cursorOverridden;
    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;

    private void OnEnable()
    {
        if (target != null)
        {
            smoothedPivot = target.position + targetOffset;
            pivotVel = Vector3.zero;
        }

        // Подхватим углы из текущего поворота камеры.
        Vector3 e = transform.rotation.eulerAngles;
        yaw = e.y;
        pitch = WrapAngle180(e.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void OnDisable()
    {
        SetCursorLock(false);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Если курсор над UI - не крутим камеру (и не зумим).
        bool overUI = false;
        if (ignoreWhenPointerOverUI && EventSystem.current != null)
            overUI = EventSystem.current.IsPointerOverGameObject();

        bool held = IsMouseButtonHeld(mouseButton);
        bool rotating = rotateOnlyWhileMouseHeld ? held : true;

        if (overUI)
            rotating = false;

        if (lockCursorWhileRotating)
            SetCursorLock(rotating);

        // Зум (если не над UI).
        if (!overUI)
        {
            float zoomDelta = ReadMouseScrollY();
            if (Mathf.Abs(zoomDelta) > 0.001f)
            {
                distance = Mathf.Clamp(distance - zoomDelta * zoomSensitivity, minDistance, maxDistance);
            }
        }

        // Вращение (если не над UI).
        if (rotating)
        {
            Vector2 delta = ReadMouseDelta();
            yaw += delta.x * mouseSensitivity;

            float dy = delta.y * mouseSensitivity;
            pitch += invertY ? dy : -dy;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Пивот (точка, вокруг которой крутимся).
        Vector3 targetPivot = target.position + targetOffset;
        smoothedPivot = Vector3.SmoothDamp(smoothedPivot, targetPivot, ref pivotVel, followSmoothTime);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // Желаемая позиция камеры.
        Vector3 desiredPos = smoothedPivot - (rot * Vector3.forward * distance);

        // Коллизия.
        Vector3 finalPos = desiredPos;
        if (enableCollision && collisionRadius > 0f && collisionMask.value != 0)
        {
            Vector3 dir = desiredPos - smoothedPivot;
            float dist = dir.magnitude;

            if (dist > 0.001f)
            {
                dir /= dist;

                // Один SphereCast. Просто и быстро.
                if (Physics.SphereCast(smoothedPivot, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float hitDist = Mathf.Max(hit.distance - collisionPadding, 0.05f);
                    finalPos = smoothedPivot + dir * hitDist;
                }
            }
        }

        transform.SetPositionAndRotation(finalPos, rot);
    }

    private void SetCursorLock(bool locked)
    {
        if (!lockCursorWhileRotating)
            return;

        if (locked)
        {
            if (!cursorOverridden)
            {
                prevLockMode = Cursor.lockState;
                prevCursorVisible = Cursor.visible;
                cursorOverridden = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (!cursorOverridden)
                return;

            Cursor.lockState = prevLockMode;
            Cursor.visible = prevCursorVisible;
            cursorOverridden = false;
        }
    }

    private static float WrapAngle180(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }

    // -------------------- INPUT HELPERS --------------------
    private static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private static float ReadMouseScrollY()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
        return Input.mouseScrollDelta.y;
#endif
    }

    private static bool IsMouseButtonHeld(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;

        if (button == 0) return Mouse.current.leftButton.isPressed;   // ЛКМ
        if (button == 1) return Mouse.current.rightButton.isPressed;  // ПКМ
        if (button == 2) return Mouse.current.middleButton.isPressed; // СКМ

        return false;
#else
        return Input.GetMouseButton(button);
#endif
    }

    /// <summary>Можно назначить цель из другого скрипта.</summary>
    public void SetTarget(Transform t)
    {
        target = t;
        if (target != null)
            smoothedPivot = target.position + targetOffset;
    }
}

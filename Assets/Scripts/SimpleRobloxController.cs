using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class SimpleRobloxController : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System - optional)")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
#endif

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSmoothTime = 0.08f;
    [SerializeField] private float visualYawOffset = 0f;

    [Header("Animation (optional)")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string runMultParam = "RunSpeedMultiplier";

    [SerializeField] private float normalizeSpeedBy = 6f;
    [SerializeField] private float speedDampTime = 0.08f;
    [SerializeField] private float minRunAnimSpeed = 1.0f;
    [SerializeField] private float maxRunAnimSpeed = 2.0f;

    private CharacterController _cc;
    private Vector3 _hVelocity;
    private float _vVelocity;
    private float _yawVel;

    private int _speedHash;
    private int _groundedHash;
    private int _runMultHash;

    public Vector3 WorldVelocity => new Vector3(_hVelocity.x, _vVelocity, _hVelocity.z);
    public bool IsGrounded => _cc != null && _cc.isGrounded;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (visualRoot == null)
            visualRoot = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorHashes();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.action?.Enable();
        jumpAction?.action?.Enable();
        sprintAction?.action?.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.action?.Disable();
        jumpAction?.action?.Disable();
        sprintAction?.action?.Disable();
#endif
    }

    private void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        bool jumpPressed = ReadJumpPressedThisFrame();
        bool sprintHeld = ReadSprintHeld();

        Vector3 moveWorld = ToWorldDirection(moveInput);

        float maxSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 desiredHVel = moveWorld * maxSpeed;

        float rate = (desiredHVel.sqrMagnitude > _hVelocity.sqrMagnitude) ? acceleration : deceleration;
        _hVelocity = Vector3.MoveTowards(_hVelocity, desiredHVel, rate * Time.deltaTime);

        if (_cc.isGrounded)
        {
            if (_vVelocity < 0f)
                _vVelocity = groundedStickForce;

            if (jumpPressed)
                _vVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            _vVelocity += gravity * Time.deltaTime;
        }

        Vector3 displacement = (_hVelocity + Vector3.up * _vVelocity) * Time.deltaTime;
        _cc.Move(displacement);

        // ВАЖНО: поворачиваемся по реальному движению (по _hVelocity),
        // чтобы при инерции модель не “замирала” лицом вперёд.
        RotateVisualByVelocity();

        UpdateAnimator();
    }

    private Vector3 ToWorldDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }

        Vector3 dir = right * moveInput.x + forward * moveInput.y;
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        return dir;
    }

    private void RotateVisualByVelocity()
    {
        if (visualRoot == null)
            return;

        Vector3 v = new Vector3(_hVelocity.x, 0f, _hVelocity.z);
        if (v.sqrMagnitude < 0.0001f)
            return;

        float targetYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg + visualYawOffset;
        float newYaw = Mathf.SmoothDampAngle(visualRoot.eulerAngles.y, targetYaw, ref _yawVel, rotationSmoothTime);
        visualRoot.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        float horizontalSpeed = new Vector3(_hVelocity.x, 0f, _hVelocity.z).magnitude;

        float denom = (normalizeSpeedBy > 0.001f) ? normalizeSpeedBy : walkSpeed;
        float speed01 = (denom > 0.001f) ? Mathf.Clamp01(horizontalSpeed / denom) : 0f;

        if (_speedHash != 0)
            animator.SetFloat(_speedHash, speed01, speedDampTime, Time.deltaTime);

        if (_groundedHash != 0)
            animator.SetBool(_groundedHash, _cc.isGrounded);

        float t = (sprintSpeed > 0.001f) ? Mathf.Clamp01(horizontalSpeed / sprintSpeed) : speed01;
        float runMult = Mathf.Lerp(minRunAnimSpeed, maxRunAnimSpeed, t);

        if (_runMultHash != 0)
            animator.SetFloat(_runMultHash, runMult);
    }

    private void CacheAnimatorHashes()
    {
        _speedHash = string.IsNullOrWhiteSpace(speedParam) ? 0 : Animator.StringToHash(speedParam);
        _groundedHash = string.IsNullOrWhiteSpace(groundedParam) ? 0 : Animator.StringToHash(groundedParam);
        _runMultHash = string.IsNullOrWhiteSpace(runMultParam) ? 0 : Animator.StringToHash(runMultParam);
    }

    // -------------------- INPUT HELPERS --------------------
    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null && moveAction.action != null)
            return Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);

        if (Keyboard.current != null)
        {
            float x =
                (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) -
                (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);

            float y =
                (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f) -
                (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);

            return new Vector2(x, y).normalized;
        }

        return Vector2.zero;
#else
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
    }

    private bool ReadJumpPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (jumpAction != null && jumpAction.action != null)
            return jumpAction.action.WasPressedThisFrame();

        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private bool ReadSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (sprintAction != null && sprintAction.action != null)
            return sprintAction.action.IsPressed();

        return Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}

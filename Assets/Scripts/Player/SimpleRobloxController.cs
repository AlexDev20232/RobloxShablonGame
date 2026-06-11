using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class SimpleRobloxController : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (optional)")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
#endif

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Roblox-like Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float airControl = 0.45f;
    [SerializeField] private float turnSpeed = 14f;

    [Header("Roblox-like Jump")]
    [SerializeField] private float jumpPower = 8.5f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallMultiplier = 1.25f;
    [SerializeField] private float lowJumpMultiplier = 1.55f;

    [Header("Animation")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string runMultParam = "RunSpeedMultiplier";
    [SerializeField] private float speedDampTime = 0.08f;
    [SerializeField] private float minRunAnimSpeed = 1f;
    [SerializeField] private float maxRunAnimSpeed = 1.6f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private int speedHash;
    private int groundedHash;
    private int jumpHash;
    private int runMultHash;
    private float coyoteTimer;
    private float jumpBufferTimer;

    public Vector3 WorldVelocity => horizontalVelocity + Vector3.up * verticalVelocity;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;

    public void SetMoveSpeeds(float newWalkSpeed, float newSprintSpeed)
    {
        walkSpeed = Mathf.Max(0.01f, newWalkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, newSprintSpeed);
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimatorHashes();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
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
        Vector2 input = ReadMoveInput();
        Vector3 inputDirection = GetCameraRelativeDirection(input);
        bool grounded = controller.isGrounded;
        bool sprint = ReadSprintHeld();

        if (ReadJumpPressedThisFrame())
        {
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTime);
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (grounded)
        {
            coyoteTimer = Mathf.Max(0f, coyoteTime);
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        float targetSpeed = sprint ? sprintSpeed : walkSpeed;
        Vector3 desiredVelocity = inputDirection * targetSpeed;
        float control = grounded ? 1f : airControl;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            desiredVelocity,
            acceleration * control * Time.deltaTime);

        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = jumpPower;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            TriggerJumpAnimation();
        }

        float gravityScale = 1f;
        if (verticalVelocity < 0f)
        {
            gravityScale = Mathf.Max(1f, fallMultiplier);
        }
        else if (verticalVelocity > 0f && !ReadJumpHeld())
        {
            gravityScale = Mathf.Max(1f, lowJumpMultiplier);
        }

        verticalVelocity += gravity * gravityScale * Time.deltaTime;

        Vector3 motion = horizontalVelocity;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        RotateVisual(inputDirection);
        UpdateAnimator();
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
        }

        Vector3 direction = right * input.x + forward * input.y;
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        return direction;
    }

    private void RotateVisual(Vector3 inputDirection)
    {
        if (visualRoot == null || inputDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(inputDirection, Vector3.up);
        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            target,
            1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        float horizontalSpeed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;
        float speed01 = walkSpeed > 0.001f ? Mathf.Clamp01(horizontalSpeed / walkSpeed) : 0f;

        if (speedHash != 0)
        {
            animator.SetFloat(speedHash, speed01, speedDampTime, Time.deltaTime);
        }

        if (groundedHash != 0)
        {
            animator.SetBool(groundedHash, controller.isGrounded);
        }

        if (runMultHash != 0)
        {
            float sprint01 = sprintSpeed > 0.001f ? Mathf.Clamp01(horizontalSpeed / sprintSpeed) : speed01;
            animator.SetFloat(runMultHash, Mathf.Lerp(minRunAnimSpeed, maxRunAnimSpeed, sprint01));
        }
    }

    private void CacheAnimatorHashes()
    {
        speedHash = string.IsNullOrWhiteSpace(speedParam) ? 0 : Animator.StringToHash(speedParam);
        groundedHash = string.IsNullOrWhiteSpace(groundedParam) ? 0 : Animator.StringToHash(groundedParam);
        jumpHash = string.IsNullOrWhiteSpace(jumpTriggerParam) ? 0 : Animator.StringToHash(jumpTriggerParam);
        runMultHash = string.IsNullOrWhiteSpace(runMultParam) ? 0 : Animator.StringToHash(runMultParam);
    }

    private void TriggerJumpAnimation()
    {
        if (animator != null && jumpHash != 0)
        {
            animator.SetTrigger(jumpHash);
        }
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null && moveAction.action != null)
        {
            return Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);
        }

        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
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
        {
            return jumpAction.action.WasPressedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private bool ReadJumpHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (jumpAction != null && jumpAction.action != null)
        {
            return jumpAction.action.IsPressed();
        }

        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
        return Input.GetKey(KeyCode.Space);
#endif
    }

    private bool ReadSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (sprintAction != null && sprintAction.action != null)
        {
            return sprintAction.action.IsPressed();
        }

        return Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}

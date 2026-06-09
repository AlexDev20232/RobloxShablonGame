using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class SimpleRunIdleController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 18f;
    public float deceleration = 22f;
    public float gravity = -20f;
    public float groundedStickForce = -2f;
    public bool rotateToMove = true;
    public float turnSmoothTime = 0.08f;

    [Header("Jump")]
    public InputActionReference jumpAction;
    public float jumpHeight = 1.2f;
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.12f;
    public float fallMultiplier = 1.3f;
    public float lowJumpMultiplier = 1.6f;

    [Header("Input (New Input System)")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;

    [Header("Camera (Third Person)")]
    public Transform cameraTransform;
    public float cameraHeight = 1.8f;
    public float cameraDistance = 5.5f;
    public float cameraMinDistance = 1.5f;
    public Vector2 pitchLimits = new Vector2(-35f, 70f);
    public float mouseSensitivity = 0.12f;
    public float stickSensitivity = 180f;
    public bool invertY = false;
    public float cameraRotationSmoothTime = 0.06f;
    public float cameraFollowSmoothTime = 0.05f;

    [Header("Camera Collision")]
    public LayerMask cameraCollisionMask = ~0;
    public float cameraCollisionRadius = 0.25f;
    public float cameraCollisionOffset = 0.15f;

    [Header("Animation")]
    public float speedDampTime = 0.05f;
    public float minRunAnimSpeed = 1.0f;
    public float maxRunAnimSpeed = 2.2f;
    public float runMultSmoothTime = 0.10f;
    public float animStopThreshold = 0.05f;

    private CharacterController _controller;
    private Animator _animator;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _lookUsesPointer;

    private float _verticalVelocity;

    private float _yaw;
    private float _pitch;
    private float _targetYaw;
    private float _targetPitch;
    private float _yawVel;
    private float _pitchVel;

    private Vector3 _pivotPos;
    private Vector3 _pivotVel;
    private Vector3 _camPosVel;

    private float _turnVel;

    private float _runMultCurrent = 1f;
    private float _runMultVel;

    private readonly RaycastHit[] _hits = new RaycastHit[8];

    private const string SpeedParam = "Speed";
    private const string RunSpeedParam = "RunSpeedMultiplier";

    private float _jumpBufferTimer;
    private float _coyoteTimer;
    private bool _jumpHeld;

    private float _currentSpeed;
    private Vector3 _currentMoveDir;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _animator.applyRootMotion = false;
        _animator.updateMode = AnimatorUpdateMode.Normal;

        if (cameraTransform != null)
        {
            var e = cameraTransform.rotation.eulerAngles;
            _yaw = e.y;
            _pitch = NormalizePitch(e.x);
            _targetYaw = _yaw;
            _targetPitch = _pitch;
        }

        _pivotPos = transform.position + Vector3.up * cameraHeight;
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Enable();
        if (lookAction != null && lookAction.action != null) lookAction.action.Enable();
        if (jumpAction != null && jumpAction.action != null) jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Disable();
        if (lookAction != null && lookAction.action != null) lookAction.action.Disable();
        if (jumpAction != null && jumpAction.action != null) jumpAction.action.Disable();
    }

    private void Update()
    {
        ReadInput();

        Vector2 input = Vector2.ClampMagnitude(_moveInput, 1f);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward; forward.y = 0f; forward.Normalize();
            right = cameraTransform.right; right.y = 0f; right.Normalize();
        }

        Vector3 desiredMoveDir = forward * input.y + right * input.x;
        if (desiredMoveDir.sqrMagnitude > 0.0001f) desiredMoveDir.Normalize();

        float targetSpeed = moveSpeed * input.magnitude;

        float rate = (targetSpeed > _currentSpeed) ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        if (_currentSpeed > 0.001f && desiredMoveDir.sqrMagnitude > 0.0001f)
            _currentMoveDir = desiredMoveDir;
        else if (targetSpeed <= 0.001f && _currentSpeed <= animStopThreshold)
            _currentMoveDir = Vector3.zero;

        bool grounded = _controller.isGrounded;

        if (grounded)
        {
            _coyoteTimer = coyoteTime;
            if (_verticalVelocity < 0f) _verticalVelocity = groundedStickForce;
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
            _verticalVelocity += gravity * Time.deltaTime;

            if (_verticalVelocity < 0f)
                _verticalVelocity += gravity * (fallMultiplier - 1f) * Time.deltaTime;

            if (!_jumpHeld && _verticalVelocity > 0f)
                _verticalVelocity += gravity * (lowJumpMultiplier - 1f) * Time.deltaTime;
        }

        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;

        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;

            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.01f, jumpHeight));
            _verticalVelocity = jumpVelocity;
        }

        Vector3 horizontalVelocity = _currentMoveDir * _currentSpeed;
        Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);

        if (rotateToMove && _currentMoveDir.sqrMagnitude > 0.0001f && _currentSpeed > 0.05f)
        {
            float targetAngle = Mathf.Atan2(_currentMoveDir.x, _currentMoveDir.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnVel, Mathf.Max(0.0001f, turnSmoothTime));
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        float normalizedSpeed = (moveSpeed > 0.001f) ? Mathf.Clamp01(_currentSpeed / moveSpeed) : 0f;
        if (normalizedSpeed < animStopThreshold) normalizedSpeed = 0f;

        _animator.SetFloat(SpeedParam, normalizedSpeed, speedDampTime, Time.deltaTime);

        float minRun = Mathf.Min(minRunAnimSpeed, maxRunAnimSpeed);
        float maxRun = Mathf.Max(minRunAnimSpeed, maxRunAnimSpeed);
        float runMultTarget = Mathf.Lerp(minRun, maxRun, normalizedSpeed);
        _runMultCurrent = Mathf.SmoothDamp(_runMultCurrent, runMultTarget, ref _runMultVel, runMultSmoothTime);
        _animator.SetFloat(RunSpeedParam, _runMultCurrent);
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void ReadInput()
    {
        _moveInput = (moveAction != null && moveAction.action != null) ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        if (lookAction != null && lookAction.action != null)
        {
            _lookInput = lookAction.action.ReadValue<Vector2>();
            var control = lookAction.action.activeControl;
            _lookUsesPointer = (control != null && control.device is Pointer);
        }
        else
        {
            _lookInput = Vector2.zero;
            _lookUsesPointer = false;
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            if (jumpAction.action.WasPressedThisFrame())
                _jumpBufferTimer = jumpBufferTime;

            _jumpHeld = jumpAction.action.IsPressed();
        }
        else
        {
            _jumpHeld = false;
        }
    }

    private void UpdateCamera()
    {
        if (cameraTransform == null) return;

        float dt = Time.deltaTime;
        float ySign = invertY ? 1f : -1f;

        if (_lookUsesPointer)
        {
            _targetYaw += _lookInput.x * mouseSensitivity;
            _targetPitch += _lookInput.y * mouseSensitivity * ySign;
        }
        else
        {
            _targetYaw += _lookInput.x * stickSensitivity * dt;
            _targetPitch += _lookInput.y * stickSensitivity * dt * ySign;
        }

        _targetPitch = Mathf.Clamp(_targetPitch, pitchLimits.x, pitchLimits.y);

        float rotSmooth = Mathf.Max(0f, cameraRotationSmoothTime);
        if (rotSmooth <= 0f)
        {
            _yaw = _targetYaw;
            _pitch = _targetPitch;
        }
        else
        {
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVel, rotSmooth);
            _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVel, rotSmooth);
        }

        Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 pivotTarget = transform.position + Vector3.up * cameraHeight;
        float followSmooth = Mathf.Max(0f, cameraFollowSmoothTime);

        _pivotPos = (followSmooth > 0f)
            ? Vector3.SmoothDamp(_pivotPos, pivotTarget, ref _pivotVel, followSmooth)
            : pivotTarget;

        Vector3 desiredDir = orbitRot * Vector3.back;
        float desiredDistance = ResolveCameraCollision(_pivotPos, desiredDir, cameraDistance);

        Vector3 targetCamPos = _pivotPos + desiredDir * desiredDistance;

        cameraTransform.position = (followSmooth > 0f)
            ? Vector3.SmoothDamp(cameraTransform.position, targetCamPos, ref _camPosVel, followSmooth)
            : targetCamPos;

        Vector3 lookDir = _pivotPos - cameraTransform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
            cameraTransform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    private float ResolveCameraCollision(Vector3 pivot, Vector3 dir, float desiredDistance)
    {
        float minDist = Mathf.Clamp(cameraMinDistance, 0f, cameraDistance);

        if (cameraCollisionRadius <= 0f || cameraCollisionMask == 0)
            return Mathf.Clamp(desiredDistance, minDist, cameraDistance);

        int hitCount = Physics.SphereCastNonAlloc(
            pivot,
            cameraCollisionRadius,
            dir,
            _hits,
            cameraDistance,
            cameraCollisionMask,
            QueryTriggerInteraction.Ignore
        );

        float closest = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var h = _hits[i];
            if (h.collider == null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < closest) closest = h.distance;
        }

        if (closest < float.MaxValue)
        {
            float d = closest - cameraCollisionOffset;
            return Mathf.Clamp(d, minDist, cameraDistance);
        }

        return Mathf.Clamp(desiredDistance, minDist, cameraDistance);
    }

    private float NormalizePitch(float xEuler)
    {
        if (xEuler > 180f) xEuler -= 360f;
        return xEuler;
    }
}

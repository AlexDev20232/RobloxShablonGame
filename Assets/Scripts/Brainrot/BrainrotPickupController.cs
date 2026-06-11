using UnityEngine;

public class BrainrotPickupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrainrotPromptSystem promptSystem;
    [SerializeField] private BrainrotInventory inventory;

    [Header("Hold")]
    [SerializeField] private TemplateGameConfig gameConfig;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private KeyCode holdKey = KeyCode.E;

    private BrainrotDefinition _lastTarget;
    private float _holdTimer;

    private void Awake()
    {
        if (promptSystem == null)
        {
            promptSystem = GetComponentInChildren<BrainrotPromptSystem>();
        }

        if (inventory == null)
        {
            inventory = GetComponentInChildren<BrainrotInventory>();
        }

        if (gameConfig == null)
        {
            BrainrotBaseManager manager = FindFirstObjectByType<BrainrotBaseManager>();
            if (manager != null)
            {
                gameConfig = manager.GameConfig;
            }
        }
    }

    private void Update()
    {
        BrainrotDefinition target = promptSystem != null ? promptSystem.CurrentTarget : null;

        if (target != _lastTarget)
        {
            _lastTarget = target;
            ResetHold();
        }

        if (target == null || inventory == null || inventory.IsCarrying)
        {
            ResetHold();
            return;
        }

        if (!IsHoldPressed())
        {
            ResetHold();
            return;
        }

        _holdTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_holdTimer / HoldDuration);
        promptSystem.SetHoldProgress(progress);

        if (progress >= 1f)
        {
            _holdTimer = 0f;
            promptSystem.SetHoldProgress(0f);
            inventory.Pickup(target);
        }
    }

    private void ResetHold()
    {
        _holdTimer = 0f;
        if (promptSystem != null)
        {
            promptSystem.SetHoldProgress(0f);
        }
    }

    private bool IsHoldPressed()
    {
        return TemplateInput.IsKeyPressed(HoldKey);
    }

    private KeyCode HoldKey => gameConfig != null ? gameConfig.InteractKey : holdKey;
    private float HoldDuration => gameConfig != null ? gameConfig.HoldDuration : Mathf.Max(0.01f, holdDuration);
}

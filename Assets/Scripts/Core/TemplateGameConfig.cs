using UnityEngine;

[CreateAssetMenu(menuName = "Brainrot Template/Game Config")]
public class TemplateGameConfig : ScriptableObject
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float holdDuration = 0.6f;

    [Header("Base Slots")]
    [SerializeField] private float slotUiRefreshInterval = 0.25f;
    [SerializeField] private float levelScaleStep = 0.05f;
    [SerializeField] private float maxLevelScaleMultiplier = 1.5f;

    public string PlayerTag => string.IsNullOrWhiteSpace(playerTag) ? "Player" : playerTag;
    public KeyCode InteractKey => interactKey;
    public float HoldDuration => Mathf.Max(0.01f, holdDuration);
    public float SlotUiRefreshInterval => Mathf.Max(0.05f, slotUiRefreshInterval);
    public float LevelScaleStep => Mathf.Max(0f, levelScaleStep);
    public float MaxLevelScaleMultiplier => Mathf.Max(1f, maxLevelScaleMultiplier);
}

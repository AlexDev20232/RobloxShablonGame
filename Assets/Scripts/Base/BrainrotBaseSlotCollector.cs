using UnityEngine;

public class BrainrotBaseSlotCollector : MonoBehaviour
{
    [SerializeField] private BrainrotBaseSlot slot;
    [SerializeField] private TemplateGameConfig gameConfig;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (slot == null)
        {
            slot = GetComponentInParent<BrainrotBaseSlot>();
        }

        if (gameConfig == null)
        {
            BrainrotBaseManager manager = GetComponentInParent<BrainrotBaseManager>();
            if (manager != null)
            {
                gameConfig = manager.GameConfig;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerTag))
        {
            return;
        }

        if (slot != null)
        {
            slot.CollectStored();
        }
    }

    private string PlayerTag => gameConfig != null ? gameConfig.PlayerTag : playerTag;
}

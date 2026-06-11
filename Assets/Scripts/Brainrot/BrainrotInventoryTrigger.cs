using UnityEngine;

public class BrainrotInventoryTrigger : MonoBehaviour
{
    [SerializeField] private BrainrotInventory inventory;
    [SerializeField] private TemplateGameConfig gameConfig;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (gameConfig == null)
        {
            BrainrotBaseManager manager = FindFirstObjectByType<BrainrotBaseManager>();
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

        if (inventory == null)
        {
            inventory = other.GetComponentInParent<BrainrotInventory>();
        }

        if (inventory == null)
        {
            return;
        }

        if (inventory.TryStoreCarried(out BrainrotDefinition stored) && stored != null)
        {
            BrainrotIndexData.Unlock(stored);
        }
    }

    private string PlayerTag => gameConfig != null ? gameConfig.PlayerTag : playerTag;
}

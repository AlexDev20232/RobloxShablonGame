using UnityEngine;

public class BrainrotInventoryTrigger : MonoBehaviour
{
    [SerializeField] private BrainrotInventory inventory;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
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
}

using UnityEngine;

public class BrainrotBaseSlotCollector : MonoBehaviour
{
    [SerializeField] private BrainrotBaseSlot slot;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (slot == null)
        {
            slot = GetComponentInParent<BrainrotBaseSlot>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (slot != null)
        {
            slot.CollectStored();
        }
    }
}

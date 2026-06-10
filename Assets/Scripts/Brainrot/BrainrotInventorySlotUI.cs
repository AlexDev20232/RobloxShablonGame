using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrainrotInventorySlotUI : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private Button button;
    [SerializeField] private BrainrotInventory inventory;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(BrainrotInventory owner, int index)
    {
        inventory = owner;
        slotIndex = index;
    }

    public void SetName(string value)
    {
        if (nameText != null)
        {
            nameText.text = value;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(selected);
        }
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }

        if (!visible && selectedHighlight != null)
        {
            selectedHighlight.SetActive(false);
        }
    }

    private void HandleClick()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.SelectSlot(slotIndex);
    }
}

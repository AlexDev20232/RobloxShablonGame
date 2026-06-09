using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TemplateUiBinder : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject speedPanel;
    [SerializeField] private GameObject indexPanel;
    [SerializeField] private bool closePanelsOnAwake = true;

    [Header("Buttons")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button shopCloseButton;
    [SerializeField] private Button indexButton;
    [SerializeField] private Button indexCloseButton;
    [SerializeField] private Button rebirthButton;
    [SerializeField] private Button baseSlotButton;
    [SerializeField] private TMP_Text baseSlotButtonText;

    [Header("Systems")]
    [SerializeField] private RebirthSystem rebirthSystem;
    [SerializeField] private BrainrotBaseUpgradeController baseSlotUpgrade;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();

        if (closePanelsOnAwake)
        {
            SetPanel(shopPanel, false);
            SetPanel(speedPanel, false);
            SetPanel(indexPanel, false);
        }

        RefreshBaseSlotButton();
    }

    private void OnEnable()
    {
        RefreshBaseSlotButton();
    }

    private void OnDestroy()
    {
        UnbindButton(shopButton, ShowShop);
        UnbindButton(shopCloseButton, HideShop);
        UnbindButton(indexButton, ShowIndex);
        UnbindButton(indexCloseButton, HideIndex);
        UnbindButton(rebirthButton, TryRebirth);
        UnbindButton(baseSlotButton, TryUpgradeBaseSlots);
    }

    private void ResolveReferences()
    {
        if (shopPanel == null)
        {
            shopPanel = FindChildObject("Shop");
        }

        if (speedPanel == null)
        {
            speedPanel = FindChildObject("UpgradeSpeed");
        }

        if (indexPanel == null)
        {
            indexPanel = FindChildObject("IndexPanel");
        }

        if (shopButton == null)
        {
            shopButton = FindChildButton("MainScreen/Shop");
        }

        if (shopCloseButton == null)
        {
            shopCloseButton = FindChildButton("Shop/ShopPanel/CloseButton");
        }

        if (indexButton == null)
        {
            indexButton = FindChildButton("MainScreen/IndexBtn");
        }

        if (indexCloseButton == null)
        {
            indexCloseButton = FindChildButton("IndexPanel/CloseBtn");
        }

        if (rebirthButton == null)
        {
            rebirthButton = FindChildButton("MainScreen/RebrishBtn");
        }

        if (baseSlotButton == null)
        {
            baseSlotButton = FindChildButton("MainScreen/vip");
        }

        if (baseSlotButtonText == null)
        {
            Transform text = transform.Find("MainScreen/vip/txt");
            if (text != null)
            {
                baseSlotButtonText = text.GetComponent<TMP_Text>();
            }
        }

        if (rebirthSystem == null)
        {
            rebirthSystem = FindFirstObjectByType<RebirthSystem>();
        }

        if (baseSlotUpgrade == null)
        {
            baseSlotUpgrade = FindFirstObjectByType<BrainrotBaseUpgradeController>();
        }
    }

    private void BindButtons()
    {
        BindButton(shopButton, ShowShop);
        BindButton(shopCloseButton, HideShop);
        BindButton(indexButton, ShowIndex);
        BindButton(indexCloseButton, HideIndex);
        BindButton(rebirthButton, TryRebirth);
        BindButton(baseSlotButton, TryUpgradeBaseSlots);
    }

    private void BindButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void UnbindButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
    }

    private Button FindChildButton(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private GameObject FindChildObject(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.gameObject : null;
    }

    private void ShowShop()
    {
        SetPanel(shopPanel, true);
    }

    private void HideShop()
    {
        SetPanel(shopPanel, false);
    }

    private void ShowIndex()
    {
        SetPanel(indexPanel, true);
    }

    private void HideIndex()
    {
        SetPanel(indexPanel, false);
    }

    private void TryRebirth()
    {
        if (rebirthSystem != null)
        {
            rebirthSystem.TryRebirth();
        }

        RefreshBaseSlotButton();
    }

    private void TryUpgradeBaseSlots()
    {
        if (baseSlotUpgrade != null)
        {
            baseSlotUpgrade.TryUpgradeBase();
        }

        RefreshBaseSlotButton();
    }

    private void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }
    }

    private void RefreshBaseSlotButton()
    {
        if (baseSlotButton == null || baseSlotUpgrade == null)
        {
            return;
        }

        bool canUpgrade = baseSlotUpgrade.CanUpgrade();
        baseSlotButton.interactable = canUpgrade;

        if (baseSlotButtonText != null)
        {
            baseSlotButtonText.text = canUpgrade
                ? "Slots\n" + UpgradeController.FormatCurrency(baseSlotUpgrade.GetNextSlotCost())
                : "Slots\nMAX";
        }
    }
}

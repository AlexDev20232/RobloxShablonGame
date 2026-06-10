using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrainrotIndexItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Image[] typeTintTargets;

    [Header("Colors")]
    [SerializeField] private Color lockedIconColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color unlockedIconColor = Color.white;
    [SerializeField] private Color lockedNameColor = Color.white;
    [SerializeField] private Color unlockedNameColor = Color.white;

    [Header("Type Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color goldColor = new Color(1f, 0.8f, 0.1f, 1f);
    [SerializeField] private Color diamondColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private Color emeraldColor = new Color(0.1f, 1f, 0.4f, 1f);

    public void Bind(BrainrotDefinition def, bool unlocked)
    {
        if (def == null)
        {
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = def.indexIcon;
            iconImage.color = unlocked ? unlockedIconColor : lockedIconColor;
        }

        if (nameText != null)
        {
            nameText.text = unlocked ? def.displayName : "???";
            nameText.color = unlocked ? unlockedNameColor : lockedNameColor;
        }

        if (rarityText != null)
        {
            rarityText.text = def.rarity.ToString();
        }

        ApplyTypeColor(def.type);
    }

    private void ApplyTypeColor(BrainrotType type)
    {
        if (typeTintTargets == null || typeTintTargets.Length == 0)
        {
            return;
        }

        Color color = GetTypeColor(type);
        for (int i = 0; i < typeTintTargets.Length; i++)
        {
            if (typeTintTargets[i] != null)
            {
                typeTintTargets[i].color = color;
            }
        }
    }

    private Color GetTypeColor(BrainrotType type)
    {
        switch (type)
        {
            case BrainrotType.Gold:
                return goldColor;
            case BrainrotType.Diamond:
                return diamondColor;
            case BrainrotType.Emerald:
                return emeraldColor;
            default:
                return normalColor;
        }
    }
}

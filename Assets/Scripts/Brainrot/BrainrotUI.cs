using TMPro;
using UnityEngine;

public class BrainrotUI : MonoBehaviour
{
    [Header("Text Fields")]
    public TMP_Text timeText;
    public TMP_Text titleText;
    public TMP_Text nameText;
    public TMP_Text rarityText;
    public TMP_Text incomeText;

    public void SetDefinition(BrainrotDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = definition.title;
        }

        if (nameText != null)
        {
            nameText.text = definition.displayName;
        }

        if (rarityText != null)
        {
            rarityText.text = definition.rarity.ToString();
        }

        if (incomeText != null)
        {
            incomeText.text = $"${definition.incomePerSecond:0.##}/s";
        }
    }

    public void SetTimeRemaining(float seconds)
    {
        if (timeText == null)
        {
            return;
        }

        int displaySeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        timeText.text = $"{displaySeconds}s";
    }

    public void SetTimerVisible(bool visible)
    {
        if (timeText == null)
        {
            return;
        }

        timeText.gameObject.SetActive(visible);
    }
}

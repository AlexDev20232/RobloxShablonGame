using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Brainrot Template/Brainrot Skin Palette")]
public class BrainrotSkinPalette : ScriptableObject
{
    [SerializeField] private BrainrotSkinEntry[] entries;

    public bool TryGet(BrainrotType type, out BrainrotSkinEntry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].Type == type)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }
}

[Serializable]
public class BrainrotSkinEntry
{
    [SerializeField] private BrainrotType type = BrainrotType.Normal;
    [SerializeField] private Material material;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private string colorProperty = "_Color";
    [SerializeField] private bool applyTint = true;

    public BrainrotType Type => type;
    public Material Material => material;
    public Color Tint => tint;
    public string ColorProperty => colorProperty;
    public bool ApplyTint => applyTint;
}

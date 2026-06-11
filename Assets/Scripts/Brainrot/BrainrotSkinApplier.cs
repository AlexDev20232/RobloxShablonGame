using UnityEngine;

[DisallowMultipleComponent]
public class BrainrotSkinApplier : MonoBehaviour
{
    [SerializeField] private BrainrotDefinition definition;
    [SerializeField] private BrainrotSkinPalette palette;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private int materialSlot = -1;

    private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }

    public void Apply()
    {
        ResolveReferences();

        if (definition == null || palette == null || !palette.TryGet(definition.type, out BrainrotSkinEntry skin))
        {
            return;
        }

        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null)
            {
                continue;
            }

            ApplyMaterial(target, skin.Material);
            ApplyTint(target, skin);
        }
    }

    private void ResolveReferences()
    {
        if (definition == null)
        {
            definition = GetComponentInParent<BrainrotDefinition>();
        }

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void ApplyMaterial(Renderer target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        Material[] shared = target.sharedMaterials;
        if (shared == null || shared.Length == 0)
        {
            return;
        }

        if (materialSlot >= 0)
        {
            if (materialSlot >= shared.Length)
            {
                return;
            }

            shared[materialSlot] = material;
        }
        else
        {
            for (int i = 0; i < shared.Length; i++)
            {
                shared[i] = material;
            }
        }

        target.sharedMaterials = shared;
    }

    private void ApplyTint(Renderer target, BrainrotSkinEntry skin)
    {
        if (target == null || skin == null || !skin.ApplyTint || string.IsNullOrWhiteSpace(skin.ColorProperty))
        {
            return;
        }

        target.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(skin.ColorProperty, skin.Tint);
        target.SetPropertyBlock(_propertyBlock);
    }
}

using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PlayerHandTrailController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SimpleRobloxController playerController;
    [SerializeField] private Material trailMaterial;

    [Header("Emission")]
    [SerializeField] private bool emitOnlyWhileMoving = true;
    [SerializeField] private float minSpeedToEmit = 0.25f;
    [SerializeField] private bool clearWhenDisabled = true;

    [Header("Attachment")]
    [SerializeField] private Vector3 leftHandLocalOffset = new Vector3(-0.04f, -0.03f, 0.08f);
    [SerializeField] private Vector3 rightHandLocalOffset = new Vector3(0.04f, -0.03f, 0.08f);

    [Header("Core Trail")]
    [SerializeField] private Color coreStartColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color coreEndColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float coreTime = 0.18f;
    [SerializeField] private float coreWidth = 0.18f;

    [Header("Glow Trail")]
    [SerializeField] private Color glowStartColor = new Color(0.58f, 1f, 0.12f, 0.45f);
    [SerializeField] private Color glowEndColor = new Color(0.58f, 1f, 0.12f, 0f);
    [SerializeField] private float glowTime = 0.28f;
    [SerializeField] private float glowWidth = 0.38f;

    [Header("Shape")]
    [SerializeField] private float minVertexDistance = 0.015f;
    [SerializeField] private int cornerVertices = 2;
    [SerializeField] private int capVertices = 2;

    private TrailRenderer[] _trails;
    private Material _runtimeMaterial;

    private void Awake()
    {
        ResolveReferences();
        RebuildTrails();
    }

    private void LateUpdate()
    {
        if (_trails == null || _trails.Length == 0)
        {
            return;
        }

        bool shouldEmit = !emitOnlyWhileMoving ||
                          playerController == null ||
                          playerController.WorldVelocity.magnitude >= minSpeedToEmit;

        for (int i = 0; i < _trails.Length; i++)
        {
            if (_trails[i] != null)
            {
                _trails[i].emitting = shouldEmit;
            }
        }
    }

    private void OnDisable()
    {
        if (_trails == null)
        {
            return;
        }

        for (int i = 0; i < _trails.Length; i++)
        {
            if (_trails[i] == null)
            {
                continue;
            }

            _trails[i].emitting = false;
            if (clearWhenDisabled)
            {
                _trails[i].Clear();
            }
        }
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }

    private void OnValidate()
    {
        minSpeedToEmit = Mathf.Max(0f, minSpeedToEmit);
        coreTime = Mathf.Max(0.01f, coreTime);
        glowTime = Mathf.Max(0.01f, glowTime);
        coreWidth = Mathf.Max(0.001f, coreWidth);
        glowWidth = Mathf.Max(0.001f, glowWidth);
        minVertexDistance = Mathf.Max(0.001f, minVertexDistance);
        cornerVertices = Mathf.Max(0, cornerVertices);
        capVertices = Mathf.Max(0, capVertices);
    }

    public void RebuildTrails()
    {
        Transform leftHand = ResolveHand(HumanBodyBones.LeftHand, "LeftHand");
        Transform rightHand = ResolveHand(HumanBodyBones.RightHand, "RightHand");

        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning("PlayerHandTrailController could not find both hand bones.", this);
            return;
        }

        Transform leftPoint = CreateAttachPoint(leftHand, "LeftHandTrailPoint", leftHandLocalOffset);
        Transform rightPoint = CreateAttachPoint(rightHand, "RightHandTrailPoint", rightHandLocalOffset);

        _trails = new[]
        {
            CreateTrail(leftPoint, "LeftHandTrail_Core", coreTime, coreWidth, coreStartColor, coreEndColor),
            CreateTrail(leftPoint, "LeftHandTrail_Glow", glowTime, glowWidth, glowStartColor, glowEndColor),
            CreateTrail(rightPoint, "RightHandTrail_Core", coreTime, coreWidth, coreStartColor, coreEndColor),
            CreateTrail(rightPoint, "RightHandTrail_Glow", glowTime, glowWidth, glowStartColor, glowEndColor)
        };
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<SimpleRobloxController>();
        }
    }

    private Transform ResolveHand(HumanBodyBones bone, string fallbackName)
    {
        if (animator != null && animator.isHuman)
        {
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform != null)
            {
                return boneTransform;
            }
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Contains(fallbackName))
            {
                return children[i];
            }
        }

        return null;
    }

    private Transform CreateAttachPoint(Transform hand, string pointName, Vector3 localOffset)
    {
        Transform existing = hand.Find(pointName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject point = new GameObject(pointName);
        point.transform.SetParent(hand, false);
        point.transform.localPosition = localOffset;
        point.transform.localRotation = Quaternion.identity;
        point.transform.localScale = Vector3.one;
        return point.transform;
    }

    private TrailRenderer CreateTrail(
        Transform parent,
        string trailName,
        float lifetime,
        float width,
        Color startColor,
        Color endColor)
    {
        GameObject trailObject = new GameObject(trailName);
        trailObject.transform.SetParent(parent, false);

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = lifetime;
        trail.widthMultiplier = width;
        trail.widthCurve = CreateWidthCurve();
        trail.colorGradient = CreateGradient(startColor, endColor);
        trail.minVertexDistance = minVertexDistance;
        trail.numCornerVertices = cornerVertices;
        trail.numCapVertices = capVertices;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.allowOcclusionWhenDynamic = false;
        trail.autodestruct = false;
        trail.emitting = false;
        trail.material = ResolveMaterial();
        trail.Clear();

        return trail;
    }

    private Material ResolveMaterial()
    {
        if (trailMaterial != null)
        {
            return trailMaterial;
        }

        if (_runtimeMaterial == null)
        {
            Shader shader = Shader.Find("BrainrotTemplate/HandTrail");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "RuntimeHandTrailMaterial"
            };
        }

        return _runtimeMaterial;
    }

    private static AnimationCurve CreateWidthCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 0.65f),
            new Keyframe(1f, 0f));
    }

    private static Gradient CreateGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(endColor.a, 1f)
            });
        return gradient;
    }
}

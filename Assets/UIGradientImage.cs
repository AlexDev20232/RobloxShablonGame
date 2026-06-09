using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class UIGradientImage : MonoBehaviour
{
    public enum GradientDirection
    {
        Horizontal,
        Vertical,
        Diagonal,
        DiagonalInverse
    }

    [Header("Gradient")]
    public Gradient gradient = new Gradient();

    [Header("Direction")]
    public GradientDirection direction = GradientDirection.Horizontal;

    private const string ShaderName = "UI/GradientTexture";
    private const int DefaultGradientResolution = 512;
    private const int MinGradientResolution = 128;
    private const int MaxGradientResolution = 2048;
    private Image _image;
    private Material _material;
    private Texture2D _gradientTexture;
    private Color[] _pixels;
    private RectTransform _rectTransform;
    private Vector2 _lastRectSize;
    private Vector2 _lastRectPivot;
    private int _lastResolution;
    private bool _lastLinear;

    private void Reset()
    {
        _image = GetComponent<Image>();
        direction = GradientDirection.Horizontal;
        gradient = new Gradient();
        gradient.colorKeys = new[]
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.black, 1f)
        };
        gradient.alphaKeys = new[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };
    }

    private void OnEnable()
    {
        EnsureResources();
        UpdateAll();
    }

    private void OnValidate()
    {
        EnsureResources();
        UpdateAll();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureResources();
        UpdateRectProperties();
        UpdateGradientTexture();
        if (_image != null)
        {
            _image.SetMaterialDirty();
        }
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled || _material == null)
        {
            return;
        }

        if (UpdateRectPropertiesIfNeeded())
        {
            _image.SetMaterialDirty();
        }
    }

    public void SetDirty()
    {
        EnsureResources();
        UpdateAll();
    }

    private void EnsureResources()
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }
        if (_rectTransform == null)
        {
            _rectTransform = transform as RectTransform;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[UIGradientImage] Shader '{ShaderName}' not found.");
            return;
        }

        if (_material == null || _material.shader != shader)
        {
            _material = new Material(shader)
            {
                name = "UIGradient (Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        if (_image != null && _image.material != _material)
        {
            _image.material = _material;
        }
    }

    private void UpdateAll()
    {
        if (!isActiveAndEnabled || _material == null)
        {
            return;
        }

        UpdateGradientTexture();
        UpdateDirection();
        UpdateRectProperties();
        if (_image != null)
        {
            _image.SetMaterialDirty();
        }
    }

    private void UpdateGradientTexture()
    {
        int width = GetTargetResolution();
        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        if (_gradientTexture == null || _gradientTexture.width != width || _lastLinear != linear)
        {
            DestroyTexture();
            _gradientTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false, linear)
            {
                name = "UIGradientTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            _pixels = new Color[width];
            _lastResolution = width;
            _lastLinear = linear;
        }

        for (int i = 0; i < width; i++)
        {
            float t = width == 1 ? 0f : i / (width - 1f);
            _pixels[i] = gradient.Evaluate(t);
        }

        _gradientTexture.SetPixels(_pixels);
        _gradientTexture.Apply(false, false);
        _material.SetTexture("_GradientTex", _gradientTexture);
    }

    private void UpdateDirection()
    {
        Vector2 dir;
        float scale;
        float offset;

        switch (direction)
        {
            case GradientDirection.Vertical:
                dir = Vector2.up;
                scale = 1f;
                offset = 0f;
                break;
            case GradientDirection.Diagonal:
                dir = new Vector2(1f, 1f);
                scale = 1f;
                offset = 0f;
                break;
            case GradientDirection.DiagonalInverse:
                dir = new Vector2(1f, -1f);
                scale = 0.70710677f;
                offset = 0.5f;
                break;
            default:
                dir = Vector2.right;
                scale = 1f;
                offset = 0f;
                break;
        }

        dir.Normalize();
        _material.SetVector("_GradientDir", new Vector4(dir.x, dir.y, 0f, 0f));
        _material.SetFloat("_GradientScale", scale);
        _material.SetFloat("_GradientOffset", offset);
    }

    private int GetTargetResolution()
    {
        if (_rectTransform == null)
        {
            return DefaultGradientResolution;
        }

        Vector2 size = _rectTransform.rect.size;
        float maxDim = Mathf.Max(size.x, size.y);
        if (maxDim <= 0f)
        {
            return DefaultGradientResolution;
        }

        int res = Mathf.NextPowerOfTwo(Mathf.CeilToInt(maxDim));
        return Mathf.Clamp(res, MinGradientResolution, MaxGradientResolution);
    }

    private void UpdateRectProperties()
    {
        if (_rectTransform == null || _material == null)
        {
            return;
        }

        Vector2 size = _rectTransform.rect.size;
        Vector2 pivot = _rectTransform.pivot;
        _lastRectSize = size;
        _lastRectPivot = pivot;

        _material.SetVector("_RectSize", new Vector4(size.x, size.y, 0f, 0f));
        _material.SetVector("_RectPivot", new Vector4(pivot.x, pivot.y, 0f, 0f));
    }

    private bool UpdateRectPropertiesIfNeeded()
    {
        if (_rectTransform == null || _material == null)
        {
            return false;
        }

        Vector2 size = _rectTransform.rect.size;
        Vector2 pivot = _rectTransform.pivot;
        if (size == _lastRectSize && pivot == _lastRectPivot)
        {
            return false;
        }

        _lastRectSize = size;
        _lastRectPivot = pivot;
        _material.SetVector("_RectSize", new Vector4(size.x, size.y, 0f, 0f));
        _material.SetVector("_RectPivot", new Vector4(pivot.x, pivot.y, 0f, 0f));
        UpdateGradientTexture();
        return true;
    }

    private void Cleanup()
    {
        if (_image != null && _image.material == _material)
        {
            _image.material = null;
        }

        DestroyTexture();

        if (_material != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_material);
            }
            else
            {
                DestroyImmediate(_material);
            }
            _material = null;
        }
    }

    private void DestroyTexture()
    {
        if (_gradientTexture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_gradientTexture);
        }
        else
        {
            DestroyImmediate(_gradientTexture);
        }

        _gradientTexture = null;
        _pixels = null;
    }
}

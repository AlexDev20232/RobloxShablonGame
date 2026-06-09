#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Генератор иконок (Sprite) из 3D-префабов.
/// ВАЖНО: используется PreviewRenderUtility, чтобы корректно рендерить в URP/HDRP (SRP).
/// </summary>
public class FruitIconBatchGeneratorWindow : EditorWindow
{
    [Header("Папка с префабами (где лежат модели фруктов)")]
    [SerializeField] private string prefabsFolder = "Assets/Fruit Market/Prefabs";

    [Header("Список префабов (если задан — используется он)")]
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();

    [Header("Папка для готовых иконок (PNG -> Sprite)")]
    [SerializeField] private string outputFolder = "Assets/FruitIcon";

    [Header("Размер иконки")]
    [SerializeField] private int iconSize = 512;

    [Header("Режим камеры")]
    [SerializeField] private bool useOrthographic = true;

    [Header("Запас вокруг модели (чтобы не обрезало)")]
    [SerializeField] private float padding = 1.15f;

    [Header("Наклон камеры по X (градусы)")]
    [SerializeField] private float tiltX = 20f;

    [Header("Использовать ручное направление камеры (опционально)")]
    [SerializeField] private bool useCustomDirection = false;

    [Header("Направление камеры (похоже на изометрию)")]
    [SerializeField] private Vector3 cameraDirection = new Vector3(0.35f, 0.25f, -1f);

    [Header("Прозрачный фон")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0);

    [MenuItem("Tools/Fruit Market/Generate Icons")]
    public static void Open()
    {
        var w = GetWindow<FruitIconBatchGeneratorWindow>();
        w.titleContent = new GUIContent("Fruit Icons");
        w.minSize = new Vector2(520, 280);
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Автогенерация иконок из 3D-префабов (URP/HDRP ок)", EditorStyles.boldLabel);
        GUILayout.Space(8);

        prefabsFolder = EditorGUILayout.TextField("Папка префабов", prefabsFolder);
        outputFolder  = EditorGUILayout.TextField("Папка иконок", outputFolder);

        SerializedObject so = new SerializedObject(this);
        so.Update();
        SerializedProperty prefabListProp = so.FindProperty("prefabList");
        EditorGUILayout.PropertyField(prefabListProp, new GUIContent("Список префабов"), true);
        so.ApplyModifiedProperties();

        iconSize = EditorGUILayout.IntPopup("Размер", iconSize,
            new[] { "128", "256", "512", "1024" },
            new[] { 128, 256, 512, 1024 });

        useOrthographic = EditorGUILayout.Toggle("Orthographic", useOrthographic);
        padding         = EditorGUILayout.Slider("Padding", padding, 1.0f, 1.6f);
        tiltX           = EditorGUILayout.Slider("Наклон X", tiltX, -60f, 60f);
        useCustomDirection = EditorGUILayout.Toggle("Use Custom Direction", useCustomDirection);
        cameraDirection = EditorGUILayout.Vector3Field("Camera Direction", cameraDirection);
        backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);

        GUILayout.Space(12);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(prefabsFolder) || string.IsNullOrWhiteSpace(outputFolder)))
        {
            if (GUILayout.Button("Generate", GUILayout.Height(36)))
                Generate();
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Рекомендация для WebGL (Яндекс Игры):\n" +
            "• 256 обычно достаточно.\n" +
            "• 512 красивее, но тяжелее по весу и памяти.\n" +
            "• Потом можно упаковать иконки в Sprite Atlas.",
            MessageType.Info);
    }

    private void Generate()
    {
        EnsureFolderExists(outputFolder);

        List<GameObject> prefabsToRender = CollectPrefabs();
        if (prefabsToRender.Count == 0)
        {
            Debug.LogWarning("Нет префабов для генерации. Заполни список или укажи папку с префабами.");
            return;
        }

        int ok = 0, fail = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (GameObject prefab in prefabsToRender)
            {
                if (prefab == null)
                {
                    fail++;
                    continue;
                }

                string iconAssetPath = $"{outputFolder}/{prefab.name}.png";

                bool result = RenderPrefabToPng_PreviewRenderUtility(prefab, iconAssetPath);

                if (result)
                {
                    ImportAsSprite(iconAssetPath);
                    ok++;
                }
                else
                {
                    fail++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Готово. Успешно: {ok}, Ошибок: {fail}");
    }

    private List<GameObject> CollectPrefabs()
    {
        var list = new List<GameObject>();
        if (prefabList != null)
        {
            for (int i = 0; i < prefabList.Count; i++)
            {
                GameObject prefab = prefabList[i];
                if (prefab != null)
                    list.Add(prefab);
            }
        }

        if (list.Count > 0)
            return list;

        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            Debug.LogError($"Папка префабов не найдена: {prefabsFolder}");
            return list;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsFolder });
        if (guids == null || guids.Length == 0)
            return list;

        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
                list.Add(prefab);
        }

        return list;
    }

    /// <summary>
    /// Рендерит префаб в PNG через PreviewRenderUtility.
    /// Это стабильнее для URP/HDRP, чем Camera.Render() в временной сцене.
    /// </summary>
    private bool RenderPrefabToPng_PreviewRenderUtility(GameObject prefab, string iconAssetPath)
    {
        UnityEditor.PreviewRenderUtility preview = null;
        Texture2D resultTex = null;

        try
        {
            preview = new UnityEditor.PreviewRenderUtility();

            // Настройка камеры
            var cam = preview.camera;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane  = 2000f;
            cam.orthographic  = useOrthographic;

            // Мягкий ambient, чтобы модель не была чёрной
            preview.ambientColor = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Свет (две лампы, как в превью инспектора)
            if (preview.lights != null && preview.lights.Length > 0)
            {
                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0f);
            }
            if (preview.lights != null && preview.lights.Length > 1)
            {
                preview.lights[1].intensity = 0.8f;
                preview.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
            }

            // Инстанс префаба в preview-сцену (не попадает в твою реальную сцену)
            GameObject instance = preview.InstantiatePrefabInScene(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;

            // Стабильные трансформы
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Bounds модели
            Bounds b = CalculateBounds(instance);

            // Если вдруг bounds нулевой — дадим запас
            float maxExtent = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
            maxExtent = Mathf.Max(maxExtent, 0.001f);

            // Направление камеры
            Vector3 dir;
            if (useCustomDirection)
            {
                dir = cameraDirection.sqrMagnitude < 0.0001f
                    ? new Vector3(0.3f, 0.2f, -1f)
                    : cameraDirection.normalized;
            }
            else
            {
                dir = Quaternion.Euler(tiltX, 0f, 0f) * Vector3.back;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector3.back;
                else
                    dir.Normalize();
            }

            // Позиционирование камеры
            if (useOrthographic)
            {
                cam.orthographicSize = maxExtent * padding;
                cam.transform.position = b.center - dir * (maxExtent * 6f);
            }
            else
            {
                cam.fieldOfView = 30f;
                float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float distance = (maxExtent * padding) / Mathf.Tan(halfFovRad);
                cam.transform.position = b.center - dir * distance;
            }

            cam.transform.LookAt(b.center);

            // Рендер в статичное превью (Texture2D на выходе)
            Rect rect = new Rect(0, 0, iconSize, iconSize);
            preview.BeginStaticPreview(rect);

            // Ключевое: разрешаем Scriptable Render Pipeline (URP/HDRP)
            preview.Render(allowScriptableRenderPipeline: true);

            resultTex = preview.EndStaticPreview();

            // Пишем PNG
            byte[] png = resultTex.EncodeToPNG();
            string fullPath = Path.GetFullPath(iconAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, png);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка генерации иконки для {prefab.name}: {e}");
            return false;
        }
        finally
        {
            if (resultTex != null)
                DestroyImmediate(resultTex);

            if (preview != null)
                preview.Cleanup();
        }
    }

    /// <summary>
    /// Считает bounds по всем Renderer внутри объекта.
    /// </summary>
    private Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }

    /// <summary>
    /// Импортирует PNG как Sprite (2D & UI).
    /// </summary>
    private void ImportAsSprite(string iconAssetPath)
    {
        AssetDatabase.ImportAsset(iconAssetPath, ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(iconAssetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Создаёт папку Assets/... если её нет.
    /// </summary>
    private void EnsureFolderExists(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        if (parts.Length == 0) return;

        string current = parts[0]; // обычно "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif

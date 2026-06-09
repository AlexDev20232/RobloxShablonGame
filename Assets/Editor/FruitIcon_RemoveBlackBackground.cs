#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Удаление чёрного фона у выбранных PNG-иконок.
/// Работает по принципу: фон — это область, связанная с краями картинки.
/// Это безопаснее, чем "удалить все чёрные пиксели" (тёмные фрукты не пострадают).
/// </summary>
public class FruitIcon_RemoveBlackBackground : EditorWindow
{
    [Header("Насколько считать цвет 'фоном' (0..60). Меньше = безопаснее.")]
    [SerializeField] private int tolerance = 12;

    [Header("Делать бэкап исходников (рекомендуется)")]
    [SerializeField] private bool makeBackup = true;

    [MenuItem("Tools/Fruit Market/Icons/Remove Black Background (Selected)")]
    public static void Open()
    {
        var w = GetWindow<FruitIcon_RemoveBlackBackground>();
        w.titleContent = new GUIContent("Remove BG");
        w.minSize = new Vector2(560, 190);
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Удалить чёрный фон у выбранных PNG", EditorStyles.boldLabel);
        GUILayout.Space(8);

        tolerance = EditorGUILayout.IntSlider("Tolerance", tolerance, 0, 60);
        makeBackup = EditorGUILayout.Toggle("Make backup", makeBackup);

        GUILayout.Space(10);

        if (GUILayout.Button("Process Selected PNG", GUILayout.Height(36)))
            ProcessSelected();

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Как пользоваться:\n" +
            "1) Выдели PNG в Project (можно выбрать папку целиком).\n" +
            "2) Нажми кнопку.\n\n" +
            "Если у тёмных фруктов съедает края — уменьши Tolerance (например 6–8).",
            MessageType.Info);
    }

    private void ProcessSelected()
    {
        // Собираем пути PNG из выделения (включая папки)
        List<string> pngPaths = CollectPngPathsFromSelection();

        if (pngPaths.Count == 0)
        {
            Debug.LogWarning("Не найдено PNG в выделении. Выдели файлы или папку с PNG.");
            return;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string backupDir = Path.Combine(projectRoot, "IconBackup");

        if (makeBackup && !Directory.Exists(backupDir))
            Directory.CreateDirectory(backupDir);

        int ok = 0, fail = 0;

        try
        {
            // ---------- ШАГ 1: делаем все текстуры читаемыми ----------
            for (int i = 0; i < pngPaths.Count; i++)
            {
                string path = pngPaths[i];
                EditorUtility.DisplayProgressBar("Prepare (Read/Write)", path, (float)i / pngPaths.Count);

                MakeTextureReadable(path);

                // ВАЖНО: синхронно реимпортим прямо сейчас
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            // ---------- ШАГ 2: удаляем фон ----------
            for (int i = 0; i < pngPaths.Count; i++)
            {
                string path = pngPaths[i];
                EditorUtility.DisplayProgressBar("Remove Background", path, (float)i / pngPaths.Count);

                // Бэкап исходника
                if (makeBackup)
                {
                    try
                    {
                        string srcFull = Path.GetFullPath(Path.Combine(projectRoot, path));
                        string dstFull = Path.Combine(backupDir, Path.GetFileName(path));
                        File.Copy(srcFull, dstFull, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Не смог сделать бэкап для {path}: {e.Message}");
                    }
                }

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                {
                    Debug.LogError($"Не смог загрузить Texture2D: {path}");
                    fail++;
                    continue;
                }

                // Если всё ещё не читается — значит настройки не применились (редко, но бывает)
                if (!tex.isReadable)
                {
                    Debug.LogWarning($"Текстура всё ещё не Read/Write: {path}. Повторно пробую реимпорт.");
                    MakeTextureReadable(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex == null || !tex.isReadable)
                    {
                        Debug.LogError($"Не удалось сделать текстуру читаемой: {path}");
                        fail++;
                        continue;
                    }
                }

                bool result = RemoveBackgroundInPlace(path, tex, tolerance);
                if (result) ok++; else fail++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Готово. Успешно: {ok}, Ошибок: {fail}");
    }

    /// <summary>
    /// Собирает PNG-пути из выделения. Если выделена папка — берёт все PNG внутри.
    /// </summary>
    private static List<string> CollectPngPathsFromSelection()
    {
        var result = new List<string>(256);

        UnityEngine.Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
            return result;

        foreach (var obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                // Ищем все текстуры в папке
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        result.Add(p);
                }
            }
            else
            {
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    result.Add(path);
            }
        }

        // Убираем дубликаты
        var unique = new HashSet<string>(result);
        return new List<string>(unique);
    }

    /// <summary>
    /// Делает текстуру читаемой (Read/Write) и сохраняет альфу.
    /// </summary>
    private static void MakeTextureReadable(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        bool changed = false;

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            changed = true;
        }

        if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        // На время обработки лучше без crunch/жёсткой компрессии
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        // Если это UI-иконки — пусть будут спрайтами
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    /// <summary>
    /// Убирает фон и перезаписывает PNG по этому же пути (сохраняет ссылки).
    /// </summary>
    private static bool RemoveBackgroundInPlace(string assetPath, Texture2D tex, int tol)
    {
        try
        {
            int w = tex.width;
            int h = tex.height;

            var pixels = tex.GetPixels32();

            // Цвет фона — среднее 4 углов
            Color32 bg = Average4(
                pixels[0],
                pixels[w - 1],
                pixels[(h - 1) * w],
                pixels[h * w - 1]
            );

            bool[] bgMask = FloodFillBackground(pixels, w, h, bg, tol);

            var outPixels = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                if (bgMask[i])
                {
                    outPixels[i] = new Color32(0, 0, 0, 0);
                }
                else
                {
                    Color32 p = pixels[i];
                    p.a = 255;
                    outPixels[i] = p;
                }
            }

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            outTex.SetPixels32(outPixels);
            outTex.Apply(false, false);

            byte[] png = outTex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(outTex);

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            File.WriteAllBytes(fullPath, png);

            // Реимпортим и возвращаем компрессию для экономии
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.isReadable = false; // можно выключить после обработки, чтобы экономить память
                importer.SaveAndReimport();
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка обработки {assetPath}: {e}");
            return false;
        }
    }

    private static bool[] FloodFillBackground(Color32[] pix, int w, int h, Color32 bg, int tol)
    {
        bool[] mask = new bool[pix.Length];
        Queue<int> q = new Queue<int>(pix.Length / 4);

        bool IsBg(Color32 c)
        {
            int dr = Mathf.Abs(c.r - bg.r);
            int dg = Mathf.Abs(c.g - bg.g);
            int db = Mathf.Abs(c.b - bg.b);
            int d = Mathf.Max(dr, Mathf.Max(dg, db));
            return d <= tol;
        }

        void TryEnqueue(int x, int y)
        {
            int idx = y * w + x;
            if (mask[idx]) return;

            if (IsBg(pix[idx]))
            {
                mask[idx] = true;
                q.Enqueue(idx);
            }
        }

        // Стартуем со всех краёв
        for (int x = 0; x < w; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, h - 1);
        }
        for (int y = 0; y < h; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(w - 1, y);
        }

        while (q.Count > 0)
        {
            int idx = q.Dequeue();
            int x = idx % w;
            int y = idx / w;

            if (x > 0)     TryEnqueue(x - 1, y);
            if (x < w - 1) TryEnqueue(x + 1, y);
            if (y > 0)     TryEnqueue(x, y - 1);
            if (y < h - 1) TryEnqueue(x, y + 1);
        }

        return mask;
    }

    private static Color32 Average4(Color32 a, Color32 b, Color32 c, Color32 d)
    {
        int r = a.r + b.r + c.r + d.r;
        int g = a.g + b.g + c.g + d.g;
        int bl = a.b + b.b + c.b + d.b;

        return new Color32(
            (byte)(r / 4),
            (byte)(g / 4),
            (byte)(bl / 4),
            255
        );
    }
}
#endif

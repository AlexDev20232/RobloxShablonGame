using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BrainrotIconAssigner : EditorWindow
{
    [SerializeField] private string prefabsFolder = "Assets/BrainrotPrefabs";
    [SerializeField] private string iconsFolder = "Assets/BrainrotIcons";
    [SerializeField] private bool onlyWhenIconMissing = true;
    [SerializeField] private bool assignIndexWhenMissingIcon = true;

    [MenuItem("Tools/Brainrot/Assign Icons To Prefabs")]
    private static void Open()
    {
        GetWindow<BrainrotIconAssigner>("Brainrot Icon Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Назначение иконок в BrainrotDefinition", EditorStyles.boldLabel);
        GUILayout.Space(6);

        prefabsFolder = EditorGUILayout.TextField("Prefabs Folder", prefabsFolder);
        iconsFolder = EditorGUILayout.TextField("Icons Folder", iconsFolder);
        onlyWhenIconMissing = EditorGUILayout.Toggle("Only if Icon Missing", onlyWhenIconMissing);
        assignIndexWhenMissingIcon = EditorGUILayout.Toggle("Assign Index If Missing Icon", assignIndexWhenMissingIcon);

        GUILayout.Space(10);
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(prefabsFolder) || string.IsNullOrWhiteSpace(iconsFolder)))
        {
            if (GUILayout.Button("Assign Icons", GUILayout.Height(32)))
            {
                AssignIcons();
            }
        }

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Имена иконок должны совпадать с именами префабов.\n" +
            "Если indexId пустой — он будет проставлен как max+1 среди префабов в папке.",
            MessageType.Info);
    }

    private void AssignIcons()
    {
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            Debug.LogError($"Prefabs folder not found: {prefabsFolder}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(iconsFolder))
        {
            Debug.LogError($"Icons folder not found: {iconsFolder}");
            return;
        }

        Dictionary<string, Sprite> spritesByName = LoadSprites(iconsFolder);
        if (spritesByName.Count == 0)
        {
            Debug.LogWarning("No sprites found in icons folder.");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsFolder });
        if (prefabGuids == null || prefabGuids.Length == 0)
        {
            Debug.LogWarning("No prefabs found in prefabs folder.");
            return;
        }

        int maxIndex = FindMaxIndex(prefabGuids);
        int assigned = 0;
        int skipped = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                BrainrotDefinition def = root != null ? root.GetComponent<BrainrotDefinition>() : null;
                if (def == null)
                {
                    skipped++;
                    continue;
                }

                if (onlyWhenIconMissing && def.indexIcon != null)
                {
                    skipped++;
                    continue;
                }

                Sprite sprite = FindSpriteForPrefab(root.name, def, spritesByName);
                if (sprite == null)
                {
                    skipped++;
                    continue;
                }

                def.indexIcon = sprite;
                if (assignIndexWhenMissingIcon && string.IsNullOrWhiteSpace(def.indexId))
                {
                    maxIndex++;
                    def.indexId = maxIndex.ToString();
                }

                EditorUtility.SetDirty(def);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                assigned++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Assign Icons done. Assigned: {assigned}, Skipped: {skipped}");
    }

    private static Dictionary<string, Sprite> LoadSprites(string folder)
    {
        Dictionary<string, Sprite> map = new Dictionary<string, Sprite>();
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                continue;
            }

            if (!map.ContainsKey(sprite.name))
            {
                map.Add(sprite.name, sprite);
            }
        }

        return map;
    }

    private static Sprite FindSpriteForPrefab(string prefabName, BrainrotDefinition def, Dictionary<string, Sprite> map)
    {
        if (!string.IsNullOrWhiteSpace(prefabName) && map.TryGetValue(prefabName, out Sprite sprite))
        {
            return sprite;
        }

        if (def != null)
        {
            if (!string.IsNullOrWhiteSpace(def.displayName) && map.TryGetValue(def.displayName, out sprite))
            {
                return sprite;
            }

            if (!string.IsNullOrWhiteSpace(def.title) && map.TryGetValue(def.title, out sprite))
            {
                return sprite;
            }
        }

        return null;
    }

    private static int FindMaxIndex(string[] prefabGuids)
    {
        int max = 0;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                BrainrotDefinition def = root != null ? root.GetComponent<BrainrotDefinition>() : null;
                if (def == null || string.IsNullOrWhiteSpace(def.indexId))
                {
                    continue;
                }

                if (int.TryParse(def.indexId, out int val))
                {
                    if (val > max)
                    {
                        max = val;
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return max;
    }
}

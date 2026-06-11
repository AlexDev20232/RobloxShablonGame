using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BrainrotIconAssigner : EditorWindow
{
    [SerializeField] private string prefabsFolder = "Assets/_Project/Art/Brainrots/Prefabs";
    [SerializeField] private string iconsFolder = "Assets/_Project/Art/Brainrots/Icons";
    [SerializeField] private bool onlyWhenIconMissing = true;
    [SerializeField] private bool assignIndexWhenMissing = true;

    [MenuItem("Tools/Brainrot/Assign Icons To Prefabs")]
    private static void Open()
    {
        GetWindow<BrainrotIconAssigner>("Brainrot Icon Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign icons to BrainrotDefinition", EditorStyles.boldLabel);
        GUILayout.Space(6);

        prefabsFolder = EditorGUILayout.TextField("Prefabs Folder", prefabsFolder);
        iconsFolder = EditorGUILayout.TextField("Icons Folder", iconsFolder);
        onlyWhenIconMissing = EditorGUILayout.Toggle("Only If Icon Missing", onlyWhenIconMissing);
        assignIndexWhenMissing = EditorGUILayout.Toggle("Assign Index If Missing", assignIndexWhenMissing);

        GUILayout.Space(10);
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(prefabsFolder) || string.IsNullOrWhiteSpace(iconsFolder)))
        {
            if (GUILayout.Button("Assign Icons", GUILayout.Height(32)))
            {
                AssignIcons();
            }
        }

        EditorGUILayout.HelpBox(
            "Icon names should match prefab names, displayName, or title.",
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
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsFolder });
        int maxIndex = FindMaxIndex(prefabGuids);
        int assigned = 0;
        int skipped = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                BrainrotDefinition definition = root != null ? root.GetComponent<BrainrotDefinition>() : null;
                if (definition == null || (onlyWhenIconMissing && definition.indexIcon != null))
                {
                    skipped++;
                    continue;
                }

                Sprite sprite = FindSprite(root.name, definition, spritesByName);
                if (sprite == null)
                {
                    skipped++;
                    continue;
                }

                definition.indexIcon = sprite;
                if (assignIndexWhenMissing && string.IsNullOrWhiteSpace(definition.indexId))
                {
                    maxIndex++;
                    definition.indexId = maxIndex.ToString();
                }

                EditorUtility.SetDirty(definition);
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
        Debug.Log($"Brainrot icon assignment done. Assigned: {assigned}, skipped: {skipped}.");
    }

    private static Dictionary<string, Sprite> LoadSprites(string folder)
    {
        Dictionary<string, Sprite> result = new Dictionary<string, Sprite>();
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && !result.ContainsKey(sprite.name))
            {
                result.Add(sprite.name, sprite);
            }
        }

        return result;
    }

    private static Sprite FindSprite(string prefabName, BrainrotDefinition definition, Dictionary<string, Sprite> sprites)
    {
        if (!string.IsNullOrWhiteSpace(prefabName) && sprites.TryGetValue(prefabName, out Sprite sprite))
        {
            return sprite;
        }

        if (definition != null)
        {
            if (!string.IsNullOrWhiteSpace(definition.displayName) && sprites.TryGetValue(definition.displayName, out sprite))
            {
                return sprite;
            }

            if (!string.IsNullOrWhiteSpace(definition.title) && sprites.TryGetValue(definition.title, out sprite))
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
                BrainrotDefinition definition = root != null ? root.GetComponent<BrainrotDefinition>() : null;
                if (definition != null && int.TryParse(definition.indexId, out int value))
                {
                    max = Mathf.Max(max, value);
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

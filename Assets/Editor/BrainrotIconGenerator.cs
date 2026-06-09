using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BrainrotIconGenerator : EditorWindow
{
    [SerializeField] private Camera captureCamera;
    [SerializeField] private Vector2Int iconSize = new Vector2Int(512, 512);
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private string outputFolder = "Assets/BrainrotIcons";
    [SerializeField] private LayerMask captureLayer = 1;
    [SerializeField] private LayerMask hideLayers;
    [SerializeField] private string[] hideNameContains = { "CanvasPoint", "UIAnchor", "Anchor", "Point" };
    [SerializeField] private bool overwriteExisting = true;
    [SerializeField] private bool usePrefabCameraPoint = true;
    [SerializeField] private string cameraPointName = "IconCamera";
    [SerializeField] private string cameraTargetName = "IconTarget";
    [SerializeField] private Vector3 cameraOffset = Vector3.zero;

    [MenuItem("Tools/Brainrot/Icon Generator")]
    private static void Open()
    {
        GetWindow<BrainrotIconGenerator>("Brainrot Icon Generator");
    }

    private void OnGUI()
    {
        captureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera (Fallback)", captureCamera, typeof(Camera), true);
        iconSize = EditorGUILayout.Vector2IntField("Icon Size", iconSize);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        int layerIndex = EditorGUILayout.LayerField("Capture Layer", GetFirstLayer(captureLayer));
        captureLayer = 1 << Mathf.Clamp(layerIndex, 0, 31);
        hideLayers = LayerMaskField("Hide Layers", hideLayers);
        usePrefabCameraPoint = EditorGUILayout.Toggle("Use Prefab Camera", usePrefabCameraPoint);
        if (usePrefabCameraPoint)
        {
            cameraPointName = EditorGUILayout.TextField("Camera Point Name", cameraPointName);
            cameraTargetName = EditorGUILayout.TextField("Camera Target Name", cameraTargetName);
            cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);
        }

        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("prefabs"), true);
        EditorGUILayout.PropertyField(so.FindProperty("hideNameContains"), true);
        so.ApplyModifiedProperties();

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

        bool canGenerate = prefabs != null && prefabs.Length > 0;
        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button("Generate Icons"))
            {
                Generate();
            }
        }
    }

    private void Generate()
    {
        EnsureFolder(outputFolder);

        RenderTexture rt = new RenderTexture(iconSize.x, iconSize.y, 24, RenderTextureFormat.ARGB32);
        Camera camToUse = captureCamera;
        bool ownsCamera = false;
        if (camToUse == null)
        {
            GameObject camGo = new GameObject("BrainrotIconTempCamera");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            camToUse = camGo.AddComponent<Camera>();
            ownsCamera = true;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
            {
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            string fileName = prefab.name + ".png";
            string iconPath = Path.Combine(outputFolder, fileName).Replace("\\", "/");

            if (!overwriteExisting && File.Exists(iconPath))
            {
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            SetLayerRecursive(instance.transform, GetFirstLayer(captureLayer));

            List<ObjectState> disabled = DisableByRules(instance.transform);

            CameraState camState = new CameraState(camToUse);
            bool prevActive = camToUse.gameObject.activeSelf;
            bool prevEnabled = camToUse.enabled;
            if (!prevActive)
            {
                camToUse.gameObject.SetActive(true);
            }
            camToUse.enabled = true;
            camToUse.targetTexture = rt;
            camToUse.clearFlags = CameraClearFlags.SolidColor;
            camToUse.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camToUse.cullingMask = captureLayer;

            CameraPose prevPose = new CameraPose(camToUse.transform);
            if (usePrefabCameraPoint)
            {
                ApplyCameraPoint(instance.transform, camToUse);
            }

            camToUse.Render();
            Texture2D tex = new Texture2D(iconSize.x, iconSize.y, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, iconSize.x, iconSize.y), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(iconPath, png);

            RestoreDisabled(disabled);
            prevPose.Restore(camToUse.transform);
            camState.Restore(camToUse);
            camToUse.enabled = prevEnabled;
            if (!prevActive)
            {
                camToUse.gameObject.SetActive(false);
            }
            DestroyImmediate(instance);
            DestroyImmediate(tex);

            AssetDatabase.ImportAsset(iconPath);
            SetSpriteImporter(iconPath);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null)
            {
                AssignIconToPrefab(prefabPath, sprite);
            }
        }

        rt.Release();
        DestroyImmediate(rt);
        if (ownsCamera && camToUse != null)
        {
            DestroyImmediate(camToUse.gameObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void AssignIconToPrefab(string prefabPath, Sprite sprite)
    {
        if (string.IsNullOrEmpty(prefabPath) || sprite == null)
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            BrainrotDefinition def = root.GetComponent<BrainrotDefinition>();
            if (def != null)
            {
                def.indexIcon = sprite;
                if (string.IsNullOrWhiteSpace(def.indexId))
                {
                    def.indexId = root.name;
                }
                EditorUtility.SetDirty(def);
            }
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = "Assets";
        string[] parts = folder.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (string.IsNullOrWhiteSpace(part) || part == "Assets")
            {
                continue;
            }

            string next = parent + "/" + part;
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(parent, part);
            }

            parent = next;
        }
    }

    private void SetSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (dirty)
        {
            importer.SaveAndReimport();
        }
    }

    private void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursive(root.GetChild(i), layer);
        }
    }

    private List<ObjectState> DisableByRules(Transform root)
    {
        List<ObjectState> disabled = new List<ObjectState>();
        DisableByRulesRecursive(root, disabled);
        return disabled;
    }

    private void DisableByRulesRecursive(Transform node, List<ObjectState> disabled)
    {
        if (node == null)
        {
            return;
        }

        bool hideByName = MatchesName(node.name);
        bool hideByLayer = ((1 << node.gameObject.layer) & hideLayers.value) != 0;

        if (hideByName || hideByLayer)
        {
            Renderer[] renderers = node.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                {
                    disabled.Add(new ObjectState(renderers[i]));
                    renderers[i].enabled = false;
                }
            }

            Canvas[] canvases = node.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].enabled)
                {
                    disabled.Add(new ObjectState(canvases[i]));
                    canvases[i].enabled = false;
                }
            }
        }

        for (int i = 0; i < node.childCount; i++)
        {
            DisableByRulesRecursive(node.GetChild(i), disabled);
        }
    }

    private bool MatchesName(string name)
    {
        if (hideNameContains == null || hideNameContains.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < hideNameContains.Length; i++)
        {
            string token = hideNameContains[i];
            if (!string.IsNullOrWhiteSpace(token) && name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void RestoreDisabled(List<ObjectState> disabled)
    {
        for (int i = 0; i < disabled.Count; i++)
        {
            disabled[i].Restore();
        }
    }

    private void ApplyCameraPoint(Transform root, Camera cam)
    {
        if (root == null || cam == null || string.IsNullOrWhiteSpace(cameraPointName))
        {
            return;
        }

        Transform point = FindByName(root, cameraPointName);
        if (point == null)
        {
            return;
        }

        cam.transform.position = point.position + cameraOffset;

        Transform target = !string.IsNullOrWhiteSpace(cameraTargetName)
            ? FindByName(root, cameraTargetName)
            : null;

        if (target != null)
        {
            cam.transform.rotation = Quaternion.LookRotation(target.position - cam.transform.position);
        }
        else
        {
            cam.transform.rotation = point.rotation;
        }
    }

    private static Transform FindByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (string.Equals(root.name, targetName, System.StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private struct ObjectState
    {
        private readonly Behaviour _behaviour;
        private readonly Renderer _renderer;
        private readonly bool _enabled;

        public ObjectState(Behaviour behaviour)
        {
            _behaviour = behaviour;
            _renderer = null;
            _enabled = behaviour != null && behaviour.enabled;
        }

        public ObjectState(Renderer renderer)
        {
            _renderer = renderer;
            _behaviour = null;
            _enabled = renderer != null && renderer.enabled;
        }

        public void Restore()
        {
            if (_behaviour != null)
            {
                _behaviour.enabled = _enabled;
            }
            else if (_renderer != null)
            {
                _renderer.enabled = _enabled;
            }
        }
    }

    private struct CameraPose
    {
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;

        public CameraPose(Transform t)
        {
            _position = t.position;
            _rotation = t.rotation;
        }

        public void Restore(Transform t)
        {
            t.position = _position;
            t.rotation = _rotation;
        }
    }

    private struct CameraState
    {
        private readonly CameraClearFlags _clearFlags;
        private readonly Color _background;
        private readonly int _cullingMask;
        private readonly RenderTexture _target;

        public CameraState(Camera cam)
        {
            _clearFlags = cam != null ? cam.clearFlags : CameraClearFlags.Skybox;
            _background = cam != null ? cam.backgroundColor : Color.black;
            _cullingMask = cam != null ? cam.cullingMask : ~0;
            _target = cam != null ? cam.targetTexture : null;
        }

        public void Restore(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            cam.clearFlags = _clearFlags;
            cam.backgroundColor = _background;
            cam.cullingMask = _cullingMask;
            cam.targetTexture = _target;
        }
    }

    private static int GetFirstLayer(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0)
            {
                return i;
            }
        }

        return 0;
    }

    private static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        string[] layers = GetLayerNames();
        int maskWithoutEmpty = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            if (string.IsNullOrEmpty(layers[i]))
            {
                continue;
            }

            maskWithoutEmpty |= 1 << i;
        }

        layerMask.value = EditorGUILayout.MaskField(label, layerMask.value, layers) & maskWithoutEmpty;
        return layerMask;
    }

    private static string[] GetLayerNames()
    {
        string[] layers = new string[32];
        for (int i = 0; i < 32; i++)
        {
            layers[i] = LayerMask.LayerToName(i);
        }
        return layers;
    }
}

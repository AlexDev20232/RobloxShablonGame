// Assets/Scripts/BodyPartsController.cs
// © 2024   (поместите в любую Runtime-папку)

using UnityEngine;
using System.Collections.Generic;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Animator))]
public class BodyPartsController : MonoBehaviour
{
    // ───────────────────────────────────────────────────────────────────── settings
    [System.Serializable]
    public class PartInfo
    {
        [Tooltip("Идентификатор части (любая строка, например \"Head\")")]
        public string id;

        [Tooltip("SkinnedMeshRenderer, который нужно отключить")]
        public GameObject sourceRenderer;

        [Tooltip("Префаб-обломок с MeshRenderer + Rigidbody")]
        public GameObject chunkPrefab;

    }

    [Header("Список запекаемых частей")]
    public List<PartInfo> parts = new List<PartInfo>();


    // <id, экземпляр обломка>
    private readonly Dictionary<string, GameObject> _spawned = new();


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Died();
        }
    }
    // ───────────────────────────────────────────────────────────────────── API
    /// <summary>Развалить все, что указано в части <see cref="parts"/>.</summary>
    public void Died()
    {
        Debug.Log("Died");
        foreach (var p in parts)
            SpawnChunk(p);
    }

    /// <summary>Отсоединить конкретную часть по её id (например «Head»).</summary>
    public void Detach(string id)
    {
        var part = parts.Find(x => x.id == id);
        if (part != null) SpawnChunk(part);
    }

    // ───────────────────────────────────────────────────────────────────── helpers
    private void SpawnChunk(PartInfo info)
    {
        if (info == null || _spawned.ContainsKey(info.id)) return;
        if (info.sourceRenderer == null) return;

        GameObject go;

        // 1. вычисляем мировые координаты нужной кости
        //  Vector3 pos = info.sourceRenderer.bounds.center;
        //  Quaternion rot = info.sourceRenderer.rootBone != null ? 
        //   info.sourceRenderer.rootBone.rotation : 
        //   info.sourceRenderer.transform.rotation;

        // if (info.bakeDynamically || info.chunkPrefab == null)
        // {
        // Запекаем меш динамически и создаем как дочерний объект
        //  go = BakeSkinnedMeshDynamic(info.sourceRenderer, pos, rot);
        //  go.transform.SetParent(this.transform); // Устанавливаем родителем текущий объект

        // Добавляем физику если требуется
        //  if (info.addPhysics)
        //  {
        // AddPhysicsToChunk(go, info.mass);
        //  }
        // }
        //else
        //{
        // Используем готовый префаб и создаем как дочерний объект
        go = Instantiate(info.chunkPrefab, this.transform); // Указываем родителя при создании
        go.transform.localScale = Vector3.one; // scale берётся из вершин
                                               // }

        // 3. запоминаем и затем отцепляем от родителя
        _spawned[info.id] = go;

        // Сохраняем мировые координаты перед отсоединением
        Vector3 worldPos = go.transform.position;
        Quaternion worldRot = go.transform.rotation;

        // Отсоединяем от родителя, сохраняя мировые координаты
        go.transform.SetParent(null, true);

        // Для безопасности явно устанавливаем мировые координаты
        go.transform.position = worldPos;
        go.transform.rotation = worldRot;

        info.sourceRenderer.gameObject.SetActive(false);
    }

    private GameObject BakeSkinnedMeshDynamic(SkinnedMeshRenderer skinned, Vector3 position, Quaternion rotation)
    {
        // 1. Запекаем меш из SkinnedMeshRenderer
        Mesh bakedMesh = new Mesh();
        skinned.BakeMesh(bakedMesh);

        // 2. Создаём новый GameObject (без указания родителя здесь)
        GameObject go = new GameObject(skinned.name + "_Baked");
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = Vector3.one;

        // 3. Добавляем MeshFilter и назначаем запечённый меш
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.mesh = bakedMesh;

        // 4. Добавляем MeshRenderer и копируем материалы
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.materials = skinned.materials;

        // 5. Копируем настройки рендерера
        mr.shadowCastingMode = skinned.shadowCastingMode;
        mr.receiveShadows = skinned.receiveShadows;
        mr.lightProbeUsage = skinned.lightProbeUsage;
        mr.reflectionProbeUsage = skinned.reflectionProbeUsage;

        // Если это редактор, сохраняем меш как ассет
#if UNITY_EDITOR
        SaveMeshAsAsset(bakedMesh, skinned.name);
#endif

        return go;
    }

    private void AddPhysicsToChunk(GameObject chunk, float mass)
    {
        // Добавляем Rigidbody
        Rigidbody rb = chunk.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Добавляем MeshCollider на основе MeshFilter
        MeshCollider mc = chunk.AddComponent<MeshCollider>();
        mc.sharedMesh = chunk.GetComponent<MeshFilter>().mesh;
        mc.convex = true;
    }

#if UNITY_EDITOR
    private string SaveMeshAsAsset(Mesh mesh, string baseName)
    {
        // Создаем директорию если её нет
        string directory = "Assets/BakedMeshes";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        // Формируем уникальное имя для файла
        string assetPath = $"{directory}/{baseName}_Baked_{System.DateTime.Now.Ticks}.asset";

        // Сохраняем меш как asset
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        return assetPath;
    }
#endif

    public void DeleteRandomPart()
    {
        if (parts.Count == 0) return;

        // Создаем временный список без частей с id = "Body"
        var availableParts = parts.FindAll(part => part.id != "Body");

        // Если все части - "Body", или список пуст, выходим
        if (availableParts.Count == 0) return;

        // Выбираем случайную часть из отфильтрованного списка
        PartInfo p = availableParts[Random.Range(0, availableParts.Count)];
        SpawnChunk(p);
    }

    void OnTriggerEnter(Collider other)
    {
       // if(other.CompareTag("Laser"))
        //{
         //   GetComponent<vHealthController>().AddHealth(-150);
          //  Died();
       // }
    }
}
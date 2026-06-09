using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform spawnPoint;
    public Transform endPoint;

    [Header("Wave Prefabs (Random)")]
    public GameObject[] wavePrefabs;

    [Header("Spawn Timing (seconds)")]
    public float minSpawnDelay = 3f;
    public float maxSpawnDelay = 7f;

    [Header("Move Mode")]
    public bool useEndPointAsTarget = true;
    public Vector3 defaultDirection = Vector3.forward;

    private float _timer;
    private float _nextDelay;

    private void Start()
    {
        ScheduleNext();
    }

    private void Update()
    {
        if (spawnPoint == null || wavePrefabs == null || wavePrefabs.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= _nextDelay)
        {
            _timer = 0f;
            SpawnWave();
            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        float a = Mathf.Max(0.01f, minSpawnDelay);
        float b = Mathf.Max(a, maxSpawnDelay);
        _nextDelay = Random.Range(a, b);
    }

    private void SpawnWave()
    {
        GameObject prefab = wavePrefabs[Random.Range(0, wavePrefabs.Length)];
        if (prefab == null) return;

        GameObject wave = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        WaveMover mover = wave.GetComponent<WaveMover>();
        if (mover == null) mover = wave.AddComponent<WaveMover>();

        Vector3 dir = defaultDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = spawnPoint.forward;

        mover.targetPoint = endPoint;
        mover.useTargetPoint = useEndPointAsTarget;
        mover.direction = dir.normalized;
    }
}

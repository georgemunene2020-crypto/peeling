using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    public GameObject[] chunkPrefabs;

    [Header("Player")]
    public Transform playerTransform;

    [Header("Spawner Settings")]
    [Tooltip("Length (Z-axis) of each chunk. Must match the actual geometry length.")]
    public float chunkLength      = 54f;
    [Tooltip("How many chunks to keep ahead of the player at all times.")]
    public int   chunksAhead      = 5;
    [Tooltip("Units past the end of a chunk before it gets destroyed.")]
    public float safeDeleteBuffer = 10f;

    // index 0 is always the oldest chunk, last index is the newest
    private readonly List<GameObject> activeChunks = new List<GameObject>();
    private float spawnZ = 0f;

    void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[ChunkSpawner] Player Transform is not assigned.");
            enabled = false;
            return;
        }

        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("[ChunkSpawner] No chunk prefabs assigned.");
            enabled = false;
            return;
        }

        // always start with chunk 0 (should be a safe flat one), then randomise the rest
        SpawnChunk(0);
        for (int i = 1; i < chunksAhead; i++)
            SpawnChunk(Random.Range(0, chunkPrefabs.Length));
    }

    void Update()
    {
        // chunks are only used in endless mode
        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Maze)
            return;

        if (activeChunks.Count == 0) return;

        // while loop instead of if so a really fast player doesn't outrun the spawner
        while (ShouldDeleteOldestChunk())
        {
            DeleteOldestChunk();
            SpawnChunk(Random.Range(0, chunkPrefabs.Length));
        }
    }

    // checks if the player has moved far enough past the oldest chunk that we can delete it
    private bool ShouldDeleteOldestChunk()
    {
        if (activeChunks.Count == 0) return false;

        float chunkEndZ       = activeChunks[0].transform.position.z + chunkLength;
        float deleteThreshold = chunkEndZ + safeDeleteBuffer;

        return playerTransform.position.z > deleteThreshold;
    }

    private void SpawnChunk(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= chunkPrefabs.Length) return;

        GameObject newChunk = Instantiate(
            chunkPrefabs[prefabIndex],
            Vector3.forward * spawnZ,
            Quaternion.identity
        );

        activeChunks.Add(newChunk);
        spawnZ += chunkLength;
    }

    private void DeleteOldestChunk()
    {
        if (activeChunks.Count == 0) return;
        Destroy(activeChunks[0]);
        activeChunks.RemoveAt(0);
    }

    // draws chunk start/end lines in the scene view so you can see what's active
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < activeChunks.Count; i++)
        {
            if (activeChunks[i] == null) continue;
            Vector3 start = activeChunks[i].transform.position;
            Vector3 end   = start + Vector3.forward * chunkLength;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(end, new Vector3(5f, 0.1f, 0.1f));
        }
    }
}
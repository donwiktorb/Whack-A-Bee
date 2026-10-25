
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Target Prefabs")]
    [SerializeField] private WhackTarget goodTargetPrefab;
    [SerializeField] private WhackTarget bombPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.2f;

    [Range(0f, 1f)]
    [SerializeField] private float bombChance = 0.2f;

    private WhackTarget[] activeTargets;
    private Coroutine spawnRoutine;

    public void StartSpawning()
    {
        if (spawnRoutine != null)
            return;

        if (spawnPoints == null || spawnPoints.Length == 0 ||
            goodTargetPrefab == null || bombPrefab == null)
        {
            Debug.LogError("TargetSpawner is not configured!");
            return;
        }

        activeTargets = new WhackTarget[spawnPoints.Length];
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (activeTargets == null)
            return;

        foreach (WhackTarget target in activeTargets)
        {
            if (target != null)
                Destroy(target.gameObject);
        }

        activeTargets = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnTarget();
            yield return new WaitForSeconds(
                Mathf.Max(0.1f, spawnInterval)
            );
        }
    }

    private void SpawnTarget()
    {
        List<int> freeHoles = new List<int>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && activeTargets[i] == null)
                freeHoles.Add(i);
        }

        if (freeHoles.Count == 0)
            return;

        int holeIndex = freeHoles[
            Random.Range(0, freeHoles.Count)
        ];

        WhackTarget prefab = Random.value < bombChance ? bombPrefab : goodTargetPrefab;
        WhackTarget target = Instantiate(
            prefab,
            spawnPoints[holeIndex],
            false
        );

        target.gameObject.SetActive(true);
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;

        activeTargets[holeIndex] = target;
        target.Begin();
    }
}

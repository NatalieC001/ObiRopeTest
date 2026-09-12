using UnityEngine;

/// <summary>
/// This script holds extra settings for paths created by the Spline Path Generator.
/// It does NOT contain any movement or spawning logic. It only holds values
/// that your existing creature or manager scripts can read when loading a path.
/// </summary>
public class PathSpawnInfo : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The type of enemy or object to spawn along this path.")]
    public GameObject enemyPrefab;

    [Tooltip("How many enemies should spawn along this path.")]
    [Min(1)]
    public int spawnCount = 1;

    [Header("Movement Settings")]
    [Tooltip("The intended speed for creatures moving on this path.")]
    public float movementSpeed = 5f;

    [Header("Path State")]
    [Tooltip("Is this path open (for attack runs) or closed (for ambient loops)?")]
    public bool isClosed = true;
}

using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float playerHealth = 100f;
    [Header("References")]
    
    private GameObject SpawnPointParent;
    private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        MapManager.Instance.OnMapLoaded.AddListener(RegisterSpawnpoints);
    }

    private void RegisterSpawnpoints(string mapName)
    {
        Instance.SpawnPointParent = GameObject.FindGameObjectWithTag("SpawnPoints");

        Instance.spawnPoints = SpawnPointParent.GetComponentsInChildren<Transform>();
    }

    public float GetPlayerHealth() => Instance.playerHealth;

    public void GetSpawnPosition(out Vector3 position, out Quaternion rotation)
    {
        position = Instance.spawnPoints[Random.Range(0, Instance.spawnPoints.Length)].position;
        rotation = Quaternion.identity;
    }
}

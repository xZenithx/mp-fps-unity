using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float playerHealth = 100f;
    [SerializeField] private string playerWeapon = "ak47";
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
        SpawnPointParent = GameObject.FindGameObjectWithTag("SpawnPoints");

        spawnPoints = SpawnPointParent.GetComponentsInChildren<Transform>();
    }

    public float GetPlayerHealth() => playerHealth;
    public string GetPlayerWeapon() => playerWeapon;

    public void GetSpawnPosition(out Vector3 position, out Quaternion rotation)
    {
        position = spawnPoints[Random.Range(0, Instance.spawnPoints.Length)].position;
        rotation = Quaternion.identity;
    }
}

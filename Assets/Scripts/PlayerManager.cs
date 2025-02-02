using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }
    [SerializeField] private GameObject RespawnCanvas;

    public UnityEvent OnLocalPlayerSpawned;
    private Transform _initialSpawnPoint;

    public List<Player> Players = new();
    [SerializeField] private Camera[] mapCameras;
    
    public override void OnNetworkSpawn()
    {
        OnLocalPlayerSpawned = new UnityEvent();

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _initialSpawnPoint = GameObject.FindGameObjectWithTag("Initial Spawnpoint").transform;

        if (IsClient)
        {
            MapManager.Instance.OnMapLoaded.AddListener(RegisterMapCameras);
            OnLocalPlayerSpawned.AddListener(DisableAllMapCameras);
            OnLocalPlayerSpawned.AddListener(EnableLocalPlayerCamera);
            RegisterMapCameras("");
            EnableRandomMapCamera();

            OpenRespawnMenu();
        }

        if (IsHost)
        {
            SetHostMaxHealth();
        }
        
        // loop over all connected players and add them to the list
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                if (client.PlayerObject.TryGetComponent<Player>(out var player))
                {
                    RecordPlayer(player);
                }
            }
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        // At this point we must use the UnityEngine's SceneManager to switch back to the MainMenu
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private async Task<bool> WaitForPlayerHealthValid(Player player)
    {
        while (player.GetComponent<PlayerHealth>() == null)
        {
            await Task.Yield();
        }

        while (GameManager.Instance == null)
        {
            await Task.Yield();
        }

        return true;
    }

    private async void SetHostMaxHealth()
    {
        Player player = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Player>();
        
        await player.WaitForPlayerReady();
        await WaitForPlayerHealthValid(player);

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.SetMaxHealthServerRpc(GameManager.Instance.GetPlayerHealth());
    }

    private void OnClientConnected(ulong clientId)
    {
        NetworkClient player = NetworkManager.Singleton.ConnectedClients[clientId];
        if (player.PlayerObject != null && player.PlayerObject.TryGetComponent<Player>(out var playerComponent))
        {
            RecordPlayer(playerComponent);

            if (!IsServer) return;

            ClientRpcParams clientRpcParams = new()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            SetPositionClientRpc(_initialSpawnPoint.position, _initialSpawnPoint.rotation, clientRpcParams);
        }
    }

    private void RecordPlayer(Player player)
    {
        Players.Add(player);

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        // Is this me?
        if (player.IsLocalPlayer)
        {
            playerHealth.OnDeath.AddListener(OpenRespawnMenu);
            OnLocalPlayerSpawned.AddListener(player.OnSpawn);

            playerHealth.OnDeath.AddListener(() => {
                Debug.Log("Client.playerHealth.OnDeath: " + player.OwnerClientId);

                SetPosition(_initialSpawnPoint.position);
            });
        }
        else if (IsServer)
        {
            playerHealth.SetMaxHealthServerRpc(GameManager.Instance.GetPlayerHealth());
            
            // playerHealth.OnDeath.AddListener(
            //     () => {
            //         Debug.Log("Server.playerHealth.OnDeath: " + player.OwnerClientId);

            //         ClientRpcParams clientRpcParams = new()
            //         {
            //             Send = new ClientRpcSendParams
            //             {
            //                 TargetClientIds = new ulong[] { player.OwnerClientId }
            //             }
            //         };
            //         SetPositionClientRpc(_initialSpawnPoint.position, Quaternion.identity, clientRpcParams);
            //     }
            // );
        }
    }

    public void HealPlayer(Player player)
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.SetMaxHealthServerRpc(GameManager.Instance.GetPlayerHealth());
        playerHealth.SetHealthServerRpc(playerHealth.GetMaxHealth());
    }

    [ClientRpc]
    public void SetPositionClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams rpcParams = default)
    {
        Debug.Log("SetPositionClientRpc " + NetworkManager.Singleton.LocalClientId + ", position: " + position + " rotation: " + rotation + " isOwner: " + IsOwner + " isClient: " + IsClient);

        SetPosition(position);
    }

    public void SetPosition(Vector3 position)
    {
        if (IsServer && !IsHost)
        {
            Debug.Log("SetPosition: IsServer && !IsHost");
            
            return;
        }

        Debug.Log($"Setting position for {NetworkManager.LocalClientId} to {position}");

        try 
        {
            // Directly access the player character through the NetworkObject
            var playerCharacter = NetworkManager.Singleton.LocalClient.PlayerObject
                .GetComponentInChildren<PlayerCharacter>();
            
            // Use NetworkTransform for proper syncing
            playerCharacter.SetPosition(position);
        }
        catch (System.Exception e) 
        {
            Debug.LogError($"SetPosition failed: {e.Message}");
        }
    }

    public void OpenRespawnMenu()
    {
        RespawnCanvas.SetActive(true);
        DisableLocalPlayerCamera();
        EnableRandomMapCamera();

        // Show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseRespawnMenu()
    {
        RespawnCanvas.SetActive(false);
        EnableLocalPlayerCamera();

        // Hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private async void RegisterMapCameras(string _)
    {
        Debug.Log("RegisterMapCameras_preawait");
        GameObject mapCamerasObject = await WaitForMapCamerasObject();
        Debug.Log("RegisterMapCameras_postawait");

        Instance.mapCameras = mapCamerasObject.GetComponentsInChildren<Camera>();
    }

    private async Task<GameObject> WaitForMapCamerasObject()
    {
        while (GameObject.FindGameObjectWithTag("Map Cameras") == null)
        {
            await Task.Yield();
        }

        return GameObject.FindGameObjectWithTag("Map Cameras");
    }

    public void DisableAllMapCameras()
    {
        for (int i = 0; i < Instance.mapCameras.Length; i++)
        {
            Instance.mapCameras[i].enabled = false;
            Instance.mapCameras[i].gameObject.GetComponent<AudioListener>().enabled = false;
        }
    }

    public void EnableRandomMapCamera()
    {
        if (Instance.mapCameras == null || Instance.mapCameras.Length == 0)
        {
            Debug.LogWarning("No map cameras found!");

            return;
        }
        DisableAllMapCameras();
        Instance.mapCameras[Random.Range(0, Instance.mapCameras.Length)].enabled = true;
        Instance.mapCameras[Random.Range(0, Instance.mapCameras.Length)].gameObject.GetComponent<AudioListener>().enabled = true;
    }

    public void EnableLocalPlayerCamera()
    {
        Debug.Log("EnableLocalPlayerCamera");
        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer");
        if (localPlayer == null) return;
        Camera localPlayerCamera = localPlayer.GetComponentInChildren<Camera>();
        localPlayerCamera.enabled = true;
        localPlayerCamera.gameObject.GetComponent<AudioListener>().enabled = true;
    }

    public void DisableLocalPlayerCamera()
    {
        Debug.Log("DisableLocalPlayerCamera");
        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer");
        if (localPlayer == null) return;
        Camera localPlayerCamera = localPlayer.GetComponentInChildren<Camera>();
        localPlayerCamera.enabled = false;
        localPlayerCamera.gameObject.GetComponent<AudioListener>().enabled = false;
    }

    // Add this new method to handle client ownership
    public void InitializeLocalPlayer(Player localPlayer)
    {
        if (localPlayer.IsOwner)
        {
            localPlayer.OnRespawnRequested.AddListener(HandleRespawnRequest);
        }
    }

    private void HandleRespawnRequest()
    {
        RequestSpawn();
    }

    public void RequestSpawn()
    {
        Debug.Log("RequestSpawn " + NetworkManager.Singleton.LocalClientId + " isOwner: " + IsOwner + " isClient: " + IsClient);
        if (IsClient && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong playerId)
    {
        Debug.Log("SpawnPlayerServerRpc " + playerId + " isOwner: " + IsOwner + " isClient: " + IsClient);
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client)) return;
        if (client.PlayerObject == null) return;

        // Set players health to max
        Player player = client.PlayerObject.GetComponent<Player>();
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.SetHealthServerRpc(playerHealth.GetMaxHealth());

        GameManager.Instance.GetSpawnPosition(out Vector3 position, out Quaternion rotation);
        SpawnCompleteClientRpc(position, rotation, new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = new ulong[] { playerId }
            }
        });
    }

    [ClientRpc]
    private void SpawnCompleteClientRpc(Vector3 spawnPosition, Quaternion spawnAngle, ClientRpcParams rpcParams = default)
    {
        Debug.Log("SpawnCompleteClientRpc " + NetworkManager.Singleton.LocalClientId + " isOwner: " + IsOwner + " isClient: " + IsClient);
        if (!IsClient && !IsHost) return;

        CloseRespawnMenu();

        SetPosition(spawnPosition);

        // Run OnSpawn from Player.cs

        OnLocalPlayerSpawned.Invoke();
    }

    // Keep other methods the same but remove singleton references from them
    // Replace Instance.mapCameras with mapCameras
    // Remove static Instance references from all methods
}
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance { get; private set; }

    public string MapName = "Map";
    public Scene MapScene;
    private Scene m_loadedScene;
    private bool m_isSceneOperationInProgress;
    private bool m_hasLoadedInitialScene;
    private bool m_hasLoadedMap = false;

    public bool HasLoadedMap => m_hasLoadedMap;

    public string[] Maps;

    public UnityEvent<string> OnMapLoaded;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        Instance.OnMapLoaded = new UnityEvent<string>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("MapManager spawned!");

        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
    }

    public async Task<bool> WaitForMapAsync()
    {
        while (!Instance.HasLoadedMap)
        {
            await Task.Yield();
        }

        Debug.Log("<Task> Map loaded!");
        return true;
    }

    public bool IsMapLoaded => Instance.m_loadedScene != null && Instance.m_loadedScene.IsValid() && Instance.m_loadedScene.isLoaded;
    
    public void LoadRandomMap()
    {
        LoadMap(Instance.Maps[Random.Range(0, Instance.Maps.Length)]);
    }

    public async void LoadMap(string mapName)
    {
        if (Instance.m_isSceneOperationInProgress)
        {
            Debug.LogWarning("Scene operation already in progress!");
            return;
        }

        // Check if network manager is still alive
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!ServerSideSceneValidation(0, mapName, LoadSceneMode.Additive))
        {
            UnloadMap();

            await Task.Delay(500);
        }

        Debug.Log($"Loading the {mapName} scene.");
        Instance.m_isSceneOperationInProgress = true; // Set flag

        Instance.MapName = mapName;

        var status = NetworkManager.SceneManager.LoadScene(
            Instance.MapName, 
            LoadSceneMode.Additive
        );

        CheckStatus(status, true);
    }

    public void UnloadMap()
    {
        if (!IsServer || !IsSpawned || !Instance.m_loadedScene.IsValid() || !Instance.m_loadedScene.isLoaded)
        {
            return;
        }
        // Unload the map
        Debug.Log($"Unloading the {Instance.m_loadedScene.name} scene.");
        SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.UnloadScene(Instance.m_loadedScene);
        CheckStatus(status, false);

        Instance.m_loadedScene = default;
    }

    private async void CheckStatus(SceneEventProgressStatus status, bool isLoading = true)
    {
        await Task.Yield();
        var sceneEventAction = isLoading ? "load" : "unload";
        
        if (status == SceneEventProgressStatus.SceneEventInProgress)
        {
            Debug.LogWarning($"Another scene operation is already in progress. Retrying...");
            // Optionally retry after a delay
            await Task.Delay(1000);
            LoadMap(Instance.MapName); // Retry load
        }
        else if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to {sceneEventAction} {Instance.MapScene.name}: {status}");
        }
    }

    private bool ServerSideSceneValidation(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
    {
        // Allow initial scene load
        if (!Instance.m_hasLoadedInitialScene) return true;

        // Block duplicate additive loads
        return !(sceneName == Instance.m_loadedScene.name && loadSceneMode == LoadSceneMode.Additive);
    }

    // [ClientRpc]
    // private void OnMapLoadedClientRpc(string mapName)
    // {
    //     Debug.Log("OnMapLoadedClientRpc | Map loaded: " + mapName);
    //     OnMapLoaded.Invoke(mapName);
    // }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
            {
                if (!Instance.m_hasLoadedInitialScene && sceneEvent.SceneName == "GameScene")
                {
                    Instance.m_hasLoadedInitialScene = true;
                    LoadRandomMap(); // Now load the map additively

                    break;
                }

                // We want to handle this for only the server-side
                if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                {
                    // *** IMPORTANT ***
                    // Keep track of the loaded scene, you need this to unload it
                    Instance.m_loadedScene = sceneEvent.Scene;
                    Debug.Log(clientOrServer + "> m_loadedScene: " + Instance.m_loadedScene.name);
                }
                Debug.Log($"Loaded the {sceneEvent.SceneName} scene on " +
                    $"{clientOrServer}-({sceneEvent.ClientId}).");
                
                Instance.OnMapLoaded.Invoke(sceneEvent.SceneName);
                // OnMapLoadedClientRpc(sceneEvent.SceneName);
                m_isSceneOperationInProgress = false; // Reset flag
                m_hasLoadedMap = true;
                break;
            }
                
            case SceneEventType.UnloadComplete:
            {
                Debug.Log($"Unloaded the {sceneEvent.SceneName} scene on " +
                    $"{clientOrServer}-({sceneEvent.ClientId}).");
                
                Instance.m_hasLoadedMap = false;
                break;
            }
            case SceneEventType.LoadEventCompleted:
            case SceneEventType.UnloadEventCompleted:
            {
                var loadUnload = sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ? "Load" : "Unload";
                Debug.Log($"{loadUnload} event completed for the following client " +
                    $"identifiers:");
                foreach (var clientId in sceneEvent.ClientsThatCompleted)
                {
                    Debug.Log($"- {clientId}");
                }
                if (sceneEvent.ClientsThatTimedOut.Count > 0)
                {
                    Debug.LogWarning($"{loadUnload} event timed out for the following client " +
                        $"identifiers:({sceneEvent.ClientsThatTimedOut})");
                }
                break;
            }
        }
    }
}
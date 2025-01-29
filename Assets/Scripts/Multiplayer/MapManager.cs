using System.Threading.Tasks;
using Eflatun.SceneReference;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance { get; private set; }

    public string MapName = "Map";
    public Scene MapScene;
    private Scene m_loadedScene;
    private bool m_isSceneOperationInProgress;
    private bool m_hasLoadedInitialScene;

    public string[] Maps;

    void Awake()
    {
        Instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            
        }

        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
    }



    public bool IsMapLoaded => m_loadedScene != null && m_loadedScene.IsValid() && m_loadedScene.isLoaded;
    
    public void LoadRandomMap()
    {
        LoadMap(Maps[Random.Range(0, Maps.Length)]);
    }

    public async void LoadMap(string mapName)
    {
        if (m_isSceneOperationInProgress)
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
        m_isSceneOperationInProgress = true; // Set flag

        MapName = mapName;

        var status = NetworkManager.SceneManager.LoadScene(
            mapName, 
            LoadSceneMode.Additive
        );

        CheckStatus(status, true);

        await Task.Delay(5000);
        LoadMap(MapName);
    }

    public void UnloadMap()
    {
        if (!IsServer || !IsSpawned || !m_loadedScene.IsValid() || !m_loadedScene.isLoaded)
        {
            return;
        }
        // Unload the map
        Debug.Log($"Unloading the {m_loadedScene.name} scene.");
        SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.UnloadScene(m_loadedScene);
        CheckStatus(status, false);

        m_loadedScene = default;
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
            LoadMap(MapName); // Retry load
        }
        else if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to {sceneEventAction} {MapScene.name}: {status}");
        }
    }

    private bool ServerSideSceneValidation(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
    {
        // Allow initial scene load
        if (!m_hasLoadedInitialScene) return true;

        // Block duplicate additive loads
        return !(sceneName == m_loadedScene.name && loadSceneMode == LoadSceneMode.Additive);
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
            {
                if (!m_hasLoadedInitialScene && sceneEvent.SceneName == "GameScene")
                {
                    m_hasLoadedInitialScene = true;
                    LoadRandomMap(); // Now load the map additively

                    break;
                }

                // We want to handle this for only the server-side
                if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                {
                    // *** IMPORTANT ***
                    // Keep track of the loaded scene, you need this to unload it
                    m_loadedScene = sceneEvent.Scene;
                    Debug.Log(clientOrServer + "> m_loadedScene: " + m_loadedScene.name);
                }
                Debug.Log($"Loaded the {sceneEvent.SceneName} scene on " +
                    $"{clientOrServer}-({sceneEvent.ClientId}).");
                
                m_isSceneOperationInProgress = false; // Reset flag
                break;
            }
                
            case SceneEventType.UnloadComplete:
            {
                Debug.Log($"Unloaded the {sceneEvent.SceneName} scene on " +
                    $"{clientOrServer}-({sceneEvent.ClientId}).");
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
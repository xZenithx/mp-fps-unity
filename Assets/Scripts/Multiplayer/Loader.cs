using Eflatun.SceneReference;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public static class Loader {
    public static void LoadNetwork(SceneReference scene) {
        NetworkManager.Singleton.SceneManager.LoadScene(scene.Name, LoadSceneMode.Single);
    }
}
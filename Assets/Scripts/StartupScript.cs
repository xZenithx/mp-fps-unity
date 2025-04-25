using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupScript : MonoBehaviour
{
    public async void Start()
    {
        await WaitForPauseManager();
        await WaitForNetworkManager();
        await WaitForMultiplayerService();

        // Switch to the next scene in the build
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private async Task WaitForPauseManager()
    {
        while (PauseManager.Instance == null)
        {
            await Task.Delay(100);
        }
    }

    private async Task WaitForNetworkManager()
    {
        while (NetworkManager.Singleton == null)
        {
            await Task.Delay(100);
        }
    }

    private async Task WaitForMultiplayerService()
    {
        while (Multiplayer.Instance == null)
        {
            await Task.Delay(100);
        }
    }

}

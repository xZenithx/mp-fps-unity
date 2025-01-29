using TMPro;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_InputField inputField;
    public async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void JoinRelay()
    {
        string joinCode = inputField.text;

        if (joinCode.Length == 0) return;

        bool success = await StartClientWithRelay(joinCode);

        if (success)
        {
            inputField.text = "";

            PauseManager.Instance.DisablePauseMenu();
        }
    }

    public async void StartRelay()
    {
        string joinCode = await StartHostWithRelay();

        if (joinCode != null)
        {
            joinCodeText.text = joinCode;

            PauseManager.Instance.DisablePauseMenu();
        }
    }

    private async Task<string> StartHostWithRelay(int maxConnections = 50)
    {
        Allocation allocation;
        try 
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return null;
        }
        RelayServerData serverData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    private async Task<bool> StartClientWithRelay(string joinCode)
    {
        JoinAllocation joinAllocation;
        try
        {
            joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }

        RelayServerData serverData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
}

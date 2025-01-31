using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using UnityEngine.UI;
using System.Threading.Tasks;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float displayHealth;

    public NetworkVariable<float> _health = new(
        readPerm: NetworkVariableReadPermission.Everyone, 
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> _maxHealth = new(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    public bool IsAlive() => _health.Value > 0;

    [SerializeField] private Slider HealthSlider;
    private NetworkObject _networkObject;

    public override void OnNetworkSpawn()
    {
        if (IsServer || IsHost)
        {
            _networkObject = GetComponent<NetworkObject>();
        }
        if (IsClient)
        {
            _health.OnValueChanged += (_, current) => 
            {
                UpdateHealthSlider();
                displayHealth = current;
            };
        }
    }

    public void Initialize()
    {
        if (IsOwner)
        {
            OnDeath.AddListener(() =>
            {
                PlayerManager.Instance.OpenRespawnMenu();
            });

            // _health.OnValueChanged += (_, current) => 
            // {
            //     HealthSlider.value = GetHealthPercentage();
            //     displayHealth = current;
            // };

            _ = WaitForHealthSlider();
        }
    }

    private async Task<bool> WaitForHealthSlider()
    {
        while (GameObject.FindGameObjectWithTag("Healthbar") == null)
        {
            await Task.Yield();
        }

        HealthSlider = GameObject.FindGameObjectWithTag("Healthbar").GetComponent<Slider>();
        UpdateHealthSlider();
        return true;
    }

    public void UpdateHealthSlider()
    {
        if (HealthSlider == null) return;

        HealthSlider.value = GetHealthPercentage();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetHealthServerRpc(float health)
    {
        if (!IsServer && !IsHost) return;

        _health.Value = Mathf.Clamp(health, 0, _maxHealth.Value);
        OnHealthChanged.Invoke(_health.Value);
        if (_health.Value <= 0)
        {
            OnDeath.Invoke();
        }
    }

    public float GetHealth() => _health.Value;
    
    [ServerRpc(RequireOwnership = false)]
    public void SetMaxHealthServerRpc(float maxHealth)
    {
        if (!IsServer && !IsHost) return;

        _maxHealth.Value = maxHealth;
    }

    public float GetMaxHealth() => _maxHealth.Value;
    public float GetHealthPercentage() => _health.Value / _maxHealth.Value;

    public void TakeDamageServer(DamageData data)
    {
        if (!IsServer && !IsHost) return;

        try
        {
            NetworkObject sourceNetworkObject = data.Source;
            NetworkObject victimNetworkObject = GetComponent<NetworkObject>();


            Debug.Log($"Server> {victimNetworkObject} took {data.Damage} damage from {sourceNetworkObject}");

            _health.Value -= data.Damage;
            OnHealthChanged.Invoke(_health.Value);
            if (_health.Value <= 0)
            {
                OnDeath.Invoke();
                OnDeathServer(_networkObject.OwnerClientId);
            }
        

            ClientRpcParams clientRpcParams = new()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{_networkObject.OwnerClientId}
                }
            };
            TakeDamageClientRpc(_health.Value, clientRpcParams);
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float newHealth, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        Debug.Log("Client> TakeDamageClientRpc: newHealth, " + newHealth);

        OnHealthChanged.Invoke(newHealth);
    }

    public void OnDeathServer(ulong clientId)
    {
        Debug.Log("Server> OnDeathServer");
        if (!IsServer) return;

        ClientRpcParams clientRpcParams = new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{clientId}
            }
        };
        DeathClientRpc(clientRpcParams);
    }

    [ClientRpc]
    public void DeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Client> DeathClientRpc {IsOwner} {IsClient} {IsServer} {IsHost}");
        if (IsServer && !IsHost) return;
        Debug.Log(OnDeath.GetPersistentEventCount());

        OnDeath.Invoke();
    }
}
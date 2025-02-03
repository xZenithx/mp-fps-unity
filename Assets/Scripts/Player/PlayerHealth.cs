using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using UnityEngine.UI;
using System.Threading.Tasks;


public class PlayerHealth : NetworkBehaviour
{
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

    public void Initialize()
    {
        if (IsOwner)
        {
            // OnDeath.AddListener(() =>
            // {
            //     PlayerManager.Instance.OpenRespawnMenu();
            // });

            WaitForHealthSlider();
        }
    }

    private async void WaitForHealthSlider()
    {
        while (GameObject.FindGameObjectWithTag("Healthbar") == null)
        {
            await Task.Yield();
        }

        Debug.Log("Client> Found HealthSlider");
        HealthSlider = GameObject.FindGameObjectWithTag("Healthbar").GetComponent<Slider>();
    }

    public void UpdateHealthSlider(float deltaTime)
    {
        if (HealthSlider == null)
        {
            Debug.LogWarning("HealthSlider is null");
            return;
        }

        float hpPercent = GetHealthPercentage();
        
        if (float.IsNaN(hpPercent))
        {
            Debug.LogWarning("hpPercent is NaN");
            return;
        }

        hpPercent = Mathf.Clamp(hpPercent, 0, 1);

        HealthSlider.value = Mathf.Lerp(
            HealthSlider.value,
            hpPercent,
            -1f + Mathf.Exp(16f * deltaTime)
        );
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
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
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetMaxHealthServerRpc(float maxHealth)
    {
        if (!IsServer && !IsHost) return;

        _maxHealth.Value = maxHealth;
    }

    public float GetMaxHealth() => _maxHealth.Value;
    public float GetHealthPercentage()
    {
        return _health.Value / _maxHealth.Value;
    }

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
                OnDeathServer(OwnerClientId);
            }
        
            TakeDamageClientRpc(_health.Value, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void TakeDamageClientRpc(float newHealth, RpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        Debug.Log("Client> TakeDamageClientRpc: newHealth, " + newHealth);

        OnHealthChanged.Invoke(newHealth);
    }

    public void OnDeathServer(ulong clientId)
    {
        Debug.Log("Server> OnDeathServer");
        if (!IsServer) return;

        DeathClientRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void DeathClientRpc(RpcParams rpcParams = default)
    {
        Debug.Log($"Client> DeathClientRpc {IsOwner} {IsClient} {IsServer} {IsHost}");
        if (IsServer && !IsHost) return;
        Debug.Log(OnDeath.GetPersistentEventCount());

        OnDeath.Invoke();
    }
}
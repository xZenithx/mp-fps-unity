using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    public float Health => _health.Value;
    private readonly NetworkVariable<float> _health = new();
    public float MaxHealth => _maxHealth.Value;
    private readonly NetworkVariable<float> _maxHealth = new();

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    public override void OnNetworkSpawn()
    {
        if (!IsSessionOwner)
        {
            return;
        }

        GameManager gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        SetMaxHealthServerRpc(
            gameManager.GetPlayerHealth()
        );
        SetHealthServerRpc(
            gameManager.GetPlayerHealth()
        );

        Slider healthSlider = GameObject.FindGameObjectWithTag("HealthSlider").GetComponent<Slider>();
        _health.OnValueChanged += (prev, current) => {
            healthSlider.value = GetHealthPercentage();
        };
    }

    [ServerRpc]
    public void SetHealthServerRpc(float health)
    {
        _health.Value = Mathf.Clamp(health, 0, MaxHealth);
        OnHealthChanged.Invoke(_health.Value);
        if (_health.Value <= 0)
        {
            OnDeath.Invoke();
        }
    }

    public float GetHealth() => Health;
    
    [ServerRpc]
    public void SetMaxHealthServerRpc(float maxHealth)
    {
        _maxHealth.Value = maxHealth;
    }

    public float GetMaxHealth() => MaxHealth;
    public float GetHealthPercentage() => Health / MaxHealth;

    [ServerRpc]
    public void TakeDamageServerRpc(DamageData data)
    {
        _health.Value -= data.Damage;
        OnHealthChanged.Invoke(_health.Value);
        if (_health.Value <= 0)
        {
            OnDeath.Invoke();
        }
    }
}
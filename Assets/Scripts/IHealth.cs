using UnityEngine;
using UnityEngine.Events;

public struct DamageData
{
    public GameObject Source;
    public float Damage;
}

public class IHealth : MonoBehaviour
{
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnDeath;

    public void SetHealth(float health)
    {
        Health = Mathf.Clamp(health, 0, MaxHealth);
    }
    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
    }
    public void TakeDamage(DamageData data)
    {
        Health -= data.Damage;
        OnHealthChanged.Invoke(Health);
        if (Health <= 0)
        {
            OnDeath.Invoke(Health);
        }
    }
}

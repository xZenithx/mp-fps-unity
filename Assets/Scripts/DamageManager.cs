using UnityEngine;

public class DamageManager : MonoBehaviour
{
    public DamageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void DealDamage(GameObject target, GameObject source, float damage)
    {
        if (target.TryGetComponent<PlayerHealth>(out var healthManager))
        {
            healthManager.TakeDamageServer(new DamageData()
            {
                Source = source,
                Damage = damage
            });
        }
    }
}

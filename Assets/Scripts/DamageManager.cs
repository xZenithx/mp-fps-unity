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
        if (target.TryGetComponent<IHealth>(out var healthManager))
        {
            healthManager.TakeDamage(new DamageData()
            {
                Source = source,
                Damage = damage
            });
        }
    }
}

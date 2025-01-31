using UnityEngine;

public class WeaponManager : MonoBehaviour
{
	public static WeaponManager Instance;

    public GameObject[] WeaponPrefabs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public GameObject GetRandomWeapon()
    {
        return WeaponPrefabs[Random.Range(0, WeaponPrefabs.Length)];
    }
}

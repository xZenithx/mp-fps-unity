using System.Linq;
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

        // Loop all prefabs and make sure they have gundata
        foreach (GameObject weapon in WeaponPrefabs)
        {
            if (weapon.GetComponent<GunDataReference>() == null)
            {
                Debug.LogError("Weapon prefab " + weapon.name + " does not have a GunDataReference component");
            }
        }
    }

    public GameObject GetRandomWeaponPrefab()
    {
        return WeaponPrefabs[Random.Range(0, WeaponPrefabs.Length)];
    }

    public GunData GetRandomWeapon()
    {
        return WeaponPrefabs[Random.Range(0, WeaponPrefabs.Length)]
            .GetComponent<GunDataReference>().gunData;
    }

    public GunData GetWeaponDataById(string weaponId)
    {
        return WeaponPrefabs.First(w => w.GetComponent<GunDataReference>().gunData.weaponId == weaponId)
            .GetComponent<GunDataReference>().gunData;
    }

    public GameObject GetWeaponPrefabById(string weaponId)
    {
        return WeaponPrefabs.First(w => w.GetComponent<GunDataReference>().gunData.weaponId == weaponId);
    }
}

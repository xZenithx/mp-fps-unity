using Unity.Netcode;
using UnityEngine;

public struct PlayerWeaponInput 
{
    public bool fire;
    public bool reload;
    public int switchWeapon;
}

public class PlayerWeapon : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float weaponSwitchTime = 0.5f;
    [SerializeField] private float weaponSwitchDelay = 0.5f;

    [Header("References")]
    [SerializeField] private GameObject weaponHolder;


    public void Initialize()
    {

    }

    public void UpdateWeapon(PlayerWeaponInput input)
    {
        
    }
}

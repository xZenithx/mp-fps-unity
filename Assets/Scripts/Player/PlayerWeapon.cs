using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerWeaponInput 
{
    public bool Fire;
    public bool Reload;
    public string SwitchWeapon;
    public Camera Camera;
}

public class PlayerWeapon : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float weaponSwitchTime = 0.5f;
    [SerializeField] private float weaponSwitchDelay = 0.5f;
    [SerializeField] private LayerMask weaponLayerMask;

    [Header("References")]
    [SerializeField] private GameObject weaponHolder;

    [HideInInspector] public GunData CurrentWeapon;
    private Vector3 _eyeStartPosition;
    private Vector3 _eyeAngles;
    private float _lastTimeFired;
    private int _currentMagazine;
    private int _magazineSize;
    private int _weaponLayer = 0;

    public NetworkVariable<FixedString64Bytes> weaponId = new
    (
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );


    [SerializeField] private DynamicAudioSource _dynamicAudioSource;

    public void Initialize()
    {
        int layer = weaponLayerMask.value;
        while(layer > 0)
        {
            layer >>= 1;
            _weaponLayer++;
        }

    }

    public void UpdateWeapon(PlayerWeaponInput input)
    {
        // Debug.Log($"UpdateWeapon {input.Fire} {input.Reload} {input.SwitchWeapon} {input.EyePosition} {input.EyeAngles}");
        _eyeStartPosition = input.Camera.transform.position;
        _eyeAngles = input.Camera.transform.forward;

        if (input.Fire)
        {
            // Fire the weapon
            FireWeapon();
        }

        if (input.Reload)
        {
            // Reload the weapon
            ReloadWeapon();
        }

        if (!string.IsNullOrEmpty(input.SwitchWeapon))
        {
            // Switch the weapon
            SwitchWeapon(input.SwitchWeapon);
        }
    }


    /*
    * FireWeapon
    */

    private void FireWeapon()
    {
        if (!IsOwner || CurrentWeapon == null) return;
        
        FireWeaponServerRpc(NetworkManager.Singleton.LocalClientId, _eyeStartPosition, _eyeAngles);
    }

    private bool CanFire()
    {
        Debug.Log($"CanFire {_currentMagazine} {_lastTimeFired} {Time.time} {60 / CurrentWeapon.fireRate}");
        if (CurrentWeapon == null) return false;
        Debug.Log($"CanFire CurrentWeapon != null");
        if (_currentMagazine <= 0) return false;
        Debug.Log($"CanFire _currentMagazine not empty");

        // fireRate is in shots per minute, so we need to convert it to seconds
        return Time.time - _lastTimeFired > (60 / CurrentWeapon.fireRate);
    }

    [Rpc(SendTo.Server)]
    private void FireWeaponServerRpc(ulong clientId, Vector3 eyeStartPosition, Vector3 eyeAngles)
    {
        Debug.Log($"FireWeaponServerRpc {clientId} {eyeStartPosition} {eyeAngles}");
        if (!IsServer) return;
        Debug.Log($"FireWeaponServerRpc IsServer {clientId}");

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        Debug.Log($"FireWeaponServerRpc client exists {clientId}");

        _lastTimeFired = Time.time;
        if (!CanFire()) return;
        Debug.Log($"FireWeaponServerRpc CanFire {clientId}");

        _currentMagazine--;
        FireSoundClientRpc();

        Ray ray = new(eyeStartPosition, eyeAngles);

        if (Physics.Raycast(ray, out RaycastHit hit, CurrentWeapon.maxDistance, weaponLayerMask))
        {
            Debug.Log(hit.transform.name);

            PlayerHealth playerHealth = hit.transform.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamageServer(new DamageData
                {
                    Damage = CurrentWeapon.damage,
                    Source = client.PlayerObject.GetComponent<NetworkObject>()
                });
            }
        }
        Debug.DrawLine(ray.origin, hit.collider ? hit.point : ray.direction * CurrentWeapon.maxDistance, Color.green, 1f);
    }

    [Rpc(SendTo.Everyone)]
    private void FireSoundClientRpc()
    {
        if (!IsClient) return;

        Debug.Log($"{OwnerClientId}: FireSoundClientRpc");
        _dynamicAudioSource.PlaySound(CurrentWeapon.shotSound, CurrentWeapon.shotSoundVolume, CurrentWeapon.shotSoundPitchMin, CurrentWeapon.shotSoundPitchMax);
    }

    /*
    * ReloadWeapon
    */

    private void ReloadWeapon()
    {
        Debug.Log($"Reloading weapon! {IsClient} {IsOwner}");
        ReloadWeaponServerRpc(NetworkManager.Singleton.LocalClientId);

    }

    [Rpc(SendTo.Server)]
    private void ReloadWeaponServerRpc(ulong clientId)
    {
        Debug.Log($"ReloadWeaponServerRpc {clientId}");
        if (!IsServer) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        if (_currentMagazine == _magazineSize) return;

        _currentMagazine = _magazineSize;
        ReloadSoundClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ReloadSoundClientRpc()
    {
        if (!IsClient) return;

        Debug.Log($"{OwnerClientId}: ReloadSoundClientRpc");
        _dynamicAudioSource.PlaySound(CurrentWeapon.reloadSound, CurrentWeapon.reloadSoundVolume, CurrentWeapon.reloadSoundPitchMin, CurrentWeapon.reloadSoundPitchMax);
    }



    /*
    * SwitchWeapon

    Switching weapon spawns the prefab individually on all clients rather than on the server.
    Because of the fact that weaponHolder is not a network object, we cannot parent a network object to it.

    Client sends a weapon change request to server and server executes it.
    */

    public void SwitchWeapon(string weaponId)
    {
        Debug.Log($"{OwnerClientId}: SwitchWeapon {weaponId}");
        SwitchWeaponServerRpc(weaponId);
    }

    [Rpc(SendTo.Server)]
    public void SwitchWeaponServerRpc(string weaponId)
    {
        Debug.Log($"{OwnerClientId}: SwitchWeaponServerRpc {weaponId}");
        if (!IsServer) return;

        CurrentWeapon = WeaponManager.Instance.GetWeaponDataById(weaponId);
        _currentMagazine = CurrentWeapon.currentAmmo;
        _magazineSize = CurrentWeapon.magSize;

        this.weaponId.Value = weaponId;
        
        SwitchWeaponClientRpc(weaponId);
    }

    [Rpc(SendTo.Everyone)]
    public void SwitchWeaponClientRpc(string weaponId)
    {
        Debug.Log($"{OwnerClientId}: SwitchWeaponClientRpc {weaponId}");
        if (!IsClient) return;

        CurrentWeapon = WeaponManager.Instance.GetWeaponDataById(weaponId);
        _currentMagazine = CurrentWeapon.currentAmmo;
        _magazineSize = CurrentWeapon.magSize;

        if (weaponHolder.transform.childCount > 0)
        {
            Destroy(weaponHolder.transform.GetChild(0).gameObject);
        }

        GameObject weaponPrefab = Instantiate(WeaponManager.Instance.GetWeaponPrefabById(weaponId), weaponHolder.transform);
        if (!IsOwner)
        {
            weaponPrefab.transform.SetLocalPositionAndRotation(new Vector3(0.31f, -0.25f, -0.12f), Quaternion.identity);
        }
    }
    
}
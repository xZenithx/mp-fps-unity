using System.Threading.Tasks;
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

public struct ShotResult : INetworkSerializable
{
    public bool Hit;
    public Vector3 Origin;
    public Vector3 HitPoint;
    public Vector3 HitNormal;
    public ulong HitObjectNetworkId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Hit);
        serializer.SerializeValue(ref Origin);
        serializer.SerializeValue(ref HitPoint);
        serializer.SerializeValue(ref HitNormal);
        serializer.SerializeValue(ref HitObjectNetworkId);
    }
}

public class PlayerWeapon : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float weaponSwitchTime = 0.5f;
    [SerializeField] private float weaponSwitchDelay = 0.5f;
    [SerializeField] private LayerMask weaponLayerMask;

    [Header("References")]
    [SerializeField] private GameObject weaponHolder;

    public GunData CurrentWeapon;
    private Vector3 _eyeStartPosition;
    private Vector3 _eyeAngles;
    private float _lastTimeFired;
    private int _currentMagazine;
    private int _magazineSize;
    private bool _reloading = false;
    private GameObject _weaponMesh;
    private GameObject _weaponMeshParent;
    private GameObject _weaponMuzzle;

    public NetworkVariable<FixedString64Bytes> weaponId = new
    (
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );


    [SerializeField] private DynamicAudioSource _dynamicAudioSource;

    public void Initialize()
    {

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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (string.IsNullOrEmpty(weaponId.Value.ToString())) return;
        Debug.Log($"Giving weapon to {clientId} {weaponId.Value}");

        // Send the new client the current weapon
        SwitchWeaponServerRpc(weaponId.Value, clientId);
    }

    /*
    * FireWeapon
    */

    private void FireWeapon()
    {
        if (!IsOwner || !CanFire()) return;
        
        FireWeaponServerRpc(NetworkManager.Singleton.LocalClientId, _eyeStartPosition, _eyeAngles);
    }

    private bool CanFire()
    {
        // Debug.Log($"CanFire {_currentMagazine} {_lastTimeFired} {Time.time} {60 / CurrentWeapon.fireRate}");
        if (CurrentWeapon == null) 
        {
            return false;
        }

        if (_reloading) 
        {
            return false;
        }

        if (Time.time - _lastTimeFired < 60 / CurrentWeapon.fireRate) 
        {
            return false;
        }
        
        if (_currentMagazine <= 0) {
            _lastTimeFired = Time.time;
            EmptySoundClientRpc();
            return false;
        };

        // fireRate is in shots per minute, so we need to convert it to seconds
        return true;
    }

    [Rpc(SendTo.Server)]
    private void FireWeaponServerRpc(ulong clientId, Vector3 eyeStartPosition, Vector3 eyeAngles)
    {
        Debug.Log($"FireWeaponServerRpc {clientId} {eyeStartPosition} {eyeAngles}");
        if (!IsServer) return;
        Debug.Log($"FireWeaponServerRpc IsServer {clientId}");

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        Debug.Log($"FireWeaponServerRpc client exists {clientId}");

        if (!CanFire()) {
            return;
        }
        _lastTimeFired = Time.time;
        Debug.Log($"FireWeaponServerRpc CanFire {clientId}");

        _currentMagazine--;

        Vector3 direction = eyeAngles;

        // Apply spread
        direction += new Vector3(
            Random.Range(-CurrentWeapon.spread.x, CurrentWeapon.spread.x), 
            Random.Range(-CurrentWeapon.spread.y, CurrentWeapon.spread.y),
            0
        );

        direction.Normalize();

        Ray ray = new(eyeStartPosition, direction);

        bool hitBool = Physics.Raycast(ray, out RaycastHit hit, CurrentWeapon.maxDistance, weaponLayerMask);
        if (hitBool)
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

        ShotResult result = new()
        {
            Hit = hitBool,
            Origin = eyeStartPosition,
            HitPoint = hit.collider != null ? hit.point : direction * CurrentWeapon.maxDistance,
            HitNormal = hit.collider != null ? hit.normal : Vector3.zero,
            HitObjectNetworkId = hit.collider != null && hit.collider.TryGetComponent<NetworkObject>(out var netObj) 
                ? netObj.NetworkObjectId 
                : 0
        };

        OnFireClientRpc(result);
    }

    [Rpc(SendTo.Everyone)]
    private void OnFireClientRpc(ShotResult result)
    {
        if (!IsClient) return;

        _currentMagazine--;

        Debug.Log($"OnFireClientRpc {result.Hit} {result.Origin} {result.HitPoint} {result.HitNormal} {result.HitObjectNetworkId}");
        
        // Draw the line renderer
        LineRendererPoolManager.Instance.RenderLine(_weaponMuzzle.transform.position, result.HitPoint, Color.red, 0.01f, 0.05f);

        // Apply impact effect
        if (result.Hit)
        {
            // GameObject impactEffect = Instantiate(WeaponManager.Instance.GetImpactEffectById(CurrentWeapon.impactEffect), result.HitPoint, Quaternion.LookRotation(result.HitNormal));
            // Destroy(impactEffect, 1f);
        }

        // Apply camera recoil
        if (IsOwner)
        {
            _eyeAngles += new Vector3(
                Random.Range(0, CurrentWeapon.recoil.x), 
                Random.Range(0, CurrentWeapon.recoil.y),
                0
            );
        }
        
        _dynamicAudioSource.PlaySound(CurrentWeapon.shotSound, CurrentWeapon.shotSoundVolume, CurrentWeapon.shotSoundPitchMin, CurrentWeapon.shotSoundPitchMax);
    }

    [Rpc(SendTo.Everyone)]
    private void EmptySoundClientRpc()
    {
        if (!IsClient) return;

        Debug.Log($"{OwnerClientId}: EmptySoundClientRpc");
        _dynamicAudioSource.PlaySound(CurrentWeapon.emptySound, CurrentWeapon.emptySoundVolume, CurrentWeapon.emptySoundPitchMin, CurrentWeapon.emptySoundPitchMax);
    }

    /*
    * ReloadWeapon
    */

    private void ReloadWeapon()
    {
        if (!IsOwner || _reloading || _currentMagazine == _magazineSize) return;
        Debug.Log($"Reloading weapon! {IsClient} {IsOwner}");
        ReloadWeaponServerRpc(NetworkManager.Singleton.LocalClientId);

    }

    [Rpc(SendTo.Server)]
    private void ReloadWeaponServerRpc(ulong clientId)
    {
        Debug.Log($"ReloadWeaponServerRpc {clientId}");
        if (!IsServer || _reloading) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        if (_currentMagazine == _magazineSize) return;

        _currentMagazine = _magazineSize;
        OnReloadServer();
        OnReloadClientRpc();
    }

    private async void OnReloadServer()
    {
        if (!IsServer) return;
        _reloading = true;

        await Task.Delay((int)CurrentWeapon.reloadTime * 1000);

        _reloading = false;
    }

    [Rpc(SendTo.Everyone)]
    private void OnReloadClientRpc()
    {
        if (!IsClient) return;

        Debug.Log($"{OwnerClientId}: ReloadSoundClientRpc");
        _dynamicAudioSource.PlaySound(CurrentWeapon.reloadSound, CurrentWeapon.reloadSoundVolume, CurrentWeapon.reloadSoundPitchMin, CurrentWeapon.reloadSoundPitchMax);

        _currentMagazine = _magazineSize;

        ReloadAsync();
    }
    
    private async void ReloadAsync()
    {
        _reloading = true;

        float reloadTime = CurrentWeapon.reloadTime;
        float elapsedTime = 0f;

        _weaponMesh.transform.GetLocalPositionAndRotation(out Vector3 originalPosition, out Quaternion originalRotation);
        while (elapsedTime < reloadTime)
        {
            float curveValue = CurrentWeapon.reloadCurve.Evaluate(elapsedTime / reloadTime);
            _weaponMesh.transform.localRotation = Quaternion.Euler(0, 360 * curveValue, 0);
            elapsedTime += Time.deltaTime;
            await Task.Yield();
        }
        _weaponMesh.transform.SetLocalPositionAndRotation(originalPosition, originalRotation);
        _reloading = false;
        // _mag.Value = gunData.magSize;
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
    public void SwitchWeaponServerRpc(FixedString64Bytes weaponId, ulong clientId)
    {
        if (!IsServer || string.IsNullOrEmpty(weaponId.ToString())) return;
        string _weaponId = weaponId.ToString();
        Debug.Log($"{OwnerClientId}: SwitchWeaponServerRpc {_weaponId}");

        CurrentWeapon = WeaponManager.Instance.GetWeaponDataById(_weaponId);
        _magazineSize = CurrentWeapon.magSize;
        _currentMagazine = _magazineSize;

        this.weaponId.Value = weaponId;
        SwitchWeaponRpc(weaponId, _magazineSize, _currentMagazine, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server)]
    public void SwitchWeaponServerRpc(FixedString64Bytes weaponId)
    {
        if (!IsServer) return;
        string _weaponId = weaponId.ToString();
        Debug.Log($"{OwnerClientId}: SwitchWeaponServerRpc {_weaponId}");

        CurrentWeapon = WeaponManager.Instance.GetWeaponDataById(_weaponId);
        _magazineSize = CurrentWeapon.magSize;
        _currentMagazine = _magazineSize;

        this.weaponId.Value = weaponId;
        
        SwitchWeaponRpc(_weaponId, _magazineSize, _currentMagazine, RpcTarget.Everyone);
    }
    
    [Rpc(SendTo.SpecifiedInParams)]
    public void SwitchWeaponRpc(FixedString64Bytes weaponId, int magazineSize, int currentMagazine, RpcParams rpcParams = default)
    {
        if (!IsClient) return;
        string _weaponId = weaponId.ToString();
        Debug.Log($"{OwnerClientId}: SwitchWeaponClientRpc {_weaponId}");

        CurrentWeapon = WeaponManager.Instance.GetWeaponDataById(_weaponId);
        _magazineSize = magazineSize;
        _currentMagazine = currentMagazine;

        if (weaponHolder.transform.childCount > 0)
        {
            Destroy(weaponHolder.transform.GetChild(0).gameObject);
        }

        GameObject weaponPrefab = Instantiate(WeaponManager.Instance.GetWeaponPrefabById(_weaponId), weaponHolder.transform);

        GunDataReference gunDataReference = weaponPrefab.GetComponent<GunDataReference>();
        _weaponMesh = gunDataReference.weaponMesh;
        _weaponMeshParent = gunDataReference.weaponMeshParent;
        _weaponMuzzle = gunDataReference.weaponMuzzle;

        if (!IsOwner)
        {
            weaponPrefab.transform.SetLocalPositionAndRotation(new Vector3(0.31f, -0.25f, -0.12f), Quaternion.identity);
            gunDataReference.weaponMeshParent.transform.localScale = new Vector3(100f, 100f, 100f);
        }
        else
        {
            gunDataReference.weaponMeshParent.transform.localScale = new Vector3(50f, 50f, 50f);

            // Set all the models to the layer with the name "Weapon" for rendering purposes
            RecursiveLayerChange(gunDataReference.weaponMeshParent.transform, LayerMask.NameToLayer("Weapon"));
        }
    }

    private void RecursiveLayerChange(Transform transform, int layer)
    {
        transform.gameObject.layer = layer;
        foreach (Transform child in transform)
        {
            RecursiveLayerChange(child, layer);
        }
    }
}
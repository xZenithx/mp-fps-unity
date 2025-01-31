using Unity.Netcode;
using UnityEngine;

public struct PlayerWeaponInput 
{
    public bool Fire;
    public bool Reload;
    public int SwitchWeapon;
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

    private DynamicAudioSource _dynamicAudioSource;

    public void Initialize()
    {
        _dynamicAudioSource = gameObject.AddComponent<DynamicAudioSource>();
    }

    private void SpawnWeapon()
    {
        GameObject weaponPrefab = WeaponManager.Instance.GetRandomWeapon();
        GameObject weapon = Instantiate(weaponPrefab, weaponHolder.transform);

        weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        // Set the weapon layer to the weapon layer
        weapon.layer = weaponLayerMask;

        // Set the weapon to the current weapon
        
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
    }

    private void FireWeapon()
    {
        Debug.Log($"Firing weapon! {IsClient} {IsOwner}");
        FireWeaponServerRpc(NetworkManager.Singleton.LocalClientId, _eyeStartPosition, _eyeAngles);
    }

    private bool CanFire()
    {
        if (CurrentWeapon == null) return false;
        if (_currentMagazine <= 0) return false;

        return Time.time - _lastTimeFired > CurrentWeapon.fireRate;
    }

    [ServerRpc]
    private void FireWeaponServerRpc(ulong clientId, Vector3 eyeStartPosition, Vector3 eyeAngles)
    {
        Debug.Log($"FireWeaponServerRpc {clientId} {eyeStartPosition} {eyeAngles}");
        if (!IsServer) return;
        Debug.Log("Server> FireWeaponServerRpc");

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        Debug.Log("Server> FireWeaponServerRpc 2");

        _lastTimeFired = Time.time;

        if (!CanFire()) return;

        FireWeaponSoundServerRpc();

        Ray ray = new(eyeStartPosition, eyeAngles);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, weaponLayerMask))
        {
            Debug.Log(hit.transform.name);

            PlayerHealth playerHealth = hit.transform.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamageServer(new DamageData
                {
                    Damage = 10f,
                    // NetworkObjectReference
                    Source = client.PlayerObject.GetComponent<NetworkObject>()
                });
            }
        }
        Debug.DrawLine(ray.origin, hit.collider ? hit.point : ray.direction * 100f, Color.green, 1f);
    }

    [ServerRpc]
    public void FireWeaponSoundServerRpc()
    {
        AudioClip audioClip = CurrentWeapon.shotSound;
        int volume = CurrentWeapon.shotSoundVolume;
        int pitchMin = CurrentWeapon.shotSoundPitchMin;
        int pitchMax = CurrentWeapon.shotSoundPitchMax;

        FireWeaponSoundClientRpc(audioClip, volume, Random.Range(pitchMin, pitchMax));
    }

    [ClientRpc]
    public void FireWeaponSoundClientRpc(AudioClip audioClip, int volume, int pitch)
    {
        _dynamicAudioSource.PlaySound(audioClip, volume, pitch);
    }

    [ServerRpc]
    public void ReloadWeaponServerRpc()
    {

    }
}

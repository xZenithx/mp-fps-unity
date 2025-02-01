using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
	[SerializeField] private GunData gunData;
    [SerializeField] private Transform gunMuzzle;
    [SerializeField] private Transform weaponMesh;
    [SerializeField] private AnimationCurve reloadCurve;

    private MultiAudioSource multiAudioSource;
    private float lastTimeShot;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }
        // Create a new instance of the gun data to avoid modifying the original data
        gunData = Instantiate(gunData);

        gunData.reloading = false;
        // gunData.currentAmmo.Value = gunData.magSize;

        multiAudioSource = GetComponent<MultiAudioSource>();

        if (multiAudioSource == null)
        {
            multiAudioSource = gameObject.AddComponent<MultiAudioSource>();
        }

        multiAudioSource.ParseGunData(gunData);
        multiAudioSource.CreateAudioSources();
    }

    private bool CanShoot()
    {
        if (gunData.reloading) return false;
        if (Time.time - lastTimeShot < 1f / (gunData.fireRate / 60)) return false;

        return true;
    }

    [ServerRpc]
    public void ShootServerRpc()
    {
        if (!CanShoot()) return;
        lastTimeShot = Time.time;

        // if (gunData.currentAmmo.Value <= 0)
        // {
        //     EmptySoundServerRpc();
        //     return;
        // }

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, gunData.maxDistance))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 3f);
        }

        // gunData.currentAmmo.Value--;
        ShootSoundServerRpc();
    }

    private void OnGunShot()
    {
        ShootSoundServerRpc();
    }

    [ServerRpc]
    public void StartReloadServerRpc()
    {
        if (!IsServer) return;
        if (gunData.reloading) return;
        // if (gunData.currentAmmo.Value == gunData.magSize) return;

        ReloadSoundServerRpc();
        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        gunData.reloading = true;

        float reloadTime = gunData.reloadTime;
        float elapsedTime = 0f;

        weaponMesh.GetLocalPositionAndRotation(out Vector3 originalPosition, out Quaternion originalRotation);
        while (elapsedTime < reloadTime)
        {
            float curveValue = reloadCurve.Evaluate(elapsedTime / reloadTime);
            weaponMesh.localRotation = Quaternion.Euler(0, 360 * curveValue, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        weaponMesh.SetLocalPositionAndRotation(originalPosition, originalRotation);
        gunData.reloading = false;
        // _mag.Value = gunData.magSize;
    }

    [Rpc(SendTo.Server)]
    private void ShootSoundServerRpc()
    {
        if (gunData.shotSound == null) return;

        multiAudioSource.PlaySound(gunData.shotSound, gunData.shotSoundVolume, gunData.shotSoundPitchMin, gunData.shotSoundPitchMax);
    }

    [Rpc(SendTo.Server)]
    private void ReloadSoundServerRpc()
    {
        if (gunData.reloadSound == null) return;

        multiAudioSource.PlaySound(gunData.reloadSound, gunData.reloadSoundVolume, gunData.reloadSoundPitchMin, gunData.reloadSoundPitchMax);
    }

    [Rpc(SendTo.Server)]
    public void EmptySoundServerRpc()
    {
        if (gunData.emptySound == null) return;

        multiAudioSource.PlaySound(gunData.emptySound, gunData.emptySoundVolume, gunData.emptySoundPitchMin, gunData.emptySoundPitchMax);
    }

    public GunData GetGunData() => gunData;
}

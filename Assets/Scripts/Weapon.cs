using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
	[SerializeField] private GunData gunData;
    [SerializeField] private Transform gunMuzzle;
    [SerializeField] private Transform weaponMesh;
    [SerializeField] private AnimationCurve reloadCurve;

    private MultiAudioSource multiAudioSource;
    private float lastTimeShot;

    private void Awake()
    {
        gunData.reloading = false;
        gunData.currentAmmo = gunData.magSize;

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

    public void Shoot()
    {
        if (!CanShoot()) return;
        lastTimeShot = Time.time;

        if (gunData.currentAmmo <= 0)
        {
            EmptySound();
            return;
        }

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, gunData.maxDistance))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 3f);
        }

        gunData.currentAmmo--;
        OnGunShot();
    }

    private void OnGunShot()
    {
        ShootSound();
    }

    public void StartReload()
    {
        if (gunData.reloading) return;
        if (gunData.currentAmmo == gunData.magSize) return;

        ReloadSound();
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
        gunData.currentAmmo = gunData.magSize;
    }

    private void ShootSound()
    {
        if (gunData.shotSound == null) return;

        multiAudioSource.PlaySound(gunData.shotSound, gunData.shotSoundVolume, gunData.shotSoundPitchMin, gunData.shotSoundPitchMax);
    }

    private void ReloadSound()
    {
        if (gunData.reloadSound == null) return;

        multiAudioSource.PlaySound(gunData.reloadSound, gunData.reloadSoundVolume, gunData.reloadSoundPitchMin, gunData.reloadSoundPitchMax);
    }

    public void EmptySound()
    {
        if (gunData.emptySound == null) return;

        multiAudioSource.PlaySound(gunData.emptySound, gunData.emptySoundVolume, gunData.emptySoundPitchMin, gunData.emptySoundPitchMax);
    }

    public GunData GetGunData() => gunData;
}

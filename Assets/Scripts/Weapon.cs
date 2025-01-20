using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
	[SerializeField] private GunData gunData;
    [SerializeField] private Transform gunMuzzle;

    private float lastTimeShot;

    private bool CanShoot()
    {
        if (gunData.reloading) return false;
        if (gunData.currentAmmo <= 0) return false;
        if (Time.time - lastTimeShot < 1f / (gunData.fireRate / 60)) return false;


        return true;
    }

    public void Shoot()
    {
        if (!CanShoot()) return;
        
        if (Physics.Raycast(gunMuzzle.position, gunMuzzle.forward, out RaycastHit hit, gunData.maxDistance))
        {
            Debug.DrawRay(gunMuzzle.position, gunMuzzle.forward * hit.distance, Color.red, 3f);

        }

        gunData.currentAmmo--;
        lastTimeShot = Time.time;
        OnGunShot();
    }

    private void OnGunShot()
    {
        
    }

    public void StartReload()
    {
        if (gunData.reloading) return;
        if (gunData.currentAmmo == gunData.magSize) return;

        StartCoroutine(Reload());
    }
    private IEnumerator Reload()
    {
        gunData.reloading = true;

        float elapsedTime = 0f;
        Quaternion initialRotation = transform.rotation;
        Quaternion finalRotation = initialRotation * Quaternion.Euler(0, 360, 0);

        while (elapsedTime < gunData.reloadTime)
        {
            transform.rotation = Quaternion.Slerp(initialRotation, finalRotation, elapsedTime / gunData.reloadTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = finalRotation;
        gunData.reloading = false;
        gunData.currentAmmo = gunData.magSize;
    }

}

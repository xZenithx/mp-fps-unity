using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Weapon/Gun")]
public class GunData : ScriptableObject
{
    [Header("Info")]
	public new string name;

    [Header("Shooting")]
    public float damage;
    public float maxDistance;

    [Header("Ammo")]
    public int currentAmmo;
    public int magSize;
    public int maxAmmo = 9999;

    [Header("Reload")]
    public float fireRate;
    public float reloadTime;
    [HideInInspector]
    public bool reloading;

    [Header("Audio")]
    public AudioClip shotSound;
    public int shotSoundPitchMin = 95;
    public int shotSoundPitchMax = 100;
    public AudioClip reloadSound;
    public int reloadSoundPitchMin = 95;
    public int reloadSoundPitchMax = 100;
    public AudioClip emptySound;
    public int emptySoundPitchMin = 95;
    public int emptySoundPitchMax = 100;
}

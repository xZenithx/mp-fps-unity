using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Weapon/Gun")]
public class GunData : ScriptableObject
{
    [Header("Info")]
	public new string name;
    public string weaponId;

    [Header("Shooting")]
    public float damage;
    public float maxDistance;
    public Vector3 spread = Vector3.zero;
    public Vector3 recoil = Vector3.zero;

    [Header("Ammo")]
    public int magSize;
    public int maxAmmo = 9999;

    [Header("Reload")]
    public float fireRate;
    public float reloadTime;
    public AnimationCurve reloadCurve;

    [Header("Audio")]
    public AudioClip shotSound;
    public int shotSoundVolume = 100;
    public int shotSoundPitchMin = 95;
    public int shotSoundPitchMax = 100;
    [Space]
    public AudioClip reloadSound;
    public int reloadSoundVolume = 100;
    public int reloadSoundPitchMin = 95;
    public int reloadSoundPitchMax = 100;
    [Space]
    public AudioClip emptySound;
    public int emptySoundVolume = 100;
    public int emptySoundPitchMin = 95;
    public int emptySoundPitchMax = 100;
}

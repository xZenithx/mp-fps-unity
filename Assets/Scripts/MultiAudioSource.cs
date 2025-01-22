using System.Collections;
using UnityEngine;

public class MultiAudioSource : MonoBehaviour
{
    private int _neededSources;
    
    public AudioSource[] audioSource;

    public void AddSource()
    {
        _neededSources++;

        CreateAudioSources();
    }

    public void ParseGunData(GunData gunData)
    {
        float shotsPerSecond = 1f / (gunData.fireRate / 60);
        
        // gunData.shotSound.length is the length of the audio clip in seconds
        // Mathf.CeilToInt rounds up to the nearest integer
        _neededSources = Mathf.CeilToInt(gunData.shotSound.length / shotsPerSecond);
        
        // One for reloading
        _neededSources++;

        // One for empty
        _neededSources++;

        // Create the audio sources
        CreateAudioSources();
    }

    public void CreateAudioSources()
    {
        audioSource = new AudioSource[_neededSources];
        for (int i = 0; i < _neededSources; i++)
        {
            audioSource[i] = gameObject.AddComponent<AudioSource>();
            audioSource[i].playOnAwake = false;
            audioSource[i].spatialBlend = 1;
        }
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in audioSource)
        {
            if (!source.clip)
            {
                return source;
            }
        }

        Debug.LogWarning("No available audio sources! " + gameObject.name);
        return null;
    }

    public void PlaySound(AudioClip clip, int volume, int pitchMin, int pitchMax)
    {
        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = volume / 100f;
        source.pitch = Random.Range(pitchMin / 100f, pitchMax / 100f);
        source.Play();

        StartCoroutine(ClearSound(source));
    }

    private IEnumerator ClearSound(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        source.clip = null;
    }
}

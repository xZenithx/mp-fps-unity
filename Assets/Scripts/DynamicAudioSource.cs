using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class DynamicAudioSource : NetworkBehaviour
{
    private int _neededSources;
    
    public AudioSource[] audioSource;

    public override void OnNetworkSpawn()
    {
        AddSource();
    }

    public void AddSource()
    {
        _neededSources++;

        CreateAudioSources();
    }

    public void CreateAudioSources()
    {
        audioSource = new AudioSource[_neededSources];
        for (int i = 0; i < _neededSources; i++)
        {
            audioSource[i] = gameObject.AddComponent<AudioSource>();
            audioSource[i].playOnAwake = false;
            audioSource[i].spatialBlend = 0.5f;
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

        AddSource();
        return GetAvailableSource();
    }

    public void PlaySound(AudioClip clip, int volume, int pitchMin, int pitchMax)
    {
        PlaySound(clip, volume, Random.Range(pitchMin, pitchMax));
    }
    
    public void PlaySound(AudioClip clip, int volume, int pitch)
    {
        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = volume / 100f;
        source.pitch = pitch / 100f;
        source.Play();

        ClearSound(source);
    }

    private async Task<bool> WaitUntilStopPlay(AudioSource source)
    {
        await Task.Yield();
        while (source.isPlaying)
        {
            await Task.Yield();
        }

        return true;
    }

    private async Task<bool> WaitSoundSeconds(float seconds)
    {
        await Task.Yield();
        await Task.Delay((int) (seconds * 1000));
        return true;
    }

    // private IEnumerator ClearSound(AudioSource source)
    // {
    //     yield return new WaitForSeconds(source.clip.length);
    //     source.clip = null;
    // }

    private async void ClearSound(AudioSource source)
    {
        await WaitUntilStopPlay(source);
        source.clip = null;
    }
}

using UnityEngine;

public class music : MonoBehaviour
{
    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.loop = true;
        source.playOnAwake = false;
    }

    public void StartMusic()
    {
        if (source != null && source.clip != null && !source.isPlaying)
        {
            source.Play();
        }
    }

    public void StopMusic()
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    public void ChangeMusic(AudioClip clip)
    {
        if (source == null)
        {
            return;
        }

        bool wasPlaying = source.isPlaying;
        source.clip = clip;

        if (wasPlaying && clip != null)
        {
            source.Play();
        }
    }

    public void SetVolume(int volume)
    {
        if (source != null)
        {
            source.volume = Mathf.Clamp01(volume / 100f);
        }
    }

    public void SetSpeed(float speed)
    {
        if (source != null)
        {
            source.pitch = Mathf.Clamp(speed, 0.25f, 2f);
        }
    }
}

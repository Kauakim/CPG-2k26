using UnityEngine;

/// Wrapper do AudioSource para música de fundo.
/// Atribua o AudioSource via Inspector — nenhum componente é criado em runtime.
public class HPMusicSystem : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.loop        = true;
            audioSource.playOnAwake = false;
        }
    }

    public void StartMusic()
    {
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public void SetSpeed(float speed)
    {
        if (audioSource != null)
            audioSource.pitch = Mathf.Clamp(speed, 0.25f, 2f);
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = Mathf.Clamp01(volume);
    }

    public void ChangeMusic(AudioClip clip)
    {
        if (audioSource == null) return;
        bool was = audioSource.isPlaying;
        audioSource.clip = clip;
        if (was && clip != null) audioSource.Play();
    }
}

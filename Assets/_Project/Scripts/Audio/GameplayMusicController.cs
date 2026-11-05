using UnityEngine;

public sealed class GameplayMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.28f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.clip = musicClip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    private void Start()
    {
        if (source.clip != null && !source.isPlaying) source.Play();
    }

    public void PauseForOverlay()
    {
        if (source != null) source.mute = true;
    }

    public void ResumeAfterOverlay()
    {
        if (source != null) source.mute = false;
    }
}

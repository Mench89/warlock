using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] public AudioSource musicSource;
    [SerializeField] public AudioClip musicStart;

    void Start()
    {
        musicSource.PlayOneShot(musicStart);
        musicSource.PlayScheduled(AudioSettings.dspTime + musicStart.length);
    }
}

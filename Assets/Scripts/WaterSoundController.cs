using UnityEngine;

public class WaterSoundController : MonoBehaviour
{
    [SerializeField] public AudioClip[] WaterSplashAudio;
    private AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

    }

    void OnCollisionEnter()
    {
        audioSource.PlayOneShot(GetRandomAudioClip(), 0.8f);
    }

    private AudioClip GetRandomAudioClip()
    {
        int randomIndex = Random.Range(0, WaterSplashAudio.Length - 1);
        Debug.Log("Magnus, index: " + randomIndex);
        return WaterSplashAudio[randomIndex];
    }
}

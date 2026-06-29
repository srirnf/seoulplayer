using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // 어디서나 쉽게 접근할 수 있도록 싱글톤(Singleton) 구조 만듦
    public static SoundManager Instance;

    public AudioSource sfxSource;
    public AudioClip playerHitSound;
    // public AudioClip enemyHitSound; // 나중에 몬스터 피격음도 추가 가능

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayPlayerHit()
    {
        sfxSource.PlayOneShot(playerHitSound);
    }
}
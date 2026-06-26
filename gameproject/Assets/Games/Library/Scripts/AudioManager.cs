using UnityEngine;

// 사운드 재생 관리. BGM(루프)과 효과음(클릭/정답/오답)을 담당.
// 걷기 소리는 플레이어에 붙어있으므로 여기서 다루지 않는다.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSource")]
    [SerializeField] private AudioSource bgmSource;   // BGM용 (Loop 체크)
    [SerializeField] private AudioSource sfxSource;   // 효과음용

    [Header("효과음 클립")]
    [SerializeField] private AudioClip shelfClickClip; // 책장/책 클릭
    [SerializeField] private AudioClip correctClip;    // 정답
    [SerializeField] private AudioClip wrongClip;      // 오답

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (bgmSource != null && bgmSource.clip != null)
            bgmSource.Play();
    }

    public void PlayShelfClick() => PlaySfx(shelfClickClip);
    public void PlayCorrect() => PlaySfx(correctClip);
    public void PlayWrong() => PlaySfx(wrongClip);

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
}

using UnityEngine;
using TMPro; // TMP 텍스트를 쓰기 위해 꼭 필요합니다!

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Score System")]
    public int score = 0;
    public TextMeshProUGUI scoreText;

    [Header("Timer System")]
    public float timeRemaining = 90f; // 1분 30초 = 90초
    public TextMeshProUGUI timerText; // 화면에 시간을 보여줄 텍스트 칸
    private bool isGameOver = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        // 게임 오버 상태가 아니라면 시간이 계속 줄어듭니다.
        if (!isGameOver)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateUI();
            }
            else
            {
                // 시간이 0 이하가 되면 게임 종료!
                timeRemaining = 0;
                isGameOver = true;
                GameOver();
            }
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return; // 게임이 끝났으면 점수가 안 오르게 막음
        score += amount;
        UpdateUI();
    }

    public void DeductScore(int amount)
    {
        if (isGameOver) return; // 게임이 끝났으면 점수가 안 깎이게 막음
        score -= amount;
        if (score < 0) score = 0; 
        UpdateUI();
    }

    void UpdateUI()
    {
        // 점수 글자 업데이트
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        // 시간 글자 업데이트 (분:초 형태로 이쁘게 출력)
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("Time: {0:0}:{1:00}", minutes, seconds);
        }
    }

    // 시간이 다 되었을 때 실행되는 함수
    void GameOver()
    {
        if (timerText != null)
        {
            timerText.text = "TIME UP!!";
        }
        
        // 게임 안의 모든 사물(시간)을 일시정지 시킵니다.
        Time.timeScale = 0f; 
        
        Debug.Log("게임 종료! 최종 점수: " + score);
    }
}
using UnityEngine;
using TMPro; // TextMeshPro를 쓰기 위해 필수 추가

public class GameTimer : MonoBehaviour
{
    [Header("UI 설정")]
    public TextMeshProUGUI timerText; // 화면에 보여줄 텍스트 연결

    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    void Start()
    {
        // 게임 시작 시 타이머 가동 (가이드 UI가 닫힐 때 켜고 싶다면 아래 함수를 다른 곳에서 호출)
        //StartTimer();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            // 매 프레임 흐른 시간(초)을 더해줍니다.
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    void UpdateTimerUI()
    {
        // 전체 초를 '분'과 '초'로 나눕니다.
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // 00:00 포맷으로 문자열을 만들어 UI에 대입합니다.
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 빨리 올라갈수록 높은 점수를 주기 위해 현재 경과 시간을 반환하는 함수
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
}

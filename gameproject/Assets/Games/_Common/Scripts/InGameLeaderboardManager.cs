using UnityEngine;
using TMPro;

public class InGameLeaderboardManager : MonoBehaviour
{
    [Header("UI 6000 Text References")]
    [SerializeField] private TextMeshProUGUI topRankText;      // TopRankText 매핑
    [SerializeField] private TextMeshProUGUI currentScoreText;  // CurrentScoreText 매핑

    [Header("Current Landmark Setting")]
    public string currentLandmarkKey = "Banpo_Hanriver"; // 현재 명소 코드명 (예: 반포 한강공원)

    private int currentScore = 0;
    private string topNickname;
    private int topScore;

    private void Start()
    {
        // 1. 이 명소의 기존 1등 데이터 불러오기
        LoadTopRecord();
        
        // 2. UI 갱신
        UpdateScoreUI();
    }

    private void LoadTopRecord()
    {
        // {명소ID}_TopName 형태로 데이터를 저장하고 불러옵니다.
        topNickname = PlayerPrefs.GetString($"{currentLandmarkKey}_TopName", "도전자 없음");
        topScore = PlayerPrefs.GetInt($"{currentLandmarkKey}_TopScore", 0);

        topRankText.text = $"👑 1등: {topNickname} ({topScore:N0}점)";
    }

    // 미니게임에서 점수가 오를 때 이 함수를 호출하세요! (예: 인게임 스크립트에서 호출)
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        currentScoreText.text = $"내 점수: {currentScore:N0}";
    }

    // 미니게임이 완전히 종료되었을 때(게임오버) 호출하는 함수
    public void OnMiniGameResult()
    {
        // 내 점수가 기존 1등 점수보다 높다면 기록 경신!
        if (currentScore > topScore)
        {
            string myNickname = PlayerPrefs.GetString("UserNickname", "익명");

            PlayerPrefs.SetString($"{currentLandmarkKey}_TopName", myNickname);
            PlayerPrefs.SetInt($"{currentLandmarkKey}_TopScore", currentScore);
            PlayerPrefs.Save();

            Debug.Log($"[신기록] {myNickname}님이 {currentLandmarkKey}의 새로운 1등이 되었습니다!");
            
            // UI 실시간 업데이트
            topRankText.text = $"👑 1등: {myNickname} ({currentScore:N0}점)";
        }
    }
}

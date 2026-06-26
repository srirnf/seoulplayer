using TMPro;
using UnityEngine;

// 서울대공원 게임 UI: 잡은 동물 수 / 경과 시간 HUD, 엔딩 전환 오버레이, 클리어 패널.
public class ParkUIManager : MonoBehaviour
{
    public static ParkUIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text caughtText;   // 동물 2 / 5
    [SerializeField] private TMP_Text timerText;    // 12.3초

    [Header("시작 안내")]
    [SerializeField] private GameObject howToPanel;       // 게임방법 안내 화면

    [Header("엔딩")]
    [SerializeField] private GameObject endingTransition; // "우리로 데려가는 중" 오버레이
    [SerializeField] private GameObject clearPanel;       // 클리어 패널
    [SerializeField] private TMP_Text clearTimeText;      // 클리어 시간 표시

    private void Awake()
    {
        Instance = this;
        if (howToPanel) howToPanel.SetActive(true);
        if (endingTransition) endingTransition.SetActive(false);
        if (clearPanel) clearPanel.SetActive(false);
    }

    // 안내 화면 "확인" 버튼: 닫고 게임 시작
    public void CloseHowToAndStart()
    {
        if (howToPanel) howToPanel.SetActive(false);
        ParkGameManager.Instance?.BeginGame();
    }

    public void UpdateCaught(int caught, int total)
    {
        if (caughtText) caughtText.text = $"동물 {caught} / {total}";
    }

    public void UpdateTimer(float seconds)
    {
        if (timerText) timerText.text = seconds.ToString("0.0") + "초";
    }

    public void ShowEndingTransition()
    {
        if (endingTransition) endingTransition.SetActive(true);
    }

    public void ShowClear(float seconds)
    {
        if (endingTransition) endingTransition.SetActive(false);
        if (clearPanel) clearPanel.SetActive(true);
        if (clearTimeText) clearTimeText.text = $"클리어 시간: {seconds:0.0}초";
    }
}

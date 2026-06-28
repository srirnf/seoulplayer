using UnityEngine;

// 게임 시작 시 설명 화면을 띄우고, "게임 시작" 버튼을 누르면 닫고 플레이어 이동/회전을 푼다.
// (서울어린이대공원 HowTo 패널과 같은 흐름)
public class LibraryIntro : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private LibraryExplorer explorer;

    private void Start()
    {
        if (panel) panel.SetActive(true);
        if (explorer) explorer.CanMove = false; // 설명 보는 동안 이동/카메라 회전 잠금
    }

    // "게임 시작" 버튼
    public void StartGame()
    {
        if (explorer) explorer.CanMove = true;
        if (panel) panel.SetActive(false);
    }
}

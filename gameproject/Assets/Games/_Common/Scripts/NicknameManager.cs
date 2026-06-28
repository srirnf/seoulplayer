using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NicknameManager : MonoBehaviour
{
    [Header("UI 6000 Components")]
    [SerializeField] private GameObject nicknamePanel;      // NicknamePopup 오브젝트 매핑
    [SerializeField] private TMP_InputField nicknameInput;  // NicknameInputField 매핑
    [SerializeField] private Button submitButton;           // SubmitButton 매핑

    private void Start()
    {
        // 버튼 클릭 및 엔터키 이벤트 연결
        submitButton.onClick.AddListener(OnSubmitNickname);
        nicknameInput.onSubmit.AddListener(delegate { OnSubmitNickname(); });

        // 기존에 저장된 닉네임이 있는지 검사
        if (PlayerPrefs.HasKey("UserNickname"))
        {
            nicknamePanel.SetActive(false); // 있으면 팝업 끄기
            Debug.Log($"환영합니다! 유저 닉네임: {PlayerPrefs.GetString("UserNickname")}");
        }
        else
        {
            nicknamePanel.SetActive(true);  // 없으면 팝업 켜고 마우스 포커스 줌
            nicknameInput.ActivateInputField();
        }
    }

    private void OnSubmitNickname()
    {
        string input = nicknameInput.text.Trim();

        // 예외 처리 (2글자 미만 불가)
        if (string.IsNullOrEmpty(input) || input.Length < 2)
        {
            Debug.LogWarning("닉네임은 최소 2글자 이상이어야 합니다.");
            return;
        }

        // 로컬 데이터에 저장
        PlayerPrefs.SetString("UserNickname", input);
        PlayerPrefs.Save();

        // 팝업 닫기
        nicknamePanel.SetActive(false);
        Debug.Log($"닉네임 저장 완료: {input}");
    }
}
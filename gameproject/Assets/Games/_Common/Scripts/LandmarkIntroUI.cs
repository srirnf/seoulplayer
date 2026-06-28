using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LandmarkIntroUI : MonoBehaviour
{
    public static LandmarkIntroUI Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button cancelButton;

    private string pendingSceneName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        popupPanel.SetActive(false); 
    }

    // 사진(Sprite)을 받던 매개변수를 완전히 없앴습니다.
    public void ShowIntro(string title, string description, string sceneName)
    {
        titleText.text = title;
        descText.text = description;
        pendingSceneName = sceneName;

        enterButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        enterButton.onClick.AddListener(OnEnterGame);
        cancelButton.onClick.AddListener(OnCancel);

        popupPanel.SetActive(true);
    }

    private void OnEnterGame()
    {
        popupPanel.SetActive(false);
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneByName(pendingSceneName);
        }
    }

    private void OnCancel()
    {
        popupPanel.SetActive(false);
    }
}
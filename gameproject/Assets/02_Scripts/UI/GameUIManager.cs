using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게임 화면 UI(HUD): 타이머, 찾은 책 수, 목표 안내, 상호작용 프롬프트,
// 정답/오답 팝업, 게임 종료 패널.
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text timerText;       // 시간 UI
    [SerializeField] private TMP_Text foundText;       // 현재 찾은 책의 수 (예: 2 / 5)

    [Header("목표 안내 UI")]
    [SerializeField] private Image targetThumbnail;    // 목표 책 책등(있으면)
    [SerializeField] private TMP_Text targetFeatureText; // 목표 책의 특징

    [Header("상호작용 프롬프트")]
    [SerializeField] private GameObject interactPrompt; // "F: 책장 열기" 안내

    [Header("결과 팝업")]
    [SerializeField] private GameObject correctPopup;  // "이 책이 맞습니다"
    [SerializeField] private GameObject wrongPopup;    // "이 책이 아닙니다"
    [SerializeField] private float popupDuration = 1f;

    [Header("게임 종료 패널")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private void Awake()
    {
        Instance = this;
        SafeSetActive(interactPrompt, false);
        SafeSetActive(correctPopup, false);
        SafeSetActive(wrongPopup, false);
        SafeSetActive(winPanel, false);
        SafeSetActive(losePanel, false);
    }

    public void UpdateTimer(float seconds)
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(seconds).ToString();
    }

    public void UpdateFound(int found, int total)
    {
        if (foundText != null)
            foundText.text = found + " / " + total;
    }

    public void ShowTargetHint(BookData book)
    {
        if (book == null) return;
        if (targetThumbnail != null)
        {
            targetThumbnail.sprite = book.spineSprite;
            targetThumbnail.enabled = book.spineSprite != null;
        }
        if (targetFeatureText != null)
            targetFeatureText.text = book.features;
    }

    public void ShowInteractPrompt(bool show)
    {
        SafeSetActive(interactPrompt, show);
    }

    public void ShowResultPopup(bool correct)
    {
        StopAllCoroutines();
        StartCoroutine(PopupRoutine(correct));
    }

    private IEnumerator PopupRoutine(bool correct)
    {
        GameObject popup = correct ? correctPopup : wrongPopup;
        if (popup == null) yield break;

        SafeSetActive(correctPopup, false);
        SafeSetActive(wrongPopup, false);
        popup.SetActive(true);
        yield return new WaitForSecondsRealtime(popupDuration);
        popup.SetActive(false);
    }

    public void ShowGameOver(bool win)
    {
        SafeSetActive(interactPrompt, false);
        if (win) SafeSetActive(winPanel, true);
        else SafeSetActive(losePanel, true);
    }

    private void SafeSetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }
}

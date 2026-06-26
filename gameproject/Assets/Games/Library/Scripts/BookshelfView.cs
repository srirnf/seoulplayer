using UnityEngine;
using UnityEngine.UI;

// 책장 정면뷰 패널. 다가간 책장의 책들을 버튼으로 펼쳐 보여준다.
public class BookshelfView : MonoBehaviour
{
    public static BookshelfView Instance { get; private set; }

    [Tooltip("정면뷰 전체 패널 루트")]
    [SerializeField] private GameObject panel;

    [Tooltip("책장 정면뷰 배경 이미지(선택)")]
    [SerializeField] private Image shelfBackground;

    [Tooltip("책 버튼들이 생성될 부모 (Grid Layout Group 권장)")]
    [SerializeField] private Transform bookSlotParent;

    [Tooltip("책 버튼 프리팹 (BookButton 컴포넌트 포함)")]
    [SerializeField] private BookButton bookButtonPrefab;

    [Tooltip("닫기 버튼(선택) - 누르면 Close")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(Bookshelf shelf)
    {
        if (shelf == null || bookButtonPrefab == null || bookSlotParent == null) return;

        // 이전 버튼 정리
        for (int i = bookSlotParent.childCount - 1; i >= 0; i--)
            Destroy(bookSlotParent.GetChild(i).gameObject);

        // 책 버튼 생성
        foreach (var book in shelf.books)
        {
            if (book == null) continue;
            BookButton btn = Instantiate(bookButtonPrefab, bookSlotParent);
            btn.Setup(book);
        }

        if (panel != null) panel.SetActive(true);
        GameManager.Instance?.SetPaused(true);
        AudioManager.Instance?.PlayShelfClick();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GameManager.Instance?.SetPaused(false);
    }
}

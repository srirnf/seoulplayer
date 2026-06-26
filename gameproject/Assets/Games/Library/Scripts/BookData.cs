using UnityEngine;

// 책 한 권의 데이터. ScriptableObject 라서 에디터에서 42권을 에셋으로 만들 수 있다.
// (Project 창에서 우클릭 > Create > 별마당도서관 > Book Data)
[CreateAssetMenu(fileName = "Book", menuName = "별마당도서관/Book Data")]
public class BookData : ScriptableObject
{
    [Tooltip("책 고유 ID (중복되지 않게)")]
    public string id;

    [Tooltip("책 제목 (UI 표시용)")]
    public string title;

    [Tooltip("책등 이미지 (책장 정면뷰에 표시됨)")]
    public Sprite spineSprite;

    [TextArea]
    [Tooltip("목표 안내 UI에 보여줄 특징. 예: 빨간색, 두꺼운 책, 금장식")]
    public string features;
}

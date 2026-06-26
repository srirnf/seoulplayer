using UnityEngine;
using UnityEngine.UI;

// 책장 정면뷰에 표시되는 책 한 권 버튼. 클릭하면 정답 판정을 요청한다.
[RequireComponent(typeof(Button))]
public class BookButton : MonoBehaviour
{
    [Tooltip("책등 이미지를 표시할 Image")]
    [SerializeField] private Image spineImage;

    private BookData book;

    public void Setup(BookData data)
    {
        book = data;
        if (spineImage != null && data != null && data.spineSprite != null)
            spineImage.sprite = data.spineSprite;

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        GameManager.Instance?.OnBookSelected(book);
    }
}

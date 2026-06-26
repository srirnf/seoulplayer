using TMPro;
using UnityEngine;

// 게임방법 안내 슬라이드. ◀ ▶ 로 슬라이드를 넘겨본다.
public class HowToSlides : MonoBehaviour
{
    [SerializeField] private GameObject[] slides;
    [SerializeField] private TMP_Text counter;   // "1 / 5"
    [SerializeField] private GameObject prevButton;
    [SerializeField] private GameObject nextButton;

    private int index;

    private void OnEnable()
    {
        Show(0);
    }

    public void Next() { Show(index + 1); }
    public void Prev() { Show(index - 1); }

    private void Show(int i)
    {
        if (slides == null || slides.Length == 0) return;
        index = Mathf.Clamp(i, 0, slides.Length - 1);
        for (int k = 0; k < slides.Length; k++)
            if (slides[k]) slides[k].SetActive(k == index);
        if (counter) counter.text = (index + 1) + " / " + slides.Length;
        if (prevButton) prevButton.SetActive(index > 0);
        if (nextButton) nextButton.SetActive(index < slides.Length - 1);
    }
}

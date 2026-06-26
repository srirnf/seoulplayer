using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 책 찾기 2D 화면. 책장 이미지에서 "목표 책"(크롭해서 보여줌)을 찾아 클릭한다.
// 비슷한 책이 많아서 난이도가 있고, 제한시간 + 기회제한 + 여러 권 연속으로 더 어렵게.
public class FindBookScreen : MonoBehaviour
{
    // 책장(캐비닛) 영역을 이미지 UV(0~1)로 정의. cols x rows 격자로 책 칸을 만든다.
    [System.Serializable]
    public class Cabinet
    {
        public Rect uv = new Rect(0.05f, 0.13f, 0.42f, 0.74f); // x,y,width,height (0~1)
        public int cols = 7;
        public int rows = 3;
    }

    [Header("UI 참조")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RawImage shelfImage;       // 전체 책장 이미지(Texture)
    [SerializeField] private RectTransform buttonsParent; // shelfImage 위에 똑같이 덮는 영역
    [SerializeField] private RawImage targetThumb;        // 찾을 책(크롭)
    [SerializeField] private TMP_Text infoText;          // 안내/시간/기회
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("책장 영역(이미지에 맞게 조정)")]
    [SerializeField] private List<Cabinet> cabinets = new List<Cabinet> { new Cabinet(), new Cabinet { uv = new Rect(0.53f, 0.13f, 0.42f, 0.74f) } };

    [Header("난이도")]
    [SerializeField] private float timeLimit = 30f;      // 제한시간(초)
    [SerializeField] private float wrongPenalty = 1.5f;  // 오답 시 시간 깎임(초)

    public bool IsOpen { get; private set; }

    private LibraryInteract owner;
    private readonly List<Rect> cellUVs = new List<Rect>();
    private int targetIndex;
    private int found;     // 찾은 책 수(점수)
    private float timeLeft;
    private bool playing;

    private void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    public void Open(LibraryInteract from)
    {
        owner = from;
        IsOpen = true;
        if (panel) panel.SetActive(true);
        if (resultPanel) resultPanel.SetActive(false);

        found = 0; timeLeft = timeLimit; playing = true;
        BuildCells();
        NextTarget();
        UpdateInfo();
    }

    public void Close()
    {
        IsOpen = false;
        playing = false;
        if (panel) panel.SetActive(false);
        if (owner) owner.OnScreenClosed();
    }

    private void Update()
    {
        if (!IsOpen) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) { Close(); return; }

        if (playing)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f) { timeLeft = 0f; EndGame(); }
            UpdateInfo();
        }
    }

    private void BuildCells()
    {
        for (int i = buttonsParent.childCount - 1; i >= 0; i--)
            Destroy(buttonsParent.GetChild(i).gameObject);
        cellUVs.Clear();

        foreach (var cab in cabinets)
        {
            float cw = cab.uv.width / Mathf.Max(1, cab.cols);
            float ch = cab.uv.height / Mathf.Max(1, cab.rows);
            for (int r = 0; r < cab.rows; r++)
                for (int c = 0; c < cab.cols; c++)
                {
                    Rect uv = new Rect(cab.uv.x + c * cw, cab.uv.y + r * ch, cw, ch);
                    int index = cellUVs.Count;
                    cellUVs.Add(uv);
                    CreateCellButton(uv, index);
                }
        }
    }

    private void CreateCellButton(Rect uv, int index)
    {
        var go = new GameObject($"Book_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(buttonsParent, false);
        rt.anchorMin = new Vector2(uv.x, uv.y);
        rt.anchorMax = new Vector2(uv.xMax, uv.yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f); // 투명(클릭만 받음)
        img.raycastTarget = true;

        int captured = index;
        go.GetComponent<Button>().onClick.AddListener(() => OnCellClicked(captured));
    }

    private void NextTarget()
    {
        if (cellUVs.Count == 0) return;
        targetIndex = Random.Range(0, cellUVs.Count);
        if (targetThumb && shelfImage)
        {
            targetThumb.texture = shelfImage.texture;
            targetThumb.uvRect = cellUVs[targetIndex];
        }
    }

    private void OnCellClicked(int index)
    {
        if (!playing) return;
        if (index == targetIndex)
        {
            found++;            // 점수 +1, 정답 책을 새로 바꿈
            NextTarget();
            UpdateInfo();
        }
        else
        {
            timeLeft = Mathf.Max(0f, timeLeft - wrongPenalty); // 오답 = 시간 깎임
            StopAllCoroutines(); StartCoroutine(Flash());
            UpdateInfo();
        }
    }

    private IEnumerator Flash()
    {
        if (infoText) { var c = infoText.color; infoText.color = Color.red; yield return new WaitForSecondsRealtime(0.2f); infoText.color = c; }
    }

    private void UpdateInfo()
    {
        if (!infoText) return;
        infoText.text = $"이 책을 찾으세요!   찾은 책 {found}권   ·   ⏱ {Mathf.CeilToInt(timeLeft)}초";
    }

    private void EndGame()
    {
        playing = false;
        if (resultPanel) resultPanel.SetActive(true);
        if (resultText) resultText.text = $"시간 종료!\n{found}권 찾았어요!";
    }
}

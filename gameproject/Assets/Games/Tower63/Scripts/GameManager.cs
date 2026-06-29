using UnityEngine;
using UnityEngine.SceneManagement; // 화면 전환을 위해 꼭 필요해요!
using UnityEngine.UI;              // HUD 알약(Image/Canvas) 자동 스타일링용
using TMPro; // TMP 텍스트를 쓰기 위해 꼭 필요합니다!

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Score System")]
    public int score = 0;
    public TextMeshProUGUI scoreText;

    [Header("Timer System")]
    public float timeRemaining = 90f; // 1분 30초 = 90초
    public TextMeshProUGUI timerText;
    private bool isGameOver = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f; // 이전 게임오버로 0이 된 채 넘어왔을 수 있으니 항상 1로 시작

        // 시작 메뉴(StartMenu)에선 HUD/타이머를 띄우지 않는다(점수·시간 숨김 + 시간 정지)
        if (SceneManager.GetActiveScene().name == "StartMenu")
        {
            if (scoreText) scoreText.gameObject.SetActive(false);
            if (timerText) timerText.gameObject.SetActive(false);
            isGameOver = true; // 메뉴에선 시간 안 흐르게
            return;
        }

        StyleHud();          // 시간/점수 UI를 자동으로 둥근 알약 코너 HUD로 정리(메뉴 클릭 불필요)
        UpdateUI();
    }

    // ===== HUD 자동 스타일링 (Play 시 자동 실행) =====
    // 시간/점수 텍스트를 정지된 오버레이 캔버스의 코너 알약으로 옮긴다.
    // (브러시를 따라다니던 문제도 여기서 해결됨)
    private void StyleHud()
    {
        if (scoreText == null || timerText == null) return;

        var canvasGO = new GameObject("HudCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 항상 맨 위
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var round = MakeRoundedSprite(48, 16);
        MakePill(scoreText, round, canvas.transform, true,  new Color(1f, 0.82f, 0.32f));    // 좌상단, 금색
        MakePill(timerText, round, canvas.transform, false, new Color(0.42f, 0.84f, 1f));    // 우상단, 하늘색
    }

    private void MakePill(TMP_Text text, Sprite round, Transform canvasT, bool leftSide, Color accent)
    {
        Vector2 corner = leftSide ? new Vector2(0f, 1f) : new Vector2(1f, 1f);

        // 바깥 테두리(악센트 색)
        var border = new GameObject(leftSide ? "ScorePill" : "TimerPill", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(canvasT, false);
        var bRT = border.GetComponent<RectTransform>();
        bRT.anchorMin = corner; bRT.anchorMax = corner; bRT.pivot = corner;
        bRT.sizeDelta = new Vector2(360f, 108f);
        bRT.anchoredPosition = leftSide ? new Vector2(34f, -34f) : new Vector2(-34f, -34f);
        var bImg = border.GetComponent<Image>();
        bImg.sprite = round; bImg.type = Image.Type.Sliced; bImg.color = accent; bImg.raycastTarget = false;

        // 안쪽 어두운 속지(테두리 5px 남기고)
        var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(border.transform, false);
        var iRT = inner.GetComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
        iRT.offsetMin = new Vector2(5f, 5f); iRT.offsetMax = new Vector2(-5f, -5f);
        var iImg = inner.GetComponent<Image>();
        iImg.sprite = round; iImg.type = Image.Type.Sliced;
        iImg.color = new Color(0.09f, 0.10f, 0.14f, 0.94f);
        iImg.raycastTarget = false;

        // 텍스트(악센트 색, 점수=좌측정렬 / 시간=우측정렬로 대칭)
        var rt = text.rectTransform;
        rt.SetParent(iRT, false); // 기존 텍스트를 알약 안으로 이동(브러시 추적 해제)
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(26f, 6f); rt.offsetMax = new Vector2(-26f, -6f);
        text.alignment = leftSide ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        text.enableAutoSizing = false;
        text.fontSize = 54f;
        text.fontStyle = FontStyles.Bold;
        text.color = accent;
        text.raycastTarget = false;
    }

    // 런타임용 둥근 9-슬라이스 스프라이트(에셋 저장 없이 메모리에서 생성)
    private Sprite MakeRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                float dx = x - cx, dy = y - cy;
                bool inside = dx * dx + dy * dy <= (float)radius * radius;
                px[y * size + x] = inside ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        tex.SetPixels(px); tex.Apply();
        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    void Update()
    {
        // 게임 오버 상태가 아니라면 시간이 계속 줄어듭니다.
        if (!isGameOver)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateUI();
            }
            else
            {
                timeRemaining = 0;
                GameOver();
            }
        }
    }

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (timerText != null) timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        UpdateUI();
    }

    public void DeductScore(int amount)
    {
        if (isGameOver) return;
        score -= amount;
        if (score < 0) score = 0;
        UpdateUI();
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over!");
        Time.timeScale = 0f; // 스포너(InvokeRepeating)·이동·시간 전부 정지
    }

    // ==========================================
    // 버튼을 누르면 게임이 시작되는(화면이 넘어가는) 함수입니다!
    public void SceneChange()
    {
        // "GameScene" 자리에 실제 플레이하는 게임 씬(화면) 이름을 정확하게 적어주세요.
        // 만약 실제 게임 화면 이름이 'tower63' 이라면 "tower63"으로 적어야 합니다!
        SceneManager.LoadScene("tower63"); 
    }
    // ==========================================
}
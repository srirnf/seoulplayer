using System.Collections.Generic;
using UnityEngine;

// 게임 전체 진행 관리: 목표 책 선정, 찾은 수/제한시간, 정답 판정, 승패.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 설정")]
    [Tooltip("제한 시간(초)")]
    [SerializeField] private float timeLimit = 120f;
    [Tooltip("클리어에 필요한 책 수")]
    [SerializeField] private int targetCount = 5;

    public bool IsPaused { get; private set; }
    public BookData CurrentTarget { get; private set; }

    private float timeLeft;
    private int foundCount;
    private bool isPlaying;
    private readonly List<BookData> allBooks = new List<BookData>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 씬에 있는 모든 책장에서 책 목록을 모은다
        foreach (var shelf in FindObjectsByType<Bookshelf>(FindObjectsSortMode.None))
        {
            foreach (var book in shelf.books)
            {
                if (book != null && !allBooks.Contains(book))
                    allBooks.Add(book);
            }
        }

        timeLeft = timeLimit;
        foundCount = 0;
        isPlaying = true;

        PickNewTarget();
        GameUIManager.Instance?.UpdateFound(foundCount, targetCount);
    }

    private void Update()
    {
        if (!isPlaying || IsPaused) return;

        timeLeft -= Time.deltaTime;
        GameUIManager.Instance?.UpdateTimer(Mathf.Max(0f, timeLeft));

        if (timeLeft <= 0f)
            GameOver(false);
    }

    private void PickNewTarget()
    {
        if (allBooks.Count == 0) return;

        // 직전 목표와 다른 책을 고른다
        BookData next = CurrentTarget;
        if (allBooks.Count > 1)
        {
            while (next == CurrentTarget)
                next = allBooks[Random.Range(0, allBooks.Count)];
        }
        else
        {
            next = allBooks[0];
        }

        CurrentTarget = next;
        GameUIManager.Instance?.ShowTargetHint(CurrentTarget);
    }

    // 책장 정면뷰에서 책을 클릭하면 호출된다
    public void OnBookSelected(BookData book)
    {
        if (!isPlaying) return;

        if (book == CurrentTarget)
        {
            foundCount++;
            AudioManager.Instance?.PlayCorrect();
            GameUIManager.Instance?.ShowResultPopup(true);
            GameUIManager.Instance?.UpdateFound(foundCount, targetCount);
            BookshelfView.Instance?.Close();

            if (foundCount >= targetCount)
                GameOver(true);
            else
                PickNewTarget();
        }
        else
        {
            AudioManager.Instance?.PlayWrong();
            GameUIManager.Instance?.ShowResultPopup(false);
        }
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
    }

    private void GameOver(bool win)
    {
        isPlaying = false;
        BookshelfView.Instance?.Close();
        GameUIManager.Instance?.ShowGameOver(win);
    }
}

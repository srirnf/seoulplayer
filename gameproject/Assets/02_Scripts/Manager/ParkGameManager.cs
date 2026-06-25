using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 서울대공원(동물꼬시기) 진행 관리: 잡은 동물 수, 경과 시간(랭킹용),
// 전부 잡으면 엔딩(동물들이 우리로 복귀) 연출 후 클리어.
public class ParkGameManager : MonoBehaviour
{
    public static ParkGameManager Instance { get; private set; }

    [Tooltip("동물들이 들어갈 우리 위치들")]
    [SerializeField] private Transform[] cagePoints;

    public bool IsPlaying { get; private set; }
    public float Elapsed { get; private set; }
    public int CaughtCount => caughtCount;

    private readonly List<Animal> allAnimals = new List<Animal>();
    private int caughtCount;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        allAnimals.AddRange(FindObjectsByType<Animal>(FindObjectsSortMode.None));
        caughtCount = 0;
        Elapsed = 0f;
        IsPlaying = false; // 게임방법 안내 확인 전까지 대기
        ParkUIManager.Instance?.UpdateCaught(0, allAnimals.Count);
        ParkUIManager.Instance?.UpdateTimer(0f);
    }

    // 안내 화면에서 확인을 누르면 시작
    public void BeginGame()
    {
        Elapsed = 0f;
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        Elapsed += Time.deltaTime;
        ParkUIManager.Instance?.UpdateTimer(Elapsed);
    }

    // 다시하기: 현재 씬을 다시 로드
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public Animal GetNearestFreeAnimal(Vector3 from, float range)
    {
        Animal best = null;
        float bestSq = range * range;
        foreach (var a in allAnimals)
        {
            if (a == null || !a.IsLureable) continue;
            float sq = (a.transform.position - from).sqrMagnitude;
            if (sq <= bestSq)
            {
                bestSq = sq;
                best = a;
            }
        }
        return best;
    }

    public void OnAnimalCaught(Animal a)
    {
        caughtCount++;
        ParkUIManager.Instance?.UpdateCaught(caughtCount, allAnimals.Count);
        if (caughtCount >= allAnimals.Count && allAnimals.Count > 0)
            StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        IsPlaying = false;

        // 화면 전환 연출
        ParkUIManager.Instance?.ShowEndingTransition();
        yield return new WaitForSeconds(0.8f);

        // 동물들을 각자 우리로
        for (int i = 0; i < allAnimals.Count; i++)
        {
            Vector3 cage = (cagePoints != null && cagePoints.Length > 0)
                ? cagePoints[i % cagePoints.Length].position
                : Vector3.zero;
            allAnimals[i].ReturnToCage(cage);
        }

        // 다 들어갈 때까지 대기(최대 4초)
        float t = 0f;
        while (t < 4f)
        {
            bool allIn = true;
            foreach (var a in allAnimals)
                if (!a.ReachedCage()) { allIn = false; break; }
            if (allIn) break;
            t += Time.deltaTime;
            yield return null;
        }

        ParkUIManager.Instance?.ShowClear(Elapsed);
    }
}

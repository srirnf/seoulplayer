using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어(사육사). WASD로 이동하고, 가장 가까운 자유 동물을 자동 록온한 뒤
// Space를 꾹 눌러 유인 게이지를 채운다. 꼬셔진 동물은 줄줄이 따라온다.
public class AnimalCatcher : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.5f;
    [Tooltip("자동 록온 범위")]
    [SerializeField] private float lockRange = 3f;

    private readonly List<Animal> caughtChain = new List<Animal>();
    private Animal locked;
    private SpriteRenderer sr;
    private float groundY;
    private float minX = float.NegativeInfinity;
    private float maxX = float.PositiveInfinity;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        groundY = transform.position.y;
    }

    // 배경 밖으로 못 나가게 좌우 이동 한계 설정
    public void SetBounds(float min, float max)
    {
        minX = min;
        maxX = max;
    }

    private void Update()
    {
        var gm = ParkGameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;

        Move();
        UpdateLockAndLure();
    }

    // 사이드뷰: 좌우로만 이동(A/D 또는 ←/→). y는 바닥 라인에 고정.
    private void Move()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = ((kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f)
                - ((kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f);

        Vector3 p = transform.position;
        p.x += x * moveSpeed * Time.deltaTime;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = groundY;
        transform.position = p;

        if (sr != null && x != 0f) sr.flipX = x < 0f;
    }

    private void UpdateLockAndLure()
    {
        Animal nearest = ParkGameManager.Instance != null
            ? ParkGameManager.Instance.GetNearestFreeAnimal(transform.position, lockRange)
            : null;

        if (nearest != locked)
        {
            if (locked != null) locked.SetLocked(false);
            locked = nearest;
            if (locked != null) locked.SetLocked(true);
        }

        // 타이밍: 마커가 성공 구간에 있을 때 Space를 "눌러" 맞추기
        var kb = Keyboard.current;
        if (locked != null && kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            bool hit = locked.AttemptTiming(transform.position);
            if (hit) CatchAnimal(locked);
            // 실패하면 동물이 도망가고, 다음 프레임에 록온이 자동 해제됨
        }
    }

    private void CatchAnimal(Animal a)
    {
        // 첫 동물은 플레이어를, 이후엔 줄의 맨 뒤 동물을 따라간다(줄줄이)
        Transform target = caughtChain.Count == 0 ? transform : caughtChain[caughtChain.Count - 1].transform;
        a.Catch(target);
        caughtChain.Add(a);
        if (locked == a) locked = null;
        ParkGameManager.Instance?.OnAnimalCaught(a);
    }
}

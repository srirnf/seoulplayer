using UnityEngine;

// 탈출한 동물. 평소 배회하고, 록온되면 멈춰서 머리 위 "타이밍 바"의 마커가 좌우로 움직인다.
// 플레이어가 성공 구간에서 Space를 맞추면 꼬셔져 따라오고, 틀리면 1~2초 도망간다.
public class Animal : MonoBehaviour
{
    public enum State { Free, Fleeing, Caught, Returning }

    [Header("배회(도망 전)")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float repathTime = 2f;

    [Header("따라오기")]
    [SerializeField] private float followSpeed = 3.5f;
    [SerializeField] private float followSpacing = 0.8f;

    [Header("도망(타이밍 실패 시)")]
    [SerializeField] private float fleeSpeed = 4.5f;

    [Header("타이밍")]
    [SerializeField] private float markerSpeed = 1.6f;     // 마커 왕복 속도
    [SerializeField] private float trackHalfWidth = 0.6f;  // 마커 이동 반경(월드)
    [SerializeField] private float zoneHalfWidth = 0.14f;  // 성공 구간 반경(월드)

    [Header("연출 참조")]
    [SerializeField] private GameObject lockIndicator; // 록온 테두리
    [SerializeField] private GameObject timingBar;     // 타이밍 바 루트
    [SerializeField] private Transform marker;         // 움직이는 마커

    public State CurrentState { get; private set; } = State.Free;
    public bool IsLureable => CurrentState == State.Free;
    public bool IsCaught => CurrentState == State.Caught || CurrentState == State.Returning;

    private SpriteRenderer sr;
    private Vector3 homeCenter;
    private Vector3 wanderTarget;
    private float repathTimer;
    private float fleeTimer;
    private float fleeDir;
    private bool isLocked;
    private Transform followTarget;
    private Vector3 cagePosition;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        homeCenter = transform.position;
        PickWanderTarget();
        if (lockIndicator) lockIndicator.SetActive(false);
        if (timingBar) timingBar.SetActive(false);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.Free: FreeUpdate(); break;
            case State.Fleeing: FleeUpdate(); break;
            case State.Caught: FollowUpdate(); break;
            case State.Returning: ReturnUpdate(); break;
        }
    }

    private void FreeUpdate()
    {
        if (isLocked)
        {
            // 록온 중엔 멈춰서 타이밍 마커만 움직임
            if (marker != null)
            {
                float t = CurrentMarkerT();
                marker.localPosition = new Vector3(t * trackHalfWidth, marker.localPosition.y, marker.localPosition.z);
            }
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f || Mathf.Abs(transform.position.x - wanderTarget.x) < 0.05f)
            PickWanderTarget();
        if (sr != null) sr.flipX = wanderTarget.x < transform.position.x;
        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);
    }

    // -1 ~ 1 사이를 왕복
    private float CurrentMarkerT()
    {
        return Mathf.PingPong(Time.time * markerSpeed * 2f, 2f) - 1f;
    }

    private void PickWanderTarget()
    {
        float dx = Random.Range(-wanderRadius, wanderRadius);
        wanderTarget = new Vector3(homeCenter.x + dx, homeCenter.y, 0f);
        repathTimer = repathTime;
    }

    public void SetLocked(bool locked)
    {
        if (CurrentState != State.Free) locked = false;
        isLocked = locked;
        if (lockIndicator) lockIndicator.SetActive(locked);
        if (timingBar) timingBar.SetActive(locked);
    }

    // 타이밍 시도: 성공이면 true(잡힘), 실패면 도망 후 false
    public bool AttemptTiming(Vector3 playerPos)
    {
        if (CurrentState != State.Free) return false;
        float x = CurrentMarkerT() * trackHalfWidth;
        bool hit = Mathf.Abs(x) <= zoneHalfWidth;
        if (!hit) Flee(playerPos);
        return hit;
    }

    private void Flee(Vector3 from)
    {
        CurrentState = State.Fleeing;
        isLocked = false;
        fleeTimer = Random.Range(1f, 2f);
        fleeDir = (transform.position.x >= from.x) ? 1f : -1f;
        if (lockIndicator) lockIndicator.SetActive(false);
        if (timingBar) timingBar.SetActive(false);
    }

    private void FleeUpdate()
    {
        fleeTimer -= Time.deltaTime;
        if (sr != null) sr.flipX = fleeDir < 0f;
        transform.position += new Vector3(fleeDir * fleeSpeed * Time.deltaTime, 0f, 0f);
        if (fleeTimer <= 0f)
        {
            CurrentState = State.Free;
            homeCenter = transform.position;
            PickWanderTarget();
        }
    }

    public void Catch(Transform target)
    {
        CurrentState = State.Caught;
        followTarget = target;
        isLocked = false;
        if (lockIndicator) lockIndicator.SetActive(false);
        if (timingBar) timingBar.SetActive(false);
    }

    private void FollowUpdate()
    {
        if (followTarget == null) return;
        if (Vector3.Distance(transform.position, followTarget.position) > followSpacing)
        {
            if (sr != null) sr.flipX = followTarget.position.x < transform.position.x;
            transform.position = Vector3.MoveTowards(transform.position, followTarget.position, followSpeed * Time.deltaTime);
        }
    }

    public void ReturnToCage(Vector3 cagePos)
    {
        cagePosition = cagePos;
        CurrentState = State.Returning;
    }

    private void ReturnUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, cagePosition, followSpeed * Time.deltaTime);
    }

    public bool ReachedCage()
    {
        return CurrentState == State.Returning && (transform.position - cagePosition).sqrMagnitude < 0.02f;
    }
}

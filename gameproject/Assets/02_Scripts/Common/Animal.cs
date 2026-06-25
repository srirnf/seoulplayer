using UnityEngine;

// 탈출한 동물. Space 길게 눌러 충전→록온. 떼면 랜덤 미니게임(타이밍/연타)이 뜨고,
// 성공하면 따라온다. 실패 시 도망. 잡은 동물이 많을수록 미니게임이 어려워진다.
public class Animal : MonoBehaviour
{
    public enum State { Free, Fleeing, Caught, Returning }
    private enum Mini { Timing, Mash }

    [Header("배회(도망 전)")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float repathTime = 2f;

    [Header("따라오기")]
    [SerializeField] private float followSpeed = 3.5f;
    [SerializeField] private float followSpacing = 0.8f;

    [Header("도망(실패 시)")]
    [SerializeField] private float fleeSpeed = 4.5f;

    [Header("타이밍 게임")]
    [SerializeField] private float markerSpeed = 1.5f;     // 마커 왕복 속도(기본)
    [SerializeField] private float trackHalfWidth = 1.0f;  // 마커 이동 반경(월드)
    [SerializeField] private float zoneHalfWidth = 0.22f;  // 성공 구간 반경(기본)

    [Header("연타 게임")]
    [SerializeField] private float mashTime = 3f;          // 제한 시간

    [Header("좌우 경계")]
    [SerializeField] private float minX = -10000f;
    [SerializeField] private float maxX = 10000f;

    [Header("연출 참조")]
    [SerializeField] private GameObject selectionRing;
    [SerializeField] private GameObject chargeRoot;
    [SerializeField] private Transform chargeFill;
    [SerializeField] private GameObject timingBar;
    [SerializeField] private Transform zoneTransform; // 성공 구간(초록) - 난이도따라 축소
    [SerializeField] private Transform marker;
    [SerializeField] private GameObject mashRoot;
    [SerializeField] private Transform mashFill;

    public State CurrentState { get; private set; } = State.Free;
    public bool IsLureable => CurrentState == State.Free;
    public bool IsCaught => CurrentState == State.Caught || CurrentState == State.Returning;

    private SpriteRenderer sr;
    private Vector3 homeCenter;
    private Vector3 wanderTarget;
    private float repathTimer;
    private float fleeTimer;
    private float fleeDir;
    private bool targeted;     // 충전/록온 중
    private bool miniActive;   // 미니게임 진행 중
    private Mini miniType;
    private Transform followTarget;
    private Vector3 cagePosition;

    // 미니게임 난이도 적용값
    private float effMarkerSpeed;
    private float effZoneHalf;
    private float mashProgress;
    private float mashTimer;
    private float mashPerTap;
    private float mashDrain;

    public void SetBounds(float a, float b) { minX = a; maxX = b; }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        homeCenter = transform.position;
        PickWanderTarget();
        if (selectionRing) selectionRing.SetActive(false);
        if (chargeRoot) chargeRoot.SetActive(false);
        if (timingBar) timingBar.SetActive(false);
        if (mashRoot) mashRoot.SetActive(false);
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
        // 좌우 경계 클램프
        if (transform.position.x < minX || transform.position.x > maxX)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            transform.position = p;
        }
    }

    private void FreeUpdate()
    {
        if (targeted || miniActive) return; // 멈춤(충전/미니게임 중)

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f || Mathf.Abs(transform.position.x - wanderTarget.x) < 0.05f)
            PickWanderTarget();
        if (sr != null) sr.flipX = wanderTarget.x < transform.position.x;
        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);
    }

    private void PickWanderTarget()
    {
        float dx = Random.Range(-wanderRadius, wanderRadius);
        float tx = Mathf.Clamp(homeCenter.x + dx, minX, maxX);
        wanderTarget = new Vector3(tx, homeCenter.y, 0f);
        repathTimer = repathTime;
    }

    // 충전/록온 표시
    public void SetTargeted(bool on)
    {
        if (CurrentState != State.Free) on = false;
        targeted = on;
        if (selectionRing) selectionRing.SetActive(on);
        if (!on && chargeRoot) chargeRoot.SetActive(false);
    }

    public void SetLockCharge(float t)
    {
        if (chargeRoot) chargeRoot.SetActive(t >= 0f);
        if (chargeFill && t >= 0f)
            chargeFill.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
    }

    // 미니게임 시작(잡은 수가 많을수록 어렵게)
    public void StartMiniGame(int caught)
    {
        if (CurrentState != State.Free) return;
        miniActive = true;
        miniType = (Random.value < 0.5f) ? Mini.Timing : Mini.Mash;

        if (miniType == Mini.Timing)
        {
            float speedMul = 1f + caught * 0.18f;
            float zoneMul = Mathf.Clamp(1f - caught * 0.13f, 0.4f, 1f);
            effMarkerSpeed = markerSpeed * speedMul;
            effZoneHalf = zoneHalfWidth * zoneMul;
            if (zoneTransform) zoneTransform.localScale = new Vector3(zoneMul, 1f, 1f);
            if (timingBar) timingBar.SetActive(true);
        }
        else
        {
            int requiredTaps = 6 + caught;
            mashPerTap = 1f / requiredTaps;
            mashDrain = 0.30f + caught * 0.06f;
            mashTimer = mashTime;
            mashProgress = 0f;
            if (mashFill) mashFill.localScale = new Vector3(0f, 1f, 1f);
            if (mashRoot) mashRoot.SetActive(true);
        }
    }

    // 미니게임 1틱. 반환: 0=진행, 1=성공, 2=실패
    public int TickMiniGame(Vector3 playerPos, bool pressed)
    {
        if (!miniActive) return 0;

        if (miniType == Mini.Timing)
        {
            float t = Mathf.PingPong(Time.time * effMarkerSpeed * 2f, 2f) - 1f;
            float x = t * trackHalfWidth;
            if (marker) marker.localPosition = new Vector3(x, marker.localPosition.y, marker.localPosition.z);
            if (pressed)
            {
                bool hit = Mathf.Abs(x) <= effZoneHalf;
                EndMini();
                if (!hit) { Flee(playerPos); return 2; }
                return 1;
            }
            return 0;
        }
        else
        {
            mashTimer -= Time.deltaTime;
            if (pressed) mashProgress += mashPerTap;
            mashProgress = Mathf.Max(0f, mashProgress - mashDrain * Time.deltaTime);
            if (mashFill) mashFill.localScale = new Vector3(Mathf.Clamp01(mashProgress), 1f, 1f);

            if (mashProgress >= 1f) { EndMini(); return 1; }
            if (mashTimer <= 0f) { EndMini(); Flee(playerPos); return 2; }
            return 0;
        }
    }

    private void EndMini()
    {
        miniActive = false;
        if (timingBar) timingBar.SetActive(false);
        if (mashRoot) mashRoot.SetActive(false);
    }

    private void Flee(Vector3 from)
    {
        CurrentState = State.Fleeing;
        ClearVisuals();
        fleeTimer = Random.Range(1f, 2f);
        fleeDir = (transform.position.x >= from.x) ? 1f : -1f;
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
        ClearVisuals();
    }

    private void ClearVisuals()
    {
        targeted = false;
        miniActive = false;
        if (selectionRing) selectionRing.SetActive(false);
        if (chargeRoot) chargeRoot.SetActive(false);
        if (timingBar) timingBar.SetActive(false);
        if (mashRoot) mashRoot.SetActive(false);
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

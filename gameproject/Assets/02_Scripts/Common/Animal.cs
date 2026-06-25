using UnityEngine;

// 탈출한 동물 한 마리. 평소엔 배회(도망)하고, 유인 게이지가 가득 차면 꼬셔져서
// 플레이어(또는 앞 동물)를 줄줄이 따라온다. 엔딩 때는 우리로 복귀한다.
public class Animal : MonoBehaviour
{
    public enum State { Free, Caught, Returning }

    [Header("배회(도망) 설정")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float repathTime = 2f;

    [Header("따라오기 설정")]
    [SerializeField] private float followSpeed = 3.5f;
    [SerializeField] private float followSpacing = 0.8f;

    [Header("유인")]
    [Tooltip("유인 안 할 때 게이지가 줄어드는 속도(초당)")]
    [SerializeField] private float lureDecay = 0.4f;

    [Header("연출 참조")]
    [SerializeField] private GameObject lockIndicator; // 록온 표시(자동 록온 시 켜짐)
    [SerializeField] private GameObject gaugeRoot;     // 유인 게이지 루트
    [SerializeField] private Transform gaugeFill;      // 채워지는 막대(localScale.x)

    public State CurrentState { get; private set; } = State.Free;
    public bool IsCaught => CurrentState != State.Free;
    public float LureProgress { get; private set; }

    private Vector3 homeCenter;
    private Vector3 wanderTarget;
    private float repathTimer;
    private bool luredThisFrame;

    private Transform followTarget;
    private Vector3 cagePosition;

    private void Start()
    {
        homeCenter = transform.position;
        PickWanderTarget();
        if (lockIndicator) lockIndicator.SetActive(false);
        if (gaugeRoot) gaugeRoot.SetActive(false);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.Free: FreeUpdate(); break;
            case State.Caught: FollowUpdate(); break;
            case State.Returning: ReturnUpdate(); break;
        }
    }

    private void FreeUpdate()
    {
        // 유인 중이면 멈춰서 잡히기 쉽게, 아니면 배회
        if (!luredThisFrame)
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f || (transform.position - wanderTarget).sqrMagnitude < 0.05f)
                PickWanderTarget();
            transform.position = Vector3.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);

            if (LureProgress > 0f)
                LureProgress = Mathf.Max(0f, LureProgress - lureDecay * Time.deltaTime);
        }

        UpdateGauge();
        luredThisFrame = false;
    }

    private void PickWanderTarget()
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        wanderTarget = homeCenter + new Vector3(r.x, r.y, 0f);
        repathTimer = repathTime;
    }

    public void SetLocked(bool locked)
    {
        if (lockIndicator) lockIndicator.SetActive(locked && CurrentState == State.Free);
    }

    // 플레이어가 유인할 때 매 프레임 호출. 가득 차면 true 반환.
    public bool ApplyLure(float amount)
    {
        if (CurrentState != State.Free) return false;
        luredThisFrame = true;
        LureProgress = Mathf.Min(1f, LureProgress + amount);
        UpdateGauge();
        return LureProgress >= 1f;
    }

    private void UpdateGauge()
    {
        if (gaugeRoot) gaugeRoot.SetActive(CurrentState == State.Free && LureProgress > 0.001f);
        if (gaugeFill) gaugeFill.localScale = new Vector3(Mathf.Clamp01(LureProgress), 1f, 1f);
    }

    public void Catch(Transform target)
    {
        CurrentState = State.Caught;
        followTarget = target;
        if (lockIndicator) lockIndicator.SetActive(false);
        if (gaugeRoot) gaugeRoot.SetActive(false);
    }

    private void FollowUpdate()
    {
        if (followTarget == null) return;
        if (Vector3.Distance(transform.position, followTarget.position) > followSpacing)
            transform.position = Vector3.MoveTowards(transform.position, followTarget.position, followSpeed * Time.deltaTime);
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

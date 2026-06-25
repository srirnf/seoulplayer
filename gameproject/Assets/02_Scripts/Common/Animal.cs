using UnityEngine;
using UnityEngine.InputSystem;

// 탈출한 동물. Space 길게 충전→록온. 떼면 랜덤 미니게임이 뜨고, 성공하면 따라온다.
// 미니게임: 타이밍 / 움직이는 초록칸 / 연타 / 방향키 순서. 잡을수록 어려워진다.
public class Animal : MonoBehaviour
{
    public enum State { Free, Fleeing, Caught, Returning }
    private enum Mini { Timing, Mash, MovingZone, Arrow }
    public const int MiniGameCount = 4;

    [Header("배회(도망 전)")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float repathTime = 2f;

    [Header("따라오기")]
    [SerializeField] private float followSpeed = 3.5f;
    [SerializeField] private float followSpacing = 0.8f;

    [Header("도망(실패 시)")]
    [SerializeField] private float fleeSpeed = 4.5f;

    [Header("타이밍/움직이는초록")]
    [SerializeField] private float markerSpeed = 1.5f;
    [SerializeField] private float trackHalfWidth = 1.0f;
    [SerializeField] private float zoneHalfWidth = 0.22f;

    [Header("연타")]
    [SerializeField] private float mashTime = 3f;

    [Header("좌우 경계")]
    [SerializeField] private float minX = -10000f;
    [SerializeField] private float maxX = 10000f;

    [Header("연출 참조")]
    [SerializeField] private GameObject selectionRing;
    [SerializeField] private GameObject chargeRoot;
    [SerializeField] private Transform chargeFill;
    [SerializeField] private GameObject timingBar;
    [SerializeField] private Transform zoneTransform;
    [SerializeField] private Transform marker;
    [SerializeField] private GameObject mashRoot;
    [SerializeField] private Transform mashFill;
    [SerializeField] private GameObject arrowRoot;
    [SerializeField] private SpriteRenderer[] arrowSlots;

    public State CurrentState { get; private set; } = State.Free;
    public bool IsLureable => CurrentState == State.Free;
    public bool IsCaught => CurrentState == State.Caught || CurrentState == State.Returning;

    private static readonly Color cDone = new Color(0.30f, 0.80f, 0.42f);
    private static readonly Color cCur = new Color(1f, 0.85f, 0.20f);
    private static readonly Color cTodo = new Color(0.62f, 0.62f, 0.62f);

    private SpriteRenderer sr;
    private Vector3 homeCenter, wanderTarget;
    private float repathTimer, fleeTimer, fleeDir;
    private bool targeted, miniActive;
    private Mini miniType;
    private Transform followTarget;
    private Vector3 cagePosition;

    private float effMarkerSpeed, effZoneHalf;
    private float mashProgress, mashTimer, mashPerTap, mashDrain;
    private int[] arrowSeq;
    private int arrowLen, arrowIdx;
    private float arrowTimer;

    public void SetBounds(float a, float b) { minX = a; maxX = b; }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        homeCenter = transform.position;
        PickWanderTarget();
        HideAll();
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
        if (transform.position.x < minX || transform.position.x > maxX)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            transform.position = p;
        }
    }

    private void FreeUpdate()
    {
        if (targeted || miniActive) return;
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
        if (chargeFill && t >= 0f) chargeFill.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
    }

    public void StartMiniGame(int caught, int type)
    {
        if (CurrentState != State.Free) return;
        miniActive = true;
        miniType = (Mini)Mathf.Clamp(type, 0, MiniGameCount - 1);

        float speedMul = 1f + caught * 0.13f;
        float zoneMul = Mathf.Clamp(1f - caught * 0.09f, 0.58f, 1f);
        effMarkerSpeed = markerSpeed * speedMul;
        effZoneHalf = zoneHalfWidth * zoneMul;

        switch (miniType)
        {
            case Mini.Timing:
            case Mini.MovingZone:
                if (zoneTransform)
                {
                    zoneTransform.localScale = new Vector3(zoneMul, 1f, 1f);
                    zoneTransform.localPosition = new Vector3(0f, zoneTransform.localPosition.y, zoneTransform.localPosition.z);
                }
                if (marker) marker.localPosition = new Vector3(0f, marker.localPosition.y, marker.localPosition.z);
                if (timingBar) timingBar.SetActive(true);
                break;

            case Mini.Mash:
                mashPerTap = 1f / (6 + caught);
                mashDrain = 0.30f + caught * 0.06f;
                mashTimer = mashTime;
                mashProgress = 0f;
                if (mashFill) mashFill.localScale = new Vector3(0f, 1f, 1f);
                if (mashRoot) mashRoot.SetActive(true);
                break;

            case Mini.Arrow:
                arrowLen = Mathf.Clamp(3 + caught / 2, 3, arrowSlots != null ? arrowSlots.Length : 4);
                arrowSeq = new int[arrowLen];
                for (int i = 0; i < arrowLen; i++) arrowSeq[i] = Random.Range(0, 4);
                arrowIdx = 0;
                arrowTimer = 1.5f + arrowLen * 0.8f - caught * 0.1f;
                ConfigureArrows();
                if (arrowRoot) arrowRoot.SetActive(true);
                break;
        }
    }

    private void ConfigureArrows()
    {
        if (arrowSlots == null) return;
        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (arrowSlots[i] == null) continue;
            bool used = i < arrowLen;
            arrowSlots[i].gameObject.SetActive(used);
            if (used)
            {
                arrowSlots[i].transform.localRotation = Quaternion.Euler(0f, 0f, DirAngle(arrowSeq[i]));
                arrowSlots[i].color = (i == arrowIdx) ? cCur : cTodo;
            }
        }
    }

    private static float DirAngle(int d) => d == 0 ? 0f : d == 1 ? -90f : d == 2 ? 180f : 90f;

    public int TickMiniGame(Vector3 playerPos, bool pressed)
    {
        if (!miniActive) return 0;

        if (miniType == Mini.Timing)
        {
            float t = Mathf.PingPong(Time.time * effMarkerSpeed * 2f, 2f) - 1f;
            float x = t * trackHalfWidth;
            if (marker) marker.localPosition = new Vector3(x, marker.localPosition.y, marker.localPosition.z);
            if (pressed) { bool hit = Mathf.Abs(x) <= effZoneHalf; EndMini(); if (!hit) { Flee(playerPos); return 2; } return 1; }
            return 0;
        }
        if (miniType == Mini.MovingZone)
        {
            float t = Mathf.PingPong(Time.time * effMarkerSpeed * 2f, 2f) - 1f;
            float zx = t * trackHalfWidth;
            if (zoneTransform) zoneTransform.localPosition = new Vector3(zx, zoneTransform.localPosition.y, zoneTransform.localPosition.z);
            if (pressed) { bool hit = Mathf.Abs(zx) <= effZoneHalf; EndMini(); if (!hit) { Flee(playerPos); return 2; } return 1; }
            return 0;
        }
        if (miniType == Mini.Mash)
        {
            mashTimer -= Time.deltaTime;
            if (pressed) mashProgress += mashPerTap;
            mashProgress = Mathf.Max(0f, mashProgress - mashDrain * Time.deltaTime);
            if (mashFill) mashFill.localScale = new Vector3(Mathf.Clamp01(mashProgress), 1f, 1f);
            if (mashProgress >= 1f) { EndMini(); return 1; }
            if (mashTimer <= 0f) { EndMini(); Flee(playerPos); return 2; }
            return 0;
        }
        // Arrow
        arrowTimer -= Time.deltaTime;
        int dir = ReadArrow();
        if (dir >= 0)
        {
            if (dir == arrowSeq[arrowIdx])
            {
                if (arrowSlots != null && arrowIdx < arrowSlots.Length && arrowSlots[arrowIdx] != null)
                    arrowSlots[arrowIdx].color = cDone;
                arrowIdx++;
                if (arrowIdx >= arrowLen) { EndMini(); return 1; }
                if (arrowSlots != null && arrowIdx < arrowSlots.Length && arrowSlots[arrowIdx] != null)
                    arrowSlots[arrowIdx].color = cCur;
            }
            else { EndMini(); Flee(playerPos); return 2; }
        }
        if (arrowTimer <= 0f) { EndMini(); Flee(playerPos); return 2; }
        return 0;
    }

    private static int ReadArrow()
    {
        var kb = Keyboard.current;
        if (kb == null) return -1;
        if (kb.upArrowKey.wasPressedThisFrame) return 0;
        if (kb.rightArrowKey.wasPressedThisFrame) return 1;
        if (kb.downArrowKey.wasPressedThisFrame) return 2;
        if (kb.leftArrowKey.wasPressedThisFrame) return 3;
        return -1;
    }

    private void EndMini()
    {
        miniActive = false;
        if (timingBar) timingBar.SetActive(false);
        if (mashRoot) mashRoot.SetActive(false);
        if (arrowRoot) arrowRoot.SetActive(false);
        if (zoneTransform) zoneTransform.localPosition = new Vector3(0f, zoneTransform.localPosition.y, zoneTransform.localPosition.z);
        if (marker) marker.localPosition = new Vector3(0f, marker.localPosition.y, marker.localPosition.z);
    }

    private void Flee(Vector3 from)
    {
        CurrentState = State.Fleeing;
        HideAll();
        targeted = false;
        miniActive = false;
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
        targeted = false;
        miniActive = false;
        HideAll();
    }

    private void HideAll()
    {
        if (selectionRing) selectionRing.SetActive(false);
        if (chargeRoot) chargeRoot.SetActive(false);
        if (timingBar) timingBar.SetActive(false);
        if (mashRoot) mashRoot.SetActive(false);
        if (arrowRoot) arrowRoot.SetActive(false);
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

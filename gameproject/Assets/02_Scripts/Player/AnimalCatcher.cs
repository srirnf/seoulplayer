using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어(사육사). 좌우(A/D) 이동. 동물 근처에서 Space를 길게 눌러 충전 게이지를 채우면 록온,
// 떼면 타이밍 바가 뜨고, 마커가 초록일 때 Space를 누르면 꼬셔진다(따라옴). 실패 시 도망.
public class AnimalCatcher : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.5f;
    [Tooltip("자동 록온 범위")]
    [SerializeField] private float lockRange = 3f;
    [Tooltip("록온까지 Space를 누르고 있어야 하는 시간(초)")]
    [SerializeField] private float lockChargeTime = 0.6f;

    private readonly List<Animal> caughtChain = new List<Animal>();
    private Animal locked;
    private SpriteRenderer sr;
    private float groundY;
    [SerializeField] private float minX = -10000f;
    [SerializeField] private float maxX = 10000f;

    private enum Phase { Idle, Charging, Locked, Timing }
    private Phase phase = Phase.Idle;
    private float charge;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        groundY = transform.position.y;
    }

    public void SetBounds(float min, float max)
    {
        minX = min;
        maxX = max;
    }

    private void Update()
    {
        var gm = ParkGameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;

        // 록온/충전/타이밍 중에는 이동 불가(상호작용에 집중)
        if (phase == Phase.Idle) Move();
        UpdateCatch();
    }

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

    private void UpdateCatch()
    {
        var kb = Keyboard.current;
        bool held = kb != null && kb.spaceKey.isPressed;
        bool pressed = kb != null && kb.spaceKey.wasPressedThisFrame;

        switch (phase)
        {
            case Phase.Idle:
                if (pressed)
                {
                    Animal cand = ParkGameManager.Instance != null
                        ? ParkGameManager.Instance.GetNearestFreeAnimal(transform.position, lockRange)
                        : null;
                    if (cand != null)
                    {
                        locked = cand;
                        locked.SetTargeted(true);
                        charge = 0f;
                        locked.SetLockCharge(0f);
                        phase = Phase.Charging;
                    }
                }
                break;

            case Phase.Charging:
                if (locked == null) { phase = Phase.Idle; break; }
                if (held)
                {
                    charge += Time.deltaTime / Mathf.Max(0.01f, lockChargeTime);
                    if (charge >= 1f)
                    {
                        charge = 1f;
                        locked.SetLockCharge(1f);
                        phase = Phase.Locked; // 충전 완료 = 록온
                    }
                    else
                    {
                        locked.SetLockCharge(charge);
                    }
                }
                else
                {
                    // 다 차기 전에 떼면 취소
                    locked.SetTargeted(false);
                    locked.SetLockCharge(-1f);
                    locked = null;
                    phase = Phase.Idle;
                }
                break;

            case Phase.Locked:
                if (locked == null) { phase = Phase.Idle; break; }
                if (!held) // 떼면 랜덤 미니게임 시작
                {
                    locked.SetLockCharge(-1f);
                    int caught = ParkGameManager.Instance != null ? ParkGameManager.Instance.CaughtCount : 0;
                    locked.StartMiniGame(caught);
                    phase = Phase.Timing;
                }
                break;

            case Phase.Timing:
                if (locked == null) { phase = Phase.Idle; break; }
                int res = locked.TickMiniGame(transform.position, pressed);
                if (res != 0) // 1=성공, 2=실패
                {
                    if (res == 1) CatchAnimal(locked);
                    locked = null;
                    phase = Phase.Idle;
                }
                break;
        }
    }

    private void CatchAnimal(Animal a)
    {
        Transform target = caughtChain.Count == 0 ? transform : caughtChain[caughtChain.Count - 1].transform;
        a.Catch(target);
        caughtChain.Add(a);
        ParkGameManager.Instance?.OnAnimalCaught(a);
    }
}

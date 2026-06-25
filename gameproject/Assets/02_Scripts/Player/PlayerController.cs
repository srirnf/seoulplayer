using UnityEngine;
using UnityEngine.InputSystem;

// 탑뷰 플레이어 이동 (WASD). 신(新) Input System 사용.
// 오브젝트에 Rigidbody2D(Gravity Scale=0, Freeze Rotation Z) + Collider2D 필요.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("걷기 소리용 AudioSource (Loop 체크). 비워두면 소리 없이 동작.")]
    [SerializeField] private AudioSource footstepSource;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 책장 정면뷰 등으로 일시정지 중이면 움직이지 않음
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            moveInput = Vector2.zero;
            SetFootstep(false);
            return;
        }

        var kb = Keyboard.current;
        if (kb == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        moveInput = new Vector2(x, y).normalized;

        SetFootstep(moveInput.sqrMagnitude > 0.01f);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    private void SetFootstep(bool moving)
    {
        if (footstepSource == null) return;
        if (moving && !footstepSource.isPlaying) footstepSource.Play();
        else if (!moving && footstepSource.isPlaying) footstepSource.Stop();
    }
}

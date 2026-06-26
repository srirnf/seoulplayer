using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class bookhansanmaincharacter : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("중력 오프 시 비행 속도")]
    public float flySpeed = 5f;

    private Rigidbody rb;
    private float hInput;
    private float vInput; // 위아래 입력을 받을 변수 추가
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // 좌우 화살표 / AD 입력
        hInput = Input.GetAxis("Horizontal");
        
        // 위아래 화살표 / WS 입력 추가
        vInput = Input.GetAxis("Vertical");

        // 점프 입력 (중력이 켜져있고, 땅에 있을 때만 작동)
        if (Input.GetButtonDown("Jump") && isGrounded && rb.useGravity)
        {
            Jump();
        }

        // G 키를 누르면 중력이 토글(Toggle)됩니다.
        if (Input.GetKeyDown(KeyCode.G))
        {
            rb.useGravity = !rb.useGravity; 
            
            // 중력을 끌 때, 남아있던 하강 관성 때문에 미끄러지는 것을 방지
            if (!rb.useGravity)
            {
                rb.linearVelocity = Vector3.zero; // 완전히 속도를 0으로 멈춤
            }
        }
    }

    void FixedUpdate()
    {
        // 중력이 켜져 있을 때만 지면 체크를 합니다.
        if (rb.useGravity)
        {
            float rayLength = 1.7f;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength);
        }
        else
        {
            isGrounded = false; 
        }

        Move();
    }

    void Move()
    {
        float yVelocity = 0f;

        if (rb.useGravity)
        {
            // [중력 ON] 일반 상태: Y축은 물리 엔진의 중력 속도를 그대로 따릅니다.
            yVelocity = rb.linearVelocity.y;
        }
        else
        {
            // [중력 OFF] 비행 상태: 위아래 화살표 입력값(vInput)에 따라 Y축 속도를 직접 제어합니다.
            yVelocity = vInput * flySpeed;
        }

        // 최종 속도 적용 (X축: 좌우 이동, Y축: 상태별 속도, Z축: 0 고정)
        rb.linearVelocity = new Vector3(hInput * moveSpeed, yVelocity, 0f);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}
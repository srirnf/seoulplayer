using UnityEngine;

public class bukhansanmaincharacter : MonoBehaviour
{
    [Header("이동 속도 설정")]
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector2 moveInput;

    void Start()
    {
        // 오브젝트에 있는 Rigidbody 컴포넌트를 가져옵니다.
        rb = GetComponent<Rigidbody>();
        
        // 3D Rigidbody가 Z축으로 회전하거나 밀려나지 않도록 고정합니다.
        rb.constraints = RigidbodyConstraints.FreezePositionZ | 
                         RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezeRotationY | 
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // 키보드 입력 받기 (WASD 또는 방향키)
        // X축: Left/Right (-1 ~ 1), Y축: Down/Up (-1 ~ 1)
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // 대각선 이동 시 속도가 빨라지는 것을 방지하기 위해 정규화(Normalize)
        if (moveInput.magnitude > 0)
        {
            moveInput.Normalize();
        }
    }

    void FixedUpdate()
    {
        // 물리 연산(Rigidbody 이동)은 FixedUpdate에서 처리하는 것이 안전합니다.
        // Z축은 움직이지 않고 X, Y축으로만 이동력을 부여합니다.
        rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, moveInput.y * moveSpeed, 0f);
    }
}
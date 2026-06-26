using UnityEngine;

public class bukhansan : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    void Update()
    {
        // 키보드 방향키 또는 WASD 입력을 받습니다. (-1.0 ~ 1.0)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 이동 방향 벡터를 생성합니다. (Z축은 0)
        Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;

        // 매 프레임마다 지정한 속도로 이동시킵니다.
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
    }
}
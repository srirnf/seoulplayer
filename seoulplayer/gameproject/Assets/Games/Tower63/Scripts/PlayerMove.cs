using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed = 5.0f; // 청소솔 움직임 속도

    // 화면 안에서만 움직이도록 제한할 가이드라인 (좌우, 위아래 벽)
    private float minX = -6.0f;
    private float maxX = 6.0f;
    private float minY = -4.0f;
    private float maxY = 4.0f;

    void Update()
    {
        // 1. WASD 및 방향키 입력 받기
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, v, 0);

        // 2. 입력받은 방향과 속도에 맞춰 이동시키기
        transform.position += dir.normalized * speed * Time.deltaTime;

        // [핵심] 3. 이동한 후, 플레이어의 위치가 화면 벽을 넘어갔다면 강제로 경계선에 맞춥니다.
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        // 최종 조절된 안전한 위치를 플레이어에게 적용!
        transform.position = new Vector3(clampedX, clampedY, 0);
    }
}
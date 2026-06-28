using UnityEngine;

public class Pigeon : MonoBehaviour
{
    public float speed = 5.0f; // 비둘기 속도
    private int direction = 1; // 1이면 오른쪽, -1이면 왼쪽

    public void SetDirection(float spawnX)
    {
        // 좌우 이미지 반전 및 날아갈 방향 설정 (대각선 삭제, 다시 직선으로!)
        if (spawnX > 0)
        {
            direction = -1;
            transform.localScale = new Vector3(-1, 1, 1); // 왼쪽 바라보기
        }
        else
        {
            direction = 1;
            transform.localScale = new Vector3(1, 1, 1); // 오른쪽 바라보기
        }
    }

    void Update()
    {
        // 무조건 이쁜 일직선으로만 날아갑니다.
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        // 화면 바깥으로 완전히 나가면 삭제
        if (transform.position.x > 9.0f || transform.position.x < -9.0f)
        {
            Destroy(gameObject);
        }
    }

    // [중요] 충돌 판정 정밀 수정
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 부딪힌 물체의 이름이 정확히 "player" 이고
        // 2. 부딪힌 부위가 손잡이 막대가 아닌 '진짜 털 부분(BoxCollider2D)' 일 때만 작동!
        if (collision.gameObject.name == "player" && collision is BoxCollider2D)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.DeductScore(2); // 2점 감점
            }

            Destroy(gameObject); // 비둘기 삭제
        }
    }
}